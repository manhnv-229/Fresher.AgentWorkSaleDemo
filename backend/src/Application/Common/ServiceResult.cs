namespace Demo.Application.Common;

/// <summary>
/// Bao bọc kết quả xử lý application service, gồm trạng thái thành công, lỗi và giá trị trả về.
/// </summary>
public sealed class ServiceResult<T>
{
    private ServiceResult(bool succeeded, string? errorCode, string? errorMessage, T? value)
    {
        Succeeded = succeeded;
        ErrorCode = errorCode;
        ErrorMessage = errorMessage;
        Value = value;
    }

    public bool Succeeded
    {
        get;
    }
    public string? ErrorCode
    {
        get;
    }
    public string? ErrorMessage
    {
        get;
    }
    public T? Value
    {
        get;
    }

    /// <summary>
    /// Tạo kết quả thành công chứa giá trị nghiệp vụ.
    /// <param name="value">Giá trị trả về khi xử lý thành công.</param>
    /// <returns>Service result có trạng thái thành công.</returns>
    /// </summary>
    public static ServiceResult<T> Success(T value) => new(true, null, null, value);

    /// <summary>
    /// Tạo kết quả thất bại chứa mã và thông điệp lỗi.
    /// <param name="errorCode">Mã lỗi dùng để ánh xạ response.</param>
    /// <param name="errorMessage">Thông điệp mô tả lỗi.</param>
    /// <returns>Service result có trạng thái thất bại.</returns>
    /// </summary>
    public static ServiceResult<T> Failure(string errorCode, string errorMessage) =>
        new(false, errorCode, errorMessage, default);
}
