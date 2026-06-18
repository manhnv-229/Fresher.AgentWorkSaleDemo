## ADDED Requirements

### Requirement: Authenticated users can change their own password
The system SHALL allow an authenticated user to change their own password by submitting their current password and a replacement password.

#### Scenario: Successful password change
- **WHEN** an authenticated user submits the correct current password and a valid new password to the password-change flow
- **THEN** the system updates the stored password hash for that user
- **AND** the system records the password change timestamp

#### Scenario: Current password mismatch is rejected
- **WHEN** an authenticated user submits an incorrect current password to the password-change flow
- **THEN** the system rejects the request
- **AND** the stored password hash remains unchanged

### Requirement: Password change forces reauthentication
The system SHALL revoke active authenticated sessions for a user after that user changes their password.

#### Scenario: Current session is invalid after password change
- **WHEN** a user successfully changes their password
- **THEN** the system revokes the session used to submit the request
- **AND** subsequent authenticated API requests with the old access token are rejected

#### Scenario: Other sessions are also invalid after password change
- **WHEN** a user successfully changes their password while other active sessions exist for the same account
- **THEN** the system revokes those other active sessions
- **AND** refresh attempts tied to those sessions are rejected

### Requirement: Admin users can view account status and manage lock state
The system SHALL let authorized admin users inspect user accounts and lock or unlock them without editing data directly in the database.

#### Scenario: Authorized admin lists user accounts
- **WHEN** a user with `user.view` permission opens the admin user-management flow
- **THEN** the system returns user records with identifying profile information and current account status

#### Scenario: Authorized admin locks an account
- **WHEN** a user with `user.update` permission locks a target account
- **THEN** the system updates that target account status to `Locked`

#### Scenario: Authorized admin unlocks an account
- **WHEN** a user with `user.update` permission unlocks a target account that is currently locked
- **THEN** the system updates that target account status to `Active`

#### Scenario: Unauthorized user cannot manage account status
- **WHEN** a user without the required user-management permission calls a lock or unlock action
- **THEN** the system rejects the request as forbidden

### Requirement: Locked accounts cannot continue or regain authenticated access
The system SHALL block locked accounts from logging in, refreshing tokens, or continuing to use previously issued sessions.

#### Scenario: Locked account login is rejected
- **WHEN** a user whose account status is `Locked` submits valid credentials to login
- **THEN** the system rejects the request without issuing tokens
- **AND** the response identifies the account as locked

#### Scenario: Locked account refresh is rejected
- **WHEN** a refresh token linked to a locked user account is submitted to refresh the session
- **THEN** the system rejects the refresh request without issuing new tokens

#### Scenario: Locking an account revokes active sessions
- **WHEN** an admin locks an account that currently has active sessions
- **THEN** the system revokes those active sessions
- **AND** subsequent authenticated API requests from those sessions are rejected

#### Scenario: Unlocked account can authenticate again
- **WHEN** an admin unlocks a previously locked account and the user submits valid credentials
- **THEN** the system allows a new login

### Requirement: Account-security flows expose clear machine-readable error states
The system SHALL return explicit error codes for account-security failures so the frontend can distinguish lockouts and reauthentication cases from generic auth failures.

#### Scenario: Locked login response is distinguishable
- **WHEN** a locked account attempts to log in
- **THEN** the error response includes a lock-specific account-state code

#### Scenario: Password-change logout path is distinguishable
- **WHEN** a user attempts to use an access token or refresh token from a session revoked by password change
- **THEN** the system rejects the request with an authentication failure that the client can treat as a forced reauthentication event
