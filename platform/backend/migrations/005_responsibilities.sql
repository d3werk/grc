CREATE TABLE organization_people (
    tenant_id uuid NOT NULL REFERENCES tenants(id),
    id uuid NOT NULL DEFAULT gen_random_uuid(),
    display_name text NOT NULL CHECK (char_length(btrim(display_name)) BETWEEN 1 AND 240),
    email text NOT NULL DEFAULT '' CHECK (char_length(email) <= 254),
    scope text NOT NULL DEFAULT '' CHECK (scope = btrim(scope) AND char_length(scope) <= 240),
    active boolean NOT NULL DEFAULT true,
    version integer NOT NULL DEFAULT 1 CHECK (version > 0),
    created_at timestamptz NOT NULL DEFAULT clock_timestamp(),
    updated_at timestamptz NOT NULL DEFAULT clock_timestamp(),
    PRIMARY KEY (tenant_id, id)
);

CREATE TABLE organization_responsibilities (
    tenant_id uuid NOT NULL,
    id uuid NOT NULL DEFAULT gen_random_uuid(),
    person_id uuid NOT NULL,
    target_id uuid NOT NULL,
    target_kind text NOT NULL,
    scope text NOT NULL,
    role text NOT NULL,
    created_at timestamptz NOT NULL DEFAULT clock_timestamp(),
    revoked_at timestamptz,
    PRIMARY KEY (tenant_id, id),
    FOREIGN KEY (tenant_id, person_id) REFERENCES organization_people(tenant_id, id),
    FOREIGN KEY (tenant_id, target_id, target_kind, scope)
        REFERENCES catalog_records(tenant_id, id, kind, scope) ON UPDATE CASCADE,
    CHECK (
        (role IN ('management', 'it_lead', 'privacy_lead', 'security_lead') AND target_kind = 'org_unit') OR
        (role = 'asset_owner' AND target_kind IN ('asset', 'it_service')) OR
        (role = 'risk_owner' AND target_kind = 'risk') OR
        (role = 'control_owner' AND target_kind = 'control') OR
        (role = 'action_owner' AND target_kind = 'measure') OR
        (role = 'process_owner' AND target_kind IN ('business_process', 'processing_activity'))
    )
);

CREATE UNIQUE INDEX organization_responsibilities_live
    ON organization_responsibilities(tenant_id, person_id, target_id, role) WHERE revoked_at IS NULL;
CREATE INDEX organization_responsibilities_target
    ON organization_responsibilities(tenant_id, target_id);

ALTER TABLE organization_people ENABLE ROW LEVEL SECURITY;
ALTER TABLE organization_people FORCE ROW LEVEL SECURITY;
ALTER TABLE organization_responsibilities ENABLE ROW LEVEL SECURITY;
ALTER TABLE organization_responsibilities FORCE ROW LEVEL SECURITY;

CREATE POLICY organization_people_read ON organization_people FOR SELECT
    USING (tenant_id = grc_current_tenant_id() AND grc_scope_allowed(scope, 'records.read'));
CREATE POLICY organization_people_create ON organization_people FOR INSERT
    WITH CHECK (tenant_id = grc_current_tenant_id() AND grc_scope_allowed(scope, 'records.write'));
CREATE POLICY organization_people_edit ON organization_people FOR UPDATE
    USING (tenant_id = grc_current_tenant_id() AND grc_scope_allowed(scope, 'records.write'))
    WITH CHECK (tenant_id = grc_current_tenant_id() AND grc_scope_allowed(scope, 'records.write'));

CREATE POLICY organization_responsibilities_read ON organization_responsibilities FOR SELECT
    USING (tenant_id = grc_current_tenant_id() AND grc_scope_allowed(scope, 'records.read'));
CREATE POLICY organization_responsibilities_create ON organization_responsibilities FOR INSERT
    WITH CHECK (tenant_id = grc_current_tenant_id() AND grc_scope_allowed(scope, 'records.write'));
CREATE POLICY organization_responsibilities_revoke ON organization_responsibilities FOR UPDATE
    USING (tenant_id = grc_current_tenant_id() AND grc_scope_allowed(scope, 'records.write'))
    WITH CHECK (tenant_id = grc_current_tenant_id() AND grc_scope_allowed(scope, 'records.write') AND revoked_at IS NOT NULL);

GRANT SELECT, INSERT ON organization_people, organization_responsibilities TO grc_runtime;
GRANT UPDATE (display_name, email, active, version, updated_at) ON organization_people TO grc_runtime;
GRANT UPDATE (revoked_at) ON organization_responsibilities TO grc_runtime;
