using System.Net;
using System.Net.Http.Json;
using Coletas.Application.Foundation;
using Coletas.Application.Identity;
using Coletas.Application.Pricing;
using Coletas.Domain.Settings;
using Coletas.Infrastructure;
using Coletas.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;
namespace Coletas.Tests;

public sealed class FoundationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration((_, configuration) =>
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Infrastructure:Postgres"] = "Host=127.0.0.1;Port=1;Database=coletas;Username=coletas;Timeout=1",
                ["Infrastructure:Redis"] = "127.0.0.1:1,connectTimeout=100,abortConnect=false",
                ["Jwt:SigningKey"] = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
            }));
    }
}
public sealed class FoundationTests(FoundationFactory factory) : IClassFixture<FoundationFactory>
{
    [Fact]
    public async Task PlatformReturnsDto()
    {
        using var client = factory.CreateClient();
        var response = await client.GetFromJsonAsync<PlatformInfo>("/api/v1/platform");
        Assert.Equal("Coletas", response?.Name);
        Assert.Equal("Fundação", response?.Stage);
    }
    [Theory]
    [InlineData("/health/live", HttpStatusCode.OK)]
    [InlineData("/health/ready", HttpStatusCode.ServiceUnavailable)]
    [InlineData("/openapi/v1.json", HttpStatusCode.OK)]
    [InlineData("/api/v1/unknown", HttpStatusCode.NotFound)]
    public async Task RoutesHaveExpectedStatus(string path, HttpStatusCode expected)
    {
        using var client = factory.CreateClient();
        Assert.Equal(expected, (await client.GetAsync(path)).StatusCode);
    }
    [Fact]
    public void MissingConnectionFailsValidation()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddInfrastructure(new ConfigurationBuilder().Build());
        using var provider = services.BuildServiceProvider();
        Assert.Throws<OptionsValidationException>(() => provider.GetRequiredService<IOptions<InfrastructureOptions>>().Value);
    }
    [Fact]
    public void PersistenceUsesSeparateSchemaAndPostgis()
    {
        using var database = new ColetasDbContextFactory().CreateDbContext([]);
        Assert.Equal("configuration", database.Model.FindEntityType(typeof(SystemSetting))?.GetSchema());
        Assert.Contains("postgis", database.Database.GenerateCreateScript());
        Assert.DoesNotContain(typeof(ColetasDbContext).Assembly.GetName().Name!,
            typeof(SystemSetting).Assembly.GetReferencedAssemblies().Select(x => x.Name));
    }

    [Theory]
    [InlineData(0, false, 7.50)]
    [InlineData(1.5, false, 7.50)]
    [InlineData(5, false, 12.23)]
    [InlineData(7, false, 14.93)]
    [InlineData(7, true, 16.43)]
    public async Task TariffQuoteAppliesDistanceAndOperationalReturn(decimal distanceKm, bool requiresReturn, decimal expectedTotal)
    {
        using var client = factory.CreateClient();
        using var response = await client.PostAsJsonAsync("/api/v1/tariffs/quote", new TariffQuoteRequest(distanceKm, requiresReturn));
        var quote = await response.Content.ReadFromJsonAsync<TariffQuoteResponse>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(expectedTotal, quote?.Total);
        Assert.Equal(requiresReturn ? 1.50m : 0m, quote?.OperationalReturnFee);
    }

    [Fact]
    public async Task TariffQuoteRejectsNegativeDistance()
    {
        using var client = factory.CreateClient();
        using var response = await client.PostAsJsonAsync("/api/v1/tariffs/quote", new TariffQuoteRequest(-1m, false));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ProtectedIdentityRoutesRejectAnonymousRequests()
    {
        using var client = factory.CreateClient();

        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/v1/me")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsync(
            $"/api/v1/admin/users/{Guid.NewGuid()}/approve", content: null)).StatusCode);
    }

    [Fact]
    public async Task LoginRejectsIncompletePayloadWithoutLeakingAccountInformation()
    {
        using var client = factory.CreateClient();
        using var response = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest("", ""));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
