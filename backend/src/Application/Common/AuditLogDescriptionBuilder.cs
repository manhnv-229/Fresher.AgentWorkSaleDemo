namespace Demo.Application.Common;

/// <summary>
/// Tạo mô tả audit log cho các thay đổi field và che giấu giá trị nhạy cảm.
/// </summary>
public static class AuditLogDescriptionBuilder
{
    public const string EmptyValuePlaceholder = "(empty)";
    public const string SensitiveValuePlaceholder = "[redacted]";

    /// <summary>
    /// Gom các field thực sự thay đổi thành một mô tả audit log.
    /// <param name="subject">Tên đối tượng được thay đổi.</param>
    /// <param name="changes">Danh sách giá trị cũ và mới cần so sánh.</param>
    /// <returns>Mô tả các thay đổi hoặc thông báo không có field thay đổi.</returns>
    /// </summary>
    public static string FormatChangeSummary(string subject, params AuditFieldChange[] changes)
    {
        var formattedChanges = changes
            .Where(change => !string.Equals(Normalize(change.OldValue), Normalize(change.NewValue), StringComparison.Ordinal))
            .Select(change => $"{change.Label}: '{Normalize(change.OldValue)}' -> '{Normalize(change.NewValue)}'")
            .ToArray();

        if (formattedChanges.Length == 0)
        {
            return $"{subject} was updated with no tracked value changes.";
        }

        return $"{subject} updated {string.Join("; ", formattedChanges)}.";
    }

    /// <summary>
    /// Chuẩn hóa giá trị field trước khi ghi vào mô tả.
    /// <param name="value">Giá trị cần chuẩn hóa.</param>
    /// <returns>Giá trị đã trim hoặc placeholder khi rỗng.</returns>
    /// </summary>
    public static string Normalize(string? value)
    {
        var trimmed = value?.Trim();
        return string.IsNullOrWhiteSpace(trimmed) ? EmptyValuePlaceholder : trimmed;
    }

    /// <summary>
    /// Tạo mô tả thay đổi mà không làm lộ giá trị nhạy cảm.
    /// <param name="label">Tên field nhạy cảm.</param>
    /// <returns>Mô tả thay đổi sử dụng placeholder đã che giấu.</returns>
    /// </summary>
    public static string SensitiveChange(string label) =>
        $"{label}: '{SensitiveValuePlaceholder}' -> '{SensitiveValuePlaceholder}'";
}

public sealed record AuditFieldChange(string Label, string? OldValue, string? NewValue);
