using Coletas.Application.Identity;
using Coletas.Domain.Couriers;
using Coletas.Domain.Identity;
using Coletas.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Coletas.Infrastructure.Identity;

/// <summary>Fila e decisões cadastrais; cada decisão e seu histórico são salvos juntos.</summary>
public sealed class RegistrationReviewService(ColetasDbContext db, SessionService sessions)
{
    /// <summary>Prepara nova análise na mesma unidade de trabalho da alteração cadastral.</summary>
    internal async Task InvalidateAsync(User user, Guid actor, string reason, CancellationToken ct)
    {
        // Uma alteração relevante torna obsoleta a decisão anterior, mas nunca desbloqueia a conta.
        // Não salvar aqui: dado, versão, histórico e revogação devem ser atômicos no chamador.
        // Mudança: docs/mudancas/2026-09-16-01-refatoracao-identidade.md
        user.ReviewStatus = ReviewStatus.Pending;
        user.ReviewVersion++;
        if (user.Status != UserStatus.Blocked)
        {
            user.Status = UserStatus.Pending;
            user.StatusReason = reason;
        }
        db.ReviewEvents.Add(new ReviewEvent
        {
            UserId = user.Id,
            ActorId = actor,
            Version = user.ReviewVersion,
            Action = "Invalidate",
            PublicReason = reason
        });
        await sessions.RevokeAsync(user, ct);
    }

    private Task<bool> IsAdminAsync(Guid actor, CancellationToken ct) => db.Users.AnyAsync(
        x => x.Id == actor && x.Role == UserRole.Admin && x.Status == UserStatus.Active, ct);

    public async Task<IdentityResult<ReviewPage>> ListAsync(Guid actor, ReviewStatus? status, UserRole? role,
        int page, int pageSize, CancellationToken ct)
    {
        if (!await IsAdminAsync(actor, ct)) return IdentityResult<ReviewPage>.Forbidden("Apenas administradores.");
        if (page < 1 || page > 100000 || pageSize is < 1 or > 100 || status.HasValue && !Enum.IsDefined(status.Value)
            || role.HasValue && role is not (UserRole.Establishment or UserRole.Courier))
            return IdentityResult<ReviewPage>.Invalid("Filtros inválidos.");
        var query = db.Users.AsNoTracking().Where(x => x.Role == UserRole.Courier || x.Role == UserRole.Establishment);
        if (status.HasValue) query = query.Where(x => x.ReviewStatus == status);
        if (role.HasValue) query = query.Where(x => x.Role == role);
        var total = await query.CountAsync(ct);
        var users = await query.OrderBy(x => x.CreatedAt).ThenBy(x => x.Id).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        // A fila não precisa expor CPF, CNPJ, telefone ou e-mail para selecionar um cadastro.
        // Mudança: docs/mudancas/2026-09-15-01-analise-administrativa.md
        return IdentityResult<ReviewPage>.Ok(new(users.Select(ToItem).ToArray(), page, pageSize, total));
    }

    public async Task<IdentityResult<ReviewDetail>> GetAsync(Guid actor, Guid userId, CancellationToken ct)
    {
        var admin = await IsAdminAsync(actor, ct);
        if (!admin && actor != userId) return IdentityResult<ReviewDetail>.Forbidden("Cadastro não autorizado.");
        var user = await db.Users.AsNoTracking().SingleOrDefaultAsync(x => x.Id == userId, ct);
        if (user is null) return IdentityResult<ReviewDetail>.NotFound("Cadastro não encontrado.");
        if (!admin && user.Status == UserStatus.Blocked) return IdentityResult<ReviewDetail>.Forbidden("Conta bloqueada.");
        var history = await db.ReviewEvents.AsNoTracking().Where(x => x.UserId == userId).OrderByDescending(x => x.Version)
            .Select(x => new ReviewHistory(x.Id, admin ? x.ActorId : null, x.Action, x.PublicReason,
                admin ? x.InternalNote : null, x.Version, x.CreatedAt)).ToListAsync(ct);
        return IdentityResult<ReviewDetail>.Ok(new(ToItem(user), history));
    }

