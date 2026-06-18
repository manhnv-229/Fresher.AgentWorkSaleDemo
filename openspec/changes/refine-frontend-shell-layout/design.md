## Context

The frontend now has routed pages for internal agents, tenant agents, agent editing, and settings subsections, but the visual shell still reflects an earlier “floating card” phase. The login screen sits too close to the top because its container is centered horizontally but not vertically, and both the primary sidebar and settings sidebar are styled as rounded, detached cards rather than structural full-height navigation rails. As the workspace becomes more page-driven, the shell needs to look more architectural and less like stacked panels.

This change is a frontend layout refinement only. It does not change routes, APIs, or auth behavior, but it does change the way the login page and authenticated shell occupy the viewport.

## Goals / Non-Goals

**Goals:**

- Center the login screen more naturally within the viewport.
- Turn the primary sidebar and settings sidebar into straighter, full-height navigation columns.
- Add a simple shared header in the authenticated content area to anchor page titles and lightweight actions.
- Preserve existing route-based flows while improving spacing, geometry, and layout hierarchy.
- Keep the styling consistent across login, shell, and content pages.

**Non-Goals:**

- Changing backend authentication or route semantics.
- Introducing a full design-system rewrite or rebranding effort.
- Reworking every page’s internal form/card layout beyond what is needed to fit the new shell.
- Adding complex header behaviors such as search, notifications, or multi-row toolbars in this change.

## Decisions

1. **Use viewport-based centering for the login shell.**

   The login page container will use a full-height shell with centered alignment so the form sits comfortably within the viewport on desktop while still adapting on smaller screens. This keeps the auth entry point visually balanced without inventing a separate auth route structure.

   Alternative considered: keep the current top-aligned flow and only add more top padding. That is rejected because it remains brittle across screen sizes and still feels visually off-center.

2. **Treat sidebars as structural columns, not floating cards.**

   The primary sidebar and settings sidebar will become full-height panels aligned edge-to-edge with the workspace, using straighter corners and continuous vertical presence. This will make the layout feel more like an application shell and less like independent content cards.

   Alternative considered: keep rounded card sidebars and only widen them. That is rejected because the issue is not just width; it is that the sidebars do not visually read as navigation rails.

3. **Add a lightweight shared header above routed page content.**

   The authenticated content region will gain a simple header bar that can show the current workspace title and optional secondary context. This gives every page a stable top anchor without requiring each page to invent its own outer framing.

   Alternative considered: leave headers fully page-local. That is rejected because the user explicitly asked for a simple header and because a shell-level header improves consistency.

4. **Preserve current route/page composition while refining only shell-level layout responsibilities.**

   The new shell will not collapse the recently separated pages back into one file. Instead, the layout refinement will sit above the routed page structure, letting each page keep its local content while inheriting improved framing.

   Alternative considered: combine layout refinement with another large routing refactor. That is rejected because it would add risk without helping the immediate visual problem.

## Risks / Trade-offs

- **Full-height sidebars could feel too heavy on smaller screens** -> Add responsive fallbacks so the columns can stack or compress below desktop widths.
- **A shared header may duplicate page titles if pages keep large local headers** -> Define a simple division of responsibility between shell header and page-level section headers during implementation.
- **Squaring the sidebars too aggressively could clash with existing rounded controls** -> Keep inner controls and cards consistent while changing only outer shell geometry.
- **Login centering could reduce perceived space on shorter viewports** -> Use responsive spacing so the auth form can still scroll naturally on mobile.

## Migration Plan

1. Update shared layout styles for `.app-shell`, `.auth-shell`, `.workspace`, `.workspace__sidebar`, and `.workspace__settings-sidebar`.
2. Add a simple shared header region to `WorkspaceShell.vue`.
3. Adjust login page spacing and container behavior so the auth form centers vertically within the viewport.
4. Tune page padding and responsive rules so routed pages sit correctly within the refined shell.
5. Verify desktop and mobile behavior for login, internal agents, tenant agents, and settings pages.

Rollback is straightforward because the change is mostly in shared frontend layout files and stylesheet rules. Reverting those files restores the current visual shell without affecting data or routing.

## Open Questions

- Should the shared header show only the current page title, or should it also expose a tiny breadcrumb/subtitle for tenant context?
- On tablet/mobile, should the settings sidebar remain a vertical column or collapse into a horizontal section nav under the main header?
