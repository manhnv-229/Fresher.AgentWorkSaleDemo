ALTER TABLE agents
    MODIFY COLUMN tenant_id CHAR(36) NULL,
    ADD COLUMN scope VARCHAR(50) NOT NULL DEFAULT 'Tenant' AFTER icon;

UPDATE agents
SET scope = 'Tenant'
WHERE scope IS NULL OR scope = '';

CREATE INDEX idx_agents_scope ON agents(scope);
