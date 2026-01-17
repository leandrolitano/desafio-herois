using Herois.Application.Common;
using Herois.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Herois.Application.Herois.Handlers;

public class DeleteHeroHandler : IRequestHandler<DeleteHeroCommand, Result<string>>
{
    private readonly AppDbContext _db;

    public DeleteHeroHandler(AppDbContext db) => _db = db;

    public async Task<Result<string>> Handle(DeleteHeroCommand request, CancellationToken ct)
    {
        if (request.Id <= 0)
            return Result<string>.Fail(400, "Id inválido.");

        var hero = await _db.Herois.FirstOrDefaultAsync(h => h.Id == request.Id, ct);
        if (hero is null)
            return Result<string>.Fail(404, "Herói não encontrado.");

        _db.Herois.Remove(hero);
        await _db.SaveChangesAsync(ct);

        return Result<string>.Ok("OK", "Herói removido com sucesso.");
    }
}
