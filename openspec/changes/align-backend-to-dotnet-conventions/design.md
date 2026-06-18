## Context

The repository contains a four-project backend solution (`Demo.Api`, `Demo.Application`, `Demo.Domain`, and `Demo.Infrastructure`) that already works functionally, but its source is not yet normalized around the attached C# / .NET coding convention. This change is a cross-cutting refactor with no intended feature work.

The convention emphasizes PascalCase for public members, `_camelCase` for private/internal instance fields, camelCase for parameters and locals, vertically aligned braces, one declaration per line, and explicit exception handling. The backend also does not currently have a repo-level `.editorconfig`, so style consistency depends mostly on manual discipline today.

## Goals / Non-Goals

**Goals:**

- Normalize backend naming, formatting, and error-handling patterns to match the supplied convention.
- Keep public behavior, API contracts, and business logic unchanged.
- Make future edits easier to keep consistent by capturing repeatable formatting rules in repository config where practical.
- Refactor in a way that is safe to review, compile, and validate incrementally.

**Non-Goals:**

- Changing business rules, API payloads, route shapes, or persistence behavior.
- Refactoring the Vue frontend or applying the C# convention to non-.NET code.
- Renaming public APIs purely for style if doing so would create avoidable breaking changes.
- Introducing new architecture, domain concepts, or external dependencies unrelated to style alignment.

## Decisions

1. **Apply the refactor in backend slices rather than as one giant rename.**

   The work will be grouped by project and responsibility boundary, starting with `Demo.Api`, then `Demo.Application`, `Demo.Domain`, and `Demo.Infrastructure`. Each slice should compile before the next one begins.

   Alternative considered: one sweeping rename across the entire solution. That is rejected because it would create a noisy diff, make regressions harder to isolate, and increase the chance of missing a behavioral change.

2. **Keep public contracts stable unless a change is clearly safe and internal.**

   Public types and externally visible members will be left alone unless they already violate the convention in a way that can be changed without risking consumers. Internal helpers, private fields, local variables, and temporary names are free to be normalized aggressively.

   Alternative considered: rename every symbol to fully match the convention, including public APIs. That is rejected because the convention itself says not to break external callers unless explicitly requested.

3. **Use repository style settings for formatting, not a new tooling stack.**

   A repo-level `.editorconfig` is the right place to encode the repeatable C# formatting rules that Visual Studio Code and Visual Studio can both honor. This keeps the change lightweight and avoids introducing a new analyzer dependency just to enforce whitespace and brace placement.

   Alternative considered: add a separate Roslyn analyzer package or custom code-fix tooling. That is rejected for now because it adds setup overhead without materially improving the first pass of the refactor.

4. **Treat error handling as a targeted cleanup rather than a blanket rewrite.**

   Empty catch blocks will be removed, and the surrounding code will be updated to log or explicitly handle errors in places where the convention requires it, especially controller boundaries, auth/session validation, and other guarded execution paths.

   Alternative considered: wrap every method in generic try/catch blocks. That is rejected because it would hide failures and add noise without improving the codebase.

5. **Use verification as a gate after each slice.**

   The refactor should be validated with the backend build and existing tests after each major step, not only at the end. That gives faster feedback if a naming change or formatting sweep accidentally breaks a dependency edge.

   Alternative considered: postpone verification until the very end. That is rejected because it makes it harder to trace which slice introduced a problem.

## Risks / Trade-offs

- **Large rename churn can obscure real changes** -> Keep each slice narrow and reviewable, and verify after each project-level pass.
- **Formatting changes can create visually large diffs** -> Use a single style source and let automated formatting handle mechanical whitespace changes where possible.
- **Some conventions are hard to enforce automatically** -> Handle naming and error-handling changes manually in the touched code, and use code review to catch exceptions.
- **Preserving public APIs can limit how far the refactor goes** -> Accept that some legacy names may remain until a later, explicit breaking-change request.
- **A style sweep can accidentally touch unrelated logic** -> Favor minimal edits around each convention violation and keep behavior checks in place.

## Migration Plan

1. Add or update repo-level C# style configuration so formatting expectations are explicit.
2. Refactor `Demo.Api` first, because it is the public boundary and makes naming and error-handling conventions visible immediately.
3. Refactor `Demo.Application`, `Demo.Domain`, and `Demo.Infrastructure` in dependency order, keeping each project buildable as the work progresses.
4. Replace empty catch blocks with explicit handling or logging where the convention requires it.
5. Normalize naming and formatting in the touched files, keeping public contracts stable.
6. Run the backend build and existing automated tests to confirm the refactor stayed behaviorally equivalent.
7. Do a final review pass for any missed identifiers, inconsistent braces, or ad hoc exception handling.

Rollback is straightforward because this change does not introduce a migration or schema change: revert the last refactor slice, or revert the repo-level style file if it causes unexpected formatting churn.

## Open Questions

None. The scope is intentionally limited to backend .NET code, and the convention already gives enough direction to proceed without further product clarification.
