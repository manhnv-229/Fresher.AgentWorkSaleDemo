## MODIFIED Requirements

### Requirement: Agent catalog UI exposes filter controls for the active list
The system SHALL provide search and status filter controls in the admin agent catalog for whichever list scope is currently active, and those controls SHALL render user-facing status labels in Vietnamese from a shared frontend mapping instead of page-local hardcoded option arrays.

#### Scenario: Admin filters internal list from the UI
- **WHEN** the internal agent view is active and the admin changes the status filter or search input
- **THEN** the system refreshes the internal agent list using the selected filters

#### Scenario: Admin filters tenant list from the UI
- **WHEN** a tenant agent view is active and the admin changes the status filter or search input
- **THEN** the system refreshes the selected tenant's agent list using the selected filters

#### Scenario: Agent status filter labels are localized consistently
- **WHEN** a frontend page renders agent status filter options for internal, tenant, or catalog agent views
- **THEN** the visible option labels use Vietnamese text for the known backend status codes
- **AND** the submitted filter values still use the backend status codes expected by the API

#### Scenario: Agent status options come from a shared frontend source
- **WHEN** multiple agent pages render the same status filter or status edit options
- **THEN** they use one shared frontend status-mapping source instead of duplicating hardcoded option arrays per page

### Requirement: Empty filter results are shown as a non-error state
The system SHALL distinguish between "no matching agents" and request failures when filters return no results.

#### Scenario: No internal agents match filters
- **WHEN** the internal agent filters return zero matching agents
- **THEN** the UI shows an empty-state message that reflects the active filters

#### Scenario: No tenant agents match filters
- **WHEN** the selected tenant's agent filters return zero matching agents
- **THEN** the UI shows an empty-state message that reflects the active filters
