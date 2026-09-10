using Coletas.Domain.Couriers;
using Coletas.Domain.Identity;

namespace Coletas.Application.Identity;

/// <summary>Dados para cadastro de estabelecimento.</summary>
public sealed record EstablishmentRegistrationRequest(
    string Email,
    string Password,
    string LegalName,
    string TradeName,
    string TaxId,
    string PhoneWhatsApp);

/// <summary>Dados para cadastro de entregador e veículo.</summary>
public sealed record CourierRegistrationRequest(
    string Email,
    string Password,
    string FullName,
    string PhoneWhatsApp,
    VehicleType VehicleType,
    string Plate);

/// <summary>Dados mínimos para registrar um documento privado.</summary>
public sealed record CourierDocumentRequest(CourierDocumentType Type, DateTimeOffset? ExpiresAt);

/// <summary>Dados de login.</summary>
public sealed record LoginRequest(string Email, string Password);

/// <summary>Resultado não sensível do cadastro.</summary>
public sealed record RegistrationResponse(Guid UserId, UserRole Role, UserStatus Status);

/// <summary>Token de acesso de curta duração.</summary>
public sealed record AuthResponse(string AccessToken, DateTimeOffset ExpiresAt, UserRole Role);

/// <summary>Documento sem conteúdo privado.</summary>
public sealed record CourierDocumentResponse(Guid Id, CourierDocumentType Type, CourierDocumentStatus Status, DateTimeOffset? ExpiresAt);

/// <summary>Resultado explícito de uma operação de identidade.</summary>
public readonly record struct IdentityResult<T>(T? Value, string? Error, bool IsSuccess, int StatusCode)
{
    /// <summary>Cria um resultado bem-sucedido.</summary>
    public static IdentityResult<T> Ok(T value) => new(value, null, true, 200);

    /// <summary>Cria um resultado de erro para entrada inválida.</summary>
    public static IdentityResult<T> Invalid(string error) => new(default, error, false, 400);

    /// <summary>Cria um resultado de conflito.</summary>
    public static IdentityResult<T> Conflict(string error) => new(default, error, false, 409);

    /// <summary>Cria um resultado não autorizado.</summary>
    public static IdentityResult<T> Unauthorized(string error) => new(default, error, false, 401);

    /// <summary>Cria um resultado proibido.</summary>
    public static IdentityResult<T> Forbidden(string error) => new(default, error, false, 403);

    /// <summary>Cria um resultado não encontrado.</summary>
    public static IdentityResult<T> NotFound(string error) => new(default, error, false, 404);
}

/// <summary>Operações de acesso e cadastros da Fase 1.</summary>
public interface IIdentityService
{
    /// <summary>Cadastra um estabelecimento pendente.</summary>
    Task<IdentityResult<RegistrationResponse>> RegisterEstablishmentAsync(EstablishmentRegistrationRequest request, CancellationToken cancellationToken);

    /// <summary>Cadastra um entregador pendente com veículo.</summary>
    Task<IdentityResult<RegistrationResponse>> RegisterCourierAsync(CourierRegistrationRequest request, CancellationToken cancellationToken);

    /// <summary>Autentica uma conta ativa.</summary>
    Task<IdentityResult<AuthResponse>> LoginAsync(LoginRequest request, CancellationToken cancellationToken);

    /// <summary>Registra metadados de documento do entregador autorizado.</summary>
    Task<IdentityResult<CourierDocumentResponse>> AddCourierDocumentAsync(Guid actorId, UserRole actorRole, Guid courierId, CourierDocumentRequest request, CancellationToken cancellationToken);

    /// <summary>Altera o estado de uma conta por ação administrativa.</summary>
    Task<IdentityResult<RegistrationResponse>> SetUserStatusAsync(UserRole actorRole, Guid userId, UserStatus status, CancellationToken cancellationToken);
}
