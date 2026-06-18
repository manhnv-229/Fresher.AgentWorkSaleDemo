# Folder Structure Migration Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Reorganize the current frontend and backend folders toward the approved feature-first/Clean Architecture hybrid structure without changing runtime behavior.

**Architecture:** Keep the existing backend project boundaries (`Demo.Api`, `Demo.Application`, `Demo.Domain`, `Demo.Infrastructure`) and move API/Application files into feature folders. On the frontend, move app bootstrap into `app`, shared primitives into `shared`, and current business code into `features/auth` and `features/agents`.

**Tech Stack:** ASP.NET Core 9, Vue 3, TypeScript, Vite

---

### Task 1: Move frontend app shell and shared infrastructure

**Files:**
- Create: `frontend/src/app/`
- Create: `frontend/src/shared/ui/`
- Create: `frontend/src/shared/api/`
- Create: `frontend/src/shared/lib/`
- Modify: `frontend/src/main.ts`
- Modify: `frontend/src/App.vue`

- [ ] **Step 1: Move shared UI and helper files**

Move these files:

```bash
mkdir -p frontend/src/app frontend/src/shared/ui frontend/src/shared/api frontend/src/shared/lib
mv frontend/src/components/ui/BaseButton.vue frontend/src/shared/ui/BaseButton.vue
mv frontend/src/components/ui/BaseInput.vue frontend/src/shared/ui/BaseInput.vue
mv frontend/src/components/ui/BaseModal.vue frontend/src/shared/ui/BaseModal.vue
mv frontend/src/services/http.ts frontend/src/shared/api/http.ts
mv frontend/src/services/interceptors.ts frontend/src/shared/api/interceptors.ts
mv frontend/src/utils/formatDate.ts frontend/src/shared/lib/formatDate.ts
mv frontend/src/types/api.types.ts frontend/src/shared/api/api.types.ts
```

- [ ] **Step 2: Move app bootstrap files**

Move these files:

```bash
mv frontend/src/App.vue frontend/src/app/App.vue
```

Keep `frontend/src/main.ts` in place for Vite entry, but update it to import the app root from `./app/App.vue`.

- [ ] **Step 3: Update imports for shared files**

Adjust imports in moved consumers, especially:
- `frontend/src/main.ts`
- `frontend/src/features/auth/api.ts`
- `frontend/src/features/auth/composables/useAuth.ts`
- any page using `BaseButton`, `BaseInput`, `BaseModal`, or `formatDate`

Expected import direction after this step:

```ts
import App from './app/App.vue';
import { httpJson, ApiError } from '../../shared/api/http';
import { setAccessTokenProvider } from '../../shared/api/interceptors';
import BaseButton from '../../shared/ui/BaseButton.vue';
import { formatDate } from '../../shared/lib/formatDate';
```

- [ ] **Step 4: Run frontend build**

Run: `npm run build`

Expected: Vite production build succeeds with no TypeScript import errors.

### Task 2: Rename dashboard feature to agents and move the page into the feature

**Files:**
- Create: `frontend/src/features/agents/pages/`
- Modify: `frontend/src/features/agents/*`
- Remove/move from: `frontend/src/features/dashboard/*`
- Remove/move from: `frontend/src/views/HomeView.vue`

- [ ] **Step 1: Move dashboard feature to agents**

Move these files:

```bash
mkdir -p frontend/src/features/agents/pages
mv frontend/src/features/dashboard/api.ts frontend/src/features/agents/api.ts
mv frontend/src/features/dashboard/index.ts frontend/src/features/agents/index.ts
mv frontend/src/features/dashboard/store.ts frontend/src/features/agents/store.ts
mv frontend/src/views/HomeView.vue frontend/src/features/agents/pages/AgentCatalogPage.vue
```

- [ ] **Step 2: Update feature imports**

Replace old dashboard imports with agent-feature imports. The app root should render:

```vue
<script setup lang="ts">
import AgentCatalogPage from '../features/agents/pages/AgentCatalogPage.vue';
</script>
```

The feature barrel should export agent feature APIs:

```ts
export { createInternalAgent, getInternalAgents, getTenantAgents, getTenants } from './api';
export type { AgentSummary, CreateAgentPayload, TenantSummary } from './api';
```

- [ ] **Step 3: Remove empty dashboard/view folders if no longer used**

After imports are fixed, remove now-unused directories if empty:

```bash
rmdir frontend/src/features/dashboard 2>/dev/null || true
rmdir frontend/src/views 2>/dev/null || true
```

- [ ] **Step 4: Run frontend build again**

Run: `npm run build`

Expected: build still succeeds after the feature rename.

### Task 3: Move backend API files into feature folders

**Files:**
- Create: `backend/Demo.Api/Features/Auth/`
- Create: `backend/Demo.Api/Features/Agents/`
- Create: `backend/Demo.Api/Features/Tenants/`
- Create: `backend/Demo.Api/Authorization/`
- Modify: moved files’ namespaces and import sites

- [ ] **Step 1: Create feature folders and move API files**

Move these files:

