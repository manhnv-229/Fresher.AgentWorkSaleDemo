# Agent Application Layer Extraction Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Move agent catalog and agent creation use cases behind `Demo.Application/Features/Agents` so API controllers become thin orchestration endpoints.

**Architecture:** Introduce agent-specific request/response DTOs and an `IAgentService` contract in the application layer, implement it in infrastructure using `DemoDbContext`, and update agent controllers to delegate to the service while preserving existing routes, validation, and filtering behavior.

**Tech Stack:** ASP.NET Core, Entity Framework Core, MySQL, existing `ServiceResult<T>` pattern.

---

### Task 1: Define agent application contracts

**Files:**
- Create: `backend/Demo.Application/Features/Agents/AgentDtos.cs`
- Create: `backend/Demo.Application/Features/Agents/IAgentService.cs`

- [ ] Add DTOs for list filters, list items, and create-agent requests/results.
- [ ] Add `IAgentService` methods for internal list/create and tenant list/create use cases.

### Task 2: Implement infrastructure agent service

**Files:**
- Create: `backend/Demo.Infrastructure/Agents/AgentService.cs`
- Modify: `backend/Demo.Infrastructure/DependencyInjection.cs`

- [ ] Move filtering, validation, and EF query logic from controllers into a single service implementation.
- [ ] Register the service in dependency injection.

### Task 3: Thin down API controllers

**Files:**
- Modify: `backend/Demo.Api/Features/Agents/AdminAgentsController.cs`
- Modify: `backend/Demo.Api/Features/Agents/AgentsController.cs`

- [ ] Replace direct `DemoDbContext` access with `IAgentService` calls.
- [ ] Keep HTTP-specific concerns in controllers: route binding, permission attributes, `BadRequest`/`NotFound`/`Created` mapping.

### Task 4: Verify refactor safety

**Files:**
- Review: `backend/**/*.cs`

- [ ] Run source scans for stale `DemoDbContext` usage in agent controllers and stale namespaces/imports.
- [ ] Run available build verification commands and report any environment blockers accurately.
