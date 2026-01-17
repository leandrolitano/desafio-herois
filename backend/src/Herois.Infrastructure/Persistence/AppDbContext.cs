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
        modelBuilder.Entity<Heroi>(e =>
        {
            e.ToTable("Herois");
            e.HasKey(x => x.Id);
            e.Property(x => x.Nome).HasMaxLength(120).IsRequired();
            e.Property(x => x.NomeHeroi).HasMaxLength(120).IsRequired();
            e.HasIndex(x => x.NomeHeroi).IsUnique();
            e.Property(x => x.DataNascimento).HasColumnType("datetime2");
            e.Property(x => x.RowVersion).IsRowVersion().IsConcurrencyToken();
        });

        modelBuilder.Entity<Superpoder>(e =>
        {
            e.ToTable("Superpoderes");
            e.HasKey(x => x.Id);
            // A coluna do desafio chama-se "Superpoder".
            // Na entidade usamos "Nome" para evitar conflito de nome (CS0542).
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
}
