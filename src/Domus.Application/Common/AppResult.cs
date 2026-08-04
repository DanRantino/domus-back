namespace Domus.Application.Common;

public sealed class AppResult<T>
{
    private AppResult(
        bool isSuccess,
        T? value,
        AppError? error,
        bool isCreated = false)
    {
        IsSuccess = isSuccess;
        Value = value;
        Error = error;
        IsCreated = isCreated;
    }

    public bool IsSuccess { get; }

    public bool IsCreated { get; }

    public T? Value { get; }

    public AppError? Error { get; }

    public static AppResult<T> Success(T value) =>
        new(
            isSuccess: true,
            value,
            error: null);

    public static AppResult<T> Created(T value) =>
        new(
            isSuccess: true,
            value,
            error: null,
            isCreated: true);

    public static AppResult<T> Failure(
        string code,
        string message) =>
        new(
            isSuccess: false,
            value: default,
            error: new AppError(code, message));

    public AppResult<TOut> Map<TOut>(Func<T, TOut> mapper)
    {
        if (!IsSuccess)
        {
            return AppResult<TOut>.Failure(Error!.Code, Error.Message);
        }

        var mapped = mapper(Value!);
        return IsCreated
            ? AppResult<TOut>.Created(mapped)
            : AppResult<TOut>.Success(mapped);
    }
}