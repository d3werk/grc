"""People and business responsibilities shared by catalog modules."""

from datetime import datetime
from typing import Annotated, Literal
from uuid import UUID

from fastapi import APIRouter, Depends, HTTPException
from pydantic import BaseModel, ConfigDict, Field, StringConstraints

from .db import audit_event
from .tenancy import RequestContext, context

router = APIRouter(prefix="/api/v1/organization", tags=["Organisation"])
Readable = Annotated[RequestContext, Depends(context("records.read"), scope="function")]
Writable = Annotated[RequestContext, Depends(context("records.write"), scope="function")]
Name = Annotated[str, StringConstraints(strip_whitespace=True, min_length=1, max_length=240, pattern=r"^[^\x00]*$")]
Email = Annotated[str, StringConstraints(strip_whitespace=True, max_length=254, pattern=r"^[^\x00]*$")]
Scope = Annotated[str, StringConstraints(strip_whitespace=True, max_length=240, pattern=r"^[^\x00]*$")]
Role = Literal[
    "management", "it_lead", "privacy_lead", "security_lead", "asset_owner",
    "risk_owner", "control_owner", "action_owner", "process_owner",
]
ROLE_TARGETS = {
    "management": {"org_unit"}, "it_lead": {"org_unit"}, "privacy_lead": {"org_unit"},
    "security_lead": {"org_unit"}, "asset_owner": {"asset", "it_service"}, "risk_owner": {"risk"},
    "control_owner": {"control"}, "action_owner": {"measure"},
    "process_owner": {"business_process", "processing_activity"},
}
PERSON_COLUMNS = "id, display_name, email, scope, active, version, created_at, updated_at"


class PersonCreate(BaseModel):
    model_config = ConfigDict(extra="forbid")
    display_name: Name
    email: Email = ""
    scope: Scope = ""


class PersonUpdate(BaseModel):
    model_config = ConfigDict(extra="forbid")
    display_name: Name
    email: Email = ""
    active: bool = True
    expected_version: int = Field(ge=1)


class ResponsibilityCreate(BaseModel):
    model_config = ConfigDict(extra="forbid")
    person_id: UUID
    target_id: UUID
    role: Role


class PersonView(BaseModel):
    id: UUID
    display_name: str
    email: str
    scope: str
    active: bool
    version: int
    created_at: datetime
    updated_at: datetime


class RevocationView(BaseModel):
    id: UUID
    revoked_at: datetime | None


class ResponsibilityView(RevocationView):
    person_id: UUID
    target_id: UUID
    target_kind: str
    scope: str
    role: Role
    created_at: datetime


class NamedResponsibilityView(ResponsibilityView):
    display_name: str


@router.get("/people", response_model=list[PersonView])
def list_people(ctx: Readable, active: bool | None = None) -> list[dict]:
    return ctx.conn.execute(
        f"SELECT {PERSON_COLUMNS} FROM organization_people WHERE tenant_id=%s "
        "AND (%s::boolean IS NULL OR active=%s) ORDER BY lower(display_name), id LIMIT 500",
        (ctx.tenant_id, active, active),
    ).fetchall()


@router.post("/people", status_code=201, response_model=PersonView)
def create_person(payload: PersonCreate, ctx: Writable) -> dict:
    ctx.require_scope(payload.scope)
    person = ctx.conn.execute(
        f"INSERT INTO organization_people(tenant_id,display_name,email,scope) VALUES (%s,%s,%s,%s) "
        f"RETURNING {PERSON_COLUMNS}",
        (ctx.tenant_id, payload.display_name, payload.email, payload.scope),
    ).fetchone()
    assert person is not None
    audit_event(ctx, "person.created", "person", person["id"], scope=payload.scope)
    return person


@router.put("/people/{person_id}", response_model=PersonView)
def edit_person(person_id: UUID, payload: PersonUpdate, ctx: Writable) -> dict:
    current = ctx.conn.execute(
        "SELECT version, scope FROM organization_people WHERE tenant_id=%s AND id=%s",
        (ctx.tenant_id, person_id),
    ).fetchone()
    if current is None:
        raise HTTPException(404, "Person nicht gefunden.")
    ctx.require_scope(current["scope"])
    person = ctx.conn.execute(
        "UPDATE organization_people SET display_name=%s, email=%s, active=%s, version=version+1, "
        f"updated_at=clock_timestamp() WHERE tenant_id=%s AND id=%s AND version=%s RETURNING {PERSON_COLUMNS}",
        (payload.display_name, payload.email, payload.active, ctx.tenant_id, person_id, payload.expected_version),
    ).fetchone()
    if person is None:
        raise HTTPException(409, "Die Person wurde inzwischen geändert. Bitte neu laden.")
    audit_event(ctx, "person.updated", "person", person_id, {"version": person["version"]}, person["scope"])
    return person


