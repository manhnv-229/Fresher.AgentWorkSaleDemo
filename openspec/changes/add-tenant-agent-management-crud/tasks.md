## 1. Application Contracts and Persistence

- [x] 1.1 Add paging request/response DTOs and agent detail/update command models in the application layer.
- [x] 1.2 Extend repository contracts and EF-backed queries to fetch scoped agent details and paged list results while excluding soft-deleted agents.
- [x] 1.3 Implement soft-delete and modification timestamp/user tracking for agent update and delete flows.

## 2. Backend API and Authorization

- [x] 2.1 Add internal-agent detail, update, and delete endpoints under `api/admin/agents/internal/{agentId}`.
- [x] 2.2 Add tenant-agent detail, update, and delete endpoints under `api/tenants/{tenantId}/agents/{agentId}`.
- [x] 2.3 Update internal-agent and tenant-agent list endpoints to accept `page` and `pageSize` and return paged payloads.
- [x] 2.4 Keep validation and permission errors consistent for invalid status values, scope mismatches, missing agents, and unauthorized access.

## 3. Frontend Agent Management Workspace

- [x] 3.1 Update frontend agent API clients and types to consume paged responses plus detail/update/delete operations.
- [x] 3.2 Add agent detail and edit UI flows for internal and tenant-scoped agents.
- [x] 3.3 Add delete confirmation behavior and optimistic refresh handling for the active scope.
- [x] 3.4 Preserve selected tenant, search text, status filter, and current page when refreshing lists after mutations.

## 4. Verification

- [x] 4.1 Verify internal-agent and tenant-agent lists page correctly with search and status filters applied.
- [x] 4.2 Verify authorized users can open, update, and soft-delete agents only within the matching internal or tenant scope.
- [x] 4.3 Verify deleted agents disappear from normal list/detail flows and that page refresh logic handles deleting the last item on a page.
- [x] 4.4 Run backend and frontend builds/tests available in the repo and capture any remaining environment or coverage gaps.
