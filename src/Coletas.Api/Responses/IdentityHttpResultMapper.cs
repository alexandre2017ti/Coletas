using Coletas.Application.Identity;

namespace Coletas.Api.Responses;

/// <summary>Converte resultados de identidade em respostas HTTP.</summary>
public static class IdentityHttpResultMapper
{
    /// <summary>Preserva os códigos e corpos de resposta dos serviços.</summary>
    public static IResult ToHttpResult<T>(IdentityResult<T> result, int successStatusCode = StatusCodes.Status200OK)
    => result.IsSuccess
        ? Results.Json(result.Value, statusCode: successStatusCode)
        : result.StatusCode switch
        {
            StatusCodes.Status400BadRequest => Results.ValidationProblem(new Dictionary<string, string[]> { ["request"] = [result.Error!] }),
            StatusCodes.Status401Unauthorized => Results.Unauthorized(),
            StatusCodes.Status403Forbidden => Results.Forbid(),
            StatusCodes.Status404NotFound => Results.NotFound(new { error = result.Error }),
            StatusCodes.Status409Conflict => Results.Conflict(new { error = result.Error }),
            _ => Results.Problem(result.Error, statusCode: result.StatusCode)
        };
}
