from __future__ import annotations

import csv
import hashlib
import io
import struct
import unicodedata
import zlib
from collections import Counter
from datetime import date, datetime, timezone
from pathlib import PurePath
from typing import Annotated, Literal
from urllib.parse import quote, urlsplit
from uuid import UUID, uuid4

from fastapi import APIRouter, Depends, HTTPException, Query, Request, Response
from pydantic import BaseModel, BeforeValidator, ConfigDict, Field, StringConstraints, ValidationError, field_validator, model_validator
from starlette.concurrency import run_in_threadpool
from starlette.datastructures import UploadFile

from psycopg.errors import ForeignKeyViolation

from .db import audit_event
from .domain_models import ATTRIBUTE_FIELDS, CatalogKind, CatalogStatus
from .tenancy import RequestContext, context

router = APIRouter(prefix="/api/v1", tags=["Risiken, Nachweise und Berichte"])

MAX_FILE_BYTES = 4 * 1024 * 1024
MAX_UPLOAD_BYTES = MAX_FILE_BYTES + 16 * 1024
_DATABASE_TEXT = r"^[^\x00]*$"
Scope = Annotated[str | None, Query(max_length=240, pattern=_DATABASE_TEXT)]
Score = Annotated[int, Field(ge=1, le=5, strict=True)]
ShortTitle = Annotated[
    str, StringConstraints(strip_whitespace=True, min_length=1, max_length=240, pattern=_DATABASE_TEXT)
]
Origin = Annotated[
    str, StringConstraints(strip_whitespace=True, min_length=1, max_length=1000, pattern=_DATABASE_TEXT)
]
ScopeName = Annotated[str, StringConstraints(strip_whitespace=True, max_length=240, pattern=_DATABASE_TEXT)]
RiskStage = Literal["inherent", "residual"]


def _utc_today() -> date:
    return datetime.now(timezone.utc).date()


def _iso_date(value: object) -> date:
    if type(value) is date:
        return value
    if isinstance(value, str):
        try:
            parsed = date.fromisoformat(value)
        except ValueError:
            pass
        else:
            if parsed.isoformat() == value:
                return parsed
    raise ValueError("Datum im Format JJJJ-MM-TT angeben.")


IsoDate = Annotated[date, BeforeValidator(_iso_date)]


class RiskAssessmentInput(BaseModel):
    model_config = ConfigDict(extra="forbid")

    risk_version: Annotated[int, Field(ge=1, le=2147483647, strict=True)]
    assessed_on: IsoDate
    inherent_likelihood: Score
    inherent_impact: Score
    residual_likelihood: Score | None = None
    residual_impact: Score | None = None
    response: Literal["undecided", "mitigate", "avoid", "transfer", "accept"]
    rationale: Annotated[
        str, StringConstraints(strip_whitespace=True, min_length=1, max_length=8000, pattern=_DATABASE_TEXT)
    ]

    @model_validator(mode="after")
    def validate_assessment(self) -> RiskAssessmentInput:
        if (self.residual_likelihood is None) != (self.residual_impact is None):
            raise ValueError("Eintrittswahrscheinlichkeit und Auswirkung des Restrisikos gemeinsam angeben.")
        if self.assessed_on > _utc_today():
            raise ValueError("Der Bewertungstag darf nicht in der Zukunft liegen.")
        return self


class EvidenceInput(BaseModel):
    model_config = ConfigDict(extra="forbid")

    title: ShortTitle
    origin: Origin
    scope: ScopeName = ""
    collected_on: IsoDate = Field(default_factory=_utc_today)
    valid_until: IsoDate | None = None

    @model_validator(mode="after")
    def validate_dates(self) -> EvidenceInput:
        if self.collected_on > _utc_today():
            raise ValueError("Der Erhebungstag darf nicht in der Zukunft liegen.")
        if self.valid_until is not None and self.valid_until < self.collected_on:
            raise ValueError("Das Ablaufdatum darf nicht vor dem Erhebungstag liegen.")
        return self


