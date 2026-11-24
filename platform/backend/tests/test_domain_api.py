import os
from datetime import UTC, datetime, timedelta
from uuid import uuid4

import pytest
from fastapi import FastAPI
from fastapi.testclient import TestClient

from app.config import get_settings
from app.domain import router as domain_router
from app.tenancy import router as tenancy_router

pytestmark = pytest.mark.integration
_PASSWORD = "Synthetic integration-only password 2026!"


@pytest.fixture
def client(monkeypatch):
    database_url = os.environ.get("GRC_TEST_DATABASE_URL")
    if not database_url:
        pytest.skip("GRC_TEST_DATABASE_URL requires a dedicated migrated PostgreSQL test database")
    bootstrap_token = uuid4().hex
    monkeypatch.setenv("DATABASE_URL", database_url)
    monkeypatch.setenv("GRC_BOOTSTRAP_TOKEN", bootstrap_token)
    get_settings.cache_clear()
    app = FastAPI()
    app.include_router(tenancy_router)
    app.include_router(domain_router)
    with TestClient(app) as test_client:
        yield test_client, bootstrap_token
    get_settings.cache_clear()


def _login(client, slug, email):
    response = client.post(
        "/api/v1/auth/login", json={"tenant_slug": slug, "email": email, "password": _PASSWORD}
    )
    assert response.status_code == 200, response.text
    return {"Authorization": f"Bearer {response.json()['access_token']}"}


def _tenant(client, bootstrap_token):
    slug = "domain-" + uuid4().hex
    response = client.post(
        "/api/v1/tenants",
        headers={"X-Bootstrap-Token": bootstrap_token},
        json={"slug": slug, "name": "Synthetischer Mandant", "admin_email": "admin@example.com", "admin_password": _PASSWORD},
    )
    assert response.status_code == 201, response.text
    return slug, _login(client, slug, "admin@example.com")


def _record(client, headers, kind, title, **values):
    response = client.post("/api/v1/catalog", headers=headers, json={"kind": kind, "title": title, **values})
    assert response.status_code == 201, response.text
    return response.json()


def _link(client, headers, kind, source, target, expected=201):
    response = client.post(
        "/api/v1/links",
        headers=headers,
        json={"kind": kind, "from_id": source["id"], "to_id": target["id"], "rationale": "Fachliche Zuordnung"},
    )
    assert response.status_code == expected, response.text
    return response.json()


