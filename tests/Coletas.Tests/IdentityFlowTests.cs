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
    public async Task EstablishmentRegistrationApprovalLoginAndBlock()
    {
        await using var factory = new Factory();
        using var client = factory.CreateClient();
        var request = new { email = " STORE@example.test ", password = Password, legalName = "Store Ltd", tradeName = "Store", taxId = "123", phoneWhatsApp = "5565999999999" };
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
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsync($"/api/v1/admin/users/{user.Id}/approve", null)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.PostAsync($"/api/v1/admin/users/{Guid.NewGuid()}/approve", null)).StatusCode);
        using var storeClient = factory.CreateClient();
        await Authenticate(storeClient, user.Email);
        Assert.Equal(HttpStatusCode.OK, (await storeClient.GetAsync("/api/v1/me")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await storeClient.PostAsync($"/api/v1/admin/users/{admin.Id}/block", null)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsync($"/api/v1/admin/users/{user.Id}/block", null)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await storeClient.PostAsJsonAsync("/api/v1/auth/login", new { email = user.Email, password = Password })).StatusCode);
    }

    [Theory]
    [InlineData("", "Valid-password-123", "123", "Name", "ABC1234", "Motorcycle")]
    [InlineData("invalid", "Valid-password-123", "123", "Name", "ABC1234", "Motorcycle")]
    [InlineData("a@test.com", "short", "123", "Name", "ABC1234", "Motorcycle")]
    [InlineData("a@test.com", "Valid-password-123", "", "Name", "ABC1234", "Motorcycle")]
    [InlineData("a@test.com", "Valid-password-123", "123", "", "ABC1234", "Motorcycle")]
    [InlineData("a@test.com", "Valid-password-123", "123", "Name", "", "Motorcycle")]
    [InlineData("a@test.com", "Valid-password-123", "123", "Name", "ABC1234", "Unknown")]
    public async Task InvalidCourierDoesNotPersist(string email, string password, string phone, string name, string plate, string type)
    {
        await using var factory = new Factory();
        using var client = factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/v1/auth/register/couriers", new { email, password, phoneWhatsApp = phone, fullName = name, plate, vehicleType = type });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var scope = factory.Services.CreateScope();
        Assert.Empty(scope.ServiceProvider.GetRequiredService<ColetasDbContext>().Users);
    }

    [Theory]
    [InlineData("", "Store", "123")]
    [InlineData("Legal", "", "123")]
    [InlineData("Legal", "Store", "")]
    public async Task InvalidEstablishmentRejected(string legalName, string tradeName, string taxId)
    {
        await using var factory = new Factory();
        using var client = factory.CreateClient();
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/v1/auth/register/establishments", new { email = "store@test.com", password = Password, phoneWhatsApp = "123", legalName, tradeName, taxId })).StatusCode);
    }

    [Fact]
    public async Task CourierDocumentsEnforceOwnershipAndValidity()
    {
        await using var factory = new Factory();
        using var client = factory.CreateClient();
        var request = new { email = "courier@test.com", password = Password, phoneWhatsApp = "123", fullName = "Courier", plate = "abc-1234", vehicleType = "Motorcycle" };
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
        Assert.Equal(HttpStatusCode.Created, (await client.PostAsJsonAsync(endpoint, new { type = "DriverLicense", expiresAt = DateTimeOffset.UtcNow.AddYears(1) })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync(endpoint, new { type = "DriverLicense", expiresAt = DateTimeOffset.UtcNow.AddDays(-1) })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync(endpoint, new { type = 99 })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.PostAsJsonAsync($"/api/v1/couriers/{Guid.NewGuid()}/documents", new { type = "DriverLicense" })).StatusCode);
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
