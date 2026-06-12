namespace Demo.Application.UseCases.Common;

public sealed class ServiceResult<T>
{
    private ServiceResult(bool succeeded, string? errorCode, string? errorMessage, T? value)
    {
        Succeeded = succeeded;
        ErrorCode = errorCode;
        ErrorMessage = errorMessage;
        Value = value;
    }

    public bool Succeeded { get; }
    public string? ErrorCode { get; }
    public string? ErrorMessage { get; }
    public T? Value { get; }

    public static ServiceResult<T> Success(T value) => new(true, null, null, value);

    public static ServiceResult<T> Failure(string errorCode, string errorMessage) =>
        new(false, errorCode, errorMessage, default);
}
