using Coletas.Domain.Identity;

namespace Coletas.Application.Identity;

public sealed record ReviewDecision(long ExpectedVersion, ReviewAction Action, string Reason, string? InternalNote = null);
public sealed record ReviewItem(Guid UserId, string Role, string AccountStatus, string ReviewStatus, long Version, DateTimeOffset CreatedAt);
public sealed record ReviewPage(IReadOnlyList<ReviewItem> Items, int Page, int PageSize, int Total);
public sealed record ReviewHistory(Guid Id, Guid? ActorId, string Action, string Reason, string? InternalNote, long Version, DateTimeOffset CreatedAt);
public sealed record ReviewDetail(ReviewItem Review, IReadOnlyList<ReviewHistory> History);
