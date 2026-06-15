## ADDED Requirements

### Requirement: Backend layers must have clear ownership
The backend SHALL separate code into Domain, Application, Infrastructure, and API layers, and each layer SHALL contain only the responsibilities assigned to it by the architecture convention.

#### Scenario: Domain keeps core model and rules
- **WHEN** a developer adds or modifies domain behavior
- **THEN** entities, enums, business rules, and domain interfaces remain in the Domain layer
- **AND** the Domain layer does not contain database, HTTP, or UI concerns

#### Scenario: Application keeps use-case orchestration
- **WHEN** a developer adds or modifies application behavior
- **THEN** DTOs, services, CQRS handlers, validation, and use-case logic remain in the Application layer
- **AND** the Application layer does not contain HTTP controllers or database implementations

#### Scenario: Infrastructure keeps technical implementations
- **WHEN** a developer adds or modifies persistence or integration code
- **THEN** repositories, database access, logging, file storage, and external service implementations remain in Infrastructure
- **AND** Infrastructure does not own API endpoints or domain rules

#### Scenario: API keeps delivery concerns
- **WHEN** a developer adds or modifies HTTP delivery code
- **THEN** controllers, middleware, authentication, authorization, and Swagger configuration remain in API
- **AND** the API layer does not contain repository implementations or domain entities

### Requirement: Backend dependency direction must remain inward
The backend SHALL keep dependencies flowing from API to Application and Infrastructure, from Application to Domain, and from Infrastructure to Application and Domain only.

#### Scenario: Domain stays independent
- **WHEN** the backend solution is built
- **THEN** the Domain project does not reference Application, Infrastructure, or API assemblies

#### Scenario: Application depends only on core abstractions
- **WHEN** the backend solution is built
- **THEN** the Application project depends on Domain and application-facing abstractions only
- **AND** it does not depend directly on API delivery concerns

#### Scenario: Infrastructure plugs into core contracts
- **WHEN** the backend solution is built
- **THEN** the Infrastructure project implements the contracts defined in the core layers
- **AND** it does not become a dependency source for the Domain project

### Requirement: Backend project layout must follow the source tree convention
The backend SHALL organize its source under `src/Domain`, `src/Application`, `src/Infrastructure`, and `src/API` so the folder structure matches the architectural boundaries.

#### Scenario: New backend code lands in the correct layer
- **WHEN** a new backend type is created
- **THEN** its file path matches the layer that owns the behavior
- **AND** its namespace reflects the same layer boundary

#### Scenario: Existing project references stay valid after relocation
- **WHEN** backend projects are moved into the new source tree
- **THEN** the solution and project references still resolve successfully
- **AND** the application continues to build without changing runtime behavior

### Requirement: Backend behavior must remain unchanged during structural refactor
The backend SHALL preserve API contracts, data flow, and user-visible behavior while the folder and project structure is being reorganized.

#### Scenario: Existing endpoints keep working
- **WHEN** the backend is rebuilt after the move
- **THEN** the existing HTTP endpoints remain available
- **AND** their request and response shapes remain unchanged

#### Scenario: No new feature behavior is introduced
- **WHEN** the architecture refactor is complete
- **THEN** the only changes are source organization and dependency boundaries
- **AND** no business rule changes are introduced solely because files were moved
