## Context

The repository currently has a backend API with `POST /api/auth/login`, refresh token support, session-backed JWT validation, Swagger, and demo users. The `frontend` directory exists but has no Vue application files yet. The requested UI is a compact login form with a phone/email input, password input, password visibility icon, and blue submit button.

## Goals / Non-Goals

**Goals:**

- Scaffold a Vue application in `frontend`.
- Make the login form the first screen, not a landing page.
- Match the provided compact form proportions and Vietnamese text.
- Call the backend login endpoint and handle success/failure.
- Store access token, refresh token, and expiration metadata for demo use.
- Provide a visible authenticated state after successful login.
- Keep the UI responsive and usable on mobile and desktop.

**Non-Goals:**

- Full dashboard, tenant list, or agent management UI.
- Registration, forgot-password, MFA, or refresh-token automation.
- Production-grade secure token storage hardening beyond a demo-friendly local approach.
- SSR or multi-page routing unless needed later.

## Decisions

1. **Use Vue 3 with Vite in `frontend`.**

   Vite is a small, standard choice for a new Vue single-page app and keeps implementation quick. The app can be run separately from the ASP.NET API during development.

   Alternative considered: serve static HTML from `Demo.Api`. That would be simpler but would not satisfy the requested Vue frontend direction.

2. **Use a single `LoginView` as the initial app surface.**

   The app should open directly to the usable login form. After login, it can swap to a compact success panel showing the signed-in user/token state and a logout button.

   Alternative considered: add marketing or explanatory content around the form. That is unnecessary for this operational demo and would slow testing.

3. **Use fetch-based API client with configurable base URL.**

   The frontend will read `VITE_API_BASE_URL` and default to `http://localhost:5066`, then call `/api/auth/login`.

   Alternative considered: hard-code the full API URL everywhere. A tiny API helper avoids duplication and makes local port changes easier.

4. **Store tokens in local storage for demo continuity.**

   Access token and refresh token can be stored in local storage so a page refresh preserves the demo login state.

   Alternative considered: memory-only storage. That is safer against persistence risk but less convenient for manual testing. This remains a demo frontend, and future production hardening can move tokens to more secure handling.

5. **Use a native button icon for password visibility.**

   The password input will include an accessible icon button to toggle between `password` and `text`. The control must not shift layout or overlap text.

   Alternative considered: text label such as "Show". The provided visual uses an eye icon, and an icon button matches expected password-field behavior.

## Risks / Trade-offs

- **Backend CORS may block a separate dev server** -> Add or document CORS configuration during implementation if the browser rejects requests.
- **Local storage is not production-hardened** -> Accept for demo scope and document that production should use a stronger token storage strategy.
- **API unavailable during UI testing** -> Show a clear error message and keep form state editable.
- **Swagger and frontend both test auth** -> Keep the frontend focused on login UX and post-login confirmation, not full API exploration.

## Migration Plan

1. Scaffold Vue/Vite files under `frontend`.
2. Add environment configuration for backend API base URL.
3. Implement login form, validation, loading, error, and password visibility states.
4. Implement API client and token persistence.
5. Add a post-login state and logout button that clears local frontend state.
6. Run frontend build and start dev server for manual verification.