@router.get("/responsibilities", response_model=list[NamedResponsibilityView])
def list_responsibilities(
    ctx: Readable, target_id: UUID | None = None, person_id: UUID | None = None, include_revoked: bool = False
) -> list[dict]:
    return ctx.conn.execute(
        """
        SELECT a.id, a.person_id, p.display_name, a.target_id, a.target_kind, a.role, a.scope, a.created_at, a.revoked_at
        FROM organization_responsibilities a JOIN organization_people p
          ON p.tenant_id=a.tenant_id AND p.id=a.person_id
        WHERE a.tenant_id=%s AND (%s OR a.revoked_at IS NULL)
          AND (%s::uuid IS NULL OR a.target_id=%s) AND (%s::uuid IS NULL OR a.person_id=%s)
        ORDER BY a.created_at DESC, a.id LIMIT 500
        """,
        (ctx.tenant_id, include_revoked, target_id, target_id, person_id, person_id),
    ).fetchall()


@router.post("/responsibilities", status_code=201, response_model=ResponsibilityView)
def create_responsibility(payload: ResponsibilityCreate, ctx: Writable) -> dict:
    person = ctx.conn.execute(
        "SELECT id, scope, active FROM organization_people WHERE tenant_id=%s AND id=%s FOR SHARE",
        (ctx.tenant_id, payload.person_id),
    ).fetchone()
    target = ctx.conn.execute(
        "SELECT id, kind, scope FROM catalog_records WHERE tenant_id=%s AND id=%s FOR SHARE",
        (ctx.tenant_id, payload.target_id),
    ).fetchone()
    if person is None or target is None:
        raise HTTPException(404, "Person oder Datensatz nicht gefunden.")
    ctx.require_scope(person["scope"])
    ctx.require_scope(target["scope"])
    if not person["active"] or target["kind"] not in ROLE_TARGETS[payload.role]:
        raise HTTPException(422, "Eine aktive Person und eine zum Datensatz passende Rolle auswählen.")
    relation = ctx.conn.execute(
        """
        INSERT INTO organization_responsibilities(tenant_id,person_id,target_id,target_kind,scope,role)
        VALUES (%s,%s,%s,%s,%s,%s)
        ON CONFLICT (tenant_id,person_id,target_id,role) WHERE revoked_at IS NULL DO NOTHING
        RETURNING id,person_id,target_id,target_kind,scope,role,created_at,revoked_at
        """,
        (ctx.tenant_id, payload.person_id, payload.target_id, target["kind"], target["scope"], payload.role),
    ).fetchone()
    if relation is None:
        return ctx.conn.execute(
            "SELECT id,person_id,target_id,target_kind,scope,role,created_at,revoked_at "
            "FROM organization_responsibilities WHERE tenant_id=%s AND person_id=%s "
            "AND target_id=%s AND role=%s AND revoked_at IS NULL",
            (ctx.tenant_id, payload.person_id, payload.target_id, payload.role),
        ).fetchone()
    audit_event(ctx, "responsibility.assigned", "responsibility", relation["id"], {
        "person_id": str(payload.person_id), "target_id": str(payload.target_id), "role": payload.role,
    }, target["scope"])
    return relation


@router.post("/responsibilities/{assignment_id}/revoke", response_model=RevocationView)
def revoke_responsibility(assignment_id: UUID, ctx: Writable) -> dict:
    row = ctx.conn.execute(
        "SELECT a.id, a.scope, p.scope AS person_scope, a.revoked_at "
        "FROM organization_responsibilities a JOIN organization_people p "
        "ON p.tenant_id=a.tenant_id AND p.id=a.person_id WHERE a.tenant_id=%s AND a.id=%s",
        (ctx.tenant_id, assignment_id),
    ).fetchone()
    if row is None:
        raise HTTPException(404, "Verantwortlichkeit nicht gefunden.")
    ctx.require_scope(row["scope"])
    ctx.require_scope(row["person_scope"])
    if row["revoked_at"] is None:
        changed = ctx.conn.execute(
            "UPDATE organization_responsibilities SET revoked_at=clock_timestamp() WHERE tenant_id=%s AND id=%s "
            "AND revoked_at IS NULL RETURNING revoked_at",
            (ctx.tenant_id, assignment_id),
        ).fetchone()
        if changed:
            row["revoked_at"] = changed["revoked_at"]
            audit_event(ctx, "responsibility.revoked", "responsibility", assignment_id, scope=row["scope"])
        else:
            row["revoked_at"] = ctx.conn.execute(
                "SELECT revoked_at FROM organization_responsibilities WHERE tenant_id=%s AND id=%s",
                (ctx.tenant_id, assignment_id),
            ).fetchone()["revoked_at"]
    return {"id": assignment_id, "revoked_at": row["revoked_at"]}
