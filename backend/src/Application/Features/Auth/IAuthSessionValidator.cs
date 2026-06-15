namespace Demo.Application.Features.Auth;

public interface IAuthSessionValidator
{
    Task<bool> IsSessionActiveAsync(Guid userId, Guid sessionId, CancellationToken cancellationToken);
}
