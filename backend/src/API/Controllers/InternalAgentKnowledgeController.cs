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
/// API surface cho tri thức của internal agent. Dùng chung application services với tenant agent nhưng truyền internal scope.
/// </summary>
[ApiController]
[Route("api/admin/agents/internal/{agentId:guid}/knowledge")]
public sealed class InternalAgentKnowledgeController(
    IKnowledgeExplorerService explorerService,
    IKnowledgeFolderService folderService,
    IKnowledgeFileService fileService) : ControllerBase
{
#region Method

    /// <summary>
    /// Tải explorer tri thức của internal agent, gồm cây thư mục và nội dung thư mục hiện tại.
    /// <param name="agentId">Định danh internal agent cần truy vấn.</param>
    /// <param name="folderId">Định danh thư mục đang mở, nếu có.</param>
    /// <returns>Trạng thái explorer hoặc lỗi nghiệp vụ.</returns>
    /// </summary>
    [HttpGet("explorer")]
    [HasPermission(PermissionCodes.DocumentView)]
    public async Task<ActionResult<KnowledgeExplorerResponse>> GetExplorer(
        Guid agentId,
        [FromQuery] Guid? folderId,
        CancellationToken cancellationToken)
    {
        var result = await explorerService.GetExplorerAsync(Guid.Empty, agentId, folderId, cancellationToken);
        return ToActionResult(result, value => Ok(value));
    }

    /// <summary>
    /// Tìm kiếm thư mục và file tri thức trong internal agent.
    /// <param name="agentId">Định danh internal agent cần tìm kiếm.</param>
    /// <param name="name">Tên hoặc từ khóa cần tìm.</param>
    /// <param name="folderId">Giới hạn kết quả trong thư mục, nếu có.</param>
    /// <param name="createdByUserId">Lọc theo người tạo, nếu có.</param>
    /// <param name="createdFrom">Mốc thời gian tạo bắt đầu, nếu có.</param>
    /// <param name="createdTo">Mốc thời gian tạo kết thúc, nếu có.</param>
    /// <returns>Kết quả tìm kiếm gồm folder và file.</returns>
    /// </summary>
    [HttpGet("search")]
    [HasPermission(PermissionCodes.DocumentView)]
    public async Task<ActionResult<KnowledgeSearchResponse>> Search(
        Guid agentId,
        [FromQuery] string? name,
        [FromQuery] Guid? folderId,
        [FromQuery] Guid? createdByUserId,
        [FromQuery] DateTime? createdFrom,
        [FromQuery] DateTime? createdTo,
        CancellationToken cancellationToken)
    {
        var result = await explorerService.SearchAsync(
            Guid.Empty,
            agentId,
            new KnowledgeSearchFilters(name, folderId, createdByUserId, createdFrom, createdTo),
            cancellationToken);
        return ToActionResult(result, value => Ok(value));
    }

    /// <summary>
    /// Tạo thư mục tri thức trong internal agent.
    /// <param name="agentId">Định danh internal agent sở hữu thư mục.</param>
    /// <param name="request">Tên và thư mục cha của thư mục mới.</param>
    /// <returns>Thư mục mới hoặc lỗi quyền/nghiệp vụ.</returns>
    /// </summary>
    [HttpPost("folders")]
    [HasPermission(PermissionCodes.DocumentCreate)]
    public async Task<ActionResult<KnowledgeFolderItem>> CreateFolder(
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
            Guid.Empty,
            agentId,
            userId.Value,
            ClientIp(),
            new CreateKnowledgeFolderCommand(request.Name, request.ParentFolderId),
            cancellationToken);
        return ToActionResult(result, value => Ok(value));
    }

    /// <summary>
    /// Đổi tên thư mục tri thức của internal agent.
    /// <param name="agentId">Định danh internal agent sở hữu thư mục.</param>
    /// <param name="folderId">Định danh thư mục cần đổi tên.</param>
    /// <param name="request">Tên mới của thư mục.</param>
    /// <returns>Thư mục sau cập nhật hoặc lỗi nghiệp vụ.</returns>
    /// </summary>
    [HttpPut("folders/{folderId:guid}/rename")]
    [HasPermission(PermissionCodes.DocumentUpdate)]
    public async Task<ActionResult<KnowledgeFolderItem>> RenameFolder(
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
            Guid.Empty,
            agentId,
            folderId,
            userId.Value,
            ClientIp(),
            new RenameKnowledgeItemCommand(request.Name),
            cancellationToken);
        return ToActionResult(result, value => Ok(value));
    }

    /// <summary>
    /// Di chuyển thư mục tri thức trong cây thư mục của internal agent.
    /// <param name="agentId">Định danh internal agent sở hữu thư mục.</param>
    /// <param name="folderId">Định danh thư mục cần di chuyển.</param>
    /// <param name="request">Thư mục đích, có thể là root.</param>
    /// <returns>Thư mục sau di chuyển hoặc lỗi ràng buộc cây.</returns>
    /// </summary>
    [HttpPut("folders/{folderId:guid}/move")]
    [HasPermission(PermissionCodes.DocumentUpdate)]
    public async Task<ActionResult<KnowledgeFolderItem>> MoveFolder(
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
            Guid.Empty,
            agentId,
            folderId,
            userId.Value,
            ClientIp(),
            new MoveKnowledgeItemCommand(request.TargetFolderId),
            cancellationToken);
        return ToActionResult(result, value => Ok(value));
    }

    /// <summary>
    /// Xóa mềm thư mục và toàn bộ subtree của internal agent.
    /// <param name="agentId">Định danh internal agent sở hữu thư mục.</param>
    /// <param name="folderId">Định danh thư mục cần xóa.</param>
    /// <returns>Không có nội dung khi xóa thành công.</returns>
    /// </summary>
    [HttpDelete("folders/{folderId:guid}")]
    [HasPermission(PermissionCodes.DocumentDelete)]
    public async Task<ActionResult> DeleteFolder(
        Guid agentId,
        Guid folderId,
        CancellationToken cancellationToken)
    {
        var userId = CurrentUserId();
        if (userId is null)
        {
            return Unauthorized(new ApiErrorResponse("invalid_token", "Access token does not contain a valid user id."));
        }

        var result = await folderService.DeleteFolderAsync(Guid.Empty, agentId, folderId, userId.Value, ClientIp(), cancellationToken);
        return result.Succeeded ? NoContent() : ToErrorResult(result.ErrorCode, result.ErrorMessage);
    }

    /// <summary>
    /// Upload file tri thức lên storage và lưu metadata cho internal agent.
    /// <param name="agentId">Định danh internal agent sở hữu file.</param>
    /// <param name="request">File upload và thư mục đích tùy chọn.</param>
    /// <returns>Metadata file mới hoặc lỗi upload.</returns>
    /// </summary>
    [HttpPost("files")]
    [HasPermission(PermissionCodes.DocumentCreate)]
    [RequestSizeLimit(50 * 1024 * 1024)]
    public async Task<ActionResult<KnowledgeFileItem>> UploadFile(
        Guid agentId,
        [FromForm] UploadKnowledgeFileRequest request,
        CancellationToken cancellationToken)
    {
        var userId = CurrentUserId();
        if (userId is null)
        {
            return Unauthorized(new ApiErrorResponse("invalid_token", "Access token does not contain a valid user id."));
        }

        await using var stream = request.File.OpenReadStream();
        var result = await fileService.UploadFileAsync(
            Guid.Empty,
            agentId,
            userId.Value,
            ClientIp(),
            new KnowledgeUploadContent(stream, request.File.FileName, request.File.ContentType, request.File.Length, request.FolderId),
            cancellationToken);
        return ToActionResult(result, value => Ok(value));
    }

    /// <summary>
    /// Lấy chi tiết file tri thức của internal agent.
    /// <param name="agentId">Định danh internal agent sở hữu file.</param>
    /// <param name="fileId">Định danh file cần xem.</param>
    /// <returns>Chi tiết file hoặc lỗi không tìm thấy.</returns>
    /// </summary>
    [HttpGet("files/{fileId:guid}")]
    [HasPermission(PermissionCodes.DocumentView)]
    public async Task<ActionResult<KnowledgeFileDetail>> GetFileDetail(
        Guid agentId,
        Guid fileId,
        CancellationToken cancellationToken)
    {
        var userId = CurrentUserId();
        if (userId is null)
        {
            return Unauthorized(new ApiErrorResponse("invalid_token", "Access token does not contain a valid user id."));
        }

        var result = await fileService.GetFileDetailAsync(Guid.Empty, agentId, fileId, userId.Value, cancellationToken);
        return ToActionResult(result, value => Ok(value));
    }

    /// <summary>
    /// Tải nội dung file tri thức của internal agent về client.
    /// <param name="agentId">Định danh internal agent sở hữu file.</param>
    /// <param name="fileId">Định danh file cần tải.</param>
    /// <returns>Nội dung file hoặc response lỗi.</returns>
    /// </summary>
    [HttpGet("files/{fileId:guid}/download")]
    [HasPermission(PermissionCodes.DocumentView)]
    public async Task<IActionResult> DownloadFile(
        Guid agentId,
        Guid fileId,
        CancellationToken cancellationToken)
    {
        var userId = CurrentUserId();
        if (userId is null)
        {
            return Unauthorized(new ApiErrorResponse("invalid_token", "Access token does not contain a valid user id."));
        }

        var result = await fileService.DownloadFileAsync(Guid.Empty, agentId, fileId, userId.Value, cancellationToken);
        if (!result.Succeeded || result.Value is null)
        {
            return ToErrorResult(result.ErrorCode, result.ErrorMessage);
        }

        return File(result.Value.Content, result.Value.ContentType, result.Value.FileName);
    }

    /// <summary>
    /// Tạo nội dung preview cho file tri thức của internal agent.
    /// <param name="agentId">Định danh internal agent sở hữu file.</param>
    /// <param name="fileId">Định danh file cần preview.</param>
    /// <returns>Nội dung preview hoặc response lỗi.</returns>
    /// </summary>
    [HttpGet("files/{fileId:guid}/preview")]
    [HasPermission(PermissionCodes.DocumentView)]
    public async Task<IActionResult> PreviewFile(
        Guid agentId,
        Guid fileId,
        CancellationToken cancellationToken)
    {
        var userId = CurrentUserId();
        if (userId is null)
        {
            return Unauthorized(new ApiErrorResponse("invalid_token", "Access token does not contain a valid user id."));
        }

        var result = await fileService.PreviewFileAsync(Guid.Empty, agentId, fileId, userId.Value, cancellationToken);
        if (!result.Succeeded || result.Value is null)
        {
            return ToErrorResult(result.ErrorCode, result.ErrorMessage);
        }

        return File(result.Value.Content, result.Value.ContentType);
    }

    /// <summary>
    /// Đổi tên file tri thức của internal agent.
    /// <param name="agentId">Định danh internal agent sở hữu file.</param>
    /// <param name="fileId">Định danh file cần đổi tên.</param>
    /// <param name="request">Tên mới của file.</param>
    /// <returns>File sau cập nhật hoặc lỗi nghiệp vụ.</returns>
    /// </summary>
    [HttpPut("files/{fileId:guid}/rename")]
    [HasPermission(PermissionCodes.DocumentUpdate)]
    public async Task<ActionResult<KnowledgeFileItem>> RenameFile(
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
            Guid.Empty,
            agentId,
            fileId,
            userId.Value,
            ClientIp(),
            new RenameKnowledgeItemCommand(request.Name),
            cancellationToken);
        return ToActionResult(result, value => Ok(value));
    }

    /// <summary>
    /// Di chuyển file tri thức trong cây thư mục của internal agent.
    /// <param name="agentId">Định danh internal agent sở hữu file.</param>
    /// <param name="fileId">Định danh file cần di chuyển.</param>
    /// <param name="request">Thư mục đích, có thể là root.</param>
    /// <returns>File sau di chuyển hoặc lỗi nghiệp vụ.</returns>
    /// </summary>
    [HttpPut("files/{fileId:guid}/move")]
    [HasPermission(PermissionCodes.DocumentUpdate)]
    public async Task<ActionResult<KnowledgeFileItem>> MoveFile(
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
            Guid.Empty,
            agentId,
            fileId,
            userId.Value,
            ClientIp(),
            new MoveKnowledgeItemCommand(request.TargetFolderId),
            cancellationToken);
        return ToActionResult(result, value => Ok(value));
    }

    /// <summary>
    /// Xóa mềm metadata file và xóa object vật lý khỏi storage.
    /// <param name="agentId">Định danh internal agent sở hữu file.</param>
    /// <param name="fileId">Định danh file cần xóa.</param>
    /// <returns>Không có nội dung khi xóa thành công.</returns>
    /// </summary>
    [HttpDelete("files/{fileId:guid}")]
    [HasPermission(PermissionCodes.DocumentDelete)]
    public async Task<ActionResult> DeleteFile(
        Guid agentId,
        Guid fileId,
        CancellationToken cancellationToken)
    {
        var userId = CurrentUserId();
        if (userId is null)
        {
            return Unauthorized(new ApiErrorResponse("invalid_token", "Access token does not contain a valid user id."));
        }

        var result = await fileService.DeleteFileAsync(Guid.Empty, agentId, fileId, userId.Value, ClientIp(), cancellationToken);
        return result.Succeeded ? NoContent() : ToErrorResult(result.ErrorCode, result.ErrorMessage);
    }

    /// <summary>
    /// Lấy user ID của người đang gọi request từ access token.
    /// <returns>User ID hợp lệ hoặc null nếu claim không hợp lệ.</returns>
    /// </summary>
    private Guid? CurrentUserId()
    {
        var userIdValue = User.FindFirstValue("userId") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(userIdValue, out var userId) ? userId : null;
    }

    /// <summary>
    /// Lấy địa chỉ IP client để ghi audit log.
    /// <returns>Địa chỉ IP hoặc null nếu không xác định được.</returns>
    /// </summary>
    private string? ClientIp() => HttpContext.Connection.RemoteIpAddress?.ToString();

#endregion

#region Declaration

    /// <summary>
    /// Chuyển ServiceResult thành ActionResult thành công hoặc response lỗi chuẩn.
    /// <param name="result">Kết quả từ application service.</param>
    /// <param name="success">Hàm tạo response khi result thành công.</param>
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
    /// Ánh xạ mã lỗi tri thức thành HTTP status code và error response.
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

#endregion
}
