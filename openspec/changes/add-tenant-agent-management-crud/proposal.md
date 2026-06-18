## Why

The current admin agent workspace only supports browsing and creating agents, which leaves admins unable to complete the full lifecycle when agent inventories grow across multiple tenants. We need tenant-aware detail, update, delete, and paginated list flows now so admins can manage agents safely without relying on database edits or incomplete UI workarounds.

## What Changes

- Add agent detail views for both internal agents and tenant-scoped agents in the admin workspace.
- Add update flows so admins can edit core agent fields such as name, role, description, icon, and status.
- Add delete flows with explicit confirmation behavior for internal agents and tenant-scoped agents.
- Extend internal-agent and tenant-agent list APIs to support pagination metadata in addition to existing search and status filters.
- Preserve tenant isolation so tenant-scoped reads, edits, and deletes only affect the selected tenant while internal agents remain admin-only records.
- Expand frontend state and API integration so the admin workspace can switch tenants, open agent details, and refresh list results after create, update, or delete actions.

## Capabilities

### New Capabilities

- None.

### Modified Capabilities

- `admin-agent-catalog`: The admin agent catalog will support tenant-aware CRUD management, detail views, and paginated list browsing for internal and tenant-scoped agents.

## Impact

- Affected projects: `frontend`, `backend/src/API`, `backend/src/Application`, `backend/src/Domain`, and `backend/src/Infrastructure`.
- Affected API surface: admin internal-agent endpoints and tenant agent endpoints will add detail, update, delete, and pagination behavior.
- Persistence impact: agent queries and response contracts will expand to support detail payloads and paged list metadata, while keeping internal and tenant scope boundaries explicit.
- Authorization impact: all new write and detail operations must continue using permission-based checks and selected-tenant context instead of cross-tenant access.
