using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

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

        if (exception is MenuGoException menuGoEx)
        {
            _logger.LogWarning(menuGoEx, "Application exception (Code {ErrorCode}): {Message}", menuGoEx.Code, menuGoEx.Message);

            problemDetails = new ProblemDetails
            {
                Status = menuGoEx.HttpStatusCode,
                Title = !string.IsNullOrWhiteSpace(menuGoEx.Title) ? menuGoEx.Title : "Lỗi nghiệp vụ",
                Detail = menuGoEx.Message,
                Instance = httpContext.Request.Path
            };
            problemDetails.Extensions["code"] = menuGoEx.Code;
            problemDetails.Extensions["errorCode"] = menuGoEx.Code;
            problemDetails.Extensions["message"] = menuGoEx.Message;
        }
        else if (exception is ArgumentException || exception is ArgumentNullException)
        {
            _logger.LogWarning(exception, "Validation exception: {Message}", exception.Message);

            problemDetails = new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Dữ liệu không hợp lệ",
                Detail = exception.Message,
                Instance = httpContext.Request.Path
            };
            problemDetails.Extensions["code"] = StatusCodes.Status400BadRequest;
            problemDetails.Extensions["errorCode"] = StatusCodes.Status400BadRequest;
            problemDetails.Extensions["message"] = exception.Message;
        }
        else if (exception is InvalidOperationException)
        {
            _logger.LogWarning(exception, "Invalid operation exception: {Message}", exception.Message);

            problemDetails = new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Thao tác không hợp lệ",
                Detail = exception.Message,
                Instance = httpContext.Request.Path
            };
            problemDetails.Extensions["code"] = StatusCodes.Status400BadRequest;
            problemDetails.Extensions["errorCode"] = StatusCodes.Status400BadRequest;
            problemDetails.Extensions["message"] = exception.Message;
        }
        else if (exception is KeyNotFoundException)
        {
            _logger.LogWarning(exception, "Resource not found exception: {Message}", exception.Message);

            problemDetails = new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Không tìm thấy dữ liệu",
                Detail = exception.Message,
                Instance = httpContext.Request.Path
            };
            problemDetails.Extensions["code"] = StatusCodes.Status404NotFound;
            problemDetails.Extensions["errorCode"] = StatusCodes.Status404NotFound;
            problemDetails.Extensions["message"] = exception.Message;
        }
        else if (exception is UnauthorizedAccessException)
        {
            _logger.LogWarning(exception, "Unauthorized exception: {Message}", exception.Message);

            problemDetails = new ProblemDetails
            {
                Status = StatusCodes.Status403Forbidden,
                Title = "Không có quyền truy cập",
                Detail = exception.Message,
                Instance = httpContext.Request.Path
            };
            problemDetails.Extensions["code"] = StatusCodes.Status403Forbidden;
            problemDetails.Extensions["errorCode"] = StatusCodes.Status403Forbidden;
            problemDetails.Extensions["message"] = exception.Message;
        }
        else
        {
            _logger.LogError(exception, "Unhandled system exception: {Message}", exception.Message);

            problemDetails = new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "Lỗi hệ thống",
                Detail = !string.IsNullOrWhiteSpace(exception.Message) ? exception.Message : "Đã xảy ra lỗi hệ thống. Vui lòng thử lại sau hoặc liên hệ quản trị viên.",
                Instance = httpContext.Request.Path
            };
            problemDetails.Extensions["code"] = 5000;
            problemDetails.Extensions["errorCode"] = 5000;
            problemDetails.Extensions["message"] = !string.IsNullOrWhiteSpace(exception.Message) ? exception.Message : "Đã xảy ra lỗi hệ thống. Vui lòng thử lại sau hoặc liên hệ quản trị viên.";
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
