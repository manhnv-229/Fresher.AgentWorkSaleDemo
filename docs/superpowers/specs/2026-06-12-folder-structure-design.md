## Context

The repository already separates backend code into `Demo.Api`, `Demo.Application`, `Demo.Domain`, and `Demo.Infrastructure`, which is a good Clean Architecture base. The current pain point is inside each layer: files are still grouped mostly by technical type such as `Controllers`, `Requests`, `Responses`, `DTOs`, and `Services`, so related auth, tenant, and agent files are spread across multiple folders.

The frontend shows a similar pattern. It has some feature-oriented grouping in `features/auth` and `features/dashboard`, but still mixes app-level and domain-level concerns across `views`, `services`, `stores`, `types`, and `api`. That makes the current admin agent catalog harder to extend and will get worse as tenant, role, and user management grow.

The goal of this design is to improve structure clarity without throwing away the current solution boundaries or forcing a large risky rewrite.

## Goals / Non-Goals

**Goals:**

- Keep the backend split into `Api`, `Application`, `Domain`, and `Infrastructure`.
- Reorganize backend internals so files are grouped by feature first, then by technical role where useful.
- Reorganize frontend internals so business logic lives under `features/<feature>` and only true shared code stays global.
- Reduce “where should this file go?” ambiguity for future work on auth, agents, tenants, roles, and users.
- Make the migration incremental so features can keep shipping while the structure improves.

**Non-Goals:**

- Rewriting business logic or changing feature behavior as part of the folder move.
- Replacing Clean Architecture with a completely different backend architecture.
- Converting every backend action into a strict vertical-slice-per-endpoint structure in one pass.
- Doing a big-bang rename of the whole repository in one change.

## Decisions

1. **Keep the current backend project boundaries, but reorganize inside each project by feature.**

   The backend will continue using:
   - `Demo.Api`
   - `Demo.Application`
   - `Demo.Domain`
   - `Demo.Infrastructure`

   Inside `Demo.Api` and `Demo.Application`, feature folders will become the primary organizing unit, for example:
   - `Features/Auth`
   - `Features/Agents`
   - `Features/Tenants`

   Alternative considered: merge backend projects or move to a single vertical-slice project. That is rejected because the current four-project separation already provides useful boundaries and changing both macro-architecture and folder layout at once would add unnecessary migration risk.

2. **Use a hybrid clean + feature structure for backend instead of pure technical buckets.**

   `Demo.Api` will keep only app-level cross-cutting areas outside features, such as:
   - `Authorization`
   - `Common`

   `Demo.Application` will mirror the same feature names and keep use-case contracts close to those features instead of concentrating everything under generic `DTOs` or `Services`.

   Alternative considered: keep global folders like `Controllers`, `Requests`, and `Responses` and only rename a few files. That is rejected because it preserves the main navigation problem and keeps related files spread too far apart.

3. **Keep `Demo.Domain` mostly technical because domain concepts are already the primary grouping.**

   `Entities`, `Enums`, and future domain-level concepts such as `Policies` or `ValueObjects` can remain organized by type because the domain layer is small and already business-oriented.

   Alternative considered: break `Domain` into feature folders immediately. That is deferred because the current domain is not yet large enough to justify the added nesting and churn.

4. **Move frontend to feature-first organization with a thin shared layer.**

   The frontend will use:
   - `src/app`
   - `src/features/auth`
   - `src/features/agents`
   - `src/features/tenants`
   - `src/shared/ui`
   - `src/shared/api`
   - `src/shared/lib`

   App bootstrap and router stay in `app`. Reusable UI primitives stay in `shared/ui`. HTTP infrastructure stays in `shared/api`. Pure helpers stay in `shared/lib`. Everything business-specific moves into feature folders.

   Alternative considered: keep `views`, `services`, `stores`, and `types` as top-level global buckets. That is rejected because those buckets mix unrelated domains and make ownership unclear.

5. **Replace the temporary `dashboard` naming with business naming centered on agents.**

   The current admin workspace is fundamentally an agent catalog, not a generic dashboard. The feature should be named `agents`, and its page can become something like `features/agents/pages/AgentCatalogPage.vue`.

   Alternative considered: keep `dashboard` as a catch-all future area. That is rejected because broad placeholder naming usually becomes a dumping ground and weakens structure over time.

