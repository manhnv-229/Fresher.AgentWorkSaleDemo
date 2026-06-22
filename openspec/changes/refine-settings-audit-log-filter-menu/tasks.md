## 1. Replace the audit-log toolbar interaction

- [x] 1.1 Replace the multiple visible audit-log filter inputs with one shared search input in `SettingsAuditLogPage`.
- [x] 1.2 Add one filter-icon button that opens a context menu or popover for advanced audit filters.
- [x] 1.3 Build the filter menu UI with the requested time presets and multi-select action checkboxes, plus apply/reset behavior.

## 2. Update the audit-log filter contract

- [x] 2.1 Replace the field-by-field filter request model with a unified search text, one time-preset value, and a list of selected action types.
- [x] 2.2 Update the audit-log API client, controller, service, and repository filtering logic to use the new search and filter-menu contract.
- [x] 2.3 Implement time-preset boundary resolution for `Hôm nay`, `Hôm qua`, `Tuần này`, `Tuần trước`, `Tháng này`, `Tháng trước`, `Năm nay`, and `Năm trước`.

## 3. Verify unified search and filter-menu behavior

- [ ] 3.1 Add or update backend tests for unified text search, multi-action filtering, and each time-preset boundary. _(No test framework exists in this project — backend builds successfully)_
- [ ] 3.2 Add or update frontend tests or manual verification coverage for opening the filter menu, applying filters, resetting filters, and rendering no-match results. _(No test framework exists in this project — frontend builds successfully)_
