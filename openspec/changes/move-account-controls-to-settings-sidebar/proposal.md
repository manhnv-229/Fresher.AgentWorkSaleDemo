## Why

The current account-security and admin-member controls are rendered directly in the main workspace content, which makes the agent dashboard feel crowded and mixes operational settings with day-to-day agent browsing. Moving these controls under a dedicated `Thiết lập` area will make navigation clearer and create room for account-related tools to grow without overwhelming the main catalog views.

## What Changes

- Add a top-level `Thiết lập` option to the primary sidebar alongside the existing workspace navigation.
- Move `đổi mật khẩu` and `quản lý thành viên` out of the dashboard body and into a dedicated settings experience.
- Show a secondary sidebar menu when `Thiết lập` is active so users can switch between `Quản lý thành viên` and `Đổi mật khẩu`.
- Preserve the existing password-change and member-management capabilities while changing how users reach and view them.
- Keep the agent catalog flows intact for `Nội bộ` and tenant views so the new settings navigation does not disrupt current admin workflows.

## Capabilities

### New Capabilities
- `settings-sidebar-navigation`: A dedicated settings entry in the main sidebar plus a secondary settings menu for account-related tools.

### Modified Capabilities

## Impact

- Affected frontend areas: `AgentCatalogPage.vue`, shared sidebar/layout state, and styling for nested navigation and settings content panels.
- Affected user workflow: admins will access member management and password change from `Thiết lập` instead of seeing both sections inline on the main dashboard.
- Affected validation/testing: navigation-state behavior, settings-panel switching, and regression coverage for existing account-security actions after the UI move.
