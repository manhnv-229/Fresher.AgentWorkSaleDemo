## Why

The current member-management screen still mixes account-security metadata with employee-listing needs, which makes the table harder to scan and exposes a `password_changed_at` field that is no longer wanted in this workflow. We need the `Quản lý thành viên` page to behave like a concise employee directory for admins, with the right columns, clearer identity grouping, and a single reused `modified_at` timestamp instead of a dedicated password-change field.

## What Changes

- Reshape the member-management list so it shows exactly these columns: `Nhân viên`, `Vị trí công việc`, `Dự án`, `Email`, and `Trạng thái`.
- Define the `Nhân viên` column to render the employee `Full name` on the first line and `Mã nhân viên` on the second line in the same cell.
- Update the admin user list API and DTOs to return the employee fields needed by the new table layout.
- Remove `password_changed_at` from the member-management contract and from database-backed user data used by this flow.
- Reuse the existing `modified_at` field as the relevant timestamp source where update-time information is needed instead of maintaining a separate password-change timestamp.
- Preserve the existing lock/unlock account actions while making the expanded employee details available in the same workflow.
- Define which fields are required versus optional when employee profile data is missing so the admin UI can still render incomplete records safely.

## Capabilities

### New Capabilities

- None.

### Modified Capabilities

- `account-security`: The admin user-management flow will expose the new employee-oriented table layout, stop showing password-change metadata, and continue supporting account status and lock/unlock controls.

## Impact

- Affected backend area: user DTOs, user-management service mapping, persistence for employee metadata, admin user APIs, and removal of `password_changed_at` usage.
- Affected frontend area: `SettingsMembersPage.vue`, shared auth/user types, and member-management table rendering for the five target columns.
- Affected data model: user or related employee-profile records may need to store employee code, project, and job position while relying on `modified_at` instead of a dedicated password-change timestamp for this surface.
- Affected UX: administrators can scan employees more quickly from a cleaner settings member list without losing current account lock/unlock behavior.