6. **Migrate in small feature-by-feature steps instead of a global move.**

   The migration sequence will be:
   1. Frontend `dashboard` to `agents`, and move the current page into the feature.
   2. Backend `Api` and `Application` folders for `Auth`, `Agents`, and `Tenants`.
   3. Backend `Infrastructure` cleanup after feature folders in upper layers are stable.

   Alternative considered: perform a repo-wide structure rewrite in one PR. That is rejected because it would create a hard-to-review diff and increase the chance of broken imports, namespaces, and routes.

## Target Structure

### Backend

`Demo.Api`
- `Features/Auth`
- `Features/Agents`
- `Features/Tenants`
- `Authorization`
- `Common`

`Demo.Application`
- `Features/Auth`
- `Features/Agents`
- `Features/Tenants`
- `Authorization`
- `Common`

`Demo.Domain`
- `Entities`
- `Enums`
- `ValueObjects`
- `Exceptions`
- `Interfaces`

`Demo.Infrastructure`
- `Persistence`
- `Auth`
- `Authorization`
- `Features/Auth`
- `Features/Agents`
- `Common`

### Frontend

`src/app`
- `App.vue`
- `main.ts`
- router and app bootstrap concerns

`src/features/auth`
- login components
- auth composables
- auth API contracts
- auth state for the feature

`src/features/agents`
- `pages/AgentCatalogPage.vue`
- internal agent list
- tenant agent list
- create-agent modal
- agent API/types/composables

`src/features/tenants`
- tenant sidebar
- tenant types
- tenant-specific API glue if it grows large enough

`src/shared/ui`
- `BaseButton`
- `BaseInput`
- `BaseModal`

`src/shared/api`
- `http.ts`
- interceptors
- shared API error handling

`src/shared/lib`
- formatters
- validators
- utility helpers

## File Placement Rules

- `app/` contains only app bootstrap and top-level composition.
- `features/<feature>/pages` contains route-level pages.
- `features/<feature>/components` contains UI tied to that feature only.
- `features/<feature>/api` contains feature-specific API clients and contracts.
- `features/<feature>/composables` contains feature-specific state and behavior hooks.
- `shared/ui` contains reusable presentational components with no business meaning.
- `shared/api` contains transport concerns shared across features.
- `shared/lib` contains pure helpers with no feature ownership.
- New generic folders such as `helpers`, `misc`, or `common` should not be added unless the code truly does not belong to a feature or an existing shared area.

## Risks / Trade-offs

- **Large rename diffs can obscure behavior changes** -> Keep structure-only changes separate from feature logic changes whenever possible.
- **Imports and namespaces can break during moves** -> Migrate one feature at a time and run build checks after each step.
- **Hybrid structure can drift back into technical buckets** -> Use explicit file placement rules and keep feature ownership visible in code review.
- **Frontend `tenants` may remain thin at first** -> Start with `agents` as the main feature and only split `tenants` further when it has enough code to justify the boundary.
- **Infrastructure organization can become inconsistent** -> Delay deep infrastructure cleanup until API and application feature boundaries are stable.

## Migration Plan

1. Move frontend `dashboard` code into `features/agents`.
2. Move `HomeView.vue` responsibilities into `features/agents/pages/AgentCatalogPage.vue`.
3. Move shared frontend primitives into `shared/ui`, `shared/api`, and `shared/lib`.
4. Create backend `Features/Auth`, `Features/Agents`, and `Features/Tenants` folders in `Demo.Api`.
5. Move related request/response/controller files into those backend feature folders.
6. Mirror the same feature grouping inside `Demo.Application`.
7. Clean up `Demo.Infrastructure` only after the upper-layer feature groupings are stable.
8. Re-run build and smoke verification after each migration slice.

## Open Questions

- Should tenant sidebar behavior stay inside `features/agents` for now, or split into `features/tenants` immediately once the folder migration starts?
- Should backend request/response contracts stay in `Demo.Api` feature folders permanently, or later move to more explicit HTTP contract subfolders if API surface grows a lot?
