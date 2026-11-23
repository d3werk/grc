import hmac
import json
import re
from collections.abc import Iterator, Mapping
from dataclasses import dataclass
from datetime import UTC, datetime
from typing import Annotated, Any, Literal
from uuid import UUID

from fastapi import APIRouter, Depends, Header, HTTPException, Response
from fastapi.security import HTTPAuthorizationCredentials, HTTPBearer
from psycopg.errors import UniqueViolation
from pydantic import BaseModel, ConfigDict, Field, SecretStr

from .config import Settings, get_settings
from .db import Connection, audit_event, transaction
from .security import hash_password, new_session_token, session_token_hash, verify_password

Role = Literal["admin", "member", "viewer", "advisor"]
PermissionScopes = Mapping[str, frozenset[str] | None]

_READ_PERMISSIONS = frozenset({"records.read", "reviews.read", "evidence.read", "exports.read"})
_WRITE_PERMISSIONS = frozenset({"records.write", "reviews.write", "evidence.write"})
_BUSINESS_PERMISSIONS = _READ_PERMISSIONS | _WRITE_PERMISSIONS
_ROLE_PERMISSIONS = {
    "admin": _BUSINESS_PERMISSIONS | {"iam.manage", "audit.read"},
    "member": _BUSINESS_PERMISSIONS | {"audit.read"},
    "viewer": _READ_PERMISSIONS,
}
_SLUG = re.compile(r"[a-z0-9][a-z0-9-]{1,62}\Z")
_EMAIL = re.compile(r"[^@\s\x00]+@[^@\s\x00]+\.[^@\s\x00]+\Z")
_TOKEN = re.compile(r"[A-Za-z0-9_-]{43}\Z")

router = APIRouter(prefix="/api/v1", tags=["Mandanten und Anmeldung"])
_bearer = HTTPBearer(auto_error=False)
AppSettings = Annotated[Settings, Depends(get_settings)]


@dataclass(frozen=True)
class RequestContext:
    conn: Connection
    tenant_id: UUID
    tenant_slug: str
    tenant_name: str
    user_id: UUID
    email: str
    role: Role
    session_id: UUID | None
    permission: str | None
    _permission_scopes: PermissionScopes

    @property
    def permissions(self) -> frozenset[str]:
        return frozenset(self._permission_scopes)

    def scopes_for(self, permission: str) -> frozenset[str] | None:
        return self._permission_scopes.get(permission, frozenset())

    @property
    def scopes(self) -> frozenset[str] | None:
        return self.scopes_for(self.permission) if self.permission else frozenset()

    def require_scope(self, scope: str) -> None:
        scopes = self.scopes
        if scopes is not None and scope not in scopes:
            raise HTTPException(403, "Der Geltungsbereich ist für diese Aktion nicht freigegeben.")


def _unauthorized() -> HTTPException:
    return HTTPException(401, "Anmeldung erforderlich oder Zugangsdaten ungültig.", headers={"WWW-Authenticate": "Bearer"})


def _set_context(ctx: RequestContext) -> None:
    permission_scopes = {
        permission: "*" if scopes is None else sorted(scopes)
        for permission, scopes in ctx._permission_scopes.items()
    }
    ctx.conn.execute(
        """
        SELECT set_config('grc.tenant_id', %s, true), set_config('grc.user_id', %s, true),
               set_config('grc.permission', %s, true), set_config('grc.permission_scopes', %s, true)
        """,
        (str(ctx.tenant_id), str(ctx.user_id), ctx.permission or "", json.dumps(permission_scopes)),
    )


def _advisor_permissions(connection: Connection, tenant_id: UUID, user_id: UUID) -> PermissionScopes:
    # Pair this transaction's authorization with the exclusive lock taken by revocation.
    connection.execute("SELECT pg_advisory_xact_lock_shared(hashtextextended(%s, 814371))", (str(user_id),))
    rows = connection.execute(
        """
        SELECT scope, permissions FROM advisor_mandates
        WHERE tenant_id = %s AND advisor_id = %s AND revoked_at IS NULL AND expires_at > clock_timestamp()
        """,
        (tenant_id, user_id),
    )
    grants: dict[str, set[str]] = {}
    for row in rows:
        for permission in _BUSINESS_PERMISSIONS.intersection(row["permissions"]):
            grants.setdefault(permission, set()).add(row["scope"])
    return {permission: frozenset(scopes) for permission, scopes in grants.items()}


