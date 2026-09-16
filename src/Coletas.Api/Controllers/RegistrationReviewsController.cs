using System.Security.Claims;
using Coletas.Api.Responses;
using Coletas.Application.Identity;
using Coletas.Domain.Identity;
using Coletas.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Coletas.Api.Controllers;

[ApiController]
[Authorize(Policy = "AdminOnly")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class RegistrationReviewsController(RegistrationReviewService reviews) : ControllerBase
{
    private Guid Actor => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet("/api/v1/admin/reviews")]
    public async Task<IResult> List(CancellationToken ct, ReviewStatus? status = null, UserRole? role = null, int page = 1, int pageSize = 20)
        => IdentityHttpResultMapper.ToHttpResult(await reviews.ListAsync(Actor, status, role, page, pageSize, ct));

    [HttpGet("/api/v1/admin/reviews/{userId:guid}")]
    public async Task<IResult> Get(Guid userId, CancellationToken ct)
        => IdentityHttpResultMapper.ToHttpResult(await reviews.GetAsync(Actor, userId, ct));

    [HttpPost("/api/v1/admin/reviews/{userId:guid}/decisions")]
    public async Task<IResult> Decide(Guid userId, [FromBody] ReviewDecision request, CancellationToken ct)
        => IdentityHttpResultMapper.ToHttpResult(await reviews.DecideAsync(Actor, userId, request, ct));
}
