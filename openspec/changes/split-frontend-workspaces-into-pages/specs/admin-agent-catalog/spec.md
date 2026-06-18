## MODIFIED Requirements

### Requirement: Admin users can open an internal agent catalog after login
The system SHALL present an authenticated admin workspace where internal agents are rendered through dedicated frontend pages rather than one combined page component.

#### Scenario: Admin opens internal agent catalog page
- **WHEN** an authenticated admin user navigates to the internal agent management area after login
- **THEN** the system loads a dedicated internal-agent page within the authenticated workspace shell
- **AND** that page shows the internal agent context as a distinct list of internal agents

#### Scenario: Non-admin cannot access internal agent catalog
- **WHEN** an authenticated user without the required global admin permission requests the internal agent catalog
- **THEN** the system denies access to internal-agent data

### Requirement: Tenant sidebar lists available tenants for the admin workspace
The system SHALL provide the admin workspace shell with navigation that can route users into tenant-specific agent pages.

#### Scenario: Admin loads tenant navigation
- **WHEN** an authenticated admin opens the agent management workspace
- **THEN** the system returns the available tenant list with stable identifiers and display names for sidebar rendering

#### Scenario: Selecting a tenant changes to a tenant page
- **WHEN** an authenticated admin selects a tenant from the sidebar
- **THEN** the system navigates to a dedicated tenant-agent page for that tenant

### Requirement: Selecting a tenant shows only that tenant's agents
The system SHALL load and display the agent list for the tenant selected in the sidebar on that tenant's dedicated page.

#### Scenario: Admin opens tenant agent page
- **WHEN** an authenticated admin navigates to a tenant-specific agent page
- **THEN** the system loads agents associated with that tenant only

#### Scenario: Agents from other tenants are excluded
- **WHEN** the system returns the selected tenant's agent list
- **THEN** the response does not include internal agents or agents belonging to other tenants

### Requirement: Agent management UI keeps tenant context after mutations
The system SHALL preserve or restore the active internal-or-tenant browsing context across route-driven navigation and mutations.

#### Scenario: Returning from detail restores routed list context
- **WHEN** a user leaves an agent detail page and returns to an internal or tenant list page
- **THEN** the workspace restores the relevant scope, tenant selection, filters, and pagination state for that page

#### Scenario: Tenant list refreshes after save or delete
- **WHEN** a user saves or deletes an agent for the selected tenant from a dedicated page
- **THEN** the UI refreshes that tenant's current result page using the active filters
- **AND** the selected tenant remains active in the workspace route
