## 1. Extend employee profile data for member management

- [ ] 1.1 Add persistence support for employee metadata needed by member management: employee code, project, and job position, while reusing existing full name, email, and account status data.
- [ ] 1.2 Update seed or demo data so member-management responses include realistic values for the new employee fields where appropriate.

## 2. Expand the admin user-management contract

- [ ] 2.1 Extend `AdminUserSummary` and related application/service mappings to return `Mã nhân viên`, `Họ tên`, `Dự án`, `Vị trí công việc`, `Email/thông tin tài khoản đăng nhập hệ thống`, and `Status`.
- [ ] 2.2 Update the admin users API and any repository/service logic so the member list returns the richer employee profile data together with existing lock/unlock behavior.
- [ ] 2.3 Ensure the API contract handles missing optional employee fields safely for legacy or partially populated accounts.

## 3. Update the settings member-management UI

- [ ] 3.1 Update frontend user types and API consumers to use the expanded member summary shape.
- [ ] 3.2 Redesign the `Quản lý thành viên` table or grouped cells to display employee code, full name, project, job position, login account information, and status clearly.
- [ ] 3.3 Preserve lock/unlock actions and verify the page still renders sensible fallback values when some employee fields are empty.

## 4. Verify behavior

- [ ] 4.1 Add or update backend tests covering the expanded admin user summary contract.
- [ ] 4.2 Add or update frontend tests or manual verification for the richer member list rendering and unchanged lock/unlock actions.
