using Herois.Application.Common;
using Herois.Application.DTOs;
using MediatR;

namespace Herois.Application.Herois;

public record GetHeroesQuery(
    int Page = 1,
    int PageSize = 20,
    string? Search = null
) : IRequest<Result<PagedResult<HeroiDto>>>;
