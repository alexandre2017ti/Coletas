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
            });
        services.AddAuthorization(options =>
            options.AddPolicy("AdminOnly", policy => policy.RequireRole(nameof(UserRole.Admin))));

    }
}
