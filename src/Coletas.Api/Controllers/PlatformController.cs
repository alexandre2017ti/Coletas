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

/// <summary>Operações HTTP de Platform.</summary>
[ApiController]
[Tags("Foundation")]
public sealed class PlatformController : ControllerBase
{
    /// <summary>Informa nome e estágio da plataforma.</summary>
    [HttpGet("/api/v1/platform", Name = "GetPlatformInfo")]
    public IResult GetPlatformInfo([FromServices] PlatformInfoService service)
        => TypedResults.Ok(service.Get());
}
