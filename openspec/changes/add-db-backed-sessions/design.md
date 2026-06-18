## Context

The current authentication module issues JWT access tokens with identity claims and a `jti`, stores refresh tokens as hashes, and revokes refresh tokens on logout. Because access tokens are stateless, logout does not immediately invalidate an already-issued access token; it remains usable until its 15-minute expiration.

The system already uses ASP.NET Core JWT bearer authentication, EF Core with MySQL, and Clean Architecture projects. This change adds a database-backed session layer beneath JWT validation so each access token can be tied to a persisted session and rejected as soon as that session is revoked.

## Goals / Non-Goals

**Goals:**

- Create a persisted `user_sessions` model for login sessions.
- Add a session id claim to access tokens.
- Validate the session id against the database on every authenticated request.
- Revoke the session during logout so access tokens from that session stop working immediately.
- Bind refresh tokens to sessions so refresh cannot continue after the session is revoked.
- Preserve existing refresh-token hashing and rotation behavior.

**Non-Goals:**

- Device management UI or a "log out all devices" API.
- Session analytics, location tracking, or suspicious activity detection.
- Distributed cache optimization for session validation.
- Changing the permission model or embedding permissions in JWTs.

## Decisions

1. **Persist sessions in a new `user_sessions` table.**

   A session row will contain `id`, `user_id`, `created_at`, `expires_at`, `revoked_at`, optional IP metadata, and `reason_revoked`. This keeps the session lifecycle separate from refresh-token rotation and makes access-token revocation explicit.

   Alternative considered: use only refresh-token rows as the session record. That is rejected because refresh rotation creates multiple refresh-token rows for one logical login session and makes access-token validity harder to reason about.

2. **Add a `sid` claim to access tokens.**

   The JWT will include the session id as `sid`. The token will still avoid role and permission lists.

   Alternative considered: reuse `jti` as the session id. That is rejected because `jti` identifies one access token, while `sid` identifies the login session across access-token refreshes.

3. **Validate sessions in JWT bearer `OnTokenValidated`.**

   After signature, issuer, audience, and lifetime validation succeeds, the API will read `userId` and `sid` claims and call a session validation service. If the session does not exist, is revoked, is expired, or belongs to another user, authentication fails.

   Alternative considered: check session only in authorization handlers. That is rejected because `/api/auth/me` and any future `[Authorize]` endpoint should be protected even if it does not use `[HasPermission]`.

4. **Keep refresh token rotation within the same session.**

   Login creates a session and the first refresh token. Refreshing validates both the refresh token and its session, revokes the old refresh token, creates a replacement refresh token, and issues a new access token with the same `sid`.

   Alternative considered: create a new session on every refresh. That is rejected because logout would only revoke the newest session and would blur the meaning of a login session.

5. **Logout revokes both refresh token and session.**

   Logout will continue to accept the refresh token. When the token is found, the system revokes that refresh token and the related session. Access tokens carrying that `sid` then fail on the next request.

   Alternative considered: logout by access token only. That is deferred because the existing API contract already accepts refresh token and Swagger users can test it directly.

## Risks / Trade-offs

- **Every authenticated request queries the database** -> Add indexes on `user_sessions.id`, `user_sessions.user_id`, and `refresh_tokens.session_id`; introduce caching later only if needed.
- **Session table grows over time** -> Keep expiration timestamps and plan a later cleanup job for expired sessions and old refresh tokens.
- **Existing refresh token rows have no session id** -> In demo, recreate DB or add nullable migration path; new rows must always have a session id.
- **DB outage makes authenticated APIs unavailable** -> This is expected for immediate logout semantics; fail closed when session validation cannot complete.
- **Logout requires refresh token** -> Keep current contract for now; later add logout-current-session by access token if desired.

## Migration Plan

1. Add `UserSession` domain entity and EF Core configuration.
2. Add `user_sessions` table to `backend/database/init.sql`.
3. Add nullable or required `session_id` column to `refresh_tokens` depending on migration strategy; for fresh demo DB it should be required.
4. Update login to create a session before issuing tokens.
5. Update JWT generation to accept session id and emit `sid`.
6. Update refresh flow to validate the session and keep the same session id.
7. Update logout to revoke the refresh token and session.
8. Register JWT bearer session validation in `Program.cs`.
9. Update Swagger/manual checks to demonstrate that `/api/auth/me` fails immediately after logout with the old access token.
