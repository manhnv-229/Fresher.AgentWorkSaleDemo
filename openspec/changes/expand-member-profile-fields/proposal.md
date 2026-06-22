## Why

The current member-management screen only exposes a small subset of account information, which makes it harder for administrators to identify the right employee and understand their working context. We need richer employee profile fields now so the `Quản lý thành viên` page can support day-to-day operations with clearer identity, project, job-role, and account-status visibility.

## What Changes

- Expand member-management data so each employee record includes `Mã nhân viên`, `Họ tên`, `Dự án`, `Vị trí công việc`, `Email/thông tin tài khoản đăng nhập hệ thống`, and `Status`.
- Update the admin user list API and DTOs to return the additional employee profile fields needed by the frontend.
- Update the `Quản lý thành viên` page to render the richer employee information in the member table instead of showing only basic account identity.
- Preserve the existing lock/unlock account actions while making the expanded employee details available in the same workflow.
- Define which fields are required versus optional when employee profile data is missing so the admin UI can still render incomplete records safely.

## Capabilities

### New Capabilities

- None.

### Modified Capabilities

- `account-security`: The admin user-management flow will expose richer employee profile information alongside account status and lock/unlock controls.

## Impact

- Affected backend area: user DTOs, user-management service mapping, repositories or persistence fields for employee metadata, and admin user APIs.
- Affected frontend area: `SettingsMembersPage.vue`, shared auth/user types, and member-management table rendering.
- Affected data model: user or related employee-profile records may need to store employee code, project, job position, and login account display metadata in addition to current account status.
- Affected UX: administrators can identify employees more accurately from the settings member list without losing current account lock/unlock behavior.
