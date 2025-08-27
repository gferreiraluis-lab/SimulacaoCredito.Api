using Microsoft.EntityFrameworkCore;
using SimulacaoCredito.Api.Domain;

namespace SimulacaoCredito.Api.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> opt) : base(opt) { }

    public DbSet<SimulacaoEntity> Simulacoes => Set<SimulacaoEntity>();
    public DbSet<SimulacaoParcelaEntity> SimulacaoParcelas => Set<SimulacaoParcelaEntity>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        // SIMULACAO
        b.Entity<SimulacaoEntity>(e =>
        {
            e.ToTable("SIMULACAO");
            e.HasKey(x => x.IdSimulacao);
            e.Property(x => x.DtCriacao).IsRequired();
            e.Property(x => x.ValorDesejado).HasColumnType("NUMERIC");
            e.Property(x => x.TaxaJuros).HasColumnType("NUMERIC");

            e.HasMany(x => x.Parcelas)
             .WithOne(p => p.Simulacao!)
             .HasForeignKey(p => p.IdSimulacao)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // SIMULACAO_PARCELA
        b.Entity<SimulacaoParcelaEntity>(e =>
        {
            e.ToTable("SIMULACAO_PARCELA");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).ValueGeneratedOnAdd();
            e.Property(x => x.Tipo).HasMaxLength(10).IsRequired();
            e.Property(x => x.Numero).IsRequired();
            e.Property(x => x.ValorAmortizacao).HasColumnType("NUMERIC");
            e.Property(x => x.ValorJuros).HasColumnType("NUMERIC");
            e.Property(x => x.ValorPrestacao).HasColumnType("NUMERIC");

            e.HasIndex(x => new { x.IdSimulacao, x.Tipo, x.Numero });
        });
    }
}
