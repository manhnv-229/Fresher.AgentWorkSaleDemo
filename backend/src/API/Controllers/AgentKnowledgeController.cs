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
    /// <param name="tenantId">Định danh tenant sở hữu agent.</param>
    /// <param name="agentId">Định danh agent cần truy vấn.</param>
    /// <param name="folderId">Định danh thư mục đang mở, nếu có.</param>
    /// <returns>Trạng thái explorer hoặc lỗi nghiệp vụ.</returns>
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
    /// <param name="tenantId">Định danh tenant sở hữu agent.</param>
    /// <param name="agentId">Định danh agent cần tìm kiếm.</param>
    /// <param name="name">Tên hoặc từ khóa cần tìm.</param>
    /// <param name="folderId">Giới hạn trong thư mục, nếu có.</param>
    /// <param name="createdByUserId">Lọc theo người tạo, nếu có.</param>
    /// <param name="createdFrom">Mốc thời gian tạo bắt đầu, nếu có.</param>
    /// <param name="createdTo">Mốc thời gian tạo kết thúc, nếu có.</param>
    /// <returns>Kết quả tìm kiếm gồm folder và file.</returns>
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
    /// <param name="tenantId">Định danh tenant sở hữu agent.</param>
    /// <param name="agentId">Định danh agent sở hữu thư mục.</param>
    /// <param name="request">Tên và thư mục cha của thư mục mới.</param>
    /// <returns>Thư mục mới hoặc lỗi nghiệp vụ.</returns>
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
    /// <param name="tenantId">Định danh tenant sở hữu agent.</param>
    /// <param name="agentId">Định danh agent sở hữu thư mục.</param>
    /// <param name="folderId">Định danh thư mục cần đổi tên.</param>
    /// <param name="request">Tên mới của thư mục.</param>
    /// <returns>Thư mục sau cập nhật hoặc lỗi nghiệp vụ.</returns>
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
    /// <param name="tenantId">Định danh tenant sở hữu agent.</param>
    /// <param name="agentId">Định danh agent sở hữu thư mục.</param>
    /// <param name="folderId">Định danh thư mục cần di chuyển.</param>
    /// <param name="request">Thư mục đích, có thể là root.</param>
    /// <returns>Thư mục sau di chuyển hoặc lỗi ràng buộc cây.</returns>
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
    /// <param name="tenantId">Định danh tenant sở hữu agent.</param>
    /// <param name="agentId">Định danh agent sở hữu thư mục.</param>
    /// <param name="folderId">Định danh thư mục cần xóa.</param>
    /// <returns>Không có nội dung khi xóa thành công.</returns>
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
    /// <param name="tenantId">Định danh tenant sở hữu agent.</param>
    /// <param name="agentId">Định danh agent sở hữu file.</param>
    /// <param name="request">File upload và thư mục đích tùy chọn.</param>
    /// <returns>Metadata file mới hoặc lỗi upload.</returns>
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
    /// <param name="tenantId">Định danh tenant sở hữu agent.</param>
    /// <param name="agentId">Định danh agent sở hữu file.</param>
    /// <param name="fileId">Định danh file cần xem.</param>
    /// <returns>Chi tiết file hoặc lỗi không tìm thấy.</returns>
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
    /// <param name="tenantId">Định danh tenant sở hữu agent.</param>
    /// <param name="agentId">Định danh agent sở hữu file.</param>
    /// <param name="fileId">Định danh file cần tải.</param>
    /// <returns>Nội dung file hoặc response lỗi.</returns>
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
    /// GET files/{fileId}/preview: lấy nội dung preview cho file tri thức. Hiện backend hỗ trợ chuyển PPTX sang PDF.
    /// <param name="tenantId">Định danh tenant sở hữu agent.</param>
    /// <param name="agentId">Định danh agent sở hữu file.</param>
    /// <param name="fileId">Định danh file cần preview.</param>
    /// <returns>Nội dung preview hoặc response lỗi.</returns>
    /// </summary>
    [HttpGet("files/{fileId:guid}/preview")]
    [HasPermission(PermissionCodes.DocumentView)]
    public async Task<IActionResult> PreviewFile(
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

        var result = await fileService.PreviewFileAsync(tenantId, agentId, fileId, userId.Value, cancellationToken);
        if (!result.Succeeded || result.Value is null)
        {
            return ToErrorResult(result.ErrorCode, result.ErrorMessage);
        }

        return File(result.Value.Content, result.Value.ContentType);
    }

    /// <summary>
    /// PUT files/{fileId}/rename: đổi tên file tri thức.
    /// <param name="tenantId">Định danh tenant sở hữu agent.</param>
    /// <param name="agentId">Định danh agent sở hữu file.</param>
    /// <param name="fileId">Định danh file cần đổi tên.</param>
    /// <param name="request">Tên mới của file.</param>
    /// <returns>File sau cập nhật hoặc lỗi nghiệp vụ.</returns>
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
    /// <param name="tenantId">Định danh tenant sở hữu agent.</param>
    /// <param name="agentId">Định danh agent sở hữu file.</param>
    /// <param name="fileId">Định danh file cần di chuyển.</param>
    /// <param name="request">Thư mục đích, có thể là root.</param>
    /// <returns>File sau di chuyển hoặc lỗi nghiệp vụ.</returns>
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
    /// <param name="tenantId">Định danh tenant sở hữu agent.</param>
    /// <param name="agentId">Định danh agent sở hữu file.</param>
    /// <param name="fileId">Định danh file cần xóa.</param>
    /// <returns>Không có nội dung khi xóa thành công.</returns>
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
    /// <param name="result">Kết quả từ application service.</param>
    /// <param name="success">Hàm tạo response khi thành công.</param>
    /// <returns>HTTP response tương ứng với kết quả nghiệp vụ.</returns>
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
    /// <param name="errorCode">Mã lỗi từ application service.</param>
    /// <param name="message">Thông điệp lỗi tùy chọn.</param>
    /// <returns>ActionResult chứa mã HTTP và nội dung lỗi.</returns>
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
                or KnowledgeErrorCodes.PreviewUnavailable or KnowledgeErrorCodes.PreviewTimedOut
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
