## ADDED Requirements

### Requirement: Audit logs persist sensitive business actions as immutable entries
The system SHALL record login, agent changes, file operations, and permission changes as append-only audit log entries.

#### Scenario: Login action creates audit entry
- **WHEN** a user logs in successfully
- **THEN** the system creates an audit log entry describing the login action and actor

#### Scenario: File deletion creates audit entry
- **WHEN** a user deletes a knowledge file
- **THEN** the system creates a separate audit log entry instead of overwriting prior history on the file entity

### Requirement: Audit log entries store actor and request context
The system SHALL store action name, user name or actor identity, created date, IP address, and descriptive content for each audit log entry.

#### Scenario: Audit log includes request metadata
- **WHEN** an auditable action occurs through an API request
- **THEN** the resulting audit log entry stores the action code, created timestamp, and source IP address when available

#### Scenario: Audit log can explain the action in human-readable terms
- **WHEN** an audit log entry is generated
- **THEN** it includes descriptive text that helps operators understand what changed

### Requirement: Audit logs support tenant and target-entity tracing
The system SHALL allow audit entries to reference tenant context and target business records so future UI or reports can filter logs accurately.

#### Scenario: Agent update log references the affected agent
- **WHEN** an agent is created, updated, published, or deleted
- **THEN** the audit log entry can reference the affected agent identifier and its tenant context

#### Scenario: Permission-change log references the affected authorization scope
- **WHEN** a role or permission assignment changes
- **THEN** the audit log entry can store the related tenant and target authorization entity identifiers needed for filtering
