namespace Coletas.Infrastructure;
/// <summary>Conexões fornecidas pelo ambiente ou user-secrets.</summary>
public sealed class InfrastructureOptions
{
    /// <summary>Seção da configuração.</summary>
    public const string SectionName = "Infrastructure";
    /// <summary>Conexão PostgreSQL.</summary>
    public string Postgres { get; init; } = "";
    /// <summary>Conexão Redis.</summary>
    public string Redis { get; init; } = "";
}
