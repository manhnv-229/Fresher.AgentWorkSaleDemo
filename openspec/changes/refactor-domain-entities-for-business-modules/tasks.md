## 1. Refactor core domain entities

- [x] 1.1 Inventory the current domain entities, enums, and repository contracts that depend on `Agent`, `Tenant`, `User`, `Role`, `UserTenant`, `UserRole`, `UserSession`, and `RefreshToken`
- [x] 1.2 Expand or replace the current status enums so agent lifecycle and account/tenant states match the approved business meanings
- [x] 1.3 Refactor `Agent` to include business identity, creator ownership, tenant ownership, lifecycle timestamps, and publish/delete-ready status fields
- [x] 1.4 Refactor `User`, `Tenant`, `Role`, `UserTenant`, and `UserRole` so account state, tenant membership, and global-vs-tenant role assignment are explicit in the domain model
- [x] 1.5 Update or remove obsolete entity properties that no longer fit the approved business model, keeping navigation properties coherent

## 2. Add knowledge and audit entities

- [x] 2.1 Add domain entities for hierarchical agent knowledge folders with self-referencing parent-child relationships
- [x] 2.2 Add domain entities for agent knowledge files with logical file metadata, creator metadata, and folder placement references
- [x] 2.3 Add an append-only audit log entity that captures action code, actor, tenant context, target context, IP address, and description
- [x] 2.4 Review aggregate boundaries and navigation properties so knowledge and audit entities stay scoped correctly to agent and tenant data

## 3. Align persistence and application contracts

- [x] 3.1 Update EF Core configurations and `DbContext` mappings for all refactored and newly added entities
- [x] 3.2 Update repository interfaces, query filters, and infrastructure repositories to use the new field names, relationships, and status values
- [x] 3.3 Update application DTOs and service mappings that currently read or write the old entity shapes
- [x] 3.4 Adjust authorization-related services so tenant membership, creator ownership, and account status use the refactored domain model

## 4. Prepare data migration and verification

- [x] 4.1 Create or update database migrations and seed data for the refactored entities, enums, knowledge records, and audit-log tables
- [ ] 4.2 Add or update automated tests covering agent status mapping, tenant-role boundaries, creator ownership behavior, knowledge tree relationships, and audit entry creation
- [ ] 4.3 Run backend verification for build/tests and resolve any regressions caused by the entity refactor
