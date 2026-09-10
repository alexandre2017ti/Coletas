using Coletas.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Coletas.Api.Database;

/// <summary>Executa migrations somente mediante solicitação explícita.</summary>
public static class DatabaseMigrationRunner
{
    /// <summary>Retorna true quando a aplicação deve encerrar após migrar.</summary>
    public static async Task<bool> RunIfRequestedAsync(WebApplication app, string[] args)
    {
        if (!args.Contains("--migrate", StringComparer.Ordinal)) return false;
        // Motivo: migrations explícitas evitam alteração concorrente de schema no início da API.
        // Mudanças: docs/mudancas/2026-09-09-02-fase-zero.md e 2026-09-10-08-organizacao-api.md
        using var scope = app.Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<ColetasDbContext>()
            .Database.MigrateAsync(app.Lifetime.ApplicationStopping);
        return true;
    }
}
