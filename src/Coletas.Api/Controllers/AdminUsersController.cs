using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Coletas.Api.Responses;
using Coletas.Application.Identity;
using Coletas.Application.Foundation;
using Coletas.Application.Pricing;
using Coletas.Domain.Identity;
using Coletas.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Coletas.Api.Controllers;

/// <summary>Operações HTTP de AdminUsers.</summary>
[ApiController]
[Tags("Identity")]
public sealed class AdminUsersController : ControllerBase
{
    /// <summary>ApproveUser: entrada HTTP delegada ao serviço responsável.</summary>
    [HttpPost("/api/v1/admin/users/{userId:guid}/approve", Name = "ApproveUser")]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(typeof(RegistrationResponse), 200)]
    [ProducesResponseType(typeof(ValidationProblemDetails), 400)]
    public async Task<IResult> ApproveUser(
        Guid userId,
        [FromBody] ReviewDecision request,
        [FromServices] RegistrationReviewService service,
        CancellationToken cancellationToken)
    {
        // Compatibilidade de rota, não de bypass: a versão e o motivo passam a ser obrigatórios.
        // Mudança: docs/mudancas/2026-09-15-01-analise-administrativa.md
        var result = await service.DecideAsync(Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!), userId,
            request with { Action = ReviewAction.Approve }, cancellationToken);
        return IdentityHttpResultMapper.ToHttpResult(result);
    }

    /// <summary>BlockUser: entrada HTTP delegada ao serviço responsável.</summary>
    [HttpPost("/api/v1/admin/users/{userId:guid}/block", Name = "BlockUser")]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(typeof(RegistrationResponse), 200)]
    [ProducesResponseType(typeof(ValidationProblemDetails), 400)]
    public async Task<IResult> BlockUser(
        Guid userId,
        [FromBody] ReviewDecision request,
        [FromServices] RegistrationReviewService service,
        CancellationToken cancellationToken)
    {
        var result = await service.DecideAsync(Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!), userId,
            request with { Action = ReviewAction.Block }, cancellationToken);
        return IdentityHttpResultMapper.ToHttpResult(result);
    }

}
