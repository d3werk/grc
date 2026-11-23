import hashlib
import json
from typing import Annotated, Any
from uuid import UUID

from fastapi import APIRouter, Depends, Header, HTTPException, Query, Response
from psycopg.errors import ForeignKeyViolation, UniqueViolation
from psycopg.types.json import Jsonb

from .db import audit_event
from .domain_models import (
    ATTRIBUTE_FIELDS,
    DPIA_KEYS,
    KIND_LABELS,
    STATUS_LABELS,
    CatalogCreate,
    CatalogKind,
    CatalogKindInfo,
    CatalogLink,
    CatalogLinkCreate,
    CatalogLinkRemoval,
    CatalogLinkType,
    CatalogLinkUpdate,
    CatalogRecord,
    CatalogStatus,
    CatalogStatusInfo,
    CatalogUpdate,
    validate_attributes,
)
from .tenancy import RequestContext, context

router = APIRouter(prefix="/api/v1", tags=["Arbeitsobjekte"])
ReadContext = Annotated[RequestContext, Depends(context("records.read"), scope="function")]
WriteContext = Annotated[RequestContext, Depends(context("records.write"), scope="function")]

_RECORD_SELECT = """
    SELECT r.id, r.kind, r.code, r.title, r.description, r.status, r.owner, r.scope, r.org_unit_id,
           CASE WHEN r.kind = 'dpia' THEN jsonb_strip_nulls(jsonb_build_object(
               'screening_reason', nullif(d.screening_reason, ''),
               'necessity', nullif(d.necessity, ''),
               'risks_to_individuals', nullif(d.risks_to_individuals, ''),
               'safeguards', nullif(d.safeguards, ''),
               'review_result', nullif(d.review_result, '')
           )) ELSE r.attributes END AS attributes,
           r.version, r.created_at, r.updated_at
    FROM catalog_records r
    LEFT JOIN catalog_dpias d ON d.tenant_id = r.tenant_id AND d.record_id = r.id
"""
_LINK_COLUMNS = "id, kind, from_id, from_kind, to_id, to_kind, rationale, version, created_at, updated_at"


@router.get("/catalog/kinds", response_model=list[CatalogKindInfo])
def catalog_kinds(_ctx: ReadContext):
    return [
        {
            "kind": kind,
            "label": label,
            "attribute_fields": [
                {"key": key, "label": field_label, "type": field_type}
                for key, field_label, field_type in ATTRIBUTE_FIELDS[kind]
            ],
        }
        for kind, label in KIND_LABELS.items()
    ]


@router.get("/catalog/statuses", response_model=list[CatalogStatusInfo])
def catalog_statuses(_ctx: ReadContext):
    return [{"status": status, "label": label} for status, label in STATUS_LABELS.items()]


@router.get("/catalog", response_model=list[CatalogRecord])
def list_catalog(
    ctx: ReadContext,
    kind: CatalogKind | None = None,
    status: CatalogStatus | None = None,
    scope: Annotated[str | None, Query(max_length=240, pattern=r"^[^\x00]*$")] = None,
    org_unit_id: UUID | None = None,
    q: Annotated[str, Query(max_length=300, pattern=r"^[^\x00]*$")] = "",
    limit: Annotated[int, Query(ge=1, le=500)] = 200,
    offset: Annotated[int, Query(ge=0)] = 0,
):
    filters = ["r.tenant_id = %s"]
    parameters: list[Any] = [ctx.tenant_id]
    for column, value in (
        ("kind", kind), ("status", status), ("scope", scope), ("org_unit_id", org_unit_id)
    ):
        if value is not None:
            filters.append(f"r.{column} = %s")
            parameters.append(value)
    if q.strip():
        filters.append("strpos(lower(concat_ws(' ', r.title, r.code, r.description, r.owner)), lower(%s)) > 0")
        parameters.append(q.strip())
    return ctx.conn.execute(
        _RECORD_SELECT + " WHERE " + " AND ".join(filters) + " ORDER BY lower(r.title), r.id LIMIT %s OFFSET %s",
        (*parameters, limit, offset),
    ).fetchall()


@router.get("/catalog/{record_id}", response_model=CatalogRecord)
def get_record(record_id: UUID, ctx: ReadContext):
    return _read_record(ctx, record_id)


@router.post("/catalog", response_model=CatalogRecord, status_code=201)
def post_record(
    payload: CatalogCreate,
    ctx: WriteContext,
    idempotency_key: Annotated[str | None, Header(alias="Idempotency-Key", min_length=1, max_length=128)] = None,
):
    return create_record(ctx, payload, idempotency_key)