class ExternalEvidenceInput(EvidenceInput):
    url: Annotated[str, StringConstraints(strip_whitespace=True, min_length=1, max_length=2000)]

    @field_validator("url")
    @classmethod
    def https_reference(cls, value: str) -> str:
        try:
            address = urlsplit(value)
            _ = address.port
        except ValueError as exc:
            raise ValueError("Eine gültige HTTPS-Adresse angeben.") from exc
        if (
            not value.startswith("https://")
            or address.scheme != "https"
            or not address.hostname
            or address.username is not None
            or address.password is not None
            or "\\" in value
            or any(character.isspace() or unicodedata.category(character).startswith("C") for character in value)
        ):
            raise ValueError("Eine HTTPS-Adresse ohne eingebettete Anmeldedaten angeben.")
        return value


class EvidenceLinkInput(BaseModel):
    model_config = ConfigDict(extra="forbid")

    record_id: UUID


class RiskAssessment(RiskAssessmentInput):
    id: UUID
    risk_id: UUID
    actor_id: UUID
    created_at: datetime
    inherent_score: int
    residual_score: int | None


class RiskMatrixCell(BaseModel):
    likelihood: Score
    impact: Score
    count: int


class RiskMatrix(BaseModel):
    stage: RiskStage
    scope: str | None
    total: int
    unassessed: int
    stale: int
    cells: list[RiskMatrixCell]


class Evidence(BaseModel):
    id: UUID
    title: str
    origin: str
    scope: str
    kind: Literal["file", "external"]
    url: str | None
    filename: str | None
    media_type: Literal["text/plain", "image/png"] | None
    size_bytes: int | None
    sha256: str | None
    collected_on: date
    valid_until: date | None
    created_at: datetime
    linked_record_ids: list[UUID]


class EvidenceLink(BaseModel):
    evidence_id: UUID
    record_id: UUID


class RiskCounts(BaseModel):
    total: int
    assessed: int
    unassessed: int
    stale: int


class EvidenceCounts(BaseModel):
    total: int
    files: int
    external: int
    expired: int
    controls_without_current: int


class Dashboard(BaseModel):
    scope: str | None
    as_of: date
    record_counts: dict[CatalogKind, int]
    status_counts: dict[CatalogKind, dict[CatalogStatus, int]]
    risk: RiskCounts | None
    evidence: EvidenceCounts | None


class PrivacyRelation(BaseModel):
    relation: str
    id: UUID
    kind: CatalogKind
    code: str
    title: str
    scope: str
    status: CatalogStatus


class ProcessingActivityReportRow(BaseModel):
    id: UUID
    code: str
    title: str
    scope: str
    status: CatalogStatus
    owner: str
    version: int
    attributes: dict[str, str]
    relations: list[PrivacyRelation]


class PrivacyReport(BaseModel):
    scope: str | None
    as_of: date
    activities: list[ProcessingActivityReportRow]


_ASSESSMENT_FIELDS = """
    id, risk_id, risk_version, assessed_on, inherent_likelihood, inherent_impact,
    residual_likelihood, residual_impact, response, rationale, actor_id, created_at,
    inherent_score, residual_score
"""

_EVIDENCE_FIELDS = """
    id, title, origin, scope, kind, url, filename, media_type, size_bytes, sha256,
    collected_on, valid_until, created_at
"""


def _catalog_item(ctx: RequestContext, item: UUID, *kinds: str) -> dict:
    row = ctx.conn.execute(
        "SELECT id,kind,scope,version FROM catalog_records WHERE tenant_id=%s AND id=%s AND kind=ANY(%s)",
        (ctx.tenant_id, item, list(kinds)),
    ).fetchone()
    if row is None:
        raise HTTPException(404, "Eintrag im Arbeitsbereich fehlt.")
    ctx.require_scope(row["scope"])
    return row


@router.get("/risks/{risk_id}/assessments", response_model=list[RiskAssessment])
def risk_history(
    risk_id: UUID,
    ctx: Annotated[RequestContext, Depends(context("reviews.read"), scope="function")],
    limit: Annotated[int, Query(ge=1, le=250)] = 100,
    offset: Annotated[int, Query(ge=0)] = 0,
):
    _catalog_item(ctx, risk_id, "risk")
    return ctx.conn.execute(
        f"""SELECT {_ASSESSMENT_FIELDS} FROM risk_assessments
        WHERE tenant_id=%s AND risk_id=%s ORDER BY assessed_on DESC,sequence DESC LIMIT %s OFFSET %s""",
        (ctx.tenant_id, risk_id, limit, offset),
    ).fetchall()


