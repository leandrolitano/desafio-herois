using Herois.Application.Common;
using MediatR;

namespace Herois.Application.Herois;

public record DeleteHeroCommand(int Id) : IRequest<Result<string>>;
