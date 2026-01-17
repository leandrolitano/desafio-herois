using Herois.Api.Contracts;
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
    public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] string? search = null, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetHeroesQuery(page, pageSize, search), ct);
        return ToActionResult(result);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetHeroByIdQuery(id), ct);
        return ToActionResult(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateHeroRequest body, CancellationToken ct)
    {
        var cmd = new CreateHeroCommand(body.Nome, body.NomeHeroi, body.DataNascimento, body.Altura, body.Peso, body.SuperpoderIds);
        var result = await _mediator.Send(cmd, ct);

        if (result.Success && result.StatusCode == 201 && result.Data is not null)
            return CreatedAtAction(nameof(GetById), new { id = result.Data.Id }, result);

        return ToActionResult(result);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateHeroRequest body, CancellationToken ct)
    {
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
        var result = await _mediator.Send(new DeleteHeroCommand(id), ct);
        return ToActionResult(result);
    }

    private IActionResult ToActionResult<T>(Herois.Application.Common.Result<T> result)
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
