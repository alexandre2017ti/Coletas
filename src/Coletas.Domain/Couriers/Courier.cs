namespace Coletas.Domain.Couriers;

/// <summary>Cadastro operacional de um entregador.</summary>
public sealed class Courier
{
    /// <summary>Identificador do entregador.</summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>Usuário responsável pela conta.</summary>
    public Guid UserId { get; init; }

    /// <summary>Nome completo.</summary>
    public required string FullName { get; init; }

    /// <summary>Telefone com WhatsApp.</summary>
    public required string PhoneWhatsApp { get; init; }

    /// <summary>CPF normalizado; nulo apenas para cadastro legado ainda não complementado.</summary>
    public string? Cpf { get; init; }

    /// <summary>Veículos cadastrados.</summary>
    public ICollection<Vehicle> Vehicles { get; init; } = [];

    /// <summary>Documentos enviados.</summary>
    public ICollection<CourierDocument> Documents { get; init; } = [];
}

/// <summary>Tipo de veículo habilitado no cadastro.</summary>
public enum VehicleType
{
    /// <summary>Motocicleta.</summary>
    Motorcycle,
    /// <summary>Automóvel.</summary>
    Car
}

/// <summary>Veículo utilizado pelo entregador.</summary>
public sealed class Vehicle
{
    /// <summary>Identificador do veículo.</summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>Entregador proprietário.</summary>
    public Guid CourierId { get; init; }

    /// <summary>Tipo do veículo.</summary>
    public VehicleType Type { get; set; }

    /// <summary>Placa normalizada.</summary>
    public required string Plate { get; set; }
}
