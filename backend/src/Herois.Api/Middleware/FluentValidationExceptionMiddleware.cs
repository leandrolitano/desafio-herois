using FluentValidation;
using Herois.Api.Errors;

namespace Herois.Api.Middleware;

/// <summary>
/// Intercepts FluentValidation.ValidationException and returns an RFC7807 payload (application/problem+json)
/// without going through ASP.NET Core ExceptionHandlerMiddleware.
/// This avoids error logs that look like "unhandled exception" for expected 400 validation failures.
/// </summary>
public sealed class FluentValidationExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<FluentValidationExceptionMiddleware> _logger;

    public FluentValidationExceptionMiddleware(RequestDelegate next, ILogger<FluentValidationExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task Invoke(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (ValidationException vex)
        {
            if (context.Response.HasStarted)
                throw;

            _logger.LogInformation("Validation failed for {Method} {Path}", context.Request.Method, context.Request.Path);

            var problem = new ApiValidationProblemDetails
            {
                Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                Title = "Falha de validacao",
                Status = StatusCodes.Status400BadRequest,
                Detail = "Uma ou mais regras de validacao falharam.",
                Instance = context.Request.Path,
                Errors = vex.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray()),
                TraceId = context.TraceIdentifier
            };

            if (context.Items.TryGetValue(CorrelationIdMiddleware.ItemName, out var cid) && cid is string correlationId)
                problem.CorrelationId = correlationId;

            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            context.Response.ContentType = "application/problem+json";
            await context.Response.WriteAsJsonAsync(problem);
        }
    }
}
