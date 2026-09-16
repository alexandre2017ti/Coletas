using Coletas.Application.Identity;
using Coletas.Domain.Couriers;
using Coletas.Domain.Identity;
using Coletas.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Coletas.Infrastructure.Identity;

/// <summary>Cria o primeiro administrador exclusivamente pelo console local.</summary>
// Extração por responsabilidade: docs/mudancas/2026-09-16-02-servicos-e-cliente-http.md
public sealed class AdminBootstrapService(ColetasDbContext db)
{
    public async Task<bool> BootstrapAsync(string email, string password, CancellationToken ct)
    {
        if (!RegistrationValidation.IsEmail(email) || string.IsNullOrWhiteSpace(password)
            || password.Length < 12 || System.Text.Encoding.UTF8.GetByteCount(password) > 72) return false;
        // Serializa o primeiro cadastro administrativo entre consoles no PostgreSQL.
        // Mudança: docs/mudancas/2026-09-15-01-analise-administrativa.md
        await using var transaction = db.Database.IsRelational() ? await db.Database.BeginTransactionAsync(ct) : null;
        if (db.Database.IsNpgsql()) await db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(758319204)", ct);
        if (await db.Users.AnyAsync(x => x.Role == UserRole.Admin, ct)) return false;
        var user = new User { Email = email.Trim().ToLowerInvariant(), PasswordHash = BCrypt.Net.BCrypt.HashPassword(password), Role = UserRole.Admin, Status = UserStatus.Active };
        db.Users.Add(user);
        Audit(user.Id, user.Id, "admin.bootstrap", "Primeiro administrador criado pelo console local.");
        try { await db.SaveChangesAsync(ct); if (transaction is not null) await transaction.CommitAsync(ct); return true; }
        catch (DbUpdateException) { return false; }
    }

    private void Audit(Guid actor, Guid subject, string action, string detail) => db.IdentityAudits.Add(new IdentityAudit { ActorId = actor, SubjectId = subject, Action = action, Detail = detail });
}
