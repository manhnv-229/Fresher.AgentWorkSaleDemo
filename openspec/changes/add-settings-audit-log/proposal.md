## Why

The system already models audit logs at the data layer, but administrators still have no dedicated way to review that history from the settings workspace. We need an `Audit Log` section now so sensitive actions such as login, agent changes, file operations, and permission updates become visible to operators in one consistent place.

## What Changes

- Add an `Audit Log` subsection under `Thiết lập` so administrators can open a dedicated settings page for operational history.
- Show an audit log listing with the required columns: `Action`, `UserName`, `CreatedDate`, `IPAddress`, and `Description`.
- Require audit entries for login, agent create/update/delete, file upload/delete, and permission-assignment changes.
- Define how the audit log page loads and presents append-only history without disrupting the current `Quản lý thành viên` and `Đổi mật khẩu` settings flows.
- Reuse the existing audit log persistence foundation while extending API and UI behavior to expose the records to administrators.

## Capabilities

### New Capabilities

- `settings-audit-log`: A dedicated settings page that lets administrators review audit history with the required operational columns.

### Modified Capabilities

- `settings-sidebar-navigation`: The settings navigation will add an `Audit Log` destination alongside the existing settings sections.
- `audit-log-tracking`: Audit requirements will explicitly cover login, agent create/update/delete, file upload/delete, and permission changes with the required display fields.

## Impact

- Affected frontend area: `frontend/src/router`, `frontend/src/layouts`, and new or updated settings views/components for the audit log page.
- Affected backend area: audit-log query endpoints, service/application handlers, and action emitters for agent, file, auth, and permission workflows.
- Affected data behavior: audit records remain append-only and must surface actor, timestamp, source IP, and human-readable descriptions for the requested action set.
- Affected UX: administrators gain a dedicated settings destination for reviewing historical activity without changing existing member-management or password-change entry points.