@router.post("/risks/{risk_id}/assessments", status_code=201, response_model=RiskAssessment)
def record_risk_assessment(
    risk_id: UUID, data: RiskAssessmentInput, ctx: Annotated[RequestContext, Depends(context("reviews.write"), scope="function")]
):
    risk = _catalog_item(ctx, risk_id, "risk")
    if data.risk_version != risk["version"]:
        raise HTTPException(409, "Das Risiko wurde geändert. Vor der Bewertung den aktuellen Stand öffnen.")
    # The source predicate also protects against an edit between reading and inserting.
    row = ctx.conn.execute(
        f"""INSERT INTO risk_assessments(tenant_id,id,risk_id,risk_version,assessed_on,
            inherent_likelihood,inherent_impact,residual_likelihood,residual_impact,response,rationale,actor_id)
        SELECT r.tenant_id,%s,r.id,r.version,%s,%s,%s,%s,%s,%s,%s,%s FROM catalog_records r
        WHERE r.tenant_id=%s AND r.id=%s AND r.kind='risk' AND r.version=%s
        RETURNING {_ASSESSMENT_FIELDS}""",
        (
            uuid4(), data.assessed_on, data.inherent_likelihood, data.inherent_impact, data.residual_likelihood,
            data.residual_impact, data.response, data.rationale, ctx.user_id, ctx.tenant_id, risk_id, data.risk_version,
        ),
    ).fetchone()
    if row is None:
        raise HTTPException(409, "Das Risiko wurde geändert. Vor der Bewertung den aktuellen Stand öffnen.")
    audit_event(
        ctx, "risk.assessed", "risk_assessment", row["id"],
        {"risk_id": str(risk_id), "risk_version": data.risk_version}, scope=risk["scope"],
    )
    return row


def _latest_risks(ctx: RequestContext, scope: str | None):
    return ctx.conn.execute(
        """SELECT r.version,a.risk_version,a.inherent_likelihood,a.inherent_impact,
            a.residual_likelihood,a.residual_impact
        FROM catalog_records r
        LEFT JOIN LATERAL (
            SELECT risk_version,inherent_likelihood,inherent_impact,residual_likelihood,residual_impact
            FROM risk_assessments WHERE tenant_id=r.tenant_id AND risk_id=r.id
            ORDER BY assessed_on DESC,sequence DESC LIMIT 1
        ) a ON true
        WHERE r.tenant_id=%s AND r.kind='risk' AND (%s::text IS NULL OR r.scope=%s)
            AND grc_scope_allowed(r.scope) AND grc_scope_allowed(r.scope, 'reviews.read')""",
        (ctx.tenant_id, scope, scope),
    ).fetchall()


def _assessments_by_age(risks):
    result = {"total": len(risks), "assessed": 0, "unassessed": 0, "stale": 0}
    for risk in risks:
        label = (
            "unassessed" if risk["risk_version"] is None else
            "stale" if risk["risk_version"] != risk["version"] else "assessed"
        )
        result[label] += 1
    return result


@router.get("/risk-matrix", response_model=RiskMatrix)
def risk_matrix(
    ctx: Annotated[RequestContext, Depends(context("reviews.read"), scope="function")],
    scope: Scope = None,
    stage: RiskStage = "inherent",
):
    if scope is not None:
        ctx.require_scope(scope)
    risks = _latest_risks(ctx, scope)
    bucket = Counter()
    counts = _assessments_by_age(risks)
    for risk in risks:
        if risk["risk_version"] != risk["version"]:
            continue
        likelihood, impact = risk[f"{stage}_likelihood"], risk[f"{stage}_impact"]
        if likelihood is None or impact is None:
            counts["unassessed"] += 1
        else:
            bucket[likelihood, impact] += 1
    return {
        "stage": stage, "scope": scope, "total": counts["total"],
        "unassessed": counts["unassessed"], "stale": counts["stale"],
        "cells": [
            {"likelihood": likelihood, "impact": impact, "count": bucket[likelihood, impact]}
            for likelihood in range(1, 6) for impact in range(1, 6)
        ],
    }