def test_shared_catalog_links_and_optimistic_versions(client):
    http, bootstrap_token = client
    _, admin = _tenant(http, bootstrap_token)
    root = _record(http, admin, "org_unit", "Unternehmen")
    site = _record(http, admin, "org_unit", "Standort", scope="Nord", org_unit_id=root["id"])
    cycle = http.put(
        f"/api/v1/catalog/{root['id']}", headers=admin,
        json={"expected_version": root["version"], "org_unit_id": site["id"]},
    )
    assert cycle.status_code == 422

    idempotent = admin | {"Idempotency-Key": "original-asset-01"}
    original = _record(http, idempotent, "asset", "Kundenverwaltung", code="AST-01", scope="Nord", org_unit_id=site["id"])
    repeated = _record(http, idempotent, "asset", "Kundenverwaltung", code="AST-01", scope="Nord", org_unit_id=site["id"])
    assert original["id"] == repeated["id"]
    changed_payload = http.post("/api/v1/catalog", headers=idempotent, json={"kind": "asset", "title": "Andere Eingabe"})
    assert changed_payload.status_code == 409

    processing = _record(
        http, admin, "processing_activity", "Auftragsabwicklung", scope="Nord",
        attributes={"purpose": "Erfüllung des Auftrags", "data_subjects": "Kundinnen und Kunden"},
    )
    control = _record(http, admin, "control", "Berechtigungsprüfung", scope="Nord")
    dpia = _record(
        http, admin, "dpia", "Folgenabschätzung Kundenverwaltung", scope="Nord",
        attributes={"screening_reason": "Risikobehaftete Änderung", "safeguards": "Zugriff begrenzen"},
    )
    _link(http, admin, "processing_asset", processing, original)
    control_link = _link(http, admin, "control_asset", control, original)
    _link(http, admin, "dpia_processing_activity", dpia, processing)
    _link(http, admin, "requirement_control", processing, control, expected=422)

    links = http.get("/api/v1/links", headers=admin, params={"record_id": original["id"]})
    assert links.status_code == 200
    assert {link["kind"] for link in links.json()} == {"processing_asset", "control_asset"}
    persisted = http.get(f"/api/v1/catalog/{dpia['id']}", headers=admin)
    assert persisted.status_code == 200
    assert persisted.json()["attributes"] == dpia["attributes"]
    dpia_update = http.put(
        f"/api/v1/catalog/{dpia['id']}", headers=admin,
        json={"expected_version": dpia["version"], "scope": "Süd", "attributes": {"review_result": "Freigabe offen"}},
    )
    assert dpia_update.status_code == 200, dpia_update.text
    assert dpia_update.json()["attributes"] == {"review_result": "Freigabe offen"}
    assert len(http.get("/api/v1/links", headers=admin, params={"record_id": dpia["id"]}).json()) == 1

    update = http.put(
        f"/api/v1/catalog/{original['id']}", headers=admin,
        json={"expected_version": original["version"], "owner": "IT-Betrieb"},
    )
    assert update.status_code == 200, update.text
    assert update.json()["version"] == original["version"] + 1
    assert update.json()["org_unit_id"] == site["id"]
    stale = http.put(
        f"/api/v1/catalog/{original['id']}", headers=admin,
        json={"expected_version": original["version"], "owner": "Ungeprüfter Stand"},
    )
    assert stale.status_code == 409

    link_address = f"/api/v1/links/{control_link['id']}"
    revised_link = http.put(
        link_address, headers=admin, json={"expected_version": control_link["version"], "rationale": "Neue Bewertung"},
    )
    assert revised_link.status_code == 200, revised_link.text
    assert revised_link.json()["version"] == control_link["version"] + 1
    removed_link = http.request(
        "DELETE", link_address, headers=admin,
        json={"expected_version": revised_link.json()["version"], "reason": "Zuordnung aufgehoben"},
    )
    assert removed_link.status_code == 204


def test_formatted_catalog_fields_preserve_legacy_plain_text_updates(client):
    http, bootstrap_token = client
    _, admin = _tenant(http, bootstrap_token)
    description = {"type": "doc", "content": [
        {"type": "heading", "attrs": {"level": 2}, "content": [{"type": "text", "text": "Zugangsprüfung"}]},
        {"type": "paragraph", "content": [{"type": "text", "text": "Anmeldungen überprüfen"}]},
    ]}
    objective = {"type": "doc", "content": [{"type": "paragraph", "content": [
        {"type": "text", "text": "Kontrollziel", "marks": [{"type": "bold"}]},
    ]}]}
    record = _record(http, admin, "control", "Berechtigungsprüfung", rich_text={
        "description": description, "control_objective": objective,
    })
    assert record["description"] == "Zugangsprüfung\nAnmeldungen überprüfen"
    assert record["attributes"]["control_objective"] == "Kontrollziel"
    assert record["rich_text"]["control_objective"] == objective
    retrieved = http.get("/api/v1/catalog", headers=admin, params={"q": "Anmeldungen überprüfen"})
    assert retrieved.status_code == 200, retrieved.text
    assert [result["id"] for result in retrieved.json()] == [record["id"]]

    address = f"/api/v1/catalog/{record['id']}"
    renamed = http.put(address, headers=admin, json={"expected_version": record["version"], "title": "Neuer Titel"})
    assert renamed.status_code == 200, renamed.text
    assert renamed.json()["rich_text"] == record["rich_text"]
    plain_edit = http.put(address, headers=admin, json={
        "expected_version": renamed.json()["version"], "description": "Neue unformatierte Beschreibung",
    })
    assert plain_edit.status_code == 200, plain_edit.text
    current = plain_edit.json()
    assert current["description"] == "Neue unformatierte Beschreibung"
    assert current["rich_text"] == {"control_objective": objective}

    for unsupported in (
        {"description": {"type": "doc", "content": [{"type": "rawHtml", "content": "<img>"}]}},
        {"control_frequency": objective},
    ):
        invalid = http.put(address, headers=admin, json={"expected_version": current["version"], "rich_text": unsupported})
        assert invalid.status_code == 422, invalid.text
    retrieved = http.get(address, headers=admin)
    assert retrieved.status_code == 200, retrieved.text
    assert retrieved.json() == current


