using Herois.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace Herois.Api.IntegrationTests;

/// <summary>
/// Sobe a API em memória para testes de integração.
/// Trocamos o SQL Server por SQLite in-memory, mantendo um banco relacional real
/// (bom para testar N:N, índices, etc.) sem depender de Docker.
/// </summary>
public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            // Remove o AppDbContext registrado pelo Program.cs (SQL Server)
            var dbContextDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));

            if (dbContextDescriptor is not null)
                services.Remove(dbContextDescriptor);

            // Mantém uma conexão aberta para o ciclo de vida do host (SQLite :memory:)
            var sqlite = new SqliteConnection("Data Source=:memory:");
            sqlite.Open();

            services.AddSingleton(sqlite);

            services.AddDbContext<AppDbContext>(opt =>
            {
                opt.UseSqlite(sqlite);
                opt.ReplaceService<IModelCacheKeyFactory, ProviderModelCacheKeyFactory>();
            });
        });
    }
}
