## Context

The admin agent catalog already separates internal agents from tenant-scoped agents and loads those lists through two backend endpoints. The next usability gap is list navigation: once the catalog contains many agents, admins need quick controls to narrow results by agent status and keyword without losing the existing scope separation between internal and tenant lists.

The current frontend page already owns the internal and tenant list state, and the backend already exposes queryable EF Core lists for both scopes. That makes this change a good fit for additive list filtering rather than a large structural rewrite.

## Goals / Non-Goals

**Goals:**

- Add status filtering for internal-agent and tenant-agent lists.
- Add keyword search for internal-agent and tenant-agent lists.
- Keep internal and tenant scopes separate while applying the same filter model to both.
- Implement filtering through backend query parameters so the frontend does not have to fetch every row and filter locally.
- Keep the UX simple with one search input and one status selector per active list view.

**Non-Goals:**

- Full-text search, fuzzy ranking, highlighted matches, or search suggestions.
- Combining multiple advanced filter dimensions such as role, tenant, created date, or owner.
- Pagination, saved filters, or URL-persisted filter state.
- Database schema changes or search indexing.

## Decisions

1. **Filter on the backend through optional query parameters.**

   Both `GET /api/admin/agents/internal` and `GET /api/tenants/{tenantId}/agents` will accept optional query parameters such as `status` and `search`. The EF Core query will add `Where` clauses only when the inputs are present.

   Alternative considered: fetch the full list once and filter entirely on the client. That is rejected because it couples the frontend to the full dataset size and makes future pagination or server-side authorization trimming harder.

2. **Use substring keyword matching across a small set of existing fields.**

   Keyword search will match against fields the catalog already displays or depends on, such as `name`, `description`, and `role`. Matching stays case-insensitive within the capabilities of the current MySQL collation and EF translation.

   Alternative considered: search only by name. That is rejected because admins often remember an agent’s role or description rather than the exact display name.

3. **Use the existing status enum values as the filter contract.**

   The frontend status dropdown will use the current agent status values already returned by the API, plus an “all” option that omits the status parameter entirely.

   Alternative considered: add a separate display-only status taxonomy for filtering. That is rejected because it would duplicate the domain enum and create mapping drift for little gain.

4. **Keep the filter state per active scope in the page-level frontend state.**

   The frontend page will maintain the active search text and selected status for the current view, and reuse those values when reloading the relevant list. Tenant changes will keep the filter controls available while swapping the tenant list data source.

   Alternative considered: split filters into a separate shared store immediately. That is deferred because the current behavior is local to the agent catalog page and does not yet justify cross-page state.

5. **Return normal filtered list responses rather than a special search payload.**

   The API response shape stays the same as the current agent card payload; only the result set changes based on filters. This keeps the frontend card rendering unchanged apart from the new controls and empty states.

   Alternative considered: return extra search metadata such as total match count or applied filters. That is deferred to keep the contract small and avoid adding UI complexity before pagination exists.

## Risks / Trade-offs

- **Rapid typing can trigger many requests** -> Keep the initial implementation simple, and add a small debounce on the frontend if request chatter becomes noisy.
- **Case-insensitive matching can depend on DB collation** -> Use conservative `Contains` filtering on existing text fields and validate behavior against the local MySQL setup.
- **Empty results may feel like an error** -> Add explicit empty states that mention active filters rather than showing a generic blank panel.
- **Status parsing can fail on invalid query values** -> Treat invalid status values as validation errors or ignore them consistently in the controller layer.
- **Different scopes may need different filter defaults later** -> Keep filter state page-local so scope-specific adjustments remain easy to add.

## Migration Plan

1. Extend the internal-agent and tenant-agent list endpoints with optional `status` and `search` query handling.
2. Update EF Core list queries to apply optional filters while preserving scope rules.
3. Add frontend search and status controls to the agent catalog page.
4. Reload internal and tenant lists based on the active filter values and selected tenant.
5. Add or update empty states to explain when no agents match the current filters.
6. Verify filtered responses for internal scope, tenant scope, and invalid/empty filter combinations.

Rollback is to remove the optional query filtering logic and revert the page controls, restoring the unfiltered list experience without any data migration concerns.

## Open Questions

- Should filter input be applied immediately while typing, or only after a short debounce / explicit submit action?
- Should the tenant view remember its last filter values separately from the internal view, or should the page share one filter state across both scopes for now?
