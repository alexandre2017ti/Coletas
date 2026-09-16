using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using Coletas.Domain.Identity;
using Coletas.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Coletas.Tests;

public sealed class IdentityFlowTests
{
    private sealed class Factory : WebApplicationFactory<Program>
    {
        private readonly string databaseName = Guid.NewGuid().ToString();
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");
            builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Infrastructure:Postgres"] = "Host=localhost;Database=unused",
                ["Infrastructure:Redis"] = "localhost:1,abortConnect=false",
                ["Jwt:SigningKey"] = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
            }));
            builder.ConfigureServices(services =>
            {
                // Motivo: isolamento por cenário, preservando serviço e validação JWT reais.
                // Mudança: docs/mudancas/2026-09-10-07-cobertura-identidade.md
                services.RemoveAll<DbContextOptions<ColetasDbContext>>();
                services.RemoveAll<IDbContextOptionsConfiguration<ColetasDbContext>>();
                services.AddDbContext<ColetasDbContext>(options => options.UseInMemoryDatabase(databaseName));
            });
        }
    }

    private const string Password = "Test-only-password-123";

    [Fact]
    public async Task RefreshRotatesTokenAndReplayRevokesSession()
    {
        await using var factory = new Factory();
        using var client = factory.CreateClient();
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ColetasDbContext>();
        db.Users.Add(new User { Email = "session@test.com", PasswordHash = BCrypt.Net.BCrypt.HashPassword(Password), Role = UserRole.Admin, Status = UserStatus.Active });
        await db.SaveChangesAsync();
        using var login = await client.PostAsJsonAsync("/api/v1/auth/login", new { email = "session@test.com", password = Password });
        Assert.True(login.Headers.CacheControl?.NoStore);
        var auth = await login.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        var refreshToken = auth.GetProperty("refreshToken").GetString();
        using var refresh = await client.PostAsJsonAsync("/api/v1/auth/refresh", new { refreshToken });
        Assert.Equal(HttpStatusCode.OK, refresh.StatusCode);
        Assert.True(refresh.Headers.CacheControl?.NoStore);
        var renewed = await refresh.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        Assert.NotEqual(refreshToken, renewed.GetProperty("refreshToken").GetString());
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", renewed.GetProperty("accessToken").GetString());
        using var profile = await client.GetAsync("/api/v1/auth/me");
        Assert.Equal(HttpStatusCode.OK, profile.StatusCode);
        Assert.Equal("Admin", (await profile.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>()).GetProperty("role").GetString());
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/api/v1/auth/refresh", new { refreshToken })).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/v1/auth/me")).StatusCode);
    }

    [Theory]
    [InlineData(UserStatus.Active)]
    [InlineData(UserStatus.Pending)]
    public async Task LogoutRevokesAuthenticatedAccountIncludingOnboarding(UserStatus status)
    {
        await using var factory = new Factory();
        using var client = factory.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsync("/api/v1/auth/logout", null)).StatusCode);
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ColetasDbContext>();
        db.Users.Add(new User { Email = "logout@test.com", PasswordHash = BCrypt.Net.BCrypt.HashPassword(Password), Role = UserRole.Courier, Status = status });
        await db.SaveChangesAsync();
        var route = status == UserStatus.Pending ? "/api/v1/auth/onboarding/login" : "/api/v1/auth/login";
        using var login = await client.PostAsJsonAsync(route, new { email = "logout@test.com", password = Password });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        var auth = await login.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.GetProperty("accessToken").GetString());
        Assert.Equal(HttpStatusCode.NoContent, (await client.PostAsync("/api/v1/auth/logout", null)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/v1/auth/me")).StatusCode);
        if (status == UserStatus.Active)
            Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/api/v1/auth/refresh", new { refreshToken = auth.GetProperty("refreshToken").GetString() })).StatusCode);
        db.ChangeTracker.Clear();
        Assert.All(await db.SecurityTokens.ToListAsync(), token => Assert.True(token.Used));
    }

    [Theory]
    [InlineData("")]
    [InlineData("unknown-token")]
    public async Task RefreshRejectsInvalidToken(string refreshToken)
    {
        await using var factory = new Factory();
        using var client = factory.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/api/v1/auth/refresh", new { refreshToken })).StatusCode);
    }

    [Theory]
    [InlineData("email")]
    [InlineData("phone")]
    [InlineData("cnpj")]
    public async Task EstablishmentIdentifiersAreIndependentlyUnique(string field)
    {
        await using var factory = new Factory();
        using var client = factory.CreateClient();
        var original = new Coletas.Application.Identity.EstablishmentRegistrationRequest("one@example.test", Password, "One", "One", "19131243000197", "65999999999");
        var other = new Coletas.Application.Identity.EstablishmentRegistrationRequest("two@example.test", Password, "Two", "Two", "31147798000122", "65988888888");
        other = field switch
        {
            "email" => other with { Email = " ONE@EXAMPLE.TEST " },
            "phone" => other with { PhoneWhatsApp = "+55 (65) 99999-9999" },
            _ => other with { TaxId = "19.131.243/0001-97" }
        };
        Assert.Equal(HttpStatusCode.Created, (await client.PostAsJsonAsync("/api/v1/auth/register/establishments", original)).StatusCode);
        var response = await client.PostAsJsonAsync("/api/v1/auth/register/establishments", other);
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.DoesNotContain(original.Email, await response.Content.ReadAsStringAsync(), StringComparison.OrdinalIgnoreCase);
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ColetasDbContext>();
        Assert.Single(db.Users);
        Assert.Single(db.Establishments);
    }

    [Theory]
    [InlineData("email")]
    [InlineData("phone")]
    [InlineData("cpf")]
    [InlineData("plate")]
    public async Task CourierIdentifiersAreIndependentlyUnique(string field)
    {
        await using var factory = new Factory();
        using var client = factory.CreateClient();
        var original = new Coletas.Application.Identity.CourierRegistrationRequest("one@example.test", Password, "One", "65999999999", Coletas.Domain.Couriers.VehicleType.Motorcycle, "ABC1234", "52998224725");
        var other = new Coletas.Application.Identity.CourierRegistrationRequest("two@example.test", Password, "Two", "65988888888", Coletas.Domain.Couriers.VehicleType.Car, "DEF1G23", "11144477735");
        other = field switch
        {
            "email" => other with { Email = " ONE@EXAMPLE.TEST " },
            "phone" => other with { PhoneWhatsApp = "+55 (65) 99999-9999" },
            "cpf" => other with { Cpf = "529.982.247-25" },
            _ => other with { Plate = "abc-1234" }
        };
        Assert.Equal(HttpStatusCode.Created, (await client.PostAsJsonAsync("/api/v1/auth/register/couriers", original)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync("/api/v1/auth/register/couriers", other)).StatusCode);
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ColetasDbContext>();
        Assert.Single(db.Users);
        Assert.Single(db.Vehicles);
        Assert.Equal("52998224725", (await db.Couriers.SingleAsync()).Cpf);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("00000000000")]
    [InlineData("52998224726")]
    public async Task InvalidCpfDoesNotPersist(string? cpf)
    {
        await using var factory = new Factory();
        using var client = factory.CreateClient();
        var request = new Coletas.Application.Identity.CourierRegistrationRequest("one@example.test", Password, "One", "65999999999", Coletas.Domain.Couriers.VehicleType.Motorcycle, "ABC1234", cpf);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/v1/auth/register/couriers", request)).StatusCode);
        using var scope = factory.Services.CreateScope();
        Assert.Empty(scope.ServiceProvider.GetRequiredService<ColetasDbContext>().Users);
    }

    [Fact]
    public async Task EstablishmentRegistrationApprovalLoginAndBlock()
    {
        await using var factory = new Factory();
        using var client = factory.CreateClient();
        var request = new { email = " STORE@example.test ", password = Password, legalName = "Store Ltd", tradeName = "Store", taxId = "19.131.243/0001-97", phoneWhatsApp = "5565999999999" };
        using var registration = await client.PostAsJsonAsync("/api/v1/auth/register/establishments", request);
        Assert.Equal(HttpStatusCode.Created, registration.StatusCode);
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ColetasDbContext>();
        var user = await db.Users.SingleAsync();
        Assert.Equal("store@example.test", user.Email);
        Assert.Equal(UserStatus.Pending, user.Status);
        Assert.NotEqual(Password, user.PasswordHash);
        Assert.True(BCrypt.Net.BCrypt.Verify(Password, user.PasswordHash));
        Assert.Equal(user.Id, (await db.Establishments.SingleAsync()).UserId);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/api/v1/auth/login", new { email = user.Email, password = Password })).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync("/api/v1/auth/register/establishments", request)).StatusCode);
        var admin = new User { Email = "admin@example.test", PasswordHash = BCrypt.Net.BCrypt.HashPassword(Password), Role = UserRole.Admin, Status = UserStatus.Active };
        db.Users.Add(admin);
        await db.SaveChangesAsync();
        await Authenticate(client, admin.Email);
        Assert.Equal(HttpStatusCode.UnsupportedMediaType, (await client.PostAsync($"/api/v1/admin/users/{user.Id}/approve", null)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync($"/api/v1/admin/reviews/{user.Id}/decisions", new { expectedVersion = 0, action = "Start", reason = "Conferência iniciada" })).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync($"/api/v1/admin/users/{user.Id}/approve", new { expectedVersion = 1, reason = "Cadastro conferido" })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.PostAsJsonAsync($"/api/v1/admin/users/{Guid.NewGuid()}/approve", new { expectedVersion = 0, reason = "Conferido" })).StatusCode);
        using var storeClient = factory.CreateClient();
        await Authenticate(storeClient, user.Email);
        Assert.Equal(HttpStatusCode.OK, (await storeClient.GetAsync("/api/v1/me")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await storeClient.PostAsync($"/api/v1/admin/users/{admin.Id}/block", null)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync($"/api/v1/admin/users/{user.Id}/block", new { expectedVersion = 2, reason = "Bloqueio de segurança" })).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await storeClient.GetAsync("/api/v1/me")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await storeClient.PostAsJsonAsync("/api/v1/auth/login", new { email = user.Email, password = Password })).StatusCode);
    }

    [Theory]
    [InlineData("", "Valid-password-123", "65999999999", "Name", "ABC1234", "Motorcycle")]
    [InlineData("invalid", "Valid-password-123", "65999999999", "Name", "ABC1234", "Motorcycle")]
    [InlineData("a@test.com", "short", "65999999999", "Name", "ABC1234", "Motorcycle")]
    [InlineData("a@test.com", "Valid-password-123", "", "Name", "ABC1234", "Motorcycle")]
    [InlineData("a@test.com", "Valid-password-123", "65999999999", "", "ABC1234", "Motorcycle")]
    [InlineData("a@test.com", "Valid-password-123", "65999999999", "Name", "", "Motorcycle")]
    [InlineData("a@test.com", "Valid-password-123", "65999999999", "Name", "ABC1234", "Unknown")]
    public async Task InvalidCourierDoesNotPersist(string email, string password, string phone, string name, string plate, string type)
    {
        await using var factory = new Factory();
        using var client = factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/v1/auth/register/couriers", new { email, password, phoneWhatsApp = phone, fullName = name, plate, vehicleType = type, cpf = "52998224725" });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var scope = factory.Services.CreateScope();
        Assert.Empty(scope.ServiceProvider.GetRequiredService<ColetasDbContext>().Users);
    }

    [Theory]
    [InlineData("", "Store", "19131243000197")]
    [InlineData("Legal", "", "19131243000197")]
    [InlineData("Legal", "Store", "")]
    public async Task InvalidEstablishmentRejected(string legalName, string tradeName, string taxId)
    {
        await using var factory = new Factory();
        using var client = factory.CreateClient();
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/v1/auth/register/establishments", new { email = "store@test.com", password = Password, phoneWhatsApp = "65999999999", legalName, tradeName, taxId })).StatusCode);
    }

    [Fact]
    public async Task CourierDocumentsEnforceOwnershipAndValidity()
    {
        await using var factory = new Factory();
        using var client = factory.CreateClient();
        var request = new { email = "courier@test.com", password = Password, phoneWhatsApp = "65999999999", fullName = "Courier", plate = "abc-1234", vehicleType = "Motorcycle", cpf = "52998224725" };
        Assert.Equal(HttpStatusCode.Created, (await client.PostAsJsonAsync("/api/v1/auth/register/couriers", request)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync("/api/v1/auth/register/couriers", request)).StatusCode);
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ColetasDbContext>();
        var courier = await db.Couriers.SingleAsync();
        Assert.Equal("ABC1234", (await db.Vehicles.SingleAsync()).Plate);
        var user = await db.Users.SingleAsync();
        user.Status = UserStatus.Active;
        await db.SaveChangesAsync();
        await Authenticate(client, user.Email);
        var endpoint = $"/api/v1/couriers/{courier.Id}/documents";
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync(endpoint, new { type = "DriverLicense", expiresAt = DateTimeOffset.UtcNow.AddDays(-1) })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync(endpoint, new { type = 99 })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.PostAsJsonAsync($"/api/v1/couriers/{Guid.NewGuid()}/documents", new { type = "DriverLicense" })).StatusCode);
        Assert.Equal(HttpStatusCode.Created, (await client.PostAsJsonAsync(endpoint, new { type = "DriverLicense", expiresAt = DateTimeOffset.UtcNow.AddYears(1) })).StatusCode);
        // Documento novo revoga a sessão anterior; dados inválidos acima não alteram a análise.
        // Mudança: docs/mudancas/2026-09-16-01-refatoracao-identidade.md
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/v1/me")).StatusCode);
        var other = new User { Email = "other@test.com", PasswordHash = BCrypt.Net.BCrypt.HashPassword(Password), Role = UserRole.Courier, Status = UserStatus.Active };
        db.Users.Add(other);
        await db.SaveChangesAsync();
        await Authenticate(client, other.Email);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PostAsJsonAsync(endpoint, new { type = "DriverLicense" })).StatusCode);
        Assert.Single(await db.CourierDocuments.ToListAsync());
    }

    [Fact]
    public async Task InvalidCredentialsAndExhaustedRateLimit()
    {
        await using var factory = new Factory();
        using var client = factory.CreateClient();
        for (var i = 0; i < 10; i++)
            Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/api/v1/auth/login", new { email = "missing@test.com", password = Password })).StatusCode);
        var limited = await client.PostAsJsonAsync("/api/v1/auth/login", new { email = "missing@test.com", password = Password });
        Assert.False(limited.IsSuccessStatusCode);
        Assert.Contains(limited.StatusCode, new[] { HttpStatusCode.ServiceUnavailable, HttpStatusCode.TooManyRequests });
    }

    private static async Task Authenticate(HttpClient client, string email)
    {
        using var response = await client.PostAsJsonAsync("/api/v1/auth/login", new { email, password = Password });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", body.GetProperty("accessToken").GetString());
    }
}
