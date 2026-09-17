using System.Security.Claims;
using Coletas.Api.Responses;
using Coletas.Application.Identity;
using Coletas.Domain.Couriers;
using Coletas.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Coletas.Api.Controllers;

[ApiController]
[Authorize(Policy = "AccountAccess")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class AccountDocumentsController(CourierDocumentService documents, ProfileService profiles) : ControllerBase
{
    private Guid Actor => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpPut("/api/v1/couriers/{courierId:guid}/vehicles/{vehicleId:guid}")]
    public async Task<IResult> Vehicle(Guid courierId, Guid vehicleId, VehicleRequest request, CancellationToken ct)
        => IdentityHttpResultMapper.ToHttpResult(await profiles.EditVehicleAsync(Actor, courierId, vehicleId, request, ct));

    [HttpPost("/api/v1/couriers/{courierId:guid}/documents/upload")]
    // Teto técnico permite o máximo configurável de 50 MiB mais o envelope multipart.
    [RequestSizeLimit(52 * 1024 * 1024)]
    public async Task<IResult> Upload(Guid courierId, [FromForm] IFormFile? file,
        [FromForm] CourierDocumentType type, [FromForm] DateTimeOffset? expiresAt, CancellationToken ct)
    {
        if (file is null || file.Length == 0) return Results.BadRequest(new { error = "Selecione um arquivo." });
        // Arquivo e titular são validados no serviço; nome/MIME do navegador não são confiáveis.
        // Mudança: docs/mudancas/2026-09-16-03-integracao-acesso-administracao.md
        await using var stream = file.OpenReadStream();
        return IdentityHttpResultMapper.ToHttpResult(await documents.UploadAsync(Actor, courierId, type, expiresAt, stream, ct));
    }

    [HttpGet("/api/v1/couriers/{courierId:guid}/documents/{documentId:guid}/file")]
    public async Task<IResult> Download(Guid courierId, Guid documentId, CancellationToken ct)
    {
        var result = await documents.DownloadAsync(Actor, courierId, documentId, ct);
        if (result.Value is null) return IdentityHttpResultMapper.ToHttpResult(result);
        Response.Headers.XContentTypeOptions = "nosniff";
        return Results.File(result.Value.Stream, result.Value.ContentType, "documento");
    }

    [Authorize(Policy = "AdminOnly")]
    [HttpPost("/api/v1/admin/documents/{documentId:guid}/decision")]
    public async Task<IResult> Decide(Guid documentId, DocumentDecision request, CancellationToken ct)
        => request.ExpectedVersion is null or < 0
            ? Results.BadRequest(new { error = "Informe a versão do cadastro exibido." })
            : IdentityHttpResultMapper.ToHttpResult(await documents.DecideDocumentAsync(Actor, documentId, request, ct));

    [Authorize(Policy = "AdminOnly")]
    [HttpGet("/api/v1/admin/users/{userId:guid}/profile")]
    public async Task<ActionResult<AccountProfile>> Profile(Guid userId, CancellationToken ct)
    {
        var profile = await profiles.GetAsync(userId, ct);
        return profile is null ? NotFound() : Ok(profile);
    }
}
