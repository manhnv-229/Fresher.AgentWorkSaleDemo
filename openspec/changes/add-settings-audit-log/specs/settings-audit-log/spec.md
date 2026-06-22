## ADDED Requirements

### Requirement: Settings workspace exposes a dedicated audit log page
The frontend SHALL provide an `Audit Log` page inside `Thiết lập` so administrators can review recorded operational history without leaving the settings workspace.

#### Scenario: User opens audit log from settings navigation
- **WHEN** the user selects `Audit Log` from the settings navigation
- **THEN** the application opens a dedicated audit log settings page within the authenticated workspace layout

#### Scenario: Audit log page preserves settings context
- **WHEN** the user is viewing the audit log page
- **THEN** the `Thiết lập` workspace remains active and the `Audit Log` settings item is shown as the active subsection

### Requirement: Audit log page displays the required operational columns
The system SHALL show audit log records with the fields `Action`, `UserName`, `CreatedDate`, `IPAddress`, and `Description`.

#### Scenario: Audit log records load successfully
- **WHEN** the audit log page receives audit records from the backend
- **THEN** each visible row shows values for `Action`, `UserName`, `CreatedDate`, `IPAddress`, and `Description`

#### Scenario: Audit log record omits IP address
- **WHEN** an audit entry does not have a captured IP address
- **THEN** the audit log page still renders the record and shows an empty or fallback value for `IPAddress` instead of failing

### Requirement: Audit log history is read-only in settings
The frontend SHALL present audit history as a read-only review surface and SHALL NOT offer controls that mutate or delete audit entries from the settings page.

#### Scenario: User reviews audit history
- **WHEN** the user opens the audit log page
- **THEN** the page provides record-review behavior only and does not expose edit or delete actions for audit rows
