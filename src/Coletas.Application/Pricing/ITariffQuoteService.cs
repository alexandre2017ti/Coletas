namespace Coletas.Application.Pricing;

/// <summary>Dados necessários para solicitar uma cotação.</summary>
public sealed record TariffQuoteRequest(decimal RouteDistanceKm, bool RequiresOperationalReturn);

/// <summary>Resultado público da composição tarifária.</summary>
public sealed record TariffQuoteResponse(
    decimal RouteDistanceKm,
    decimal DistanceAllowanceKm,
    decimal PricePerKm,
    decimal MinimumFee,
    decimal DistanceFee,
    decimal OperationalReturnFee,
    bool RequiresOperationalReturn,
    decimal Total);

/// <summary>Resultado explícito do cálculo de tarifa.</summary>
public readonly record struct TariffQuoteResult(TariffQuoteResponse? Value, string? Error, bool IsSuccess)
{
    /// <summary>Cria um resultado bem-sucedido.</summary>
    public static TariffQuoteResult Ok(TariffQuoteResponse value) => new(value, null, true);

    /// <summary>Cria um resultado de validação inválido.</summary>
    public static TariffQuoteResult Fail(string error) => new(null, error, false);
}

/// <summary>Serviço de aplicação para cotação tarifária.</summary>
public interface ITariffQuoteService
{
    /// <summary>Calcula uma cotação sem criar ou confirmar uma entrega.</summary>
    TariffQuoteResult Calculate(TariffQuoteRequest request);
}
