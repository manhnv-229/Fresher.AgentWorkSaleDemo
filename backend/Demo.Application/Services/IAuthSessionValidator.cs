namespace Demo.Application.Services;

public interface IAuthSessionValidator
{
    Task<bool> IsSessionActiveAsync(Guid userId, Guid sessionId, CancellationToken cancellationToken);
}
