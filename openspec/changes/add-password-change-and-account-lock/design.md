## Context

The current system already supports email/password login, refresh-token rotation, logout, and database-backed session validation. The domain model includes `AccountStatus` values such as `Active`, `Inactive`, `Locked`, and `Suspended`, and the `User` entity already stores `PasswordHash` plus `PasswordChangedAt`. However, those fields are only partially exercised today: users cannot change their own password through the product, admins have no first-class user management surface for account state changes, and operational lockouts still require direct persistence edits.

The repository also already seeds `user.view` and `user.update` permissions, exposes an authenticated admin dashboard after login, and uses Clean Architecture boundaries across API, Application, Domain, and Infrastructure. This change should extend those existing patterns rather than introducing a separate identity subsystem.

## Goals / Non-Goals

**Goals:**

- Add a secure self-service password change flow for authenticated users.
- Add an admin-only account management flow to view users and lock or unlock accounts.
- Reuse existing session validation so password change and account lock events immediately cut off active access.
- Reuse seeded user permissions and existing backend/frontend architecture.
- Return clear machine-readable error states for locked accounts and forced reauthentication paths.

**Non-Goals:**

- Forgot-password, email verification, OTP, MFA, or password-reset-by-email workflows.
- Full user CRUD, invitation management, role editing, or tenant membership editing.
- Device/session history UI beyond revoking active sessions as part of password or lock events.
- Introducing ASP.NET Identity or replacing the current custom auth stack.

## Decisions

1. **Add one self-service auth endpoint and a small admin user-management surface.**

   The backend will add an authenticated password-change endpoint under `api/auth`, plus admin-oriented user listing and lock/unlock endpoints under an admin controller. The frontend will add a compact password-change form for the current user and an admin-only user list with lock/unlock actions inside the existing authenticated workspace.

   Alternative considered: implement backend-only commands with no UI. That is rejected because the request is for product functionality, and the current repo already uses the browser UI as the main operational surface after login.

2. **Password change must verify the current password and revoke all active sessions for that user.**

   A password change will require the current password, validate the new password against policy, write a new BCrypt hash, set `PasswordChangedAt`, and revoke the user's current sessions plus refresh-token continuity. This keeps a stolen access token or refresh token from surviving a credential rotation.

   Alternative considered: change the password but keep the current session alive for convenience. That is rejected because it weakens the security value of password rotation and creates inconsistent behavior across devices.

3. **Use `AccountStatus.Locked` as the operational lockout state and keep status changes explicit.**

   Admin lock will update the user status to `Locked`; unlock will move the user back to `Active`. Login and refresh will continue to treat any non-`Active` status as unauthenticated, but lock-specific responses should return a dedicated account-state error code so the UI can distinguish a locked account from invalid credentials.

   Alternative considered: add a separate lock table or temporary lock flag. That is rejected because the domain model already carries account lifecycle state, and a second lock mechanism would duplicate the source of truth.

4. **Reuse existing `user.view` and `user.update` permissions rather than creating new permission codes.**

   Listing users will require `user.view`, while lock/unlock actions will require `user.update`. This aligns with the permissions already seeded in `DatabaseSeeder` and keeps authorization consistent with the rest of the repo.

   Alternative considered: introduce dedicated `user.lock` permission codes. That is deferred because the current permission model is intentionally compact and the requested scope does not require finer-grained policy splits yet.

5. **Represent session invalidation as user-wide revocation, not only current-session revocation.**

   The implementation should revoke all active `user_sessions` for the affected user when an admin locks an account or when a user changes their password. Related refresh tokens should no longer refresh successfully because their session becomes revoked. This decision relies on the existing per-request session validation hook in JWT bearer auth.

   Alternative considered: revoke only the current session or only the session referenced by one refresh token. That is rejected because password changes and account locks are account-level security events, not device-local events.

6. **Keep admin account management minimal and focused on status, not profile editing.**

   The admin UI/API should expose the information needed to identify a user and act on account status, such as email, display name, current status, and password-changed timestamp. It should not expand into editing names, roles, tenants, or credentials for other users.

   Alternative considered: build a full user administration module now. That is rejected because it would broaden scope far beyond the requested security workflows.

## Risks / Trade-offs

- **Revoking all sessions after password change may feel disruptive** -> Favor security-first semantics and show a clear frontend message that re-login is required after the password is updated.
- **Admin user management needs a way to identify accounts without an existing user page** -> Keep the first UI small: a simple list with status badges and lock/unlock actions inside the current admin workspace.
- **Locking an account while it has active access tokens could otherwise leave a short grace period** -> Reuse database-backed session validation so revoked sessions fail on the next authenticated request.
- **Reusing `user.update` for lock/unlock is broader than a dedicated lock permission** -> Accept the coarser permission boundary for now and revisit only if broader user-edit features arrive later.
- **Unlocking always returns `Active` and may ignore pre-lock business nuance** -> Treat this as acceptable for the current demo workflow and revisit if more account lifecycle states become productized.

## Migration Plan

1. Extend application DTOs and service contracts for password change, admin user summaries, and lock/unlock commands.
2. Add repository support for querying users for admin views and revoking all active sessions for a user.
3. Implement auth service logic for current-password verification, password hash updates, `PasswordChangedAt`, and session revocation.
4. Add admin user-management endpoints protected by `user.view` and `user.update`.
5. Update auth/login/refresh error handling so locked-account responses are explicit and password-change-triggered reauthentication is handled consistently.
6. Add frontend account-security UI for password change and admin-only lock/unlock controls.
7. Validate login, refresh, password change, forced logout, locked login rejection, and unlock recovery flows.

Rollback is to remove the new endpoints and UI while preserving password and session data already written. If a rollback happens after users have changed passwords or admins have locked accounts, data should be left intact rather than trying to reconstruct prior credentials or sessions.

## Open Questions

- Should admins be allowed to lock their own account, or should the API reject self-lock to avoid accidental loss of the last admin session?
- Should unlock always return a user to `Active`, or should it restore a previous non-active state if that distinction becomes important later?
