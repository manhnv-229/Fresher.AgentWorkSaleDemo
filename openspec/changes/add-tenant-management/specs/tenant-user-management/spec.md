## ADDED Requirements

### Requirement: Admins can manage tenant membership
The system SHALL let authorized admins list tenant members, add a user to a tenant, change membership status, and remove a user from that tenant.

#### Scenario: Admin lists tenant members
- **WHEN** an authorized admin requests the users for a tenant
- **THEN** the system returns the membership list for that tenant only
- **AND** each item includes user identity and membership status needed by the admin UI

#### Scenario: Admin adds a user to a tenant
- **WHEN** an authorized admin adds an eligible user to a tenant
- **THEN** the system creates a tenant membership for that user under that tenant

#### Scenario: Admin updates membership status
- **WHEN** an authorized admin changes a tenant membership to `Active`, `Inactive`, or `Locked`
- **THEN** the system saves the new membership status for that tenant-user pair only

#### Scenario: Admin removes a user from a tenant
- **WHEN** an authorized admin removes a user from a tenant
- **THEN** the system removes or deactivates that tenant membership according to the platform's membership-removal rule

### Requirement: Tenant membership stays tenant-scoped and unique
The system SHALL treat tenant membership as a unique relationship between one user and one tenant.

#### Scenario: Duplicate membership is rejected
- **WHEN** an admin tries to add a user who already belongs to the tenant
- **THEN** the system rejects the request instead of creating a duplicate membership

#### Scenario: Membership change does not affect other tenants
- **WHEN** an admin updates or removes a user's membership in one tenant
- **THEN** the system does not alter that user's memberships in other tenants

#### Scenario: Unknown user or tenant membership is rejected
- **WHEN** an admin targets a missing user, missing tenant, or missing tenant membership
- **THEN** the system returns a validation-style or not-found style result appropriate to the missing resource

### Requirement: Membership status affects tenant access
The system SHALL use tenant membership status as part of tenant access control.

#### Scenario: Active membership allows tenant-scoped authorization checks
- **WHEN** a user has an `Active` membership in the requested tenant and the required permissions
- **THEN** the system can authorize tenant-scoped operations for that tenant

#### Scenario: Inactive membership blocks tenant-scoped access
- **WHEN** a user has `Inactive` or `Locked` membership in the requested tenant
- **THEN** the system denies tenant-scoped access that requires active membership

#### Scenario: Removed membership no longer grants access
- **WHEN** a user's membership is removed from a tenant
- **THEN** subsequent tenant-scoped permission checks do not treat that user as a member of that tenant
