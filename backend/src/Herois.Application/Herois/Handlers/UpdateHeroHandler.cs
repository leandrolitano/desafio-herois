using Herois.Application.Common;
using Herois.Application.DTOs;
using Herois.Domain.Entities;
using Herois.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Herois.Application.Herois.Handlers;

public class UpdateHeroHandler : IRequestHandler<UpdateHeroCommand, Result<HeroiDto>>
{
    private readonly AppDbContext _db;

    public UpdateHeroHandler(AppDbContext db) => _db = db;

    public async Task<Result<HeroiDto>> Handle(UpdateHeroCommand request, CancellationToken ct)
    {
        if (request.Id <= 0)
            return Result<HeroiDto>.Fail(400, "Id invalido.");

        var hero = await _db.Herois
            .Include(h => h.HeroisSuperpoderes)
            .FirstOrDefaultAsync(h => h.Id == request.Id, ct);

        if (hero is null)
            return Result<HeroiDto>.Fail(404, "Heroi nao encontrado.");

        // Concurrency: garante que estamos atualizando a versao que o usuario leu
        _db.Entry(hero).Property(h => h.RowVersion).OriginalValue = request.RowVersion;

        var duplicate = await _db.Herois
            .AsNoTracking()
            .AnyAsync(h => h.NomeHeroi == request.NomeHeroi && h.Id != request.Id, ct);

        if (duplicate)
            return Result<HeroiDto>.Fail(409, "Ja existe um heroi com este Nome de Heroi.");

        hero.Nome = request.Nome.Trim();
        hero.NomeHeroi = request.NomeHeroi.Trim();
        hero.DataNascimento = request.DataNascimento;
        hero.Altura = request.Altura;
        hero.Peso = request.Peso;

        // Atualiza N:N
        var desired = request.SuperpoderIds.Distinct().ToHashSet();
        hero.HeroisSuperpoderes.RemoveAll(x => !desired.Contains(x.SuperpoderId));

        var existing = hero.HeroisSuperpoderes.Select(x => x.SuperpoderId).ToHashSet();
        foreach (var id in desired)
        {
            if (!existing.Contains(id))
                hero.HeroisSuperpoderes.Add(new HeroiSuperpoder { HeroiId = hero.Id, SuperpoderId = id });
        }

        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Result<HeroiDto>.Fail(409, "Conflito de concorrencia: o registro foi alterado por outro processo.");
        }

        var updated = await _db.Herois
            .AsNoTracking()
            .Include(h => h.HeroisSuperpoderes)
                .ThenInclude(hs => hs.Superpoder)
            .FirstAsync(h => h.Id == hero.Id, ct);

        var dto = new HeroiDto(
            updated.Id,
            updated.Nome,
            updated.NomeHeroi,
            updated.DataNascimento,
            updated.Altura,
            updated.Peso,
            Convert.ToBase64String(updated.RowVersion ?? Array.Empty<byte>()),
            updated.HeroisSuperpoderes
                .Where(x => x.Superpoder != null)
                .Select(x => new SuperpoderDto(x.Superpoder!.Id, x.Superpoder!.Nome, x.Superpoder!.Descricao))
                .ToList()
        );

        return Result<HeroiDto>.Ok(dto, "Heroi atualizado com sucesso.");
    }
}
