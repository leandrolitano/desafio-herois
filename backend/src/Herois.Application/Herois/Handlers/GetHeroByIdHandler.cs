using Herois.Application.Common;
using Herois.Application.DTOs;
using Herois.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Herois.Application.Herois.Handlers;

public class GetHeroByIdHandler : IRequestHandler<GetHeroByIdQuery, Result<HeroiDto>>
{
    private readonly AppDbContext _db;

    public GetHeroByIdHandler(AppDbContext db) => _db = db;

    public async Task<Result<HeroiDto>> Handle(GetHeroByIdQuery request, CancellationToken ct)
    {
        if (request.Id <= 0)
            return Result<HeroiDto>.Fail(400, "Id invalido.");

        var hero = await _db.Herois
            .AsNoTracking()
            .Include(h => h.HeroisSuperpoderes)
                .ThenInclude(hs => hs.Superpoder)
            .FirstOrDefaultAsync(h => h.Id == request.Id, ct);

        if (hero is null)
            return Result<HeroiDto>.Fail(404, "Heroi nao encontrado.");

        var dto = new HeroiDto(
            hero.Id,
            hero.Nome,
            hero.NomeHeroi,
            hero.DataNascimento,
            hero.Altura,
            hero.Peso,
            Convert.ToBase64String(hero.RowVersion ?? Array.Empty<byte>()),
            hero.HeroisSuperpoderes
                .Where(x => x.Superpoder != null)
                .Select(x => new SuperpoderDto(x.Superpoder!.Id, x.Superpoder!.Nome, x.Superpoder!.Descricao))
                .ToList()
        );

        return Result<HeroiDto>.Ok(dto);
    }
}
