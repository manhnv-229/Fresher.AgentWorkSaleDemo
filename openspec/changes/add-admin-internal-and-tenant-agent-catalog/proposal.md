## Why

After login, the admin user still lacks a usable agent management workspace that separates internal agents from tenant-owned agents. The next step is to let admins browse internal agents, create admin-only agents, and switch between tenants to see each tenant's dedicated agent list from one dashboard flow.

## What Changes

- Add an authenticated agent catalog experience that opens after admin login.
- Add an internal agent list that is visible only to admin users.
- Add an admin-only create-agent flow for internal agents.
- Add a tenant sidebar that lists available tenants or business units for the current admin.
- Add tenant selection behavior that loads the agent list belonging to the selected tenant.
- Extend backend agent data and APIs so the system can distinguish internal agents from tenant-scoped agents.
- Return enough tenant and agent metadata for the frontend to render the admin dashboard without hard-coded mock data.

## Capabilities

### New Capabilities

- `admin-agent-catalog`: Admin-facing agent workspace with internal agents, tenant navigation, and tenant-specific agent lists.

### Modified Capabilities

- None.

## Impact

- Affected projects: `frontend`, `backend/Demo.Api`, `backend/Demo.Domain`, and `backend/Demo.Infrastructure`.
- Affected API surface: authenticated tenant/agent endpoints will expand to support internal-agent listing and creation plus admin dashboard data needs.
- Affected persistence: the agent model and seed/demo data will need a way to represent internal admin-only agents separately from tenant-owned agents.
- Authorization impact: only admin-level users should see or create internal agents, while tenant agent visibility remains scoped to the chosen tenant.
