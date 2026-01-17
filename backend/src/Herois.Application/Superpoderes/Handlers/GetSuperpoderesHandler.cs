using Herois.Application.Common;
using Herois.Application.DTOs;
using Herois.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace Herois.Application.Superpoderes.Handlers;

public class GetSuperpoderesHandler : IRequestHandler<GetSuperpoderesQuery, Result<List<SuperpoderDto>>>
{
    private const string CacheKey = "superpoderes:v1";

    private readonly AppDbContext _db;
    private readonly IMemoryCache _cache;

    public GetSuperpoderesHandler(AppDbContext db, IMemoryCache cache)
    {
        _db = db;
        _cache = cache;
    }

    public async Task<Result<List<SuperpoderDto>>> Handle(GetSuperpoderesQuery request, CancellationToken ct)
    {
        var list = await _cache.GetOrCreateAsync(CacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);

            return await _db.Superpoderes
                .AsNoTracking()
                .OrderBy(p => p.Id)
                .Select(p => new SuperpoderDto(p.Id, p.Nome, p.Descricao))
                .ToListAsync(ct);
        });

        return Result<List<SuperpoderDto>>.Ok(list ?? new List<SuperpoderDto>());
    }
}
