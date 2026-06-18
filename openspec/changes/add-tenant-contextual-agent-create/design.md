## Context

The admin agent catalog already has separate internal and tenant workspaces, plus a tenant-scoped create API at `POST /api/tenants/{tenantId}/agents`. The remaining usability gap is that the create entry point in the tenant view is not yet treated as a first-class tenant-context action, which can make scope feel indirect and increases the risk of admins wondering whether they are creating an internal agent or a tenant agent. Because the backend contract already supports tenant-bound creation, this change should primarily standardize frontend state, action placement, and create-flow behavior around the currently selected tenant.

## Goals / Non-Goals

**Goals:**

- Show a clear create-agent action while the user is viewing a selected tenant's agent list.
- Ensure the tenant create modal submits against the selected tenant automatically without requiring the user to reselect scope.
- Preserve the separate internal-agent create flow and keep it free from tenant context.
- Refresh the currently selected tenant list after creation so the new tenant agent appears immediately.

**Non-Goals:**

- Adding new backend agent-create endpoints or changing the tenant agent domain model.
- Allowing cross-tenant reassignment during creation or editing.
- Redesigning the entire agent catalog layout beyond what is needed for context-aware creation.
- Changing permission semantics beyond continuing to rely on the existing `agent.create` authorization checks.

## Decisions

1. **Treat selected tenant state as the source of truth for tenant create actions.**

   When the tenant workspace is active and a tenant is selected, the create button will open a tenant-scoped create flow bound to that tenant id. The modal does not need a separate tenant picker because the sidebar selection already defines the intended target.

   Alternative considered: include a tenant dropdown inside the create modal. That is rejected because it duplicates the sidebar context and increases the chance of mismatched UI state.

2. **Keep separate create entry points for internal and tenant scopes.**

   The internal workspace will continue opening the internal-agent create flow, while the tenant workspace exposes a create action labeled and scoped for the selected tenant. This keeps scope visible before the user starts filling the form.

   Alternative considered: use one generic create button for all workspaces. That is rejected because it hides scope at the moment where the user most needs confidence about where the new agent will live.

3. **Reuse the existing tenant create API instead of adding a new backend orchestration contract.**

   The frontend will continue calling `POST /api/tenants/{tenantId}/agents` for tenant-scoped creation and `POST /api/admin/agents/internal` for internal creation. The change is in how the UI selects which endpoint to call, not in how the backend stores agents.

   Alternative considered: add a single unified create endpoint that infers scope from payload. That is rejected because the route-level distinction is already explicit and aligns with existing authorization and tenant-boundary rules.

4. **Refresh the active tenant result set in place after a successful create.**

   After creating a tenant agent, the UI should close the modal, keep the same tenant selected, and reload that tenant's current filtered/paged list so the created agent appears without navigating away.

   Alternative considered: redirect users to a neutral success state or detail screen. That is deferred because the current workflow is list-centric and benefits most from staying in the selected tenant context.

## Risks / Trade-offs

- **Create action may open without a tenant actually being selected** -> Disable or hide the tenant create action until a tenant is active.
- **Modal state could retain the previous tenant after sidebar changes** -> Reset modal-scoped tenant state from the current selection each time the modal opens.
- **Refreshing the list after create could reset user filters or page** -> Reuse the existing tenant list refresh path that preserves active filters and pagination state.
- **Internal and tenant create labels may still feel ambiguous** -> Use explicit tenant-aware button text and modal titles in the tenant workspace.

## Migration Plan

1. Update tenant-workspace actions in the frontend to expose a selected-tenant create button.
2. Wire modal-open logic so tenant create state is derived from the current sidebar selection.
3. Reuse the existing tenant create API client and tenant list refresh flow after successful creation.
4. Verify internal and tenant create flows remain separated and respect selected tenant context.

Rollback is straightforward because the backend create APIs remain unchanged. Reverting the frontend state and action placement returns the workspace to its previous create behavior without requiring data migration.

## Open Questions

- Should the tenant create button include the tenant name directly in its label, or is using the modal title enough to communicate scope?
- After creating a tenant agent, should the UI stay on the list or automatically open the newly created agent detail view?
