## MODIFIED Requirements

### Requirement: Agent create flows preserve visibility boundaries
The system SHALL preserve the visibility boundary between admin-only internal agents and tenant-owned agents after creation, and the admin workspace SHALL bind tenant create actions to the tenant currently selected in the sidebar.

#### Scenario: Internal agent stays admin-only after creation
- **WHEN** an admin creates an internal agent successfully from the internal workspace
- **THEN** only admin-authorized internal-agent views can retrieve that agent

#### Scenario: Tenant create action follows selected tenant context
- **WHEN** an admin is viewing a selected tenant's agent list and opens the create-agent action from that tenant workspace
- **THEN** the create flow is bound to the currently selected tenant
- **AND** the UI does not require the admin to choose a different tenant inside the create flow

#### Scenario: Tenant agent stays within selected tenant after creation
- **WHEN** a tenant-scoped agent is created from the selected tenant workspace
- **THEN** the UI submits the create request to that tenant's scoped API
- **AND** the created agent is retrievable from that tenant's list and not from another tenant's list

#### Scenario: Tenant list refreshes after tenant-scoped creation
- **WHEN** a tenant-scoped agent is created successfully for the selected tenant
- **THEN** the workspace refreshes the same tenant's current result set
- **AND** the selected tenant remains active in the workspace
