using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
namespace Coletas.Infrastructure.Persistence;
/// <summary>Geração offline de migrations.</summary>
public sealed class ColetasDbContextFactory : IDesignTimeDbContextFactory<ColetasDbContext>
{
    /// <inheritdoc />
    public ColetasDbContext CreateDbContext(string[] args)
    {
        // Motivo: gerar migration não exige credenciais reais nem conexão.
        // Mudança: docs/mudancas/2026-09-09-02-fase-zero.md
        var connection = Environment.GetEnvironmentVariable("Infrastructure__Postgres")
            ?? "Host=localhost;Database=coletas;Username=coletas";
        return new(new DbContextOptionsBuilder<ColetasDbContext>()
            .UseNpgsql(connection, postgres => postgres.UseNetTopologySuite()).Options);
    }
}
