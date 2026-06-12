## ADDED Requirements

### Requirement: Refresh token is stored in HttpOnly cookie
The system SHALL store browser refresh tokens in a backend-managed `HttpOnly` cookie and SHALL NOT require browser JavaScript to persist the raw refresh token.

#### Scenario: Login sets refresh token cookie
- **WHEN** a user logs in with valid credentials from the browser frontend
- **THEN** the API sets a refresh token cookie with `HttpOnly` enabled
- **AND** the frontend receives access-token data without storing a raw refresh token in browser storage

#### Scenario: Frontend cannot read refresh token
- **WHEN** the browser frontend completes login
- **THEN** the refresh token is not available from frontend JavaScript state, `localStorage`, or `sessionStorage`

### Requirement: Access token is stored in memory only
The frontend SHALL keep the access token in memory-only auth state and SHALL NOT persist it in `localStorage` or `sessionStorage`.

#### Scenario: Login stores access token in memory
- **WHEN** the browser frontend receives a successful login response
- **THEN** it stores the access token in reactive in-memory auth state
- **AND** it does not write the access token to persistent browser storage

#### Scenario: Page reload restores access through refresh
- **WHEN** the page reloads after a previous successful login
- **THEN** the frontend attempts to refresh the access token using the refresh cookie
- **AND** it shows authenticated state if refresh succeeds

### Requirement: Refresh uses cookie-based rotation
The system SHALL refresh access tokens by reading the refresh token from the cookie, revoking the old refresh token, creating a replacement refresh token, and setting a replacement cookie.

#### Scenario: Refresh succeeds with valid cookie
- **WHEN** the browser calls the refresh endpoint with a valid refresh token cookie
- **THEN** the API returns a new access token
- **AND** revokes the previous refresh token in the database
- **AND** sets a new refresh token cookie

#### Scenario: Refresh fails without valid cookie
- **WHEN** the browser calls the refresh endpoint without a valid refresh token cookie
- **THEN** the API rejects the request with an unauthorized response
- **AND** the frontend clears in-memory authenticated state

### Requirement: Logout revokes server session and clears cookie
The system SHALL logout browser users by revoking the refresh token/session represented by the refresh cookie and clearing the refresh token cookie.

#### Scenario: Logout clears browser auth state
- **WHEN** an authenticated browser user logs out
- **THEN** the API revokes the associated refresh token and session in the database
- **AND** the API clears the refresh token cookie
- **AND** the frontend clears its in-memory access token state

#### Scenario: Old access token is invalid after logout
- **WHEN** logout revokes the session associated with an access token
- **THEN** subsequent protected API requests using that old access token are rejected

### Requirement: Credentialed browser requests are supported locally
The API SHALL allow the configured local frontend origin to send credentialed auth requests during development.

#### Scenario: Local frontend sends login request
- **WHEN** the Vue dev server at `http://localhost:5173` submits a login request
- **THEN** the API accepts the request with credentials enabled
- **AND** browser cookie rules allow the refresh cookie to be stored

#### Scenario: Unauthorized origins are not broadly allowed
- **WHEN** the API configures credentialed CORS
- **THEN** it uses explicit allowed origins
- **AND** it does not combine credentials with wildcard origin allowance