```bash
mkdir -p backend/Demo.Api/Features/Auth backend/Demo.Api/Features/Agents backend/Demo.Api/Features/Tenants backend/Demo.Api/Authorization
mv backend/Demo.Api/Controllers/AuthController.cs backend/Demo.Api/Features/Auth/AuthController.cs
mv backend/Demo.Api/Controllers/AgentsController.cs backend/Demo.Api/Features/Agents/AgentsController.cs
mv backend/Demo.Api/Controllers/AdminAgentsController.cs backend/Demo.Api/Features/Agents/AdminAgentsController.cs
mv backend/Demo.Api/Controllers/TenantsController.cs backend/Demo.Api/Features/Tenants/TenantsController.cs
mv backend/Demo.Api/Requests/CreateAgentRequest.cs backend/Demo.Api/Features/Agents/CreateAgentRequest.cs
mv backend/Demo.Api/Requests/CreateTenantRequest.cs backend/Demo.Api/Features/Tenants/CreateTenantRequest.cs
mv backend/Demo.Api/Responses/ApiErrorResponse.cs backend/Demo.Api/Features/Auth/ApiErrorResponse.cs
mv backend/Demo.Api/Responses/RefreshTokenCookie.cs backend/Demo.Api/Features/Auth/RefreshTokenCookie.cs
mv backend/Demo.Api/Middlewares/Authorization/HasPermissionAttribute.cs backend/Demo.Api/Authorization/HasPermissionAttribute.cs
mv backend/Demo.Api/Middlewares/Authorization/PermissionAuthorizationHandler.cs backend/Demo.Api/Authorization/PermissionAuthorizationHandler.cs
mv backend/Demo.Api/Middlewares/Authorization/PermissionPolicyProvider.cs backend/Demo.Api/Authorization/PermissionPolicyProvider.cs
mv backend/Demo.Api/Middlewares/Authorization/PermissionRequirement.cs backend/Demo.Api/Authorization/PermissionRequirement.cs
mv backend/Demo.Api/Middlewares/Authorization/TenantContextResolver.cs backend/Demo.Api/Authorization/TenantContextResolver.cs
```

- [ ] **Step 2: Update namespaces to match new API paths**

Expected namespaces:

```csharp
namespace Demo.Api.Features.Auth;
namespace Demo.Api.Features.Agents;
namespace Demo.Api.Features.Tenants;
namespace Demo.Api.Authorization;
```

Update using statements in controllers and `Program.cs` accordingly.

- [ ] **Step 3: Remove now-empty API technical buckets if possible**

```bash
rmdir backend/Demo.Api/Controllers 2>/dev/null || true
rmdir backend/Demo.Api/Requests 2>/dev/null || true
rmdir backend/Demo.Api/Responses 2>/dev/null || true
rmdir backend/Demo.Api/Middlewares/Authorization 2>/dev/null || true
rmdir backend/Demo.Api/Middlewares 2>/dev/null || true
```

- [ ] **Step 4: Run backend build**

Run: `dotnet build backend/Demo.Api/Demo.Api.csproj`

Expected: API project compiles or reports only the already-known local SDK/environment issue, not namespace/file resolution errors.

### Task 4: Move backend application contracts into feature folders

**Files:**
- Create: `backend/Demo.Application/Features/Auth/`
- Create: `backend/Demo.Application/Features/Agents/`
- Create: `backend/Demo.Application/Features/Tenants/`
- Modify: interfaces and DTO namespaces plus their consumers

- [ ] **Step 1: Move auth application contracts**

Move these files:

```bash
mkdir -p backend/Demo.Application/Features/Auth backend/Demo.Application/Features/Agents backend/Demo.Application/Features/Tenants
mv backend/Demo.Application/DTOs/Auth/AuthDtos.cs backend/Demo.Application/Features/Auth/AuthDtos.cs
mv backend/Demo.Application/Services/IAuthService.cs backend/Demo.Application/Features/Auth/IAuthService.cs
mv backend/Demo.Application/Services/IAuthSessionValidator.cs backend/Demo.Application/Features/Auth/IAuthSessionValidator.cs
mv backend/Demo.Application/Services/IJwtTokenService.cs backend/Demo.Application/Features/Auth/IJwtTokenService.cs
mv backend/Demo.Application/Services/IPasswordHasher.cs backend/Demo.Application/Features/Auth/IPasswordHasher.cs
mv backend/Demo.Application/Services/IRefreshTokenHasher.cs backend/Demo.Application/Features/Auth/IRefreshTokenHasher.cs
```

- [ ] **Step 2: Keep tenant/agent shared authorization contracts in place for this slice**

Do not force-move `PermissionCodes` and `IPermissionService` yet unless import cleanup stays simple. This slice should prefer low-risk moves around Auth first, while API feature folders for Agents and Tenants are already introduced.

- [ ] **Step 3: Update namespaces and all consumers**

Expected namespaces:

```csharp
namespace Demo.Application.Features.Auth;
```

Update imports in:
- `backend/Demo.Api/Features/Auth/AuthController.cs`
- `backend/Demo.Api/Program.cs`
- `backend/Demo.Infrastructure/Auth/*`
- any other references found by search

- [ ] **Step 4: Run solution build**

Run: `dotnet build backend/Demo.sln`

Expected: no missing namespace/type errors; if the local SDK issue still blocks build, record that separately.

### Task 5: Final verification and cleanup

**Files:**
- Modify: any remaining broken imports
- Verify: frontend and backend entry points

- [ ] **Step 1: Search for stale old paths**

Run:

```bash
rg -n "features/dashboard|components/ui|services/http|services/interceptors|utils/formatDate|Controllers|Requests|Responses|Middlewares/Authorization|Demo\\.Application\\.DTOs\\.Auth|Demo\\.Application\\.Services" frontend backend
```

Expected: no stale imports that should have moved in this slice.

- [ ] **Step 2: Run frontend build one last time**

Run: `npm run build`

Expected: PASS

- [ ] **Step 3: Run backend build one last time**

Run: `dotnet build backend/Demo.Api/Demo.Api.csproj`

Expected: compile success or only the known environment-level SDK blocker, not code-structure failures.

- [ ] **Step 4: Commit**

```bash
git add frontend/src backend/Demo.Api backend/Demo.Application docs/superpowers/plans/2026-06-12-folder-structure-migration-plan.md
git commit -m "refactor: organize app by feature-first structure"
```