@router.get("/evidence", response_model=list[Evidence])
def list_evidence(
    ctx: Annotated[RequestContext, Depends(context("evidence.read"), scope="function")],
    scope: Scope = None,
    limit: Annotated[int, Query(ge=1, le=250)] = 100,
    offset: Annotated[int, Query(ge=0)] = 0,
):
    if scope is not None:
        ctx.require_scope(scope)
    return ctx.conn.execute(
        f"""SELECT {_EVIDENCE_FIELDS}, ARRAY(
            SELECT record_id FROM evidence_links l WHERE l.tenant_id=e.tenant_id AND l.evidence_id=e.id
            ORDER BY record_id
        ) AS linked_record_ids FROM evidence_items e
        WHERE e.tenant_id=%s AND (%s::text IS NULL OR e.scope=%s)
        ORDER BY e.created_at DESC,e.id LIMIT %s OFFSET %s""",
        (ctx.tenant_id, scope, scope, limit, offset),
    ).fetchall()


def _store_evidence(
    ctx: RequestContext, data: EvidenceInput, *, url: str | None = None,
    filename: str | None = None, media_type: str | None = None, content: bytes | None = None,
):
    ctx.require_scope(data.scope)
    kind = "external" if url is not None else "file"
    row = ctx.conn.execute(
        f"""INSERT INTO evidence_items(tenant_id,id,kind,scope,title,origin,url,filename,media_type,content,
            collected_on,valid_until,created_by) VALUES(%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s)
            RETURNING {_EVIDENCE_FIELDS}""",
        (
            ctx.tenant_id, uuid4(), kind, data.scope, data.title, data.origin, url, filename, media_type, content,
            data.collected_on, data.valid_until, ctx.user_id,
        ),
    ).fetchone()
    audit_event(ctx, "evidence.created", "evidence", row["id"], {"kind": kind}, scope=data.scope)
    return {**row, "linked_record_ids": []}


@router.post("/evidence", status_code=201, response_model=Evidence)
def new_external_evidence(
    data: ExternalEvidenceInput, ctx: Annotated[RequestContext, Depends(context("evidence.write"), scope="function")]
):
    return _store_evidence(ctx, data, url=data.url)


def _valid_png(content: bytes) -> bool:
    if not content.startswith(b"\x89PNG\r\n\x1a\n"):
        return False
    offset = 8
    header_seen = data_seen = False
    while offset + 12 <= len(content):
        length, kind = struct.unpack_from(">I4s", content, offset)
        data_start = offset + 8
        data_end = data_start + length
        chunk_end = data_end + 4
        if chunk_end > len(content):
            return False
        if zlib.crc32(memoryview(content)[offset + 4:data_end]) != struct.unpack_from(">I", content, data_end)[0]:
            return False
        if not header_seen:
            if kind != b"IHDR" or length != 13:
                return False
            width, height = struct.unpack_from(">II", content, data_start)
            if min(width, height) < 1 or max(width, height) > 4096:
                return False
            header_seen = True
        elif kind == b"IHDR" or kind == b"acTL":
            return False
        elif kind == b"IDAT":
            data_seen |= length > 0
        elif kind == b"IEND":
            return data_seen and length == 0 and chunk_end == len(content)
        offset = chunk_end
    return False


def _media_type(filename: str, content: bytes) -> str:
    suffix = PurePath(filename).suffix.lower()
    if suffix == ".png" and _valid_png(content):
        return "image/png"
    if suffix in {".txt", ".log"}:
        try:
            text = content.decode("utf-8-sig")
            if text and all(ord(character) >= 32 or character in "\r\n\t" for character in text):
                return "text/plain"
        except UnicodeDecodeError:
            pass
    raise HTTPException(415, "Eine PNG-Datei bis 4096 × 4096 Pixel oder eine gültige UTF-8-Textdatei (.txt, .log) wählen.")


