## MODIFIED Requirements

### Requirement: Audit logs persist sensitive business actions as immutable entries
The system SHALL record login, agent create/update/delete actions, file upload/delete actions, and permission changes as append-only audit log entries.

#### Scenario: Login action creates audit entry
- **WHEN** a user logs in successfully
- **THEN** the system creates an audit log entry describing the login action and actor

#### Scenario: Agent lifecycle actions create audit entries
- **WHEN** a user creates, updates, or deletes an agent
- **THEN** the system creates a separate audit log entry for each completed action instead of overwriting prior history on the agent

#### Scenario: File lifecycle actions create audit entries
- **WHEN** a user uploads or deletes a knowledge file
- **THEN** the system creates a separate audit log entry for each completed file action instead of relying on mutable file metadata alone

#### Scenario: Permission changes create audit entries
- **WHEN** a user changes a role assignment or permission mapping
- **THEN** the system creates an audit log entry describing the permission change and affected authorization scope

### Requirement: Audit log entries store actor and request context
The system SHALL store action name, user name or actor identity, created date, IP address, and descriptive content for each audit log entry.

#### Scenario: Audit log includes request metadata
- **WHEN** an auditable action occurs through an API request
- **THEN** the resulting audit log entry stores the action code, created timestamp, and source IP address when available

#### Scenario: Audit log includes operator identity
- **WHEN** an audit log entry is generated for an authenticated action
- **THEN** the entry stores the acting user's identity so the UI can render the `UserName` field

#### Scenario: Audit log can explain the action in human-readable terms
- **WHEN** an audit log entry is generated
- **THEN** it includes descriptive text that helps operators understand what changed
