## 1. Backend Agent List Filtering

- [x] 1.1 Extend the internal-agent list endpoint to accept optional `status` and `search` query parameters.
- [x] 1.2 Extend the tenant-agent list endpoint to accept optional `status` and `search` query parameters.
- [x] 1.3 Apply optional EF Core filtering for agent `status`, `name`, `description`, and `role` while preserving internal-vs-tenant scope boundaries.
- [x] 1.4 Add request validation or normalization for empty and invalid filter values so the API behavior stays predictable.

## 2. Frontend Agent Catalog Filters

- [x] 2.1 Add a shared search input and status selector to the admin agent catalog page for the active list view.
- [x] 2.2 Update internal-agent loading so the frontend sends the active `status` and `search` filters to the internal-agent API.
- [x] 2.3 Update tenant-agent loading so the frontend sends the active `status` and `search` filters to the selected tenant-agent API.
- [x] 2.4 Add empty-state messaging that explains when no agents match the current filters instead of showing a generic blank list.

## 3. Verification

- [ ] 3.1 Verify internal-agent status filtering returns only matching statuses.
- [ ] 3.2 Verify internal-agent keyword search matches `name`, `description`, or `role`.
- [ ] 3.3 Verify tenant-agent filtering does not return internal agents or agents from other tenants.
- [ ] 3.4 Verify the UI refreshes agent cards correctly when search text or status changes.
- [x] 3.5 Run frontend and backend builds or focused smoke checks and record any remaining environment limitations.
