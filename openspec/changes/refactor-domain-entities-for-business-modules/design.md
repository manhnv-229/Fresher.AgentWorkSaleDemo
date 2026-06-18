## Context

The backend already contains a first-pass domain model for agents, tenants, users, roles, permissions, sessions, and refresh tokens. That model is sufficient for basic login and tenant/internal agent listing, but it does not yet encode several business rules that now appear in scope: agent codes and publish states, creator-based ownership for staff, tenant-manager boundaries, account lock behavior, hierarchical agent knowledge storage, and audit history for sensitive actions.

This change is cross-cutting because it reshapes the persistent entity graph used by the Domain, Application, Infrastructure, and API layers. The design needs to keep the current Clean Architecture boundaries intact while preventing future features from introducing one-off columns or special cases in services. The main constraint is that the refactor should produce entities and enums that map cleanly to later CRUD APIs, repository filtering, and EF Core migrations without forcing the services to infer business meaning from nullable or overloaded fields.

## Goals / Non-Goals

**Goals:**

- Align entity names, fields, and relationships with the requested business modules for agent management, login, RBAC, knowledge management, audit logging, and tenant management.
- Make tenant isolation and creator ownership explicit in the domain model so manager and staff permissions can be enforced consistently.
- Extend status modeling so agents, users, and tenants can represent the required business lifecycle states without string-based logic.
- Introduce first-class entities for knowledge folders, knowledge files, and audit logs instead of embedding those concerns inside `Agent` or `UserSession`.
- Preserve the existing auth/session foundation while making room for account lock, password change tracking, and richer audit data.

**Non-Goals:**

- Implement the actual CRUD controllers, EF migrations, repository methods, or UI changes in this planning change.
- Define file binary storage, OCR/vector indexing, or document content extraction pipelines.
- Design full workflow/versioning for agent publishing beyond the lifecycle states required by the business request.
- Introduce distributed caching, soft-delete infrastructure libraries, or event-driven audit pipelines in this change.

## Decisions

1. **Separate stable business modules into focused aggregate roots and child entities.**

   `Agent`, `Tenant`, `User`, `Role`, and `Permission` remain the core roots, while new concerns become dedicated entities such as `AgentKnowledgeFolder`, `AgentKnowledgeFile`, and `AuditLogEntry`. This keeps each business area explicit and prevents `Agent` from turning into a catch-all record for documents and logs.

   Alternative considered: keep adding nullable columns and collections onto existing entities. That is rejected because knowledge management and audit history have different lifecycles, query patterns, and retention rules from the agent master record.

2. **Model agent ownership with both tenant association and creator association.**

   `Agent` will carry both a required creator reference (`CreatedByUserId`) and an optional tenant reference depending on scope. This allows staff permissions to be enforced against "agents I created" while managers retain tenant-wide visibility and control. The entity should also gain `AgentCode`, lifecycle status, timestamps, and optional soft-delete metadata or a delete status state.

   Alternative considered: infer ownership from audit logs or last modifier. That is rejected because authorization needs a stable, first-class ownership field and audit logs are append-only historical records rather than the source of truth.

3. **Use explicit business enums rather than reusing broad generic status enums.**

   `AgentStatus` should be expanded to match `Draft`, `Active`, `Inactive`, `Deleted`, and `Publish`. User account state should distinguish inactive/locked/suspended behavior from refresh-token session state. Tenant state should support active and locked/inactive semantics without overloading role or membership tables.

   Alternative considered: keep `RecordStatus` for all entities. That is rejected because agent publishing and account locking have domain-specific transitions that should not be flattened into a single generic enum.

4. **Represent tenant access and RBAC through layered membership and assignment entities.**

   Tenant membership remains separate from role assignment. `UserTenant` represents whether a user belongs to a tenant and whether that membership is active. `UserRole` represents the role assignment, with global assignments kept distinct from tenant assignments. The design should support the three current business personas: system admin, tenant manager, and staff.

   Alternative considered: store a single role directly on `User` or a single role-per-tenant column in `UserTenant`. That is rejected because it cannot scale to multiple assignments, dynamic permissions, or mixed global and tenant roles.

