"""Entrypoint for the same-origin GRC API and web client."""

import os
from datetime import datetime
from pathlib import Path
from typing import Annotated, Any
from uuid import UUID

import psycopg
from fastapi import Depends, FastAPI, Query, Request
from fastapi.exceptions import RequestValidationError
from fastapi.responses import JSONResponse
from fastapi.staticfiles import StaticFiles
from pydantic import BaseModel

from .db import transaction
from .domain import router as domain_router
from .imports import router as imports_router
from .operations import router as operations_router
from .organization import router as organization_router
from .scopes import router as scopes_router
from .tenancy import RequestContext, context, router as tenancy_router

class ValidationIssue(BaseModel):
    location: list[str | int]
    code: str


class InvalidRequest(BaseModel):
    detail: str
    errors: list[ValidationIssue] | None = None


class AuditEvent(BaseModel):
    id: UUID
    actor_id: UUID
    action: str
    entity_type: str
    entity_id: str
    details: dict[str, Any]
    scope: str
    created_at: datetime


app = FastAPI(
    title="D3 GRC Platform",
    version="0.12.0",
    description=(
        "Mandantengebundene Arbeitsbereiche für Organisation, Assets, Risiken, "
        "Controls, Maßnahmen, Nachweise, Datenschutz, interne Audits, operationelle Resilienz, "
        "Business Continuity, KI-Governance und Verträge."
    ),
    responses={422: {"model": InvalidRequest, "description": "Ungültige Eingabefelder"}},
    swagger_ui_parameters={"persistAuthorization": False},
)


@app.exception_handler(RequestValidationError)
async def validation_error(_request: Request, exc: RequestValidationError) -> JSONResponse:
    # Pydantic errors can contain the original input, including credentials.
    errors = [
        {"location": error["loc"], "code": error["type"]}
        for error in exc.errors()
    ]
    return JSONResponse(
        status_code=422,
        content={"detail": "Bitte prüfen Sie die Eingabefelder.", "errors": errors},
    )


@app.middleware("http")
async def security_headers(request: Request, call_next):
    response = await call_next(request)
    response.headers.setdefault("X-Content-Type-Options", "nosniff")
    response.headers.setdefault("X-Frame-Options", "DENY")
    response.headers.setdefault("Referrer-Policy", "no-referrer")
    if request.url.path.startswith("/api/"):
        response.headers["Cache-Control"] = "no-store"
    return response


@app.get("/healthz", tags=["Betrieb"])
def liveness() -> dict[str, str]:
    return {"status": "ok"}


@app.get("/readyz", tags=["Betrieb"])
def readiness() -> JSONResponse:
    url = os.environ.get("DATABASE_URL")
    if url:
        try:
            with transaction(url) as connection:
                schema = connection.execute(
                    "SELECT to_regclass('public.organization_responsibilities') IS NOT NULL "
                    "AND EXISTS (SELECT FROM catalog_link_types WHERE kind='processing_it_service') "
                    "AND EXISTS (SELECT FROM catalog_link_types WHERE kind='contract_supplier') "
                    "AND to_regclass('public.scope_options') IS NOT NULL "
                    "AND EXISTS (SELECT FROM pg_constraint WHERE conrelid=to_regclass('public.evidence_links') "
                    "AND conname='evidence_links_target_kind' AND pg_get_constraintdef(oid) LIKE '%contract%') "
                    "AND EXISTS (SELECT FROM pg_attribute WHERE attrelid=to_regclass('public.evidence_links') "
                    "AND attname='record_kind' AND attnotnull AND attgenerated='' AND NOT attisdropped) "
                    "AND EXISTS (SELECT FROM pg_attribute WHERE attrelid=to_regclass('public.catalog_records') "
                    "AND attname='rich_text' AND atttypid='jsonb'::regtype AND NOT attisdropped) AS ready"
                ).fetchone()
            if schema and schema["ready"]:
                return JSONResponse({"status": "ready"})
        except (psycopg.Error, RuntimeError):
            pass
    return JSONResponse({"status": "unavailable"}, status_code=503)


@app.get("/api/v1/audit-events", tags=["Audit-Log"], response_model=list[AuditEvent])
def audit_events(
    ctx: Annotated[RequestContext, Depends(context("audit.read"), scope="function")],
    limit: Annotated[int, Query(ge=1, le=200)] = 50,
) -> list[dict]:
    return ctx.conn.execute(
        "SELECT id, actor_id, action, entity_type, entity_id, details, scope, created_at "
        "FROM audit_events WHERE tenant_id=%s ORDER BY created_at DESC, id DESC LIMIT %s",
        (ctx.tenant_id, limit),
    ).fetchall()


app.include_router(tenancy_router)
app.include_router(domain_router)
app.include_router(operations_router)
app.include_router(imports_router)
app.include_router(organization_router)
app.include_router(scopes_router)

frontend = os.environ.get("GRC_FRONTEND_DIR")
if frontend:
    app.mount("/", StaticFiles(directory=Path(frontend), html=True), name="web")
