using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Herois.Infrastructure.Persistence;

/// <summary>
/// Ensures EF Core builds a separate model per provider (SqlServer vs Sqlite).
/// Required because our RowVersion mapping differs by provider.
/// </summary>
public sealed class ProviderModelCacheKeyFactory : IModelCacheKeyFactory
{
    public object Create(DbContext context, bool designTime)
        => (context.GetType(), context.Database.ProviderName, designTime);
}
