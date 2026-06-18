## MODIFIED Requirements

### Requirement: Settings workspace provides a secondary sidebar menu
The frontend SHALL show a secondary settings sidebar menu next to the primary sidebar when the settings workspace is active, and that sidebar SHALL read as a full-height structural navigation column rather than a floating card.

#### Scenario: Settings sidebar appears when settings is active
- **WHEN** the `Thiết lập` workspace is active
- **THEN** a secondary sidebar menu is displayed beside the primary sidebar
- **AND** the menu lists at least `Quản lý thành viên` and `Đổi mật khẩu`

#### Scenario: Settings sidebar uses structural shell layout
- **WHEN** the settings sidebar is rendered on desktop
- **THEN** it stretches with the workspace height and aligns visually as a navigation rail
- **AND** it does not appear as a detached rounded card floating inside the page

#### Scenario: Secondary sidebar is hidden outside settings
- **WHEN** the user returns to an agent workspace such as `Nội bộ` or a tenant list
- **THEN** the secondary settings sidebar is not shown
