## Context

The current member-management flow is table-first: admins can scan employees and perform status changes directly from the list, but they do not have a focused detail surface for reading or editing employee information. The latest member-list direction already emphasizes employee identity and job context, so the next step is to make each row open a popup that centralizes inspection and actions.

This change crosses frontend interaction design, backend update APIs, and audit logging. Updating `job_position` is a profile mutation rather than a pure status toggle, so it needs a clear edit path, authorization, and an audit entry describing what changed. Moving lock/unlock into the same popup also changes how admins reach an existing security action.

## Goals / Non-Goals

**Goals:**
- Open a member-detail popup when an admin clicks a member in the settings member list.
- Show employee information in the popup in a way that supports quick review before action.
- Allow authorized admins to update `job_position` from the popup.
- Move lock/unlock controls into the popup while preserving current authorization rules and status behavior.
- Generate an audit log entry for every successful `job_position` update with actor and request metadata.

**Non-Goals:**
- Building full inline editing for every employee field.
- Replacing the list page with a dedicated employee profile route.
- Adding bulk member actions or edit history UI inside the popup.
- Changing unrelated audit-log filters or settings navigation in this change.

## Decisions

1. Use a popup/modal detail surface instead of a route-level detail page.

   The user asked for a popup, and that also fits the current settings workflow better than introducing a full navigation step. The member list remains the main context, while the popup handles focused review and actions.

   Why this approach:
   - It preserves list context and makes it easy to inspect multiple members quickly.
   - It keeps the feature scoped to the existing settings page.
   - It avoids adding router complexity for a workflow centered on lightweight admin actions.

   Alternative considered: navigate to a separate member detail page. This is rejected because it adds more navigation overhead than the requested interaction needs.

2. Update `job_position` through a dedicated member-management mutation rather than overloading lock/unlock endpoints.

   `job_position` editing is a different type of state change from account locking, so it should use a separate update command or endpoint with its own validation and audit side effects.

   Why this approach:
   - It keeps the API contract explicit and easier to test.
   - It allows job-position updates to evolve without coupling them to account-status semantics.
   - It makes audit logging straightforward because the mutation boundary is clear.

   Alternative considered: combine status and profile updates in one generic patch endpoint. This is rejected because it broadens scope and weakens the clarity of authorization and auditing behavior.

3. Reuse list summary data for popup display when possible, and return updated summary-compatible data after mutation.

   The popup can open from the selected row using data already loaded in the table, and update responses should return enough data to refresh the list and popup consistently after a successful `job_position` change or lock/unlock action.

   Why this approach:
   - It minimizes extra network round-trips for initial popup open.
   - It keeps the frontend state model simpler.
   - It ensures the list and popup stay synchronized after actions.

   Alternative considered: require a second detail fetch every time a popup opens. This is rejected unless later needed for fields not present in the list.

4. Record audit entries only for successful `job_position` updates, with meaningful change description.

   The audit log should capture what job position was changed, who changed it, and request context such as IP address when available. Failed or unauthorized attempts can continue to rely on normal API error handling unless a broader security audit requirement is added later.

   Why this approach:
   - It aligns with the existing audit-log model of recording completed sensitive actions.
   - It avoids generating noisy audit entries for validation errors.
   - It gives operators a clear trace of employee role/profile changes.

   Alternative considered: log every attempted update. This is rejected for now because it changes audit semantics and could create excess noise without a stated requirement.

## Risks / Trade-offs

- [A popup can become crowded if too many fields or actions are shown at once] -> Keep the popup focused on review, `job_position` editing, and account-status controls only.
- [Moving lock/unlock out of the table may slow down operators who only want a quick toggle] -> Make the popup quick to open and ensure the status action remains prominent inside it.
- [Frontend state can drift if popup edits do not refresh the table row correctly] -> Return updated member data from mutations and use that payload to update both popup and list state.
- [Audit log descriptions may be inconsistent across profile updates] -> Define one stable action name and description format for `job_position` updates.
