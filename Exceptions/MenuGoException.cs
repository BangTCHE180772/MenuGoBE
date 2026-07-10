using System.Net;

namespace MenuGoBE.Exceptions;

public class MenuGoException : Exception
{
    public int Code { get; }
    public int HttpStatusCode { get; }
    public string Title { get; }

    public MenuGoException(ErrorResult errorResult, string? messageOverride = null)
        : base(messageOverride ?? errorResult.Message)
    {
        Code = errorResult.Code;
        HttpStatusCode = errorResult.HttpStatusCode;
        Title = GetTitleForStatusCode(errorResult.HttpStatusCode);
    }

    private static string GetTitleForStatusCode(int statusCode)
    {
        return statusCode switch
        {
            400 => "Bad Request",
            401 => "Unauthorized",
            403 => "Forbidden",
            404 => "Not Found",
            409 => "Conflict",
            422 => "Unprocessable Entity",
            500 => "Internal Server Error",
            _ => Enum.IsDefined(typeof(HttpStatusCode), statusCode)
                ? ((HttpStatusCode)statusCode).ToString()
                : "Error"
        };
    }
}
