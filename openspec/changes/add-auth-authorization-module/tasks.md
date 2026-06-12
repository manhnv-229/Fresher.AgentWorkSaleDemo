## 1. Domain and Persistence Model

- [x] 1.1 Add domain entities and status/value enums for users, tenants, user-tenants, roles, permissions, role-permissions, user-roles, refresh tokens, agents, branch info, and agent instructions in `Demo.Domain`.
- [x] 1.2 Add EF Core `DemoDbContext` with `DbSet` properties and entity configurations in `Demo.Infrastructure`.
- [x] 1.3 Configure MySQL table names, keys, relationships, indexes, uniqueness constraints, nullable fields, and column lengths to match the target SQL schema.
- [x] 1.4 Add database initialization through EF migrations or a checked-in SQL script for the authentication, authorization, tenant, and demo agent tables.
- [x] 1.5 Add configuration binding for database connection settings in `Demo.Api`.

## 2. Application Contracts and DTOs

- [x] 2.1 Add auth request/response DTOs for login, refresh token, logout, token response, and current user profile in `Demo.Application`.
- [x] 2.2 Add application service interfaces for `IAuthService`, `IJwtTokenService`, `IPasswordHasher`, `IRefreshTokenHasher`, and `IPermissionService`.
- [x] 2.3 Add result models or exception conventions for unauthorized, forbidden, validation, and not-found auth outcomes.
- [x] 2.4 Add shared permission code constants only where they avoid string duplication outside controller attributes.

## 3. Authentication Services

- [x] 3.1 Implement BCrypt password hashing and verification in `Demo.Infrastructure`.
- [x] 3.2 Implement JWT access token generation with identity-only claims and configurable 15-minute expiration.
- [x] 3.3 Implement cryptographically random refresh token generation and SHA-256 refresh token hashing.
- [x] 3.4 Implement login flow that validates active users, verifies passwords, creates access tokens, stores refresh token hashes, and returns raw refresh tokens once.
- [x] 3.5 Implement refresh flow that validates token hash, expiry, revocation state, revokes the old token, stores the replacement token hash, and returns rotated tokens.
- [x] 3.6 Implement logout flow that revokes a submitted refresh token without exposing whether unknown tokens exist.
- [x] 3.7 Implement current-user lookup for `/api/auth/me`.

## 4. Dynamic Permission Authorization

- [x] 4.1 Implement `HasPermissionAttribute`, permission requirement, dynamic policy provider, and authorization handler in `Demo.Api`.
- [x] 4.2 Implement tenant id resolution from route value `tenantId` with a safe header fallback when required.
- [x] 4.3 Implement `IPermissionService` queries for global roles, tenant roles, active user-tenant membership, role-permission mappings, and permission codes.
- [x] 4.4 Register authentication, JWT bearer validation, authorization handlers, policy provider, and current-user claim parsing in the API startup.
- [x] 4.5 Ensure authorization returns unauthenticated for missing/invalid access tokens and forbidden for authenticated users without required permissions.

## 5. API Endpoints

- [x] 5.1 Replace or extend the default API scaffold with controllers and controller routing.
- [x] 5.2 Add `POST /api/auth/login`, `POST /api/auth/refresh-token`, `POST /api/auth/logout`, and `GET /api/auth/me`.
- [x] 5.3 Add permission-protected demo tenant endpoints `GET /api/tenants` and `POST /api/tenants`.
- [x] 5.4 Add permission-protected demo agent endpoints `GET /api/tenants/{tenantId}/agents` and `POST /api/tenants/{tenantId}/agents`.
- [x] 5.5 Add request/response validation and appropriate HTTP status mapping for auth and permission failures.

## 6. Seed Data

- [x] 6.1 Seed default permission codes for tenant, agent, user, and role groups.
- [x] 6.2 Seed global `SystemAdmin` and tenant roles `TenantAdmin`, `AgentManager`, and `AgentViewer`.
- [x] 6.3 Seed role-permission mappings matching the requested default role definitions.
- [x] 6.4 Seed demo tenants, active user-tenants, user-role assignments, and demo users with BCrypt password hashes.
- [x] 6.5 Document local demo credentials in a development-safe location if credentials are needed for manual API testing.

## 7. Verification

- [x] 7.1 Add automated tests or focused integration checks for successful login, invalid login, inactive-user login rejection, and `/api/auth/me`.
- [x] 7.2 Add verification for refresh token rotation, old token revocation, expired token rejection, and logout revocation.
- [x] 7.3 Add verification that JWT payloads do not contain role or permission lists.
- [x] 7.4 Add verification that `SystemAdmin` can access global tenant APIs.
- [x] 7.5 Add verification that tenant-scoped roles authorize only the assigned tenant and deny access in other tenants.
- [x] 7.6 Run `dotnet build` for the backend solution and record any remaining test or environment limitations.
