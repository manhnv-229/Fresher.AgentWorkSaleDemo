using Demo.Application.DTOs;

namespace Demo.Application.Interfaces.Service;

/// <summary>
/// Chuyển đổi file tri thức sang định dạng phù hợp cho preview trong trình duyệt.
/// </summary>
public interface IKnowledgePreviewConverter
{
    /// <summary>
    /// Chuyển file trình chiếu PPTX sang PDF để frontend có thể render bằng viewer PDF hiện có.
    /// </summary>
    Task<KnowledgePreviewResult> ConvertPptxToPdfAsync(
        Stream content,
        string fileName,
        CancellationToken cancellationToken);
}
