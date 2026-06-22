## Why

The current audit log filtering direction uses separate inputs for each field, which makes the settings page feel heavy and turns a simple review flow into a form-filling experience. We need a more compact audit toolbar now so administrators can search quickly with one text box and open richer filters only when needed.

## What Changes

- Replace the multiple visible audit-log search fields with one unified search input for quick lookup.
- Show only one filter button with a filter icon next to the search input, and open a context menu or popover when the user presses it.
- Support predefined time filters in the menu: `Hôm nay`, `Hôm qua`, `Tuần này`, `Tuần trước`, `Tháng này`, `Tháng trước`, `Năm nay`, and `Năm trước`.
- Support action-type filtering inside the same menu by using multi-select checkboxes for audit action categories.
- Keep backend-driven filtering so the unified search text and menu filters still query the server instead of narrowing only in the browser.

## Capabilities

### New Capabilities

- None.

### Modified Capabilities

- `settings-audit-log`: The audit log settings page will use a unified search bar plus a single filter-menu interaction for date presets and action-type selection.

## Impact

- Affected frontend area: `frontend/src/views/SettingsAuditLogPage.vue`, shared input/button/popover components, and audit-log page state handling.
- Affected backend area: audit-log query request models and filtering logic to support one free-text search plus time-preset and action-list filters.
- Affected UX: the audit log toolbar becomes lighter and more focused while still allowing combined filtering for investigations.
- Compatibility impact: implementations based on separate visible field inputs will need to be updated to the unified search and filter-menu model.