def context(permission: str | None = None):
    """Authorize a bearer session and use one tenant-scoped transaction for the request."""
    if permission is not None and permission not in _ROLE_PERMISSIONS["admin"]:
        raise ValueError(f"Unknown platform permission: {permission}")

    def dependency(
        credentials: Annotated[HTTPAuthorizationCredentials | None, Depends(_bearer)],
        settings: AppSettings,
    ) -> Iterator[RequestContext]:
        if credentials is None or not _TOKEN.fullmatch(credentials.credentials):
            raise _unauthorized()
        with transaction(settings.database_url) as connection:
            session = connection.execute(
                "SELECT * FROM grc_auth_resolve_session(%s)", (session_token_hash(credentials.credentials),)
            ).fetchone()
            if session is None:
                raise _unauthorized()

            role = session["role"]
            grants: PermissionScopes = {grant: None for grant in _ROLE_PERMISSIONS.get(role, ())}
            if role == "advisor":
                connection.execute(
                    "SELECT set_config('grc.tenant_id', %s, true), set_config('grc.user_id', %s, true)",
                    (str(session["tenant_id"]), str(session["user_id"])),
                )
                grants = _advisor_permissions(connection, session["tenant_id"], session["user_id"])

            if permission is not None and permission not in grants:
                raise HTTPException(403, "Für diese Aktion fehlt eine Freigabe.")
            ctx = RequestContext(conn=connection, permission=permission, _permission_scopes=grants, **session)
            _set_context(ctx)
            yield ctx

    return dependency


class ApiInput(BaseModel):
    model_config = ConfigDict(extra="forbid")


class BootstrapInput(ApiInput):
    slug: str = Field(max_length=63)
    name: str = Field(max_length=200)
    admin_email: str = Field(max_length=254)
    admin_password: SecretStr


class LoginInput(ApiInput):
    tenant_slug: str = Field(max_length=63)
    email: str = Field(max_length=254)
    password: SecretStr


class MemberInput(ApiInput):
    email: str = Field(max_length=254)
    password: SecretStr
    role: Role


class MandateInput(ApiInput):
    advisor_id: UUID
    scope: str = Field(max_length=240)
    permissions: list[str] = Field(min_length=1, max_length=len(_BUSINESS_PERMISSIONS))
    expires_at: datetime


def _slug(value: str) -> str:
    return value.strip().lower()


def _email(value: str) -> str:
    return value.strip().casefold()


def _checked_email(value: str) -> str:
    email = _email(value)
    if len(email) > 254 or not _EMAIL.fullmatch(email):
        raise HTTPException(422, "Eine gültige E-Mail-Adresse eingeben.")
    return email


def _checked_text(value: str, label: str) -> str:
    text = value.strip()
    if not text or "\x00" in text:
        raise HTTPException(422, f"{label} eingeben.")
    return text


def _password_hash(password: SecretStr) -> str:
    try:
        return hash_password(password.get_secret_value())
    except ValueError as error:
        raise HTTPException(422, str(error)) from error


@router.post("/tenants", status_code=201)
def bootstrap_tenant(
    payload: BootstrapInput,
    settings: AppSettings,
    bootstrap_token: Annotated[str | None, Header(alias="X-Bootstrap-Token")] = None,
) -> dict[str, Any]:
    if not settings.bootstrap_token or bootstrap_token is None or not hmac.compare_digest(
        bootstrap_token.encode("utf-8"), settings.bootstrap_token.encode("utf-8")
    ):
        raise HTTPException(403, "Die Einrichtung eines Mandanten ist nicht autorisiert.")
    slug = _slug(payload.slug)
    if not _SLUG.fullmatch(slug):
        raise HTTPException(422, "Mandantenkürzel mit 2 bis 63 Kleinbuchstaben, Ziffern oder Bindestrichen eingeben.")
    email = _checked_email(payload.admin_email)
    name = _checked_text(payload.name, "Mandantenname")
    encoded_password = _password_hash(payload.admin_password)

    try:
        with transaction(settings.database_url) as connection:
            tenant = connection.execute(
                "SELECT * FROM grc_auth_bootstrap(%s, %s, %s, %s)", (slug, name, email, encoded_password)
            ).fetchone()
            assert tenant is not None
            ctx = RequestContext(connection, tenant["id"], slug, name, tenant["admin_id"], email, "admin",
                                 None, "iam.manage", {grant: None for grant in _ROLE_PERMISSIONS["admin"]})
            _set_context(ctx)
            audit_event(ctx, "tenant.created", "tenant", tenant["id"], {"slug": slug})
            return tenant
    except UniqueViolation as error:
        raise HTTPException(409, "Das Mandantenkürzel ist bereits vergeben.") from error


@router.post("/auth/login")
def login(payload: LoginInput, settings: AppSettings) -> dict[str, Any]:
    slug, email = _slug(payload.tenant_slug), _email(payload.email)
    with transaction(settings.database_url) as connection:
        member = None
        if _SLUG.fullmatch(slug) and _EMAIL.fullmatch(email):
            member = connection.execute("SELECT * FROM grc_auth_find_login(%s, %s)", (slug, email)).fetchone()
        if not verify_password(payload.password.get_secret_value(), member["password_hash"] if member else None) or member is None:
            raise _unauthorized()
        token = new_session_token()
        session = connection.execute(
            "SELECT * FROM grc_auth_open_session(%s, %s, %s, %s)",
            (member["tenant_id"], member["user_id"], session_token_hash(token), settings.session_ttl_seconds),
        ).fetchone()
        if session is None:
            raise _unauthorized()
        return {"access_token": token, "token_type": "bearer", "expires_at": session["expires_at"]}


