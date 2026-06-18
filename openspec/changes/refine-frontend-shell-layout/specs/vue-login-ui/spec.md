## MODIFIED Requirements

### Requirement: Login form is the initial screen
The frontend SHALL show a login form as the first screen when the app loads, and that login screen SHALL be visually centered within the viewport instead of appearing top-heavy.

#### Scenario: Initial login form render
- **WHEN** a user opens the frontend app
- **THEN** the app displays a phone/email input, a password input, and a primary login button labeled `Đăng nhập`

#### Scenario: Login layout is centered in the viewport
- **WHEN** the login screen is rendered on desktop
- **THEN** the login container is positioned with balanced vertical spacing inside the viewport
- **AND** it does not appear pinned unusually close to the top edge

#### Scenario: Form matches compact layout
- **WHEN** the login form is rendered on desktop or mobile
- **THEN** the inputs and button remain aligned, compact, and do not overlap or shift layout
