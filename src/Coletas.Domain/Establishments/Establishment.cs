namespace Coletas.Domain.Establishments;

/// <summary>Dados cadastrais de um estabelecimento.</summary>
public sealed class Establishment
{
    /// <summary>Identificador do estabelecimento.</summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>Usuário responsável pela conta.</summary>
    public Guid UserId { get; init; }

    /// <summary>Razão social.</summary>
    public required string LegalName { get; init; }

    /// <summary>Nome utilizado na operação.</summary>
    public required string TradeName { get; init; }

    /// <summary>Documento empresarial normalizado.</summary>
    public required string TaxId { get; init; }

    /// <summary>Telefone com WhatsApp.</summary>
    public required string PhoneWhatsApp { get; init; }
}
