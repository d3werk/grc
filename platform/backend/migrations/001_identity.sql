DO $roles$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'grc_runtime') THEN
        CREATE ROLE grc_runtime NOLOGIN NOSUPERUSER NOCREATEDB NOCREATEROLE NOREPLICATION NOBYPASSRLS;
    END IF;
    IF NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'grc_authenticator') THEN
        CREATE ROLE grc_authenticator NOLOGIN NOSUPERUSER NOCREATEDB NOCREATEROLE NOREPLICATION NOBYPASSRLS;
    END IF;
    IF EXISTS (SELECT 1 FROM pg_roles WHERE rolname IN ('grc_runtime', 'grc_authenticator')
               AND (rolcanlogin OR rolsuper OR rolcreatedb OR rolcreaterole OR rolreplication OR rolbypassrls)) THEN
        RAISE EXCEPTION 'Platform database roles must be unprivileged and cannot log in';
    END IF;
END;
$roles$;

REVOKE CREATE ON SCHEMA public FROM PUBLIC;
GRANT USAGE ON SCHEMA public TO grc_runtime, grc_authenticator;

CREATE FUNCTION grc_current_tenant_id() RETURNS uuid LANGUAGE sql STABLE
    AS $$ SELECT NULLIF(current_setting('grc.tenant_id', true), '')::uuid $$;

CREATE FUNCTION grc_current_user_id() RETURNS uuid LANGUAGE sql STABLE
    AS $$ SELECT NULLIF(current_setting('grc.user_id', true), '')::uuid $$;

CREATE FUNCTION grc_scope_allowed(resource_scope text, required_permission text) RETURNS boolean LANGUAGE sql STABLE
    AS $$
        SELECT COALESCE(
            grant_value = '"*"'::jsonb OR
            (jsonb_typeof(grant_value) = 'array' AND grant_value ? resource_scope),
            false
        )
        FROM (SELECT NULLIF(current_setting('grc.permission_scopes', true), '')::jsonb -> required_permission AS grant_value) context
    $$;

CREATE FUNCTION grc_scope_allowed(resource_scope text) RETURNS boolean LANGUAGE sql STABLE
    AS $$ SELECT public.grc_scope_allowed(resource_scope, NULLIF(current_setting('grc.permission', true), '')) $$;

CREATE TABLE tenants (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    slug text NOT NULL UNIQUE CHECK (slug ~ '^[a-z0-9][a-z0-9-]{1,62}$'),
    name text NOT NULL CHECK (length(btrim(name)) BETWEEN 1 AND 200),
    created_at timestamptz NOT NULL DEFAULT clock_timestamp()
);

CREATE TABLE members (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id uuid NOT NULL REFERENCES tenants(id) ON DELETE RESTRICT,
    email text NOT NULL,
    password_hash text NOT NULL,
    role text NOT NULL CHECK (role IN ('admin', 'member', 'viewer', 'advisor')),
    active boolean NOT NULL DEFAULT true,
    created_at timestamptz NOT NULL DEFAULT clock_timestamp(),
    UNIQUE (tenant_id, id),
    UNIQUE (tenant_id, email)
);

CREATE TABLE sessions (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id uuid NOT NULL,
    user_id uuid NOT NULL,
    token_hash bytea NOT NULL UNIQUE CHECK (length(token_hash) = 32),
    created_at timestamptz NOT NULL DEFAULT clock_timestamp(),
    expires_at timestamptz NOT NULL,
    revoked_at timestamptz,
    FOREIGN KEY (tenant_id, user_id) REFERENCES members(tenant_id, id) ON DELETE RESTRICT,
    CHECK (expires_at > created_at)
);

CREATE TABLE advisor_mandates (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id uuid NOT NULL,
    advisor_id uuid NOT NULL,
    scope text NOT NULL CHECK (length(btrim(scope)) BETWEEN 1 AND 240),
    permissions text[] NOT NULL CHECK (cardinality(permissions) > 0),
    created_by uuid NOT NULL,
    created_at timestamptz NOT NULL DEFAULT clock_timestamp(),
    expires_at timestamptz NOT NULL,
    revoked_by uuid,
    revoked_at timestamptz,
    FOREIGN KEY (tenant_id, advisor_id) REFERENCES members(tenant_id, id) ON DELETE RESTRICT,
    FOREIGN KEY (tenant_id, created_by) REFERENCES members(tenant_id, id) ON DELETE RESTRICT,
    FOREIGN KEY (tenant_id, revoked_by) REFERENCES members(tenant_id, id) ON DELETE RESTRICT,
    CHECK (expires_at > created_at),
    CHECK ((revoked_by IS NULL) = (revoked_at IS NULL))
);