    public async Task<IdentityResult<ReviewItem>> DecideAsync(Guid actor, Guid userId, ReviewDecision request, CancellationToken ct)
    {
        if (!await IsAdminAsync(actor, ct)) return IdentityResult<ReviewItem>.Forbidden("Apenas administradores.");
        if (!Enum.IsDefined(request.Action) || request.ExpectedVersion < 0 || string.IsNullOrWhiteSpace(request.Reason)
            || request.Reason.Length > 500 || request.InternalNote?.Length > 1000)
            return IdentityResult<ReviewItem>.Invalid("Informe decisão, versão e motivo de até 500 caracteres; nota interna até 1000.");
        var user = await db.Users.SingleOrDefaultAsync(x => x.Id == userId, ct);
        if (user is null) return IdentityResult<ReviewItem>.NotFound("Cadastro não encontrado.");
        if (user.Role is not (UserRole.Courier or UserRole.Establishment))
            return IdentityResult<ReviewItem>.Forbidden("Este fluxo não altera administradores ou operadores.");
        if (user.ReviewVersion != request.ExpectedVersion) return Stale();
        if (!RegistrationReview.CanTransition(user.ReviewStatus, request.Action)
            || user.Status == UserStatus.Blocked && request.Action != ReviewAction.Unblock
            || user.Status != UserStatus.Blocked && request.Action == ReviewAction.Unblock)
            return IdentityResult<ReviewItem>.Conflict("Ação incompatível com o estado atual. Recarregue o cadastro.");
        if (request.Action == ReviewAction.Approve && user.Role == UserRole.Courier)
        {
            var courier = await db.Couriers.AsNoTracking().SingleOrDefaultAsync(x => x.UserId == userId, ct);
            if (courier is null || !RegistrationValidation.IsCpf(courier.Cpf))
                return IdentityResult<ReviewItem>.Invalid("O cadastro exige CPF válido.");
            var documents = await db.CourierDocuments.AsNoTracking().Where(x => x.CourierId == courier.Id).ToListAsync(ct);
            // Somente a última versão de cada documento vale: um arquivo antigo aprovado não valida sua substituição.
            // Mudança: docs/mudancas/2026-09-15-01-analise-administrativa.md
            if (Enum.GetValues<CourierDocumentType>().Any(type => !IsApproved(documents.Where(x => x.Type == type)
                .OrderByDescending(x => x.CreatedAt).ThenByDescending(x => x.Id).FirstOrDefault())))
                return IdentityResult<ReviewItem>.Invalid("Aprove os documentos atuais, com arquivo e validade vigente, antes de liberar o cadastro.");
        }
        user.ReviewStatus = RegistrationReview.Next(user.ReviewStatus, request.Action);
        user.Status = request.Action == ReviewAction.Block ? UserStatus.Blocked
            : user.ReviewStatus == ReviewStatus.Approved ? UserStatus.Active : UserStatus.Pending;
        user.StatusReason = request.Reason.Trim();
        user.ReviewVersion++;
        db.ReviewEvents.Add(new ReviewEvent
        {
            UserId = userId,
            ActorId = actor,
            Version = user.ReviewVersion,
            Action = request.Action.ToString(),
            PublicReason = request.Reason.Trim(),
            InternalNote = request.InternalNote?.Trim()
        });
        if (request.Action is not ReviewAction.Start) await sessions.RevokeAsync(user, ct);
        // Uma única transação EF persiste decisão, revogação e histórico; concorrência nunca sobrescreve outra análise.
        // Mudança: docs/mudancas/2026-09-15-01-analise-administrativa.md
        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateConcurrencyException) { db.ChangeTracker.Clear(); return Stale(); }
        return IdentityResult<ReviewItem>.Ok(ToItem(user));
    }

    private static bool IsApproved(CourierDocument? doc) => doc is { StorageKey: not null, Status: CourierDocumentStatus.Approved }
        && doc.ExpiresAt > DateTimeOffset.UtcNow;
    private static IdentityResult<ReviewItem> Stale() => IdentityResult<ReviewItem>.Conflict("Cadastro alterado. Recarregue antes de decidir.");
    private static ReviewItem ToItem(User user) => new(user.Id, user.Role.ToString(),
        user.Status == UserStatus.Blocked ? "Blocked" : "Enabled", user.ReviewStatus.ToString(), user.ReviewVersion, user.CreatedAt);
}
