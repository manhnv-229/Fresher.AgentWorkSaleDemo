## 1. Extend the audit log filter contract

- [x] 1.1 Define a filter request model for `Action`, `UserName`, `CreatedDate` range, `IPAddress`, and `Description`.
- [x] 1.2 Update the audit-log API endpoint and frontend client call so selected filters are sent to the backend.
- [x] 1.3 Implement repository/query predicates that apply each provided filter and combine them in one audit-log query.

## 2. Build the audit log filter UI

- [x] 2.1 Add filter controls to the settings audit log page for `Action`, `UserName`, `CreatedDate`, `IPAddress`, and `Description`.
- [x] 2.2 Add apply/reset behavior so administrators can submit combined filters and return to the unfiltered audit list.
- [x] 2.3 Show appropriate loading and empty-results states when filtered audit queries are in progress or return no matches.

## 3. Verify filtering behavior

- [ ] 3.1 Add or update backend tests for single-field and multi-field audit-log filtering, including created-date range behavior. _(No test framework exists in this project — backend builds successfully)_
- [ ] 3.2 Add or update frontend tests or manual verification coverage for applying filters, resetting filters, and rendering no-match results. _(No test framework exists in this project — frontend builds successfully)_
