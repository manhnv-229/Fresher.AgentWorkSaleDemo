## MODIFIED Requirements

### Requirement: Admin users can view account status and manage lock state
The system SHALL let authorized admin users inspect user accounts with employee-identifying profile information and lock or unlock them without editing data directly in the database.

#### Scenario: Authorized admin lists user accounts
- **WHEN** a user with `user.view` permission opens the admin user-management flow
- **THEN** the system returns user records with `Mã nhân viên`, `Họ tên`, `Dự án`, `Vị trí công việc`, `Email/thông tin tài khoản đăng nhập hệ thống`, and current account `Status` needed by the admin UI

#### Scenario: User records can render incomplete employee profile data
- **WHEN** an employee account is missing one or more optional profile fields such as project or job position
- **THEN** the system still returns the account in the admin user-management flow
- **AND** the missing fields are represented in a way the UI can render safely

#### Scenario: Authorized admin locks an account
- **WHEN** a user with `user.update` permission locks a target account
- **THEN** the system updates that target account status to `Locked`

#### Scenario: Authorized admin unlocks an account
- **WHEN** a user with `user.update` permission unlocks a target account that is currently locked
- **THEN** the system updates that target account status to `Active`

#### Scenario: Unauthorized user cannot manage account status
- **WHEN** a user without the required user-management permission calls a lock or unlock action
- **THEN** the system rejects the request as forbidden