@router.post(
    "/evidence/upload",
    status_code=201,
    response_model=Evidence,
    openapi_extra={
        "requestBody": {
            "required": True,
            "content": {
                "multipart/form-data": {
                    "schema": {
                        "type": "object",
                        "required": ["file", "title", "origin"],
                        "properties": {
                            "file": {
                                "type": "string", "format": "binary",
                                "description": "PNG, UTF-8-TXT oder UTF-8-LOG bis 4 MiB",
                            },
                            "title": {"type": "string", "maxLength": 240},
                            "origin": {"type": "string", "maxLength": 1000},
                            "scope": {"type": "string", "maxLength": 240},
                            "collected_on": {"type": "string", "format": "date"},
                            "valid_until": {"type": "string", "format": "date"},
                        },
                    },
                },
            },
        },
    },
)
async def upload_evidence(request: Request, ctx: Annotated[RequestContext, Depends(context("evidence.write"), scope="function")]):
    length_header = request.headers.get("content-length")
    if length_header is None:
        raise HTTPException(411, "Für den Datei-Upload ist eine Inhaltslänge erforderlich.")
    try:
        request_length = int(length_header)
    except ValueError as exc:
        raise HTTPException(400, "Ungültige Inhaltslänge.") from exc
    if request_length <= 0:
        raise HTTPException(400, "Der Upload ist leer.")
    if request_length > MAX_UPLOAD_BYTES:
        raise HTTPException(413, "Nachweisdateien dürfen maximal 4 MiB groß sein.")
    if request.headers.get("content-type", "").split(";", 1)[0].strip().lower() != "multipart/form-data":
        raise HTTPException(415, "Ein multipart/form-data-Upload mit den Nachweisdaten wird erwartet.")

    received = 0

    async def bounded_receive():
        nonlocal received
        message = await request.receive()
        received += len(message.get("body", b""))
        if received > MAX_UPLOAD_BYTES:
            raise HTTPException(413, "Nachweisdateien dürfen maximal 4 MiB groß sein.")
        return message

    bounded_request = Request(request.scope, receive=bounded_receive)
    async with bounded_request.form(max_files=1, max_fields=5, max_part_size=4096) as form:
        values = {}
        for key, value in form.multi_items():
            if key in values:
                raise HTTPException(422, "Jedes Upload-Feld darf nur einmal übertragen werden.")
            values[key] = value
        file = values.pop("file", None)
        if not isinstance(file, UploadFile):
            raise HTTPException(422, "Eine Nachweisdatei auswählen.")
        if not values.get("valid_until"):
            values.pop("valid_until", None)
        if not values.get("collected_on"):
            values.pop("collected_on", None)
        try:
            metadata = EvidenceInput.model_validate(values)
        except ValidationError as exc:
            raise HTTPException(422, "Titel, Herkunft und Gültigkeitsdaten des Nachweises prüfen.") from exc
        ctx.require_scope(metadata.scope)
        filename = (file.filename or "").strip()
        if (
            not 1 <= len(filename) <= 200
            or filename in {".", ".."}
            or "/" in filename or "\\" in filename
            or any(unicodedata.category(character).startswith("C") for character in filename)
        ):
            raise HTTPException(422, "Einen gültigen Dateinamen angeben.")
        content = await file.read(MAX_FILE_BYTES + 1)
        if not content:
            raise HTTPException(422, "Die Nachweisdatei ist leer.")
        if len(content) > MAX_FILE_BYTES:
            raise HTTPException(413, "Nachweisdateien dürfen maximal 4 MiB groß sein.")
        media_type = _media_type(filename, content)
    return await run_in_threadpool(
        _store_evidence, ctx, metadata, filename=filename, media_type=media_type, content=content
    )


@router.get(
    "/evidence/{evidence_id}/file",
    response_class=Response,
    responses={
        200: {
            "description": "Nachweisdatei zum Download",
            "content": {
                "text/plain": {"schema": {"type": "string"}},
                "image/png": {"schema": {"type": "string", "format": "binary"}},
            },
        },
    },
)
def download_evidence(evidence_id: UUID, ctx: Annotated[RequestContext, Depends(context("evidence.read"), scope="function")]):
    row = ctx.conn.execute(
        """SELECT scope,filename,media_type,sha256,size_bytes,content FROM evidence_items
        WHERE tenant_id=%s AND id=%s AND kind='file'""",
        (ctx.tenant_id, evidence_id),
    ).fetchone()
    if row is None:
        raise HTTPException(404, "Nachweisdatei fehlt.")
    ctx.require_scope(row["scope"])
    body = bytes(row["content"])
    if len(body) != row["size_bytes"] or hashlib.sha256(body).hexdigest() != row["sha256"]:
        raise HTTPException(409, "Die Integritätsprüfung der Nachweisdatei ist fehlgeschlagen.")
    audit_event(ctx, "evidence.downloaded", "evidence", evidence_id, scope=row["scope"])
    return Response(
        body, media_type=row["media_type"],
        headers={
            "Cache-Control": "no-store",
            "X-Content-Type-Options": "nosniff",
            "Content-Disposition": "attachment; filename*=UTF-8''" + quote(row["filename"], safe=""),
        },
    )


