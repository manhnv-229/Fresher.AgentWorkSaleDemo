namespace Demo.Application.DTOs;

public sealed record KnowledgeExplorerResponse(
    Guid AgentId,
    Guid? SelectedFolderId,
    IReadOnlyList<KnowledgeFolderTreeItem> Tree,
    IReadOnlyList<KnowledgeBreadcrumbItem> Breadcrumb,
    IReadOnlyList<KnowledgeFolderItem> Folders,
    IReadOnlyList<KnowledgeFileItem> Files);

public sealed record KnowledgeSearchResponse(
    Guid AgentId,
    IReadOnlyList<KnowledgeFolderItem> Folders,
    IReadOnlyList<KnowledgeFileItem> Files);

public sealed record KnowledgeFolderTreeItem(
    Guid Id,
    Guid? ParentFolderId,
    string Name,
    string NormalizedName,
    IReadOnlyList<KnowledgeFolderTreeItem> Children);

public sealed record KnowledgeBreadcrumbItem(Guid Id, string Name);

public sealed record KnowledgeFolderItem(
    Guid Id,
    Guid? ParentFolderId,
    string Name,
    string NormalizedName,
    Guid CreatedByUserId,
    string CreatedByUserName,
    DateTime CreatedAt,
    DateTime? ModifiedAt);

public sealed record KnowledgeFileItem(
    Guid Id,
    Guid? FolderId,
    string Name,
    string OriginalName,
    string Extension,
    string ContentType,
    long SizeBytes,
    string Status,
    Guid CreatedByUserId,
    string CreatedByUserName,
    DateTime CreatedAt,
    DateTime? ModifiedAt);

public sealed record KnowledgeFileDetail(
    Guid Id,
    Guid? FolderId,
    string Name,
    string OriginalName,
    string Extension,
    string ContentType,
    long SizeBytes,
    string Status,
    string StorageBucket,
    string StorageObjectKey,
    Guid CreatedByUserId,
    string CreatedByUserName,
    DateTime CreatedAt,
    DateTime? ModifiedAt);

public sealed record KnowledgeSearchFilters(
    string? Name,
    Guid? FolderId,
    Guid? CreatedByUserId,
    DateTime? CreatedFrom,
    DateTime? CreatedTo);

public sealed record CreateKnowledgeFolderCommand(string Name, Guid? ParentFolderId);

public sealed record RenameKnowledgeItemCommand(string Name);

public sealed record MoveKnowledgeItemCommand(Guid? TargetFolderId);

public sealed record KnowledgeUploadContent(
    Stream Content,
    string FileName,
    string ContentType,
    long Length,
    Guid? FolderId);

public sealed record KnowledgeDownloadResult(
    Stream Content,
    string FileName,
    string ContentType,
    long SizeBytes);

public sealed record KnowledgePreviewResult(
    Stream Content,
    string FileName,
    string ContentType,
    long SizeBytes);

public sealed record KnowledgeStorageUploadRequest(
    Stream Content,
    string Bucket,
    string ObjectKey,
    string ContentType,
    long SizeBytes,
    string PayloadSha256);

public sealed record KnowledgeStorageUploadResult(
    string Bucket,
    string ObjectKey,
    string? ETag,
    string? VersionId);

public sealed record KnowledgeStorageDownloadResult(
    Stream Content,
    string ContentType,
    long? SizeBytes);
