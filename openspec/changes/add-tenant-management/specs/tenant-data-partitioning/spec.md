## ADDED Requirements

### Requirement: Tenant-owned data is always filtered by tenant context
The system SHALL scope tenant-owned reads and writes to the requested tenant so data from one tenant never appears in another tenant's management flow.

#### Scenario: Tenant-scoped list excludes other tenants
- **WHEN** a client requests a tenant-scoped list of tenant-owned records
- **THEN** the system returns only records whose tenant ownership matches the requested tenant

#### Scenario: Tenant-scoped detail excludes cross-tenant records
- **WHEN** a client requests a tenant-owned record by id through a tenant route
- **THEN** the system returns that record only if it belongs to the requested tenant

#### Scenario: Tenant-scoped mutation excludes cross-tenant records
- **WHEN** a client updates or deletes a tenant-owned record through a tenant route
- **THEN** the system mutates that record only when its tenant ownership matches the requested tenant

### Requirement: Tenant lifecycle state participates in operational data access
The system SHALL use tenant lifecycle state to determine whether tenant-owned operational changes are allowed.

#### Scenario: Active tenant allows operational mutations
- **WHEN** a tenant-owned mutation targets a tenant whose status is `Active`
- **THEN** the system can continue evaluating the remaining validation and authorization rules for that mutation

#### Scenario: Locked tenant blocks operational mutations
- **WHEN** a tenant-owned mutation targets a tenant whose status is `Locked`
- **THEN** the system rejects the mutation even if the caller otherwise has permission

#### Scenario: Admin tenant detail still works for locked tenant
- **WHEN** an authorized admin requests tenant-management detail for a locked tenant
- **THEN** the system still returns the locked tenant's administrative data

### Requirement: Tenant-aware queries and services remain the enforcement boundary
The system SHALL enforce tenant partitioning in application and persistence layers rather than depending only on UI state or route naming.

#### Scenario: Shared repository query includes tenant predicate
- **WHEN** a repository method is used for tenant-owned data access
- **THEN** its query constrains results by tenant ownership when the operation is tenant-scoped

#### Scenario: Service rejects mismatched tenant references
- **WHEN** a service receives a tenant id and a tenant-owned resource id that do not belong together
- **THEN** the service returns a not-found style result without exposing cross-tenant data

#### Scenario: Authorization context and data context must agree
- **WHEN** a request passes permission authorization for one tenant but targets data owned by another tenant
- **THEN** the system still denies the data access or mutation
