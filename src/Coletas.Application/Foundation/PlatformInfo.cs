namespace Coletas.Application.Foundation;
/// <summary>Identificação pública sem entidades de persistência.</summary>
public sealed record PlatformInfo(string Name, string Stage);
/// <summary>Identifica a base instalada.</summary>
public sealed class PlatformInfoService
{
    /// <summary>Retorna a fase implementada.</summary>
    public PlatformInfo Get() => new("Coletas", "Fundação");
}
