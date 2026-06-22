## MODIFIED Requirements

### Requirement: Admin users can view account status and manage lock state
The system SHALL let authorized admin users inspect user accounts in a member-management table with employee-identifying profile information, open a detail popup for a selected employee, update that employee's `job_position`, and lock or unlock the account from the popup without editing data directly in the database.

#### Scenario: Authorized admin opens member detail popup
- **WHEN** a user with `user.view` permission clicks a member in the admin user-management flow
- **THEN** the system opens a popup showing the selected employee's available member-management information

#### Scenario: Authorized admin updates employee job position
- **WHEN** a user with `user.update` permission submits a new `job_position` for a selected employee from the member-detail popup
- **THEN** the system updates that employee's `job_position`
- **AND** the response returns data the UI can use to refresh the popup and member list

#### Scenario: Member detail popup includes account status action
- **WHEN** a user with permission opens the member-detail popup
- **THEN** the popup shows the current account `Trạng thái`
- **AND** the popup is the surface from which lock or unlock actions are initiated

#### Scenario: Authorized admin locks an account from the popup
- **WHEN** a user with `user.update` permission locks a target account from the member-detail popup
- **THEN** the system updates that target account status to `Locked`

#### Scenario: Authorized admin unlocks an account from the popup
- **WHEN** a user with `user.update` permission unlocks a target account that is currently locked from the member-detail popup
- **THEN** the system updates that target account status to `Active`

#### Scenario: Unauthorized user cannot update employee job position
- **WHEN** a user without the required user-management permission attempts to update `job_position`
- **THEN** the system rejects the request as forbidden
