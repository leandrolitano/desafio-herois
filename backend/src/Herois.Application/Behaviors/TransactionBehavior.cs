using Herois.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Herois.Application.Behaviors;

public class TransactionBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly AppDbContext _db;

    public TransactionBehavior(AppDbContext db)
    {
        _db = db;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        // Só envolve Commands em transação
        var name = typeof(TRequest).Name;
        if (!name.EndsWith("Command", StringComparison.Ordinal))
            return await next();

        // Evita abrir transacao se ja houver uma ativa
        if (_db.Database.CurrentTransaction is not null)
            return await next();

        await using var tx = await _db.Database.BeginTransactionAsync(ct);
        try
        {
            var response = await next();
            await tx.CommitAsync(ct);
            return response;
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }
    }
}
