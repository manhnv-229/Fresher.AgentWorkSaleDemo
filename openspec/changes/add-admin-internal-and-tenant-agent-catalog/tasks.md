## 1. Agent Scope and Persistence

- [x] 1.1 Extend the agent domain model and persistence configuration to represent internal scope separately from tenant scope.
- [x] 1.2 Update the SQL initialization or migration scripts so internal agents can exist without a tenant while tenant agents remain tenant-bound.
- [x] 1.3 Extend seed data with at least one internal admin-only agent plus tenant agents for the demo tenants.

## 2. Backend API and Authorization

- [x] 2.1 Add admin-only internal-agent list and create endpoints in `backend/Demo.Api`.
- [x] 2.2 Protect internal-agent endpoints with global admin authorization using the existing permission-based model.
- [x] 2.3 Update tenant agent queries and create flow so tenant endpoints return and create tenant-scoped agents only.
- [x] 2.4 Return agent response fields needed by the frontend catalog cards and modal flow without relying on mock data.

## 3. Frontend Admin Agent Workspace

- [x] 3.1 Add an authenticated admin agent management view that becomes the post-login destination for admin users.
- [x] 3.2 Build the internal-agent list UI and admin-only create-agent modal matching the requested layout and form fields.
- [x] 3.3 Add a tenant sidebar that loads available tenants and tracks the selected tenant context.
- [x] 3.4 Fetch and render the selected tenant's agents separately from the internal-agent list.
- [x] 3.5 Add loading, empty, and forbidden/error states for internal-agent and tenant-agent data sources.

## 4. Verification

- [ ] 4.1 Verify admin login opens the agent workspace and shows the internal-agent list.
- [ ] 4.2 Verify admin can create an internal agent and that it is not returned by tenant agent APIs.
- [ ] 4.3 Verify selecting each tenant in the sidebar loads only that tenant's agents.
- [ ] 4.4 Verify non-admin users cannot access internal-agent APIs or views.
- [x] 4.5 Run backend and frontend builds and record any remaining environment limitations.
