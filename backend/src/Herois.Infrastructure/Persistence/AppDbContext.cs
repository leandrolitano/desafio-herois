using System.Security.Cryptography;
using Herois.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Herois.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Heroi> Herois => Set<Heroi>();
    public DbSet<Superpoder> Superpoderes => Set<Superpoder>();
    public DbSet<HeroiSuperpoder> HeroisSuperpoderes => Set<HeroiSuperpoder>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var isSqlite = Database.ProviderName?.Contains("Sqlite", StringComparison.OrdinalIgnoreCase) == true;

        modelBuilder.Entity<Heroi>(e =>
        {
            e.ToTable("Herois");
            e.HasKey(x => x.Id);
            e.Property(x => x.Nome).HasMaxLength(120).IsRequired();
            e.Property(x => x.NomeHeroi).HasMaxLength(120).IsRequired();
            e.HasIndex(x => x.NomeHeroi).IsUnique();
            e.Property(x => x.DataNascimento).HasColumnType("datetime2");

            // RowVersion / concurrency:
            // - SqlServer: use native rowversion (store-generated)
            // - Sqlite (tests): generate bytes in SaveChanges and persist as BLOB
            if (isSqlite)
            {
                e.Property(x => x.RowVersion)
                    .HasColumnType("BLOB")
                    .IsRequired()
                    .IsConcurrencyToken()
                    .ValueGeneratedNever();
            }
            else
            {
                e.Property(x => x.RowVersion)
                    .IsRowVersion()
                    .IsConcurrencyToken();
            }
        });

        modelBuilder.Entity<Superpoder>(e =>
        {
            e.ToTable("Superpoderes");
            e.HasKey(x => x.Id);
            // The challenge column is called "Superpoder".
            // We use "Nome" in C# to avoid CS0542.
            e.Property(x => x.Nome).HasColumnName("Superpoder").HasMaxLength(120).IsRequired();
            e.Property(x => x.Descricao).HasMaxLength(400).IsRequired();
        });

        modelBuilder.Entity<HeroiSuperpoder>(e =>
        {
            e.ToTable("HeroisSuperpoderes");
            e.HasKey(x => new { x.HeroiId, x.SuperpoderId });

            e.HasOne(x => x.Heroi)
                .WithMany(h => h.HeroisSuperpoderes)
                .HasForeignKey(x => x.HeroiId);

            e.HasOne(x => x.Superpoder)
                .WithMany(s => s.HeroisSuperpoderes)
                .HasForeignKey(x => x.SuperpoderId);
        });
    }

    public override int SaveChanges()
    {
		if (Database.ProviderName?.Contains("Sqlite", StringComparison.OrdinalIgnoreCase) == true)
            PrepareSqliteRowVersions();

        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
		if (Database.ProviderName?.Contains("Sqlite", StringComparison.OrdinalIgnoreCase) == true)
            PrepareSqliteRowVersions();

        return base.SaveChangesAsync(cancellationToken);
    }

    private void PrepareSqliteRowVersions()
    {
        foreach (var entry in ChangeTracker.Entries<Heroi>())
        {
            if (entry.State == EntityState.Added)
            {
                if (entry.Entity.RowVersion is null || entry.Entity.RowVersion.Length == 0)
                    entry.Entity.RowVersion = NewRowVersion();
            }
            else if (entry.State == EntityState.Modified)
            {
                // Bump RowVersion on every update for Sqlite so concurrency checks are meaningful in tests.
                entry.Entity.RowVersion = NewRowVersion();
                entry.Property(x => x.RowVersion).IsModified = true;
            }
        }
    }

    private static byte[] NewRowVersion()
    {
        var bytes = new byte[8];
        RandomNumberGenerator.Fill(bytes);
        return bytes;
    }
}
