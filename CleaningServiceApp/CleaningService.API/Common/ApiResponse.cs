namespace CleaningService.API.Common;

public interface IApiResponse
{
    bool Success { get; }
}

public sealed class ApiResponse<T> : IApiResponse
{
    public bool Success { get; init; }
    public string? Message { get; init; }
    public T? Data { get; init; }
    public string? ErrorCode { get; init; }
    public IDictionary<string, string[]>? Errors { get; init; }
}

public static class ApiResponse
{
    public static ApiResponse<T> Ok<T>(T data, string? message = null) =>
        new() { Success = true, Message = message, Data = data };

    public static ApiResponse<object?> Message(string message) =>
        new() { Success = true, Message = message, Data = null };

    public static ApiResponse<object?> Fail(string message, string errorCode, IDictionary<string, string[]>? errors = null) =>
        new() { Success = false, Message = message, Data = null, ErrorCode = errorCode, Errors = errors };
}
