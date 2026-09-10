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

/// <summary>Operações HTTP de Auth.</summary>
[ApiController]
[Tags("Identity")]
public sealed class AuthController : ControllerBase
{
    /// <summary>RegisterEstablishment: entrada HTTP delegada ao serviço responsável.</summary>
    [HttpPost("/api/v1/auth/register/establishments", Name = "RegisterEstablishment")]
    [EnableRateLimiting("auth")]
    [ProducesResponseType(typeof(RegistrationResponse), 201)]
    [ProducesResponseType(typeof(ValidationProblemDetails), 400)]
    public async Task<IResult> RegisterEstablishment(
        [FromBody] EstablishmentRegistrationRequest request,
        [FromServices] IIdentityService service,
        CancellationToken cancellationToken)
    {
        var result = await service.RegisterEstablishmentAsync(request, cancellationToken);
        return IdentityHttpResultMapper.ToHttpResult(result, StatusCodes.Status201Created);
    }

    /// <summary>RegisterCourier: entrada HTTP delegada ao serviço responsável.</summary>
    [HttpPost("/api/v1/auth/register/couriers", Name = "RegisterCourier")]
    [EnableRateLimiting("auth")]
    [ProducesResponseType(typeof(RegistrationResponse), 201)]
    [ProducesResponseType(typeof(ValidationProblemDetails), 400)]
    public async Task<IResult> RegisterCourier(
        [FromBody] CourierRegistrationRequest request,
        [FromServices] IIdentityService service,
        CancellationToken cancellationToken)
    {
        var result = await service.RegisterCourierAsync(request, cancellationToken);
        return IdentityHttpResultMapper.ToHttpResult(result, StatusCodes.Status201Created);
    }

    /// <summary>Login: entrada HTTP delegada ao serviço responsável.</summary>
    [HttpPost("/api/v1/auth/login", Name = "Login")]
    [EnableRateLimiting("auth")]
    [ProducesResponseType(typeof(AuthResponse), 200)]
    [ProducesResponseType(typeof(ValidationProblemDetails), 400)]
    public async Task<IResult> Login(
        [FromBody] LoginRequest request,
        [FromServices] IIdentityService service,
        CancellationToken cancellationToken)
    {
        var result = await service.LoginAsync(request, cancellationToken);
        return IdentityHttpResultMapper.ToHttpResult(result);
    }

    /// <summary>GetCurrentUser: entrada HTTP delegada ao serviço responsável.</summary>
    [HttpGet("/api/v1/me", Name = "GetCurrentUser")]
    [Authorize]
    public IResult GetCurrentUser()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);
        return Results.Ok(new { UserId = userId, Role = User.FindFirstValue(ClaimTypes.Role) });
    }

}
