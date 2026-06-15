## Why

The backend already has working business logic, but its structure and conventions still need to be normalized so future changes are easier to read, review, and maintain. This refactor applies the attached C# / .NET naming, formatting, and error-handling rules without changing intended behavior.

## What Changes

- Standardize backend naming to match the convention for namespaces, types, methods, properties, parameters, locals, and fields.
- Normalize code formatting to Visual Studio style, including brace placement, one declaration per line, and consistent spacing.
- Align private and internal instance fields to `_camelCase`.
- Review controllers, services, handlers, and utility classes for consistent error handling and replace empty catch blocks with explicit handling or logging.
- Add or adjust `#region` grouping only where it improves readability and matches the prescribed order.
- Preserve public behavior and API contracts unless a change is required to fix an obvious bug.
- Leave the frontend out of scope for this change because the supplied convention is C# / .NET specific.

## Capabilities

### New Capabilities
- `backend-coding-convention-alignment`: Backend codebase refactoring that standardizes C# naming, formatting, structure, and error handling while keeping behavior equivalent.

### Modified Capabilities
- None.

## Impact

- Affected projects: `backend/Demo.Api`, `backend/Demo.Application`, `backend/Demo.Domain`, and `backend/Demo.Infrastructure`.
- Affected code areas: class and method naming, field naming, parameter and local variable naming, formatting, regions, and exception handling patterns.
- Expected behavioral impact: none, except for any narrowly scoped fixes needed to preserve correctness or replace unsafe error handling.
- Build/test impact: existing backend tests and solution build should continue to pass after the refactor.
