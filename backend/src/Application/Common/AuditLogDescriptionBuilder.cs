namespace Demo.Application.Common;

public static class AuditLogDescriptionBuilder
{
    public const string EmptyValuePlaceholder = "(empty)";
    public const string SensitiveValuePlaceholder = "[redacted]";

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

    public static string Normalize(string? value)
    {
        var trimmed = value?.Trim();
        return string.IsNullOrWhiteSpace(trimmed) ? EmptyValuePlaceholder : trimmed;
    }

    public static string SensitiveChange(string label) =>
        $"{label}: '{SensitiveValuePlaceholder}' -> '{SensitiveValuePlaceholder}'";
}

public sealed record AuditFieldChange(string Label, string? OldValue, string? NewValue);
