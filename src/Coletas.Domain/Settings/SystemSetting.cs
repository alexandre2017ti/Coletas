namespace Coletas.Domain.Settings;
/// <summary>Configuração operacional persistida.</summary>
public sealed class SystemSetting
{
    /// <summary>Chave estável.</summary>
    public required string Key { get; init; }
    /// <summary>Valor interpretado pelo caso de uso.</summary>
    public required string Value { get; init; }
}
