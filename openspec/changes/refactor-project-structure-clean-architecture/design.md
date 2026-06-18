## Context

The repository currently uses a pragmatic structure: backend code lives under `backend/Demo.*` projects, while frontend code lives under `frontend/src` with feature-oriented and shared folders. The codebase works, but the current layout obscures Clean Architecture boundaries and makes it harder to reason about ownership at a glance.

The requested target layout is explicit: backend should read like `src/Domain`, `src/Application`, `src/Infrastructure`, `src/API`, and frontend should read like `src/api`, `src/views`, `src/components`, `src/layouts`, `src/stores`, `src/router`, `src/utils`, and `src/composables`. This is a structural refactor only; the goal is to reduce coupling and improve maintainability without changing runtime behavior.

## Goals / Non-Goals

**Goals:**

- Make architectural boundaries obvious from the folder structure.
- Keep domain, application, infrastructure, and API responsibilities separated in the backend.
- Reorganize frontend code into the requested top-level folders.
- Preserve all existing behavior, API contracts, and user-facing flows.
- Keep the migration incremental so each slice can be built and reviewed safely.

**Non-Goals:**

- Changing business rules, routes, UI behavior, or data models.
- Rewriting the backend into a different framework or the frontend into a different state-management approach.
- Introducing new product features as part of the refactor.
- Solving every possible code-style issue at the same time as the structural move.

## Decisions

1. **Refactor the backend and frontend as one change, but migrate them in separate slices.**

   The change is conceptually one architecture cleanup, but the backend and frontend will be moved independently so each layer can stay buildable. Backend will be migrated first because its boundaries are more rigid and its dependency graph is easier to validate mechanically.

   Alternative considered: split into two separate changes immediately. That would reduce scope, but it would also leave the repo half-migrated and require two planning cycles for what is really one architectural direction.

2. **Use folder ownership as the primary expression of architecture.**

   Backend source files will move to folders that mirror the Clean Architecture layers, and namespaces will follow those folders. Frontend source will move into the requested top-level buckets so responsibility is visible from the path alone.

   Alternative considered: keep the current folders and add documentation only. That is rejected because the current problem is structural clarity, and documentation alone does not make the codebase easier to navigate.

3. **Preserve public contracts and introduce adapter code only when necessary.**

   For the backend, public APIs, DTOs, and behavior should remain stable; internal files can move freely. For the frontend, imports and route registrations will be updated in place so user-visible behavior does not change.

   Alternative considered: rename everything to perfectly match the target structure, including public symbols. That is rejected because it increases breaking-change risk without adding value for this refactor.

4. **Keep core layers free of outward dependencies.**

   Domain will remain isolated, Application will depend on Domain and application-facing abstractions, Infrastructure will implement technical concerns, and API will remain the delivery boundary. This follows Clean Architecture and prevents the source tree from drifting back into tangled imports.

   Alternative considered: allow convenience references across layers when compile-time problems appear. That is rejected because it would reintroduce coupling and defeat the purpose of the move.

5. **Validate each migration step by compiling before moving on.**

   After the backend structure is moved, the solution should still build. After frontend files are relocated, the app should still start. Verification is important because structural moves tend to fail in import paths, namespace mismatches, or project references rather than in business logic.

   Alternative considered: move everything first and verify at the end. That is rejected because it makes it hard to locate the breakage source when a path or reference changes.

## Risks / Trade-offs

- **Large file moves can create noisy diffs** -> Move in dependency order and keep edits limited to path and namespace updates where possible.
- **Namespace and import churn can hide logic changes** -> Avoid functional edits during the move; use build verification to separate structure problems from behavior problems.
- **Frontend folder flattening can temporarily make feature ownership less obvious** -> Use stable naming and keep each folder role explicit in the design and tasks.
- **Some code may not fit the requested folder perfectly** -> Prefer the requested top-level buckets and introduce small subfolders only when they improve clarity.
- **Build breakage is likely during the transition** -> Migrate one layer at a time and verify after each slice.

## Migration Plan

1. Move backend projects into the Clean Architecture source tree and update the solution/project references.
2. Update backend namespaces and file paths so Domain, Application, Infrastructure, and API clearly own their responsibilities.
3. Keep backend buildable after each layer move and confirm the layered dependency direction still holds.
4. Reorganize frontend source into the requested top-level folders.
5. Update frontend imports, router registrations, and layout/component references to match the new locations.
6. Verify the frontend still starts and key user flows still work after the move.
7. Do a final cleanup pass for any stale references, dead folders, or leftover adapter paths.

Rollback strategy:

- If a backend slice breaks the build, revert only the last layer move and restore the previous project references.
- If a frontend slice breaks imports, revert the affected file moves and keep the old import paths until the next incremental pass.

## Open Questions

None. The requested target structure is explicit enough to choose a concrete migration path without more product clarification.
