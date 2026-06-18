## Why

The repository already has working backend and frontend code, but its folder layout does not yet reflect the intended Clean Architecture boundaries. Refactoring the structure now will make dependencies clearer, reduce accidental coupling, and give future feature work a consistent place to live.

## What Changes

- Reorganize the backend into explicit Clean Architecture layers with clear boundaries between domain rules, application use cases, infrastructure details, and API delivery concerns.
- Move backend code toward a `src/Domain`, `src/Application`, `src/Infrastructure`, and `src/API` layout.
- Keep domain logic independent of infrastructure and API concerns.
- Keep application logic focused on DTOs, services, CQRS, validation, and use-case orchestration.
- Keep infrastructure responsible for repositories, database access, logging, file storage, and external integrations.
- Keep API responsible for controllers, middleware, authentication, authorization, and Swagger.
- Reorganize the frontend into the requested `src/api`, `src/views`, `src/components`, `src/layouts`, `src/stores`, `src/router`, `src/utils`, and `src/composables` structure.
- **BREAKING**: update import paths, project references, and file locations where needed to match the new folder structure.

## Capabilities

### New Capabilities

- `backend-clean-architecture-layout`: Backend project structure and layer boundaries aligned to Clean Architecture conventions.
- `frontend-folder-structure-alignment`: Frontend source tree reorganized into consistent top-level folders for API, views, components, layouts, stores, router, utils, and composables.

### Modified Capabilities

- None.

## Impact

- Affected backend projects: `backend/Demo.Api`, `backend/Demo.Application`, `backend/Demo.Domain`, and `backend/Demo.Infrastructure`.
- Affected frontend sources: `frontend/src/**/*`.
- Affected solution and project references: backend `.sln` and `.csproj` files may need path updates after files move.
- Affected developer workflow: import paths, namespace alignment, and solution structure will change, but runtime behavior should remain the same.
- Migration impact: no database or API contract migration is expected; this is primarily a source layout and dependency-boundary refactor.
