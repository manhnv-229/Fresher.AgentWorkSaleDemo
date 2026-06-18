## Why

The frontend admin experience has grown beyond what a single `AgentCatalogPage.vue` can hold comfortably: it now mixes internal and tenant agent lists, detail/edit flows, tenant actions, member management, password change, and workspace navigation in one large stateful component. We need to split these concerns into dedicated pages now so the UI becomes easier to extend, the routing model can support deeper workflows cleanly, and future changes no longer depend on one monolithic view.

## What Changes

- Split the current monolithic frontend workspace into dedicated pages instead of rendering all admin flows inside `AgentCatalogPage.vue`.
- Introduce route-driven navigation for major work areas such as internal agents, tenant agent lists, agent detail/edit, tenant management, and settings subsections.
- Move settings subsections like member management and password change into their own frontend pages while preserving the current sidebar navigation model.
- Preserve tenant-aware list, detail, create, update, and delete flows while moving them into separate views that can share layout and state helpers instead of one oversized page component.
- Reorganize frontend components and shared state so list context, tenant selection, and route transitions remain stable across the new page structure.

## Capabilities

### New Capabilities

- None.

### Modified Capabilities

- `admin-agent-catalog`: The admin agent catalog will be rendered through dedicated frontend pages and route-driven navigation instead of one combined page component.
- `settings-sidebar-navigation`: The settings workspace will move from shared in-page state to standalone frontend pages or route segments for each settings section.

## Impact

- Affected frontend area: `frontend/src/views`, `frontend/src/router`, shared composables/state, and agent/settings UI components.
- Affected API usage: existing agent, tenant, user-management, and password-change endpoints will be reused through separate pages rather than one combined view.
- Backend impact: no new backend capability is required, but frontend routing and page decomposition will rely on existing APIs staying stable.
- UX impact: users will navigate between explicit pages for lists, detail/edit, and settings areas instead of toggling many modes inside a single page.
