## Why

The admin agent workspace already supports loading agent details and saving updates, but those actions still live inside an in-page modal flow that feels cramped and hides quick actions from the list itself. We need a clearer management experience now so users can open a dedicated agent page with preloaded data, edit and save confidently, and trigger common actions directly from each agent card.

## What Changes

- Change the agent detail/edit experience so clicking an agent opens a dedicated page or route-level view instead of an overlay modal.
- Load the selected agent's current data automatically on that detail page so users can view, edit, and save changes without re-entering context.
- Add a top-right action menu on each agent card with options to view details, edit, and delete.
- Keep internal-agent and tenant-agent scope behavior intact so detail, edit, and delete actions still respect the active scope and tenant context.
- Preserve list refresh and selected tenant context after save or delete when users return from the detail page or act from the card menu.

## Capabilities

### New Capabilities

- None.

### Modified Capabilities

- `admin-agent-catalog`: The admin agent catalog will support page-style agent detail/edit navigation and per-card action menus for view, edit, and delete.

## Impact

- Affected frontend area: `frontend` agent catalog view, routing or page-state handling, and agent card interactions.
- Affected API usage: existing internal-agent and tenant-agent detail/update/delete endpoints will be reused through a dedicated detail page flow.
- Backend impact: no new resource model is required, but existing detail/update/delete contracts become a direct dependency of the new navigation UX.
- Authorization impact: card-level actions and detail-page saves must continue respecting internal vs tenant scope and existing permission checks.
