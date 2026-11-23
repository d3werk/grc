CREATE TABLE risk_assessments (
    tenant_id uuid NOT NULL REFERENCES tenants(id) ON DELETE RESTRICT,
    id uuid NOT NULL,
    sequence bigint GENERATED ALWAYS AS IDENTITY,
    risk_id uuid NOT NULL,
    risk_kind text GENERATED ALWAYS AS ('risk'::text) STORED,
    risk_version integer NOT NULL CHECK (risk_version >= 1),
    assessed_on date NOT NULL,
    inherent_likelihood smallint NOT NULL CHECK (inherent_likelihood BETWEEN 1 AND 5),
    inherent_impact smallint NOT NULL CHECK (inherent_impact BETWEEN 1 AND 5),
    residual_likelihood smallint CHECK (residual_likelihood BETWEEN 1 AND 5),
    residual_impact smallint CHECK (residual_impact BETWEEN 1 AND 5),
    inherent_score smallint GENERATED ALWAYS AS (inherent_likelihood * inherent_impact) STORED,
    residual_score smallint GENERATED ALWAYS AS (residual_likelihood * residual_impact) STORED,
    response text NOT NULL CHECK (response IN ('undecided', 'mitigate', 'avoid', 'transfer', 'accept')),
    rationale text NOT NULL CHECK (length(btrim(rationale)) BETWEEN 1 AND 8000),
    actor_id uuid NOT NULL,
    created_at timestamptz NOT NULL DEFAULT now(),
    PRIMARY KEY (tenant_id, id),
    FOREIGN KEY (tenant_id, risk_id, risk_kind) REFERENCES catalog_records(tenant_id, id, kind) ON DELETE RESTRICT,
    FOREIGN KEY (tenant_id, actor_id) REFERENCES members(tenant_id, id) ON DELETE RESTRICT,
    CHECK ((residual_likelihood IS NULL) = (residual_impact IS NULL))
);

CREATE INDEX risk_assessments_latest ON risk_assessments(tenant_id, risk_id, assessed_on DESC, sequence DESC);

CREATE FUNCTION operations_prevent_assessment_change() RETURNS trigger LANGUAGE plpgsql AS $$
BEGIN
    RAISE EXCEPTION 'Risikobewertungen bleiben unverändert erhalten' USING ERRCODE = '23514';
END;
$$;

CREATE TRIGGER operations_assessment_immutable
BEFORE UPDATE OR DELETE OR TRUNCATE ON risk_assessments
FOR EACH STATEMENT EXECUTE FUNCTION operations_prevent_assessment_change();

CREATE TABLE evidence_items (
    tenant_id uuid NOT NULL REFERENCES tenants(id) ON DELETE RESTRICT,
    id uuid NOT NULL,
    kind text NOT NULL CHECK (kind IN ('file', 'external')),
    scope text NOT NULL DEFAULT '' CHECK (scope = btrim(scope) AND length(scope) <= 240),
    title text NOT NULL CHECK (length(btrim(title)) BETWEEN 1 AND 240),
    origin text NOT NULL CHECK (length(btrim(origin)) BETWEEN 1 AND 1000),
    url text CHECK (length(url) <= 2000 AND url LIKE 'https://%'),
    filename text CHECK (length(filename) BETWEEN 1 AND 200),
    media_type text CHECK (media_type IN ('text/plain', 'image/png')),
    content bytea,
    size_bytes integer GENERATED ALWAYS AS (octet_length(content)) STORED,
    sha256 text GENERATED ALWAYS AS (encode(pg_catalog.sha256(content), 'hex')) STORED,
    collected_on date NOT NULL,
    valid_until date CHECK (valid_until >= collected_on),
    created_by uuid NOT NULL,
    created_at timestamptz NOT NULL DEFAULT now(),
    PRIMARY KEY (tenant_id, id),
    UNIQUE (tenant_id, id, scope),
    FOREIGN KEY (tenant_id, created_by) REFERENCES members(tenant_id, id) ON DELETE RESTRICT,
    CHECK (
        (kind = 'file' AND url IS NULL AND filename IS NOT NULL AND media_type IS NOT NULL
            AND content IS NOT NULL AND octet_length(content) BETWEEN 1 AND 4194304)
        OR (kind = 'external' AND url IS NOT NULL AND filename IS NULL AND media_type IS NULL AND content IS NULL)
    )
);

