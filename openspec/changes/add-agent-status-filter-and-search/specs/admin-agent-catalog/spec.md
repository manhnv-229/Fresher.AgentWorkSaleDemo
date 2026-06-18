## ADDED Requirements

### Requirement: Internal agent list supports status filtering and keyword search
The system SHALL let admin users narrow the internal agent list by status and keyword search without mixing in tenant-scoped agents.

#### Scenario: Filter internal agents by status
- **WHEN** an admin requests the internal agent list with a valid status filter
- **THEN** the system returns only internal agents whose status matches that filter

#### Scenario: Search internal agents by keyword
- **WHEN** an admin requests the internal agent list with a search keyword
- **THEN** the system returns only internal agents whose name, description, or role matches the keyword

#### Scenario: Combine internal status filter and search
- **WHEN** an admin requests the internal agent list with both status and search filters
- **THEN** the system returns only internal agents that satisfy both filters

### Requirement: Tenant agent list supports status filtering and keyword search
The system SHALL let admin users narrow the selected tenant's agent list by status and keyword search while keeping tenant scope boundaries intact.

#### Scenario: Filter tenant agents by status
- **WHEN** an admin requests `GET /api/tenants/{tenantId}/agents` with a valid status filter
- **THEN** the system returns only agents for that tenant whose status matches the filter

#### Scenario: Search tenant agents by keyword
- **WHEN** an admin requests `GET /api/tenants/{tenantId}/agents` with a search keyword
- **THEN** the system returns only agents for that tenant whose name, description, or role matches the keyword

#### Scenario: Tenant filters do not cross scope boundaries
- **WHEN** an admin requests a tenant agent list with status and search filters
- **THEN** the response does not include internal agents or agents from other tenants even if they match the filters

### Requirement: Agent catalog UI exposes filter controls for the active list
The system SHALL provide search and status filter controls in the admin agent catalog for whichever list scope is currently active.

#### Scenario: Admin filters internal list from the UI
- **WHEN** the internal agent view is active and the admin changes the status filter or search input
- **THEN** the system refreshes the internal agent list using the selected filters

#### Scenario: Admin filters tenant list from the UI
- **WHEN** a tenant agent view is active and the admin changes the status filter or search input
- **THEN** the system refreshes the selected tenant's agent list using the selected filters

### Requirement: Empty filter results are shown as a non-error state
The system SHALL distinguish between "no matching agents" and request failures when filters return no results.

#### Scenario: No internal agents match filters
- **WHEN** the internal agent filters return zero matching agents
- **THEN** the UI shows an empty-state message that reflects the active filters

#### Scenario: No tenant agents match filters
- **WHEN** the selected tenant's agent filters return zero matching agents
- **THEN** the UI shows an empty-state message that reflects the active filters
