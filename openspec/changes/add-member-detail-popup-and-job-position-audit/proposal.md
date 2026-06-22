## Why

The member list now shows better employee context, but administrators still have to act directly from the table and cannot inspect or edit an employee in a focused detail view. We need a member popup so admins can review employee information, update `job_position`, move lock/unlock controls into the same detail surface, and preserve an audit trail for profile changes.

## What Changes

- Add a member-detail popup that opens when an administrator clicks a member from the `Quản lý thành viên` list.
- Show employee information in the popup using the member data already exposed for the list, plus any detail fields needed for a clear read-only summary.
- Allow authorized administrators to update the selected employee's `job_position` from the popup.
- Move the lock/unlock employee action from the table row into the popup so account state changes happen in the same contextual workflow.
- Record an audit log entry whenever an employee's `job_position` is updated, including actor identity and request context.
- Preserve existing permission boundaries for viewing member data and changing account status.

## Capabilities

### New Capabilities

- None.

### Modified Capabilities

- `account-security`: Member management will support opening a detail popup, editing `job_position`, and performing lock/unlock actions from that popup.
- `audit-log-tracking`: Updating an employee's `job_position` from member management will create an immutable audit log entry.

## Impact

- Affected frontend area: `SettingsMembersPage.vue`, member row interaction patterns, popup state management, and update/lock action flows.
- Affected backend area: admin user endpoints, DTOs for member detail/update payloads, service logic for `job_position` updates, and authorization handling.
- Affected audit area: audit log service/repository integration for employee profile updates.
- Affected UX: administrators will use a popup-centered workflow instead of row-level actions in the member table.