def create_record(
    ctx: RequestContext, payload: CatalogCreate, idempotency_key: str | None = None
) -> dict[str, Any]:
    ctx.require_scope(payload.scope)
    attributes = validate_attributes(payload.kind, payload.attributes)
    if idempotency_key is not None and (
        not idempotency_key.strip() or idempotency_key != idempotency_key.strip()
        or len(idempotency_key) > 128 or "\x00" in idempotency_key
    ):
        raise HTTPException(422, "Ungültiger Idempotenzschlüssel.")
    request_hash = None
    if idempotency_key is not None:
        values = payload.model_dump(mode="json")
        values["attributes"] = attributes
        request_hash = hashlib.sha256(json.dumps(values, sort_keys=True, separators=(",", ":")).encode()).hexdigest()
        previous = _idempotent_record(ctx, idempotency_key, request_hash)
        if previous is not None:
            return previous

    if payload.kind == "org_unit":
        _lock_org_hierarchy(ctx)
    _require_org_unit(ctx, payload.org_unit_id)
    database_attributes = {} if payload.kind == "dpia" else attributes
    try:
        created = ctx.conn.execute(
            """
            INSERT INTO catalog_records
                (tenant_id, kind, code, title, description, status, owner, scope, org_unit_id,
                 attributes, created_by, create_key, create_hash)
            VALUES (%s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s)
            ON CONFLICT (tenant_id, created_by, create_key) DO NOTHING
            RETURNING id
            """,
            (ctx.tenant_id, payload.kind, payload.code, payload.title, payload.description, payload.status,
             payload.owner, payload.scope, payload.org_unit_id, Jsonb(database_attributes), ctx.user_id,
             idempotency_key, request_hash),
        ).fetchone()
    except UniqueViolation as error:
        raise HTTPException(409, "Dieses Kürzel wird für den Typ im gewählten Geltungsbereich bereits verwendet.") from error

    if created is None:
        previous = _idempotent_record(ctx, idempotency_key, request_hash)
        if previous is None:
            raise HTTPException(409, "Der Idempotenzschlüssel wurde bereits verwendet.")
        return previous

    record_id = created["id"]
    if payload.kind == "dpia":
        _save_dpia(ctx, record_id, payload.scope, attributes)
    audit_event(ctx, "catalog.created", "catalog_record", record_id, {"kind": payload.kind, "version": 1}, payload.scope)
    return _read_record(ctx, record_id)


@router.put("/catalog/{record_id}", response_model=CatalogRecord)
def update_record(record_id: UUID, payload: CatalogUpdate, ctx: WriteContext):
    changes = payload.model_dump(exclude_unset=True, exclude={"expected_version"})
    if any(value is None for key, value in changes.items() if key != "org_unit_id"):
        raise HTTPException(422, "Felder mit Textinhalt können als leere Zeichenfolge übergeben werden.")

    initial = _read_record(ctx, record_id)
    if initial["kind"] == "org_unit" and changes.get("org_unit_id", initial["org_unit_id"]) != initial["org_unit_id"]:
        _lock_org_hierarchy(ctx)
    current = _read_record(ctx, record_id, for_update=True)
    _require_version(payload.expected_version, current["version"])
    if "attributes" in changes:
        changes["attributes"] = validate_attributes(current["kind"], changes["attributes"])
    changes = {key: value for key, value in changes.items() if value != current[key]}
    if not changes:
        return current

    updated = current | changes
    ctx.require_scope(updated["scope"])
    if "org_unit_id" in changes:
        _require_org_unit(ctx, updated["org_unit_id"])
        if updated["kind"] == "org_unit":
            _reject_org_cycle(ctx, record_id, updated["org_unit_id"])
    database_attributes = {} if updated["kind"] == "dpia" else updated["attributes"]
    try:
        ctx.conn.execute(
            """
            UPDATE catalog_records
            SET code = %s, title = %s, description = %s, status = %s, owner = %s, scope = %s,
                org_unit_id = %s, attributes = %s, version = version + 1, updated_at = clock_timestamp()
            WHERE tenant_id = %s AND id = %s
            """,
            (updated["code"], updated["title"], updated["description"], updated["status"], updated["owner"],
             updated["scope"], updated["org_unit_id"], Jsonb(database_attributes), ctx.tenant_id, record_id),
        )
    except UniqueViolation as error:
        raise HTTPException(409, "Dieses Kürzel wird für den Typ im gewählten Geltungsbereich bereits verwendet.") from error
    except ForeignKeyViolation as error:
        raise HTTPException(409, "Eine bestehende Zuordnung bindet den Datensatz an seinen bisherigen Geltungsbereich.") from error
    if updated["kind"] == "dpia" and "attributes" in changes:
        _save_dpia(ctx, record_id, updated["scope"], updated["attributes"])
    audit_event(
        ctx, "catalog.updated", "catalog_record", record_id,
        {"kind": updated["kind"], "version": current["version"] + 1, "fields": sorted(changes)}, updated["scope"],
    )
    return _read_record(ctx, record_id)


