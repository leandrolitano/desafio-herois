using Herois.Application.Common;
using Herois.Application.DTOs;
using MediatR;

namespace Herois.Application.Herois;

public record UpdateHeroCommand(
    int Id,
    string Nome,
    string NomeHeroi,
    DateTime DataNascimento,
    double Altura,
    double Peso,
    List<int> SuperpoderIds,
    byte[] RowVersion
) : IRequest<Result<HeroiDto>>;
