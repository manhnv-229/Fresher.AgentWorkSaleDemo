## ADDED Requirements

### Requirement: Audit log page supports field-level search and filtering
The system SHALL allow administrators to search and filter audit log results by `Action`, `UserName`, `CreatedDate`, `IPAddress`, and `Description` from the settings audit log page.

#### Scenario: User filters by a single field
- **WHEN** the user enters or selects a value for one audit log field filter and applies the filters
- **THEN** the page shows only audit log records that match that field criterion

#### Scenario: User combines multiple field filters
- **WHEN** the user provides values for multiple audit log field filters and applies the filters
- **THEN** the page shows only audit log records that satisfy all provided filter criteria together

### Requirement: CreatedDate filtering uses a time-aware range
The system SHALL support `CreatedDate` filtering as a date-based range instead of plain free-text matching.

#### Scenario: User filters by date range
- **WHEN** the user provides a start date, an end date, or both for `CreatedDate`
- **THEN** the page shows only audit log records whose created timestamp falls within the selected range boundaries

### Requirement: Audit log filtering is backed by the server query contract
The system SHALL request filtered audit log results from the backend using the selected filter fields instead of loading the entire audit history and narrowing it only in the browser.

#### Scenario: Filtered audit query returns matching rows
- **WHEN** the user applies any audit log filter
- **THEN** the frontend sends the selected filter values to the audit log API
- **AND** the backend returns only records that match those filter values

#### Scenario: No records match active filters
- **WHEN** the selected audit log filters match no records
- **THEN** the page shows an empty-results state for the active filters instead of treating the response as an error
