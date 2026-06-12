## Why

The backend authentication API is ready, but users currently need Swagger or raw HTTP requests to log in. A small Vue login UI gives a realistic browser flow for entering email/password, receiving tokens, and preparing the frontend for later tenant/agent screens.

## What Changes

- Scaffold a Vue frontend in the existing `frontend` directory.
- Build a login screen matching the provided visual: email/phone input, password input with visibility toggle, and primary blue login button.
- Submit login credentials to `POST /api/auth/login`.
- Show loading, validation, and authentication error states.
- Store returned access token and refresh token in a frontend auth state suitable for demo use.
- Provide a post-login authenticated state that confirms login and can later link to protected pages.
- Configure the frontend API base URL for local backend development.

## Capabilities

### New Capabilities

- `vue-login-ui`: Vue login interface and client-side authentication flow for the existing backend login API.

### Modified Capabilities

- None.

## Impact

- Affected area: `frontend`.
- New frontend dependencies expected: Vue, Vite, TypeScript or JavaScript build tooling, and optionally an icon source for the password visibility control.
- Backend impact: none expected beyond using the existing `POST /api/auth/login` API and local CORS if needed during implementation.
- User workflow impact: users can test login from a browser UI instead of Swagger only.
