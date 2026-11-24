"""Transactional CSV import through the same catalog write service as the UI."""

import csv
import hashlib
import io
import json
from typing import Annotated, Literal

from fastapi import APIRouter, Depends, Header, HTTPException
from psycopg.types.json import Jsonb
from pydantic import BaseModel, ConfigDict, Field

from .domain import create_record
from .domain_models import CatalogCreate, validate_attributes, validate_catalog_workflow
from .tenancy import RequestContext, context

router = APIRouter(prefix="/api/v1/imports", tags=["Importe"])
COLUMNS = {"title", "code", "description", "owner", "scope", "status", "org_unit_id", "attributes"}


class CatalogCsvImport(BaseModel):
    model_config = ConfigDict(extra="forbid")

    kind: str = Field(min_length=1, max_length=64)
    content: str = Field(min_length=1, max_length=200000)
    delimiter: Literal[";", ","] = ";"
    dry_run: bool = True


class CatalogImportResult(BaseModel):
    status: Literal["validiert", "importiert"]
    rows: int
    record_ids: list[str]


def csv_records(payload: CatalogCsvImport) -> list[CatalogCreate]:
    source = io.StringIO(payload.content.lstrip("\ufeff"), newline="")
    try:
        reader = csv.DictReader(source, delimiter=payload.delimiter, strict=True)
        columns = reader.fieldnames
        if not columns or "title" not in columns or len(columns) != len(set(columns)) or set(columns) - COLUMNS:
            raise HTTPException(422, "Die CSV-Kopfzeile benötigt title und darf nur dokumentierte Spalten enthalten.")
        records = []
        for row in reader:
            if len(records) == 100:
                raise HTTPException(422, "Pro Import sind höchstens 100 Datensätze möglich.")
            if None in row or any(value is None for value in row.values()):
                raise HTTPException(422, f"Die Spaltenzahl in CSV-Zeile {reader.line_num} stimmt nicht.")
            model = {field: value for field, value in row.items() if value != ""}
            model["kind"] = payload.kind
            try:
                if "attributes" in model:
                    model["attributes"] = json.loads(model["attributes"])
                record = CatalogCreate.model_validate(model)
                attributes = validate_attributes(record.kind, record.attributes)
                validate_catalog_workflow(record.kind, attributes, record.status, record.owner)
                records.append(record.model_copy(update={"attributes": attributes}))
            except ValueError as exc:
                raise HTTPException(422, f"Felder in CSV-Zeile {reader.line_num} prüfen.") from exc
    except csv.Error as exc:
        raise HTTPException(422, "Die CSV-Datei kann mit dem gewählten Trennzeichen nicht gelesen werden.") from exc
    if not records:
        raise HTTPException(422, "Die CSV-Datei enthält keine Datensätze.")
    return records


@router.post("/catalog", response_model=CatalogImportResult)
def import_catalog(
    payload: CatalogCsvImport,
    ctx: Annotated[RequestContext, Depends(context("records.write"), scope="function")],
    idempotency_key: Annotated[str | None, Header(min_length=8, max_length=128)] = None,
) -> CatalogImportResult:
    records = csv_records(payload)
    for record in records:
        ctx.require_scope(record.scope)
    if payload.dry_run:
        return CatalogImportResult(status="validiert", rows=len(records), record_ids=[])
    if idempotency_key is None or not idempotency_key.strip():
        raise HTTPException(422, "Für den Import ist ein Idempotency-Key erforderlich.")

    normalized = [record.model_dump(mode="json") for record in records]
    fingerprint = hashlib.sha256(json.dumps(normalized, sort_keys=True, ensure_ascii=False).encode()).hexdigest()
    tx_key = f"grc-csv:{ctx.tenant_id}:{ctx.user_id}:{idempotency_key}"
    # This lock covers only the calling tenant, actor and import key.
    ctx.conn.execute("SELECT pg_advisory_xact_lock(hashtextextended(%s, 0))", (tx_key,))
    previous = ctx.conn.execute(
        "SELECT fingerprint, result FROM catalog_imports WHERE tenant_id = %s AND actor_id = %s AND key = %s",
        (ctx.tenant_id, ctx.user_id, idempotency_key),
    ).fetchone()
    if previous:
        if previous["fingerprint"] != fingerprint:
            raise HTTPException(409, "Dieser Importschlüssel wurde bereits für andere Daten verwendet.")
        result = CatalogImportResult.model_validate(previous["result"])
        visible = ctx.conn.execute(
            "SELECT count(*) AS n FROM catalog_records WHERE tenant_id=%s AND id=ANY(%s::uuid[]) "
            "AND grc_scope_allowed(scope, 'records.write')",
            (ctx.tenant_id, result.record_ids),
        ).fetchone()
        if visible is None or visible["n"] != result.rows:
            raise HTTPException(403, "Der ursprüngliche Import ist nicht mehr vollständig zugänglich.")
        return result

    ids = [str(create_record(ctx, record)["id"]) for record in records]
    result = CatalogImportResult(status="importiert", rows=len(ids), record_ids=ids)
    ctx.conn.execute(
        "INSERT INTO catalog_imports (tenant_id, actor_id, key, fingerprint, result) VALUES (%s, %s, %s, %s, %s)",
        (ctx.tenant_id, ctx.user_id, idempotency_key, fingerprint, Jsonb(result.model_dump(mode="json"))),
    )
    return result
