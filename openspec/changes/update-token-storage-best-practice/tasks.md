## 1. Backend Cookie Contract

- [x] 1.1 Add refresh-token cookie constants/options in the API layer.
- [x] 1.2 Configure refresh-token cookie with `HttpOnly`, `SameSite=Lax`, environment-aware `Secure`, auth path, and refresh expiration.
- [x] 1.3 Update local CORS policy to allow credentialed requests from Vite origins without wildcard origins.
- [x] 1.4 Keep Swagger/demo compatibility documented or supported for refresh/logout request bodies if needed.

## 2. Backend Auth Endpoint Updates

- [x] 2.1 Update login endpoint to set the refresh-token cookie after successful login.
- [x] 2.2 Update login response contract so frontend does not need to persist raw refresh token.
- [x] 2.3 Update refresh endpoint to read refresh token from cookie by default.
- [x] 2.4 Ensure refresh rotation revokes the old token, creates a new token, and replaces the refresh cookie.
- [x] 2.5 Update logout endpoint to read refresh token from cookie by default.
- [x] 2.6 Ensure logout revokes the refresh token/session and clears the refresh cookie.
- [x] 2.7 Preserve database hash storage, rotation metadata, and session validation behavior.

## 3. Frontend Auth State Refactor

- [x] 3.1 Remove access token and refresh token persistence from `localStorage`.
- [x] 3.2 Store access token and access-token expiration only in memory auth state.
- [x] 3.3 Update login API call to send credentials and consume access-token response only.
- [x] 3.4 Add refresh API call that sends credentials and restores access token from the cookie.
- [x] 3.5 Attempt startup refresh before deciding the user is logged out.
- [x] 3.6 Update logout to call backend logout with credentials and clear memory state.
- [x] 3.7 Update shared HTTP client to support credentialed auth calls and bearer access token injection for protected APIs.

## 4. UI Behavior

- [x] 4.1 Keep login form behavior unchanged for initial unauthenticated users.
- [x] 4.2 Show authenticated state after login using memory access-token state.
- [x] 4.3 Preserve authenticated state after page reload when cookie refresh succeeds.
- [x] 4.4 Return to login form when refresh fails or logout succeeds.
- [x] 4.5 Ensure DevTools `localStorage` and `sessionStorage` do not contain access or refresh tokens.

## 5. Verification

- [x] 5.1 Build backend and fix compile errors.
- [x] 5.2 Build frontend and fix compile errors.
- [x] 5.3 Verify login sets an `HttpOnly` refresh-token cookie and does not store tokens in browser storage.
- [x] 5.4 Verify page reload refreshes access token from the cookie.
- [x] 5.5 Verify refresh rotates the database refresh token and replaces the cookie.
- [x] 5.6 Verify logout clears the cookie, revokes the session, and old access token calls return unauthorized.
- [x] 5.7 Verify local Vite frontend can call the API with credentials under the configured CORS policy.
