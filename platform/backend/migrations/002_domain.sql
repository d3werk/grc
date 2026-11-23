CREATE TABLE catalog_records (
    tenant_id uuid NOT NULL REFERENCES tenants(id),
    id uuid NOT NULL DEFAULT gen_random_uuid(),
    kind text NOT NULL CHECK (kind IN (
        'org_unit', 'asset', 'it_service', 'business_process', 'risk', 'control', 'measure',
        'framework_version', 'requirement', 'processing_activity', 'supplier', 'dpia'
    )),
    code text NOT NULL DEFAULT '' CHECK (char_length(code) <= 80),
    title text NOT NULL CHECK (char_length(btrim(title)) BETWEEN 1 AND 300),
    description text NOT NULL DEFAULT '' CHECK (char_length(description) <= 30000),
    status text NOT NULL DEFAULT 'draft' CHECK (status IN ('draft', 'active', 'in_review', 'completed', 'archived')),
    owner text NOT NULL DEFAULT '' CHECK (char_length(owner) <= 240),
    scope text NOT NULL DEFAULT '' CHECK (scope = btrim(scope) AND char_length(scope) <= 240),
    org_unit_id uuid,
    org_unit_kind text GENERATED ALWAYS AS ('org_unit'::text) STORED,
    attributes jsonb NOT NULL DEFAULT '{}'::jsonb CHECK (jsonb_typeof(attributes) = 'object'),
    version integer NOT NULL DEFAULT 1 CHECK (version > 0),
    created_by uuid NOT NULL,
    created_at timestamptz NOT NULL DEFAULT clock_timestamp(),
    updated_at timestamptz NOT NULL DEFAULT clock_timestamp(),
    create_key text CHECK (char_length(create_key) BETWEEN 1 AND 128),
    create_hash text CHECK (char_length(create_hash) = 64),
    PRIMARY KEY (tenant_id, id),
    UNIQUE (tenant_id, id, kind),
    UNIQUE (tenant_id, id, kind, scope),
    UNIQUE (tenant_id, created_by, create_key),
    CHECK (org_unit_id IS DISTINCT FROM id),
    CHECK ((create_key IS NULL) = (create_hash IS NULL)),
    CHECK (kind <> 'dpia' OR attributes = '{}'::jsonb),
    FOREIGN KEY (tenant_id, org_unit_id, org_unit_kind) REFERENCES catalog_records(tenant_id, id, kind),
    FOREIGN KEY (tenant_id, created_by) REFERENCES members(tenant_id, id)
);

CREATE INDEX catalog_records_kind_title ON catalog_records(tenant_id, kind, lower(title), id);
CREATE INDEX catalog_records_scope ON catalog_records(tenant_id, scope);
CREATE INDEX catalog_records_org_unit ON catalog_records(tenant_id, org_unit_id) WHERE org_unit_id IS NOT NULL;
CREATE UNIQUE INDEX catalog_records_code ON catalog_records(tenant_id, kind, scope, lower(code)) WHERE code <> '';

CREATE TABLE catalog_dpias (
    tenant_id uuid NOT NULL,
    record_id uuid NOT NULL,
    record_kind text NOT NULL DEFAULT 'dpia' CHECK (record_kind = 'dpia'),
    scope text NOT NULL,
    screening_reason text NOT NULL DEFAULT '' CHECK (char_length(screening_reason) <= 5000),
    necessity text NOT NULL DEFAULT '' CHECK (char_length(necessity) <= 5000),
    risks_to_individuals text NOT NULL DEFAULT '' CHECK (char_length(risks_to_individuals) <= 5000),
    safeguards text NOT NULL DEFAULT '' CHECK (char_length(safeguards) <= 5000),
    review_result text NOT NULL DEFAULT '' CHECK (char_length(review_result) <= 5000),
    PRIMARY KEY (tenant_id, record_id),
    FOREIGN KEY (tenant_id, record_id, record_kind, scope)
        REFERENCES catalog_records(tenant_id, id, kind, scope) ON UPDATE CASCADE
);