@router.get("/auth/me")
def me(ctx: Annotated[RequestContext, Depends(context(), scope="function")]) -> dict[str, Any]:
    return {
        "tenant_id": ctx.tenant_id,
        "tenant_slug": ctx.tenant_slug,
        "tenant_name": ctx.tenant_name,
        "user_id": ctx.user_id,
        "email": ctx.email,
        "role": ctx.role,
        "permissions": sorted(ctx.permissions),
    }


@router.post("/auth/logout", status_code=204)
def logout(ctx: Annotated[RequestContext, Depends(context(), scope="function")]) -> Response:
    ctx.conn.execute("UPDATE sessions SET revoked_at = clock_timestamp() WHERE id = %s AND revoked_at IS NULL", (ctx.session_id,))
    return Response(status_code=204)


@router.get("/members")
def members(ctx: Annotated[RequestContext, Depends(context("iam.manage"), scope="function")]) -> list[dict[str, Any]]:
    return ctx.conn.execute("SELECT id, email, role, active, created_at FROM members ORDER BY email").fetchall()


@router.post("/members", status_code=201)
def create_member(payload: MemberInput, ctx: Annotated[RequestContext, Depends(context("iam.manage"), scope="function")]) -> dict[str, Any]:
    email = _checked_email(payload.email)
    try:
        member = ctx.conn.execute(
            """
            INSERT INTO members (tenant_id, email, password_hash, role) VALUES (%s, %s, %s, %s)
            RETURNING id, email, role, active, created_at
            """,
            (ctx.tenant_id, email, _password_hash(payload.password), payload.role),
        ).fetchone()
    except UniqueViolation as error:
        raise HTTPException(409, "Diese E-Mail-Adresse ist im Mandanten bereits vergeben.") from error
    assert member is not None
    audit_event(ctx, "member.created", "member", member["id"], {"role": payload.role})
    return member


@router.get("/advisor-mandates")
def mandates(ctx: Annotated[RequestContext, Depends(context("iam.manage"), scope="function")]) -> list[dict[str, Any]]:
    return ctx.conn.execute(
        """
        SELECT id, advisor_id, scope, permissions, created_by, created_at, expires_at, revoked_by, revoked_at
        FROM advisor_mandates ORDER BY created_at DESC, id
        """
    ).fetchall()


@router.post("/advisor-mandates", status_code=201)
def create_mandate(payload: MandateInput, ctx: Annotated[RequestContext, Depends(context("iam.manage"), scope="function")]) -> dict[str, Any]:
    scope = _checked_text(payload.scope, "Geltungsbereich für das Beratermandat")
    permissions = set(payload.permissions)
    if not permissions <= _BUSINESS_PERMISSIONS:
        raise HTTPException(422, "Für dieses Beratermandat sind nur fachliche Berechtigungen zulässig.")
    if any(permission.removesuffix(".write") + ".read" not in permissions for permission in permissions & _WRITE_PERMISSIONS):
        raise HTTPException(422, "Schreibrechte immer zusammen mit den entsprechenden Leserechten vergeben.")
    if payload.expires_at.utcoffset() is None or payload.expires_at <= datetime.now(UTC):
        raise HTTPException(422, "Einen künftigen Ablaufzeitpunkt mit Zeitzone angeben.")

    advisor = ctx.conn.execute("SELECT id FROM members WHERE id = %s AND active AND role = 'advisor'", (payload.advisor_id,)).fetchone()
    if advisor is None:
        raise HTTPException(404, "Beraterprofil im Mandanten nicht gefunden.")
    mandate = ctx.conn.execute(
        """
        INSERT INTO advisor_mandates (tenant_id, advisor_id, scope, permissions, created_by, expires_at)
        VALUES (%s, %s, %s, %s, %s, %s)
        RETURNING id, advisor_id, scope, permissions, created_by, created_at, expires_at, revoked_by, revoked_at
        """,
        (ctx.tenant_id, payload.advisor_id, scope, sorted(permissions), ctx.user_id, payload.expires_at),
    ).fetchone()
    assert mandate is not None
    audit_event(ctx, "advisor_mandate.created", "advisor_mandate", mandate["id"], {"permissions": sorted(permissions)}, scope)
    return mandate


@router.post("/advisor-mandates/{mandate_id}/revoke", status_code=204)
def revoke_mandate(mandate_id: UUID, ctx: Annotated[RequestContext, Depends(context("iam.manage"), scope="function")]) -> Response:
    locked = ctx.conn.execute(
        """
        SELECT pg_advisory_xact_lock(hashtextextended(advisor_id::text, 814371))
        FROM advisor_mandates WHERE id = %s
        """,
        (mandate_id,),
    ).fetchone()
    if locked is None:
        raise HTTPException(404, "Beratermandat nicht gefunden.")
    mandate = ctx.conn.execute(
        """
        UPDATE advisor_mandates SET revoked_by = %s, revoked_at = clock_timestamp()
        WHERE id = %s AND revoked_at IS NULL RETURNING scope
        """,
        (ctx.user_id, mandate_id),
    ).fetchone()
    if mandate is not None:
        audit_event(ctx, "advisor_mandate.revoked", "advisor_mandate", mandate_id, scope=mandate["scope"])
    return Response(status_code=204)
