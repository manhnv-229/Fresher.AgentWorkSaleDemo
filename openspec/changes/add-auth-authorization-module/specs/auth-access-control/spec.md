## ADDED Requirements

### Requirement: User login issues access and refresh tokens
The system SHALL allow an active user to authenticate with email and password and receive a JWT access token and refresh token.

#### Scenario: Successful login
- **WHEN** an active user submits a valid email and password to `POST /api/auth/login`
- **THEN** the system returns a JWT access token, a raw refresh token, and token expiration metadata

#### Scenario: Invalid credentials are rejected
- **WHEN** a user submits an unknown email or incorrect password to `POST /api/auth/login`
- **THEN** the system rejects the request without issuing tokens

#### Scenario: Inactive user cannot login
- **WHEN** a user whose status is not `Active` submits valid credentials to `POST /api/auth/login`
- **THEN** the system rejects the request without issuing tokens

### Requirement: Passwords are verified from BCrypt hashes
The system MUST store user passwords as BCrypt hashes and MUST verify login passwords against those hashes.

#### Scenario: Plain text password is never stored
- **WHEN** a user record is created or seeded for authentication
- **THEN** the persisted `password_hash` value is a BCrypt hash and not the raw password

### Requirement: JWT access tokens contain identity claims only
The system SHALL issue short-lived JWT access tokens containing only user identity claims and token metadata.

#### Scenario: Access token payload excludes permissions
- **WHEN** the system generates an access token after login or refresh
- **THEN** the token includes identity claims such as user id, email, display name, token id, and expiration
- **AND** the token does not include role lists or permission lists

#### Scenario: Expired access token is rejected
- **WHEN** a client calls a protected API with an expired access token
- **THEN** the system rejects the request as unauthenticated

### Requirement: Current user endpoint returns authenticated user profile
The system SHALL expose `GET /api/auth/me` for authenticated users to retrieve their current user profile.

#### Scenario: Authenticated user requests profile
- **WHEN** a client calls `GET /api/auth/me` with a valid access token
- **THEN** the system returns the current user's id, email, full name, and status

#### Scenario: Anonymous user requests profile
- **WHEN** a client calls `GET /api/auth/me` without a valid access token
- **THEN** the system rejects the request as unauthenticated

### Requirement: Refresh tokens are stored as hashes
The system MUST generate refresh tokens as random secrets and MUST store only SHA-256 hashes of refresh tokens in the database.

#### Scenario: Refresh token is persisted after login
- **WHEN** a user logs in successfully
- **THEN** the system stores a refresh token record with `token_hash`, `expires_at`, `created_at`, and optional creation IP metadata
- **AND** the raw refresh token is not stored

### Requirement: Refresh token rotation renews sessions
The system SHALL allow a valid refresh token to obtain a new access token and a new refresh token while revoking the old refresh token.

#### Scenario: Valid refresh token is rotated
- **WHEN** a client submits a valid, unexpired, unrevoked refresh token to `POST /api/auth/refresh-token`
- **THEN** the system revokes the old refresh token
- **AND** the system stores a new refresh token hash linked by `replaced_by_token_hash`
- **AND** the system returns a new access token and raw refresh token

#### Scenario: Expired refresh token is rejected
- **WHEN** a client submits an expired refresh token to `POST /api/auth/refresh-token`
- **THEN** the system rejects the request without issuing new tokens

#### Scenario: Revoked refresh token is rejected
- **WHEN** a client submits a refresh token that already has `revoked_at` set
- **THEN** the system rejects the request without issuing new tokens

### Requirement: Logout revokes refresh token
The system SHALL revoke the submitted refresh token during logout.

#### Scenario: Valid logout revokes token
- **WHEN** a client submits a known refresh token to `POST /api/auth/logout`
- **THEN** the system sets revocation metadata on the matching refresh token record
- **AND** the token cannot be used for future refresh requests

#### Scenario: Logout with unknown token does not expose token existence
- **WHEN** a client submits an unknown refresh token to `POST /api/auth/logout`
- **THEN** the system completes without revealing whether the token existed

### Requirement: Permissions are database-backed and grouped by code
The system SHALL persist permission definitions with unique permission codes and group names.

#### Scenario: Default permission codes are seeded
- **WHEN** the authentication and authorization schema is initialized
- **THEN** the system contains tenant, agent, user, and role permission codes including `tenant.view`, `tenant.create`, `agent.view`, `agent.create`, `role.view`, and `role.assign`

### Requirement: Roles map to permissions dynamically
The system SHALL authorize users through roles and role-permission mappings stored in the database.

#### Scenario: Role permission change affects authorization
- **WHEN** a permission is added to or removed from a user's assigned role in the database
- **THEN** subsequent protected API requests use the updated permission assignment without controller code changes

### Requirement: Global roles authorize global permissions
The system SHALL support roles whose `tenant_id` is null for global authorization such as `SystemAdmin`.

#### Scenario: SystemAdmin accesses global tenant API
- **WHEN** a user has a global role with `tenant.view`
- **THEN** the user can access `GET /api/tenants`

#### Scenario: SystemAdmin creates tenant
- **WHEN** a user has a global role with `tenant.create`
- **THEN** the user can access `POST /api/tenants`

### Requirement: Tenant roles authorize tenant-scoped permissions
The system SHALL support different role assignments for the same user across different tenants.

#### Scenario: User has permission in one tenant only
- **WHEN** a user has `AgentManager` in Tenant 1 and `AgentViewer` in Tenant 2
- **THEN** the user can create agents in Tenant 1
- **AND** the user cannot create agents in Tenant 2

#### Scenario: Tenant membership is required for tenant role authorization
- **WHEN** a user has no active membership in the requested tenant
- **THEN** the system denies tenant-scoped permission checks for that tenant

### Requirement: Controllers use permission attributes instead of role attributes
The system SHALL protect permission-restricted APIs through permission codes rather than hard-coded role names.

#### Scenario: Permission-protected endpoint grants access
- **WHEN** a controller action requires `agent.create` and the authenticated user has `agent.create` for the requested tenant
- **THEN** the system allows the request

#### Scenario: Permission-protected endpoint denies access
- **WHEN** a controller action requires `agent.create` and the authenticated user does not have `agent.create` for the requested tenant
- **THEN** the system returns forbidden

### Requirement: Demo tenant APIs validate global authorization
The system SHALL expose demo tenant endpoints protected by tenant permission codes.

#### Scenario: List tenants requires tenant view permission
- **WHEN** a client calls `GET /api/tenants`
- **THEN** the system requires a valid access token and `tenant.view`

#### Scenario: Create tenant requires tenant create permission
- **WHEN** a client calls `POST /api/tenants`
- **THEN** the system requires a valid access token and `tenant.create`

### Requirement: Demo agent APIs validate tenant authorization
The system SHALL expose demo tenant-scoped agent endpoints protected by agent permission codes.

#### Scenario: List tenant agents requires agent view permission
- **WHEN** a client calls `GET /api/tenants/{tenantId}/agents`
- **THEN** the system requires a valid access token, active tenant membership unless satisfied by a global role, and `agent.view`

#### Scenario: Create tenant agent requires agent create permission
- **WHEN** a client calls `POST /api/tenants/{tenantId}/agents`
- **THEN** the system requires a valid access token, active tenant membership unless satisfied by a global role, and `agent.create`