@router.post(
    "/evidence/{evidence_id}/links",
    response_model=EvidenceLink,
    description="Einen vorhandenen Nachweis einem Control oder einer Maßnahme im selben Geltungsbereich zuordnen.",
)
def link_evidence(
    evidence_id: UUID, data: EvidenceLinkInput, ctx: Annotated[RequestContext, Depends(context("evidence.write"), scope="function")]
):
    evidence = ctx.conn.execute(
        "SELECT scope FROM evidence_items WHERE tenant_id=%s AND id=%s", (ctx.tenant_id, evidence_id)
    ).fetchone()
    if evidence is None:
        raise HTTPException(404, "Nachweis fehlt.")
    ctx.require_scope(evidence["scope"])
    target = _catalog_item(ctx, data.record_id, "control", "measure")
    if target["scope"] != evidence["scope"]:
        raise HTTPException(422, "Nachweis und Ziel müssen denselben Geltungsbereich haben.")
    try:
        row = ctx.conn.execute(
            """INSERT INTO evidence_links(tenant_id,evidence_id,record_id,record_kind,scope,created_by)
            VALUES(%s,%s,%s,%s,%s,%s) ON CONFLICT (tenant_id,evidence_id,record_id) DO NOTHING
            RETURNING evidence_id,record_id""",
            (ctx.tenant_id, evidence_id, data.record_id, target["kind"], evidence["scope"], ctx.user_id),
        ).fetchone()
    except ForeignKeyViolation as exc:
        raise HTTPException(409, "Der verknüpfte Eintrag wurde geändert. Bitte die Auswahl erneut prüfen.") from exc
    if row is not None:
        audit_event(
            ctx, "evidence.linked", "evidence", evidence_id, {"record_id": str(data.record_id)},
            scope=evidence["scope"],
        )
    return {"evidence_id": evidence_id, "record_id": data.record_id}


@router.delete("/evidence/{evidence_id}/links/{record_id}", status_code=204)
def unlink_evidence(
    evidence_id: UUID, record_id: UUID, ctx: Annotated[RequestContext, Depends(context("evidence.write"), scope="function")]
):
    row = ctx.conn.execute(
        """DELETE FROM evidence_links WHERE tenant_id=%s AND evidence_id=%s AND record_id=%s RETURNING scope""",
        (ctx.tenant_id, evidence_id, record_id),
    ).fetchone()
    if row is not None:
        audit_event(
            ctx, "evidence.unlinked", "evidence", evidence_id, {"record_id": str(record_id)}, scope=row["scope"]
        )
    return Response(status_code=204)


def _evidence_counts(ctx: RequestContext, scope: str | None, today: date) -> dict:
    items = ctx.conn.execute(
        """SELECT count(*) AS total,count(*) FILTER(WHERE kind='file') AS files,
            count(*) FILTER(WHERE kind='external') AS external,
            count(*) FILTER(WHERE valid_until<%s) AS expired
        FROM evidence_items WHERE tenant_id=%s AND (%s::text IS NULL OR scope=%s)""",
        (today, ctx.tenant_id, scope, scope),
    ).fetchone()
    controls = ctx.conn.execute(
        """SELECT count(*) AS controls_without_current FROM catalog_records r
        WHERE r.tenant_id=%s AND r.kind='control' AND (%s::text IS NULL OR r.scope=%s)
            AND grc_scope_allowed(r.scope) AND grc_scope_allowed(r.scope, 'evidence.read')
            AND NOT EXISTS (
                SELECT 1 FROM evidence_links l JOIN evidence_items e ON e.tenant_id=l.tenant_id AND e.id=l.evidence_id
                WHERE l.tenant_id=r.tenant_id AND l.record_id=r.id AND e.collected_on<=%s
                    AND (e.valid_until IS NULL OR e.valid_until>=%s)
            )""",
        (ctx.tenant_id, scope, scope, today, today),
    ).fetchone()
    return {**items, **controls}


