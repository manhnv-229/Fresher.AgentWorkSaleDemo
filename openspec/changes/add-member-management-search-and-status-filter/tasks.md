## 1. Member list query contract

- [x] 1.1 Add or extend member-management query parameters/DTOs to accept one search text input and one optional employee-status filter.
- [x] 1.2 Implement backend filtering logic so member search and status filtering can be applied together in the admin user list flow.

## 2. Member-management UI

- [x] 2.1 Add a search bar to `Quản lý thành viên` and wire it to the member list query state.
- [x] 2.2 Add a status filter control for employee `Trạng thái` and combine it with the search state when requesting member data.
- [x] 2.3 Ensure filtered results still support opening the member detail popup and show a distinct empty-results state when nothing matches.

## 3. Verification

- [ ] 3.1 Add or update backend tests for search matching, status filtering, and combined filter behavior in the admin user list API.
- [ ] 3.2 Add or update frontend tests or manual verification for search input behavior, status filter behavior, popup interaction on filtered rows, and empty-results rendering.
