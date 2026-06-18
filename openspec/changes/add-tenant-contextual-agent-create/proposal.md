## Why

The admin workspace already separates internal agents from tenant agents, but the create flow still needs a clearer tenant-aware entry point so admins do not have to think about scope switching when adding an agent for a tenant. We need the create action to follow the currently selected tenant context now so tenant onboarding and day-to-day agent management become faster and less error-prone.

## What Changes

- Add a tenant-scoped create-agent button directly in each tenant agent view of the admin workspace.
- Make the tenant create flow automatically bind the new agent to the tenant currently selected in the sidebar.
- Keep the internal create flow separate so creating an internal agent does not accidentally use tenant context.
- Refresh the selected tenant's agent list after a successful create so the new agent appears immediately in the same tenant view.

## Capabilities

### New Capabilities

- None.

### Modified Capabilities

- `admin-agent-catalog`: The admin agent catalog will expose a context-aware create action so creating an agent from a selected tenant view always creates a tenant-scoped agent for that tenant.

## Impact

- Affected frontend area: `frontend` agent catalog view, especially tenant workspace actions and create modal state.
- Affected API usage: the UI will consistently call `POST /api/tenants/{tenantId}/agents` when the active workspace is a selected tenant and will continue using the internal create endpoint only from the internal workspace.
- Backend impact: no new capability is required, but existing tenant create behavior becomes an explicit dependency of the admin create UX.
- Authorization impact: tenant-scoped create actions must continue using the selected tenant context and existing `agent.create` permission checks.
