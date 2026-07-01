using Demo.Application.Common;
using Demo.Application.DTOs;
using Demo.Application.Errors;
using Demo.Application.Interfaces.Repository;
using Demo.Domain.Entities;
using Demo.Domain.Enums;
using Demo.Domain.Interfaces.Repository;
using Demo.Domain.Interfaces.Service;
using AutoMapper;
using Microsoft.Extensions.Logging;

namespace Demo.Application.Services;

public sealed class AgentCatalogService(
    IAgentQueryRepository agentQueryRepository,
    IAgentRepository agentRepository,
    ITenantRepository tenantRepository,
    IAuditLogService auditLogService,
    IAuthUserRepository authUserRepository,
    IDistributedCacheService distributedCacheService,
    ICacheVersionService cacheVersionService,
    IApplicationCachePolicyProvider cachePolicyProvider,
    IMapper mapper,
    ILogger<AgentCatalogService> logger,
    IUnitOfWork unitOfWork) : IAgentCatalogService
{
    #region Method

    /// <summary>
    /// Lấy danh sách agent nội bộ theo bộ lọc và phân trang.
    /// </summary>
    public async Task<ServiceResult<PagedResult<AgentListItem>>> GetInternalAgentsPagedAsync(
        AgentListFilters filters,
        CancellationToken cancellationToken)
    {
        if (!TryCreateQueryFilters(filters, out var queryFilters))
        {
            return ServiceResult<PagedResult<AgentListItem>>.Failure(
                AgentErrorCodes.ValidationError,
                "Status filter is invalid.");
        }

        if (await TryBuildAgentListCacheKeyAsync(null, queryFilters, cancellationToken) is { } cacheKey)
        {
            var cachedResult = await TryGetCachedValueAsync<PagedResult<AgentListItem>>(cacheKey, "internal agent list", cancellationToken);
            if (cachedResult is not null)
            {
                return ServiceResult<PagedResult<AgentListItem>>.Success(cachedResult);
            }
        }

        var pagedResult = await agentQueryRepository.GetInternalAgentsPagedAsync(queryFilters, cancellationToken);
        var items = pagedResult.Items.Select(agent => mapper.Map<AgentListItem>(agent)).ToList();
        var result = new PagedResult<AgentListItem>(
            items,
            pagedResult.Page,
            pagedResult.PageSize,
            pagedResult.TotalCount,
            pagedResult.TotalPages);

        if (await TryBuildAgentListCacheKeyAsync(null, queryFilters, cancellationToken) is { } internalListCacheKey)
        {
            await TrySetCachedValueAsync(internalListCacheKey, result, cachePolicyProvider.AgentListTimeToLive, "internal agent list", cancellationToken);
        }

        return ServiceResult<PagedResult<AgentListItem>>.Success(result);
    }

    /// <summary>
    /// Lấy danh sách agent thuộc tenant theo bộ lọc và phân trang.
    /// </summary>
    public async Task<ServiceResult<PagedResult<AgentListItem>>> GetTenantAgentsPagedAsync(
        Guid tenantId,
        AgentListFilters filters,
        CancellationToken cancellationToken)
    {
        if (!TryCreateQueryFilters(filters, out var queryFilters))
        {
            return ServiceResult<PagedResult<AgentListItem>>.Failure(
                AgentErrorCodes.ValidationError,
                "Status filter is invalid.");
        }

        if (await TryBuildAgentListCacheKeyAsync(tenantId, queryFilters, cancellationToken) is { } cacheKey)
        {
            var cachedResult = await TryGetCachedValueAsync<PagedResult<AgentListItem>>(cacheKey, "tenant agent list", cancellationToken);
            if (cachedResult is not null)
            {
                return ServiceResult<PagedResult<AgentListItem>>.Success(cachedResult);
            }
        }

        var pagedResult = await agentQueryRepository.GetTenantAgentsPagedAsync(tenantId, queryFilters, cancellationToken);
        var items = pagedResult.Items.Select(agent => mapper.Map<AgentListItem>(agent)).ToList();
        var result = new PagedResult<AgentListItem>(
            items,
            pagedResult.Page,
            pagedResult.PageSize,
            pagedResult.TotalCount,
            pagedResult.TotalPages);

        if (await TryBuildAgentListCacheKeyAsync(tenantId, queryFilters, cancellationToken) is { } tenantListCacheKey)
        {
            await TrySetCachedValueAsync(tenantListCacheKey, result, cachePolicyProvider.AgentListTimeToLive, "tenant agent list", cancellationToken);
        }

        return ServiceResult<PagedResult<AgentListItem>>.Success(result);
    }

    /// <summary>
    /// Lấy thông tin chi tiết của agent nội bộ.
    /// </summary>
    public async Task<ServiceResult<AgentDetailItem>> GetInternalAgentDetailAsync(
        Guid agentId,
        CancellationToken cancellationToken)
    {
        if (await TryBuildAgentDetailCacheKeyAsync(null, agentId, cancellationToken) is { } cacheKey)
        {
            var cachedAgent = await TryGetCachedValueAsync<AgentDetailItem>(cacheKey, "internal agent detail", cancellationToken);
            if (cachedAgent is not null)
            {
                return ServiceResult<AgentDetailItem>.Success(cachedAgent);
            }
        }

        var agent = await agentQueryRepository.GetInternalAgentDetailByIdAsync(agentId, cancellationToken);
        if (agent is null)
        {
            return ServiceResult<AgentDetailItem>.Failure(
                AgentErrorCodes.AgentNotFound,
                "Agent was not found.");
        }

        var result = mapper.Map<AgentDetailItem>(agent);
        if (await TryBuildAgentDetailCacheKeyAsync(null, agentId, cancellationToken) is { } detailCacheKey)
        {
            await TrySetCachedValueAsync(detailCacheKey, result, cachePolicyProvider.AgentDetailTimeToLive, "internal agent detail", cancellationToken);
        }

        return ServiceResult<AgentDetailItem>.Success(result);
    }

    /// <summary>
    /// Lấy thông tin chi tiết của agent thuộc tenant.
    /// </summary>
    public async Task<ServiceResult<AgentDetailItem>> GetTenantAgentDetailAsync(
        Guid tenantId,
        Guid agentId,
        CancellationToken cancellationToken)
    {
        if (await TryBuildAgentDetailCacheKeyAsync(tenantId, agentId, cancellationToken) is { } cacheKey)
        {
            var cachedAgent = await TryGetCachedValueAsync<AgentDetailItem>(cacheKey, "tenant agent detail", cancellationToken);
            if (cachedAgent is not null)
            {
                return ServiceResult<AgentDetailItem>.Success(cachedAgent);
            }
        }

        var agent = await agentQueryRepository.GetTenantAgentDetailByIdAsync(tenantId, agentId, cancellationToken);
        if (agent is null)
        {
            return ServiceResult<AgentDetailItem>.Failure(
                AgentErrorCodes.AgentNotFound,
                "Agent was not found.");
        }

        var result = mapper.Map<AgentDetailItem>(agent);
        if (await TryBuildAgentDetailCacheKeyAsync(tenantId, agentId, cancellationToken) is { } detailCacheKey)
        {
            await TrySetCachedValueAsync(detailCacheKey, result, cachePolicyProvider.AgentDetailTimeToLive, "tenant agent detail", cancellationToken);
        }

        return ServiceResult<AgentDetailItem>.Success(result);
    }

    /// <summary>
    /// Tạo mới agent nội bộ và ghi nhận audit log tương ứng.
    /// </summary>
    public async Task<ServiceResult<AgentListItem>> CreateInternalAgentAsync(
        Guid createdByUserId,
        string? ipAddress,
        CreateAgentCommand command,
        CancellationToken cancellationToken)
    {
        var validation = ValidateCreateCommand(command);
        if (validation is not null)
        {
            return validation;
        }

        var agent = CreateAgent(command, tenantId: null, AgentScope.Internal, createdByUserId);
        agentRepository.Add(agent);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await InvalidateAgentReadCacheAsync(null, agent.Id, cancellationToken);

        var userName = await GetUserNameAsync(createdByUserId, cancellationToken);
        await auditLogService.RecordAsync(
            "agent.create",
            userName,
            createdByUserId,
            null,
            ipAddress,
            $"Internal agent '{agent.Name}' was created.",
            "Agent",
            agent.Id.ToString(),
            cancellationToken);

        return ServiceResult<AgentListItem>.Success(mapper.Map<AgentListItem>(agent));
    }

    /// <summary>
    /// Tạo mới agent cho tenant khi tenant còn hoạt động.
    /// </summary>
    public async Task<ServiceResult<AgentListItem>> CreateTenantAgentAsync(
        Guid tenantId,
        Guid createdByUserId,
        string? ipAddress,
        CreateAgentCommand command,
        CancellationToken cancellationToken)
    {
        var validation = ValidateCreateCommand(command);
        if (validation is not null)
        {
            return validation;
        }

        var tenant = await tenantRepository.GetByIdAsync(tenantId, cancellationToken);
        if (tenant is null)
        {
            return ServiceResult<AgentListItem>.Failure(
                AgentErrorCodes.TenantNotFound,
                "Tenant was not found.");
        }

        if (tenant.Status == TenantStatus.Locked)
        {
            return ServiceResult<AgentListItem>.Failure(
                AgentErrorCodes.TenantLocked,
                "Cannot create agents in a locked tenant.");
        }

        var agent = CreateAgent(command, tenantId, AgentScope.Tenant, createdByUserId);
        agentRepository.Add(agent);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await InvalidateAgentReadCacheAsync(tenantId, agent.Id, cancellationToken);

        var userName = await GetUserNameAsync(createdByUserId, cancellationToken);
        await auditLogService.RecordAsync(
            "agent.create",
            userName,
            createdByUserId,
            tenantId,
            ipAddress,
            $"Tenant agent '{agent.Name}' was created.",
            "Agent",
            agent.Id.ToString(),
            cancellationToken);

        return ServiceResult<AgentListItem>.Success(mapper.Map<AgentListItem>(agent));
    }

    /// <summary>
    /// Cập nhật agent nội bộ và ghi lại mô tả thay đổi phục vụ audit.
    /// </summary>
    public async Task<ServiceResult<AgentListItem>> UpdateInternalAgentAsync(
        Guid agentId,
        Guid modifiedByUserId,
        string? ipAddress,
        UpdateAgentCommand command,
        CancellationToken cancellationToken)
    {
        var agent = await agentRepository.GetInternalAgentByIdAsync(agentId, cancellationToken);
        if (agent is null)
        {
            return ServiceResult<AgentListItem>.Failure(
                AgentErrorCodes.AgentNotFound,
                "Agent was not found.");
        }

        var validation = ValidateUpdateCommand(command);
        if (validation is not null)
        {
            return validation;
        }

        var auditDescription = BuildAgentUpdateDescription("Internal agent", agent, command);
        UpdateAgentFromCommand(agent, command, modifiedByUserId);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await InvalidateAgentReadCacheAsync(null, agent.Id, cancellationToken);

        var userName = await GetUserNameAsync(modifiedByUserId, cancellationToken);
        await auditLogService.RecordAsync(
            "agent.update",
            userName,
            modifiedByUserId,
            null,
            ipAddress,
            auditDescription,
            "Agent",
            agent.Id.ToString(),
            cancellationToken);

        return ServiceResult<AgentListItem>.Success(mapper.Map<AgentListItem>(agent));
    }

    /// <summary>
    /// Cập nhật agent của tenant sau khi kiểm tra trạng thái tenant.
    /// </summary>
    public async Task<ServiceResult<AgentListItem>> UpdateTenantAgentAsync(
        Guid tenantId,
        Guid agentId,
        Guid modifiedByUserId,
        string? ipAddress,
        UpdateAgentCommand command,
        CancellationToken cancellationToken)
    {
        var agent = await agentRepository.GetTenantAgentByIdAsync(tenantId, agentId, cancellationToken);
        if (agent is null)
        {
            return ServiceResult<AgentListItem>.Failure(
                AgentErrorCodes.AgentNotFound,
                "Agent was not found.");
        }

        var tenant = await tenantRepository.GetByIdAsync(tenantId, cancellationToken);
        if (tenant is null)
        {
            return ServiceResult<AgentListItem>.Failure(
                AgentErrorCodes.TenantNotFound,
                "Tenant was not found.");
        }

        if (tenant.Status == TenantStatus.Locked)
        {
            return ServiceResult<AgentListItem>.Failure(
                AgentErrorCodes.TenantLocked,
                "Cannot update agents in a locked tenant.");
        }

        var validation = ValidateUpdateCommand(command);
        if (validation is not null)
        {
            return validation;
        }

        var auditDescription = BuildAgentUpdateDescription("Tenant agent", agent, command);
        UpdateAgentFromCommand(agent, command, modifiedByUserId);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await InvalidateAgentReadCacheAsync(tenantId, agent.Id, cancellationToken);

        var userName = await GetUserNameAsync(modifiedByUserId, cancellationToken);
        await auditLogService.RecordAsync(
            "agent.update",
            userName,
            modifiedByUserId,
            tenantId,
            ipAddress,
            auditDescription,
            "Agent",
            agent.Id.ToString(),
            cancellationToken);

        return ServiceResult<AgentListItem>.Success(mapper.Map<AgentListItem>(agent));
    }

    /// <summary>
    /// Xóa mềm agent nội bộ.
    /// </summary>
    public async Task<ServiceResult<bool>> DeleteInternalAgentAsync(
        Guid agentId,
        Guid modifiedByUserId,
        string? ipAddress,
        CancellationToken cancellationToken)
    {
        var agent = await agentRepository.GetInternalAgentByIdAsync(agentId, cancellationToken);
        if (agent is null)
        {
            return ServiceResult<bool>.Failure(
                AgentErrorCodes.AgentNotFound,
                "Agent was not found.");
        }

        agent.Status = AgentStatus.Deleted;
        agent.DeletedAt = DateTime.UtcNow;
        agent.ModifiedAt = DateTime.UtcNow;
        agent.ModifiedByUserId = modifiedByUserId;
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await InvalidateAgentReadCacheAsync(null, agent.Id, cancellationToken);

        var userName = await GetUserNameAsync(modifiedByUserId, cancellationToken);
        await auditLogService.RecordAsync(
            "agent.delete",
            userName,
            modifiedByUserId,
            null,
            ipAddress,
            $"Internal agent '{agent.Name}' was deleted.",
            "Agent",
            agent.Id.ToString(),
            cancellationToken);

        return ServiceResult<bool>.Success(true);
    }

    /// <summary>
    /// Xóa mềm agent của tenant sau khi kiểm tra tenant còn cho phép chỉnh sửa.
    /// </summary>
    public async Task<ServiceResult<bool>> DeleteTenantAgentAsync(
        Guid tenantId,
        Guid agentId,
        Guid modifiedByUserId,
        string? ipAddress,
        CancellationToken cancellationToken)
    {
        var agent = await agentRepository.GetTenantAgentByIdAsync(tenantId, agentId, cancellationToken);
        if (agent is null)
        {
            return ServiceResult<bool>.Failure(
                AgentErrorCodes.AgentNotFound,
                "Agent was not found.");
        }

        var tenant = await tenantRepository.GetByIdAsync(tenantId, cancellationToken);
        if (tenant is null)
        {
            return ServiceResult<bool>.Failure(
                AgentErrorCodes.TenantNotFound,
                "Tenant was not found.");
        }

        if (tenant.Status == TenantStatus.Locked)
        {
            return ServiceResult<bool>.Failure(
                AgentErrorCodes.TenantLocked,
                "Cannot delete agents in a locked tenant.");
        }

        agent.Status = AgentStatus.Deleted;
        agent.DeletedAt = DateTime.UtcNow;
        agent.ModifiedAt = DateTime.UtcNow;
        agent.ModifiedByUserId = modifiedByUserId;
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await InvalidateAgentReadCacheAsync(tenantId, agent.Id, cancellationToken);

        var userName = await GetUserNameAsync(modifiedByUserId, cancellationToken);
        await auditLogService.RecordAsync(
            "agent.delete",
            userName,
            modifiedByUserId,
            tenantId,
            ipAddress,
            $"Tenant agent '{agent.Name}' was deleted.",
            "Agent",
            agent.Id.ToString(),
            cancellationToken);

        return ServiceResult<bool>.Success(true);
    }

    /// <summary>
    /// Lấy tên hiển thị của người thao tác để ghi vào audit log.
    /// </summary>
    private async Task<string> GetUserNameAsync(Guid? userId, CancellationToken cancellationToken)
    {
        if (userId is null) return "System";
        var user = await authUserRepository.GetByIdAsync(userId.Value, cancellationToken);
        return user?.FullName ?? user?.Email ?? "Unknown";
    }

    /// <summary>
    /// Tạo mô tả thay đổi để lưu trong audit log khi cập nhật agent.
    /// </summary>
    private static string BuildAgentUpdateDescription(string scopeLabel, Agent agent, UpdateAgentCommand command)
    {
        return AuditLogDescriptionBuilder.FormatChangeSummary(
            $"{scopeLabel} '{agent.Name}'",
            new AuditFieldChange("Name", agent.Name, command.Name),
            new AuditFieldChange("Role", agent.Role, command.Role),
            new AuditFieldChange("Description", agent.Description, command.Description),
            new AuditFieldChange("Icon", agent.Icon, command.Icon),
            new AuditFieldChange("Status", agent.Status.ToString(), command.Status));
    }

    /// <summary>
    /// Khởi tạo entity agent mới với thông tin mặc định cho luồng tạo.
    /// </summary>
    private static Agent CreateAgent(CreateAgentCommand command, Guid? tenantId, AgentScope scope, Guid createdByUserId)
    {
        var id = Guid.NewGuid();
        return new Agent
        {
            Id = id,
            TenantId = tenantId,
            CreatedByUserId = createdByUserId,
            Code = CreateAgentCode(command.Name, id),
            Scope = scope,
            Name = command.Name.Trim(),
            Role = command.Role.Trim(),
            Description = command.Description?.Trim(),
            Icon = command.Icon?.Trim(),
            Status = AgentStatus.Draft,
            CreatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Đồng bộ dữ liệu cập nhật từ command vào entity agent hiện tại.
    /// </summary>
    private static void UpdateAgentFromCommand(Agent agent, UpdateAgentCommand command, Guid modifiedByUserId)
    {
        agent.Name = command.Name.Trim();
        agent.Role = command.Role.Trim();
        agent.Description = command.Description?.Trim();
        agent.Icon = command.Icon?.Trim();
        agent.Status = Enum.Parse<AgentStatus>(command.Status, true);
        agent.ModifiedAt = DateTime.UtcNow;
        agent.ModifiedByUserId = modifiedByUserId;
    }

    /// <summary>
    /// Sinh mã agent ổn định từ tên hiển thị và định danh ngẫu nhiên.
    /// </summary>
    private static string CreateAgentCode(string name, Guid id)
    {
        // Chỉ giữ ký tự chữ và số để mã sinh ra an toàn cho tìm kiếm/lọc dữ liệu.
        var normalized = new string(name.Trim().ToUpperInvariant().Where(char.IsLetterOrDigit).ToArray());
        var prefix = string.IsNullOrWhiteSpace(normalized) ? "AGENT" : normalized[..Math.Min(normalized.Length, 10)];
        return $"{prefix}-{id.ToString("N")[..8]}";
    }

    /// <summary>
    /// Kiểm tra dữ liệu đầu vào cho luồng tạo agent.
    /// </summary>
    private static ServiceResult<AgentListItem>? ValidateCreateCommand(CreateAgentCommand command)
    {
        if (string.IsNullOrWhiteSpace(command.Name) || string.IsNullOrWhiteSpace(command.Role))
        {
            return ServiceResult<AgentListItem>.Failure(
                AgentErrorCodes.ValidationError,
                "Name and role are required.");
        }

        return null;
    }

    /// <summary>
    /// Kiểm tra dữ liệu đầu vào cho luồng cập nhật agent.
    /// </summary>
    private static ServiceResult<AgentListItem>? ValidateUpdateCommand(UpdateAgentCommand command)
    {
        if (string.IsNullOrWhiteSpace(command.Name) || string.IsNullOrWhiteSpace(command.Role))
        {
            return ServiceResult<AgentListItem>.Failure(
                AgentErrorCodes.ValidationError,
                "Name and role are required.");
        }

        if (!Enum.TryParse<AgentStatus>(command.Status, true, out _))
        {
            return ServiceResult<AgentListItem>.Failure(
                AgentErrorCodes.InvalidStatus,
                "Status value is invalid.");
        }

        return null;
    }

    /// <summary>
    /// Chuẩn hóa filter đầu vào từ API sang kiểu filter nội bộ cho tầng truy vấn.
    /// </summary>
    private static bool TryCreateQueryFilters(AgentListFilters filters, out AgentQueryFilters queryFilters)
    {
        var hasValidStatus = TryParseStatus(filters.Status, out var status);
        var page = filters.Page ?? 1;
        var pageSize = filters.PageSize ?? 20;
        queryFilters = new AgentQueryFilters(status, NormalizeSearch(filters.Search), page, pageSize);
        return hasValidStatus;
    }

    /// <summary>
    /// Chuẩn hóa chuỗi tìm kiếm để tránh truyền giá trị rỗng xuống tầng truy vấn.
    /// </summary>
    private static string? NormalizeSearch(string? search)
    {
        var normalized = search?.Trim();
        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }

    /// <summary>
    /// Chỉ cache các danh sách phổ biến: không search tự do, trang đầu, và page size vừa phải.
    /// </summary>
    private async Task<string?> TryBuildAgentListCacheKeyAsync(
        Guid? tenantId,
        AgentQueryFilters filters,
        CancellationToken cancellationToken)
    {
        if (filters.Page != 1 || filters.PageSize > 50 || !string.IsNullOrWhiteSpace(filters.Search))
        {
            return null;
        }

        var namespaceKey = ApplicationCacheKeys.AgentListNamespace(tenantId);
        try
        {
            var version = await cacheVersionService.GetVersionAsync(namespaceKey, cancellationToken);
            return ApplicationCacheKeys.AgentList(
                tenantId,
                filters.Status?.ToString().ToLowerInvariant() ?? "all",
                filters.PageSize,
                version);
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Không thể lấy agent-list cache version.");
            return null;
        }
    }

    /// <summary>
    /// Tạo cache key cho chi tiết agent theo scope hiện tại.
    /// </summary>
    private async Task<string?> TryBuildAgentDetailCacheKeyAsync(
        Guid? tenantId,
        Guid agentId,
        CancellationToken cancellationToken)
    {
        var namespaceKey = ApplicationCacheKeys.AgentDetailNamespace(tenantId, agentId);
        try
        {
            var version = await cacheVersionService.GetVersionAsync(namespaceKey, cancellationToken);
            return ApplicationCacheKeys.AgentDetail(tenantId, agentId, version);
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Không thể lấy agent-detail cache version cho agent {AgentId}.", agentId);
            return null;
        }
    }

    /// <summary>
    /// Invalidate cache đọc liên quan đến agent sau khi ghi thành công.
    /// </summary>
    private async Task InvalidateAgentReadCacheAsync(Guid? tenantId, Guid agentId, CancellationToken cancellationToken)
    {
        await TryRefreshCacheVersionAsync(
            ApplicationCacheKeys.AgentDetailNamespace(tenantId, agentId),
            "agent detail",
            cancellationToken);

        await TryRefreshCacheVersionAsync(
            ApplicationCacheKeys.AgentListNamespace(tenantId),
            "agent list",
            cancellationToken);
    }

    /// <summary>
    /// Đọc cache typed và fallback về luồng cũ khi Redis lỗi.
    /// </summary>
    private async Task<TValue?> TryGetCachedValueAsync<TValue>(
        string cacheKey,
        string cacheLabel,
        CancellationToken cancellationToken)
    {
        try
        {
            return await distributedCacheService.GetAsync<TValue>(cacheKey, cancellationToken);
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Không thể đọc {CacheLabel} cache.", cacheLabel);
            return default;
        }
    }

    /// <summary>
    /// Ghi cache typed mà không làm gián đoạn response khi Redis lỗi.
    /// </summary>
    private async Task TrySetCachedValueAsync<TValue>(
        string cacheKey,
        TValue value,
        TimeSpan timeToLive,
        string cacheLabel,
        CancellationToken cancellationToken)
    {
        try
        {
            await distributedCacheService.SetAsync(cacheKey, value, timeToLive, cancellationToken);
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Không thể ghi {CacheLabel} cache.", cacheLabel);
        }
    }

    /// <summary>
    /// Làm mới version token của namespace cache.
    /// </summary>
    private async Task TryRefreshCacheVersionAsync(
        string namespaceKey,
        string cacheLabel,
        CancellationToken cancellationToken)
    {
        try
        {
            await cacheVersionService.RefreshVersionAsync(namespaceKey, cancellationToken);
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Không thể invalidate {CacheLabel} cache.", cacheLabel);
        }
    }

    /// <summary>
    /// Phân tích giá trị trạng thái từ query string sang enum nội bộ.
    /// </summary>
    private static bool TryParseStatus(string? status, out AgentStatus? parsedStatus)
    {
        parsedStatus = null;
        var normalized = status?.Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return true;
        }

        if (Enum.TryParse<AgentStatus>(normalized, true, out var statusValue))
        {
            parsedStatus = statusValue;
            return true;
        }

        return false;
    }

    #endregion
}
