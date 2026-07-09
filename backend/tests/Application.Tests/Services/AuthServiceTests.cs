using Demo.Application.DTOs;
using Demo.Application.Errors;
using Demo.Application.Services;
using Demo.Domain.Entities;
using Demo.Domain.Enums;
using Demo.Domain.Interfaces.Repository;
using Demo.Domain.Interfaces.Service;
using Demo.Domain.Options;

using Microsoft.Extensions.Logging;

using Moq;

namespace Demo.Application.Tests.Services;

/// <summary>
/// Kiểm tra các luồng xác thực chính: đăng nhập, đổi mật khẩu, refresh token và đăng xuất.
/// </summary>
public sealed class AuthServiceTests
{
    [Fact]
    public async Task LoginAsync_ShouldCreateSessionAndReturnTokens_WhenCredentialsAreValid()
    {
        var user = CreateUser(status: AccountStatus.Active);
        var authUserRepository = new Mock<IAuthUserRepository>();
        var refreshTokenRepository = new Mock<IRefreshTokenRepository>();
        var userSessionRepository = new Mock<IUserSessionRepository>();
        var auditLogService = new Mock<IAuditLogService>();
        var passwordHasher = new Mock<IPasswordHasher>();
        var jwtTokenService = new Mock<IJwtTokenService>();
        var permissionService = new Mock<IPermissionService>();
        var refreshTokenHasher = new Mock<IRefreshTokenHasher>();
        var authOptions = new Mock<IAuthOptions>();
        var distributedCacheService = new Mock<IDistributedCacheService>();
        var logger = new Mock<ILogger<AuthService>>();
        var unitOfWork = new Mock<IUnitOfWork>();

        authUserRepository.Setup(repository => repository.GetByEmailAsync("demo@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        passwordHasher.Setup(service => service.VerifyPassword("Password123!", user.PasswordHash))
            .Returns(true);
        permissionService.Setup(service => service.GetGrantedPermissionCodesAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(["agent.read", "agent.write"]);
        jwtTokenService.Setup(service => service.CreateAccessToken(user, It.IsAny<Guid>(), It.IsAny<IEnumerable<string>>()))
            .Returns(new JwtTokenResult("access-token", new DateTime(2026, 7, 9, 12, 0, 0, DateTimeKind.Utc), "jwt-id"));
        refreshTokenHasher.Setup(service => service.GenerateToken())
            .Returns(new RefreshTokenSecret("raw-refresh-token", "refresh-token-hash"));
        authOptions.SetupGet(options => options.RefreshTokenDays).Returns(7);

        UserSession? createdSession = null;
        RefreshToken? createdRefreshToken = null;
        userSessionRepository.Setup(repository => repository.Add(It.IsAny<UserSession>()))
            .Callback<UserSession>(session => createdSession = session);
        refreshTokenRepository.Setup(repository => repository.Add(It.IsAny<RefreshToken>()))
            .Callback<RefreshToken>(token => createdRefreshToken = token);

        var service = CreateService();

        var result = await service.LoginAsync(
            new LoginRequest("demo@example.com", "Password123!"),
            "127.0.0.1",
            CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.AccessToken.Should().Be("access-token");
        result.Value.RefreshToken.Should().Be("raw-refresh-token");

        createdSession.Should().NotBeNull();
        createdSession!.UserId.Should().Be(user.Id);
        createdSession.CreatedByIp.Should().Be("127.0.0.1");

        createdRefreshToken.Should().NotBeNull();
        createdRefreshToken!.UserId.Should().Be(user.Id);
        createdRefreshToken.TokenHash.Should().Be("refresh-token-hash");
        createdRefreshToken.CreatedByIp.Should().Be("127.0.0.1");

        unitOfWork.Verify(work => work.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        auditLogService.Verify(
            service => service.RecordAsync(
                "login",
                user.FullName!,
                user.Id,
                null,
                "127.0.0.1",
                It.Is<string>(value => value.Contains("logged in successfully", StringComparison.Ordinal)),
                "User",
                user.Id.ToString(),
                It.IsAny<CancellationToken>()),
            Times.Once);

        AuthService CreateService()
        {
            return new AuthService(
                authUserRepository.Object,
                refreshTokenRepository.Object,
                userSessionRepository.Object,
                auditLogService.Object,
                passwordHasher.Object,
                jwtTokenService.Object,
                permissionService.Object,
                refreshTokenHasher.Object,
                authOptions.Object,
                distributedCacheService.Object,
                logger.Object,
                unitOfWork.Object);
        }
    }

    [Fact]
    public async Task LoginAsync_ShouldFail_WhenCredentialsAreInvalid()
    {
        var authUserRepository = new Mock<IAuthUserRepository>();
        var refreshTokenRepository = new Mock<IRefreshTokenRepository>();
        var userSessionRepository = new Mock<IUserSessionRepository>();
        var auditLogService = new Mock<IAuditLogService>();
        var passwordHasher = new Mock<IPasswordHasher>();
        var jwtTokenService = new Mock<IJwtTokenService>();
        var permissionService = new Mock<IPermissionService>();
        var refreshTokenHasher = new Mock<IRefreshTokenHasher>();
        var authOptions = new Mock<IAuthOptions>();
        var distributedCacheService = new Mock<IDistributedCacheService>();
        var logger = new Mock<ILogger<AuthService>>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var user = CreateUser(status: AccountStatus.Active);

        authUserRepository.Setup(repository => repository.GetByEmailAsync("demo@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        passwordHasher.Setup(service => service.VerifyPassword("wrong-password", user.PasswordHash))
            .Returns(false);

        var service = new AuthService(
            authUserRepository.Object,
            refreshTokenRepository.Object,
            userSessionRepository.Object,
            auditLogService.Object,
            passwordHasher.Object,
            jwtTokenService.Object,
            permissionService.Object,
            refreshTokenHasher.Object,
            authOptions.Object,
            distributedCacheService.Object,
            logger.Object,
            unitOfWork.Object);

        var result = await service.LoginAsync(
            new LoginRequest("demo@example.com", "wrong-password"),
            "127.0.0.1",
            CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.ErrorCode.Should().Be(AuthErrorCodes.InvalidCredentials);
        userSessionRepository.Verify(repository => repository.Add(It.IsAny<UserSession>()), Times.Never);
        refreshTokenRepository.Verify(repository => repository.Add(It.IsAny<RefreshToken>()), Times.Never);
        unitOfWork.Verify(work => work.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        auditLogService.Verify(
            service => service.RecordAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<Guid?>(),
                It.IsAny<Guid?>(),
                It.IsAny<string?>(),
                It.IsAny<string>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ChangePasswordAsync_ShouldUpdatePasswordAndRevokeActiveSessions()
    {
        var user = CreateUser(status: AccountStatus.Active);
        var sessions = new List<UserSession>
        {
            new() { Id = Guid.NewGuid(), UserId = user.Id, ExpiresAt = DateTime.UtcNow.AddDays(1) },
            new() { Id = Guid.NewGuid(), UserId = user.Id, ExpiresAt = DateTime.UtcNow.AddDays(1), RevokedAt = DateTime.UtcNow.AddMinutes(-5) }
        };

        var authUserRepository = new Mock<IAuthUserRepository>();
        var refreshTokenRepository = new Mock<IRefreshTokenRepository>();
        var userSessionRepository = new Mock<IUserSessionRepository>();
        var auditLogService = new Mock<IAuditLogService>();
        var passwordHasher = new Mock<IPasswordHasher>();
        var jwtTokenService = new Mock<IJwtTokenService>();
        var permissionService = new Mock<IPermissionService>();
        var refreshTokenHasher = new Mock<IRefreshTokenHasher>();
        var authOptions = new Mock<IAuthOptions>();
        var distributedCacheService = new Mock<IDistributedCacheService>();
        var logger = new Mock<ILogger<AuthService>>();
        var unitOfWork = new Mock<IUnitOfWork>();

        authUserRepository.Setup(repository => repository.GetForUpdateByIdAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        passwordHasher.Setup(service => service.VerifyPassword("Current123!", user.PasswordHash))
            .Returns(true);
        passwordHasher.Setup(service => service.HashPassword("New123!"))
            .Returns("new-password-hash");
        userSessionRepository.Setup(repository => repository.GetActiveByUserIdAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(sessions);

        var service = new AuthService(
            authUserRepository.Object,
            refreshTokenRepository.Object,
            userSessionRepository.Object,
            auditLogService.Object,
            passwordHasher.Object,
            jwtTokenService.Object,
            permissionService.Object,
            refreshTokenHasher.Object,
            authOptions.Object,
            distributedCacheService.Object,
            logger.Object,
            unitOfWork.Object);

        var result = await service.ChangePasswordAsync(
            user.Id,
            new ChangePasswordRequest("Current123!", "New123!"),
            "10.0.0.1",
            CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        user.PasswordHash.Should().Be("new-password-hash");
        sessions.Should().OnlyContain(session => session.RevokedByIp == "10.0.0.1" && session.ReasonRevoked == "PasswordChanged");
        distributedCacheService.Verify(
            service => service.RemoveAsync(It.Is<string>(key => key.StartsWith("auth-session:", StringComparison.Ordinal)), It.IsAny<CancellationToken>()),
            Times.Exactly(2));
        unitOfWork.Verify(work => work.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RefreshAsync_ShouldRotateRefreshToken_WhenSessionAndUserAreValid()
    {
        var user = CreateUser(status: AccountStatus.Active);
        var session = new UserSession
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            ExpiresAt = DateTime.UtcNow.AddDays(2)
        };
        var existingRefreshToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            SessionId = session.Id,
            TokenHash = "current-hash",
            ExpiresAt = DateTime.UtcNow.AddDays(1),
            Session = session,
            User = user
        };

        var authUserRepository = new Mock<IAuthUserRepository>();
        var refreshTokenRepository = new Mock<IRefreshTokenRepository>();
        var userSessionRepository = new Mock<IUserSessionRepository>();
        var auditLogService = new Mock<IAuditLogService>();
        var passwordHasher = new Mock<IPasswordHasher>();
        var jwtTokenService = new Mock<IJwtTokenService>();
        var permissionService = new Mock<IPermissionService>();
        var refreshTokenHasher = new Mock<IRefreshTokenHasher>();
        var authOptions = new Mock<IAuthOptions>();
        var distributedCacheService = new Mock<IDistributedCacheService>();
        var logger = new Mock<ILogger<AuthService>>();
        var unitOfWork = new Mock<IUnitOfWork>();

        refreshTokenHasher.Setup(service => service.HashToken("refresh-raw"))
            .Returns("current-hash");
        refreshTokenRepository.Setup(repository => repository.GetByTokenHashWithUserAndSessionAsync("current-hash", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingRefreshToken);
        refreshTokenHasher.Setup(service => service.GenerateToken())
            .Returns(new RefreshTokenSecret("new-raw-token", "new-token-hash"));
        permissionService.Setup(service => service.GetGrantedPermissionCodesAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(["agent.read"]);
        jwtTokenService.Setup(service => service.CreateAccessToken(user, session.Id, It.IsAny<IEnumerable<string>>()))
            .Returns(new JwtTokenResult("new-access-token", new DateTime(2026, 7, 9, 15, 0, 0, DateTimeKind.Utc), "jwt-id"));
        authOptions.SetupGet(options => options.RefreshTokenDays).Returns(30);

        RefreshToken? createdRefreshToken = null;
        refreshTokenRepository.Setup(repository => repository.Add(It.IsAny<RefreshToken>()))
            .Callback<RefreshToken>(token => createdRefreshToken = token);

        var service = new AuthService(
            authUserRepository.Object,
            refreshTokenRepository.Object,
            userSessionRepository.Object,
            auditLogService.Object,
            passwordHasher.Object,
            jwtTokenService.Object,
            permissionService.Object,
            refreshTokenHasher.Object,
            authOptions.Object,
            distributedCacheService.Object,
            logger.Object,
            unitOfWork.Object);

        var result = await service.RefreshAsync(
            new RefreshTokenRequest("refresh-raw"),
            "127.0.0.2",
            CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        result.Value!.AccessToken.Should().Be("new-access-token");
        result.Value.RefreshToken.Should().Be("new-raw-token");
        existingRefreshToken.ReasonRevoked.Should().Be("Rotated");
        existingRefreshToken.RevokedByIp.Should().Be("127.0.0.2");
        existingRefreshToken.ReplacedByTokenHash.Should().Be("new-token-hash");
        createdRefreshToken.Should().NotBeNull();
        createdRefreshToken!.TokenHash.Should().Be("new-token-hash");
        createdRefreshToken.SessionId.Should().Be(session.Id);
        unitOfWork.Verify(work => work.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task LogoutAsync_ShouldRevokeRefreshTokenAndSession_WhenTokenExists()
    {
        var session = new UserSession
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            ExpiresAt = DateTime.UtcNow.AddDays(1)
        };
        var refreshToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = session.UserId,
            SessionId = session.Id,
            TokenHash = "hashed-token",
            ExpiresAt = DateTime.UtcNow.AddDays(1),
            Session = session
        };

        var authUserRepository = new Mock<IAuthUserRepository>();
        var refreshTokenRepository = new Mock<IRefreshTokenRepository>();
        var userSessionRepository = new Mock<IUserSessionRepository>();
        var auditLogService = new Mock<IAuditLogService>();
        var passwordHasher = new Mock<IPasswordHasher>();
        var jwtTokenService = new Mock<IJwtTokenService>();
        var permissionService = new Mock<IPermissionService>();
        var refreshTokenHasher = new Mock<IRefreshTokenHasher>();
        var authOptions = new Mock<IAuthOptions>();
        var distributedCacheService = new Mock<IDistributedCacheService>();
        var logger = new Mock<ILogger<AuthService>>();
        var unitOfWork = new Mock<IUnitOfWork>();

        refreshTokenHasher.Setup(service => service.HashToken("raw-token")).Returns("hashed-token");
        refreshTokenRepository.Setup(repository => repository.GetByTokenHashWithSessionAsync("hashed-token", It.IsAny<CancellationToken>()))
            .ReturnsAsync(refreshToken);

        var service = new AuthService(
            authUserRepository.Object,
            refreshTokenRepository.Object,
            userSessionRepository.Object,
            auditLogService.Object,
            passwordHasher.Object,
            jwtTokenService.Object,
            permissionService.Object,
            refreshTokenHasher.Object,
            authOptions.Object,
            distributedCacheService.Object,
            logger.Object,
            unitOfWork.Object);

        await service.LogoutAsync(new LogoutRequest("raw-token"), "192.168.1.1", CancellationToken.None);

        refreshToken.ReasonRevoked.Should().Be("Logout");
        refreshToken.RevokedByIp.Should().Be("192.168.1.1");
        session.ReasonRevoked.Should().Be("Logout");
        session.RevokedByIp.Should().Be("192.168.1.1");
        distributedCacheService.Verify(
            service => service.RemoveAsync(It.Is<string>(key => key.Contains(session.Id.ToString("N"), StringComparison.Ordinal)), It.IsAny<CancellationToken>()),
            Times.Once);
        unitOfWork.Verify(work => work.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    private static User CreateUser(AccountStatus status)
    {
        return new User
        {
            Id = Guid.NewGuid(),
            Email = "demo@example.com",
            FullName = "Demo User",
            PasswordHash = "stored-password-hash",
            Status = status
        };
    }
}
