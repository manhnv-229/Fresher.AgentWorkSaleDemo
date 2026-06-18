## 1. Style Foundation

- [x] 1.1 Add or update repo-level C# style configuration so Visual Studio and VS Code apply the backend formatting rules consistently.
- [x] 1.2 Establish the initial formatting baseline for the backend solution so later diffs are limited to convention cleanup and intentional edits.

## 2. Backend Refactor

- [x] 2.1 Refactor `Demo.Api` naming, private fields, and explicit error handling so controllers and auth boundary code follow the convention.
- [x] 2.2 Refactor `Demo.Application` identifiers, helper types, and local naming to match the convention without changing use-case behavior.
- [x] 2.3 Refactor `Demo.Domain` and `Demo.Infrastructure` class members, fields, and supporting helpers to match the convention and keep public contracts stable.
- [x] 2.4 Review and adjust `#region` grouping only where it improves readability and follows the required order.

## 3. Verification

- [x] 3.1 Build the backend solution and run the existing automated tests to confirm the refactor is behaviorally equivalent.
- [x] 3.2 Perform a final pass for any remaining convention violations, empty catch blocks, or public API changes that were not intended.
