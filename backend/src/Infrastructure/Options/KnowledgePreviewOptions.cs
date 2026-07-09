namespace Demo.Infrastructure.Options;

/// <summary>
/// Cấu hình cho luồng chuyển đổi file preview ở backend.
/// </summary>
public sealed class KnowledgePreviewOptions
{
    /// <summary>
    /// Đường dẫn executable dùng để chuyển đổi tài liệu Office, mặc định là soffice.
    /// </summary>
    public string OfficeCommandPath
    {
        get; set;
    } = "soffice";

    /// <summary>
    /// Timeout tối đa cho một lần chuyển đổi preview, tính bằng giây.
    /// </summary>
    public int ConversionTimeoutSeconds
    {
        get; set;
    } = 30;
}
