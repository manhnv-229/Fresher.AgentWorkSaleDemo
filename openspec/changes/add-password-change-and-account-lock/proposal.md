## Why

The current authentication flow supports login, refresh, logout, and account status checks, but it does not give authenticated users a safe way to rotate their own password or give admins a product-level workflow to lock risky accounts. That gap leaves common account-security actions dependent on direct database edits and makes the existing `AccountStatus` model incomplete from an operational perspective.

## What Changes

- Add an authenticated password change flow that requires the current password, validates the new password, updates the stored password hash, and records password-change metadata.
- Add admin-managed account lock and unlock behavior so privileged users can prevent a target account from authenticating without deleting it.
- Revoke active sessions and refresh-token continuity when a password is changed or an account is locked so prior credentials cannot continue to access protected APIs.
- Expose the new account-security actions through backend APIs and extend the frontend authenticated experience with basic controls and feedback for these flows.
- Return explicit auth/account-state error responses so the frontend can distinguish invalid credentials, locked accounts, and forced reauthentication cases.

## Capabilities

### New Capabilities
- `account-security`: Password change for authenticated users, admin account lock or unlock operations, and session invalidation rules tied to those events.

### Modified Capabilities

## Impact

- Affected backend areas: `Demo.Application` auth DTOs/services, `Demo.Api` auth or admin controllers, authorization rules, and related error contracts.
- Affected persistence: user status and password-change handling, plus session or refresh-token revocation logic already stored in the database.
- Affected frontend areas: authenticated account UI for password change and an admin-facing control surface for account locking.
- Affected validation/testing: auth service tests, authorization coverage, and end-to-end verification for login, password rotation, lockout, unlock, and forced logout behavior.
