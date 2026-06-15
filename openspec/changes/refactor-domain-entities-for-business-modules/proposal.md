## Why

The current domain entities cover the initial authentication, tenant, and agent catalog flows, but they do not yet model several required business modules such as agent ownership rules, tenant-scoped RBAC roles, knowledge-base folders/files, account lock state, and audit history. This change is needed now so the backend entity model becomes a stable foundation for the next CRUD, authorization, and document-management features instead of forcing ad hoc fields and inconsistent relationships later.

## What Changes

- Refactor core domain entities so `Agent`, `Tenant`, `User`, `Role`, and related joins match the required business fields, statuses, and tenant boundaries.
- Add explicit agent business metadata such as agent code, creator, publishable lifecycle state, and ownership information required for staff-vs-manager permissions.
- Extend authentication/account entities to support logout, refresh-token sessions, password changes, and account lock or inactive states.
- Refine RBAC entities so global admin, tenant manager, and staff assignments can be represented cleanly and enforced per tenant.
- Add new domain entities for agent knowledge management, including hierarchical folders and documents with creator, file type, and movement metadata.
- Add new domain entities for audit logging so login, agent changes, file actions, and permission actions can be persisted consistently.
- Define the entity relationships and invariants needed for tenant isolation, creator-based permissions, soft deletion, and search/filter support.
- **BREAKING**: Existing entity properties, enums, and relationships may be renamed, split, or replaced to align with the requested business terminology and lifecycle states.

## Capabilities

### New Capabilities

- `agent-lifecycle-domain-model`: Domain requirements for agent identity, lifecycle status, tenant ownership, creator ownership, and business metadata used by agent CRUD and filtering.
- `tenant-rbac-governance`: Domain requirements for login/account state, tenant membership, global and tenant-scoped roles, and creator-aware authorization boundaries.
- `agent-knowledge-tree`: Domain requirements for hierarchical folders, agent documents, supported file metadata, and tenant-scoped knowledge organization.
- `audit-log-tracking`: Domain requirements for recording user actions such as login, agent changes, file operations, and permission changes with actor and request metadata.

### Modified Capabilities

- None.

## Impact

- Affected backend layers: `backend/src/Domain`, `backend/src/Application`, `backend/src/Infrastructure`, and the API contracts that depend on these entities.
- Affected persistence model: entity classes, EF Core configuration, migrations, seed data, and repository query filters for agents, tenants, users, files, and logs.
- Affected API behavior: future agent, auth, tenant, permission, document, and audit endpoints will depend on the refactored entity model and updated enums.
- Cross-cutting impact: validation, authorization checks, pagination/search filters, and audit creation will need to follow the new ownership and tenant-isolation rules.
