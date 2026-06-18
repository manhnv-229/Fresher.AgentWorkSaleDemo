## ADDED Requirements

### Requirement: Primary sidebar exposes a settings workspace entry
The frontend SHALL provide a `Thiết lập` option in the primary authenticated sidebar so account-related tools can be accessed as a separate workspace area.

#### Scenario: Settings entry is visible in authenticated workspace
- **WHEN** an authenticated user opens the main workspace
- **THEN** the primary sidebar shows a `Thiết lập` navigation option alongside the existing workspace navigation

#### Scenario: Selecting settings changes the active workspace area
- **WHEN** the user activates the `Thiết lập` option
- **THEN** the main workspace switches away from the inline dashboard account cards and into the settings experience

### Requirement: Settings workspace provides a secondary sidebar menu
The frontend SHALL show a secondary sidebar menu next to the primary sidebar when the settings workspace is active.

#### Scenario: Settings sidebar appears when settings is active
- **WHEN** the `Thiết lập` workspace is active
- **THEN** a secondary sidebar menu is displayed beside the primary sidebar
- **AND** the menu lists at least `Quản lý thành viên` and `Đổi mật khẩu`

#### Scenario: Secondary sidebar is hidden outside settings
- **WHEN** the user returns to an agent workspace such as `Nội bộ` or a tenant list
- **THEN** the secondary settings sidebar is not shown

### Requirement: Member management is rendered inside the settings workspace
The frontend SHALL move the existing member-management controls into the `Quản lý thành viên` settings subsection.

#### Scenario: Member management opens from settings menu
- **WHEN** the user selects `Quản lý thành viên` from the settings sidebar
- **THEN** the content area shows the member-management table and related actions

#### Scenario: Member management is not duplicated in dashboard body
- **WHEN** the user is viewing the agent catalog workspace
- **THEN** the member-management section does not appear inline above the catalog content

### Requirement: Password change is rendered inside the settings workspace
The frontend SHALL move the password-change entry point into the `Đổi mật khẩu` settings subsection.

#### Scenario: Password change opens from settings menu
- **WHEN** the user selects `Đổi mật khẩu` from the settings sidebar
- **THEN** the content area shows the password-change controls for the authenticated account

#### Scenario: Password change is not duplicated in dashboard body
- **WHEN** the user is viewing the agent catalog workspace
- **THEN** the password-change panel does not appear inline above the catalog content

### Requirement: Existing catalog workflows remain reachable after settings navigation is added
The frontend SHALL preserve the current `Nội bộ` and tenant agent browsing flows after the settings navigation is introduced.

#### Scenario: Returning from settings to internal catalog
- **WHEN** the user switches from `Thiết lập` back to `Nội bộ`
- **THEN** the internal agent catalog is shown again with its current controls and content behavior

#### Scenario: Returning from settings to tenant catalog
- **WHEN** the user switches from `Thiết lập` to a tenant workspace
- **THEN** the selected tenant agent view is shown again without requiring the settings sections to remain visible
