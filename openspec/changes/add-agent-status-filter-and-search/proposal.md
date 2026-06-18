## Why

The current admin agent catalog becomes harder to use as the number of internal and tenant agents grows because the list is always fully expanded with no filtering tools. Admin users need a fast way to narrow agent lists by publishing status and keyword search so they can locate the right agent without scanning every card manually.

## What Changes

- Add a status filter for internal-agent and tenant-agent lists in the admin workspace.
- Add keyword search for internal-agent and tenant-agent lists in the admin workspace.
- Extend agent list APIs to accept optional filter inputs for `status` and search text.
- Return filtered agent results while keeping internal and tenant scopes separated.
- Add frontend filter controls, empty states, and request flow updates so the catalog reflects the active search and status criteria.

## Capabilities

### New Capabilities

- None.

### Modified Capabilities

- `admin-agent-catalog`: Agent list browsing will support status filtering and keyword search across internal and tenant-specific catalog views.

## Impact

- Affected projects: `frontend`, `backend/Demo.Api`, and `backend/Demo.Infrastructure`.
- Affected API surface: `GET /api/admin/agents/internal` and `GET /api/tenants/{tenantId}/agents` will accept optional filtering inputs.
- Affected user workflow: admins can refine agent lists without scrolling through the full catalog.
- Persistence impact: no schema change is expected; filtering will use existing agent fields such as `status`, `name`, `description`, and `role`.
