## ADDED Requirements

### Requirement: Admin users can open an internal agent catalog after login
The system SHALL present an authenticated admin workspace where internal agents are listed separately from tenant-owned agents.

#### Scenario: Admin opens internal agent catalog
- **WHEN** an authenticated admin user navigates to the agent management workspace after login
- **THEN** the system shows the internal agent context as a distinct list of internal agents

#### Scenario: Non-admin cannot access internal agent catalog
- **WHEN** an authenticated user without the required global admin permission requests the internal agent catalog
- **THEN** the system denies access to internal-agent data

### Requirement: Internal agents are admin-only records
The system SHALL store internal agents separately from tenant agents and SHALL allow only admin-authorized users to create them.

#### Scenario: Admin creates internal agent
- **WHEN** an authenticated admin submits a valid internal-agent create request
- **THEN** the system stores the agent as an internal-scoped agent
- **AND** the new agent appears in the internal agent list

#### Scenario: Tenant-scoped create request cannot create internal agent
- **WHEN** a client calls the tenant-scoped create API for a tenant agent
- **THEN** the created record is associated to that tenant and is not visible in the internal agent list

### Requirement: Tenant sidebar lists available tenants for the admin workspace
The system SHALL provide the admin workspace with a sidebar list of tenants or business units that can be selected to view tenant-owned agents.

#### Scenario: Admin loads tenant sidebar
- **WHEN** an authenticated admin opens the agent management workspace
- **THEN** the system returns the available tenant list with stable identifiers and display names for sidebar rendering

#### Scenario: Sidebar excludes internal agents
- **WHEN** the tenant sidebar is rendered
- **THEN** the internal agent context is not represented as a business-unit tenant item

### Requirement: Selecting a tenant shows only that tenant's agents
The system SHALL load and display the agent list for the tenant selected in the sidebar.

#### Scenario: Admin selects a tenant
- **WHEN** an authenticated admin selects a tenant from the sidebar
- **THEN** the system loads agents associated with that tenant only

#### Scenario: Agents from other tenants are excluded
- **WHEN** the system returns the selected tenant's agent list
- **THEN** the response does not include internal agents or agents belonging to other tenants

### Requirement: Agent APIs distinguish internal scope from tenant scope
The system SHALL expose API behavior that lets clients fetch internal agents separately from tenant-scoped agents.

#### Scenario: Internal-agent API returns internal agents only
- **WHEN** an admin client requests the internal-agent list API
- **THEN** the system returns internal-scoped agents only

#### Scenario: Tenant-agent API returns tenant agents only
- **WHEN** a client requests `GET /api/tenants/{tenantId}/agents`
- **THEN** the system returns tenant-scoped agents for the requested tenant only

### Requirement: Agent create flows preserve visibility boundaries
The system SHALL preserve the visibility boundary between admin-only internal agents and tenant-owned agents after creation.

#### Scenario: Internal agent stays admin-only after creation
- **WHEN** an admin creates an internal agent successfully
- **THEN** only admin-authorized internal-agent views can retrieve that agent

#### Scenario: Tenant agent stays within selected tenant after creation
- **WHEN** a tenant-scoped agent is created for a tenant
- **THEN** that agent is retrievable from that tenant's list and not from another tenant's list