def test_tenant_and_scope_isolation_for_shared_links(client):
    http, bootstrap_token = client
    slug, admin = _tenant(http, bootstrap_token)
    _, outsider = _tenant(http, bootstrap_token)
    north = _record(http, admin, "asset", "Norddienst", scope="Nord")
    south = _record(http, admin, "risk", "Südrisiko", scope="Süd")
    cross_scope = _link(http, admin, "risk_asset", south, north)
    assert cross_scope["to_id"] == north["id"]
    denied = http.get(f"/api/v1/catalog/{north['id']}", headers=outsider)
    assert denied.status_code == 404
    _link(http, outsider, "risk_asset", south, north, expected=404)

    advisor = http.post(
        "/api/v1/members", headers=admin,
        json={"email": "advisor@example.com", "password": _PASSWORD, "role": "advisor"},
    )
    assert advisor.status_code == 201, advisor.text
    mandate = http.post(
        "/api/v1/advisor-mandates", headers=admin,
        json={
            "advisor_id": advisor.json()["id"], "scope": "Nord",
            "permissions": ["records.read", "records.write"],
            "expires_at": (datetime.now(UTC) + timedelta(days=1)).isoformat(),
        },
    )
    assert mandate.status_code == 201, mandate.text
    scoped = _login(http, slug, "advisor@example.com")
    visible = http.get("/api/v1/catalog", headers=scoped)
    assert visible.status_code == 200
    assert [record["id"] for record in visible.json()] == [north["id"]]
    linked = http.get("/api/v1/links", headers=scoped, params={"record_id": north["id"]})
    assert linked.status_code == 200
    assert linked.json() == []
    forbidden = http.post(
        "/api/v1/catalog", headers=scoped, json={"kind": "asset", "title": "Keine Freigabe", "scope": "Süd"}
    )
    assert forbidden.status_code == 403
    authorized = _record(http, scoped, "asset", "Geprüfte Anlage", scope="Nord")
    assert authorized["scope"] == "Nord"


def test_contract_lifecycle_validates_dates_and_preserves_supplier_links(client):
    http, bootstrap_token = client
    _, admin = _tenant(http, bootstrap_token)
    _, outsider = _tenant(http, bootstrap_token)
    supplier = _record(http, admin, "supplier", "Synthetischer Vertragspartner", scope="Nord")
    attributes = {
        "contract_type": "Dienstleistung", "internal_party": "Einkauf",
        "counterparty": "Synthetischer Vertragspartner", "starts_on": "2030-01-01",
        "ends_on": "2030-12-31", "notice_due": "2030-09-30", "renewal_mode": "Durch Vereinbarung",
    }
    contract = _record(
        http, admin, "contract", "Wartungsvertrag", scope="Nord", owner="Einkauf",
        status="active", attributes=attributes,
    )
    link = _link(http, admin, "contract_supplier", contract, supplier)
    assert link["to_id"] == supplier["id"]
    address = f"/api/v1/catalog/{contract['id']}"
    for invalid_attributes in ({"ends_on": "2029-12-31"}, {"renewal_mode": "Unbekannter Modus"}):
        invalid = http.put(address, headers=admin, json={
            "expected_version": contract["version"], "attributes": {**attributes, **invalid_attributes},
        })
        assert invalid.status_code == 422, invalid.text
    missing_owner = http.put(address, headers=admin, json={"expected_version": contract["version"], "owner": ""})
    assert missing_owner.status_code == 422
    assert http.get(address, headers=admin).json() == contract
    assert http.get(address, headers=outsider).status_code == 404

    decision = {"decision": "Neu verhandeln", "decision_note": "Änderungsangebot einholen"}
    updated = http.put(address, headers=admin, json={
        "expected_version": contract["version"], "status": "in_review", "attributes": {**attributes, **decision},
    })
    assert updated.status_code == 200, updated.text
    assert updated.json()["status"] == "in_review"
    assert updated.json()["version"] == contract["version"] + 1
    assert updated.json()["attributes"] == {**attributes, **decision}
    links = http.get("/api/v1/links", headers=admin, params={"record_id": contract["id"]})
    assert links.status_code == 200, links.text
    assert links.json() == [link]
