## Why

The current Vue login demo stores both access token and refresh token in `localStorage`, which is convenient for testing but not a good production security posture. Updating token storage now reduces XSS impact and aligns the auth flow with the session-backed refresh-token design already present in the backend.

## What Changes

- Move frontend access token storage from persistent `localStorage` to in-memory auth state.
- Stop exposing refresh tokens to frontend JavaScript.
- Have the backend set, rotate, and clear the refresh token via an `HttpOnly`, `Secure`-aware cookie.
- Update login, refresh-token, and logout API flows so refresh token is read from cookie by default.
- Keep refresh-token hashes, rotation metadata, and session association in the database.
- Update Swagger/demo compatibility carefully so API testing remains possible during development.

## Capabilities

### New Capabilities

- `secure-token-storage`: Defines browser token storage behavior, refresh-token cookie handling, token refresh, and logout semantics.

### Modified Capabilities

- None.

## Impact

- Affected backend code: `Demo.Api` auth controller/CORS setup, auth DTOs, auth service contracts, refresh/logout handling, cookie options.
- Affected frontend code: `features/auth`, `services/http`, auth state store/composable, login/logout UI behavior.
- Affected API behavior: login response should no longer require the client to persist raw refresh token; refresh/logout should support cookie-based refresh token flow.
- Security impact: refresh token is no longer readable by browser JavaScript; access token survives only in memory and is lost on full page reload unless refreshed from the cookie.
