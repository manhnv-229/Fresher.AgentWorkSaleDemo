## Context

The current audit log page implementation has already moved toward backend-backed filtering, but it exposes several always-visible inputs for action, user, date range, IP address, and description. That satisfies the earlier filtering requirement functionally, yet it produces a dense toolbar that competes with the table content and does not match the lighter interaction pattern shown in the requested mockup.

This refinement still spans frontend and backend concerns. The frontend needs to collapse the visible controls into a single search bar plus a popover-style filter menu, while the backend query contract needs to evolve from many field-specific text filters into a more intentional model: one free-text search, one predefined time filter, and a multi-select action filter.

## Goals / Non-Goals

**Goals:**
- Replace multiple visible audit-log search fields with one unified search input.
- Add one filter-icon button that opens a context menu or popover for advanced filters.
- Support the requested time presets: `Hôm nay`, `Hôm qua`, `Tuần này`, `Tuần trước`, `Tháng này`, `Tháng trước`, `Năm nay`, and `Năm trước`.
- Support selecting multiple action types through checkboxes in the filter menu.
- Keep filtering server-driven and compatible with the existing read-only audit log page.

**Non-Goals:**
- Reintroducing separate inline inputs for every audit-log field.
- Building saved filter presets, query-string persistence, or export actions in this change.
- Redesigning the audit log table itself or changing audit entry retention/creation behavior.

## Decisions

1. Use a single free-text search input for general lookup.

   The page will expose one visible search field for quick lookup across operator-facing audit content, such as action labels, user names, IP addresses, and descriptions. This keeps common searches fast without forcing users to decide upfront which field they want to type into.

   Why this approach:
   - It matches the requested “gộp các trường search vào làm 1” behavior.
   - It reduces visual noise in the audit-log header area.
   - It supports quick investigation flows where users only remember one fragment of a log row.

   Alternative considered: keep dedicated inputs and hide some of them responsively. This is rejected because it preserves the same fragmented filtering mental model.

2. Move advanced filtering into one filter-menu interaction.

   A single icon-only or icon-led filter button will open a context menu or popover anchored near the search bar. That menu will contain the time preset selector and the multi-select action checklist, along with apply/reset controls.

   Why this approach:
   - It matches the requested interaction pattern and mockup closely.
   - It keeps advanced filters available without occupying permanent page space.
   - It allows time and action filters to be grouped into one focused review flow.

   Alternative considered: inline accordion filters under the search input. This is rejected because it still expands the page chrome and feels heavier than the requested context menu.

3. Represent time filtering as named presets instead of manual date inputs.

   The backend query model will accept a predefined time-range token corresponding to `Hôm nay`, `Hôm qua`, `Tuần này`, `Tuần trước`, `Tháng này`, `Tháng trước`, `Năm nay`, or `Năm trước`. The server or application layer will resolve the selected token into actual date boundaries consistently.

   Why this approach:
   - It matches the requested menu options exactly.
   - It simplifies the user experience compared with entering from/to dates manually.
   - It makes the UI faster for common operational review windows.

   Alternative considered: keep free-form start/end date inputs inside the menu. This is rejected because the request explicitly calls for canned relative periods rather than manual date entry.

4. Model action filtering as a multi-select list of known audit action types.

   The filter menu will provide checkbox selection for action categories or action codes, and the backend query will accept multiple selected values in one request. The menu should make it clear that more than one action type can be active at the same time.

   Why this approach:
   - It matches the requested checkbox interaction.
   - It supports real audit workflows where users want to review several related actions together.
   - It is more reliable than using free-text matching for action values.

   Alternative considered: use a plain text input for action names. This is rejected because the request explicitly prefers checkbox-driven selection and because action sets are finite enough to present as options.

## Risks / Trade-offs

- [A single search box may be less precise than field-specific inputs] -> Keep advanced filters for time and action, and define the free-text search scope clearly in the API contract.
- [Relative time presets can behave unexpectedly across time zones or week boundaries] -> Centralize preset-to-range resolution and test each preset boundary explicitly.
- [A long list of action checkboxes can make the menu tall] -> Use a scrollable checklist and allow selected actions to remain visible in the trigger summary or menu header when practical.
- [Popover state can be harder to implement accessibly than inline filters] -> Use existing button/popover patterns where possible and verify keyboard/focus behavior during implementation.
