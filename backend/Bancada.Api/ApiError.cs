namespace Bancada.Api;

public sealed class ApiError(int status, string? detail = null) : Exception(detail)
{
    public int Status { get; } = status;
    public string? Detail { get; } = detail;

    public static IResult Result(int status, string? detail = null) => Results.Problem(
        statusCode: status, title: Title(status), detail: detail,
        extensions: new Dictionary<string, object?> { ["code"] = Code(status) });

    public static string Code(int status) => status switch
    {
        400 => "validation", 401 => "unauthorized", 403 => "forbidden",
        404 => "notFound", 409 => "conflict", 429 => "rateLimited", _ => "internalError"
    };

    private static string Title(int status) => status switch
    {
        400 => "Dados invalidos.", 401 => "Autenticacao necessaria.", 403 => "Operacao nao permitida.",
        404 => "Recurso indisponivel.", 409 => "A ordem foi alterada ou a operacao nao e permitida.",
        429 => "Limite de requisicoes excedido.", _ => "Erro interno."
    };
}
