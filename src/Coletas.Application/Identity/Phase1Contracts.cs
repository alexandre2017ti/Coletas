using Coletas.Domain.Couriers;
using Coletas.Domain.Identity;

namespace Coletas.Application.Identity;

public sealed record RefreshRequest(string RefreshToken);
public sealed record RecoveryRequest(string Email);
public sealed record ResetPasswordRequest(string Token, string Password);
public sealed record VehicleRequest(VehicleType Type, string Plate);
public sealed record VehicleResponse(Guid Id, VehicleType Type, string Plate);
public sealed record DocumentProfile(Guid Id, CourierDocumentType Type, CourierDocumentStatus Status, DateTimeOffset? ExpiresAt, bool HasFile, string? Reason);
public sealed record AccountProfile(Guid UserId, string Email, UserRole Role, UserStatus Status, Guid? CourierId,
    IReadOnlyList<VehicleResponse> Vehicles, IReadOnlyList<DocumentProfile> Documents, string? Reason, RegistrationDetails? Registration = null);
public sealed record RegistrationDetails(string Name, string? TradeName, string? TaxId, string PhoneWhatsApp);
public sealed record UserSummary(Guid UserId, string Email, UserRole Role, UserStatus Status, Guid? CourierId);
public sealed record UserPage(IReadOnlyList<UserSummary> Items, int Page, int PageSize, int Total);
public sealed record UserDecision(UserStatus Status, string Reason);
public sealed record DocumentDecision(CourierDocumentStatus Status, string? Reason, long? ExpectedVersion = null);
public sealed record DecisionReason(string? Reason);
public sealed record PrivateDocumentContent(Stream Stream, string ContentType);
