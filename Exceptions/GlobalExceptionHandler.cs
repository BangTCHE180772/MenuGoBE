using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace MenuGoBE.Exceptions;

public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;
    private readonly IProblemDetailsService _problemDetailsService;

    public GlobalExceptionHandler(
        ILogger<GlobalExceptionHandler> logger,
        IProblemDetailsService problemDetailsService)
    {
        _logger = logger;
        _problemDetailsService = problemDetailsService;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        ProblemDetails problemDetails;

        switch (exception)
        {
            case MenuGoException menuGoEx:
                _logger.LogWarning(menuGoEx, "Application exception (Code {ErrorCode}): {Message}", menuGoEx.Code, menuGoEx.Message);

                problemDetails = new ProblemDetails
                {
                    Status = menuGoEx.HttpStatusCode,
                    Title = menuGoEx.Title,
                    Detail = menuGoEx.Message,
                    Instance = httpContext.Request.Path
                };
                problemDetails.Extensions.Add("errorCode", menuGoEx.Code);
                break;

            default:
                _logger.LogError(exception, "Unhandled exception: {Message}", exception.Message);

                problemDetails = new ProblemDetails
                {
                    Status = StatusCodes.Status500InternalServerError,
                    Title = "Internal Server Error",
                    Detail = "An unexpected error occurred.",
                    Instance = httpContext.Request.Path
                };
                problemDetails.Extensions.Add("errorCode", 9999);
                break;
        }

        httpContext.Response.StatusCode = problemDetails.Status ?? StatusCodes.Status500InternalServerError;

        return await _problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problemDetails,
            Exception = exception
        });
    }
}
