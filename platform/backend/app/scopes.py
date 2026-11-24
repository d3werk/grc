from typing import Annotated

from fastapi import APIRouter, Depends, HTTPException
from pydantic import Field

from .db import audit_event
from .domain_models import DatabaseText, RequestModel
from .tenancy import RequestContext, context

router = APIRouter(prefix="/api/v1/scopes", tags=["Geltungsbereiche"])
ReadScopeContext = Annotated[RequestContext, Depends(context(), scope="function")]
WriteScopeContext = Annotated[RequestContext, Depends(context("records.write"), scope="function")]

_FILTER_PERMISSIONS = ("records.read", "reviews.read", "evidence.read", "exports.read")
_WRITER_PERMISSIONS = ("records.write", "evidence.write")


class ScopeInput(RequestModel):
    scope: Annotated[DatabaseText, Field(min_length=1, max_length=240)]


class ScopeOptions(RequestModel):
    scopes: list[str]
    read_scopes: dict[str, list[str] | None]
    catalog_write: list[str] | None
    evidence_write: list[str] | None
    can_create: bool


def _granted_scopes(ctx: RequestContext, permission: str) -> list[str] | None:
    scopes = ctx.scopes_for(permission)
    return None if scopes is None else sorted(scopes)


@router.get("", response_model=ScopeOptions)
def list_scopes(ctx: ReadScopeContext):
    grants = {permission: _granted_scopes(ctx, permission) for permission in _FILTER_PERMISSIONS + _WRITER_PERMISSIONS}
    if ctx.role == "advisor":
        scopes = {name for grant in grants.values() if grant is not None for name in grant if name}
    else:
        # Evidence policies check the current permission as well as the named grant.
        ctx.conn.execute("SELECT set_config('grc.permission', 'evidence.read', true)")
        rows = ctx.conn.execute(
            """
            SELECT scope FROM scope_options WHERE tenant_id = %(tenant)s
            UNION SELECT scope FROM catalog_records WHERE tenant_id = %(tenant)s AND scope <> ''
            UNION SELECT scope FROM organization_people WHERE tenant_id = %(tenant)s AND scope <> ''
            UNION SELECT scope FROM evidence_items WHERE tenant_id = %(tenant)s AND scope <> ''
            UNION SELECT scope FROM advisor_mandates WHERE tenant_id = %(tenant)s
            """,
            {"tenant": ctx.tenant_id},
        )
        scopes = {row["scope"] for row in rows}

    return ScopeOptions(
        scopes=sorted(scopes),
        read_scopes={permission: grants[permission] for permission in _FILTER_PERMISSIONS},
        catalog_write=grants["records.write"],
        evidence_write=grants["evidence.write"],
        can_create=grants["records.write"] is None,
    )


@router.post("", status_code=201, response_model=ScopeInput)
def create_scope(payload: ScopeInput, ctx: WriteScopeContext):
    if ctx.scopes is not None:
        raise HTTPException(403, "Zum Anlegen von Geltungsbereichen sind mandantenweite Schreibrechte erforderlich.")

    created = ctx.conn.execute(
        "INSERT INTO scope_options (tenant_id, scope) VALUES (%s, %s) ON CONFLICT DO NOTHING RETURNING scope",
        (ctx.tenant_id, payload.scope),
    ).fetchone()
    if created:
        audit_event(ctx, "scope.created", "scope", payload.scope, scope=payload.scope)
    return payload
