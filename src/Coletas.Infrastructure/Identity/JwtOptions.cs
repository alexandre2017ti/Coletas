namespace Coletas.Infrastructure.Identity;

/// <summary>Configuração do emissor JWT.</summary>
public sealed class JwtOptions
{
    /// <summary>Nome da seção de configuração.</summary>
    public const string SectionName = "Jwt";

    /// <summary>Chave de assinatura fornecida por segredo do ambiente.</summary>
    public string SigningKey { get; init; } = "";

    /// <summary>Emissor esperado.</summary>
    public string Issuer { get; init; } = "";

    /// <summary>Audiência esperada.</summary>
    public string Audience { get; init; } = "";

    /// <summary>Validade do access token em minutos.</summary>
    public int ExpirationMinutes { get; init; }
}
