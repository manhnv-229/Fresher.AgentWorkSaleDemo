namespace Demo.Domain.Interfaces.Service;

public interface IPermissionService
{
    Task<bool> HasPermissionAsync(Guid userId, Guid? tenantId, string permissionCode, CancellationToken cancellationToken);
}
