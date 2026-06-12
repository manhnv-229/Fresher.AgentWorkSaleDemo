## Why

The AI Agents management system needs a production-like authentication and authorization foundation before tenant, agent, user, role, and permission management can be safely exposed. This change establishes multi-tenant login, JWT session handling, refresh token rotation, and dynamic permission checks so API access can be controlled from database state instead of hard-coded controller roles.

## What Changes

- Add email/password login with BCrypt password verification.
- Issue short-lived JWT access tokens containing only user identity claims.
- Issue refresh tokens, store only SHA-256 token hashes, and support refresh token rotation.
- Revoke refresh tokens during logout and reject expired or revoked tokens.
- Add `/api/auth/login`, `/api/auth/refresh-token`, `/api/auth/logout`, and `/api/auth/me`.
- Add database-backed RBAC for users, tenants, roles, permissions, role permissions, user roles, and user-tenant membership.
- Add tenant-aware dynamic authorization using permission codes such as `tenant.view`, `agent.create`, and `role.assign`.
- Add a `[HasPermission]` authorization model so controllers depend on permissions rather than role names.
- Add demo permission-protected tenant and agent endpoints for validating the authorization flow.
- Seed default permissions, global `SystemAdmin`, and tenant roles such as `TenantAdmin`, `AgentManager`, and `AgentViewer`.

## Capabilities

### New Capabilities

- `auth-access-control`: Authentication, refresh token rotation, dynamic permission authorization, and multi-tenant RBAC behavior for API access.

### Modified Capabilities

- None.

## Impact

- Affected projects: `backend/Demo.Api`, `backend/Demo.Application`, `backend/Demo.Domain`, and `backend/Demo.Infrastructure`.
- Affected API surface: new auth endpoints and permission-protected demo tenant/agent endpoints.
- Affected persistence: MySQL tables for users, tenants, user-tenants, roles, permissions, role-permissions, user-roles, and refresh tokens, aligned with the attached SQL schema.
- New dependencies expected: ASP.NET Core JWT bearer authentication, EF Core MySQL provider if not already present, BCrypt password hashing, and cryptographic helpers for refresh token hashing/generation.
- Security impact: access tokens remain short-lived and do not embed permissions; permission decisions are evaluated against database state per request.
