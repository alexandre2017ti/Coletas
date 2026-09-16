using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;

namespace Coletas.Infrastructure.Identity;

public sealed class RecoveryMailOptions
{
    public string Host { get; set; } = "";
    public int Port { get; set; } = 587;
    public string Username { get; set; } = "";
    public string Password { get; set; } = "";
    public string From { get; set; } = "";
    public string ResetUrl { get; set; } = "";
    public bool EnableSsl { get; set; } = true;
}

public interface IRecoveryMailer
{
    bool IsConfigured { get; }
    Task SendAsync(string recipient, string token, CancellationToken ct);
}

/// <summary>SMTP configurável; nunca registra destinatários, conteúdo ou falhas com credenciais.</summary>
public sealed class RecoveryMailer(IOptions<RecoveryMailOptions> settings) : IRecoveryMailer
{
    public bool IsConfigured => !string.IsNullOrWhiteSpace(settings.Value.Host) && !string.IsNullOrWhiteSpace(settings.Value.From)
        && settings.Value.EnableSsl && Uri.TryCreate(settings.Value.ResetUrl, UriKind.Absolute, out var uri) && uri.Scheme == "https";

    public async Task SendAsync(string recipient, string token, CancellationToken ct)
    {
        if (!IsConfigured) return;
        var o = settings.Value;
        // Regra: fragmento evita enviar o token em query/log de acesso; frontend lê #token.
        // Mudança: docs/mudancas/2026-09-10-14-backend-fase-1.md
        using var message = new MailMessage(o.From, recipient, "Recuperação de acesso Coletas", $"Redefina sua senha: {o.ResetUrl}#token={Uri.EscapeDataString(token)}");
        using var client = new SmtpClient(o.Host, o.Port) { EnableSsl = true, Credentials = new NetworkCredential(o.Username, o.Password), Timeout = 15000 };
        try { await client.SendMailAsync(message, ct); }
        catch (SmtpException) { /* Resposta genérica impede enumeração por falha de entrega. */ }
    }
}
