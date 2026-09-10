using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Coletas.Api.Responses;
using Coletas.Application.Identity;
using Coletas.Application.Foundation;
using Coletas.Application.Pricing;
using Coletas.Domain.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Coletas.Api.Controllers;

/// <summary>Operações HTTP de Tariffs.</summary>
[ApiController]
[Tags("Tariffs")]
public sealed class TariffsController : ControllerBase
{
    /// <summary>QuoteTariff: entrada HTTP delegada ao serviço responsável.</summary>
    [HttpPost("/api/v1/tariffs/quote", Name = "QuoteTariff")]
    [ProducesResponseType(typeof(TariffQuoteResponse), 200)]
    [ProducesResponseType(typeof(ValidationProblemDetails), 400)]
    public IResult QuoteTariff(
        [FromBody] TariffQuoteRequest request,
        [FromServices] ITariffQuoteService service)
    {
        var result = service.Calculate(request);
        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.ValidationProblem(new Dictionary<string, string[]> { ["routeDistanceKm"] = [result.Error!] });
    }

}
