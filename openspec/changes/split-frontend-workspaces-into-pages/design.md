## Context

The current frontend admin experience concentrates almost every authenticated workflow inside `frontend/src/views/AgentCatalogPage.vue`. That file currently mixes authentication bootstrapping, primary sidebar behavior, internal-agent list rendering, tenant-agent list rendering, agent detail/edit/delete state, tenant detail state, member management, password change, create flows, filters, pagination, and ad hoc query-string restoration. The router exists only as a stub, which means newer requirements such as dedicated agent detail pages and settings subsections have to be modeled as local state inside one component instead of as first-class navigation targets.

This change is a frontend architecture change rather than a backend feature change. The goal is to reshape the existing UI into page-sized units that can reuse shared layout and state helpers without forcing every workflow through one component.

## Goals / Non-Goals

**Goals:**

- Split the monolithic authenticated frontend into dedicated page components with route-driven navigation.
- Introduce a reusable authenticated shell that owns the primary navigation while individual pages own their local concerns.
- Support internal-agent list, tenant-agent list, agent detail/edit, tenant-management views, and settings subsections as separate pages or nested route segments.
- Preserve tenant context, filters, pagination, and auth behavior while moving logic out of `AgentCatalogPage.vue`.
- Make future UI changes such as agent detail pages or extra settings sections cheaper to implement and reason about.

**Non-Goals:**

- Changing backend APIs, permission semantics, or domain models.
- Redesigning every visual style token in the frontend.
- Rebuilding all shared UI elements from scratch when an extraction into smaller components is enough.
- Introducing a global state library if composables and route state are sufficient.

## Decisions

1. **Create an authenticated workspace shell plus route-level child pages.**

   The frontend will introduce a shared authenticated layout component responsible for sidebar navigation, auth initialization checks, and common page framing. Individual pages such as internal agents, tenant agents, agent detail, members, and password settings will render as routed content inside that shell.

   Alternative considered: keep one top-level page and only split large template sections into child components. That is rejected because the user explicitly wants separate frontend pages, and component-only extraction would still leave navigation and state ownership centralized in one view.

2. **Use route structure to represent workspace intent rather than local mode flags.**

   Instead of toggling `activeWorkspace`, `viewMode`, and settings subsection refs inside one file, route paths will express major destinations such as internal catalog, tenant catalog, agent detail, tenant detail/management, settings members, and settings password. Local component state can still handle transient UI such as open menus or form dirtiness, but page identity will belong to the router.

   Alternative considered: preserve one route and encode everything in query params or local refs. That is rejected because it keeps navigation opaque and does not reduce conceptual complexity enough.

3. **Preserve shared browsing context through route params, query params, and focused composables.**

   Tenant id, search text, status filter, pagination, and “return to list” context should survive route changes by living in route state where useful and in extracted composables where repeated API-loading logic exists. This keeps deep links meaningful without pushing every transient detail into a single global store.

   Alternative considered: move all page state into one global singleton store immediately. That is deferred because it would increase upfront complexity and is not necessary for the current scope.

4. **Extract reusable feature modules around pages, not around every small widget first.**

   The first extraction layer should follow workflow boundaries such as `agents/internal-list`, `agents/tenant-list`, `agents/detail`, and `settings/members`, with supporting shared components/composables underneath. This avoids over-fragmenting the codebase before the page boundaries are stable.

   Alternative considered: start by splitting every template region into tiny presentational components only. That is rejected because it would create many files without addressing the routing and ownership problem that motivated the change.

5. **Keep existing APIs and auth flow intact while reshaping the frontend composition.**

   The new page structure will continue using the same agent, tenant, user, and password-change APIs. Auth initialization and refresh behavior should move into the shell or router guard layer so individual pages do not each reinvent authentication bootstrapping.

   Alternative considered: redesign auth and routing together in one large rewrite. That is rejected because it adds unnecessary risk to a primarily frontend-structure change.

## Risks / Trade-offs

- **Route migration could temporarily break current deep-link or back-button behavior** -> Define route names and return-navigation rules early, then verify browser back/forward flows during implementation.
- **State such as selected tenant or filters could be lost during page transitions** -> Encode persistent browsing context in route/query state and centralize restoration in focused composables.
- **Splitting the page may duplicate loading logic across new views** -> Extract shared data loaders and list-state helpers instead of copying API orchestration into each page.
- **The refactor touches many frontend files at once** -> Migrate incrementally through an authenticated shell and a small number of page extractions first, then move remaining settings and detail flows.
- **A half-finished split could leave both old and new navigation models active** -> Remove obsolete `AgentCatalogPage.vue` responsibilities as each routed page becomes authoritative.

## Migration Plan

1. Define route structure and an authenticated shell for the admin workspace.
2. Extract internal-agent and tenant-agent list views into dedicated pages under the new shell.
3. Move agent detail/edit into its own page flow using the routed structure.
4. Move settings subsections such as members and password into standalone pages or nested route segments.
5. Extract shared composables/components for tenant selection, filter state, list loading, and action handling.
6. Remove obsolete workspace-mode state and template branches from `AgentCatalogPage.vue`, or retire that file entirely once replacement pages are complete.
7. Verify auth initialization, list context restoration, route navigation, and CRUD flows across internal, tenant, and settings pages.

Rollback is to keep or temporarily restore the current single-page workspace while preserving any extracted shared components that are still useful. Because backend contracts remain unchanged, rollback is limited to frontend routing and composition.

## Open Questions

- Should tenant management itself become its own top-level workspace route now, or remain a modal/detail flow nested under the existing tenant browsing routes for the first pass?
- Do we want route paths that are fully human-readable for each settings and agent page immediately, or is named-route navigation with minimal path design enough for the first iteration?
