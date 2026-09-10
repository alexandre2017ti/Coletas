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

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();
builder.Services.AddSingleton<PlatformInfoService>();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Logging.AddJsonConsole();
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var jwt = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new();
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
builder.Services.AddAuthorization(options =>
    options.AddPolicy("AdminOnly", policy => policy.RequireRole(nameof(UserRole.Admin))));
builder.Services.AddRateLimiter(options =>
    options.AddFixedWindowLimiter("auth", limiter =>
    {
        limiter.PermitLimit = 10;
        limiter.Window = TimeSpan.FromMinutes(1);
        limiter.QueueLimit = 0;
    }));
var app = builder.Build();

// Motivo: migrations explícitas evitam alteração concorrente de schema no início da API.
// Mudança: docs/mudancas/2026-09-09-02-fase-zero.md
if (args.Contains("--migrate", StringComparer.Ordinal))
{
    using var scope = app.Services.CreateScope();
    await scope.ServiceProvider.GetRequiredService<ColetasDbContext>()
        .Database.MigrateAsync(app.Lifetime.ApplicationStopping);
    return;
}

app.UseExceptionHandler();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}
app.MapGet("/api/v1/platform", (PlatformInfoService service) => TypedResults.Ok(service.Get()))
    .WithName("GetPlatformInfo").WithTags("Foundation");
app.MapPost("/api/v1/tariffs/quote", (TariffQuoteRequest request, ITariffQuoteService service) =>
{
    var result = service.Calculate(request);
    return result.IsSuccess
        ? Results.Ok(result.Value)
        : Results.ValidationProblem(new Dictionary<string, string[]> { ["routeDistanceKm"] = [result.Error!] });
})
    .WithName("QuoteTariff").WithTags("Tariffs")
    .Produces<TariffQuoteResponse>()
    .ProducesValidationProblem();
app.MapPost("/api/v1/auth/register/establishments", async (
    EstablishmentRegistrationRequest request,
    IIdentityService service,
    CancellationToken cancellationToken) =>
{
    var result = await service.RegisterEstablishmentAsync(request, cancellationToken);
    return ToHttpResult(result, StatusCodes.Status201Created);
})
    .RequireRateLimiting("auth")
    .WithName("RegisterEstablishment").WithTags("Identity")
    .Produces<RegistrationResponse>(StatusCodes.Status201Created)
    .ProducesValidationProblem();
app.MapPost("/api/v1/auth/register/couriers", async (
    CourierRegistrationRequest request,
    IIdentityService service,
    CancellationToken cancellationToken) =>
{
    var result = await service.RegisterCourierAsync(request, cancellationToken);
    return ToHttpResult(result, StatusCodes.Status201Created);
})
    .RequireRateLimiting("auth")
    .WithName("RegisterCourier").WithTags("Identity")
    .Produces<RegistrationResponse>(StatusCodes.Status201Created)
    .ProducesValidationProblem();
app.MapPost("/api/v1/auth/login", async (
    LoginRequest request,
    IIdentityService service,
    CancellationToken cancellationToken) =>
{
    var result = await service.LoginAsync(request, cancellationToken);
    return ToHttpResult(result);
})
    .RequireRateLimiting("auth")
    .WithName("Login").WithTags("Identity")
    .Produces<AuthResponse>()
    .ProducesProblem(StatusCodes.Status401Unauthorized);
app.MapGet("/api/v1/me", (ClaimsPrincipal principal) =>
{
    var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier) ?? principal.FindFirstValue(JwtRegisteredClaimNames.Sub);
    return Results.Ok(new { UserId = userId, Role = principal.FindFirstValue(ClaimTypes.Role) });
})
    .RequireAuthorization()
    .WithName("GetCurrentUser").WithTags("Identity");
app.MapPost("/api/v1/couriers/{courierId:guid}/documents", async (
    Guid courierId,
    CourierDocumentRequest request,
    ClaimsPrincipal principal,
    IIdentityService service,
    CancellationToken cancellationToken) =>
{
    if (!Guid.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier) ?? principal.FindFirstValue(JwtRegisteredClaimNames.Sub), out var actorId)
        || !Enum.TryParse<UserRole>(principal.FindFirstValue(ClaimTypes.Role), out var actorRole))
    {
        return Results.Unauthorized();
    }

    var result = await service.AddCourierDocumentAsync(actorId, actorRole, courierId, request, cancellationToken);
    return ToHttpResult(result, StatusCodes.Status201Created);
})
    .RequireAuthorization()
    .WithName("AddCourierDocument").WithTags("Identity")
    .Produces<CourierDocumentResponse>(StatusCodes.Status201Created)
    .ProducesValidationProblem();
app.MapPost("/api/v1/admin/users/{userId:guid}/approve", async (
    Guid userId,
    IIdentityService service,
    CancellationToken cancellationToken) =>
{
    var result = await service.SetUserStatusAsync(UserRole.Admin, userId, UserStatus.Active, cancellationToken);
    return ToHttpResult(result);
})
    .RequireAuthorization("AdminOnly")
    .WithName("ApproveUser").WithTags("Identity")
    .Produces<RegistrationResponse>()
    .ProducesProblem(StatusCodes.Status404NotFound);
app.MapPost("/api/v1/admin/users/{userId:guid}/block", async (
    Guid userId,
    IIdentityService service,
    CancellationToken cancellationToken) =>
{
    var result = await service.SetUserStatusAsync(UserRole.Admin, userId, UserStatus.Blocked, cancellationToken);
    return ToHttpResult(result);
})
    .RequireAuthorization("AdminOnly")
    .WithName("BlockUser").WithTags("Identity")
    .Produces<RegistrationResponse>()
    .ProducesProblem(StatusCodes.Status404NotFound);
app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false });
app.MapHealthChecks("/health/ready", new HealthCheckOptions { Predicate = check => check.Tags.Contains("ready") });
await app.RunAsync();

static IResult ToHttpResult<T>(IdentityResult<T> result, int successStatusCode = StatusCodes.Status200OK)
    => result.IsSuccess
        ? Results.Json(result.Value, statusCode: successStatusCode)
        : result.StatusCode switch
        {
            StatusCodes.Status400BadRequest => Results.ValidationProblem(new Dictionary<string, string[]> { ["request"] = [result.Error!] }),
            StatusCodes.Status401Unauthorized => Results.Unauthorized(),
            StatusCodes.Status403Forbidden => Results.Forbid(),
            StatusCodes.Status404NotFound => Results.NotFound(new { error = result.Error }),
            StatusCodes.Status409Conflict => Results.Conflict(new { error = result.Error }),
            _ => Results.Problem(result.Error, statusCode: result.StatusCode)
        };

/// <summary>Ponto de entrada para testes HTTP.</summary>
public partial class Program;
