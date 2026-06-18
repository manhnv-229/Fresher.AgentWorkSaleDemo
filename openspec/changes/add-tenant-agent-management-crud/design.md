## Context

The current implementation already separates internal agents from tenant-scoped agents, supports create and filtered list APIs, and renders those lists in a single admin workspace. What is still missing is lifecycle management: admins cannot open a full detail view, update an agent, delete an agent, or work with larger result sets using paging. The backend already has permission-based authorization, an `Agent` entity with `DeletedAt`, `ModifiedAt`, and `AgentStatus.Deleted`, plus tenant-routed APIs that make scope boundaries explicit, so this design should extend those patterns instead of introducing a parallel model.

## Goals / Non-Goals

**Goals:**

- Add detail, update, and delete flows for internal and tenant-scoped agents.
- Add consistent pagination inputs and metadata for internal-agent and tenant-agent list APIs.
- Keep internal/admin scope and tenant scope isolated across reads and mutations.
- Reuse the existing permission model, application service, and repository layering.
- Let the frontend preserve selected tenant and active filters while refreshing data after mutations.

**Non-Goals:**

- Editing branch info, knowledge tree, or instruction content as part of this change.
- Bulk actions such as multi-select delete or mass status updates.
- Cross-tenant reassignment of an agent from one tenant to another.
- Reworking the overall dashboard navigation beyond what is needed for CRUD and detail interactions.

## Decisions

1. **Keep separate route families for internal and tenant-scoped CRUD.**

   Internal-agent management will stay under `/api/admin/agents/internal`, with detail/update/delete routes such as `/api/admin/agents/internal/{agentId}`. Tenant-scoped management will stay under `/api/tenants/{tenantId}/agents` with sibling detail/update/delete routes such as `/api/tenants/{tenantId}/agents/{agentId}`.

   Alternative considered: unify everything under `/api/agents/{agentId}` plus query parameters for scope. That is rejected because the existing tenant-aware permission flow and frontend mental model already depend on explicit route scope, and flattening the routes would make authorization and resource ownership less obvious.

2. **Use a shared paged result contract for list endpoints.**

   The current list APIs return raw arrays, which is not enough for pagination. Both internal and tenant list endpoints will move to a consistent paged response shape containing `items`, `page`, `pageSize`, `totalCount`, and `totalPages`, while still accepting `status` and `search` plus new paging inputs such as `page` and `pageSize`.

   Alternative considered: keep array responses and expose pagination only through response headers. That is rejected because the frontend already uses JSON DTOs directly and needs explicit metadata in the same contract to keep the view logic simple and testable.

3. **Implement delete as soft delete using existing lifecycle fields.**

   Agent deletion will mark `Status = Deleted`, set `DeletedAt`, and update modification metadata instead of physically removing the row. Normal list and detail queries for active management screens will exclude deleted agents by default.

   Alternative considered: hard delete the agent row. That is rejected because the entity already models deletion state, and agents have related records such as knowledge folders/files and one-to-one data that would make hard delete riskier for data integrity and future audit expectations.

4. **Split list and detail DTOs while reusing the current service boundary.**

   The application layer will keep `IAgentCatalogService` as the main orchestration point, but it will add methods for detail, update, and delete plus paged list results. A dedicated detail DTO will include fields the detail/edit UI needs beyond the current card summary, while the list DTO remains optimized for paginated grids.

   Alternative considered: reuse the existing list item DTO for detail screens. That is rejected because the detail flow needs timestamps and mutation-related fields that would either bloat every list response or force the frontend to infer unavailable state.

5. **Keep tenant selection in the frontend as the source of tenant context.**

   The UI will continue loading tenants separately, and tenant CRUD/detail requests will always use the selected tenant id already held in view state. After create, update, or delete, the page will re-fetch the current result page with the same filters and tenant selection.

   Alternative considered: create a large bootstrap or mutation response that fully rehydrates the whole workspace. That is deferred because the current page already composes small resource calls cleanly, and keeping focused refreshes reduces coupling between backend responses and UI layout.

## Risks / Trade-offs

- **Soft-deleted rows could still leak into existing queries** -> Centralize deleted-row filtering in repository methods used by management endpoints.
- **Changing list contracts from arrays to paged objects will touch frontend and backend together** -> Update API client types and views in one slice, and keep response naming identical across internal and tenant routes.
- **Users may delete the last item on a page and land on an empty page** -> Refresh after mutation and step back one page when the current page becomes invalid after deletion.
- **Permission behavior may differ between global admin and tenant-scoped roles** -> Keep backend permission evaluation as the source of truth and cover both internal and tenant routes with authorization tests.
- **Detail/edit scope checks could be accidentally bypassed by shared repository methods** -> Require repository lookups to include both `agentId` and expected scope/tenant predicates.

## Migration Plan

1. Add paging request/response DTOs plus detail/update/delete service contracts in the application layer.
2. Extend the repository and EF query logic so list endpoints exclude soft-deleted agents and return filtered paged results.
3. Add scope-specific detail, update, and delete API endpoints for internal and tenant-scoped agents.
4. Implement soft-delete and modification metadata updates in the service layer.
5. Update frontend API clients and the admin agent workspace to support paged lists, detail presentation, edit flows, and delete confirmation.
6. Verify internal-admin management, tenant-scoped management, filter persistence, pagination behavior, and unauthorized access paths.

Rollback is to remove the new detail/update/delete endpoints and frontend flows, then restore the old list-response contract if the feature has not yet been consumed externally. If soft-deleted rows were created before rollback, the team should decide whether to reactivate them or leave them hidden from the old UI.

## Open Questions

- Should deleting an agent immediately hide it forever from the UI, or does the product need a future recycle-bin or restore flow?
- Should tenant-scoped edit/delete remain available to any user with `agent.update` and `agent.delete`, or does the admin workspace intend to reserve those actions for higher-trust roles only?
