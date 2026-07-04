using Cleaning.BLL.Common;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace CleaningService.API.Common;

public sealed class ApiResponseWrapperFilter : IAsyncResultFilter
{
    public async Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next)
    {
        switch (context.Result)
        {
            case ObjectResult { Value: IApiResponse }:
                break;

            case ObjectResult objectResult:
                var status = objectResult.StatusCode ?? StatusCodes.Status200OK;
                objectResult.DeclaredType = null;
                objectResult.Value = status is >= 200 and < 300
                    ? ApiResponse.Ok(objectResult.Value)
                    : ApiResponse.Fail(DefaultMessage(status), DefaultCode(status));
                break;

            case StatusCodeResult statusCodeResult when statusCodeResult.StatusCode >= 400:
                context.Result = new ObjectResult(
                    ApiResponse.Fail(DefaultMessage(statusCodeResult.StatusCode), DefaultCode(statusCodeResult.StatusCode)))
                {
                    StatusCode = statusCodeResult.StatusCode
                };
                break;
        }

        await next();
    }

    private static string DefaultCode(int status) => status switch
    {
        400 => AppErrors.ValidationError.Code,
        401 => AppErrors.Unauthorized.Code,
        403 => AppErrors.Forbidden.Code,
        404 => AppErrors.NotFound.Code,
        _ => AppErrors.InternalError.Code,
    };

    private static string DefaultMessage(int status) => status switch
    {
        400 => AppErrors.ValidationError.Message,
        401 => AppErrors.Unauthorized.Message,
        403 => AppErrors.Forbidden.Message,
        404 => AppErrors.NotFound.Message,
        _ => AppErrors.InternalError.Message,
    };
}
