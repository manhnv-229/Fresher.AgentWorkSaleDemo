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
/// Kiểm tra các luồng chính của quản lý agent.
/// </summary>
public sealed class AgentCatalogServiceTests
{
    private static readonly IMapper Mapper = TestMapperFactory.Create();

    [Fact]
    public async Task CreateInternalAgentAsync_ShouldCreateDraftInternalAgentAndRecordAudit()
    {
        var actorUserId = Guid.NewGuid();
        var authUserRepository = new Mock<IAuthUserRepository>();
        var testContext = CreateService(authUserRepository: authUserRepository);
        var service = testContext.Service;

        authUserRepository.Setup(repository => repository.GetByIdAsync(actorUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new User { Id = actorUserId, FullName = "Admin User", Email = "admin@example.com" });

        var result = await service.CreateInternalAgentAsync(
            actorUserId,
            "127.0.0.1",
            new CreateAgentCommand("  Support Agent  ", "  Advisor  ", "  Helps users  ", "  headset  "),
            CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        testContext.AddedAgent.Should().NotBeNull();
        testContext.AddedAgent!.Scope.Should().Be(AgentScope.Internal);
        testContext.AddedAgent.Status.Should().Be(AgentStatus.Draft);
        testContext.AddedAgent.TenantId.Should().BeNull();
        testContext.AddedAgent.Name.Should().Be("Support Agent");
        testContext.AddedAgent.Role.Should().Be("Advisor");
        testContext.AddedAgent.Description.Should().Be("Helps users");
        testContext.AddedAgent.Icon.Should().Be("headset");
        testContext.AddedAgent.Code.Should().StartWith("SUPPORTAGE-");
        testContext.UnitOfWork.Verify(work => work.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        testContext.CacheVersionService.Verify(service => service.RefreshVersionAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
        testContext.AuditLogService.Verify(
            service => service.RecordAsync(
                "agent.create",
                "Admin User",
                actorUserId,
                null,
                "127.0.0.1",
                It.Is<string>(value => value.Contains("Internal agent", StringComparison.Ordinal)),
                "Agent",
                testContext.AddedAgent.Id.ToString(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task UpdateTenantAgentAsync_ShouldUpdateFieldsAndInvalidateCaches()
    {
        var tenantId = Guid.NewGuid();
        var actorUserId = Guid.NewGuid();
        var agent = new Agent
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Name = "Old Agent",
            Role = "Support",
            Description = "Old description",
            Icon = "old-icon",
            Scope = AgentScope.Tenant,
            Status = AgentStatus.Draft
        };

        var authUserRepository = new Mock<IAuthUserRepository>();
        authUserRepository.Setup(repository => repository.GetByIdAsync(actorUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new User { Id = actorUserId, Email = "editor@example.com" });

        var testContext = CreateService(
            authUserRepository: authUserRepository,
            tenantRepository: repository =>
                repository.Setup(item => item.GetByIdAsync(tenantId, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new Tenant { Id = tenantId, Status = TenantStatus.Active }),
            agentRepositorySetup: repository =>
                repository.Setup(item => item.GetTenantAgentByIdAsync(tenantId, agent.Id, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(agent));
        var service = testContext.Service;

        var result = await service.UpdateTenantAgentAsync(
            tenantId,
            agent.Id,
            actorUserId,
            "10.0.0.1",
            new UpdateAgentCommand("  New Agent  ", "  Sales  ", "  New description  ", "  rocket  ", "Active"),
            CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        agent.Name.Should().Be("New Agent");
        agent.Role.Should().Be("Sales");
        agent.Description.Should().Be("New description");
        agent.Icon.Should().Be("rocket");
        agent.Status.Should().Be(AgentStatus.Active);
        agent.ModifiedByUserId.Should().Be(actorUserId);
        agent.ModifiedAt.Should().NotBeNull();
        testContext.UnitOfWork.Verify(work => work.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        testContext.CacheVersionService.Verify(service => service.RefreshVersionAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
        testContext.AuditLogService.Verify(
            service => service.RecordAsync(
                "agent.update",
                "editor@example.com",
                actorUserId,
                tenantId,
                "10.0.0.1",
                It.Is<string>(value => value.Contains("Tenant agent 'Old Agent' updated", StringComparison.Ordinal)),
                "Agent",
                agent.Id.ToString(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task DeleteInternalAgentAsync_ShouldSoftDeleteAgent()
    {
        var actorUserId = Guid.NewGuid();
        var agent = new Agent
        {
            Id = Guid.NewGuid(),
            Name = "Legacy Agent",
            Scope = AgentScope.Internal,
            Status = AgentStatus.Active
        };

        var authUserRepository = new Mock<IAuthUserRepository>();
        authUserRepository.Setup(repository => repository.GetByIdAsync(actorUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new User { Id = actorUserId, FullName = "Ops Admin" });

        var testContext = CreateService(
            authUserRepository: authUserRepository,
            agentRepositorySetup: repository =>
                repository.Setup(item => item.GetInternalAgentByIdAsync(agent.Id, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(agent));
        var service = testContext.Service;

        var result = await service.DeleteInternalAgentAsync(
            agent.Id,
            actorUserId,
            "172.16.0.2",
            CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        agent.Status.Should().Be(AgentStatus.Deleted);
        agent.ModifiedByUserId.Should().Be(actorUserId);
        agent.ModifiedAt.Should().NotBeNull();
        agent.DeletedAt.Should().NotBeNull();
        testContext.UnitOfWork.Verify(work => work.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        testContext.CacheVersionService.Verify(service => service.RefreshVersionAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
        testContext.AuditLogService.Verify(
            service => service.RecordAsync(
                "agent.delete",
                "Ops Admin",
                actorUserId,
                null,
                "172.16.0.2",
                It.Is<string>(value => value.Contains("Legacy Agent", StringComparison.Ordinal)),
                "Agent",
                agent.Id.ToString(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    private static AgentCatalogServiceTestContext CreateService(
        Mock<IAuthUserRepository>? authUserRepository = null,
        Action<Mock<ITenantRepository>>? tenantRepository = null,
        Action<Mock<IAgentRepository>>? agentRepositorySetup = null)
    {
        var agentQueryRepository = new Mock<IAgentQueryRepository>();
        var agentRepository = new Mock<IAgentRepository>();
        var tenantRepo = new Mock<ITenantRepository>();
        var auditLogService = new Mock<IAuditLogService>();
        var authRepo = authUserRepository ?? new Mock<IAuthUserRepository>();
        var distributedCacheService = new Mock<IDistributedCacheService>();
        var cacheVersionService = new Mock<ICacheVersionService>();
        var cachePolicyProvider = new Mock<IApplicationCachePolicyProvider>();
        var logger = new Mock<ILogger<AgentCatalogService>>();
        var unitOfWork = new Mock<IUnitOfWork>();

        var holder = new AddedAgentHolder();
        cacheVersionService.Setup(service => service.RefreshVersionAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        cacheVersionService.Setup(service => service.GetVersionAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("v1");
        agentRepository.Setup(repository => repository.Add(It.IsAny<Agent>()))
            .Callback<Agent>(agent => holder.Value = agent);

        tenantRepository?.Invoke(tenantRepo);
        agentRepositorySetup?.Invoke(agentRepository);

        var service = new AgentCatalogService(
            agentQueryRepository.Object,
            agentRepository.Object,
            tenantRepo.Object,
            auditLogService.Object,
            authRepo.Object,
            distributedCacheService.Object,
            cacheVersionService.Object,
            cachePolicyProvider.Object,
            Mapper,
            logger.Object,
            unitOfWork.Object);

        return new AgentCatalogServiceTestContext(service, agentRepository, cacheVersionService, auditLogService, unitOfWork, holder);
    }

    private sealed class AgentCatalogServiceTestContext(
        AgentCatalogService service,
        Mock<IAgentRepository> agentRepository,
        Mock<ICacheVersionService> cacheVersionService,
        Mock<IAuditLogService> auditLogService,
        Mock<IUnitOfWork> unitOfWork,
        AddedAgentHolder addedAgentHolder)
    {
        public AgentCatalogService Service { get; } = service;
        public Mock<IAgentRepository> AgentRepository { get; } = agentRepository;
        public Mock<ICacheVersionService> CacheVersionService { get; } = cacheVersionService;
        public Mock<IAuditLogService> AuditLogService { get; } = auditLogService;
        public Mock<IUnitOfWork> UnitOfWork { get; } = unitOfWork;
        public Agent? AddedAgent => addedAgentHolder.Value;
    }

    private sealed class AddedAgentHolder
    {
        public Agent? Value { get; set; }
    }
}
