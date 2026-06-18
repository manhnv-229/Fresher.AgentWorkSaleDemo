## Context

The repo already has three important building blocks for this feature: JWT login on the backend, database-backed RBAC with a global `SystemAdmin` role, and tenant-scoped agent APIs under `GET/POST /api/tenants/{tenantId}/agents`. The frontend also already has login flow scaffolding and a post-login shell where an admin dashboard can be introduced.

The requested behavior adds a second agent scope besides tenant-owned agents: internal agents that only admin users can view and create. The design needs to fit the existing multi-tenant authorization model instead of bypassing it with frontend-only logic, and it needs to support a dashboard where the admin can switch between an internal tab and tenant-specific lists from the sidebar.

## Goals / Non-Goals

**Goals:**

- Add a first-class internal-agent scope that is separate from tenant-scoped agents.
- Keep internal-agent visibility and creation restricted to admin-authorized users.
- Preserve the existing tenant-scoped agent behavior while allowing the frontend to switch tenant context from a sidebar.
- Provide backend responses that let the frontend render the admin agent catalog using real API data rather than hard-coded mock cards.
- Reuse the current auth and permission model where possible so future agent CRUD can extend the same foundation.

**Non-Goals:**

- Full non-admin self-service agent management for every tenant role.
- Complex agent publishing workflows, approval states, versioning, or audit history.
- Real-time synchronization between tabs or live sidebar refresh.
- A broad redesign of login, routing, or the whole dashboard information architecture beyond what is needed for this agent catalog.

## Decisions

1. **Model agent scope explicitly in the domain and persistence layer.**

   Agents will gain an explicit scope marker such as `Internal` or `Tenant`. Tenant-scoped agents keep a required `TenantId`; internal agents allow `TenantId` to be null and are filtered by scope instead of overloading a fake tenant row.

   Alternative considered: store internal agents under a synthetic "internal" tenant. That is rejected because it mixes global admin-only data with business tenants and complicates permission rules, reporting, and future tenant lifecycle operations.

2. **Expose dedicated admin internal-agent endpoints instead of overloading tenant endpoints.**

   The backend will add endpoints such as `GET /api/admin/agents/internal` and `POST /api/admin/agents/internal` for internal agents, while keeping `GET /api/tenants/{tenantId}/agents` for tenant lists. This keeps route intent obvious and allows admin-only authorization rules without special casing a tenant id.

   Alternative considered: use a single `/api/agents?scope=internal|tenant&tenantId=...` endpoint. That is rejected for now because the current API shape is already tenant-route based, and separate endpoints reduce ambiguity in authorization and validation.

3. **Use permission codes plus global role checks for internal agents.**

   Internal-agent endpoints will require admin-authorized permissions that are only granted to global roles such as `SystemAdmin`. Tenant-scoped agent endpoints continue to use tenant-aware `agent.view` and `agent.create` checks.

   Alternative considered: hard-code email or role-name checks in the controller. That is rejected because the project already uses dynamic permission evaluation and should not introduce a parallel authorization path.

4. **Drive the frontend dashboard from composable API calls, not one large custom aggregate endpoint.**

   After login, the frontend can load tenants from `GET /api/tenants`, internal agents from the new admin endpoint, and tenant agents from the selected tenant endpoint. This keeps the API aligned with existing resources and avoids creating a dashboard-specific backend contract too early.

   Alternative considered: create a single `GET /api/admin/agent-catalog` bootstrap endpoint returning tenants and internal agents together. That is deferred because the current feature can be assembled cleanly from resource APIs and the aggregate shape may change as the UI evolves.

5. **Represent the admin workspace as a dedicated frontend view with two context layers: scope tab and selected tenant.**

   The UI will treat "Internal" and "Tenant" as top-level browsing contexts. The internal context shows admin-only cards plus an add-agent modal; the tenant context keeps the sidebar selection active and fetches agents for the chosen tenant.

   Alternative considered: make internal agents behave like another sidebar tenant item. That is rejected because internal agents are not business units, and separating them at the top level makes the permission and mental model clearer.

## Risks / Trade-offs

- **Schema change for nullable `TenantId` or new scope field** -> Add a targeted migration/update script and keep tenant-agent queries filtering explicitly by scope.
- **Frontend may need multiple requests on first load** -> Load tenants and internal agents in parallel and fetch tenant agents on demand when a tenant is selected.
- **Admin-only logic could drift between frontend and backend** -> Treat backend authorization as the source of truth and use frontend state only for presentation.
- **Existing seed/demo data may no longer cover both scopes** -> Extend seeding with at least one internal agent and tenant agents for each demo tenant.
- **Separate endpoints add surface area** -> Keep resource contracts small and consistent so later edit/delete flows can follow the same route families.

## Migration Plan

1. Extend the agent domain model, EF configuration, and SQL initialization or migration scripts with scope-aware fields and nullable tenant association for internal agents.
2. Seed demo internal agents plus existing tenant agents so the admin dashboard has realistic data immediately after login.
3. Add admin internal-agent list/create endpoints with permission protection for global admin users.
4. Keep tenant agent list/create endpoints working and ensure they filter to tenant-scope agents only.
5. Build a frontend admin agent catalog view that loads tenants, internal agents, and tenant agents based on the selected context.
6. Validate login-to-dashboard flow, admin-only internal agent creation, tenant switching, and unauthorized access behavior.

Rollback is to remove the new frontend route/view and revert the schema/API changes together. If data already exists, rollback must decide whether internal-agent rows are archived or deleted before restoring the old non-null tenant constraint.

## Open Questions

- Should internal-agent creation reuse the same card fields as tenant agents exactly, or should internal agents also carry extra metadata such as department/category from the start?
- Should non-admin tenant users eventually get a reduced version of the tenant agent list screen, or is this dashboard intentionally admin-only for the near term?
