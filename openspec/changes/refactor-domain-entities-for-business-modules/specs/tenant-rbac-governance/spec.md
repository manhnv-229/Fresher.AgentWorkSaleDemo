## ADDED Requirements

### Requirement: User accounts support active, inactive, and locked access control states
The system SHALL model user account state separately from session state so login, password change, and account lock rules can be enforced consistently.

#### Scenario: Locked account cannot authenticate
- **WHEN** a user account is marked as locked or otherwise disabled for login
- **THEN** the authentication flow rejects login even if the password is correct

#### Scenario: Password change retains account identity
- **WHEN** a user changes their password
- **THEN** the user account entity remains the source of truth for current password state and account status

### Requirement: Tenant membership is distinct from role assignment
The system SHALL model tenant membership separately from role assignment so a user can belong to a tenant, be disabled in that tenant, and hold one or more tenant-specific roles independently.

#### Scenario: Membership exists without edit permissions
- **WHEN** a user belongs to a tenant but has only read-oriented staff permissions
- **THEN** the membership entity remains valid without implying manager privileges

#### Scenario: Inactive membership blocks tenant-scoped access
- **WHEN** a user's tenant membership is inactive or locked
- **THEN** tenant-scoped authorization cannot treat that user as an active member of the tenant

### Requirement: RBAC assignments support global admin and tenant-scoped roles
The system SHALL support both global role assignments and tenant-specific role assignments in the domain model.

#### Scenario: Global admin role applies without tenant binding
- **WHEN** a user is assigned the system admin role
- **THEN** the role assignment can exist without binding to a specific tenant

#### Scenario: Tenant manager role stays within one tenant
- **WHEN** a user is assigned a tenant manager role
- **THEN** the role assignment stores the tenant context required to prevent that authority from applying to other tenants

### Requirement: Role and permission entities support business personas
The system SHALL model roles and permissions so the system admin, tenant manager, and staff personas can be composed through RBAC rather than hard-coded user types.

#### Scenario: Staff permissions are limited by ownership rules
- **WHEN** a staff role is evaluated for agent modification
- **THEN** the system can combine the assigned permissions with creator ownership from the agent entity to restrict edits to self-created agents

#### Scenario: Tenant manager permissions apply across tenant agents
- **WHEN** a tenant manager role is evaluated in its tenant
- **THEN** the system can authorize actions across agents and documents within that tenant regardless of creator
