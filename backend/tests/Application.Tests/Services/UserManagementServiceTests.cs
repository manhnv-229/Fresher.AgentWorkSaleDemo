using AutoMapper;

using Demo.Application.DTOs;
using Demo.Application.Errors;
using Demo.Application.Interfaces.Repository;
using Demo.Application.Services;
using Demo.Application.Tests.TestHelpers;
using Demo.Domain.Entities;
using Demo.Domain.Enums;
using Demo.Domain.Interfaces.Repository;
using Demo.Domain.Interfaces.Service;

using Microsoft.Extensions.Logging;

using Moq;

namespace Demo.Application.Tests.Services;

/// <summary>
/// Kiểm tra các luồng quản lý staff: khóa, mở khóa và cập nhật chức danh.
/// </summary>
public sealed class UserManagementServiceTests
{
    private static readonly IMapper Mapper = TestMapperFactory.Create();

    [Fact]
    public async Task LockUserAsync_ShouldFail_WhenActorAttemptsToLockSelf()
    {
        var service = CreateService().Service;
        var userId = Guid.NewGuid();

        var result = await service.LockUserAsync(userId, userId, "127.0.0.1", CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.ErrorCode.Should().Be(AuthErrorCodes.CannotLockSelf);
    }

    [Fact]
    public async Task LockUserAsync_ShouldLockTargetAndRevokeActiveSessions()
    {
        var actorUserId = Guid.NewGuid();
        var targetUserId = Guid.NewGuid();
        var targetUser = new User
        {
            Id = targetUserId,
            Email = "staff@example.com",
            FullName = "Staff User",
            Status = AccountStatus.Active
        };
        var sessions = new List<UserSession>
        {
            new() { Id = Guid.NewGuid(), UserId = targetUserId, ExpiresAt = DateTime.UtcNow.AddDays(1) },
            new() { Id = Guid.NewGuid(), UserId = targetUserId, ExpiresAt = DateTime.UtcNow.AddDays(1) }
        };

        var testContext = CreateService(
            authUserRepository: repository =>
            {
                repository.Setup(item => item.GetForUpdateByIdAsync(targetUserId, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(targetUser);
                repository.Setup(item => item.GetByIdAsync(actorUserId, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new User { Id = actorUserId, FullName = "Admin" });
            },
            userSessionRepository: repository =>
                repository.Setup(item => item.GetActiveByUserIdAsync(targetUserId, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(sessions));
        var service = testContext.Service;

        var result = await service.LockUserAsync(actorUserId, targetUserId, "10.10.10.10", CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        targetUser.Status.Should().Be(AccountStatus.Locked);
        targetUser.ModifiedAt.Should().NotBeNull();
        sessions.Should().OnlyContain(session => session.RevokedByIp == "10.10.10.10" && session.ReasonRevoked == "AccountLocked");
        testContext.UnitOfWork.Verify(work => work.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        testContext.CacheVersionService.Verify(
            service => service.RefreshVersionAsync(It.Is<string>(key => key.Contains(targetUserId.ToString("N"), StringComparison.Ordinal)), It.IsAny<CancellationToken>()),
            Times.Once);
        testContext.AuditLogService.Verify(
            service => service.RecordAsync(
                "user.lock",
                "Admin",
                actorUserId,
                null,
                "10.10.10.10",
                It.Is<string>(value => value.Contains("đã bị khóa", StringComparison.Ordinal)),
                "User",
                targetUserId.ToString(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task UnlockUserAsync_ShouldSetStatusActiveWithoutRevokingSessions()
    {
        var actorUserId = Guid.NewGuid();
        var targetUserId = Guid.NewGuid();
        var targetUser = new User
        {
            Id = targetUserId,
            Email = "locked@example.com",
            FullName = "Locked User",
            Status = AccountStatus.Locked
        };

        var testContext = CreateService(
            authUserRepository: repository =>
            {
                repository.Setup(item => item.GetForUpdateByIdAsync(targetUserId, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(targetUser);
                repository.Setup(item => item.GetByIdAsync(actorUserId, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new User { Id = actorUserId, Email = "hr@example.com" });
            });
        var service = testContext.Service;

        var result = await service.UnlockUserAsync(actorUserId, targetUserId, "10.10.10.11", CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        targetUser.Status.Should().Be(AccountStatus.Active);
        testContext.UserSessionRepository.Verify(repository => repository.GetActiveByUserIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        testContext.UnitOfWork.Verify(work => work.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        testContext.CacheVersionService.Verify(service => service.RefreshVersionAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
        testContext.AuditLogService.Verify(
            service => service.RecordAsync(
                "user.unlock",
                "hr@example.com",
                actorUserId,
                null,
                "10.10.10.11",
                It.Is<string>(value => value.Contains("đã được mở khóa", StringComparison.Ordinal)),
                "User",
                targetUserId.ToString(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task UpdateJobPositionAsync_ShouldPersistNewJobPositionAndRecordChangeSummary()
    {
        var actorUserId = Guid.NewGuid();
        var targetUserId = Guid.NewGuid();
        var targetUser = new User
        {
            Id = targetUserId,
            Email = "staff@example.com",
            FullName = "Staff User",
            JobPosition = "Junior"
        };

        var testContext = CreateService(
            authUserRepository: repository =>
            {
                repository.Setup(item => item.GetForUpdateByIdAsync(targetUserId, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(targetUser);
                repository.Setup(item => item.GetByIdAsync(actorUserId, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new User { Id = actorUserId, FullName = "Manager" });
            });
        var service = testContext.Service;

        var result = await service.UpdateJobPositionAsync(
            actorUserId,
            targetUserId,
            "Senior",
            "192.168.0.5",
            CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        targetUser.JobPosition.Should().Be("Senior");
        targetUser.ModifiedAt.Should().NotBeNull();
        testContext.UnitOfWork.Verify(work => work.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        testContext.AuditLogService.Verify(
            service => service.RecordAsync(
                "user.update_job_position",
                "Manager",
                actorUserId,
                null,
                "192.168.0.5",
                It.Is<string>(value => value.Contains("JobPosition: 'Junior' -> 'Senior'", StringComparison.Ordinal)),
                "User",
                targetUserId.ToString(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    private static UserManagementServiceTestContext CreateService(
        Action<Mock<IAuthUserRepository>>? authUserRepository = null,
        Action<Mock<IUserSessionRepository>>? userSessionRepository = null)
    {
        var userQueryRepository = new Mock<IUserQueryRepository>();
        var authUserRepositoryMock = new Mock<IAuthUserRepository>();
        var userSessionRepositoryMock = new Mock<IUserSessionRepository>();
        var auditLogService = new Mock<IAuditLogService>();
        var cacheVersionService = new Mock<ICacheVersionService>();
        var logger = new Mock<ILogger<UserManagementService>>();
        var unitOfWork = new Mock<IUnitOfWork>();

        cacheVersionService.Setup(service => service.RefreshVersionAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        authUserRepository?.Invoke(authUserRepositoryMock);
        userSessionRepository?.Invoke(userSessionRepositoryMock);

        var service = new UserManagementService(
            userQueryRepository.Object,
            authUserRepositoryMock.Object,
            userSessionRepositoryMock.Object,
            auditLogService.Object,
            cacheVersionService.Object,
            Mapper,
            logger.Object,
            unitOfWork.Object);

        return new UserManagementServiceTestContext(
            service,
            authUserRepositoryMock,
            userSessionRepositoryMock,
            auditLogService,
            cacheVersionService,
            unitOfWork,
            userQueryRepository);
    }

    private sealed record UserManagementServiceTestContext(
        UserManagementService Service,
        Mock<IAuthUserRepository> AuthUserRepository,
        Mock<IUserSessionRepository> UserSessionRepository,
        Mock<IAuditLogService> AuditLogService,
        Mock<ICacheVersionService> CacheVersionService,
        Mock<IUnitOfWork> UnitOfWork,
        Mock<IUserQueryRepository> UserQueryRepository);
}