CREATE TABLE catalog_link_types (
    kind text PRIMARY KEY,
    from_kind text NOT NULL,
    to_kind text NOT NULL,
    label text NOT NULL,
    UNIQUE (kind, from_kind, to_kind)
);

INSERT INTO catalog_link_types (kind, from_kind, to_kind, label) VALUES
    ('risk_asset', 'risk', 'asset', 'Risiko betrifft Informationswert'),
    ('risk_it_service', 'risk', 'it_service', 'Risiko betrifft IT-Dienst'),
    ('risk_process', 'risk', 'business_process', 'Risiko betrifft Geschäftsprozess'),
    ('risk_supplier', 'risk', 'supplier', 'Risiko betrifft Lieferanten'),
    ('risk_control', 'risk', 'control', 'Control behandelt Risiko'),
    ('control_asset', 'control', 'asset', 'Control schützt Informationswert'),
    ('requirement_control', 'requirement', 'control', 'Control deckt Anforderung ab'),
    ('requirement_framework_version', 'requirement', 'framework_version', 'Anforderung gehört zur Regelwerksfassung'),
    ('action_control', 'measure', 'control', 'Maßnahme setzt Control um'),
    ('action_risk', 'measure', 'risk', 'Maßnahme behandelt Risiko'),
    ('process_it_service', 'business_process', 'it_service', 'Geschäftsprozess nutzt IT-Dienst'),
    ('process_asset', 'business_process', 'asset', 'Geschäftsprozess nutzt Informationswert'),
    ('it_service_asset', 'it_service', 'asset', 'IT-Dienst nutzt Informationswert'),
    ('it_service_supplier', 'it_service', 'supplier', 'Lieferant unterstützt IT-Dienst'),
    ('processing_process', 'processing_activity', 'business_process', 'Verarbeitung findet im Geschäftsprozess statt'),
    ('processing_asset', 'processing_activity', 'asset', 'Verarbeitung nutzt Informationswert'),
    ('processing_supplier', 'processing_activity', 'supplier', 'Lieferant ist an Verarbeitung beteiligt'),
    ('processing_control', 'processing_activity', 'control', 'Control schützt Verarbeitung'),
    ('dpia_processing_activity', 'dpia', 'processing_activity', 'Folgenabschätzung bewertet Verarbeitung'),
    ('dpia_control', 'dpia', 'control', 'Control behandelt Datenschutzrisiko');

CREATE TABLE catalog_links (
    tenant_id uuid NOT NULL REFERENCES tenants(id),
    id uuid NOT NULL DEFAULT gen_random_uuid(),
    kind text NOT NULL,
    from_id uuid NOT NULL,
    from_kind text NOT NULL,
    from_scope text NOT NULL,
    to_id uuid NOT NULL,
    to_kind text NOT NULL,
    to_scope text NOT NULL,
    rationale text NOT NULL CHECK (char_length(btrim(rationale)) BETWEEN 1 AND 5000),
    version integer NOT NULL DEFAULT 1 CHECK (version > 0),
    created_by uuid NOT NULL,
    created_at timestamptz NOT NULL DEFAULT clock_timestamp(),
    updated_at timestamptz NOT NULL DEFAULT clock_timestamp(),
    PRIMARY KEY (tenant_id, id),
    UNIQUE (tenant_id, kind, from_id, to_id),
    CHECK (from_id <> to_id),
    FOREIGN KEY (kind, from_kind, to_kind) REFERENCES catalog_link_types(kind, from_kind, to_kind),
    FOREIGN KEY (tenant_id, from_id, from_kind, from_scope)
        REFERENCES catalog_records(tenant_id, id, kind, scope) ON UPDATE CASCADE,
    FOREIGN KEY (tenant_id, to_id, to_kind, to_scope)
        REFERENCES catalog_records(tenant_id, id, kind, scope) ON UPDATE CASCADE,
    FOREIGN KEY (tenant_id, created_by) REFERENCES members(tenant_id, id)
);

CREATE INDEX catalog_links_source ON catalog_links(tenant_id, from_id);
CREATE INDEX catalog_links_target ON catalog_links(tenant_id, to_id);

