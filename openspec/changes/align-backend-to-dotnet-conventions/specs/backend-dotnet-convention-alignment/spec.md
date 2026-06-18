## ADDED Requirements

### Requirement: Backend identifiers conform to C# naming rules
The backend SHALL use English identifiers and the C# naming convention from the attached standard for namespaces, types, methods, properties, events, parameters, locals, and fields.

#### Scenario: New or modified source files follow casing
- **WHEN** a backend source file is added or changed
- **THEN** namespaces, classes, interfaces, enums, methods, properties, and public/static/constant fields use PascalCase
- **AND** parameters and local variables use camelCase
- **AND** private and internal instance fields use `_camelCase`
- **AND** generic type parameters use `T` prefixes

### Requirement: Backend formatting matches Visual Studio style
The backend SHALL use vertically aligned braces, one declaration or statement per line, and blank lines that improve readability.

#### Scenario: Formatted file stays readable
- **WHEN** a backend file is formatted or edited
- **THEN** braces remain vertically aligned
- **AND** declarations are kept on separate lines
- **AND** complex expressions use parentheses to make intent explicit

### Requirement: Backend error handling is explicit and non-empty
The backend SHALL not contain empty catch blocks and SHALL handle or log exceptions in event handlers, multithreaded code, and other explicitly guarded sections.

#### Scenario: Exception handling is visible
- **WHEN** a backend catch block is introduced
- **THEN** it handles the error or logs it explicitly
- **AND** it does not swallow exceptions silently

#### Scenario: Guarded code logs failures
- **WHEN** an event handler or background task fails
- **THEN** the failure is surfaced through an explicit handler or logger
- **AND** the failure does not disappear in an empty catch block

### Requirement: Behavioral contracts remain unchanged
The backend SHALL preserve existing public behavior, API contracts, and business logic unless a narrowly scoped fix is required to correct an obvious bug.

#### Scenario: Existing endpoints remain compatible
- **WHEN** the refactor is applied
- **THEN** existing API routes and response shapes remain compatible
- **AND** no new runtime behavior is introduced solely for style changes

### Requirement: Backend solution remains buildable
The backend SHALL remain compilable and keep the existing automated tests passing after the refactor.

#### Scenario: Verification succeeds after refactor
- **WHEN** the backend solution is built and tested
- **THEN** compilation succeeds
- **AND** the existing automated tests pass
