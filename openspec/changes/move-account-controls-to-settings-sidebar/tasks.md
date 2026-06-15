## 1. Settings navigation state

- [ ] 1.1 Add a top-level `Thiết lập` option to the authenticated primary sidebar navigation.
- [ ] 1.2 Introduce explicit page state for the settings workspace and the selected settings subsection without breaking the current agent-scope switching flow.
- [ ] 1.3 Add a secondary settings sidebar that appears only when the settings workspace is active and lists `Quản lý thành viên` plus `Đổi mật khẩu`.

## 2. Settings content migration

- [ ] 2.1 Move the current member-management panel into the `Quản lý thành viên` settings section.
- [ ] 2.2 Move the current password-change controls into the `Đổi mật khẩu` settings section.
- [ ] 2.3 Remove the inline account-security and admin-control sections from the main dashboard content so the catalog area stays focused on agent workflows.

## 3. Layout and regression verification

- [ ] 3.1 Update sidebar and content styling so the dual-sidebar layout reads clearly on desktop and adapts sensibly on smaller screens.
- [ ] 3.2 Verify users can switch between `Thiết lập`, `Nội bộ`, and tenant views without losing the expected content or leaving the wrong sidebar visible.
- [ ] 3.3 Run frontend build or focused smoke checks and record any remaining responsive or navigation limitations.