@router.get("/links/kinds", response_model=list[CatalogLinkType])
def link_kinds(ctx: ReadContext):
    return ctx.conn.execute("SELECT kind, from_kind, to_kind, label FROM catalog_link_types ORDER BY label").fetchall()


@router.get("/links", response_model=list[CatalogLink])
def list_links(
    ctx: ReadContext,
    record_id: UUID | None = None,
    kind: Annotated[str | None, Query(max_length=80, pattern=r"^[^\x00]*$")] = None,
    limit: Annotated[int, Query(ge=1, le=500)] = 200,
    offset: Annotated[int, Query(ge=0)] = 0,
):
    if record_id is not None:
        _read_record(ctx, record_id)
    return ctx.conn.execute(
        f"""
        SELECT {_LINK_COLUMNS} FROM catalog_links
        WHERE tenant_id = %s
          AND (%s::uuid IS NULL OR from_id = %s OR to_id = %s)
          AND (%s::text IS NULL OR kind = %s)
        ORDER BY created_at DESC, id LIMIT %s OFFSET %s
        """,
        (ctx.tenant_id, record_id, record_id, record_id, kind, kind, limit, offset),
    ).fetchall()


@router.post("/links", response_model=CatalogLink, status_code=201)
def create_link(payload: CatalogLinkCreate, ctx: WriteContext):
    if payload.from_id == payload.to_id:
        raise HTTPException(422, "Eine Verknüpfung braucht zwei verschiedene Datensätze.")
    endpoints = {
        row["id"]: row for row in ctx.conn.execute(
            """
            SELECT id, kind, scope FROM catalog_records
            WHERE tenant_id = %s AND id = ANY(%s) ORDER BY id FOR KEY SHARE
            """,
            (ctx.tenant_id, [payload.from_id, payload.to_id]),
        ).fetchall()
    }
    source = endpoints.get(payload.from_id)
    target = endpoints.get(payload.to_id)
    if source is None or target is None:
        raise HTTPException(404, "Ein verknüpfter Datensatz ist im gewählten Geltungsbereich nicht verfügbar.")
    ctx.require_scope(source["scope"])
    ctx.require_scope(target["scope"])
    relation = ctx.conn.execute(
        "SELECT from_kind, to_kind FROM catalog_link_types WHERE kind = %s", (payload.kind,)
    ).fetchone()
    if relation is None or (relation["from_kind"], relation["to_kind"]) != (source["kind"], target["kind"]):
        raise HTTPException(422, "Diese Verknüpfungsart passt nicht zu Ausgangs- und Zieldatensatz.")

    created = ctx.conn.execute(
        f"""
        INSERT INTO catalog_links
            (tenant_id, kind, from_id, from_kind, from_scope, to_id, to_kind, to_scope, rationale, created_by)
        VALUES (%s, %s, %s, %s, %s, %s, %s, %s, %s, %s)
        ON CONFLICT (tenant_id, kind, from_id, to_id) DO NOTHING
        RETURNING {_LINK_COLUMNS}
        """,
        (ctx.tenant_id, payload.kind, source["id"], source["kind"], source["scope"], target["id"], target["kind"],
         target["scope"], payload.rationale, ctx.user_id),
    ).fetchone()
    if created is None:
        raise HTTPException(409, "Diese Verknüpfung ist bereits vorhanden.")
    audit_event(ctx, "catalog.link_created", "catalog_link", created["id"], {"kind": payload.kind}, source["scope"])
    return created


@router.put("/links/{link_id}", response_model=CatalogLink)
def update_link(link_id: UUID, payload: CatalogLinkUpdate, ctx: WriteContext):
    current = _read_link_for_update(ctx, link_id)
    _require_version(payload.expected_version, current["version"])
    if current["rationale"] == payload.rationale:
        return current
    updated = ctx.conn.execute(
        f"""
        UPDATE catalog_links SET rationale = %s, version = version + 1, updated_at = clock_timestamp()
        WHERE tenant_id = %s AND id = %s RETURNING {_LINK_COLUMNS}
        """,
        (payload.rationale, ctx.tenant_id, link_id),
    ).fetchone()
    audit_event(
        ctx, "catalog.link_updated", "catalog_link", link_id,
        {"kind": current["kind"], "version": updated["version"]}, current["from_scope"],
    )
    return updated


@router.delete("/links/{link_id}", status_code=204)
def delete_link(link_id: UUID, payload: CatalogLinkRemoval, ctx: WriteContext) -> Response:
    current = _read_link_for_update(ctx, link_id)
    _require_version(payload.expected_version, current["version"])
    ctx.conn.execute("DELETE FROM catalog_links WHERE tenant_id = %s AND id = %s", (ctx.tenant_id, link_id))
    audit_event(
        ctx, "catalog.link_removed", "catalog_link", link_id,
        {"kind": current["kind"], "reason": payload.reason}, current["from_scope"],
    )
    return Response(status_code=204)


