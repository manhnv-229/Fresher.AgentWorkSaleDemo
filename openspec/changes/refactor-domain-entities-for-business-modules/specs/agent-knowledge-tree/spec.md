## ADDED Requirements

### Requirement: Agent knowledge is organized as a hierarchical folder tree
The system SHALL represent agent knowledge folders with parent-child relationships so users can create root folders, nested folders, and move folders within the same agent knowledge space.

#### Scenario: Root folder is created under an agent
- **WHEN** a user creates a top-level folder for an agent
- **THEN** the folder record is stored for that agent without a parent folder reference

#### Scenario: Child folder references parent folder
- **WHEN** a user creates a nested folder
- **THEN** the folder record stores the parent folder identifier needed to reconstruct the tree

### Requirement: Agent knowledge files store document metadata independently from storage implementation
The system SHALL represent uploaded agent documents with metadata that includes file identity, display name, file type, folder placement, creator, and creation/update timestamps.

#### Scenario: Supported file metadata is stored on upload
- **WHEN** a user uploads a supported file such as PDF, DOCX, XLSX, PPTX, TXT, PNG, or JPG
- **THEN** the knowledge file entity stores enough metadata to identify the file type, owning agent, folder location, creator, and created date

#### Scenario: File rename does not require storage-path semantics in the entity model
- **WHEN** a user renames a file
- **THEN** the entity updates the logical file metadata without requiring folder structure to be encoded in a single path string

### Requirement: Knowledge entities support move, delete, and lookup behavior
The system SHALL model folders and files so they can be renamed, moved, deleted, and queried by name, folder, creator, and created date.

#### Scenario: File move updates folder association
- **WHEN** a user moves a file to another folder within the same agent
- **THEN** the file metadata updates its folder reference while preserving file identity and auditability

#### Scenario: Folder search uses structured metadata
- **WHEN** a client searches knowledge data by folder or creator
- **THEN** the system can evaluate the query from entity relationships and metadata rather than parsing display paths

### Requirement: Knowledge data remains scoped to an agent and tenant context
The system SHALL keep folder and file entities anchored to the owning agent and its tenant context so documents cannot drift across tenant boundaries accidentally.

#### Scenario: File belongs to one agent knowledge space
- **WHEN** a knowledge file is stored
- **THEN** it references one owning agent and cannot be shared implicitly with another agent

#### Scenario: Tenant-scoped agent knowledge stays tenant-scoped
- **WHEN** an agent belongs to a tenant
- **THEN** its knowledge folders and files inherit that tenant boundary through the owning agent relationship
