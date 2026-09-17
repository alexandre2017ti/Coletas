using Coletas.Application.Identity;
using Coletas.Infrastructure.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Coletas.Api.Controllers;

[ApiController]
[EnableRateLimiting("auth")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class RecoveryController(SessionService sessions) : ControllerBase
{
    [HttpPost("/api/v1/auth/recovery")]
    public async Task<IResult> RequestRecovery(RecoveryRequest request, CancellationToken ct)
    {
        // Resposta uniforme não confirma se a conta existe nem se houve entrega do e-mail.
        // Mudança: docs/mudancas/2026-09-16-03-integracao-acesso-administracao.md
        await sessions.RequestRecoveryAsync(request.Email, ct);
        return Results.Ok(new { message = "Se houver uma conta elegível, você receberá as instruções." });
    }

    [HttpPost("/api/v1/auth/reset-password")]
    public async Task<IResult> Reset(ResetPasswordRequest request, CancellationToken ct)
        => await sessions.ResetAsync(request.Token, request.Password, ct)
            ? Results.NoContent()
            : Results.BadRequest(new { error = "Código inválido ou expirado, ou senha fora dos requisitos." });
}
