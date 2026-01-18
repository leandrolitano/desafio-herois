using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Herois.Api.Errors;

public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        // Fallback global handler. Validation should normally be handled earlier by
        // FluentValidationExceptionMiddleware, but we still handle it here to ensure consistent
        // RFC7807 payloads even if middleware ordering changes.
        object problem;

        switch (exception)
        {
            case ValidationException vex:
            {
                problem = new ApiValidationProblemDetails
                {
                    Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                    Title = "Falha de validacao",
                    Status = StatusCodes.Status400BadRequest,
                    Detail = "Uma ou mais regras de validacao falharam.",
                    Instance = httpContext.Request.Path,
                    Errors = vex.Errors
                        .GroupBy(e => e.PropertyName)
                        .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray()),
                    TraceId = httpContext.TraceIdentifier
                };

                if (httpContext.Items.TryGetValue("CorrelationId", out var cid) && cid is string correlationId)
                    ((ApiValidationProblemDetails)problem).CorrelationId = correlationId;

                httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
                break;
            }

            case FormatException fex:
                problem = new ProblemDetails
                {
                    Title = "Requisicao invalida",
                    Status = StatusCodes.Status400BadRequest,
                    Detail = fex.Message
                };
                httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
                break;

            default:
                _logger.LogError(exception, "Unhandled exception");
                problem = new ProblemDetails
                {
                    Title = "Erro inesperado",
                    Status = StatusCodes.Status500InternalServerError,
                    Detail = "Ocorreu um erro inesperado."
                };
                httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
                break;
        }

        // Anexa correlationId/traceId ao ProblemDetails "padrao"
        if (problem is ProblemDetails pd)
        {
            if (httpContext.Items.TryGetValue("CorrelationId", out var cid2) && cid2 is string correlationId2)
                pd.Extensions["correlationId"] = correlationId2;

            pd.Extensions["traceId"] = httpContext.TraceIdentifier;
        }

        httpContext.Response.ContentType = "application/problem+json";
        await httpContext.Response.WriteAsJsonAsync(problem, cancellationToken);
        return true;
    }
}
