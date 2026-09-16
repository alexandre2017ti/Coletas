namespace Coletas.Domain.Identity;

public enum ReviewStatus { Pending, InReview, NeedsCorrection, Approved, Rejected }
public enum ReviewAction { Start, RequestCorrection, Approve, Reject, Block, Unblock }

/// <summary>Transições cadastrais independentes do bloqueio de acesso.</summary>
public static class RegistrationReview
{
    public static bool CanTransition(ReviewStatus status, ReviewAction action) => action switch
    {
        ReviewAction.Start => status is ReviewStatus.Pending or ReviewStatus.NeedsCorrection,
        ReviewAction.Approve or ReviewAction.Reject or ReviewAction.RequestCorrection => status == ReviewStatus.InReview,
        ReviewAction.Block or ReviewAction.Unblock => true,
        _ => false
    };

    public static ReviewStatus Next(ReviewStatus status, ReviewAction action) => action switch
    {
        ReviewAction.Start => ReviewStatus.InReview,
        ReviewAction.RequestCorrection => ReviewStatus.NeedsCorrection,
        ReviewAction.Approve => ReviewStatus.Approved,
        ReviewAction.Reject => ReviewStatus.Rejected,
        _ => status
    };
}

/// <summary>Registro imutável de decisão; notas internas nunca compõem a visão do titular.</summary>
public sealed class ReviewEvent
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid UserId { get; init; }
    public Guid ActorId { get; init; }
    public long Version { get; init; }
    public required string Action { get; init; }
    public required string PublicReason { get; init; }
    public string? InternalNote { get; init; }
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
}
