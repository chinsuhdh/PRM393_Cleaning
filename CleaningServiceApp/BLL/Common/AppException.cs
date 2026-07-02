namespace Cleaning.BLL.Common;

public sealed class AppException(AppError error, Exception? innerException = null)
    : Exception(error.Message, innerException)
{
    public string Code { get; } = error.Code;
    public int StatusCode { get; } = error.StatusCode;
}
