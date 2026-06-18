## Context

The current platform has a basic tenant catalog with list and create operations, seeded tenant permissions, and existing domain entities for `Tenant`, `UserTenant`, `UserRole`, and tenant-owned `Agent` records. It does not yet provide tenant detail, update, lock, or tenant-user membership management flows, which means admins cannot safely maintain tenant lifecycle state after onboarding. The codebase already distinguishes global permissions from tenant-scoped permissions and carries tenant identifiers through authorization and persistence layers, so this change should extend those patterns instead of introducing a parallel tenant model.

## Goals / Non-Goals

**Goals:**

- Add admin tenant lifecycle management for create, detail, update, and lock operations.
- Add tenant-user membership management using the existing membership model and status lifecycle.
- Define consistent tenant data-partitioning behavior across read, write, and authorization-sensitive flows.
- Reuse existing permission codes, service layering, and EF-backed repositories wherever possible.
- Prepare the frontend admin workspace to manage tenants and memberships without bypassing backend authorization.

**Non-Goals:**

- Building a full self-service tenant portal for tenant end users.
- Designing cross-tenant bulk actions such as mass membership import or tenant merge.
- Replacing the existing RBAC model with a new authorization system.
- Repartitioning historical data that already violates tenant ownership outside the supported domain relationships.

## Decisions

1. **Represent tenant lifecycle changes through status transitions instead of hard deletion.**

   Tenant management will support create, update, detail, and lock behavior, with locking expressed by `TenantStatus.Locked` and ordinary edits preserving tenant identity and history. Tenant removal is intentionally excluded from this change so tenant-owned agents, memberships, roles, and audit references remain intact.

   Alternative considered: expose hard-delete or soft-delete tenant APIs now. That is rejected because tenant records already anchor several related aggregates, and a delete workflow would require broader archival, cascade, and recovery rules than the request needs.

2. **Reuse `UserTenant` as the source of truth for tenant-user management.**

   Membership management will be modeled as operations on `UserTenant` records, including list, add, activate/deactivate, lock, and remove behavior. This lets the platform keep tenant membership state separate from user accounts and separate from role assignment, which already matches the existing domain.

   Alternative considered: manage tenant users only through `UserRole` assignments. That is rejected because the current permission flow already checks active membership independently, so collapsing membership into role assignment would blur an important authorization boundary.

3. **Keep tenant-management routes global and membership routes nested under the tenant.**

   Tenant catalog operations will remain under `/api/tenants`, expanding to detail and update routes such as `/api/tenants/{tenantId}` plus a lifecycle action such as `/api/tenants/{tenantId}/lock`. Membership operations will live under the tenant route family, such as `/api/tenants/{tenantId}/users`, because the tenant id is the primary scope boundary for those actions.

   Alternative considered: place tenant management under a separate `/api/admin/tenants` route family. That is deferred because the current API surface already exposes tenant resources under `/api/tenants`, and permission attributes already distinguish who can call them.

4. **Enforce tenant partitioning in repository predicates and service orchestration, not only in controllers.**

   Query and mutation methods for tenant-owned resources will require tenant-aware predicates so the application does not rely on route shape alone to protect data boundaries. Services will verify referenced tenants and memberships explicitly, while authorization continues to validate whether the caller has permission to act in or across the requested tenant.

   Alternative considered: rely only on controller route parameters and frontend-selected tenant context. That is rejected because cross-tenant leakage risks are highest when shared repository methods can be called from multiple flows.

5. **Use focused DTOs for tenant detail and membership management instead of overloading list contracts.**

   The application layer will keep lightweight tenant list items for browsing, add a tenant detail DTO for edit screens, and add tenant membership DTOs for user-management screens. This keeps list responses compact while still allowing the UI to render tenant metadata, status, and membership lifecycle state without extra inference.

   Alternative considered: return a single large tenant payload everywhere. That is rejected because it would overfetch for list screens and increase coupling between unrelated UI surfaces.

## Risks / Trade-offs

- **Locked tenants may still appear writable in older flows** -> Centralize tenant status checks in services used by tenant-scoped mutations so locked tenants consistently block changes.
- **Membership removal could orphan tenant-specific role assignments** -> Define whether role assignments are removed or ignored when membership is removed, and enforce that rule in the service layer.
- **Adding tenant detail and membership APIs increases authorization surface area** -> Reuse permission attributes and add route-specific tests for global and tenant-scoped callers.
- **Partitioning rules may be inconsistently applied across existing resource queries** -> Audit tenant-owned repositories touched by admin flows and codify required tenant predicates in specs and implementation tasks.
- **Frontend tenant-management state could drift after mutations** -> Refresh active tenant detail and membership list after create, update, lock, or membership changes using the current selection state.

## Migration Plan

1. Extend application DTOs, service interfaces, and repository contracts for tenant detail, update, lock, and membership-management flows.
2. Implement repository and service logic for tenant lifecycle updates and `UserTenant` membership operations.
3. Add or update API endpoints under `/api/tenants` and `/api/tenants/{tenantId}/users` with permission-based authorization.
4. Add service-level tenant status checks so locked tenants cannot accept tenant-owned mutations that should be blocked.
5. Update admin frontend flows for tenant browsing, detail/edit, lock actions, and tenant-user membership management.
6. Verify authorization, tenant isolation, and lifecycle behavior through backend and frontend tests.

Rollback is to remove the new tenant-management endpoints and frontend screens while preserving tenant and membership data already written. If tenant or membership statuses were changed before rollback, those records can remain because the existing model already supports their lifecycle values.

## Open Questions

- When a tenant is locked, should tenant-user membership edits also be blocked, or should global admins still be able to repair memberships while the tenant is locked?
- When a user is removed from a tenant, should the system delete related `UserRole` tenant assignments immediately or leave them inert until membership is restored?
- Does the admin UI need pagination for tenant membership lists in this change, or is filtered non-paged membership browsing acceptable for the initial release?