CREATE INDEX advisor_mandates_active ON advisor_mandates (tenant_id, advisor_id, expires_at) WHERE revoked_at IS NULL;

CREATE TABLE audit_events (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id uuid NOT NULL,
    actor_id uuid NOT NULL,
    action text NOT NULL,
    entity_type text NOT NULL,
    entity_id text NOT NULL,
    details jsonb NOT NULL DEFAULT '{}'::jsonb,
    scope text NOT NULL DEFAULT '',
    created_at timestamptz NOT NULL DEFAULT clock_timestamp(),
    FOREIGN KEY (tenant_id, actor_id) REFERENCES members(tenant_id, id) ON DELETE RESTRICT
);

CREATE INDEX audit_events_tenant_created ON audit_events (tenant_id, created_at DESC);

ALTER TABLE tenants ENABLE ROW LEVEL SECURITY;
ALTER TABLE tenants FORCE ROW LEVEL SECURITY;
ALTER TABLE members ENABLE ROW LEVEL SECURITY;
ALTER TABLE members FORCE ROW LEVEL SECURITY;
ALTER TABLE sessions ENABLE ROW LEVEL SECURITY;
ALTER TABLE sessions FORCE ROW LEVEL SECURITY;
ALTER TABLE advisor_mandates ENABLE ROW LEVEL SECURITY;
ALTER TABLE advisor_mandates FORCE ROW LEVEL SECURITY;
ALTER TABLE audit_events ENABLE ROW LEVEL SECURITY;
ALTER TABLE audit_events FORCE ROW LEVEL SECURITY;

CREATE POLICY tenants_authn_read ON tenants FOR SELECT TO grc_authenticator USING (true);
CREATE POLICY tenants_authn_insert ON tenants FOR INSERT TO grc_authenticator WITH CHECK (true);
CREATE POLICY members_authn_read ON members FOR SELECT TO grc_authenticator USING (true);
CREATE POLICY members_authn_insert ON members FOR INSERT TO grc_authenticator WITH CHECK (true);
CREATE POLICY sessions_authn_read ON sessions FOR SELECT TO grc_authenticator USING (true);
CREATE POLICY sessions_authn_insert ON sessions FOR INSERT TO grc_authenticator WITH CHECK (true);

CREATE POLICY tenants_runtime_read ON tenants FOR SELECT TO grc_runtime USING (id = grc_current_tenant_id());
CREATE POLICY members_runtime_read ON members FOR SELECT TO grc_runtime USING (
    tenant_id = grc_current_tenant_id() AND
    (id = grc_current_user_id() OR grc_scope_allowed('', 'iam.manage'))
);
CREATE POLICY members_runtime_insert ON members FOR INSERT TO grc_runtime WITH CHECK (
    tenant_id = grc_current_tenant_id() AND grc_scope_allowed('', 'iam.manage')
);
CREATE POLICY sessions_runtime_read ON sessions FOR SELECT TO grc_runtime USING (
    tenant_id = grc_current_tenant_id() AND user_id = grc_current_user_id()
);
CREATE POLICY sessions_runtime_revoke ON sessions FOR UPDATE TO grc_runtime USING (
    tenant_id = grc_current_tenant_id() AND user_id = grc_current_user_id()
) WITH CHECK (tenant_id = grc_current_tenant_id() AND user_id = grc_current_user_id() AND revoked_at IS NOT NULL);

CREATE POLICY mandates_runtime_read ON advisor_mandates FOR SELECT TO grc_runtime USING (
    tenant_id = grc_current_tenant_id() AND
    (advisor_id = grc_current_user_id() OR grc_scope_allowed('', 'iam.manage'))
);
CREATE POLICY mandates_runtime_insert ON advisor_mandates FOR INSERT TO grc_runtime WITH CHECK (
    tenant_id = grc_current_tenant_id() AND created_by = grc_current_user_id() AND grc_scope_allowed('', 'iam.manage')
);
CREATE POLICY mandates_runtime_revoke ON advisor_mandates FOR UPDATE TO grc_runtime USING (
    tenant_id = grc_current_tenant_id() AND grc_scope_allowed('', 'iam.manage')
) WITH CHECK (tenant_id = grc_current_tenant_id() AND grc_scope_allowed('', 'iam.manage')
              AND revoked_by = grc_current_user_id() AND revoked_at IS NOT NULL);

