## Context

The member-management page now acts as an employee directory with five main columns and is also gaining popup-driven actions for profile review, `job_position` updates, and lock/unlock behavior. As the number of employees grows, scanning the full table becomes slower, especially when admins only remember part of a person's name, employee code, email, or need to focus on one account status such as `Locked` or `Active`.

This change spans frontend and backend behavior because search and status filters should shape the returned member list rather than only hiding rows after the full dataset is loaded. The feature also needs to coexist cleanly with the current member-table layout and the detail popup workflow.

## Goals / Non-Goals

**Goals:**
- Add a visible search input to the member-management page.
- Support filtering the member list by employee `Trạng thái`.
- Define a stable search scope for member lookup, such as employee name, employee code, email, and possibly project/job position if included in the page contract.
- Ensure search and status filter state can be applied together and reflected in the rendered list.
- Preserve compatibility with row click and popup detail actions on the filtered result set.

**Non-Goals:**
- Adding advanced multi-field filtering beyond status in this change.
- Building saved filters, pagination redesign, or server-side sorting changes unless required incidentally by the implementation.
- Changing audit-log behavior or employee profile editing requirements.

## Decisions

1. Use one visible free-text search input for member lookup.

   The page will expose a single search field rather than separate inputs for name, code, and email. This keeps the toolbar light and matches the most common operator workflow where admins only remember one identifying fragment.

   Why this approach:
   - It reduces UI clutter on the settings page.
   - It supports quick lookup across multiple identifying fields.
   - It stays consistent with the simpler search patterns already used elsewhere in the workspace.

   Alternative considered: separate field-specific search boxes. This is rejected because it adds complexity without clear value for the current member-management scope.

2. Represent status filtering as an explicit selectable employee-status control.

   The member toolbar should provide a clear status filter so admins can switch between all employees and targeted states such as `Active` or `Locked`. The exact control can be a select, segmented filter, or similar lightweight pattern, but the contract should treat it as one optional status filter value.

   Why this approach:
   - Status is already a first-class column and operational signal in member management.
   - A dedicated filter is faster and clearer than relying on free-text search for status values.
   - It keeps the filtering model intentional without introducing a heavy advanced-filter UI.

   Alternative considered: fold status into the free-text search only. This is rejected because it is less discoverable and less precise.

3. Keep filtering server-driven when possible.

   The backend list query should accept search text and status filter inputs so the page can request only matching member results. This avoids over-fetching as the dataset grows and keeps behavior aligned with other admin list screens that use query-driven filtering.

   Why this approach:
   - It scales better than always loading all users into the browser.
   - It keeps frontend and backend behavior consistent for empty results and future pagination.
   - It makes the search/filter contract reusable for later refinements.

   Alternative considered: frontend-only filtering on the already loaded list. This is rejected as the primary approach because it assumes small datasets forever.

## Risks / Trade-offs

- [Search scope may feel inconsistent if users do not know which fields are searchable] -> Define the supported search fields clearly in the API and UI behavior.
- [Status filtering could conflict with popup refresh after an update or lock/unlock action] -> Re-apply the current active search and filter state after member mutations.
- [Server-driven filtering adds query-contract work across layers] -> Keep the filter model minimal with one search text and one optional status value.
- [No-result states may be confused with load failures] -> Provide a distinct empty-results state for the active search/filter combination.
