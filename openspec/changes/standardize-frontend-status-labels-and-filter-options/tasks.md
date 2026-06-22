## 1. Shared frontend status mapping

- [ ] 1.1 Add a shared frontend status-mapping module for known agent and member status codes with Vietnamese labels.
- [ ] 1.2 Add helpers or exported option lists for common uses such as filter dropdowns and edit-form selects without requiring page-local hardcoded arrays.

## 2. Migrate agent screens

- [ ] 2.1 Replace hardcoded status filter options in `AgentCatalogPage.vue`, `InternalAgentsPage.vue`, and `TenantAgentsPage.vue` with the shared status source.
- [ ] 2.2 Update status rendering in agent list/detail/edit flows so visible labels are Vietnamese while underlying values remain backend codes.
- [ ] 2.3 Audit the current agent-related frontend pages for any remaining hardcoded status/filter options and migrate applicable cases to the shared source.

## 3. Migrate member-management screens

- [ ] 3.1 Replace hardcoded member status options in `SettingsMembersPage.vue` with the shared status source.
- [ ] 3.2 Update member table and popup status labels to use the shared Vietnamese mapping consistently.

## 4. Verification

- [ ] 4.1 Add or update frontend tests or manual verification for localized status labels and shared filter options across agent list, agent detail/edit, and member-management pages.
- [ ] 4.2 Verify API requests still send backend status codes correctly after the frontend label mapping changes.
