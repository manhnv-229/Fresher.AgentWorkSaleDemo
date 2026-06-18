# Tenant Application Clean Boundary Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Move tenant use-case orchestration into `Demo.Application` and leave `Demo.Infrastructure` responsible only for EF Core persistence.

**Architecture:** Introduce `ITenantCatalogService` and `ITenantRepository`, implement tenant use cases in the application layer, and replace direct `DemoDbContext` access in `TenantsController` with service calls. Use the existing `IUnitOfWork` for write commits so tenant behavior matches agent and auth flows.

**Tech Stack:** ASP.NET Core, Entity Framework Core, MySQL, existing `ServiceResult<T>` pattern.

---

### Task 1: Define application-layer tenant contracts

**Files:**
- Create: `backend/Demo.Application/Features/Tenants/TenantDtos.cs`
- Create: `backend/Demo.Application/Features/Tenants/TenantErrorCodes.cs`
- Create: `backend/Demo.Application/Features/Tenants/ITenantCatalogService.cs`
- Create: `backend/Demo.Application/Features/Tenants/ITenantRepository.cs`
- Create: `backend/Demo.Application/Features/Tenants/TenantCatalogService.cs`

- [ ] Add tenant DTOs, error codes, repository contract, and application service.
- [ ] Keep list/create behavior aligned with current API semantics.

### Task 2: Add infrastructure repository and DI wiring

**Files:**
- Create: `backend/Demo.Infrastructure/Tenants/TenantRepository.cs`
- Modify: `backend/Demo.Infrastructure/DependencyInjection.cs`

- [ ] Implement tenant persistence with `DemoDbContext`.
- [ ] Register tenant repository and tenant catalog service in DI.

### Task 3: Thin down tenant API controller

**Files:**
- Modify: `backend/Demo.Api/Features/Tenants/TenantsController.cs`

- [ ] Replace direct `DemoDbContext` access with `ITenantCatalogService`.
- [ ] Preserve existing HTTP responses and payload shape.

### Task 4: Verify boundary cleanup

**Files:**
- Review: `backend/Demo.Api/Features/Tenants/*.cs`
- Review: `backend/Demo.Application/Features/Tenants/*.cs`
- Review: `backend/Demo.Infrastructure/Tenants/*.cs`

- [ ] Run source scans to confirm tenant flow no longer goes directly from API to `DemoDbContext`.
- [ ] Attempt backend build and report the real environment result without overstating success.
