## MODIFIED Requirements

### Requirement: Admin users can open agent details within the active scope
The system SHALL let authorized users open a dedicated detail page for an internal agent or a tenant-scoped agent without crossing scope boundaries.

#### Scenario: Admin opens internal agent details from the list
- **WHEN** an authorized admin clicks an internal agent card or chooses `Xem chi tiết` from that card's action menu
- **THEN** the system opens a dedicated detail page for that internal agent
- **AND** the page loads the internal agent detail payload for that id only

#### Scenario: Admin opens tenant agent details from the list
- **WHEN** an authorized user clicks a tenant agent card or chooses `Xem chi tiết` from that card's action menu while a tenant is active
- **THEN** the system opens a dedicated detail page for that tenant-scoped agent
- **AND** the page loads the tenant-scoped detail only when the agent belongs to the active tenant

#### Scenario: Scope mismatch is rejected
- **WHEN** a client requests an internal-agent detail from a tenant route or requests a tenant agent from another tenant route
- **THEN** the system denies the request with a not-found style result that does not expose cross-scope data

### Requirement: Admin users can update agent metadata within scope
The system SHALL let authorized users update an agent's editable metadata from the dedicated detail page while preserving its scope and tenant ownership.

#### Scenario: Admin saves internal agent edits from detail page
- **WHEN** an authorized admin edits an internal agent on the dedicated detail page and submits a valid save
- **THEN** the system saves the updated name, role, description, icon, and status
- **AND** the agent remains internal-scoped with no tenant assignment

#### Scenario: Admin saves tenant agent edits from detail page
- **WHEN** an authorized user edits a tenant agent on the dedicated detail page and submits a valid save
- **THEN** the system updates only that tenant's agent
- **AND** the agent remains associated with the same tenant

#### Scenario: Invalid update is rejected
- **WHEN** a client submits an update with missing required fields or an unsupported status value
- **THEN** the system rejects the request with a validation error

### Requirement: Agent delete operations are soft-deleted and scope-safe
The system SHALL support deleting agents through scope-specific routes by marking them deleted instead of removing related data immediately, including delete actions launched from an agent card menu.

#### Scenario: Internal agent is soft-deleted from card action
- **WHEN** an authorized admin chooses `Xóa` for an internal agent from the card action menu and confirms the action
- **THEN** the system marks the agent with deleted lifecycle data
- **AND** the agent is excluded from normal internal-agent list results

#### Scenario: Tenant agent is soft-deleted from card or detail action
- **WHEN** an authorized user deletes a tenant agent from the card action menu or from the dedicated detail page
- **THEN** the system marks only that tenant's agent as deleted
- **AND** the deleted agent no longer appears in the selected tenant's normal list results

#### Scenario: Deleted agent cannot be managed from another scope
- **WHEN** a client attempts to delete an agent through a route that does not match its scope or tenant
- **THEN** the system returns a not-found style result and does not mutate the agent

### Requirement: Agent management UI keeps tenant context after mutations
The system SHALL preserve the current internal-or-tenant context after view, save, or delete actions launched from the list or dedicated detail page.

#### Scenario: Returning from detail restores list context
- **WHEN** a user leaves an agent detail page and returns to the list
- **THEN** the workspace restores the active scope, selected tenant, filters, and pagination state from before navigation

#### Scenario: Tenant list refreshes after save or delete
- **WHEN** a user saves or deletes an agent for the selected tenant from the detail page or card menu
- **THEN** the UI refreshes that tenant's current result page using the active filters
- **AND** the selected tenant remains active in the workspace

#### Scenario: Card action menu does not trigger unintended navigation
- **WHEN** a user clicks `Xem chi tiết`, `Sửa`, or `Xóa` from the top-right action menu on an agent card
- **THEN** the system performs only the chosen action
- **AND** it does not also trigger the card's default click behavior unintentionally
