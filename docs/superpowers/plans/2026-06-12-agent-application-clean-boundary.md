# Agent Application Clean Boundary Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Move agent use-case orchestration into `Demo.Application` while leaving `Demo.Infrastructure` responsible only for persistence implementations.

**Architecture:** Introduce application-layer repository contracts and an `IAgentCatalogService` use-case service, then replace the current infrastructure-hosted `AgentService` with infrastructure repository implementations backed by `DemoDbContext`. Keep API routes, payloads, and error codes unchanged so the frontend remains compatible.

**Tech Stack:** ASP.NET Core, Entity Framework Core, MySQL, existing `ServiceResult<T>` pattern.

---

### Task 1: Define application-layer boundaries for agent use cases

**Files:**
- Create: `backend/Demo.Application/Features/Agents/AgentFilters.cs`
- Create: `backend/Demo.Application/Features/Agents/IAgentCatalogService.cs`
- Create: `backend/Demo.Application/Features/Agents/IAgentRepository.cs`
- Create: `backend/Demo.Application/Features/Agents/ITenantRepository.cs`
- Create: `backend/Demo.Application/Features/Agents/AgentCatalogService.cs`
- Modify: `backend/Demo.Application/Features/Agents/AgentDtos.cs`

- [ ] Add parsed filter DTOs and repository contracts needed by the application layer.
- [ ] Implement `AgentCatalogService` with validation, status parsing, search normalization, and use-case orchestration.

### Task 2: Replace infrastructure use-case service with persistence implementations

**Files:**
- Delete: `backend/Demo.Infrastructure/Agents/AgentService.cs`
- Create: `backend/Demo.Infrastructure/Agents/AgentRepository.cs`
- Create: `backend/Demo.Infrastructure/Agents/TenantRepository.cs`
- Modify: `backend/Demo.Infrastructure/DependencyInjection.cs`

- [ ] Move EF Core querying and persistence into repository implementations.
- [ ] Update dependency injection to register repositories and the new application service.

### Task 3: Repoint API controllers to the application service

**Files:**
- Modify: `backend/Demo.Api/Features/Agents/AdminAgentsController.cs`
- Modify: `backend/Demo.Api/Features/Agents/AgentsController.cs`

- [ ] Change controller dependencies from `IAgentService` to `IAgentCatalogService`.
- [ ] Keep HTTP status mapping and response payloads unchanged.

### Task 4: Verify boundary cleanup

**Files:**
- Review: `backend/Demo.Api/Features/Agents/*.cs`
- Review: `backend/Demo.Application/Features/Agents/*.cs`
- Review: `backend/Demo.Infrastructure/Agents/*.cs`

- [ ] Run source scans to confirm controllers no longer touch `DemoDbContext` and infrastructure no longer owns agent use-case decisions.
- [ ] Attempt backend build and report the actual environment result without overstating success.
