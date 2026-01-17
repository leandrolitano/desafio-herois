using Herois.Application.Common;
using Herois.Application.DTOs;
using Herois.Domain.Entities;
using Herois.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Herois.Application.Herois.Handlers;

public class CreateHeroHandler : IRequestHandler<CreateHeroCommand, Result<HeroiDto>>
{
    private readonly AppDbContext _db;

    public CreateHeroHandler(AppDbContext db) => _db = db;

    public async Task<Result<HeroiDto>> Handle(CreateHeroCommand request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.NomeHeroi))
            return Result<HeroiDto>.Fail(400, "NomeHeroi é obrigatório.");

        var exists = await _db.Herois.AsNoTracking().AnyAsync(h => h.NomeHeroi == request.NomeHeroi, ct);
        if (exists)
            return Result<HeroiDto>.Fail(409, "Já existe um herói com este Nome de Herói.");

        var poderes = await _db.Superpoderes
            .Where(p => request.SuperpoderIds.Contains(p.Id))
            .ToListAsync(ct);

        var hero = new Heroi
        {
            Nome = request.Nome.Trim(),
            NomeHeroi = request.NomeHeroi.Trim(),
            DataNascimento = request.DataNascimento,
            Altura = request.Altura,
            Peso = request.Peso,
        };

        foreach (var p in poderes)
        {
            hero.HeroisSuperpoderes.Add(new HeroiSuperpoder { SuperpoderId = p.Id });
        }

        _db.Herois.Add(hero);
        await _db.SaveChangesAsync(ct);

        // Recarrega com navegações
        var created = await _db.Herois
            .AsNoTracking()
            .Include(h => h.HeroisSuperpoderes)
                .ThenInclude(hs => hs.Superpoder)
            .FirstAsync(h => h.Id == hero.Id, ct);

        var dto = new HeroiDto(
            created.Id,
            created.Nome,
            created.NomeHeroi,
            created.DataNascimento,
            created.Altura,
            created.Peso,
            Convert.ToBase64String(created.RowVersion ?? Array.Empty<byte>()),
            created.HeroisSuperpoderes
                .Where(x => x.Superpoder != null)
                .Select(x => new SuperpoderDto(x.Superpoder!.Id, x.Superpoder!.Nome, x.Superpoder!.Descricao))
                .ToList()
        );

        return Result<HeroiDto>.Created(dto, "Herói cadastrado com sucesso.");
    }
}
