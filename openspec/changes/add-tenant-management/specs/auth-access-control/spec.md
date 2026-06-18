## MODIFIED Requirements

### Requirement: Permissions are database-backed and grouped by code
The system SHALL persist permission definitions with unique permission codes and group names.

#### Scenario: Default permission codes are seeded
- **WHEN** the authentication and authorization schema is initialized
- **THEN** the system contains tenant, agent, user, and role permission codes including `tenant.view`, `tenant.create`, `tenant.update`, `tenant.delete`, `user.view`, `user.invite`, and `user.update`

### Requirement: Global roles authorize global permissions
The system SHALL support roles whose `tenant_id` is null for global authorization such as `SystemAdmin`.

#### Scenario: SystemAdmin accesses global tenant API
- **WHEN** a user has a global role with `tenant.view`
- **THEN** the user can access `GET /api/tenants`

#### Scenario: SystemAdmin creates tenant
- **WHEN** a user has a global role with `tenant.create`
- **THEN** the user can access `POST /api/tenants`

#### Scenario: SystemAdmin updates tenant
- **WHEN** a user has a global role with `tenant.update`
- **THEN** the user can access tenant detail, update, and lock actions exposed by the tenant-management API

### Requirement: Tenant roles authorize tenant-scoped permissions
The system SHALL support different role assignments for the same user across different tenants.

#### Scenario: User has permission in one tenant only
- **WHEN** a user has `AgentManager` in Tenant 1 and `AgentViewer` in Tenant 2
- **THEN** the user can create agents in Tenant 1
- **AND** the user cannot create agents in Tenant 2

#### Scenario: Tenant membership is required for tenant role authorization
- **WHEN** a user has no active membership in the requested tenant
- **THEN** the system denies tenant-scoped permission checks for that tenant

#### Scenario: Inactive membership blocks tenant user operations
- **WHEN** a user attempts a tenant-scoped action after their membership in that tenant becomes `Inactive` or `Locked`
- **THEN** the system denies tenant-scoped permission checks for that tenant

### Requirement: Demo tenant APIs validate global authorization
The system SHALL expose demo tenant endpoints protected by tenant permission codes.

#### Scenario: List tenants requires tenant view permission
- **WHEN** a client calls `GET /api/tenants`
- **THEN** the system requires a valid access token and `tenant.view`

#### Scenario: Create tenant requires tenant create permission
- **WHEN** a client calls `POST /api/tenants`
- **THEN** the system requires a valid access token and `tenant.create`

#### Scenario: Update tenant requires tenant update permission
- **WHEN** a client calls a tenant detail, update, or lock endpoint under `/api/tenants/{tenantId}`
- **THEN** the system requires a valid access token and `tenant.update`

#### Scenario: Tenant membership read requires user view permission
- **WHEN** a client calls `GET /api/tenants/{tenantId}/users`
- **THEN** the system requires a valid access token, tenant-aware authorization, and `user.view`

#### Scenario: Tenant membership add requires user invite permission
- **WHEN** a client calls a tenant membership create endpoint under `/api/tenants/{tenantId}/users`
- **THEN** the system requires a valid access token, tenant-aware authorization, and `user.invite`

#### Scenario: Tenant membership update requires user update permission
- **WHEN** a client updates or removes a tenant membership under `/api/tenants/{tenantId}/users/{userId}`
- **THEN** the system requires a valid access token, tenant-aware authorization, and `user.update`
