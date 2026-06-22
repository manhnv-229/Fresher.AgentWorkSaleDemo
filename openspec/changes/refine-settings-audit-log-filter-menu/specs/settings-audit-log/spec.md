## MODIFIED Requirements

### Requirement: Audit log page supports field-level search and filtering
The system SHALL provide one unified audit log search input for general text lookup, and it SHALL provide advanced filtering through a single filter-button interaction instead of exposing separate visible search fields for each audit-log column.

#### Scenario: User searches with one shared search input
- **WHEN** the user enters text into the audit log search input and submits the search
- **THEN** the page shows only audit log records that match the provided search text across the supported audit-log content fields

#### Scenario: Audit log toolbar stays compact
- **WHEN** the user opens the audit log page
- **THEN** the page shows one shared search input and one filter button in the audit toolbar
- **AND** it does not show separate always-visible search inputs for each audit-log field

### Requirement: CreatedDate filtering uses a time-aware range
The system SHALL support `CreatedDate` filtering through predefined relative time options in the filter menu: `Hôm nay`, `Hôm qua`, `Tuần này`, `Tuần trước`, `Tháng này`, `Tháng trước`, `Năm nay`, and `Năm trước`.

#### Scenario: User selects a predefined time filter
- **WHEN** the user opens the filter menu and selects one of the predefined time options
- **THEN** the page shows only audit log records whose created timestamp falls within the selected preset range

### Requirement: Audit log filtering is backed by the server query contract
The system SHALL request filtered audit log results from the backend using the unified search text, the selected time preset, and the selected action types instead of loading the entire audit history and narrowing it only in the browser.

#### Scenario: Filter menu applies action and time filters
- **WHEN** the user selects one or more audit action types and an optional time preset from the filter menu and applies the filters
- **THEN** the frontend sends those selected filter values to the audit log API
- **AND** the backend returns only records that match the selected action types and time range

#### Scenario: No records match active search and filters
- **WHEN** the active audit log search text and filter-menu selections match no records
- **THEN** the page shows an empty-results state for the current search and filters instead of treating the response as an error

## ADDED Requirements

### Requirement: Filter button opens a context menu for advanced audit filters
The audit log page SHALL expose one filter button with a filter icon, and activating that button SHALL open a context menu or popover containing advanced audit filters.

#### Scenario: User opens filter menu
- **WHEN** the user presses the filter button in the audit log toolbar
- **THEN** a context menu or popover opens near the button
- **AND** the menu contains time filtering and action-type filtering controls

### Requirement: Action-type filtering supports multi-select checkboxes
The audit log filter menu SHALL let users select multiple action types at the same time by using checkboxes.

#### Scenario: User selects multiple action types
- **WHEN** the user checks multiple action types in the filter menu and applies the filters
- **THEN** the page shows audit log records whose action matches any of the selected action types
