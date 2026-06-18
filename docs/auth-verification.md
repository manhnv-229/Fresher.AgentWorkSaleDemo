# Auth Verification

Verified on 2026-06-12:

- `dotnet restore backend/Demo.sln` completed successfully.
- `dotnet build backend/Demo.sln` completed successfully with 0 warnings and 0 errors.

Focused integration checks are listed in `backend/Demo.Api/Demo.Api.http`.

To run them locally:

1. For a fresh database, create the MySQL schema with `backend/database/init.sql`.
2. For an existing demo database created before DB-backed sessions, run `backend/database/add-db-backed-sessions.sql`.
3. Set `Database:SeedOnStartup` to `true` for one local run, then set it back to `false`.
4. Start `Demo.Api`.
5. Login with the demo accounts in `docs/demo-auth-credentials.md`.
6. Paste the returned access and refresh tokens into `Demo.Api.http`.

Manual checks to perform:

- Login returns access and refresh tokens.
- Invalid login returns unauthorized.
- `/api/auth/me` works with a valid token.
- Refresh returns a new access token and new refresh token.
- Reusing an old rotated refresh token returns unauthorized.
- Logout revokes the submitted refresh token and its database session.
- `/api/auth/me` returns unauthorized immediately after logout when called with the old access token.
- Refresh returns unauthorized after logout when called with the old refresh token.
- Decode the access token and confirm it contains `sid` plus identity claims only, not roles or permissions.
- `admin@example.com` can access tenant APIs.
- `manager@example.com` can create agents in Tenant One and is forbidden from creating agents in Tenant Two.

Limitations:

- Live HTTP checks were not executed in this session because no MySQL server was available in the workspace.
