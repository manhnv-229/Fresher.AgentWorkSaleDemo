## Context

The current admin agent catalog already has the backend pieces for detail, update, and delete, and the frontend can load those actions inside a modal-like state within `AgentCatalogPage.vue`. The remaining issue is usability: agent cards do not expose obvious quick actions, and opening details inside the same page makes the edit flow feel temporary instead of like a dedicated management surface. Because the data APIs already exist, this change should focus on page navigation, card-level actions, and preserving scope-aware list context when moving between list and detail screens.

## Goals / Non-Goals

**Goals:**

- Open an agent detail screen as a dedicated page or route-level view when a user clicks an agent card.
- Preload the selected agent's current data on that page so the user can review, edit, and save from one place.
- Add a top-right action menu on each agent card for `view details`, `edit`, and `delete`.
- Preserve internal and tenant scope handling so the right detail/update/delete APIs are still used.
- Return users to a consistent list state after save or delete with tenant selection and filters preserved.

**Non-Goals:**

- Adding new backend CRUD capabilities or changing the agent domain model.
- Changing how permissions are evaluated for internal or tenant-scoped agent actions.
- Introducing bulk card actions or multi-select list management.
- Redesigning unrelated sections of the admin dashboard beyond the agent browsing and detail flow.

## Decisions

1. **Promote agent detail/edit from modal state to a dedicated page-level view.**

   Clicking an agent card will navigate to a dedicated detail page or route-aware screen that loads the selected agent before rendering view/edit content. This gives more space for metadata and editing, and it makes the action feel like a primary workflow rather than a temporary overlay.

   Alternative considered: keep the existing modal and simply enlarge it. That is rejected because the user explicitly asked for opening a new page, and larger overlays still make navigation and save/delete state feel secondary.

2. **Treat the card click as the primary view-details action and add a menu for explicit actions.**

   The whole card remains clickable for the common “open details” path, while the top-right menu offers explicit `view`, `edit`, and `delete` actions for users who want direct intent. The menu should stop event propagation so action clicks do not trigger the default card navigation unintentionally.

   Alternative considered: replace card click entirely with buttons only. That is rejected because it slows down the most common action and makes the cards feel less direct.

3. **Reuse the existing scoped APIs and selected tenant context.**

   The detail page will still decide between internal and tenant endpoints based on route state and selected scope context. Tenant agent pages continue depending on the selected tenant or tenant identifier carried in navigation state so that updates and deletes remain tenant-safe.

   Alternative considered: add a new generic detail endpoint or new page bootstrap endpoint. That is rejected because the current internal and tenant route families already express ownership clearly.

4. **Preserve list context across navigation rather than rebuilding state from scratch.**

   When the user returns from the detail page or after a successful save/delete, the workspace should restore the relevant list context, including internal vs tenant scope, selected tenant, active filters, and pagination. This avoids the feeling of “losing your place” after managing an agent.

   Alternative considered: always return users to a default first page of the internal list. That is rejected because it would regress multi-tenant browsing workflows and force extra navigation.

## Risks / Trade-offs

- **Page navigation may lose selected tenant or filter state** -> Carry scope context in route/query state or preserve it in shared frontend state before navigating.
- **Card action menu clicks may accidentally open the card detail page too** -> Stop click propagation and isolate menu handlers from the card-click handler.
- **Delete from the card menu could feel abrupt** -> Keep the existing confirmation flow before performing delete.
- **New page layout may diverge from the existing edit/detail logic** -> Reuse the current detail-loading and save/delete service calls, changing only presentation and navigation structure.

## Migration Plan

1. Add page-level navigation or route-state support for agent detail screens in the frontend.
2. Move existing detail/edit rendering and save logic from modal mode into the dedicated detail screen.
3. Add per-card action menus wired to view, edit, and delete handlers with propagation-safe click behavior.
4. Preserve and restore list context after back navigation, save, and delete.
5. Verify internal-agent and tenant-agent flows still call the correct scoped APIs and refresh the correct list state.

Rollback is mainly frontend-only because the backend contracts already exist. Reverting the navigation and card-menu layer would return the workspace to the current modal-based detail flow without data migration.

## Open Questions

- Should the dedicated agent page use a separate frontend route path, or should it be a page-level state inside the same route with deep-link support deferred?
- When a user chooses `Edit` from the card menu, should the detail page open in read mode first or jump directly into edit mode?
