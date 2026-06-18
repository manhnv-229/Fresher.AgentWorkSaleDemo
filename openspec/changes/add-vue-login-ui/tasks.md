## 1. Frontend Scaffold

- [x] 1.1 Create Vue/Vite project files in `frontend`.
- [x] 1.2 Add package scripts for development, build, and preview.
- [x] 1.3 Add environment configuration for `VITE_API_BASE_URL`.
- [x] 1.4 Add base app entry files and global CSS reset.

## 2. API and Auth State

- [x] 2.1 Implement a small API client for `POST /api/auth/login`.
- [x] 2.2 Define login request and token response types or interfaces.
- [x] 2.3 Implement token persistence for access token, refresh token, and expiration metadata.
- [x] 2.4 Implement local logout that clears stored frontend auth state.

## 3. Login UI

- [x] 3.1 Build the initial login form with phone/email input, password input, and `Đăng nhập` button.
- [x] 3.2 Style the form to match the provided compact visual and remain responsive.
- [x] 3.3 Add password visibility toggle with an accessible icon button.
- [x] 3.4 Add required-field validation before calling the backend.
- [x] 3.5 Add loading, disabled, success, and error states.

## 4. Backend Integration

- [x] 4.1 Configure frontend dev server proxy or backend CORS so browser login requests work locally.
- [x] 4.2 Ensure the app defaults to the local API at `http://localhost:5066`.
- [x] 4.3 Show a post-login authenticated state after successful backend login.

## 5. Verification

- [x] 5.1 Run frontend dependency install if needed.
- [x] 5.2 Run frontend build and fix compile errors.
- [x] 5.3 Start the frontend dev server and record the local URL.
- [x] 5.4 Verify empty form validation prevents API calls.
- [x] 5.5 Verify invalid credentials show an error.
- [x] 5.6 Verify `admin@example.com` / `Password123!` logs in and stores tokens.
- [x] 5.7 Verify local logout clears tokens and returns to the login form.
