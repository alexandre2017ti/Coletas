using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Coletas.Application.Foundation;
using Coletas.Application.Identity;
using Coletas.Application.Pricing;
using Coletas.Domain.Identity;
using Coletas.Infrastructure;
using Coletas.Infrastructure.Identity;
using Coletas.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text.Json.Serialization;

namespace Coletas.Api.Configuration;

/// <summary>Configura validação JWT e permissões da API.</summary>
public static class AuthenticationConfiguration
{
    /// <summary>Registra a autenticação e a política administrativa existentes.</summary>
    public static void AddApiAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                var jwt = configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new();
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SigningKey)),
                    ValidateIssuer = true,
                    ValidIssuer = jwt.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwt.Audience,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromSeconds(30),
                    NameClaimType = ClaimTypes.NameIdentifier,
                    RoleClaimType = ClaimTypes.Role
                };
                options.Events = new JwtBearerEvents
                {
                    OnTokenValidated = async context =>
                    {
                        // Bloqueio e revogação precisam valer também para JWT já emitido.
                        // Mudança: docs/mudancas/2026-09-15-01-analise-administrativa.md
                        var db = context.HttpContext.RequestServices.GetRequiredService<ColetasDbContext>();
                        if (!Guid.TryParse(context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier), out var id)
                            || !Guid.TryParse(context.Principal?.FindFirstValue("sid"), out var sid))
                        { context.Fail("Sessão inválida."); return; }
                        var user = await db.Users.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, context.HttpContext.RequestAborted);
                        var scope = context.Principal?.FindFirstValue("scope");
                        var purpose = scope == "onboarding" ? "onboarding" : "refresh";
                        if (user is null || user.Status == UserStatus.Blocked
                            || user.Role.ToString() != context.Principal?.FindFirstValue(ClaimTypes.Role)
                            || scope is not ("access" or "onboarding")
                            || scope == "access" && user.Status != UserStatus.Active
                            || !await db.SecurityTokens.AnyAsync(x => x.Id == sid && x.UserId == id && !x.Used
                                && x.Purpose == purpose && x.ExpiresAt > DateTimeOffset.UtcNow, context.HttpContext.RequestAborted))
                            context.Fail("Sessão inválida.");
                    }
                };
            });
        services.AddAuthorization(options =>
        {
            options.DefaultPolicy = new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser().RequireClaim("scope", "access").Build();
            options.AddPolicy("AccountAccess", policy => policy.RequireAuthenticatedUser().RequireClaim("scope", "access", "onboarding"));
            options.AddPolicy("AdminOnly", policy => policy.RequireAuthenticatedUser().RequireClaim("scope", "access").RequireRole(nameof(UserRole.Admin)));
        });

    }
}
