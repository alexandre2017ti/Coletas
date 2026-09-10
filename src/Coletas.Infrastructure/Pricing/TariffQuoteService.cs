using Coletas.Application.Pricing;
using Coletas.Domain.Pricing;
using Microsoft.Extensions.Options;

namespace Coletas.Infrastructure.Pricing;

/// <summary>Implementa cotação com parâmetros fornecidos pelo ambiente.</summary>
public sealed class TariffQuoteService(IOptions<TariffOptions> options) : ITariffQuoteService
{
    /// <inheritdoc />
    public TariffQuoteResult Calculate(TariffQuoteRequest request)
    {
        if (request.RouteDistanceKm < 0m)
        {
            return TariffQuoteResult.Fail("A distância da rota não pode ser negativa.");
        }

        var configuration = options.Value;
        var policy = new TariffPolicy(
            configuration.MinimumFee,
            configuration.DistanceAllowanceKm,
            configuration.PricePerKm,
            configuration.OperationalReturnFee);
        var calculation = new TariffCalculator(policy).Calculate(
            request.RouteDistanceKm,
            request.RequiresOperationalReturn);

        return TariffQuoteResult.Ok(new TariffQuoteResponse(
            request.RouteDistanceKm,
            policy.DistanceAllowanceKm,
            policy.PricePerKm,
            calculation.MinimumFee,
            calculation.DistanceFee,
            calculation.OperationalReturnFee,
            request.RequiresOperationalReturn,
            calculation.Total));
    }
}
