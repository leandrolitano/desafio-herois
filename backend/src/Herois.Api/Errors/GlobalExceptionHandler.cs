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
        // A resposta final sempre sera ProblemDetails
        ProblemDetails problem;

        switch (exception)
        {
            case ValidationException vex:
                problem = new ValidationProblemDetails(
                    vex.Errors
                        .GroupBy(e => e.PropertyName)
                        .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray()))
                {
                    Title = "Falha de validacao",
                    Status = StatusCodes.Status400BadRequest,
                    Detail = "Uma ou mais regras de validacao falharam."
                };
                httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
                break;

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

        // Anexa correlationId, se existir
        if (httpContext.Items.TryGetValue("CorrelationId", out var cid) && cid is string correlationId)
        {
            problem.Extensions["correlationId"] = correlationId;
        }

        problem.Extensions["traceId"] = httpContext.TraceIdentifier;

        await httpContext.Response.WriteAsJsonAsync(problem, cancellationToken);
        return true;
    }
}
