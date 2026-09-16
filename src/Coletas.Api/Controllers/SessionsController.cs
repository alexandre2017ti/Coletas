using System.Security.Claims;
using Coletas.Api.Responses;
using Coletas.Application.Identity;
using Coletas.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Coletas.Api.Controllers;

/// <summary>Renovação explícita de token e encerramento das sessões do titular.</summary>
[ApiController]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class SessionsController(SessionService sessions) : ControllerBase
{
    [HttpPost("/api/v1/auth/refresh")]
    [EnableRateLimiting("auth")]
    public async Task<IResult> Refresh([FromBody] RefreshRequest request, CancellationToken ct)
        => IdentityHttpResultMapper.ToHttpResult(await sessions.RefreshAsync(request.RefreshToken, ct));

    [HttpPost("/api/v1/auth/logout")]
    [Authorize(Policy = "AccountAccess")]
    public async Task<IResult> Logout(CancellationToken ct)
    {
        await sessions.LogoutUserAsync(Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!), ct);
        return Results.NoContent();
    }
}
