## Why

The system currently supports listing and creating tenants, but admins still cannot manage the full tenant lifecycle or control which users belong to each tenant. We need a complete tenant-management capability now so the platform can safely onboard tenants, lock them when needed, manage tenant membership, and enforce data isolation consistently across tenant-owned resources.

## What Changes

- Add admin tenant management flows for creating, viewing, updating, and locking tenants without deleting historical tenant data.
- Add tenant user management so authorized admins can list tenant members, add users to a tenant, change membership status, and remove users from that tenant.
- Define tenant data-partitioning rules so tenant-owned records remain scoped to their tenant in list, detail, mutation, and authorization flows.
- Extend backend service, repository, and API contracts to return tenant detail and tenant membership data needed by the admin UI.
- Expand the frontend admin workspace to manage tenants and tenant users from a dedicated tenant-management experience.

## Capabilities

### New Capabilities

- `admin-tenant-management`: Admins can create, view, update, and lock tenants while preserving lifecycle metadata and explicit tenant status.
- `tenant-user-management`: Admins can manage which users belong to a tenant and control whether those memberships stay active.
- `tenant-data-partitioning`: Tenant-owned data remains isolated by tenant across reads, writes, and authorization-sensitive flows.

### Modified Capabilities

- `auth-access-control`: Permission requirements and tenant-aware authorization behavior will expand to cover tenant update, tenant lock, and tenant user-management actions.

## Impact

- Affected backend areas: `backend/src/API`, `backend/src/Application`, `backend/src/Domain`, and `backend/src/Infrastructure`.
- Affected frontend area: `frontend` admin workspace for tenant management and tenant-user membership screens.
- Affected APIs: `/api/tenants` will expand beyond list/create, and new tenant membership endpoints will be introduced under tenant-scoped admin routes.
- Persistence impact: tenant and membership queries will need lifecycle-aware status updates and data-shaping for tenant detail and membership management.
- Authorization impact: existing global and tenant-scoped permission checks must remain the source of truth for tenant-management and membership operations.
