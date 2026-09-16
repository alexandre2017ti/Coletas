using Coletas.Infrastructure.Identity;

namespace Coletas.Api.Database;

/// <summary>Cria o primeiro administrador somente no console local interativo.</summary>
public static class AdminBootstrapRunner
{
    public static async Task<bool> RunIfRequestedAsync(WebApplication app, string[] args)
    {
        if (!args.Contains("--bootstrap-admin", StringComparer.Ordinal)) return false;
        // Senha não entra em argumentos, histórico do shell, arquivo ou logs.
        // Mudança: docs/mudancas/2026-09-15-01-analise-administrativa.md
        if (Console.IsInputRedirected) throw new InvalidOperationException("Use um terminal interativo para criar o administrador.");
        Console.Write("E-mail do administrador: ");
        var email = Console.ReadLine() ?? "";
        Console.Write("Senha (12+ caracteres; não será exibida): ");
        var password = new System.Text.StringBuilder();
        while (true)
        {
            var key = Console.ReadKey(intercept: true);
            if (key.Key == ConsoleKey.Enter) break;
            if (key.Key == ConsoleKey.Backspace) { if (password.Length > 0) password.Length--; }
            else if (!char.IsControl(key.KeyChar) && password.Length < 128) password.Append(key.KeyChar);
        }
        Console.WriteLine();
        using var scope = app.Services.CreateScope();
        var created = await scope.ServiceProvider.GetRequiredService<AdminBootstrapService>().BootstrapAsync(email, password.ToString(), CancellationToken.None);
        password.Clear();
        Console.WriteLine(created ? "Administrador criado." : "Não criado: confira os dados ou a existência de administrador.");
        Environment.ExitCode = created ? 0 : 1;
        return true;
    }
}
