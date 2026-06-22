## 1. Member detail interaction

- [x] 1.1 Update the settings member list interaction so clicking a member opens a detail popup instead of relying on row-level status actions.
- [x] 1.2 Design the popup content to show employee information, current account status, editable `job_position`, and the lock/unlock action in one focused surface.

## 2. Backend member update flow

- [x] 2.1 Add or extend backend DTOs and API endpoints needed to update an employee `job_position` from member management.
- [x] 2.2 Implement service and persistence logic for updating `job_position` with existing authorization boundaries preserved.
- [x] 2.3 Return updated member data from profile/status mutations so the frontend can refresh both the popup and the table row.

## 3. Audit logging

- [x] 3.1 Create an audit-log action for successful employee `job_position` updates with actor, target employee context, and request metadata.
- [x] 3.2 Ensure the audit log description clearly communicates the old/new or resulting `job_position` in a human-readable format.

## 4. Verification

- [ ] 4.1 Add or update backend tests for popup-driven member update APIs, authorization, and audit-log creation.
- [ ] 4.2 Add or update frontend tests or manual verification for popup open/close behavior, `job_position` editing, and moved lock/unlock actions.
