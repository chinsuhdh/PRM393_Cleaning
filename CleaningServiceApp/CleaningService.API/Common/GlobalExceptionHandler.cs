using Cleaning.BLL.Common;
using Microsoft.AspNetCore.Diagnostics;

namespace CleaningService.API.Common;

public sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        if (exception is AppException appException)
        {
            logger.LogWarning(
                appException,
                "Yêu cầu tại {Path} thất bại với {ErrorCode}: {Message}",
                httpContext.Request.Path, appException.Code, appException.Message);

            httpContext.Response.StatusCode = appException.StatusCode;
            await httpContext.Response.WriteAsJsonAsync(
                ApiResponse.Fail(appException.Message, appException.Code), cancellationToken);
            return true;
        }

        logger.LogError(exception, "Lỗi không xử lý được tại {Path}", httpContext.Request.Path);

        httpContext.Response.StatusCode = AppErrors.InternalError.StatusCode;
        await httpContext.Response.WriteAsJsonAsync(
            ApiResponse.Fail(ResponseMessages.InternalError, AppErrors.InternalError.Code), cancellationToken);
        return true;
    }
}
