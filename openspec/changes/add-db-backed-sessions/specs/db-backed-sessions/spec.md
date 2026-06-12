## ADDED Requirements

### Requirement: Login creates a persisted session
The system SHALL create a persisted user session when an active user logs in successfully.

#### Scenario: Successful login creates active session
- **WHEN** an active user submits valid credentials to `POST /api/auth/login`
- **THEN** the system creates a `user_sessions` record for that user
- **AND** the session is active, unrevoked, and has an expiration timestamp

#### Scenario: Failed login does not create session
- **WHEN** a login attempt is rejected because credentials are invalid or the user is inactive
- **THEN** the system does not create a user session

### Requirement: Access tokens include session id
The system SHALL include the login session id in access tokens using the `sid` claim.

#### Scenario: Login access token contains sid
- **WHEN** the system returns an access token from a successful login
- **THEN** the access token contains a `sid` claim matching the created user session id
- **AND** the token does not contain role lists or permission lists

#### Scenario: Refreshed access token keeps same sid
- **WHEN** the system returns a new access token from a valid refresh token
- **THEN** the new access token contains the same `sid` as the session bound to the refresh token

### Requirement: Refresh tokens are bound to sessions
The system SHALL persist the session id on refresh token records.

#### Scenario: Login refresh token is linked to session
- **WHEN** the system stores the first refresh token after login
- **THEN** the refresh token record references the created session id

#### Scenario: Rotated refresh token remains linked to session
- **WHEN** a valid refresh token is rotated
- **THEN** the replacement refresh token record references the same session id as the revoked refresh token

### Requirement: Authenticated requests validate session state
The system MUST validate the JWT session id against the database for every authenticated API request.

#### Scenario: Active session allows authenticated request
- **WHEN** a client calls an authenticated API with a valid access token whose `sid` belongs to an active unrevoked session for that user
- **THEN** the system treats the request as authenticated

#### Scenario: Missing session id is rejected
- **WHEN** a client calls an authenticated API with an access token that has no `sid` claim
- **THEN** the system rejects the request as unauthenticated

#### Scenario: Unknown session is rejected
- **WHEN** a client calls an authenticated API with an access token whose `sid` does not exist in the database
- **THEN** the system rejects the request as unauthenticated

#### Scenario: Revoked session is rejected
- **WHEN** a client calls an authenticated API with an access token whose session has `revoked_at` set
- **THEN** the system rejects the request as unauthenticated

#### Scenario: Expired session is rejected
- **WHEN** a client calls an authenticated API with an access token whose session is expired
- **THEN** the system rejects the request as unauthenticated

### Requirement: Logout revokes session
The system SHALL revoke the session associated with the submitted refresh token during logout.

#### Scenario: Logout invalidates current access token
- **WHEN** a client logs out with a refresh token linked to an active session
- **THEN** the system revokes the refresh token
- **AND** the system revokes the related session
- **AND** subsequent authenticated requests using access tokens from that session are rejected

#### Scenario: Logout with unknown token does not reveal token existence
- **WHEN** a client logs out with an unknown refresh token
- **THEN** the system completes without revealing whether the token existed

### Requirement: Refresh requires active session
The system SHALL reject refresh attempts when the related session is missing, expired, or revoked.

#### Scenario: Refresh succeeds with active session
- **WHEN** a client submits a valid unrevoked refresh token linked to an active session
- **THEN** the system rotates the refresh token and returns a new access token

#### Scenario: Refresh fails after logout
- **WHEN** a client submits a refresh token whose session was revoked by logout
- **THEN** the system rejects the refresh attempt without issuing new tokens

### Requirement: Session validation applies before permission authorization
The system SHALL complete database-backed session validation before evaluating permission requirements.

#### Scenario: Revoked session does not reach permission success
- **WHEN** a user has the required permission but the access token session is revoked
- **THEN** the system rejects the request as unauthenticated rather than allowing permission authorization
