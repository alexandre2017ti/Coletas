using Coletas.Application.Identity;
using Coletas.Domain.Couriers;
using Coletas.Domain.Identity;
using Coletas.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Coletas.Infrastructure.Identity;

/// <summary>Armazenamento privado e decisões documentais, sem alterar regras de acesso.</summary>
// Extração por responsabilidade: docs/mudancas/2026-09-16-02-servicos-e-cliente-http.md
public sealed class CourierDocumentService(ColetasDbContext db, PrivateDocumentStore files, RegistrationReviewService reviews)
{
    public async Task<IdentityResult<CourierDocumentResponse>> UploadAsync(Guid actor, Guid courierId, CourierDocumentType type, DateTimeOffset? expiresAt, Stream content, CancellationToken ct)
    {
        if (!await CourierAccess.CanEditAsync(db, actor, courierId, ct)) return IdentityResult<CourierDocumentResponse>.Forbidden("Cadastro não autorizado.");
        if (!Enum.IsDefined(type) || expiresAt <= DateTimeOffset.UtcNow) return IdentityResult<CourierDocumentResponse>.Invalid("Tipo ou validade inválidos.");
        if (!files.IsConfigured) return new(default, "Armazenamento privado não configurado.", false, 503);
        (string Key, string ContentType, long Length) stored;
        try { stored = await files.SaveAsync(content, ct); }
        catch (InvalidDataException e) { return IdentityResult<CourierDocumentResponse>.Invalid(e.Message); }
        var doc = new CourierDocument
        {
            CourierId = courierId,
            Type = type,
            ExpiresAt = expiresAt,
            Status = CourierDocumentStatus.UnderReview,
            StorageKey = stored.Key,
            ContentType = stored.ContentType,
            ContentLength = stored.Length
        };
        try
        {
            var courier = await db.Couriers.SingleAsync(x => x.Id == courierId, ct);
            var owner = await db.Users.SingleAsync(x => x.Id == courier.UserId, ct);
            db.CourierDocuments.Add(doc);
            await reviews.InvalidateAsync(owner, actor, "Novo documento recebido; análise precisa ser refeita.", ct);
            Audit(actor, doc.Id, "document.upload", "Documento recebido para análise.");
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            files.Delete(stored.Key);
            db.ChangeTracker.Clear();
            return IdentityResult<CourierDocumentResponse>.Conflict("Cadastro alterado; recarregue antes de reenviar.");
        }
        catch { files.Delete(stored.Key); throw; }
        return IdentityResult<CourierDocumentResponse>.Ok(ToResponse(doc));
    }

    public async Task<IdentityResult<PrivateDocumentContent>> DownloadAsync(Guid actor, Guid courierId, Guid documentId, CancellationToken ct)
    {
        if (!await CourierAccess.CanEditAsync(db, actor, courierId, ct)) return IdentityResult<PrivateDocumentContent>.Forbidden("Cadastro não autorizado.");
        var doc = await db.CourierDocuments.AsNoTracking().SingleOrDefaultAsync(x => x.Id == documentId && x.CourierId == courierId, ct);
        if (doc?.StorageKey is null) return IdentityResult<PrivateDocumentContent>.NotFound("Arquivo não encontrado.");
        var stream = files.Open(doc.StorageKey);
        return stream is null ? IdentityResult<PrivateDocumentContent>.NotFound("Arquivo não encontrado.")
            : IdentityResult<PrivateDocumentContent>.Ok(new(stream, doc.ContentType!));
    }

    public async Task<IdentityResult<CourierDocumentResponse>> DecideDocumentAsync(Guid actor, Guid id, DocumentDecision decision, CancellationToken ct)
    {
        if (!await CourierAccess.IsAdminAsync(db, actor, ct)) return IdentityResult<CourierDocumentResponse>.Forbidden("Apenas administradores.");
        if (!Enum.IsDefined(decision.Status) || decision.Reason?.Length > 500)
            return IdentityResult<CourierDocumentResponse>.Invalid("Informe status e mensagem de até 500 caracteres.");
        var doc = await db.CourierDocuments.SingleOrDefaultAsync(x => x.Id == id, ct);
        if (doc is null) return IdentityResult<CourierDocumentResponse>.NotFound("Documento não encontrado.");
        if (decision.Status == CourierDocumentStatus.Approved && (doc.StorageKey is null || doc.ExpiresAt <= DateTimeOffset.UtcNow))
            return IdentityResult<CourierDocumentResponse>.Invalid("Documento sem arquivo ou vencido não pode ser aprovado.");
        // A decisão documental também é compreensível sem texto livre; a justificativa detalhada continua opcional para o administrador.
        // Mudança: docs/mudancas/2026-09-17-04-inicio-analise-sem-motivo.md
        var reason = string.IsNullOrWhiteSpace(decision.Reason) ? $"Documento {(decision.Status == CourierDocumentStatus.Approved ? "aprovado" : "reprovado")}." : decision.Reason.Trim();
        doc.Status = decision.Status; doc.ReviewReason = reason;
        var courier = await db.Couriers.SingleAsync(x => x.Id == doc.CourierId, ct);
        var user = await db.Users.SingleAsync(x => x.Id == courier.UserId, ct);
        // A tela decide sobre a versão que exibiu, não sobre uma revisão posterior.
        // Mudança: docs/mudancas/2026-09-16-03-integracao-acesso-administracao.md
        if (decision.ExpectedVersion.HasValue && decision.ExpectedVersion != user.ReviewVersion)
        {
            db.ChangeTracker.Clear();
            return IdentityResult<CourierDocumentResponse>.Conflict("Cadastro alterado; recarregue antes de decidir.");
        }
        await reviews.InvalidateAsync(user, actor, "Análise documental alterada; revise o cadastro.", ct);
        Audit(actor, id, "document.review", $"{decision.Status}: {reason}"[..Math.Min(500, $"{decision.Status}: {reason}".Length)]);
        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateConcurrencyException) { db.ChangeTracker.Clear(); return IdentityResult<CourierDocumentResponse>.Conflict("Cadastro alterado; recarregue."); }
        return IdentityResult<CourierDocumentResponse>.Ok(ToResponse(doc));
    }

    private void Audit(Guid actor, Guid subject, string action, string detail) => db.IdentityAudits.Add(new IdentityAudit { ActorId = actor, SubjectId = subject, Action = action, Detail = detail });
    private static CourierDocumentResponse ToResponse(CourierDocument d) => new(d.Id, d.Type, d.Status, d.ExpiresAt);
}
