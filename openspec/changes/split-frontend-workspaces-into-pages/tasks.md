## 1. Routing and Shell Foundation

- [x] 1.1 Define the authenticated frontend route structure for internal agents, tenant agents, agent detail, and settings pages.
- [x] 1.2 Create a shared authenticated workspace shell that owns sidebar navigation, auth initialization checks, and routed content rendering.
- [x] 1.3 Move common workspace navigation logic out of `AgentCatalogPage.vue` into the new shell and router flow.

## 2. Agent Workspace Extraction

- [x] 2.1 Extract the internal-agent list into its own dedicated page component.
- [x] 2.2 Extract the tenant-agent list into its own dedicated page component with tenant-aware route handling.
- [x] 2.3 Extract agent detail/edit into a dedicated page component that reuses the existing detail/update/delete APIs.
- [x] 2.4 Preserve tenant selection, search filters, status filters, pagination, and return-to-list behavior across routed agent pages.

## 3. Settings and Supporting Features

- [x] 3.1 Extract member management into a dedicated settings page or nested route segment.
- [x] 3.2 Extract password change into a dedicated settings page or nested route segment.
- [x] 3.3 Add shared composables or helper modules for repeated data-loading, filter-state, and action-handling logic used across the new pages.

## 4. Cleanup and Verification

- [x] 4.1 Remove obsolete workspace-mode branches and responsibilities from `AgentCatalogPage.vue`, or retire the file entirely once replacement pages are authoritative.
- [x] 4.2 Verify route navigation, browser back/forward behavior, and auth-protected access across agent and settings pages.
- [x] 4.3 Verify internal, tenant, detail/edit, member-management, and password-change flows still work with the same APIs and preserve expected context.
