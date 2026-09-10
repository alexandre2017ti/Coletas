using Microsoft.AspNetCore.RateLimiting;

namespace Coletas.Api.Configuration;

/// <summary>Centraliza a política de limitação das rotas de acesso.</summary>
public static class RateLimitingConfiguration
{
    /// <summary>Preserva os limites existentes durante a reorganização.</summary>
    public static void AddApiRateLimiting(this IServiceCollection services)
    {
        // Motivo: não alterar a política comercial/técnica junto da extração de arquivos.
        // Mudança: docs/mudancas/2026-09-10-08-organizacao-api.md
        services.AddRateLimiter(options =>
            options.AddFixedWindowLimiter("auth", limiter =>
            {
                limiter.PermitLimit = 10;
                limiter.Window = TimeSpan.FromMinutes(1);
                limiter.QueueLimit = 0;
            }));

    }
}
