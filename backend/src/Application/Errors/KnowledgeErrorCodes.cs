namespace Demo.Application.Errors;

public static class KnowledgeErrorCodes
{
    public const string AgentNotFound = "knowledge.agent_not_found";
    public const string TenantNotFound = "knowledge.tenant_not_found";
    public const string TenantLocked = "knowledge.tenant_locked";
    public const string FolderNotFound = "knowledge.folder_not_found";
    public const string FileNotFound = "knowledge.file_not_found";
    public const string InvalidName = "knowledge.invalid_name";
    public const string InvalidMove = "knowledge.invalid_move";
    public const string UnsupportedFileType = "knowledge.unsupported_file_type";
    public const string FileTooLarge = "knowledge.file_too_large";
    public const string EmptyFile = "knowledge.empty_file";
    public const string FileOwnerRequired = "knowledge.file_owner_required";
    public const string FolderOwnerRequired = "knowledge.folder_owner_required";
    public const string StorageUnavailable = "knowledge.storage_unavailable";
    public const string StorageUnreachable = "knowledge.storage_unreachable";
    public const string StorageTimedOut = "knowledge.storage_timed_out";
    public const string StorageRejected = "knowledge.storage_rejected";
    public const string ValidationError = "knowledge.validation_error";
}
