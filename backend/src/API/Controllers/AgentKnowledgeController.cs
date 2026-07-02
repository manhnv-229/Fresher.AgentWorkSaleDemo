using System.Security.Claims;

using Demo.Api.Authorization;
using Demo.Api.Common;
using Demo.Application.Common;
using Demo.Application.DTOs;
using Demo.Application.Errors;
using Demo.Application.Interfaces.Service;
using Demo.Domain.Authorization;

using Microsoft.AspNetCore.Mvc;

namespace Demo.Api.Controllers;

/// <summary>
/// API surface cho quản lý tri thức agent: explorer, CRUD thư mục, upload/download/search file. Áp dụng permission-based authorization.
/// </summary>
[ApiController]
[Route("api/tenants/{tenantId:guid}/agents/{agentId:guid}/knowledge")]
public sealed class AgentKnowledgeController(
    IKnowledgeExplorerService explorerService,
    IKnowledgeFolderService folderService,
    IKnowledgeFileService fileService) : ControllerBase
{
#region Method

    /// <summary>
    /// GET explorer: tải cây thư mục, breadcrumb, thư mục con, và file trong thư mục được chọn.
    /// </summary>
    [HttpGet("explorer")]
    [HasPermission(PermissionCodes.DocumentView)]
    public async Task<ActionResult<KnowledgeExplorerResponse>> GetExplorer(
        Guid tenantId,
        Guid agentId,
        [FromQuery] Guid? folderId,
        CancellationToken cancellationToken)
    {
        var result = await explorerService.GetExplorerAsync(tenantId, agentId, folderId, cancellationToken);
        return ToActionResult(result, value => Ok(value));
    }

    /// <summary>
    /// GET search: tìm kiếm tri thức theo tên, trả cả folder và file theo một contract backend thống nhất.
    /// </summary>
    [HttpGet("search")]
    [HasPermission(PermissionCodes.DocumentView)]
    public async Task<ActionResult<KnowledgeSearchResponse>> Search(
        Guid tenantId,
        Guid agentId,
        [FromQuery] string? name,
        [FromQuery] Guid? folderId,
        [FromQuery] Guid? createdByUserId,
        [FromQuery] DateTime? createdFrom,
        [FromQuery] DateTime? createdTo,
        CancellationToken cancellationToken)
    {
        var result = await explorerService.SearchAsync(
            tenantId,
            agentId,
            new KnowledgeSearchFilters(name, folderId, createdByUserId, createdFrom, createdTo),
            cancellationToken);
        return ToActionResult(result, value => Ok(value));
    }

    /// <summary>
    /// POST folders: tạo mới thư mục tri thức agent.
    /// </summary>
    [HttpPost("folders")]
    [HasPermission(PermissionCodes.DocumentCreate)]
    public async Task<ActionResult<KnowledgeFolderItem>> CreateFolder(
        Guid tenantId,
        Guid agentId,
        CreateKnowledgeFolderRequest request,
        CancellationToken cancellationToken)
    {
        var userId = CurrentUserId();
        if (userId is null)
        {
            return Unauthorized(new ApiErrorResponse("invalid_token", "Access token does not contain a valid user id."));
        }

        var result = await folderService.CreateFolderAsync(
            tenantId,
            agentId,
            userId.Value,
            ClientIp(),
            new CreateKnowledgeFolderCommand(request.Name, request.ParentFolderId),
            cancellationToken);
        return ToActionResult(result, value => Ok(value));
    }

    /// <summary>
    /// PUT folders/{folderId}/rename: đổi tên thư mục tri thức.
    /// </summary>
    [HttpPut("folders/{folderId:guid}/rename")]
    [HasPermission(PermissionCodes.DocumentUpdate)]
    public async Task<ActionResult<KnowledgeFolderItem>> RenameFolder(
        Guid tenantId,
        Guid agentId,
        Guid folderId,
        RenameKnowledgeItemRequest request,
        CancellationToken cancellationToken)
    {
        var userId = CurrentUserId();
        if (userId is null)
        {
            return Unauthorized(new ApiErrorResponse("invalid_token", "Access token does not contain a valid user id."));
        }

        var result = await folderService.RenameFolderAsync(
            tenantId,
            agentId,
            folderId,
            userId.Value,
            ClientIp(),
            new RenameKnowledgeItemCommand(request.Name),
            cancellationToken);
        return ToActionResult(result, value => Ok(value));
    }

    /// <summary>
    /// PUT folders/{folderId}/move: di chuyển thư mục đến thư mục đích.
    /// </summary>
    [HttpPut("folders/{folderId:guid}/move")]
    [HasPermission(PermissionCodes.DocumentUpdate)]
    public async Task<ActionResult<KnowledgeFolderItem>> MoveFolder(
        Guid tenantId,
        Guid agentId,
        Guid folderId,
        MoveKnowledgeItemRequest request,
        CancellationToken cancellationToken)
    {
        var userId = CurrentUserId();
        if (userId is null)
        {
            return Unauthorized(new ApiErrorResponse("invalid_token", "Access token does not contain a valid user id."));
        }

        var result = await folderService.MoveFolderAsync(
            tenantId,
            agentId,
            folderId,
            userId.Value,
            ClientIp(),
            new MoveKnowledgeItemCommand(request.TargetFolderId),
            cancellationToken);
        return ToActionResult(result, value => Ok(value));
    }

    /// <summary>
    /// DELETE folders/{folderId}: xóa mềm toàn bộ subtree thư mục.
    /// </summary>
    [HttpDelete("folders/{folderId:guid}")]
    [HasPermission(PermissionCodes.DocumentDelete)]
    public async Task<ActionResult> DeleteFolder(
        Guid tenantId,
        Guid agentId,
        Guid folderId,
        CancellationToken cancellationToken)
    {
        var userId = CurrentUserId();
        if (userId is null)
        {
            return Unauthorized(new ApiErrorResponse("invalid_token", "Access token does not contain a valid user id."));
        }

        var result = await folderService.DeleteFolderAsync(tenantId, agentId, folderId, userId.Value, ClientIp(), cancellationToken);
        return result.Succeeded ? NoContent() : ToErrorResult(result.ErrorCode, result.ErrorMessage);
    }

    /// <summary>
    /// POST files: upload file tri thức lên MinIO và lưu metadata vào database.
    /// </summary>
    [HttpPost("files")]
    [HasPermission(PermissionCodes.DocumentCreate)]
    [RequestSizeLimit(50 * 1024 * 1024)]
    public async Task<ActionResult<KnowledgeFileItem>> UploadFile(
        Guid tenantId,
        Guid agentId,
        [FromForm] UploadKnowledgeFileRequest request,
        CancellationToken cancellationToken)
    {
        var userId = CurrentUserId();
        if (userId is null)
        {
            return Unauthorized(new ApiErrorResponse("invalid_token", "Access token does not contain a valid user id."));
        }

        // Buffer stream trước khi truyền vào service để đảm bảo stream có thể đọc được trong toàn bộ quá trình upload
        await using var stream = request.File.OpenReadStream();
        var result = await fileService.UploadFileAsync(
            tenantId,
            agentId,
            userId.Value,
            ClientIp(),
            new KnowledgeUploadContent(stream, request.File.FileName, request.File.ContentType, request.File.Length, request.FolderId),
            cancellationToken);
        return ToActionResult(result, value => Ok(value));
    }

    /// <summary>
    /// GET files/{fileId}: lấy thông tin chi tiết file tri thức bao gồm metadata storage.
    /// </summary>
    [HttpGet("files/{fileId:guid}")]
    [HasPermission(PermissionCodes.DocumentView)]
    public async Task<ActionResult<KnowledgeFileDetail>> GetFileDetail(
        Guid tenantId,
        Guid agentId,
        Guid fileId,
        CancellationToken cancellationToken)
    {
        var userId = CurrentUserId();
        if (userId is null)
        {
            return Unauthorized(new ApiErrorResponse("invalid_token", "Access token does not contain a valid user id."));
        }

        var result = await fileService.GetFileDetailAsync(tenantId, agentId, fileId, userId.Value, cancellationToken);
        return ToActionResult(result, value => Ok(value));
    }

    /// <summary>
    /// GET files/{fileId}/download: tải file tri thức từ MinIO về.
    /// </summary>
    [HttpGet("files/{fileId:guid}/download")]
    [HasPermission(PermissionCodes.DocumentView)]
    public async Task<IActionResult> DownloadFile(
        Guid tenantId,
        Guid agentId,
        Guid fileId,
        CancellationToken cancellationToken)
    {
        var userId = CurrentUserId();
        if (userId is null)
        {
            return Unauthorized(new ApiErrorResponse("invalid_token", "Access token does not contain a valid user id."));
        }

        var result = await fileService.DownloadFileAsync(tenantId, agentId, fileId, userId.Value, cancellationToken);
        if (!result.Succeeded || result.Value is null)
        {
            return ToErrorResult(result.ErrorCode, result.ErrorMessage);
        }

        return File(result.Value.Content, result.Value.ContentType, result.Value.FileName);
    }

    /// <summary>
    /// PUT files/{fileId}/rename: đổi tên file tri thức.
    /// </summary>
    [HttpPut("files/{fileId:guid}/rename")]
    [HasPermission(PermissionCodes.DocumentUpdate)]
    public async Task<ActionResult<KnowledgeFileItem>> RenameFile(
        Guid tenantId,
        Guid agentId,
        Guid fileId,
        RenameKnowledgeItemRequest request,
        CancellationToken cancellationToken)
    {
        var userId = CurrentUserId();
        if (userId is null)
        {
            return Unauthorized(new ApiErrorResponse("invalid_token", "Access token does not contain a valid user id."));
        }

        var result = await fileService.RenameFileAsync(
            tenantId,
            agentId,
            fileId,
            userId.Value,
            ClientIp(),
            new RenameKnowledgeItemCommand(request.Name),
            cancellationToken);
        return ToActionResult(result, value => Ok(value));
    }

    /// <summary>
    /// PUT files/{fileId}/move: di chuyển file đến thư mục đích.
    /// </summary>
    [HttpPut("files/{fileId:guid}/move")]
    [HasPermission(PermissionCodes.DocumentUpdate)]
    public async Task<ActionResult<KnowledgeFileItem>> MoveFile(
        Guid tenantId,
        Guid agentId,
        Guid fileId,
        MoveKnowledgeItemRequest request,
        CancellationToken cancellationToken)
    {
        var userId = CurrentUserId();
        if (userId is null)
        {
            return Unauthorized(new ApiErrorResponse("invalid_token", "Access token does not contain a valid user id."));
        }

        var result = await fileService.MoveFileAsync(
            tenantId,
            agentId,
            fileId,
            userId.Value,
            ClientIp(),
            new MoveKnowledgeItemCommand(request.TargetFolderId),
            cancellationToken);
        return ToActionResult(result, value => Ok(value));
    }

    /// <summary>
    /// DELETE files/{fileId}: xóa mềm file tri thức và xóa vật lý object khỏi MinIO.
    /// </summary>
    [HttpDelete("files/{fileId:guid}")]
    [HasPermission(PermissionCodes.DocumentDelete)]
    public async Task<ActionResult> DeleteFile(
        Guid tenantId,
        Guid agentId,
        Guid fileId,
        CancellationToken cancellationToken)
    {
        var userId = CurrentUserId();
        if (userId is null)
        {
            return Unauthorized(new ApiErrorResponse("invalid_token", "Access token does not contain a valid user id."));
        }

        var result = await fileService.DeleteFileAsync(tenantId, agentId, fileId, userId.Value, ClientIp(), cancellationToken);
        return result.Succeeded ? NoContent() : ToErrorResult(result.ErrorCode, result.ErrorMessage);
    }

#endregion

#region Declaration

    /// <summary>
    /// Chuyển ServiceResult sang ActionResult. Nếu thành công, gọi success function; nếu không, trả về error response.
    /// </summary>
    private ActionResult<T> ToActionResult<T>(ServiceResult<T> result, Func<T, ActionResult<T>> success)
    {
        if (result.Succeeded && result.Value is not null)
        {
            return success(result.Value);
        }

        return ToErrorResult(result.ErrorCode, result.ErrorMessage);
    }

    /// <summary>
    /// Tạo error response từ error code và message. Áp dụng mapping error code sang HTTP status code tương ứng.
    /// </summary>
    private ActionResult ToErrorResult(string? errorCode, string? message)
    {
        var code = errorCode ?? KnowledgeErrorCodes.ValidationError;
        var response = new ApiErrorResponse(code, message ?? "Knowledge request failed.");
        return code switch
        {
            KnowledgeErrorCodes.AgentNotFound or KnowledgeErrorCodes.FolderNotFound or KnowledgeErrorCodes.FileNotFound => NotFound(response),
            KnowledgeErrorCodes.FileOwnerRequired or KnowledgeErrorCodes.FolderOwnerRequired => StatusCode(StatusCodes.Status403Forbidden, response),
            KnowledgeErrorCodes.StorageUnavailable or KnowledgeErrorCodes.StorageUnreachable or KnowledgeErrorCodes.StorageTimedOut or KnowledgeErrorCodes.StorageRejected
                => StatusCode(StatusCodes.Status503ServiceUnavailable, response),
            _ => BadRequest(response)
        };
    }

    /// <summary>
    /// Lấy user ID từ claims trong access token. Hỗ trợ cả custom claim "userId" và ClaimTypes.NameIdentifier.
    /// </summary>
    private Guid? CurrentUserId()
    {
        var userIdValue = User.FindFirstValue("userId") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(userIdValue, out var userId) ? userId : null;
    }

    /// <summary>
    /// Lấy IP address của client request để ghi audit log.
    /// </summary>
    private string? ClientIp() => HttpContext.Connection.RemoteIpAddress?.ToString();

#endregion
}

/// <summary>
/// Request body cho tạo mới thư mục tri thức.
/// </summary>
public sealed record CreateKnowledgeFolderRequest(string Name, Guid? ParentFolderId);

/// <summary>
/// Request body cho đổi tên item tri thức (thư mục hoặc file).
/// </summary>
public sealed record RenameKnowledgeItemRequest(string Name);

/// <summary>
/// Request body cho di chuyển item tri thức đến thư mục đích.
/// </summary>
public sealed record MoveKnowledgeItemRequest(Guid? TargetFolderId);

/// <summary>
/// Request body cho upload file tri thức. Hỗ trợ multipart form data với file và folderId tùy chọn.
/// </summary>
public sealed class UploadKnowledgeFileRequest
{
    public IFormFile File { get; init; } = default!;

    public Guid? FolderId { get; init; }
}
