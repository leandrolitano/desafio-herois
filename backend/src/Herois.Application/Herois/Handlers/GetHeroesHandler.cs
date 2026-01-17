using Herois.Application.Common;
using Herois.Application.DTOs;
using Herois.Domain.Entities;
using Herois.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Herois.Application.Herois.Handlers;

public class GetHeroesHandler : IRequestHandler<GetHeroesQuery, Result<PagedResult<HeroiDto>>>
{
    private readonly AppDbContext _db;

    public GetHeroesHandler(AppDbContext db) => _db = db;

    public async Task<Result<PagedResult<HeroiDto>>> Handle(GetHeroesQuery request, CancellationToken ct)
    {
        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = request.PageSize < 1 ? 20 : request.PageSize;
        if (pageSize > 100) pageSize = 100;

        // Use the Domain entity type directly; the EF mapping controls table/column names.
        // This also avoids any ambiguity between similarly named namespaces/classes.
        IQueryable<Heroi> query = _db.Herois
            .AsNoTracking()
            .Include(h => h.HeroisSuperpoderes)
                .ThenInclude(hs => hs.Superpoder);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var s = request.Search.Trim();
            query = query.Where(h => h.Nome.Contains(s) || h.NomeHeroi.Contains(s));
        }

        var total = await query.CountAsync(ct);
        if (total == 0)
            return Result<PagedResult<HeroiDto>>.Fail(404, "Nenhum heroi cadastrado.");

        var heroes = await query
            .OrderBy(h => h.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        var items = heroes.Select(h => new HeroiDto(
            h.Id,
            h.Nome,
            h.NomeHeroi,
            h.DataNascimento,
            h.Altura,
            h.Peso,
            Convert.ToBase64String(h.RowVersion ?? Array.Empty<byte>()),
            h.HeroisSuperpoderes
                .Where(x => x.Superpoder != null)
                .Select(x => new SuperpoderDto(x.Superpoder!.Id, x.Superpoder!.Nome, x.Superpoder!.Descricao))
                .ToList()
        )).ToList();

        var paged = new PagedResult<HeroiDto>(items, total, page, pageSize);
        return Result<PagedResult<HeroiDto>>.Ok(paged);
    }
}