def _read_record(ctx: RequestContext, record_id: UUID, *, for_update: bool = False) -> dict[str, Any]:
    lock = " FOR UPDATE OF r" if for_update else ""
    row = ctx.conn.execute(
        _RECORD_SELECT + " WHERE r.tenant_id = %s AND r.id = %s" + lock,
        (ctx.tenant_id, record_id),
    ).fetchone()
    if row is None:
        raise HTTPException(404, "Datensatz nicht verfügbar.")
    return row


def _read_link_for_update(ctx: RequestContext, link_id: UUID) -> dict[str, Any]:
    row = ctx.conn.execute(
        f"""
        SELECT {_LINK_COLUMNS}, from_scope, to_scope FROM catalog_links
        WHERE tenant_id = %s AND id = %s FOR UPDATE
        """,
        (ctx.tenant_id, link_id),
    ).fetchone()
    if row is None:
        raise HTTPException(404, "Verknüpfung nicht verfügbar.")
    ctx.require_scope(row["from_scope"])
    ctx.require_scope(row["to_scope"])
    return row


def _require_version(expected: int, actual: int) -> None:
    if expected != actual:
        raise HTTPException(409, "Der Datensatz wurde geändert. Laden Sie den aktuellen Stand vor dem Speichern.")


def _require_org_unit(ctx: RequestContext, org_unit_id: UUID | None) -> None:
    if org_unit_id is None:
        return
    row = ctx.conn.execute(
        "SELECT 1 FROM catalog_records WHERE tenant_id = %s AND id = %s AND kind = 'org_unit'",
        (ctx.tenant_id, org_unit_id),
    ).fetchone()
    if row is None:
        raise HTTPException(422, "Wählen Sie eine verfügbare Organisationseinheit.")


def _lock_org_hierarchy(ctx: RequestContext) -> None:
    if ctx.scopes is not None:
        raise HTTPException(403, "Die Organisationshierarchie wird mit mandantenweiten Datenrechten bearbeitet.")
    ctx.conn.execute("SELECT pg_advisory_xact_lock(hashtextextended(%s, 0))", (f"catalog-hierarchy:{ctx.tenant_id}",))


def _reject_org_cycle(ctx: RequestContext, record_id: UUID, parent_id: UUID | None) -> None:
    if parent_id is None:
        return
    cycle = ctx.conn.execute(
        """
        WITH RECURSIVE parents (id, org_unit_id) AS (
            SELECT id, org_unit_id FROM catalog_records WHERE tenant_id = %s AND id = %s AND kind = 'org_unit'
            UNION
            SELECT r.id, r.org_unit_id FROM parents p JOIN catalog_records r ON r.id = p.org_unit_id
            WHERE r.tenant_id = %s AND r.kind = 'org_unit'
        )
        SELECT 1 FROM parents WHERE id = %s
        """,
        (ctx.tenant_id, parent_id, ctx.tenant_id, record_id),
    ).fetchone()
    if cycle is not None:
        raise HTTPException(422, "Die übergeordnete Einheit darf nicht unter der bearbeiteten Einheit liegen.")


def _idempotent_record(ctx: RequestContext, key: str | None, request_hash: str | None) -> dict[str, Any] | None:
    if key is None:
        return None
    existing = ctx.conn.execute(
        """
        SELECT id, create_hash FROM catalog_records
        WHERE tenant_id = %s AND created_by = %s AND create_key = %s
        """,
        (ctx.tenant_id, ctx.user_id, key),
    ).fetchone()
    if existing is None:
        return None
    if existing["create_hash"] != request_hash:
        raise HTTPException(409, "Der Idempotenzschlüssel gehört bereits zu anderen Eingabedaten.")
    return _read_record(ctx, existing["id"])


def _save_dpia(ctx: RequestContext, record_id: UUID, scope: str, attributes: dict[str, str]) -> None:
    values = tuple(attributes.get(key, "") for key in DPIA_KEYS)
    ctx.conn.execute(
        """
        INSERT INTO catalog_dpias
            (tenant_id, record_id, scope, screening_reason, necessity, risks_to_individuals, safeguards, review_result)
        VALUES (%s, %s, %s, %s, %s, %s, %s, %s)
        ON CONFLICT (tenant_id, record_id) DO UPDATE SET
            screening_reason = EXCLUDED.screening_reason, necessity = EXCLUDED.necessity,
            risks_to_individuals = EXCLUDED.risks_to_individuals, safeguards = EXCLUDED.safeguards,
            review_result = EXCLUDED.review_result
        """,
        (ctx.tenant_id, record_id, scope, *values),
    )
