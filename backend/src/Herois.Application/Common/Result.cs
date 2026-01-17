namespace Herois.Application.Common;

public class Result<T>
{
    public bool Success { get; init; }
    public int StatusCode { get; init; }
    public string Message { get; init; } = string.Empty;
    public T? Data { get; init; }

    public static Result<T> Ok(T data, string? message = null) => new() { Success = true, StatusCode = 200, Data = data, Message = message ?? string.Empty };
    public static Result<T> Created(T data, string? message = null) => new() { Success = true, StatusCode = 201, Data = data, Message = message ?? string.Empty };
    public static Result<T> Fail(int statusCode, string message) => new() { Success = false, StatusCode = statusCode, Message = message };
}
