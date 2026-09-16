namespace Coletas.Infrastructure.Identity;

public sealed class PrivateDocumentOptions
{
    public string RootPath { get; set; } = "";
    public long MaxBytes { get; set; } = 10 * 1024 * 1024;
}

/// <summary>Arquivos privados fora da raiz web, com nomes gerados pelo servidor.</summary>
public sealed class PrivateDocumentStore(Microsoft.Extensions.Options.IOptions<PrivateDocumentOptions> settings)
{
    public long MaxBytes => settings.Value.MaxBytes;
    public bool IsConfigured => Path.IsPathFullyQualified(settings.Value.RootPath);

    private string Resolve(string key)
    {
        if (!IsConfigured || !Guid.TryParseExact(key, "N", out _)) throw new InvalidOperationException("Armazenamento privado indisponível.");
        return Path.Combine(settings.Value.RootPath, key);
    }

    public async Task<(string Key, string ContentType, long Length)> SaveAsync(Stream input, CancellationToken ct)
    {
        if (!IsConfigured) throw new InvalidOperationException("Configure PrivateDocuments:RootPath absoluto fora da raiz web.");
        Directory.CreateDirectory(settings.Value.RootPath);
        var key = Guid.NewGuid().ToString("N");
        var path = Resolve(key);
        try
        {
            await using var output = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, true);
            var buffer = new byte[81920];
            var header = new byte[8];
            var headerLength = 0;
            long length = 0;
            int count;
            while ((count = await input.ReadAsync(buffer, ct)) != 0)
            {
                length += count;
                if (length > MaxBytes) throw new InvalidDataException("Arquivo excede o limite configurado.");
                var take = Math.Min(8 - headerLength, count);
                buffer.AsSpan(0, take).CopyTo(header.AsSpan(headerLength));
                headerLength += take;
                await output.WriteAsync(buffer.AsMemory(0, count), ct);
            }
            // Regra: nome e Content-Type do cliente não comprovam formato; validar assinatura e servir como attachment.
            // Mudança: docs/mudancas/2026-09-10-14-backend-fase-1.md
            var type = headerLength >= 5 && header.AsSpan(0, 5).SequenceEqual("%PDF-"u8) ? "application/pdf"
                : headerLength >= 3 && header[0] == 255 && header[1] == 216 && header[2] == 255 ? "image/jpeg"
                : headerLength == 8 && header.AsSpan().SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }) ? "image/png" : null;
            if (type is null) throw new InvalidDataException("Envie PDF, JPEG ou PNG válido.");
            return (key, type, length);
        }
        catch { File.Delete(path); throw; }
    }

    public Stream? Open(string key)
    {
        var path = Resolve(key);
        return File.Exists(path) ? new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, true) : null;
    }

    public void Delete(string key) => File.Delete(Resolve(key));
}
