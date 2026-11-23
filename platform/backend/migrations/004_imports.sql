CREATE TABLE catalog_imports (
    tenant_id uuid NOT NULL REFERENCES tenants (id) ON DELETE RESTRICT,
    actor_id uuid NOT NULL,
    key text NOT NULL CHECK (length(key) BETWEEN 8 AND 128),
    fingerprint text NOT NULL CHECK (length(fingerprint) = 64),
    result jsonb NOT NULL,
    created_at timestamptz NOT NULL DEFAULT now(),
    PRIMARY KEY (tenant_id, actor_id, key),
    FOREIGN KEY (tenant_id, actor_id) REFERENCES members (tenant_id, id)
);

ALTER TABLE catalog_imports ENABLE ROW LEVEL SECURITY;
ALTER TABLE catalog_imports FORCE ROW LEVEL SECURITY;
CREATE POLICY catalog_imports_tenant ON catalog_imports
    USING (tenant_id = grc_current_tenant_id() AND actor_id = grc_current_user_id())
    WITH CHECK (tenant_id = grc_current_tenant_id() AND actor_id = grc_current_user_id());
GRANT SELECT, INSERT ON catalog_imports TO grc_runtime;
