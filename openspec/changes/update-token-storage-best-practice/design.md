## Context

The backend already supports JWT access tokens, refresh token rotation, hashed refresh token storage, and database-backed sessions. The Vue frontend currently persists both access token and refresh token in `localStorage` under `demo.auth`, which makes manual testing easy but exposes both tokens to any successful XSS.

The target architecture is a browser-friendly split: access token in Vue memory only, refresh token in a backend-managed `HttpOnly` cookie, and refresh-token hash/session state in MySQL. This keeps the backend as the source of truth for long-lived session continuity while the frontend only holds the short-lived bearer token needed for API calls.

## Goals / Non-Goals

**Goals:**

- Remove raw refresh token access from frontend JavaScript.
- Store access token only in in-memory Vue auth state.
- Set refresh token as an `HttpOnly` cookie on login and refresh.
- Rotate the refresh-token cookie whenever `/api/auth/refresh-token` succeeds.
- Clear the refresh-token cookie when logout succeeds.
- Support silent access-token restoration after page reload by calling refresh with cookie credentials.
- Preserve the existing database-backed refresh token and session validation model.
- Keep local development working between Vite `http://localhost:5173` and API `http://localhost:5066`.

**Non-Goals:**

- MFA, device management, or full session administration UI.
- Moving access tokens to cookies.
- Implementing CSRF token double-submit protection in this change; SameSite cookie settings are included, and CSRF hardening can be added later if cross-site flows become necessary.
- Replacing JWT access tokens or tenant permission checks.
- Full router-based protected page implementation beyond the current login/demo state.

## Decisions

1. **Refresh token is transported through an `HttpOnly` cookie.**

   The backend will set a cookie such as `demo.refresh_token` after successful login and refresh. The cookie value is the raw refresh token secret; the database continues storing only its SHA-256 hash. JavaScript cannot read the cookie because it is `HttpOnly`.

   Alternative considered: keep refresh token in `localStorage`. This preserves the current flow but keeps the highest-value token exposed to XSS, so it does not meet the security goal.

2. **Access token is stored in memory only.**

   The Vue auth composable/store will hold `accessToken` and `accessTokenExpiresAt` in reactive memory. A full page reload clears the access token, then the app can call `/api/auth/refresh-token` with cookie credentials to obtain a fresh one.

   Alternative considered: use `sessionStorage`. It avoids cross-tab persistence but is still readable by JavaScript, so memory-only is the safer default for a short-lived token.

3. **Refresh and logout use cookie-first semantics.**

   `/api/auth/refresh-token` should read the refresh token from the cookie by default. `/api/auth/logout` should revoke the refresh token/session associated with the cookie and clear the cookie. Request-body refresh tokens can be kept temporarily for Swagger/backward compatibility if desired, but the browser frontend must not depend on them.

   Alternative considered: add separate browser-only endpoints. Reusing the existing endpoints keeps API surface small and makes the semantic change easier to test.

4. **Frontend HTTP requests include credentials where cookie auth is needed.**

   Login, refresh, and logout requests will use `credentials: "include"`. CORS must allow credentials and the exact Vite origin, not `AllowAnyOrigin`.

   Alternative considered: rely on same-origin proxy only. That works in development but hides production cookie/CORS requirements.

5. **Cookie security settings are environment-aware.**

   Cookie options should include `HttpOnly = true`, `SameSite = Lax`, a path scoped to auth APIs where practical, and an expiration aligned with the refresh token. `Secure` should be true for HTTPS/production; local HTTP development may require `Secure = false`.

   Alternative considered: always `Secure = true`. This is production-correct but breaks plain HTTP local testing on `localhost:5066`.

## Risks / Trade-offs

- **Access token disappears on reload** -> The app must attempt a refresh on startup and show the login form only when refresh fails.
- **Cookie + CORS misconfiguration can break login in browser** -> Configure explicit local origins with credentials and verify preflight behavior.
- **HttpOnly cookie does not remove all CSRF risk** -> Use `SameSite=Lax` initially; add anti-CSRF tokens later if cross-site credentialed requests are introduced.
- **Swagger cannot read HttpOnly cookies from JSON responses** -> Keep API testing path documented, or temporarily allow request-body refresh tokens during demo while frontend uses cookie flow.
- **Multiple tabs share the refresh cookie but not memory state** -> Each tab can refresh independently on load; rotation must continue revoking old refresh tokens correctly.

## Migration Plan

1. Add backend refresh-token cookie helpers/options.
2. Change login to set the refresh cookie and return access-token metadata without requiring frontend refresh-token persistence.
3. Change refresh endpoint to read from cookie, rotate token, set replacement cookie, and return a new access token.
4. Change logout endpoint to read from cookie, revoke token/session, clear cookie, and return success.
5. Update CORS for credentialed local frontend requests.
6. Update frontend auth store/composable so access token is memory-only and startup refresh restores auth state.
7. Update HTTP client to include credentials for auth endpoints and attach bearer token from memory for protected API calls.
8. Verify browser login, reload refresh, protected API call, logout, and post-logout access-token/session invalidation.
