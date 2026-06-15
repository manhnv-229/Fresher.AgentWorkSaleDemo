namespace Demo.Application.Authorization;

public interface IPermissionService
{
    Task<bool> HasPermissionAsync(Guid userId, Guid? tenantId, string permissionCode, CancellationToken cancellationToken);
}
