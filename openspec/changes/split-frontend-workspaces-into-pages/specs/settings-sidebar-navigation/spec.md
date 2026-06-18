## MODIFIED Requirements

### Requirement: Primary sidebar exposes a settings workspace entry
The frontend SHALL provide a `Thiết lập` option in the primary authenticated sidebar that navigates to dedicated settings pages.

#### Scenario: Settings entry is visible in authenticated workspace
- **WHEN** an authenticated user opens the main workspace
- **THEN** the primary sidebar shows a `Thiết lập` navigation option alongside the existing workspace navigation

#### Scenario: Selecting settings changes route-level workspace area
- **WHEN** the user activates the `Thiết lập` option
- **THEN** the main workspace navigates away from agent pages and into the settings page structure

### Requirement: Settings workspace provides a secondary sidebar menu
The frontend SHALL show a secondary settings sidebar or settings-navigation area when a settings page is active.

#### Scenario: Settings navigation appears on settings pages
- **WHEN** the user is on a settings page
- **THEN** a secondary settings navigation area is displayed beside or within the main workspace layout
- **AND** the navigation lists at least `Quản lý thành viên` and `Đổi mật khẩu`

#### Scenario: Secondary settings navigation is hidden on agent pages
- **WHEN** the user returns to an internal-agent or tenant-agent page
- **THEN** the settings-specific navigation area is not shown

### Requirement: Member management is rendered inside the settings workspace
The frontend SHALL move the existing member-management controls into a dedicated settings page or route segment.

#### Scenario: Member management opens as its own page
- **WHEN** the user selects `Quản lý thành viên` from the settings navigation
- **THEN** the content area shows the member-management page and related actions

#### Scenario: Member management is not duplicated in agent pages
- **WHEN** the user is viewing an agent-management page
- **THEN** the member-management section does not appear inline in that page

### Requirement: Password change is rendered inside the settings workspace
The frontend SHALL move the password-change entry point into a dedicated settings page or route segment.

#### Scenario: Password change opens as its own page
- **WHEN** the user selects `Đổi mật khẩu` from the settings navigation
- **THEN** the content area shows the password-change page for the authenticated account

#### Scenario: Password change is not duplicated in agent pages
- **WHEN** the user is viewing an agent-management page
- **THEN** the password-change panel does not appear inline in that page

### Requirement: Existing catalog workflows remain reachable after settings navigation is added
The frontend SHALL preserve the current catalog workflows after the settings experience is split into separate pages.

#### Scenario: Returning from settings to internal catalog
- **WHEN** the user switches from a settings page back to `Nội bộ`
- **THEN** the internal agent page is shown again with its current controls and content behavior

#### Scenario: Returning from settings to tenant catalog
- **WHEN** the user switches from a settings page to a tenant workspace
- **THEN** the selected tenant agent page is shown again without requiring settings content to remain visible
