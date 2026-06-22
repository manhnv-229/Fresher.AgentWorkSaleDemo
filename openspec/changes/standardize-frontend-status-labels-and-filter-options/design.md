## Context

The frontend currently mixes backend-facing status codes with user-facing labels. In multiple places, pages render raw values such as `Draft`, `Active`, `Inactive`, or `Locked`, and several views define their own option arrays inline for status filters or edit forms. The current scan found repeated hardcoded status options in `AgentCatalogPage.vue`, `InternalAgentsPage.vue`, `TenantAgentsPage.vue`, `AgentDetailPage.vue`, and `SettingsMembersPage.vue`.

This is mostly a frontend consistency and maintainability problem, but it crosses multiple screens and flows. We want one place that knows which backend status code maps to which Vietnamese label, and one place that can produce reusable option lists for filters or forms without changing the backend API contract.

## Goals / Non-Goals

**Goals:**
- Standardize frontend display labels for known status values into Vietnamese.
- Remove duplicated hardcoded status-option arrays from agent list pages.
- Reuse the same status mapping/options in related screens such as member management and agent detail/edit flows where status is shown or selected.
- Identify and migrate remaining hardcoded status/filter option definitions in the current relevant frontend scope.

**Non-Goals:**
- Changing backend enum/string values returned by APIs.
- Introducing a full i18n framework in this change.
- Refactoring unrelated option lists such as avatar choices or job-position lists unless they are directly part of the status/filter hardcoding problem.

## Decisions

1. Keep backend status codes in English and translate only at the frontend presentation layer.

   The API contract already uses status values like `Draft`, `Active`, `Inactive`, and `Locked`. The frontend should continue sending and receiving those codes for filtering and mutations, while using a shared mapping to render Vietnamese labels.

   Why this approach:
   - It avoids backend changes and keeps existing query/mutation contracts stable.
   - It isolates the localization concern to the UI layer.
   - It supports filter controls that submit the correct backend values while displaying Vietnamese text.

   Alternative considered: change backend status values to Vietnamese. This is rejected because it would broaden scope unnecessarily and risk breaking API consumers.

2. Create one shared frontend status dictionary plus helper option builders.

   A shared module should define status metadata for each relevant domain status and expose helpers for list-filter options, edit-form options, and label lookup. Pages can then consume only the subset they need.

   Why this approach:
   - It removes duplicated page-local arrays.
   - It makes the UI consistent across agent and member screens.
   - It provides a clear place to add future statuses or rename labels.

   Alternative considered: a composable per page family. This is rejected because the core problem is a shared dictionary, not reactive per-page state.

3. Scope the hardcode cleanup to status/filter option sources and direct raw status rendering in the current frontend pages.

   The user specifically called out status filters and hardcoded filter options in list-agent screens, plus any remaining similar hardcoding. This change should focus on that concern and migrate the currently discovered relevant pages rather than attempting a repo-wide generic constant cleanup.

   Why this approach:
   - It keeps the change practical and testable.
   - It targets the duplicated patterns that most directly affect the user-visible filtering experience.
   - It avoids expanding into unrelated constants that do not affect status/filter consistency.

   Alternative considered: audit and centralize every hardcoded option array in the frontend. This is rejected because it is much broader than the requested problem.

## Risks / Trade-offs

- [Different pages may need slightly different option sets such as “all” filters vs edit-form values] -> Build helpers that can derive filtered subsets from one shared dictionary instead of reintroducing page-local arrays.
- [A partial migration could leave some raw English labels visible] -> Audit the current targeted pages and update both filter controls and status-chip display paths together.
- [Future new statuses may appear without a Vietnamese label] -> Provide one central lookup path so missing mappings are easy to detect and add.
- [Some pages may currently compare against raw strings for styling logic] -> Keep backend codes unchanged and let style logic continue using codes while labels come from the shared mapping.
