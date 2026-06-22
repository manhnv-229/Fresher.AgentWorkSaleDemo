## 1. Extend employee profile data for member management

- [x] 1.1 Add persistence support for employee metadata needed by member management: employee code, project, and job position, while reusing existing full name, email, and account status data.
- [x] 1.2 Update seed or demo data so member-management responses include realistic values for the new employee fields where appropriate.
- [x] 1.3 Remove `password_changed_at` from the user data model used by member management and ensure `modified_at` remains the reused timestamp field.

## 2. Expand the admin user-management contract

- [x] 2.1 Extend `AdminUserSummary` and related application/service mappings to return the data needed for `Nhân viên`, `Vị trí công việc`, `Dự án`, `Email`, and `Trạng thái`, with `Full name` and `Mã nhân viên` grouped for the employee cell.
- [x] 2.2 Remove `password_changed_at` from the admin users API contract and update any repository/service logic to rely on `modified_at` where timestamp data is still required.
- [x] 2.3 Ensure the API contract handles missing optional employee fields safely for legacy or partially populated accounts while preserving existing lock/unlock behavior.

## 3. Update the settings member-management UI

- [x] 3.1 Update frontend user types and API consumers to use the expanded member summary shape.
- [x] 3.2 Redesign the `Quản lý thành viên` table to show exactly `Nhân viên`, `Vị trí công việc`, `Dự án`, `Email`, and `Trạng thái`, with `Full name` above `Mã nhân viên` in the employee cell.
- [x] 3.3 Remove the `password_changed_at` column from the page, preserve lock/unlock actions, and verify the page still renders sensible fallback values when some employee fields are empty.

## 4. Verify behavior

- [ ] 4.1 Add or update backend tests covering the expanded admin user summary contract and removal of `password_changed_at`.
- [ ] 4.2 Add or update frontend tests or manual verification for the five-column member list rendering, grouped employee cell, and unchanged lock/unlock actions.
