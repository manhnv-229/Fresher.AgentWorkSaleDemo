## Context

The current `Quản lý thành viên` page and admin user API are intentionally small: they expose enough data to identify an account by name or email, inspect status, view password-change timing, and lock or unlock the account. That is useful for basic security operations, but it is not enough for real employee administration because admins cannot easily distinguish staff by employee code, current project, or job position.

This change touches both backend and frontend contracts. The backend needs to provide richer employee metadata in the admin user summary, and the frontend needs to render those additional fields in the settings member table without losing the existing account-status actions. Depending on how the repo currently stores user data, the change may also require a user-entity extension or a related employee-profile structure.

## Goals / Non-Goals

**Goals:**
- Expose `Mã nhân viên`, `Họ tên`, `Dự án`, `Vị trí công việc`, `Email/thông tin tài khoản đăng nhập hệ thống`, and `Status` in the admin member-management flow.
- Extend the admin user DTO/API contract so the frontend receives those fields from one member-list request.
- Preserve the existing lock/unlock actions and current account-security authorization boundaries.
- Allow the UI to render incomplete employee metadata safely when some optional fields are not yet populated.

**Non-Goals:**
- Building full employee CRUD, edit forms, invitation flows, or HR-style profile management.
- Changing authentication credentials or introducing separate profile-edit workflows in this change.
- Redesigning settings navigation or unrelated account-security behaviors.

## Decisions

1. Expand the existing admin user summary contract instead of creating a separate employee-detail endpoint.

   The admin member list already depends on `AdminUserSummary`, so the simplest consistent path is to extend that summary with the new employee-facing fields and keep the list request as the primary data source for the page.

   Why this approach:
   - It preserves the current one-request page load model.
   - It keeps the member-management UI simple and avoids introducing a detail screen prematurely.
   - It aligns with the user's request, which is about enriching the displayed member information rather than creating a new workflow.

   Alternative considered: add a separate employee profile endpoint per row. This is rejected because it complicates the page without a matching requirement for row-level detail navigation.

2. Treat employee metadata as account-adjacent profile data returned with the security summary.

   The application layer will continue to use account status and lock/unlock operations from the existing user-management flow, but the summary DTO will now include employee code, full name, project, job title/position, and login-account display information alongside status.

   Why this approach:
   - It keeps account-security operations and employee identification in one administrative surface.
   - It matches how operators read the page: first identify the employee, then inspect or change account status.
   - It allows persistence choices to stay flexible, whether the fields live directly on `User` or in a related profile record.

   Alternative considered: limit the API to only email and status, and have the frontend synthesize the rest. This is rejected because project and job-position data must come from authoritative backend state.

3. Keep status as a first-class field in the richer employee table.

   Even after adding more employee context, `Status` remains an explicit column or visible field in the member-management view because account state is still the operational control point for this screen.

   Why this approach:
   - It preserves the existing value of the page as an account-security surface.
   - It prevents richer profile data from burying the most important actionable signal.

   Alternative considered: move status into a secondary detail section. This is rejected because it would weaken the page's primary operational purpose.

4. Allow optional rendering for fields that may be absent in seeded or legacy accounts.

   The UI and API should tolerate missing employee code, project, or job-position values while the system is backfilled or while some accounts remain minimally configured.

   Why this approach:
   - It reduces migration pressure and allows incremental data completion.
   - It prevents the richer member list from failing on legacy/demo accounts.

   Alternative considered: require all new fields immediately for every account. This is rejected because current data likely does not yet contain every requested field.

## Risks / Trade-offs

- [The current `User` entity may not have enough fields for the requested employee metadata] -> Keep the spec focused on returned behavior while allowing implementation to choose direct user columns or a related profile structure.
- [A wider member table may become dense on smaller screens] -> Group identity fields thoughtfully and verify responsive rendering in the settings page layout.
- [Legacy or seeded accounts may have partially missing employee metadata] -> Render fallback values consistently and avoid treating absent optional fields as API errors.
- [Extending the summary DTO changes frontend/backend contracts together] -> Update shared types, API handlers, and member-page rendering in the same implementation slice with regression coverage.