def _dashboard(ctx: RequestContext, scope: str | None) -> dict:
    if scope is not None:
        ctx.require_scope(scope)
    now = _utc_today()
    records = ctx.conn.execute(
        """SELECT kind,status,count(*) AS count FROM catalog_records
        WHERE tenant_id=%s AND (%s::text IS NULL OR scope=%s)
            AND grc_scope_allowed(scope) AND grc_scope_allowed(scope, 'records.read')
        GROUP BY kind,status ORDER BY kind,status""",
        (ctx.tenant_id, scope, scope),
    ).fetchall()
    record_counts = Counter()
    status_counts = {}
    for record in records:
        kind = record["kind"]
        record_counts[kind] += record["count"]
        status_counts.setdefault(kind, {})[record["status"]] = record["count"]
    return {
        "scope": scope, "as_of": now, "record_counts": dict(record_counts), "status_counts": status_counts,
        "risk": _assessments_by_age(_latest_risks(ctx, scope)) if "reviews.read" in ctx.permissions else None,
        "evidence": _evidence_counts(ctx, scope, now) if "evidence.read" in ctx.permissions else None,
    }


@router.get("/dashboard", response_model=Dashboard)
def dashboard(ctx: Annotated[RequestContext, Depends(context("records.read"), scope="function")], scope: Scope = None):
    return _dashboard(ctx, scope)


def _audit_report(ctx: RequestContext, report_type: str, report_format: str, scope: str | None) -> None:
    report_id = uuid4()
    audit_scopes = (scope,) if scope is not None else sorted(ctx.scopes) if ctx.scopes is not None else ("",)
    for audit_scope in audit_scopes:
        audit_event(ctx, "report.exported", report_type, report_id, {"format": report_format}, scope=audit_scope)


def _csv_response(stream: io.StringIO, filename: str) -> Response:
    return Response(
        "\ufeff" + stream.getvalue(), media_type="text/csv",
        headers={"Content-Disposition": f"attachment; filename={filename}", "Cache-Control": "no-store"},
    )


@router.get(
    "/reports/management",
    response_model=Dashboard,
    responses={200: {"content": {"text/csv": {"schema": {"type": "string"}}}}},
)
def management_report(
    response: Response,
    ctx: Annotated[RequestContext, Depends(context("exports.read"), scope="function")],
    scope: Scope = None,
    report_format: Annotated[Literal["json", "csv"], Query(alias="format")] = "json",
):
    report = _dashboard(ctx, scope)
    _audit_report(ctx, "management_report", report_format, scope)
    if report_format == "json":
        response.headers["Cache-Control"] = "no-store"
        return report
    stream = io.StringIO(newline="")
    writer = csv.writer(stream, delimiter=";")
    writer.writerow(["Bereich", "Kennzahl", "Status", "Anzahl"])
    for kind, count in report["record_counts"].items():
        writer.writerow(["Register", kind, "", count])
    for kind, statuses in report["status_counts"].items():
        for status, count in statuses.items():
            writer.writerow(["Registerstatus", kind, status, count])
    for section in ("risk", "evidence"):
        for metric, count in (report[section] or {}).items():
            writer.writerow([section, metric, "", count])
    return _csv_response(stream, "grc-management.csv")


_PRIVACY_FIELDS = ATTRIBUTE_FIELDS["processing_activity"]
_PRIVACY_RELATIONS = {
    "processing_asset": "Informationswerte",
    "processing_it_service": "IT-Dienste",
    "processing_control": "TOM / Controls",
    "processing_process": "Geschäftsprozesse",
    "processing_supplier": "Lieferanten",
    "dpia_processing_activity": "Datenschutz-Folgenabschätzungen",
}


