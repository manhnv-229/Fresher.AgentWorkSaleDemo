## Why

Logout currently revokes the refresh token, but already-issued access tokens remain valid until expiration because JWT validation is stateless. Adding database-backed sessions lets the system invalidate access tokens immediately after logout and centralizes session state for future security controls.

## What Changes

- Add a persisted user session record created during login.
- Include a session id claim in every access token.
- Link refresh tokens to the session that issued them.
- Check the session id against the database during every authenticated API request.
- Mark the session revoked during logout so access tokens from that session are rejected immediately.
- Keep refresh token rotation, but ensure rotated refresh tokens remain tied to the same active session.
- Reject refresh and API access when the related session is expired, revoked, or missing.
- Add indexes and schema updates for session lookup and refresh-token/session relationships.

## Capabilities

### New Capabilities

- `db-backed-sessions`: Database-backed user sessions for access-token validation, logout invalidation, and refresh-token session binding.

### Modified Capabilities

- None.

## Impact

- Affected API behavior: all authenticated requests will perform a session lookup after JWT signature/lifetime validation.
- Affected auth behavior: logout invalidates both future refresh and current access-token use for the same session.
- Affected JWT design: access tokens gain a `sid` claim while still avoiding embedded roles or permissions.
- Affected persistence: add `user_sessions` table and add `session_id` to `refresh_tokens`.
- Affected code: `Demo.Domain`, `Demo.Application`, `Demo.Infrastructure`, `Demo.Api`, `backend/database/init.sql`, and Swagger/API test notes.
