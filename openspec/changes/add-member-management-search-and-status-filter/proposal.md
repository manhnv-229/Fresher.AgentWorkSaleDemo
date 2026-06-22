## Why

The member-management page is growing richer with employee data and detail actions, so admins now need faster ways to find the right person without scanning the entire list manually. Adding search and employee-status filtering will make the member directory practical at larger scale and keep the workflow efficient as more records appear.

## What Changes

- Add a search bar to the `Quản lý thành viên` page for quickly finding employees from the member list.
- Add filtering by employee `Trạng thái` so admins can narrow the list to relevant account states.
- Define which member fields are included in search matching for the employee-management workflow.
- Ensure search and status filters work together with the existing member table and detail popup workflow.
- Update the member-management query contract and UI state so empty-result cases are handled cleanly.

## Capabilities

### New Capabilities

- None.

### Modified Capabilities

- `account-security`: The admin user-management flow will support member search and employee-status filtering in addition to listing, popup detail, and account actions.

## Impact

- Affected frontend area: `SettingsMembersPage.vue`, member list toolbar, page state, and empty-results rendering.
- Affected backend area: admin user list API, query DTOs/parameters, repository or service filtering logic, and response shaping for filtered results.
- Affected UX: administrators can find employees by search text and isolate specific statuses before opening the member detail popup.