def _privacy_snapshot(ctx: RequestContext, scope: str | None) -> dict:
    if scope is not None:
        ctx.require_scope(scope)
    read_scopes = ctx.scopes_for("records.read")
    if "records.read" not in ctx.permissions or (scope is not None and read_scopes is not None and scope not in read_scopes):
        raise HTTPException(403, "Für den Datenschutzbericht sind Lese- und Exportrechte im Bereich erforderlich.")

    activities = ctx.conn.execute(
        """SELECT id,code,title,scope,status,owner,version,attributes FROM catalog_records
        WHERE tenant_id=%s AND kind='processing_activity' AND (%s::text IS NULL OR scope=%s)
            AND grc_scope_allowed(scope, 'records.read') AND grc_scope_allowed(scope, 'exports.read')
        ORDER BY lower(title),id""",
        (ctx.tenant_id, scope, scope),
    ).fetchall()
    activity_by_id = {activity["id"]: activity for activity in activities}
    for activity in activities:
        attributes = activity["attributes"]
        activity["attributes"] = {
            key: attributes[key] for key, _, _ in _PRIVACY_FIELDS if isinstance(attributes.get(key), str)
        }
        activity["relations"] = []

    if activity_by_id:
        activity_ids = list(activity_by_id)
        relations = ctx.conn.execute(
            """WITH included AS (
                SELECT kind AS relation,
                    CASE WHEN kind='dpia_processing_activity' THEN to_id ELSE from_id END AS activity_id,
                    CASE WHEN kind='dpia_processing_activity' THEN from_id ELSE to_id END AS target_id
                FROM catalog_links
                WHERE tenant_id=%s AND grc_scope_allowed(from_scope, 'exports.read')
                    AND grc_scope_allowed(to_scope, 'exports.read')
                    AND (
                        (kind=ANY(%s::text[]) AND kind<>'dpia_processing_activity' AND from_id=ANY(%s::uuid[]))
                        OR (kind='dpia_processing_activity' AND to_id=ANY(%s::uuid[]))
                    )
            )
            SELECT l.activity_id,l.relation,r.id,r.kind,r.code,r.title,r.scope,r.status FROM included l
            JOIN catalog_records r ON r.tenant_id=%s AND r.id=l.target_id
            WHERE (%s::text IS NULL OR r.scope=%s)
                AND grc_scope_allowed(r.scope, 'records.read') AND grc_scope_allowed(r.scope, 'exports.read')
            ORDER BY l.activity_id,l.relation,lower(r.title),r.id""",
            (ctx.tenant_id, list(_PRIVACY_RELATIONS), activity_ids, activity_ids, ctx.tenant_id, scope, scope),
        ).fetchall()
        for relation in relations:
            activity_by_id[relation.pop("activity_id")]["relations"].append(relation)
    return {"scope": scope, "as_of": _utc_today(), "activities": activities}


def _csv_text(value: str) -> str:
    normalized = unicodedata.normalize("NFKC", value.lstrip())
    return "'" + value if value.startswith(("\t", "\r", "\n")) or normalized.startswith(("=", "+", "-", "@")) else value


@router.get(
    "/reports/privacy",
    response_model=PrivacyReport,
    responses={200: {"content": {"text/csv": {"schema": {"type": "string"}}}}},
)
def privacy_report(
    response: Response,
    ctx: Annotated[RequestContext, Depends(context("exports.read"), scope="function")],
    scope: Scope = None,
    report_format: Annotated[Literal["json", "csv"], Query(alias="format")] = "json",
):
    report = _privacy_snapshot(ctx, scope)
    _audit_report(ctx, "privacy_report", report_format, scope)
    if report_format == "json":
        response.headers["Cache-Control"] = "no-store"
        return report

    stream = io.StringIO(newline="")
    writer = csv.writer(stream, delimiter=";")
    writer.writerow([
        "Stand (UTC)", "ID", "Code", "Verarbeitungstätigkeit", "Geltungsbereich", "Status", "Verantwortung", "Version",
        *(label for _, label, _ in _PRIVACY_FIELDS), *_PRIVACY_RELATIONS.values(),
    ])
    for activity in report["activities"]:
        relation_titles = {kind: [] for kind in _PRIVACY_RELATIONS}
        for relation in activity["relations"]:
            relation_titles[relation["relation"]].append(
                f'{relation["code"]} · {relation["title"]}' if relation["code"] else relation["title"]
            )
        writer.writerow([
            report["as_of"].isoformat(), str(activity["id"]),
            *(_csv_text(activity[field]) for field in ("code", "title", "scope", "status", "owner")), activity["version"],
            *(_csv_text(activity["attributes"].get(key, "")) for key, _, _ in _PRIVACY_FIELDS),
            *(_csv_text(" | ".join(relation_titles[kind])) for kind in _PRIVACY_RELATIONS),
        ])
    return _csv_response(stream, "grc-datenschutz.csv")
