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

/// <summary>Operações HTTP de Couriers.</summary>
[ApiController]
[Tags("Identity")]
public sealed class CouriersController : ControllerBase
{
    /// <summary>AddCourierDocument: entrada HTTP delegada ao serviço responsável.</summary>
    [HttpPost("/api/v1/couriers/{courierId:guid}/documents", Name = "AddCourierDocument")]
    [Authorize]
    [ProducesResponseType(typeof(CourierDocumentResponse), 201)]
    [ProducesResponseType(typeof(ValidationProblemDetails), 400)]
    public async Task<IResult> AddCourierDocument(
        Guid courierId,
        [FromBody] CourierDocumentRequest request,
        [FromServices] IIdentityService service,
        CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub), out var actorId)
            || !Enum.TryParse<UserRole>(User.FindFirstValue(ClaimTypes.Role), out var actorRole))
        {
            return Results.Unauthorized();
        }

        var result = await service.AddCourierDocumentAsync(actorId, actorRole, courierId, request, cancellationToken);
        return IdentityHttpResultMapper.ToHttpResult(result, StatusCodes.Status201Created);
    }

}
