namespace Demo.Application.Services;

/// <summary>
/// Helper so khớp tên tri thức theo nhiều mức: exact, contains, và subsequence.
/// Dùng chung cho backend search để giữ rule xếp hạng nhất quán.
/// </summary>
public static class KnowledgeSearchHelper
{
#region Constant

    private const int ExactMatchScore = 3;
    private const int ContainsMatchScore = 2;
    private const int SubsequenceMatchScore = 1;

#endregion

#region Method

    /// <summary>
    /// Normalize text để so khớp search: trim, chuyển chữ hoa, và trả về chuỗi rỗng nếu null.
    /// </summary>
    public static string Normalize(string? value)
    {
        return (value ?? string.Empty).Trim().ToUpperInvariant();
    }

    /// <summary>
    /// Tính điểm khớp tên. Score cao hơn nghĩa là match liên quan hơn.
    /// </summary>
    public static int GetMatchScore(string? normalizedQuery, string? normalizedTarget)
    {
        if (string.IsNullOrWhiteSpace(normalizedQuery) || string.IsNullOrWhiteSpace(normalizedTarget))
        {
            return 0;
        }

        if (string.Equals(normalizedQuery, normalizedTarget, StringComparison.Ordinal))
        {
            return ExactMatchScore;
        }

        if (normalizedTarget.Contains(normalizedQuery, StringComparison.Ordinal))
        {
            return ContainsMatchScore;
        }

        return IsSubsequence(normalizedQuery, normalizedTarget) ? SubsequenceMatchScore : 0;
    }

    /// <summary>
    /// Kiểm tra query có phải là subsequence của target không.
    /// </summary>
    public static bool IsMatch(string? normalizedQuery, string? normalizedTarget)
    {
        return GetMatchScore(normalizedQuery, normalizedTarget) > 0;
    }

    private static bool IsSubsequence(string normalizedQuery, string normalizedTarget)
    {
        var queryIndex = 0;
        for (var targetIndex = 0; targetIndex < normalizedTarget.Length && queryIndex < normalizedQuery.Length; targetIndex++)
        {
            if (normalizedTarget[targetIndex] == normalizedQuery[queryIndex])
            {
                queryIndex++;
            }
        }

        return queryIndex == normalizedQuery.Length;
    }

#endregion
}
