using Herois.Domain.Entities;
using Herois.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Herois.Tests.Common;

/// <summary>
/// Banco SQLite em memória para testes.
/// Mantém a conexão aberta para que o DB exista durante o teste.
/// </summary>
public sealed class SqliteTestDb : IAsyncDisposable
{
    private readonly SqliteConnection _connection;

    public AppDbContext Db { get; }

    private SqliteTestDb(SqliteConnection connection, AppDbContext db)
    {
        _connection = connection;
        Db = db;
    }

    public static async Task<SqliteTestDb> CreateAsync(bool seedDefaultSuperpowers = true)
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .ReplaceService<IModelCacheKeyFactory, ProviderModelCacheKeyFactory>()
            .Options;

        var db = new AppDbContext(options);
        await db.Database.EnsureCreatedAsync();

        if (seedDefaultSuperpowers)
            await SeedDefaultSuperpowersAsync(db);

        return new SqliteTestDb(connection, db);
    }

    public static async Task SeedDefaultSuperpowersAsync(AppDbContext db)
    {
        if (await db.Superpoderes.AnyAsync())
            return;

        db.Superpoderes.AddRange(
            new Superpoder { Id = 1, Nome = "Força", Descricao = "Força física acima do normal" },
            new Superpoder { Id = 2, Nome = "Velocidade", Descricao = "Movimenta-se muito rápido" },
            new Superpoder { Id = 3, Nome = "Voo", Descricao = "Capacidade de voar" },
            new Superpoder { Id = 4, Nome = "Invisibilidade", Descricao = "Pode ficar invisível" }
        );

        await db.SaveChangesAsync();
    }

    public async ValueTask DisposeAsync()
    {
        await Db.DisposeAsync();
        await _connection.DisposeAsync();
    }
}
