using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Coletas.Application.Identity;
using Coletas.Domain.Identity;
using Coletas.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using SecurityToken = Coletas.Domain.Identity.SecurityToken;

namespace Coletas.Infrastructure.Identity;

public sealed class SessionOptions
{
    public int RefreshDays { get; set; } = 7;
    public int RecoveryMinutes { get; set; } = 30;
    public int OnboardingMinutes { get; set; } = 20;
}

/// <summary>Credenciais opacas de uso único e revogação de sessões.</summary>
public sealed class SessionService(ColetasDbContext database, IOptions<JwtOptions> jwt,
    IOptions<SessionOptions> settings, IRecoveryMailer mailer)
{
    internal static string Hash(string token) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
    private static string RandomToken() => Convert.ToHexString(RandomNumberGenerator.GetBytes(32));

    public async Task<AuthResponse> CreateAsync(User user, bool onboarding, CancellationToken ct)
    {
        var raw = RandomToken();
        var expiry = onboarding ? DateTimeOffset.UtcNow.AddMinutes(settings.Value.OnboardingMinutes)
            : DateTimeOffset.UtcNow.AddDays(settings.Value.RefreshDays);
        var session = new SecurityToken { UserId = user.Id, Hash = Hash(raw), Purpose = onboarding ? "onboarding" : "refresh", ExpiresAt = expiry };
        database.SecurityTokens.Add(session);
        user.SecurityVersion++;
        await database.SaveChangesAsync(ct);
        var expires = DateTimeOffset.UtcNow.AddMinutes(onboarding ? settings.Value.OnboardingMinutes : jwt.Value.ExpirationMinutes);
        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()), new Claim(ClaimTypes.Role, user.Role.ToString()),
            new Claim("sid", session.Id.ToString()), new Claim("scope", onboarding ? "onboarding" : "access") };
        var token = new JwtSecurityToken(jwt.Value.Issuer, jwt.Value.Audience, claims, DateTime.UtcNow, expires.UtcDateTime,
            new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Value.SigningKey)), SecurityAlgorithms.HmacSha256));
        return new(new JwtSecurityTokenHandler().WriteToken(token), expires, user.Role, onboarding ? null : raw, onboarding ? null : expiry);
    }

    public async Task<IdentityResult<AuthResponse>> OnboardingAsync(LoginRequest request, CancellationToken ct)
    {
        var email = request.Email?.Trim().ToLowerInvariant();
        var user = await database.Users.SingleOrDefaultAsync(x => x.Email == email, ct);
        if (user is null || user.Status != UserStatus.Pending || string.IsNullOrEmpty(request.Password)
            || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            return IdentityResult<AuthResponse>.Unauthorized("Credenciais inválidas.");
        return IdentityResult<AuthResponse>.Ok(await CreateAsync(user, true, ct));
    }

    public async Task<IdentityResult<AuthResponse>> RefreshAsync(string? raw, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(raw) || raw.Length > 128) return InvalidSession();
        // Regra: a versão concorrente do usuário serializa consumo e emissão no mesmo SaveChanges.
        // Reuso revoga todas as sessões, inclusive a emitida pelo vencedor de uma corrida.
        // Mudança: docs/mudancas/2026-09-10-14-backend-fase-1.md
        for (var attempt = 0; attempt < 3; attempt++)
        {
            var hash = Hash(raw);
            var token = await database.SecurityTokens.SingleOrDefaultAsync(x => x.Hash == hash && x.Purpose == "refresh", ct);
            if (token is null) return InvalidSession();
            var user = await database.Users.SingleAsync(x => x.Id == token.UserId, ct);
            try
            {
                if (token.Used)
                {
                    await RevokeAsync(user, ct);
                    await database.SaveChangesAsync(ct);
                    return InvalidSession();
                }
                if (token.ExpiresAt <= DateTimeOffset.UtcNow || user.Status != UserStatus.Active) return InvalidSession();
                token.Used = true;
                return IdentityResult<AuthResponse>.Ok(await CreateAsync(user, false, ct));
            }
            catch (DbUpdateConcurrencyException) { database.ChangeTracker.Clear(); }
        }
        return InvalidSession();
    }

    /// <summary>Revoga inclusive onboarding, cujo token não possui refresh token.</summary>
    public async Task LogoutUserAsync(Guid userId, CancellationToken ct)
    {
        // O identificador vem do JWT validado, nunca do corpo enviado pelo cliente.
        // Mudança: docs/mudancas/2026-09-16-02-servicos-e-cliente-http.md
        var user = await database.Users.SingleAsync(x => x.Id == userId, ct);
        await RevokeAsync(user, ct);
        await database.SaveChangesAsync(ct);
    }

    public async Task RequestRecoveryAsync(string? email, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(email) || email.Length > 254) return;
        // Regra: sem SMTP configurado, a resposta continua genérica e nenhum token é divulgado.
        // Mudança: docs/mudancas/2026-09-10-14-backend-fase-1.md
        if (!mailer.IsConfigured) return;
        var normalized = email.Trim().ToLowerInvariant();
        var user = await database.Users.SingleOrDefaultAsync(x => x.Email == normalized, ct);
        if (user is null || user.Status == UserStatus.Blocked) return;
        var raw = RandomToken();
        database.SecurityTokens.Add(new SecurityToken
        {
            UserId = user.Id,
            Hash = Hash(raw),
            Purpose = "recovery",
            ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(settings.Value.RecoveryMinutes)
        });
        user.SecurityVersion++;
        await database.SaveChangesAsync(ct);
        await mailer.SendAsync(user.Email, raw, ct);
    }

    public async Task<bool> ResetAsync(string? raw, string? password, CancellationToken ct)
    {
        // BCrypt só usa até 72 bytes; manter o mesmo limite do cadastro evita truncamento.
        // Mudança: docs/mudancas/2026-09-16-03-integracao-acesso-administracao.md
        if (string.IsNullOrEmpty(raw) || raw.Length > 128 || string.IsNullOrWhiteSpace(password)
            || password.Length is < 12 or > 128 || Encoding.UTF8.GetByteCount(password) > 72) return false;
        var hash = Hash(raw);
        var token = await database.SecurityTokens.SingleOrDefaultAsync(x => x.Hash == hash && x.Purpose == "recovery", ct);
        if (token is null || token.Used || token.ExpiresAt <= DateTimeOffset.UtcNow) return false;
        var user = await database.Users.SingleAsync(x => x.Id == token.UserId, ct);
        if (user.Status == UserStatus.Blocked) return false;
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(password);
        await RevokeAsync(user, ct);
        try { await database.SaveChangesAsync(ct); return true; }
        catch (DbUpdateConcurrencyException) { database.ChangeTracker.Clear(); return false; }
    }

    public async Task RevokeAsync(User user, CancellationToken ct)
    {
        foreach (var token in await database.SecurityTokens.Where(x => x.UserId == user.Id && !x.Used).ToListAsync(ct)) token.Used = true;
        user.SecurityVersion++;
    }

    private static IdentityResult<AuthResponse> InvalidSession() => IdentityResult<AuthResponse>.Unauthorized("Sessão inválida.");
}
