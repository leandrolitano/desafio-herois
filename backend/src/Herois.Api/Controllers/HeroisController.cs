using Herois.Api.Contracts;
using Herois.Api.Errors;
using Herois.Api.Middleware;
using Herois.Application.Common;
using Herois.Application.Herois;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Herois.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HeroisController : ControllerBase
{
    private readonly IMediator _mediator;

    public HeroisController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetHeroesQuery(page, pageSize, search), ct);
        return ToActionResult(result);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        if (id <= 0)
            return ValidationFail(new Dictionary<string, string[]>
            {
                ["Id"] = new[] { "Id deve ser maior que 0" }
            });

        var result = await _mediator.Send(new GetHeroByIdQuery(id), ct);
        return ToActionResult(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateHeroRequest body, CancellationToken ct)
    {
        // Fast-fail for common validation cases (helps integration tests and avoids noisy logs)
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(body.Nome))
            errors["Nome"] = new[] { "Nome e obrigatorio" };

        if (string.IsNullOrWhiteSpace(body.NomeHeroi))
            errors["NomeHeroi"] = new[] { "NomeHeroi e obrigatorio" };

        if (body.SuperpoderIds is null || body.SuperpoderIds.Count == 0)
            errors["SuperpoderIds"] = new[] { "Selecione ao menos 1 superpoder" };

        if (errors.Count > 0)
            return ValidationFail(errors);

        var cmd = new CreateHeroCommand(body.Nome, body.NomeHeroi, body.DataNascimento, body.Altura, body.Peso, body.SuperpoderIds);
        var result = await _mediator.Send(cmd, ct);

        if (result.Success && result.StatusCode == 201 && result.Data is not null)
            return CreatedAtAction(nameof(GetById), new { id = result.Data.Id }, result);

        return ToActionResult(result);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateHeroRequest body, CancellationToken ct)
    {
        if (id <= 0)
            return ValidationFail(new Dictionary<string, string[]>
            {
                ["Id"] = new[] { "Id deve ser maior que 0" }
            });

        var row = string.IsNullOrWhiteSpace(body.RowVersion)
            ? Array.Empty<byte>()
            : Convert.FromBase64String(body.RowVersion);

        var cmd = new UpdateHeroCommand(id, body.Nome, body.NomeHeroi, body.DataNascimento, body.Altura, body.Peso, body.SuperpoderIds, row);
        var result = await _mediator.Send(cmd, ct);
        return ToActionResult(result);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        if (id <= 0)
            return ValidationFail(new Dictionary<string, string[]>
            {
                ["Id"] = new[] { "Id deve ser maior que 0" }
            });

        var result = await _mediator.Send(new DeleteHeroCommand(id), ct);
        return ToActionResult(result);
    }

    private ObjectResult ValidationFail(Dictionary<string, string[]> errors)
    {
        var problem = new ApiValidationProblemDetails
        {
            Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
            Title = "Falha de validacao",
            Status = StatusCodes.Status400BadRequest,
            Detail = "Uma ou mais regras de validacao falharam.",
            Instance = HttpContext.Request.Path,
            Errors = errors,
            TraceId = HttpContext.TraceIdentifier
        };

        if (HttpContext.Items.TryGetValue(CorrelationIdMiddleware.ItemName, out var cid) && cid is string correlationId)
            problem.CorrelationId = correlationId;

        var result = new ObjectResult(problem) { StatusCode = StatusCodes.Status400BadRequest };
        result.ContentTypes.Add("application/problem+json");
        return result;
    }

    private IActionResult ToActionResult<T>(Result<T> result)
    {
        if (result.Success)
            return StatusCode(result.StatusCode, result);

        // Padroniza erro em ProblemDetails
        return Problem(
            title: "Erro",
            detail: result.Message,
            statusCode: result.StatusCode
        );
    }
}
