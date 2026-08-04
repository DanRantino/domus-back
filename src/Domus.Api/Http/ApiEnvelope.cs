namespace Domus.Api.Http;

public sealed record ApiError(string Code, string Message);

public sealed record ApiEnvelope<T>(bool Success, T? Data, ApiError? Error)
{
    public static ApiEnvelope<T> Ok(T data) => new(true, data, null);

    public static ApiEnvelope<T> Fail(string code, string message) =>
        new(false, default, new ApiError(code, message));
}