5. **Model knowledge management as a tree using self-referencing folders plus file metadata entities.**

   Folders will use an adjacency-list structure with `ParentFolderId` for parent/child relationships under an agent. Files will belong either to a folder or directly to an agent root container, and they will store metadata such as file name, original name, content type, extension, size, creator, and timestamps. Search filters can then target folder, creator, created date, and file name without coupling to physical storage.

   Alternative considered: store flat file rows with a string path column. That is rejected because rename/move operations and hierarchical authorization checks become fragile when path segments encode structure.

6. **Keep audit logs append-only and decoupled from transactional entities.**

   Audit records should be created as immutable entries containing action code, actor identity, tenant context, target entity context, IP address, and human-readable description. They should not be updated except through retention or archival mechanisms outside the normal business flow.

   Alternative considered: add `LastAction` or `LastModifiedBy` columns to every entity and treat that as audit. That is rejected because it loses history and does not satisfy the required log views for login, file deletion, or permission changes.

7. **Preserve the current auth/session entities, but enrich user-account modeling instead of moving lock state into sessions.**

   `UserSession` and `RefreshToken` continue to represent login continuity and logout/revocation. Account lock, password change policy fields, and user activity state should live on `User` or a dedicated account-policy child entity so account governance remains separate from individual sessions.

   Alternative considered: mark a user as locked by revoking active sessions only. That is rejected because future logins must also be blocked, and session revocation alone does not represent persistent account state.

## Risks / Trade-offs

- **Large entity refactor can break existing services and mappings** -> Introduce the new entities and enum changes behind a coordinated update of repositories, DTOs, and EF configuration rather than piecemeal edits.
- **Status semantics may drift between business language and code names** -> Centralize domain enums and document allowed transitions in the entity layer and specs before implementation.
- **Folder trees can create recursive query complexity** -> Start with adjacency-list modeling and repository methods scoped per agent; optimize traversal only when UI/API requirements prove it necessary.
- **Audit logging can become noisy or inconsistent across services** -> Standardize action codes and minimum metadata fields so all future services emit the same shape of entry.
- **Creator-based permissions can conflict with tenant-manager authority** -> Encode both creator ownership and tenant association explicitly, then keep permission evaluation rules in the application/authorization layers instead of embedding them in entity setters.
- **Soft delete versus hard delete rules may remain ambiguous** -> Treat `Deleted` as a domain-visible state for agents and files, and leave irreversible physical purge as a later operational concern.

## Migration Plan

1. Inventory the current entity classes, enums, and EF mappings that participate in auth, tenant, and agent flows.
2. Refactor core entities (`Agent`, `Tenant`, `User`, `Role`, `UserTenant`, `UserRole`, `UserSession`, `RefreshToken`) to include the required business fields and relationships.
3. Add new entity classes and configurations for knowledge folders, knowledge files, and audit logs.
4. Update repository contracts, query filters, and DTO mappings to use the new field names, statuses, and ownership relationships.
5. Create or update EF Core migrations and seed data so the database schema matches the refactored model.
6. Update service-layer authorization logic to use creator ownership, tenant membership, and new account/agent statuses.
7. Validate backward compatibility decisions, then remove obsolete fields and mappings that no longer represent the agreed business model.

Rollback would revert the entity/configuration changes together with their migrations. If any migration transforms existing data, rollback must include a reverse data-mapping path for renamed columns, split tables, and new foreign keys before restoring the previous code.

## Open Questions

- Should the business lifecycle state named `Publish` be represented as the terminal status name exactly, or should implementation normalize it to `Published` while keeping API/database mapping compatible with business terminology?
- For knowledge files, do we need a separate version-history entity immediately, or is single-version metadata enough for the first iteration?
- Should tenant lock state disable all user memberships automatically, or should tenant and membership status remain independently queryable?
- Is password-change history required at the entity level now, or is a `PasswordChangedAt` timestamp on `User` sufficient for the first implementation?
