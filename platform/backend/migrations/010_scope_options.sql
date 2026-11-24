CREATE TABLE scope_options (
    tenant_id uuid NOT NULL REFERENCES tenants(id),
    scope text NOT NULL CHECK (scope = btrim(scope) AND char_length(scope) BETWEEN 1 AND 240),
    PRIMARY KEY (tenant_id, scope)
);

ALTER TABLE scope_options ENABLE ROW LEVEL SECURITY;
ALTER TABLE scope_options FORCE ROW LEVEL SECURITY;

CREATE POLICY scope_options_read ON scope_options FOR SELECT TO grc_runtime USING (
    tenant_id = grc_current_tenant_id()
    AND (
        grc_scope_allowed(scope, 'records.read') OR grc_scope_allowed(scope, 'reviews.read')
        OR grc_scope_allowed(scope, 'evidence.read') OR grc_scope_allowed(scope, 'exports.read')
    )
);
CREATE POLICY scope_options_create ON scope_options FOR INSERT TO grc_runtime WITH CHECK (
    tenant_id = grc_current_tenant_id() AND grc_scope_allowed(scope, 'records.write')
    AND grc_scope_allowed('', 'records.write')
);

GRANT SELECT, INSERT ON scope_options TO grc_runtime;
