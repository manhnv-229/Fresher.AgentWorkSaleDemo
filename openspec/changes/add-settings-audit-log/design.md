## Context

The repo already has a routed settings workspace in the frontend and an `audit_logs` table in the backend, but those two pieces are not yet connected into a usable audit-review experience. Administrators can change agents, upload or delete files, manage permissions, and sign in, yet there is no dedicated settings page that exposes the resulting audit history with the fields operators expect to review.

This change crosses multiple modules: frontend settings navigation, audit-log read APIs, and the command flows that emit audit entries for agent, file, auth, and permission actions. The design therefore needs to keep audit persistence append-only while adding a narrow read surface that the settings UI can consume safely.

## Goals / Non-Goals

**Goals:**
- Add an `Audit Log` destination inside the existing `Thiết lập` navigation without disrupting the current settings pages.
- Provide a read-only audit log page that shows `Action`, `UserName`, `CreatedDate`, `IPAddress`, and `Description`.
- Ensure the requested actions are consistently recorded: login, agent create/update/delete, file upload/delete, and permission changes.
- Reuse the current audit-log persistence model and expose records through a backend query flow that respects existing auth boundaries.

**Non-Goals:**
- Building a full audit analytics dashboard, export flow, or advanced reporting.
- Introducing edit or delete behavior for audit entries.
- Redesigning unrelated settings pages or changing the broader authorization model beyond what is needed to read audit history.

## Decisions

1. Add a dedicated settings route and sidebar item for audit history.

   The frontend will extend the existing settings route tree with a dedicated page such as `/settings/audit-log`, and the shared settings navigation in `WorkspaceShell.vue` will include an `Audit Log` item next to `Quản lý thành viên` and `Đổi mật khẩu`.

   Why this approach:
   - It matches the repo's current route-per-settings-page structure.
   - It keeps audit review discoverable in the same workspace as other administrative settings.
   - It avoids reintroducing view-state branching inside large page components.

   Alternative considered: render audit logs inline on an existing settings page. This is rejected because audit history is a distinct workflow with its own loading, table state, and access expectations.

2. Expose audit logs through a read-only query endpoint tailored for the settings page.

   The backend will add an application/query flow and API endpoint that returns audit-log rows shaped for the UI fields: action, user name, created date, IP address, and description. The endpoint should return append-only records and can map existing persistence fields into presentation-friendly DTOs without changing how logs are stored.

   Why this approach:
   - It decouples internal entity structure from the settings page contract.
   - It allows the frontend to stay simple and use a single list-fetching call.
   - It leaves room for future paging or filtering without changing the page's basic contract.

   Alternative considered: have the frontend query generic entity endpoints or infer history from domain records. This is rejected because audit history is cross-cutting, append-only, and should not depend on reconstructing events from mutable resources.

3. Standardize audit emission around explicit action codes and human-readable descriptions.

   Each requested workflow will emit a dedicated audit entry with a stable action name and a descriptive message. Login, agent create/update/delete, file upload/delete, and permission changes will be treated as first-class audit events instead of optional side effects.

   Why this approach:
   - It makes the settings page predictable because every row has a clear action and description.
   - It reduces ambiguity when future filters or reports need to group by action type.
   - It aligns with the existing append-only audit-log model already described in prior OpenSpec changes.

   Alternative considered: store only a free-form description and derive the action later. This is rejected because the requested UI explicitly needs an `Action` column and stable categorization.

4. Capture request metadata at the point where commands already know actor context.

   Login handlers, agent commands, file-management handlers, and permission-management handlers should attach actor identity and source IP when creating audit entries. This keeps metadata capture closest to the workflow that knows what changed and avoids brittle inference later in the request pipeline.

   Why this approach:
   - It improves accuracy for descriptions and target references.
   - It avoids a generic interceptor having to inspect many payload shapes to guess what happened.
   - It keeps missing-IP behavior explicit when request context is unavailable.

   Alternative considered: central middleware that logs every request uniformly. This is rejected for now because it would capture low-value noise and still would not reliably describe domain-level changes such as which agent or permission assignment changed.

## Risks / Trade-offs

- [Some existing write flows may not yet emit audit entries consistently] -> Audit each requested workflow during implementation and add regression coverage around log creation for the enumerated actions.
- [The audit page may need paging once history grows] -> Design the backend response as a query boundary that can add pagination without breaking the route or sidebar contract.
- [IP address availability can vary by environment or proxy setup] -> Treat IP capture as best-effort but keep the field in the API response so the UI can render it when present.
- [Authorization for viewing audit history may be broader or narrower than expected] -> Reuse existing admin/settings access rules first and document any scope gaps discovered during implementation before widening access.