CREATE INDEX evidence_items_scope_date ON evidence_items(tenant_id, scope, valid_until);

CREATE TABLE evidence_links (
    tenant_id uuid NOT NULL,
    evidence_id uuid NOT NULL,
    record_id uuid NOT NULL,
    record_kind text GENERATED ALWAYS AS ('control'::text) STORED,
    scope text NOT NULL,
    created_by uuid NOT NULL,
    created_at timestamptz NOT NULL DEFAULT now(),
    PRIMARY KEY (tenant_id, evidence_id, record_id),
    FOREIGN KEY (tenant_id, evidence_id, scope) REFERENCES evidence_items(tenant_id, id, scope) ON UPDATE RESTRICT ON DELETE RESTRICT,
    FOREIGN KEY (tenant_id, record_id, record_kind, scope) REFERENCES catalog_records(tenant_id, id, kind, scope) ON UPDATE RESTRICT ON DELETE RESTRICT,
    FOREIGN KEY (tenant_id, created_by) REFERENCES members(tenant_id, id) ON DELETE RESTRICT
);

CREATE INDEX evidence_links_record ON evidence_links(tenant_id, record_id);

ALTER TABLE risk_assessments ENABLE ROW LEVEL SECURITY;
ALTER TABLE risk_assessments FORCE ROW LEVEL SECURITY;
CREATE POLICY risk_assessment_read ON risk_assessments FOR SELECT USING (
    tenant_id = grc_current_tenant_id() AND EXISTS (
        SELECT 1 FROM catalog_records r
        WHERE r.tenant_id = risk_assessments.tenant_id AND r.id = risk_assessments.risk_id
            AND r.kind = 'risk' AND grc_scope_allowed(r.scope) AND grc_scope_allowed(r.scope, 'reviews.read')
    )
);
CREATE POLICY risk_assessment_write ON risk_assessments FOR INSERT WITH CHECK (
    tenant_id = grc_current_tenant_id() AND actor_id = grc_current_user_id() AND EXISTS (
        SELECT 1 FROM catalog_records r
        WHERE r.tenant_id = risk_assessments.tenant_id AND r.id = risk_assessments.risk_id
            AND r.kind = 'risk' AND r.version = risk_assessments.risk_version
            AND grc_scope_allowed(r.scope) AND grc_scope_allowed(r.scope, 'reviews.write')
    )
);

ALTER TABLE evidence_items ENABLE ROW LEVEL SECURITY;
ALTER TABLE evidence_items FORCE ROW LEVEL SECURITY;
CREATE POLICY evidence_read ON evidence_items FOR SELECT USING (
    tenant_id = grc_current_tenant_id() AND grc_scope_allowed(scope) AND grc_scope_allowed(scope, 'evidence.read')
);
CREATE POLICY evidence_write ON evidence_items FOR INSERT WITH CHECK (
    tenant_id = grc_current_tenant_id() AND created_by = grc_current_user_id()
        AND grc_scope_allowed(scope) AND grc_scope_allowed(scope, 'evidence.write')
);

ALTER TABLE evidence_links ENABLE ROW LEVEL SECURITY;
ALTER TABLE evidence_links FORCE ROW LEVEL SECURITY;
CREATE POLICY evidence_link_read ON evidence_links FOR SELECT USING (
    tenant_id = grc_current_tenant_id() AND grc_scope_allowed(scope)
        AND grc_scope_allowed(scope, 'records.read') AND grc_scope_allowed(scope, 'evidence.read')
);
CREATE POLICY evidence_link_write ON evidence_links FOR INSERT WITH CHECK (
    tenant_id = grc_current_tenant_id() AND created_by = grc_current_user_id()
        AND grc_scope_allowed(scope) AND grc_scope_allowed(scope, 'evidence.write')
);
CREATE POLICY evidence_link_remove ON evidence_links FOR DELETE USING (
    tenant_id = grc_current_tenant_id() AND grc_scope_allowed(scope) AND grc_scope_allowed(scope, 'evidence.write')
);

GRANT SELECT, INSERT ON risk_assessments, evidence_items, evidence_links TO grc_runtime;
GRANT DELETE ON evidence_links TO grc_runtime;
GRANT USAGE, SELECT ON SEQUENCE risk_assessments_sequence_seq TO grc_runtime;
