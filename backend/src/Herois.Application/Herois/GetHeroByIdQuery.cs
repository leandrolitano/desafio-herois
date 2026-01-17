using Herois.Application.Common;
using Herois.Application.DTOs;
using MediatR;

namespace Herois.Application.Herois;

public record GetHeroByIdQuery(int Id) : IRequest<Result<HeroiDto>>;
