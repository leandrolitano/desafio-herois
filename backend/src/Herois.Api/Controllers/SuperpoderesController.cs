using Herois.Application.Common;
using Herois.Application.Superpoderes;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Herois.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SuperpoderesController : ControllerBase
{
    private readonly IMediator _mediator;

    public SuperpoderesController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var result = await _mediator.Send(new GetSuperpoderesQuery(), ct);
        if (result.Success)
            return StatusCode(result.StatusCode, result);

        return Problem(title: "Erro", detail: result.Message, statusCode: result.StatusCode);
    }
}
