## Why

The new `Audit Log` page lets administrators review activity history, but the list becomes much less useful once records grow because operators cannot narrow results to a specific action, user, date, IP, or description. We need field-level search and filtering now so audit reviews stay practical during investigations and daily operations.

## What Changes

- Add search and filter controls for each audit log field shown in the settings page: `Action`, `UserName`, `CreatedDate`, `IPAddress`, and `Description`.
- Support combining multiple field filters at the same time so administrators can narrow audit history precisely.
- Treat `CreatedDate` as a date-based filter instead of plain free-text search so users can constrain logs by time window.
- Extend the audit-log query contract so the frontend can request filtered results from the backend instead of loading and scanning the full list locally.
- Preserve the current read-only audit review experience while making the result list react to the selected search criteria.

## Capabilities

### New Capabilities

- None.

### Modified Capabilities

- `settings-audit-log`: The audit log settings page will support field-level search and filtering across all displayed columns.

## Impact

- Affected frontend area: `frontend/src/views/SettingsAuditLogPage.vue`, related form/table components, and audit-log API client calls.
- Affected backend area: audit-log query endpoint, request models, and repository/query logic for filtering by action, user, created date, IP address, and description.
- Affected UX: administrators can combine filters to inspect a narrow slice of audit history without losing the existing read-only settings workflow.
- Performance impact: filtering should be performed by the backend query layer so growing audit datasets do not require the page to load every row before narrowing results.
