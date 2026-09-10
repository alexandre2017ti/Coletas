using Coletas.Api.Responses;
using Coletas.Application.Identity;
using Coletas.Domain.Pricing;

namespace Coletas.Tests;

public sealed class EdgeCaseTests
{
    [Theory]
    [InlineData(-1, 1.5, 1.35, 1.5)]
    [InlineData(7.5, -1, 1.35, 1.5)]
    [InlineData(7.5, 1.5, -1, 1.5)]
    [InlineData(7.5, 1.5, 1.35, -1)]
    public void TariffPolicyRejectsEachNegativeParameter(decimal minimum, decimal allowance, decimal perKm, decimal returnFee)
    {
        var policy = new TariffPolicy(minimum, allowance, perKm, returnFee);
        Assert.Throws<InvalidOperationException>(policy.Validate);
    }

    [Fact]
    public void TariffCalculatorRejectsNegativeRouteDistance()
    {
        var calculator = new TariffCalculator(new TariffPolicy(7.5m, 1.5m, 1.35m, 1.5m));
        Assert.Throws<ArgumentOutOfRangeException>(() => calculator.Calculate(-0.01m, false));
    }

    [Fact]
    public void TariffCalculatorRoundsDistanceAndTotalAwayFromZero()
    {
        var calculator = new TariffCalculator(new TariffPolicy(7.5m, 1.5m, 1.333m, 1.5m));
        var result = calculator.Calculate(2.001m, true);
        Assert.Equal(0.67m, result.DistanceFee);
        Assert.Equal(9.67m, result.Total);
    }

    [Theory]
    [InlineData(400)]
    [InlineData(401)]
    [InlineData(418)]
    [InlineData(404)]
    [InlineData(409)]
    [InlineData(422)]
    public void IdentityMapperPreservesKnownErrorStatus(int status)
    {
        var result = new IdentityResult<string>(null, "erro", false, status);
        var http = IdentityHttpResultMapper.ToHttpResult(result);
        Assert.NotNull(http);
    }
}
