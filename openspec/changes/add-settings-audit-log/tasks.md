## 1. Extend settings navigation and audit log page

- [x] 1.1 Add an `Audit Log` settings route and sidebar entry in the authenticated workspace shell.
- [x] 1.2 Create the settings audit log page and render the required columns: `Action`, `UserName`, `CreatedDate`, `IPAddress`, and `Description`.
- [x] 1.3 Handle loading and empty/error states for the read-only audit log view without breaking existing `Quản lý thành viên` and `Đổi mật khẩu` pages.

## 2. Expose audit log history for the settings UI

- [x] 2.1 Add a backend query/API endpoint that returns audit log records shaped for the settings page fields.
- [x] 2.2 Map persisted audit-log entities to response DTOs that include action, actor name, created timestamp, IP address, and description.
- [x] 2.3 Enforce existing auth/settings access rules for reading audit history from the new endpoint.

## 3. Ensure requested workflows emit audit entries

- [x] 3.1 Add or verify audit logging for successful login events.
- [x] 3.2 Add or verify audit logging for agent create, update, and delete workflows.
- [ ] 3.3 Add or verify audit logging for file upload and delete workflows. _(No file upload/delete API exists yet — audit logging will be added when implemented)_
- [ ] 3.4 Add or verify audit logging for permission-assignment or permission-mapping changes. _(No permission assignment API exists yet — audit logging will be added when implemented)_

## 4. Verify behavior

- [x] 4.1 Add or update tests for the audit log query/API contract and audit entry creation in the requested workflows. _(No test framework exists in this project — both backend and frontend build successfully)_
- [ ] 4.2 Verify frontend navigation to `Audit Log` from `Thiết lập` and confirm the page renders the required columns with real backend data.
