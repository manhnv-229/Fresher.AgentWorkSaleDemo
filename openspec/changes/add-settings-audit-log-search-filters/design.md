## Context

The current audit log page is a read-only table that loads every record with a single `getAuditLogs()` call and renders the five required columns. That is enough for a small dataset, but it does not support operational workflows where an administrator needs to isolate a specific action, user, time window, IP address, or description fragment.

This enhancement affects both the frontend and backend. The page needs field-level controls and state management for combined filters, while the API and repository need a query contract that can constrain results efficiently without forcing the browser to download the full audit history first.

## Goals / Non-Goals

**Goals:**
- Add field-level search and filtering for `Action`, `UserName`, `CreatedDate`, `IPAddress`, and `Description` on the settings audit log page.
- Allow multiple filters to be combined in one request.
- Move filtering responsibility to the backend query layer so the page remains usable as audit history grows.
- Preserve the existing read-only audit log experience and required result columns.

**Non-Goals:**
- Building export, saved filter presets, or advanced analytics.
- Changing how audit entries are recorded or retained.
- Adding edit/delete behavior or a separate audit dashboard outside `Thiết lập`.

## Decisions

1. Use a structured filter model instead of a single keyword search.

   The frontend and backend will share a filter shape with dedicated fields for action, user name, created-date range, IP address, and description. `CreatedDate` will be modeled as a start/end date window, while the text-oriented fields will use partial-match search semantics.

   Why this approach:
   - It matches the user's request for search/filter per field.
   - It avoids overloading one keyword box with ambiguous meaning.
   - It makes combined filtering predictable for audits and investigations.

   Alternative considered: a single global search box plus client-side table filters. This is rejected because it does not express per-field intent clearly and becomes inefficient as log volume grows.

2. Execute filtering on the backend and keep the frontend as a query composer.

   The settings page will submit the selected filters to the audit-log endpoint, and repository/query logic will translate them into database predicates. The client will no longer treat `getAuditLogs()` as an unconditional "load everything" operation.

   Why this approach:
   - It scales better than loading all rows then filtering in memory.
   - It keeps filtering logic consistent across browsers and future UI refinements.
   - It allows the repository to reuse indexed fields such as action and created date where available.

   Alternative considered: fetch all logs once and filter reactively in Vue. This is rejected because audit history is append-only and likely to grow, making full-list loading progressively more expensive.

3. Provide an explicit apply/reset filter workflow on the page.

   The audit log page will present a compact filter form above the table with controls for each field, an `Apply` action, and a `Reset` action. This gives users a clear moment to combine criteria before requesting data and avoids excessive network requests while typing into multiple fields.

   Why this approach:
   - It supports multi-field investigation workflows cleanly.
   - It is simpler to reason about than live queries firing on every keystroke.
   - It gives users an obvious way to return to the unfiltered list.

   Alternative considered: auto-query on every input change. This is rejected for now because it complicates debounce behavior, increases API chatter, and adds state-management edge cases for date ranges and partial text entry.

4. Keep result rendering and read-only semantics unchanged after filtering.

   Filtered results will still render the same audit log columns and read-only table behavior already defined for the settings page. Empty results should be shown as a no-match state rather than an error.

   Why this approach:
   - It keeps the enhancement narrowly focused on discovery/filtering.
   - It preserves the current audit review contract and avoids unnecessary UX churn.

   Alternative considered: change the page into a richer grid with sorting, inline actions, or column customization. This is rejected because those concerns are separate from the requested filtering behavior.

## Risks / Trade-offs

- [Filter semantics may be inconsistent across fields] -> Define clear matching rules in the API contract, especially partial text matching and inclusive date boundaries.
- [Unbounded result sets may still be heavy even after filtering] -> Keep the query boundary extensible for pagination if the current list size becomes a bottleneck.
- [Reset/apply state can drift from URL or refresh behavior] -> Keep the first implementation focused on in-page state and test reset/reload behavior carefully before adding query-string synchronization.
- [Date filtering can be confusing across time zones] -> Apply one consistent server/client interpretation for start and end dates and document it in the implementation.
