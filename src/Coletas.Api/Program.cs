using System.Text.Json.Serialization;
using Coletas.Api.Configuration;
using Coletas.Api.Database;
using Coletas.Application.Foundation;
using Coletas.Infrastructure;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();
builder.Services.AddSingleton<PlatformInfoService>();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Logging.AddJsonConsole();

// Motivo: controllers usam opções JSON do MVC; enum inválido deve produzir 400.
// Não inferir Required de strings: as validações continuam nos serviços existentes.
// Mudança: docs/mudancas/2026-09-10-08-organizacao-api.md
builder.Services.AddControllers(options => options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true)
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddApiAuthentication(builder.Configuration);
builder.Services.AddApiRateLimiting();

var app = builder.Build();
if (await DatabaseMigrationRunner.RunIfRequestedAsync(app, args)) return;

app.UseExceptionHandler();
app.UseRouting();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
if (app.Environment.IsDevelopment()) app.MapOpenApi();
app.MapControllers();
app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false });
app.MapHealthChecks("/health/ready", new HealthCheckOptions { Predicate = check => check.Tags.Contains("ready") });
await app.RunAsync();

/// <summary>Ponto de entrada para testes HTTP.</summary>
public partial class Program;
