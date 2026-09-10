namespace Coletas.Domain.Pricing;

/// <summary>Parâmetros monetários usados para calcular uma cotação.</summary>
public sealed record TariffPolicy(
    decimal MinimumFee,
    decimal DistanceAllowanceKm,
    decimal PricePerKm,
    decimal OperationalReturnFee)
{
    /// <summary>Valida os invariantes necessários para uma política utilizável.</summary>
    public void Validate()
    {
        if (MinimumFee < 0m)
        {
            throw new InvalidOperationException("A taxa mínima não pode ser negativa.");
        }

        if (DistanceAllowanceKm < 0m)
        {
            throw new InvalidOperationException("A franquia de distância não pode ser negativa.");
        }

        if (PricePerKm < 0m)
        {
            throw new InvalidOperationException("O adicional por quilômetro não pode ser negativo.");
        }

        if (OperationalReturnFee < 0m)
        {
            throw new InvalidOperationException("O adicional de volta não pode ser negativo.");
        }
    }
}

/// <summary>Composição monetária de uma cotação de entrega.</summary>
public sealed record TariffCalculation(
    decimal MinimumFee,
    decimal DistanceFee,
    decimal OperationalReturnFee,
    decimal Total);

/// <summary>Calcula tarifas sem dependência de transporte, banco ou interface.</summary>
public sealed class TariffCalculator(TariffPolicy policy)
{
    /// <summary>Calcula a tarifa usando a distância da rota e a necessidade de volta.</summary>
    /// <param name="routeDistanceKm">Distância da rota em quilômetros.</param>
    /// <param name="requiresOperationalReturn">Indica se haverá retorno ao estabelecimento.</param>
    public TariffCalculation Calculate(decimal routeDistanceKm, bool requiresOperationalReturn)
    {
        policy.Validate();

        if (routeDistanceKm < 0m)
        {
            throw new ArgumentOutOfRangeException(nameof(routeDistanceKm), "A distância da rota não pode ser negativa.");
        }

        // Regra: a franquia impede que entregas curtas fiquem abaixo da taxa mínima; a volta
        // é um custo fixo separado porque troco, maquininha e casco geram retorno operacional.
        // Mudança: docs/mudancas/2026-09-10-02-tarifa-volta-operacional.md
        var excessDistanceKm = Math.Max(0m, routeDistanceKm - policy.DistanceAllowanceKm);
        var distanceFee = excessDistanceKm * policy.PricePerKm;
        var returnFee = requiresOperationalReturn ? policy.OperationalReturnFee : 0m;
        var total = decimal.Round(policy.MinimumFee + distanceFee + returnFee, 2, MidpointRounding.AwayFromZero);

        return new(policy.MinimumFee, decimal.Round(distanceFee, 2, MidpointRounding.AwayFromZero), returnFee, total);
    }
}
