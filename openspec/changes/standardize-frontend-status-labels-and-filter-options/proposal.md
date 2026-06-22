## Why

Several frontend pages still render status values and filter options directly in English or define their own hardcoded status-option arrays inline. This creates an inconsistent Vietnamese UI, duplicates logic across agent and member screens, and makes it easy for filter labels to drift as more pages are added.

## What Changes

- Map status values shown in frontend filters and status chips from backend English codes to Vietnamese labels across the relevant pages.
- Replace hardcoded status filter options in the agent list screens with a shared frontend source of truth.
- Extend the same shared mapping approach to other frontend screens that currently duplicate status-option declarations or display raw English status labels.
- Audit the current frontend for remaining hardcoded status/filter option definitions related to agent and member management, and migrate them to the shared configuration where applicable.
- Preserve existing backend status codes and filter values while changing only frontend presentation and option sourcing.

## Capabilities

### New Capabilities

- None.

### Modified Capabilities

- `admin-agent-catalog`: Agent catalog filter controls and status rendering will use Vietnamese labels sourced from a shared frontend mapping instead of page-local hardcoded options.
- `account-security`: Member-management status filters and status display will use Vietnamese labels sourced from a shared frontend mapping instead of page-local hardcoded options.

## Impact

- Affected frontend area: `AgentCatalogPage.vue`, `InternalAgentsPage.vue`, `TenantAgentsPage.vue`, `AgentDetailPage.vue`, `SettingsMembersPage.vue`, shared composables/types, and any shared filter/status utilities introduced for reuse.
- Affected UX: users will see consistent Vietnamese status labels and filter choices across agent and member pages.
- Affected maintenance flow: future status/filter changes can be made in one shared frontend location instead of multiple page-local arrays.
