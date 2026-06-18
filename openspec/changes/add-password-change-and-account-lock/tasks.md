## 1. Backend account-security contracts

- [x] 1.1 Add application DTOs and service interface methods for self-service password change, admin user summaries, and lock/unlock commands.
- [x] 1.2 Extend auth and user repository contracts so services can load admin-facing user lists and revoke all active sessions for a target user.
- [x] 1.3 Add or refine auth error codes so locked-account and forced-reauthentication paths are distinguishable from invalid credentials.

## 2. Backend account-security behavior

- [x] 2.1 Implement password-change service logic that verifies the current password, writes a new BCrypt hash, updates `PasswordChangedAt`, and revokes all active sessions for that user.
- [x] 2.2 Implement admin user-management service logic that lists users with status metadata and updates account status for lock or unlock actions.
- [x] 2.3 Update login and refresh handling so locked accounts return explicit lock-related failures and revoked sessions cannot continue after password or status changes.
- [x] 2.4 Add API endpoints for password change plus admin user list and lock/unlock actions, protected by existing authentication and `user.view` or `user.update` permissions.

## 3. Frontend account-security experience

- [x] 3.1 Add an authenticated password-change UI that collects current and new passwords, handles validation, and reacts cleanly to forced logout after success.
- [x] 3.2 Add an admin-only user-management panel in the authenticated workspace that lists users, shows current status, and exposes lock/unlock controls.
- [x] 3.3 Update frontend auth error handling so locked-account responses and revoked-session reauthentication flows show clear user feedback.

## 4. Verification

- [x] 4.1 Verify a successful password change updates credentials and causes old access or refresh tokens to fail on subsequent requests.
- [x] 4.2 Verify an incorrect current password does not change stored credentials or revoke sessions.
- [x] 4.3 Verify locking an account blocks login, blocks refresh, and invalidates already-active sessions.
- [x] 4.4 Verify unlocking a locked account allows a fresh login again with the current password.
- [x] 4.5 Run relevant backend and frontend smoke checks or builds and record any remaining environment limitations.
