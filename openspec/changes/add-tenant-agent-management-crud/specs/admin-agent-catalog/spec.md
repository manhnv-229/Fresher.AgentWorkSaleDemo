## ADDED Requirements

### Requirement: Admin users can open agent details within the active scope
The system SHALL let authorized users open a detailed view for an internal agent or a tenant-scoped agent without crossing scope boundaries.

#### Scenario: Admin opens internal agent details
- **WHEN** an authorized admin requests an internal agent by id from the internal-agent workspace
- **THEN** the system returns the internal agent detail payload for that id only
- **AND** the response includes the agent's core fields, scope, status, and timestamps needed by the detail UI

#### Scenario: Admin opens tenant agent details
- **WHEN** an authorized user requests an agent detail for the selected tenant
- **THEN** the system returns the tenant-scoped agent detail only when the agent belongs to that tenant

#### Scenario: Scope mismatch is rejected
- **WHEN** a client requests an internal-agent detail from a tenant route or requests a tenant agent from another tenant route
- **THEN** the system denies the request with a not-found style result that does not expose cross-scope data

### Requirement: Admin users can update agent metadata within scope
The system SHALL let authorized users update an agent's editable metadata while preserving its scope and tenant ownership.

#### Scenario: Admin updates internal agent metadata
- **WHEN** an authorized admin submits a valid update for an internal agent
- **THEN** the system saves the updated name, role, description, icon, and status
- **AND** the agent remains internal-scoped with no tenant assignment

#### Scenario: Admin updates tenant agent metadata
- **WHEN** an authorized user submits a valid update for an agent under `PUT /api/tenants/{tenantId}/agents/{agentId}`
- **THEN** the system updates only that tenant's agent
- **AND** the agent remains associated with the same tenant

#### Scenario: Invalid update is rejected
- **WHEN** a client submits an update with missing required fields or an unsupported status value
- **THEN** the system rejects the request with a validation error

### Requirement: Agent delete operations are soft-deleted and scope-safe
The system SHALL support deleting agents through scope-specific routes by marking them deleted instead of removing related data immediately.

#### Scenario: Internal agent is soft-deleted
- **WHEN** an authorized admin deletes an internal agent
- **THEN** the system marks the agent with deleted lifecycle data
- **AND** the agent is excluded from normal internal-agent list results

#### Scenario: Tenant agent is soft-deleted
- **WHEN** an authorized user deletes an agent from a tenant-scoped route
- **THEN** the system marks only that tenant's agent as deleted
- **AND** the deleted agent no longer appears in the selected tenant's normal list results

#### Scenario: Deleted agent cannot be managed from another scope
- **WHEN** a client attempts to delete an agent through a route that does not match its scope or tenant
- **THEN** the system returns a not-found style result and does not mutate the agent

### Requirement: Agent lists support pagination with filters
The system SHALL return paginated results for internal-agent and tenant-agent list queries while preserving existing status and keyword filters.

#### Scenario: Internal list returns a page of results
- **WHEN** an authorized admin requests the internal-agent list with `page` and `pageSize`
- **THEN** the system returns only the requested slice of internal agents
- **AND** the response includes paging metadata with the current page, page size, total item count, and total page count

#### Scenario: Tenant list returns a page of results
- **WHEN** an authorized user requests `GET /api/tenants/{tenantId}/agents` with paging and filters
- **THEN** the system returns only agents for that tenant that match the active filters
- **AND** the response includes paging metadata for that filtered tenant result set

#### Scenario: Out-of-range page is handled consistently
- **WHEN** a client requests a page beyond the available result range
- **THEN** the system returns an empty items collection with valid paging metadata instead of mixing results from another page

### Requirement: Agent management UI keeps tenant context after mutations
The system SHALL refresh the active agent list and preserve the current internal-or-tenant context after create, update, or delete actions.

#### Scenario: Internal list refreshes after mutation
- **WHEN** an admin creates, updates, or deletes an internal agent
- **THEN** the UI refreshes the internal-agent page using the current search, status filter, and pagination state

#### Scenario: Tenant list refreshes after mutation
- **WHEN** a user creates, updates, or deletes an agent for the selected tenant
- **THEN** the UI refreshes that tenant's current result page using the active filters
- **AND** the selected tenant remains active in the workspace

#### Scenario: Detail actions return user to consistent list state
- **WHEN** a user saves or deletes an agent from the detail view
- **THEN** the workspace returns to a list state that reflects the updated data and current tenant context
