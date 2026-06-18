## MODIFIED Requirements

### Requirement: Admin users can open an internal agent catalog after login
The system SHALL present an authenticated admin workspace where internal agents are listed separately from tenant-owned agents, and the workspace SHALL include a simple shared header above routed content.

#### Scenario: Admin opens internal agent catalog
- **WHEN** an authenticated admin user navigates to the agent management workspace after login
- **THEN** the system shows the internal agent context as a distinct list of internal agents
- **AND** the workspace content area is framed by a simple shared header

#### Scenario: Non-admin cannot access internal agent catalog
- **WHEN** an authenticated user without the required global admin permission requests the internal agent catalog
- **THEN** the system denies access to internal-agent data

### Requirement: Tenant sidebar lists available tenants for the admin workspace
The system SHALL provide the admin workspace with a sidebar list of tenants or business units that can be selected to view tenant-owned agents, and that sidebar SHALL behave as a full-height structural navigation panel.

#### Scenario: Admin loads tenant sidebar
- **WHEN** an authenticated admin opens the agent management workspace
- **THEN** the system returns the available tenant list with stable identifiers and display names for sidebar rendering

#### Scenario: Sidebar excludes internal agents
- **WHEN** the tenant sidebar is rendered
- **THEN** the internal agent context is not represented as a business-unit tenant item

#### Scenario: Sidebar stretches with workspace shell
- **WHEN** the primary sidebar is rendered on desktop
- **THEN** it visually spans the workspace height as a navigation rail
- **AND** it does not appear as a small floating card separated from the page shell
