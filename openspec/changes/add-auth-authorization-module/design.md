## Context

The backend is an ASP.NET Core Web API solution already split into `Demo.Api`, `Demo.Application`, `Demo.Domain`, and `Demo.Infrastructure`. `Demo.Api` is currently close to the default minimal scaffold, while `Demo.Infrastructure` already references the Pomelo EF Core MySQL provider. The new authentication and authorization module must fit this Clean Architecture shape and support a multi-tenant AI Agents management system.

The attached SQL schema defines the target persistence model for users, tenants, user membership, roles, permissions, role permissions, user roles, refresh tokens, and demo agent tables. The authorization model must be dynamic: controllers protect actions by permission code, and runtime checks read current role/permission assignments from the database.

## Goals / Non-Goals

**Goals:**

- Implement authentication and authorization across the existing Clean Architecture projects.
- Keep domain entities and enums in `Demo.Domain`.
- Keep application DTOs and service contracts in `Demo.Application`.
- Keep EF Core, token generation, password hashing, permission lookup, persistence seeding, and other infrastructure services in `Demo.Infrastructure`.
- Keep HTTP controllers, attributes, middleware setup, JWT bearer configuration, and authorization policy registration in `Demo.Api`.
- Use short-lived JWT access tokens for identity only and refresh token rotation for session continuation.
- Use tenant-aware permission checks that account for global roles and tenant-specific roles.
- Provide demo endpoints to validate permission behavior before full tenant/agent management is built.

**Non-Goals:**

- Full CRUD management APIs for users, roles, permissions, tenants, and agents beyond the demo endpoints listed in the proposal.
- External identity providers, social login, MFA, password reset, account invitation email, or audit dashboards.
- Permission caching or distributed cache invalidation.
- Refresh token reuse incident response beyond detecting that a revoked token cannot be reused.

## Decisions

1. **Use Clean Architecture boundaries already present in the solution.**

   Domain will contain persistence-agnostic entities such as `User`, `Tenant`, `Role`, `Permission`, `RefreshToken`, and join entities. Application will define DTOs and interfaces such as `IAuthService`, `IJwtTokenService`, `IPasswordHasher`, `IPermissionService`, and request/response models. Infrastructure will implement those interfaces with EF Core and cryptographic services. Api will expose controllers and configure ASP.NET Core authentication/authorization.

   Alternative considered: place all logic in `Demo.Api` for speed. That is rejected because auth, token rotation, and RBAC rules are cross-cutting and need stable contracts for later tenant/agent management.

2. **JWT access tokens contain identity claims only.**

   Access tokens will include `sub`, `userId`, `email`, `name`, `jti`, and expiration. Permissions and roles will not be embedded.

   Alternative considered: include permission codes in JWTs to avoid database lookups. That is rejected because permission changes in the database would not take effect until token expiry.

3. **Refresh tokens are random secrets stored only as SHA-256 hashes.**

   The raw refresh token is returned to the client once. The database stores `token_hash`, expiry, revocation metadata, and `replaced_by_token_hash`. Refreshing a token revokes the old row and creates a new row.

   Alternative considered: store raw refresh tokens for simpler lookup/debugging. That is rejected because leaked database rows would become active credentials.

4. **Use BCrypt for user password hashes.**

   The password hasher contract will allow BCrypt verification and future replacement if needed.

   Alternative considered: use framework password hashing from ASP.NET Identity. That is deferred because the requested model uses custom tables and dynamic permissions rather than the full Identity schema.

5. **Use policy-based authorization with a permission requirement.**

   `HasPermissionAttribute` will map permission codes to authorization policy names. A custom policy provider and authorization handler will evaluate the current user, resolve the tenant id from route values or header context, and call `IPermissionService`.

   Alternative considered: use `[Authorize(Roles = "...")]`. That is rejected because role names would be hard-coded in controllers and could not support dynamic role composition.

6. **Treat global roles separately from tenant roles.**

   A user assigned a global role such as `SystemAdmin` via a role whose `tenant_id` is null can satisfy permissions for global APIs and tenant APIs. Tenant-specific roles require an active `user_tenants` membership and a matching tenant role assignment.

   Alternative considered: require every permission check to have a tenant id. That would make global tenant creation/listing awkward and would not represent `SystemAdmin` cleanly.

7. **Start with direct database permission checks.**

   Permission lookups will query EF Core directly and can be optimized later with cache keys like `permissions:{userId}:{tenantId}`.

   Alternative considered: add caching immediately. That is deferred to keep the demo reliable and avoid cache invalidation complexity before role/permission management APIs exist.

## Risks / Trade-offs

- **Database permission check on every protected request** -> Start simple with indexed joins; add cache after role and permission mutation APIs exist.
- **Permission changes are not reflected in already issued access tokens for authentication status** -> Keep token lifetime short at 15 minutes and check authorization against database on each request.
- **Refresh token theft** -> Store only token hashes, rotate tokens on refresh, revoke on logout, and reject revoked tokens.
- **Tenant id extraction can vary by endpoint** -> Support route value `tenantId` first and allow a header fallback only where an endpoint has no tenant route segment.
- **Custom auth instead of ASP.NET Identity increases implementation responsibility** -> Keep explicit tests around login, password verification, token rotation, logout, and authorization decisions.
- **Seeded demo users require known credentials** -> Store only BCrypt hashes in seed data and document credentials outside of source-sensitive contexts if needed.

## Migration Plan

1. Add domain entities, EF Core configuration, and a `DemoDbContext` that matches the target MySQL schema.
2. Add migrations or a SQL initialization script for auth and demo tables, including indexes and uniqueness constraints.
3. Seed permission codes and default roles/role-permission mappings.
4. Seed demo tenants, users, memberships, user roles, and BCrypt password hashes for local validation.
5. Add authentication and authorization services behind application interfaces.
6. Configure JWT bearer authentication, custom permission authorization, and controllers in `Demo.Api`.
7. Validate login, refresh, logout, `/api/auth/me`, and permission-protected demo endpoints.

Rollback is to remove the new migrations or drop the added tables in a demo environment. In production-like data, rollback must preserve users and refresh-token revocation history or export them before reverting.

## Open Questions

- Should refresh tokens be accepted only from the same IP address that created them, or should IP be informational for now?
- Should global `SystemAdmin` bypass tenant membership checks for tenant-scoped APIs, or require explicit membership for tenant data access despite having global permissions?
- Should the demo expose Swagger/OpenAPI auth helpers as part of this change, or keep the API surface focused on runtime behavior?
