## 1. Persistence Model

- [x] 1.1 Add `UserSession` domain entity with user id, expiration, revocation, IP metadata, and navigation properties.
- [x] 1.2 Add `SessionId` to `RefreshToken` and configure refresh-token to session relationship.
- [x] 1.3 Add `DbSet<UserSession>` and EF Core configuration for `user_sessions`.
- [x] 1.4 Update `refresh_tokens` EF configuration with `session_id`, foreign key, and index.
- [x] 1.5 Update `backend/database/init.sql` to create `user_sessions` and add `refresh_tokens.session_id`.

## 2. Application Contracts

- [x] 2.1 Update JWT token contract so access-token generation receives a session id.
- [x] 2.2 Add session validation service contract for checking whether a user/session pair is active.
- [x] 2.3 Add session-related auth error codes for invalid or revoked sessions.

## 3. Authentication Flow

- [x] 3.1 Update login flow to create an active session before creating access and refresh tokens.
- [x] 3.2 Update access-token generation to include the `sid` claim.
- [x] 3.3 Update refresh flow to require a refresh token linked to an active session.
- [x] 3.4 Update refresh rotation to preserve the same session id on replacement refresh tokens.
- [x] 3.5 Update logout flow to revoke both the submitted refresh token and its related session.

## 4. Request-Time Session Validation

- [x] 4.1 Implement session validation service in `Demo.Infrastructure`.
- [x] 4.2 Register JWT bearer `OnTokenValidated` to read `userId` and `sid`.
- [x] 4.3 Reject authenticated requests when `sid` is missing, invalid, expired, revoked, or belongs to another user.
- [x] 4.4 Ensure session validation happens before permission authorization succeeds.

## 5. Demo and Documentation

- [x] 5.1 Update Swagger/OpenAPI notes so users can verify access token invalidation after logout.
- [x] 5.2 Update `Demo.Api.http` with a logout-then-me check using the old access token.
- [x] 5.3 Update demo database setup notes to recreate or migrate existing demo data for session support.

## 6. Verification

- [x] 6.1 Run `dotnet build backend/Demo.sln` and fix compile errors.
- [x] 6.2 Verify login creates one active session and refresh token with matching `session_id`.
- [x] 6.3 Verify access token contains `sid` but not roles or permissions.
- [x] 6.4 Verify `/api/auth/me` succeeds before logout and returns unauthenticated after logout with the same access token.
- [x] 6.5 Verify refresh fails after the related session is revoked.
- [x] 6.6 Verify permission-protected endpoints reject revoked sessions before permission checks.
