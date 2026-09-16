using Coletas.Infrastructure.Persistence;
using Coletas.Application.Identity;
using Coletas.Application.Pricing;
using Coletas.Infrastructure.Pricing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Coletas.Infrastructure.Identity;
namespace Coletas.Infrastructure;
/// <summary>Composição das dependências externas.</summary>
public static class DependencyInjection
{
    /// <summary>Registra banco, cache e prontidão.</summary>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<InfrastructureOptions>()
            .Bind(configuration.GetSection(InfrastructureOptions.SectionName))
            .Validate(x => !string.IsNullOrWhiteSpace(x.Postgres), "Configure Infrastructure:Postgres.")
            .Validate(x => !string.IsNullOrWhiteSpace(x.Redis), "Configure Infrastructure:Redis.")
            .ValidateOnStart();
        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName))
            .Validate(x => x.SigningKey.Length >= 32, "Jwt:SigningKey deve ter ao menos 32 caracteres e ser fornecida por segredo do ambiente.")
            .Validate(x => !string.IsNullOrWhiteSpace(x.Issuer), "Jwt:Issuer é obrigatório.")
            .Validate(x => !string.IsNullOrWhiteSpace(x.Audience), "Jwt:Audience é obrigatório.")
            .Validate(x => x.ExpirationMinutes is >= 5 and <= 60, "Jwt:ExpirationMinutes deve estar entre 5 e 60.")
            .ValidateOnStart();
        services.AddDbContext<ColetasDbContext>((provider, options) =>
            options.UseNpgsql(provider.GetRequiredService<IOptions<InfrastructureOptions>>().Value.Postgres,
                postgres => postgres.UseNetTopologySuite()));
        services.AddStackExchangeRedisCache(_ => { });
        services.AddOptions<RedisCacheOptions>()
            .Configure<IOptions<InfrastructureOptions>>((cache, options) =>
            {
                cache.Configuration = options.Value.Redis;
                cache.InstanceName = "coletas:";
            });
        services.AddHealthChecks().AddCheck<DependencyHealthCheck>("dependencies", tags: ["ready"]);
        services.AddOptions<TariffOptions>()
            .Bind(configuration.GetSection(TariffOptions.SectionName))
            .Validate(x => x.MinimumFee >= 0m, "Tariff:MinimumFee deve ser maior ou igual a zero.")
            .Validate(x => x.DistanceAllowanceKm >= 0m, "Tariff:DistanceAllowanceKm deve ser maior ou igual a zero.")
            .Validate(x => x.PricePerKm is >= 1.20m and <= 1.50m, "Tariff:PricePerKm deve estar entre 1,20 e 1,50.")
            .Validate(x => x.OperationalReturnFee >= 0m, "Tariff:OperationalReturnFee deve ser maior ou igual a zero.")
            .ValidateOnStart();
        services.AddSingleton<ITariffQuoteService, TariffQuoteService>();
        services.AddScoped<IIdentityService, IdentityService>();
        services.AddOptions<SessionOptions>().BindConfiguration("Session")
            .Validate(x => x.RefreshDays is >= 1 and <= 30 && x.RecoveryMinutes is >= 5 and <= 60 && x.OnboardingMinutes is >= 5 and <= 60, "Configure prazos de sessão válidos.").ValidateOnStart();
        services.AddOptions<RecoveryMailOptions>().BindConfiguration("RecoveryMail");
        services.AddOptions<PrivateDocumentOptions>().BindConfiguration("PrivateDocuments")
            .Validate(x => x.MaxBytes is > 0 and <= 52428800, "PrivateDocuments:MaxBytes inválido.").ValidateOnStart();
        services.AddScoped<SessionService>();
        services.AddScoped<ProfileService>();
        services.AddScoped<CourierDocumentService>();
        services.AddScoped<AdminBootstrapService>();
        services.AddScoped<RegistrationReviewService>();
        services.AddScoped<IRecoveryMailer, RecoveryMailer>();
        services.AddSingleton<PrivateDocumentStore>();
        return services;
    }
}
