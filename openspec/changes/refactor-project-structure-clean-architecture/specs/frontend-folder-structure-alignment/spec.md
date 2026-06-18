## ADDED Requirements

### Requirement: Frontend source tree must follow the requested top-level folders
The frontend SHALL organize source files under `src/api`, `src/views`, `src/components`, `src/layouts`, `src/stores`, `src/router`, `src/utils`, and `src/composables`.

#### Scenario: New frontend code lands in a matching folder
- **WHEN** a developer adds a new frontend file
- **THEN** the file is placed in the top-level folder that matches its responsibility
- **AND** the file path stays consistent with the source tree convention

#### Scenario: Existing application code is relocated into the new tree
- **WHEN** the frontend structure is refactored
- **THEN** existing views, reusable UI, routing, stores, utilities, and composables are moved into the requested folders
- **AND** the application still starts successfully

### Requirement: Frontend reusable UI and layout concerns must be separated
The frontend SHALL keep reusable UI components in `components` and shell or structure-level UI in `layouts`.

#### Scenario: Layout chrome is isolated
- **WHEN** the application shell is assembled
- **THEN** header, sidebar, footer, and other app chrome live in `layouts`
- **AND** they are not mixed into page-specific view code

#### Scenario: Reusable controls stay generic
- **WHEN** the frontend renders common controls such as buttons, tables, modals, or inputs
- **THEN** those controls remain in `components`
- **AND** they can be reused by multiple views without feature-specific coupling

### Requirement: Frontend import paths and navigation must remain functional after relocation
The frontend SHALL update imports, router registrations, and store references to match the new folder structure while preserving user-visible behavior.

#### Scenario: Routes still resolve
- **WHEN** the app starts after files are moved
- **THEN** the router still resolves the same pages
- **AND** navigation works without broken imports

#### Scenario: State and utility usage still works
- **WHEN** views and components consume shared stores, utilities, or composables
- **THEN** those imports resolve from the new locations
- **AND** the user experience remains unchanged

### Requirement: Frontend behavior must remain unchanged during structural refactor
The frontend SHALL preserve existing page behavior, component behavior, and API interactions while the folder structure is being reorganized.

#### Scenario: User flows stay intact
- **WHEN** the refactor is complete
- **THEN** login, navigation, and catalog flows behave the same as before

#### Scenario: Structural cleanup does not change UI semantics
- **WHEN** files are moved into the new source tree
- **THEN** the refactor changes organization only
- **AND** it does not introduce new feature behavior
