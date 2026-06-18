## 1. Tenant Lifecycle Backend

- [x] 1.1 Extend tenant DTOs, service contracts, and repository contracts for tenant detail, update, and lock operations.
- [x] 1.2 Implement tenant validation for required fields, unique code checks, and not-found handling in the application layer.
- [x] 1.3 Add persistence and service logic to update tenant metadata, transition tenant status to `Locked`, and preserve lifecycle timestamps.

## 2. Tenant User Management Backend

- [x] 2.1 Add DTOs and service/repository methods for listing tenant members and fetching tenant membership state.
- [x] 2.2 Implement add, status update, and remove flows for `UserTenant` membership records with tenant-scoped uniqueness checks.
- [x] 2.3 Define and implement the rule for how tenant membership changes affect related tenant-specific role assignments.

## 3. API Authorization and Data Partitioning

- [x] 3.1 Add tenant detail, update, and lock endpoints under `/api/tenants/{tenantId}` with `tenant.*` permission checks.
- [x] 3.2 Add tenant membership endpoints under `/api/tenants/{tenantId}/users` with `user.view`, `user.invite`, and `user.update` permission checks.
- [x] 3.3 Audit tenant-owned query and mutation paths so repository and service logic enforce tenant predicates and block operations for locked tenants.

## 4. Frontend Tenant Management Experience

- [x] 4.1 Update frontend API clients and state models to consume tenant detail and tenant membership responses.
- [x] 4.2 Add admin UI flows for tenant browsing, tenant detail/edit, and tenant lock actions.
- [x] 4.3 Add tenant user-management UI flows for listing members, adding users, changing membership status, and removing membership while preserving the active tenant context.

## 5. Verification

- [x] 5.1 Verify tenant create, detail, update, and lock flows for authorized and unauthorized users.
- [x] 5.2 Verify tenant membership list, add, update, and remove flows including duplicate membership and missing-resource paths.
- [x] 5.3 Verify locked tenants and inactive memberships block tenant-scoped operational access as specified.
- [x] 5.4 Run available backend and frontend builds/tests and capture any remaining gaps or follow-up questions.
