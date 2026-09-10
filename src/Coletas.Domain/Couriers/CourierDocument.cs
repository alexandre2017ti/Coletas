namespace Coletas.Domain.Couriers;

/// <summary>Tipos de documentos exigidos na análise do entregador.</summary>
public enum CourierDocumentType
{
    /// <summary>Carteira Nacional de Habilitação.</summary>
    DriverLicense,
    /// <summary>Documento do veículo.</summary>
    VehicleRegistration
}

/// <summary>Estados do documento enviado.</summary>
public enum CourierDocumentStatus
{
    /// <summary>Aguardando análise.</summary>
    Pending,
    /// <summary>Aprovado.</summary>
    Approved,
    /// <summary>Reprovado.</summary>
    Rejected,
    /// <summary>Vencido.</summary>
    Expired,
    /// <summary>Bloqueado.</summary>
    Blocked
}

/// <summary>Metadados privados de um documento do entregador.</summary>
public sealed class CourierDocument
{
    /// <summary>Identificador do documento.</summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>Entregador relacionado.</summary>
    public Guid CourierId { get; init; }

    /// <summary>Tipo documental.</summary>
    public CourierDocumentType Type { get; init; }

    /// <summary>Data de vencimento informada.</summary>
    public DateTimeOffset? ExpiresAt { get; init; }

    /// <summary>Estado da análise.</summary>
    public CourierDocumentStatus Status { get; set; }

    /// <summary>Data da criação do registro.</summary>
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
}
