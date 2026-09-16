namespace Coletas.Domain.Identity;

/// <summary>Token opaco persistido somente como hash.</summary>
public sealed class SecurityToken
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid UserId { get; init; }
    public required string Hash { get; init; }
    public required string Purpose { get; init; }
    public DateTimeOffset ExpiresAt { get; init; }
    public bool Used { get; set; }
}

/// <summary>Evento administrativo sem credenciais ou conteúdo documental.</summary>
public sealed class IdentityAudit
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid ActorId { get; init; }
    public Guid SubjectId { get; init; }
    public required string Action { get; init; }
    public required string Detail { get; init; }
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
}
