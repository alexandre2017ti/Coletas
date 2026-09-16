using System.Security.Claims;
using Coletas.Api.Responses;
using Coletas.Application.Identity;
using Coletas.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Coletas.Api.Controllers;

[ApiController]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class AccountReviewController(RegistrationReviewService reviews, SessionService sessions, ProfileService profiles) : ControllerBase
{
    [HttpPost("/api/v1/auth/onboarding/login")]
    [EnableRateLimiting("auth")]
    public async Task<IResult> Login([FromBody] LoginRequest request, CancellationToken ct)
        => IdentityHttpResultMapper.ToHttpResult(await sessions.OnboardingAsync(request, ct));

    [Authorize(Policy = "AccountAccess")]
    [HttpGet("/api/v1/account/review")]
    public async Task<IResult> OwnReview(CancellationToken ct)
    {
        var actor = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        return IdentityHttpResultMapper.ToHttpResult(await reviews.GetAsync(actor, actor, ct));
    }

    [Authorize(Policy = "AccountAccess")]
    [HttpGet("/api/v1/auth/me")]
    public async Task<ActionResult<AccountProfile>> Profile(CancellationToken ct)
    {
        var profile = await profiles.GetAsync(Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!), ct);
        // MVC usa o contrato de enums textuais configurado em AddJsonOptions.
        // Mudança: docs/mudancas/2026-09-16-02-servicos-e-cliente-http.md
        return profile is null ? NotFound() : Ok(profile);
    }
}
