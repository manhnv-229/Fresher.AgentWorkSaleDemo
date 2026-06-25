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

[ApiController]
[Route("api/tenants/{tenantId:guid}/agents/{agentId:guid}/knowledge")]
public sealed class AgentKnowledgeController(
    IKnowledgeExplorerService explorerService,
    IKnowledgeFolderService folderService,
    IKnowledgeFileService fileService) : ControllerBase
{
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

    [HttpGet("files/search")]
    [HasPermission(PermissionCodes.DocumentView)]
    public async Task<ActionResult<IReadOnlyList<KnowledgeFileItem>>> SearchFiles(
        Guid tenantId,
        Guid agentId,
        [FromQuery] string? name,
        [FromQuery] Guid? folderId,
        [FromQuery] Guid? createdByUserId,
        [FromQuery] DateTime? createdFrom,
        [FromQuery] DateTime? createdTo,
        CancellationToken cancellationToken)
    {
        var result = await explorerService.SearchFilesAsync(
            tenantId,
            agentId,
            new KnowledgeSearchFilters(name, folderId, createdByUserId, createdFrom, createdTo),
            cancellationToken);
        return ToActionResult(result, value => Ok(value));
    }

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

        if (request.File.Length == 0)
        {
            return BadRequest(new ApiErrorResponse(KnowledgeErrorCodes.EmptyFile, "File is empty."));
        }

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

    [HttpGet("files/{fileId:guid}")]
    [HasPermission(PermissionCodes.DocumentView)]
    public async Task<ActionResult<KnowledgeFileDetail>> GetFileDetail(
        Guid tenantId,
        Guid agentId,
        Guid fileId,
        CancellationToken cancellationToken)
    {
        var result = await fileService.GetFileDetailAsync(tenantId, agentId, fileId, cancellationToken);
        return ToActionResult(result, value => Ok(value));
    }

    [HttpGet("files/{fileId:guid}/download")]
    [HasPermission(PermissionCodes.DocumentView)]
    public async Task<IActionResult> DownloadFile(
        Guid tenantId,
        Guid agentId,
        Guid fileId,
        CancellationToken cancellationToken)
    {
        var result = await fileService.DownloadFileAsync(tenantId, agentId, fileId, cancellationToken);
        if (!result.Succeeded || result.Value is null)
        {
            return ToErrorResult(result.ErrorCode, result.ErrorMessage);
        }

        return File(result.Value.Content, result.Value.ContentType, result.Value.FileName);
    }

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

    private ActionResult<T> ToActionResult<T>(ServiceResult<T> result, Func<T, ActionResult<T>> success)
    {
        if (result.Succeeded && result.Value is not null)
        {
            return success(result.Value);
        }

        return ToErrorResult(result.ErrorCode, result.ErrorMessage);
    }

    private ActionResult ToErrorResult(string? errorCode, string? message)
    {
        var code = errorCode ?? KnowledgeErrorCodes.ValidationError;
        var response = new ApiErrorResponse(code, message ?? "Knowledge request failed.");
        return code switch
        {
            KnowledgeErrorCodes.AgentNotFound or KnowledgeErrorCodes.FolderNotFound or KnowledgeErrorCodes.FileNotFound => NotFound(response),
            KnowledgeErrorCodes.StorageUnavailable or KnowledgeErrorCodes.StorageUnreachable or KnowledgeErrorCodes.StorageTimedOut or KnowledgeErrorCodes.StorageRejected
                => StatusCode(StatusCodes.Status503ServiceUnavailable, response),
            _ => BadRequest(response)
        };
    }

    private Guid? CurrentUserId()
    {
        var userIdValue = User.FindFirstValue("userId") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(userIdValue, out var userId) ? userId : null;
    }

    private string? ClientIp() => HttpContext.Connection.RemoteIpAddress?.ToString();
}

public sealed record CreateKnowledgeFolderRequest(string Name, Guid? ParentFolderId);

public sealed record RenameKnowledgeItemRequest(string Name);

public sealed record MoveKnowledgeItemRequest(Guid? TargetFolderId);

public sealed class UploadKnowledgeFileRequest
{
    public IFormFile File { get; init; } = default!;

    public Guid? FolderId { get; init; }
}
