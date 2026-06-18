namespace Demo.Domain.Interfaces.Service;

public interface IAuthSessionValidator
{
    Task<bool> IsSessionActiveAsync(Guid userId, Guid sessionId, CancellationToken cancellationToken);
}