CREATE POLICY audit_runtime_read ON audit_events FOR SELECT TO grc_runtime USING (
    tenant_id = grc_current_tenant_id() AND grc_scope_allowed(scope, 'audit.read')
);
CREATE POLICY audit_runtime_insert ON audit_events FOR INSERT TO grc_runtime WITH CHECK (
    tenant_id = grc_current_tenant_id() AND actor_id = grc_current_user_id() AND grc_scope_allowed(scope)
);

GRANT SELECT, INSERT ON tenants, members, sessions TO grc_authenticator;
GRANT SELECT ON tenants TO grc_runtime;
GRANT SELECT (id, tenant_id, email, role, active, created_at), INSERT (tenant_id, email, password_hash, role) ON members TO grc_runtime;
GRANT SELECT (id, tenant_id, user_id, expires_at, revoked_at), UPDATE (revoked_at) ON sessions TO grc_runtime;
GRANT SELECT, INSERT, UPDATE (revoked_at, revoked_by) ON advisor_mandates TO grc_runtime;
GRANT SELECT, INSERT ON audit_events TO grc_runtime;

CREATE FUNCTION grc_auth_bootstrap(p_slug text, p_name text, p_email text, p_password_hash text)
RETURNS TABLE (id uuid, slug text, name text, admin_id uuid)
LANGUAGE plpgsql SECURITY DEFINER SET search_path = pg_catalog, public, pg_temp AS $$
DECLARE
    new_tenant_id uuid;
    first_admin_id uuid;
BEGIN
    INSERT INTO public.tenants (slug, name) VALUES (p_slug, p_name) RETURNING tenants.id INTO new_tenant_id;
    INSERT INTO public.members (tenant_id, email, password_hash, role)
        VALUES (new_tenant_id, p_email, p_password_hash, 'admin') RETURNING members.id INTO first_admin_id;
    RETURN QUERY SELECT new_tenant_id, p_slug, p_name, first_admin_id;
END;
$$;

CREATE FUNCTION grc_auth_find_login(p_slug text, p_email text)
RETURNS TABLE (tenant_id uuid, user_id uuid, password_hash text)
LANGUAGE sql STABLE SECURITY DEFINER SET search_path = pg_catalog, public, pg_temp AS $$
    SELECT t.id, m.id, m.password_hash FROM public.tenants t
    JOIN public.members m ON m.tenant_id = t.id
    WHERE t.slug = p_slug AND m.email = p_email AND m.active
$$;

CREATE FUNCTION grc_auth_open_session(p_tenant uuid, p_user uuid, p_hash bytea, p_ttl_seconds integer)
RETURNS TABLE (id uuid, expires_at timestamptz)
LANGUAGE sql SECURITY DEFINER SET search_path = pg_catalog, public, pg_temp AS $$
    INSERT INTO public.sessions (tenant_id, user_id, token_hash, expires_at)
    SELECT tenant_id, id, p_hash, clock_timestamp() + make_interval(secs => p_ttl_seconds)
    FROM public.members WHERE tenant_id = p_tenant AND id = p_user AND active
    RETURNING sessions.id, sessions.expires_at
$$;

CREATE FUNCTION grc_auth_resolve_session(p_hash bytea)
RETURNS TABLE (session_id uuid, tenant_id uuid, tenant_slug text, tenant_name text, user_id uuid, email text, role text)
LANGUAGE sql SECURITY DEFINER SET search_path = pg_catalog, public, pg_temp AS $$
    SELECT s.id, t.id, t.slug, t.name, m.id, m.email, m.role FROM public.sessions s
    JOIN public.members m ON (m.tenant_id, m.id) = (s.tenant_id, s.user_id)
    JOIN public.tenants t ON t.id = s.tenant_id
    WHERE s.token_hash = p_hash AND s.revoked_at IS NULL AND s.expires_at > clock_timestamp() AND m.active
$$;

ALTER FUNCTION grc_auth_bootstrap(text, text, text, text) OWNER TO grc_authenticator;
ALTER FUNCTION grc_auth_find_login(text, text) OWNER TO grc_authenticator;
ALTER FUNCTION grc_auth_open_session(uuid, uuid, bytea, integer) OWNER TO grc_authenticator;
ALTER FUNCTION grc_auth_resolve_session(bytea) OWNER TO grc_authenticator;
REVOKE ALL ON FUNCTION grc_auth_bootstrap(text, text, text, text), grc_auth_find_login(text, text),
    grc_auth_open_session(uuid, uuid, bytea, integer), grc_auth_resolve_session(bytea) FROM PUBLIC;
GRANT EXECUTE ON FUNCTION grc_auth_bootstrap(text, text, text, text), grc_auth_find_login(text, text),
    grc_auth_open_session(uuid, uuid, bytea, integer), grc_auth_resolve_session(bytea) TO grc_runtime;
