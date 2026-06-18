## ADDED Requirements

### Requirement: Admins can manage tenant lifecycle from the tenant catalog
The system SHALL let authorized admins create, view, update, and lock tenants from a dedicated tenant-management workflow.

#### Scenario: Admin creates a tenant
- **WHEN** an authorized admin submits a valid tenant name and code
- **THEN** the system creates a tenant with `Active` status and stable tenant identity metadata

#### Scenario: Admin opens tenant detail
- **WHEN** an authorized admin requests a tenant by id from the tenant catalog
- **THEN** the system returns tenant detail including name, code, status, and lifecycle timestamps needed by the admin UI

#### Scenario: Admin updates tenant metadata
- **WHEN** an authorized admin submits a valid update for an existing tenant
- **THEN** the system saves the updated tenant metadata without changing the tenant's identity

#### Scenario: Admin locks a tenant
- **WHEN** an authorized admin locks a tenant
- **THEN** the system changes the tenant status to `Locked`
- **AND** the tenant remains visible for admin management and audit-oriented workflows

### Requirement: Tenant updates validate identity and lifecycle constraints
The system SHALL reject tenant-management requests that would violate uniqueness, required fields, or valid lifecycle transitions.

#### Scenario: Duplicate tenant code is rejected
- **WHEN** an admin attempts to create or update a tenant using a code that already belongs to another tenant
- **THEN** the system rejects the request with a validation-style error

#### Scenario: Unknown tenant cannot be updated
- **WHEN** an admin submits an update or lock request for a tenant id that does not exist
- **THEN** the system returns a not-found style result

#### Scenario: Invalid tenant payload is rejected
- **WHEN** an admin submits a tenant request with missing required fields or unsupported status input
- **THEN** the system rejects the request without mutating tenant data

### Requirement: Locked tenants are preserved but restricted
The system SHALL preserve locked tenants for visibility and historical integrity while preventing tenant state from being treated as active.

#### Scenario: Locked tenant remains visible in admin catalog
- **WHEN** an authorized admin lists or views tenants after one has been locked
- **THEN** the locked tenant still appears with `Locked` status

#### Scenario: Locked tenant is excluded from active-only selectors
- **WHEN** a workflow requests tenants intended for active operational selection
- **THEN** the system excludes tenants whose status is `Locked` unless the workflow explicitly asks for them

#### Scenario: Locked tenant cannot be used as if it were active
- **WHEN** a tenant-scoped operation requires the tenant to be active
- **THEN** the system denies the operation for a locked tenant
