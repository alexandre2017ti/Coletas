namespace Coletas.Domain.Identity;

/// <summary>Perfis autorizáveis da plataforma.</summary>
public enum UserRole
{
    /// <summary>Administra a plataforma.</summary>
    Admin,
    /// <summary>Opera entregas e cadastros.</summary>
    Operator,
    /// <summary>Representa um estabelecimento.</summary>
    Establishment,
    /// <summary>Representa um entregador.</summary>
    Courier
}

/// <summary>Estados de acesso da conta.</summary>
public enum UserStatus
{
    /// <summary>Cadastro aguardando análise.</summary>
    Pending,
    /// <summary>Conta liberada para autenticação.</summary>
    Active,
    /// <summary>Conta impedida de autenticar.</summary>
    Blocked
}

/// <summary>Usuário autenticável e proprietário de um perfil operacional.</summary>
public sealed class User
{
    /// <summary>Identificador público não sequencial.</summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>E-mail normalizado.</summary>
    public required string Email { get; init; }

    /// <summary>Hash BCrypt da senha.</summary>
    public required string PasswordHash { get; init; }

    /// <summary>Perfil de autorização.</summary>
    public UserRole Role { get; init; }

    /// <summary>Estado atual da conta.</summary>
    public UserStatus Status { get; set; }

    /// <summary>Momento de criação em UTC.</summary>
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
}
