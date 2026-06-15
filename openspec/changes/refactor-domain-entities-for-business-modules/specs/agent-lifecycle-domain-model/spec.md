## ADDED Requirements

### Requirement: Agent entity stores required business identity and lifecycle fields
The system SHALL represent each agent with a domain entity that stores agent identifier, agent code, agent name, description, lifecycle status, tenant context, creator context, and creation/update timestamps.

#### Scenario: Agent is created with required business fields
- **WHEN** the system creates a new agent record
- **THEN** the agent stores `AgentId`, `AgentCode`, `AgentName`, `Status`, `CreatedBy`, `CreatedDate`, and tenant ownership metadata

#### Scenario: Agent description remains optional
- **WHEN** an agent is created or updated without a description
- **THEN** the agent remains valid as long as required identity and ownership fields are present

### Requirement: Agent lifecycle status supports the required business states
The system SHALL support agent lifecycle states `Draft`, `Active`, `Inactive`, `Deleted`, and `Publish` as explicit domain status values.

#### Scenario: New agent starts as draft
- **WHEN** a staff or manager creates a new agent
- **THEN** the agent status is initialized as `Draft` unless an authorized workflow changes it

#### Scenario: Deleted agent remains distinguishable from inactive agent
- **WHEN** an agent is removed from normal business use
- **THEN** the system can mark it as `Deleted` without conflating that state with `Inactive`

### Requirement: Agent ownership supports tenant-wide and creator-based permissions
The system SHALL model both tenant ownership and creator ownership for agents so tenant managers and staff permissions can be enforced separately.

#### Scenario: Staff-owned agent can be identified by creator
- **WHEN** a staff user creates an agent within a tenant
- **THEN** the agent stores the creator user identifier for later creator-scoped permission checks

#### Scenario: Tenant manager can enumerate tenant agents regardless of creator
- **WHEN** the system queries agents for a tenant manager within a tenant
- **THEN** the entity model provides the tenant association needed to retrieve all agents in that tenant

### Requirement: Agent queries support search, filter, and paging metadata
The system SHALL provide an agent model and related query contracts that support search by name or code, filtering by status, and paginated retrieval within tenant or global scope.

#### Scenario: Search uses stable business identity fields
- **WHEN** a client searches for agents
- **THEN** the system can evaluate the search against agent code and agent name without depending on unrelated child entities

#### Scenario: Filter uses explicit status values
- **WHEN** a client filters agents by status
- **THEN** the system evaluates the filter against the explicit lifecycle status stored on the agent