ALTER TABLE catalog_records ENABLE ROW LEVEL SECURITY;
ALTER TABLE catalog_records FORCE ROW LEVEL SECURITY;
CREATE POLICY catalog_records_read ON catalog_records FOR SELECT TO grc_runtime
    USING (tenant_id = grc_current_tenant_id() AND grc_scope_allowed(scope, 'records.read'));
CREATE POLICY catalog_records_create ON catalog_records FOR INSERT TO grc_runtime
    WITH CHECK (tenant_id = grc_current_tenant_id() AND created_by = grc_current_user_id()
        AND grc_scope_allowed(scope, 'records.write'));
CREATE POLICY catalog_records_edit ON catalog_records FOR UPDATE TO grc_runtime
    USING (tenant_id = grc_current_tenant_id() AND grc_scope_allowed(scope, 'records.write'))
    WITH CHECK (tenant_id = grc_current_tenant_id() AND grc_scope_allowed(scope, 'records.write'));

ALTER TABLE catalog_dpias ENABLE ROW LEVEL SECURITY;
ALTER TABLE catalog_dpias FORCE ROW LEVEL SECURITY;
CREATE POLICY catalog_dpias_read ON catalog_dpias FOR SELECT TO grc_runtime
    USING (tenant_id = grc_current_tenant_id() AND grc_scope_allowed(scope, 'records.read'));
CREATE POLICY catalog_dpias_create ON catalog_dpias FOR INSERT TO grc_runtime
    WITH CHECK (tenant_id = grc_current_tenant_id() AND grc_scope_allowed(scope, 'records.write'));
CREATE POLICY catalog_dpias_edit ON catalog_dpias FOR UPDATE TO grc_runtime
    USING (tenant_id = grc_current_tenant_id() AND grc_scope_allowed(scope, 'records.write'))
    WITH CHECK (tenant_id = grc_current_tenant_id() AND grc_scope_allowed(scope, 'records.write'));

ALTER TABLE catalog_links ENABLE ROW LEVEL SECURITY;
ALTER TABLE catalog_links FORCE ROW LEVEL SECURITY;
CREATE POLICY catalog_links_read ON catalog_links FOR SELECT TO grc_runtime
    USING (tenant_id = grc_current_tenant_id()
        AND grc_scope_allowed(from_scope, 'records.read') AND grc_scope_allowed(to_scope, 'records.read'));
CREATE POLICY catalog_links_create ON catalog_links FOR INSERT TO grc_runtime
    WITH CHECK (tenant_id = grc_current_tenant_id() AND created_by = grc_current_user_id()
        AND grc_scope_allowed(from_scope, 'records.write') AND grc_scope_allowed(to_scope, 'records.write'));
CREATE POLICY catalog_links_edit ON catalog_links FOR UPDATE TO grc_runtime
    USING (tenant_id = grc_current_tenant_id()
        AND grc_scope_allowed(from_scope, 'records.write') AND grc_scope_allowed(to_scope, 'records.write'))
    WITH CHECK (tenant_id = grc_current_tenant_id()
        AND grc_scope_allowed(from_scope, 'records.write') AND grc_scope_allowed(to_scope, 'records.write'));
CREATE POLICY catalog_links_remove ON catalog_links FOR DELETE TO grc_runtime
    USING (tenant_id = grc_current_tenant_id()
        AND grc_scope_allowed(from_scope, 'records.write') AND grc_scope_allowed(to_scope, 'records.write'));

GRANT SELECT, INSERT ON catalog_records, catalog_dpias TO grc_runtime;
GRANT UPDATE (code, title, description, status, owner, scope, org_unit_id, attributes, version, updated_at)
    ON catalog_records TO grc_runtime;
GRANT UPDATE (screening_reason, necessity, risks_to_individuals, safeguards, review_result)
    ON catalog_dpias TO grc_runtime;
GRANT SELECT, INSERT, DELETE ON catalog_links TO grc_runtime;
GRANT UPDATE (rationale, version, updated_at) ON catalog_links TO grc_runtime;
GRANT SELECT ON catalog_link_types TO grc_runtime;
