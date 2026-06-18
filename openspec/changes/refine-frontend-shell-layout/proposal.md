## Why

The current frontend works functionally, but the visual structure still feels uneven: the login screen sits too high, while the workspace sidebars behave like floating rounded cards instead of stable full-height navigation columns. We need a cleaner shell now so the app feels intentional and easier to scan, especially as the routed workspace grows across agent and settings pages.

## What Changes

- Recenter the login experience so the login UI sits naturally within the viewport instead of hugging the top.
- Restyle the primary sidebar and settings sidebar into straighter, full-height navigation panels that visually anchor the workspace.
- Add a simple shared header to the authenticated workspace content area.
- Preserve the current routed agent and settings flows while updating layout, spacing, and visual hierarchy around them.
- Keep the design system consistent across login, workspace shell, and settings pages after the layout change.

## Capabilities

### New Capabilities

- None.

### Modified Capabilities

- `vue-login-ui`: The login screen layout will be recentered and refined so the initial auth experience is visually balanced.
- `settings-sidebar-navigation`: The settings sidebar will adopt a full-height, square-edged workspace style instead of a floating card treatment.
- `admin-agent-catalog`: The authenticated agent workspace will gain a simple shared header and a more structural sidebar layout.

## Impact

- Affected frontend area: `frontend/src/views`, `frontend/src/layouts`, and shared stylesheet assets.
- Affected UX: login, workspace navigation, and settings navigation will look more stable and page-like without changing backend behavior.
- Backend impact: none expected.
- Design impact: sidebar geometry, login alignment, and shell hierarchy will change while preserving existing page routes and API flows.
