namespace Coletas.Infrastructure.Pricing;

/// <summary>Configuração externa da política tarifária.</summary>
public sealed class TariffOptions
{
    /// <summary>Nome da seção de configuração.</summary>
    public const string SectionName = "Tariff";

    /// <summary>Taxa mínima em reais.</summary>
    public decimal MinimumFee { get; init; }

    /// <summary>Franquia de distância em quilômetros.</summary>
    public decimal DistanceAllowanceKm { get; init; }

    /// <summary>Adicional aplicado por quilômetro excedente.</summary>
    public decimal PricePerKm { get; init; }

    /// <summary>Adicional cobrado quando a operação exige retorno.</summary>
    public decimal OperationalReturnFee { get; init; }
}
