## Context

The current authenticated workspace is centered on the agent catalog, with `Nội bộ` and tenant navigation in the main sidebar. The recently added password-change and member-management controls now appear inline near the top of the main content area, which mixes account settings with catalog browsing and makes the page feel heavier than the rest of the experience.

The requested change is not a new backend capability. It is a navigation and layout restructuring for the existing frontend experience: account-related tools should live under a dedicated `Thiết lập` area, and that area should expose a second level of navigation for settings subsections.

## Goals / Non-Goals

**Goals:**

- Add a dedicated `Thiết lập` entry to the primary sidebar.
- Show a secondary settings navigation when `Thiết lập` is active.
- Move `Quản lý thành viên` and `Đổi mật khẩu` into separate settings subsections rather than rendering them inline above the dashboard.
- Preserve the existing member-management and password-change behavior while improving information architecture.
- Keep the current agent list flows for internal and tenant scopes intact.

**Non-Goals:**

- Changing backend APIs, permissions, or account-security business rules.
- Introducing deeper settings categories beyond `Quản lý thành viên` and `Đổi mật khẩu`.
- Reworking the visual identity of the entire dashboard beyond what is needed to support nested navigation.
- Splitting the settings area into separate routes unless the existing page structure requires it during implementation.

## Decisions

1. **Treat `Thiết lập` as a top-level workspace mode, parallel to agent scopes.**

   The primary sidebar will let users switch between catalog-oriented modes and a settings-oriented mode. This keeps the first navigation layer responsible for major work areas rather than trying to overload the content pane with unrelated cards.

   Alternative considered: keep the current sidebar unchanged and add a button near the content header that opens settings. That is rejected because the user explicitly wants `Thiết lập` in the sidebar and because settings should be discoverable as a persistent workspace area.

2. **Use a second sidebar column for settings subsections.**

   When `Thiết lập` is active, the layout should reveal a secondary sidebar next to the primary sidebar. That secondary column will host subsection links such as `Quản lý thành viên` and `Đổi mật khẩu`, while the main content pane renders the selected settings section.

   Alternative considered: replace the main content with tabs or cards only. That is rejected because the user explicitly asked for a sidebar menu appearing beside the main sidebar, and a secondary column better supports future settings growth.

3. **Keep settings on the same page state model before introducing route-level nesting.**

   The initial implementation can use local view state such as a primary workspace mode plus a selected settings subsection. That matches the current single-view structure in `AgentCatalogPage.vue` and avoids unnecessary router churn for a localized layout change.

   Alternative considered: create dedicated routes such as `/settings/members` and `/settings/password`. That is deferred because the current app already concentrates admin flows in one page, and state-driven rendering is the smallest consistent change.

4. **Remove inline account cards from the catalog content area once settings navigation exists.**

   The main dashboard content should focus on agent browsing and creation. After this change, member management and password change should only appear inside the settings mode, not duplicated in both places.

   Alternative considered: keep duplicate shortcuts in the dashboard body. That is rejected because duplication would weaken the information-architecture improvement and keep the page visually crowded.

## Risks / Trade-offs

- **A second sidebar could feel heavy on smaller screens** -> Collapse naturally into stacked sections on tablet and mobile breakpoints while preserving clear labels and active states.
- **State logic may become more complex if agent scopes and settings share one page component** -> Keep the navigation model explicit with separate primary-mode and settings-subsection state instead of overloading the existing `activeScope`.
- **Users may temporarily lose visibility of member-management tools they were used to seeing inline** -> Make the new `Thiết lập` entry prominent and label its subsections clearly.
- **Keeping everything in one page can increase component size** -> Extract focused rendering helpers or child components during implementation if the page becomes too difficult to reason about.

## Migration Plan

1. Add a primary navigation state for `Thiết lập` alongside existing agent-oriented modes.
2. Add a secondary settings sidebar that appears only when the settings mode is active.
3. Move the current member-management panel into a `Quản lý thành viên` settings section.
4. Move the current password-change action into a dedicated `Đổi mật khẩu` settings section.
5. Update styling and responsive behavior for the new two-sidebar layout.
6. Verify agent catalog behavior still works for `Nội bộ` and tenant selection after the settings move.

Rollback is to restore the existing inline account-security and member-management sections in the main content area and remove the `Thiết lập` navigation mode.

## Open Questions

- Should `Thiết lập` be available only to users who can see member management, or should all authenticated users see it and only the `Quản lý thành viên` subsection become permission-aware?
- Should the settings secondary sidebar remain visible on mobile as a stacked panel, or should it collapse into buttons/dropdown behavior at smaller breakpoints?
