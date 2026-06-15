## 1. Backend Foundation

- [x] 1.1 Move the backend solution and project files toward the `src/Domain`, `src/Application`, `src/Infrastructure`, and `src/API` layout and update project references accordingly.
- [x] 1.2 Align backend namespaces and folder ownership so Domain, Application, Infrastructure, and API code live in the correct layer boundaries.

## 2. Backend Validation

- [x] 2.1 Update backend references and imports so the solution builds cleanly after the move without changing runtime behavior.
- [x] 2.2 Verify the backend dependency direction still follows Clean Architecture rules after the structural refactor.

## 3. Frontend Foundation

- [x] 3.1 Reorganize frontend source files into the requested `src/api`, `src/views`, `src/components`, `src/layouts`, `src/stores`, `src/router`, `src/utils`, and `src/composables` folders.
- [x] 3.2 Update frontend imports, router registrations, and layout/component references so the app still starts with the new structure.

## 4. End-to-End Verification

- [x] 4.1 Build the backend solution and run the frontend build or dev-check path to confirm the refactor is behaviorally equivalent.
- [x] 4.2 Perform a final cleanup pass for stale paths, dead folders, and leftover references from the old structure.
