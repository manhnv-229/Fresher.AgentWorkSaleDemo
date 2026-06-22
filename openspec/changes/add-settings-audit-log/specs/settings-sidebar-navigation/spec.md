## MODIFIED Requirements

### Requirement: Settings workspace provides a secondary sidebar menu
The frontend SHALL show a secondary sidebar menu next to the primary sidebar when a settings page is active, and that menu SHALL include `Quản lý thành viên`, `Đổi mật khẩu`, and `Audit Log`.

#### Scenario: Settings sidebar appears when settings is active
- **WHEN** the user is on a settings page
- **THEN** a secondary sidebar menu is displayed beside or within the main workspace layout
- **AND** the menu lists `Quản lý thành viên`, `Đổi mật khẩu`, and `Audit Log`

#### Scenario: Secondary settings navigation is hidden on agent pages
- **WHEN** the user is on an agent page
- **THEN** the settings-specific navigation area is not shown
