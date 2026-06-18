# Auth Application Clean Boundary Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Move auth use-case orchestration into `Demo.Application` while leaving `Demo.Infrastructure` responsible only for persistence, hashing, JWT, and session validation implementations.

**Architecture:** Introduce application-layer auth repositories and move the `AuthService` use-case implementation into `Demo.Application/Features/Auth`, then replace the infrastructure-hosted auth service with repository implementations backed by `DemoDbContext`. Keep API routes, DTOs, and refresh-token cookie behavior unchanged.

**Tech Stack:** ASP.NET Core, Entity Framework Core, MySQL, JWT, BCrypt, existing `ServiceResult<T>` pattern.

---

### Task 1: Define application-layer auth boundaries

**Files:**
- Create: `backend/Demo.Application/Features/Auth/IAuthUserRepository.cs`
- Create: `backend/Demo.Application/Features/Auth/IRefreshTokenRepository.cs`
- Create: `backend/Demo.Application/Features/Auth/IUserSessionRepository.cs`
- Create: `backend/Demo.Application/Features/Auth/AuthService.cs`

- [ ] Add repository contracts for users, refresh tokens, and sessions needed by auth use cases.
- [ ] Move auth orchestration into an application-layer `AuthService` that keeps current behavior and error codes.

### Task 2: Replace infrastructure auth use-case service with persistence implementations

**Files:**
- Delete: `backend/Demo.Infrastructure/Auth/AuthService.cs`
- Create: `backend/Demo.Infrastructure/Auth/AuthUserRepository.cs`
- Create: `backend/Demo.Infrastructure/Auth/RefreshTokenRepository.cs`
- Create: `backend/Demo.Infrastructure/Auth/UserSessionRepository.cs`
- Modify: `backend/Demo.Infrastructure/DependencyInjection.cs`

- [ ] Move EF Core auth persistence into repository implementations.
- [ ] Update dependency injection so `IAuthService` resolves to the application service and repositories resolve from infrastructure.

### Task 3: Preserve API and technical contracts

**Files:**
- Review: `backend/Demo.Api/Features/Auth/AuthController.cs`
- Review: `backend/Demo.Infrastructure/Auth/JwtTokenService.cs`
- Review: `backend/Demo.Infrastructure/Auth/AuthSessionValidator.cs`

- [ ] Keep controller/cookie flow unchanged while ensuring dependencies still resolve.
- [ ] Leave JWT/hash/session-validation technical services in infrastructure.

### Task 4: Verify boundary cleanup

**Files:**
- Review: `backend/Demo.Application/Features/Auth/*.cs`
- Review: `backend/Demo.Infrastructure/Auth/*.cs`
- Review: `backend/Demo.Api/Features/Auth/*.cs`

- [ ] Run source scans to confirm `Infrastructure/Auth` no longer owns auth use-case orchestration.
- [ ] Attempt backend build and report the actual environment result without overstating success.
