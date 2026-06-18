## ADDED Requirements

### Requirement: Login form is the initial screen
The frontend SHALL show a login form as the first screen when the app loads.

#### Scenario: Initial login form render
- **WHEN** a user opens the frontend app
- **THEN** the app displays a phone/email input, a password input, and a primary login button labeled `Đăng nhập`

#### Scenario: Form matches compact layout
- **WHEN** the login form is rendered on desktop or mobile
- **THEN** the inputs and button remain aligned, compact, and do not overlap or shift layout

### Requirement: Password visibility can be toggled
The frontend SHALL provide an icon button inside the password field to toggle password visibility.

#### Scenario: Show password
- **WHEN** the user activates the password visibility control while the password is hidden
- **THEN** the password input changes to visible text mode

#### Scenario: Hide password
- **WHEN** the user activates the password visibility control while the password is visible
- **THEN** the password input changes back to password mode

### Requirement: Login form validates required fields
The frontend SHALL prevent login submission when required fields are empty.

#### Scenario: Empty credentials are rejected client-side
- **WHEN** the user clicks `Đăng nhập` without entering phone/email or password
- **THEN** the app displays a validation message and does not call the backend login API

### Requirement: Login calls backend authentication API
The frontend SHALL submit entered credentials to the backend `POST /api/auth/login` endpoint.

#### Scenario: Successful login
- **WHEN** the user submits valid credentials
- **THEN** the app calls `POST /api/auth/login`
- **AND** stores the returned access token, refresh token, and expiration metadata
- **AND** shows an authenticated success state

#### Scenario: Invalid login
- **WHEN** the backend rejects the submitted credentials
- **THEN** the app shows a clear login error message
- **AND** the app does not show an authenticated state

#### Scenario: API unavailable
- **WHEN** the login API cannot be reached
- **THEN** the app shows a clear connection error message and keeps the form editable

### Requirement: Login button reflects loading state
The frontend SHALL prevent duplicate login submissions while a login request is in progress.

#### Scenario: Login request in progress
- **WHEN** the user submits the login form
- **THEN** the login button is disabled until the request finishes
- **AND** the UI communicates that login is in progress

### Requirement: Authenticated state can be cleared
The frontend SHALL allow the user to clear the local authenticated state after login.

#### Scenario: Local logout clears tokens
- **WHEN** the user clicks the frontend logout action after login
- **THEN** the app removes stored access and refresh tokens
- **AND** the login form is shown again
