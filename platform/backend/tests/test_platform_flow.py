import csv
import io
import os
from datetime import UTC, datetime
from uuid import uuid4

import pytest
from fastapi.testclient import TestClient

from app.config import Settings, get_settings
from app.main import app

pytestmark = pytest.mark.integration
PASSWORD = "Synthetic integration test password"


@pytest.fixture
def platform(monkeypatch):
    database_url = os.environ.get("GRC_TEST_DATABASE_URL")
    if not database_url:
        pytest.skip("GRC_TEST_DATABASE_URL requires a dedicated migrated PostgreSQL test database")
    bootstrap_token = uuid4().hex
    monkeypatch.setitem(app.dependency_overrides, get_settings, lambda: Settings(database_url, bootstrap_token))
    monkeypatch.setenv("DATABASE_URL", database_url)
    with TestClient(app) as client:
        assert client.get("/readyz").status_code == 200
        yield client, bootstrap_token


def request(client, method, path, expected, **options):
    response = client.request(method, f"/api/v1{path}", **options)
    assert response.status_code == expected, response.text
    return response


def create_tenant(client, bootstrap_token):
    slug = "flow-" + uuid4().hex
    credentials = {"tenant_slug": slug, "email": "admin@example.test", "password": PASSWORD}
    request(
        client, "POST", "/tenants", 201, headers={"X-Bootstrap-Token": bootstrap_token},
        json={"slug": slug, "name": "Synthetischer Arbeitsbereich", "admin_email": credentials["email"],
              "admin_password": credentials["password"]},
    )
    session = request(client, "POST", "/auth/login", 200, json=credentials).json()
    return {"Authorization": f"Bearer {session['access_token']}"}


def test_single_service_in_security_privacy_and_reporting(platform):
    client, token = platform
    admin = create_tenant(client, token)
    outsider = create_tenant(client, token)

    def entry(kind, title, **values):
        return request(client, "POST", "/catalog", 201, headers=admin,
                       json={"kind": kind, "title": title, "scope": "Verwaltung", **values}).json()

    org = entry("org_unit", "Unternehmens-IT")
    service = entry("it_service", "Microsoft 365", org_unit_id=org["id"])
    risk = entry("risk", "Unerlaubter Kontozugriff")
    control = entry("control", "Authentifizierung absichern")
    action = entry("measure", "MFA-Richtlinie aktivieren")
    processing = entry("processing_activity", "Personalverwaltung", attributes={"purpose": "Personalbetreuung"})
    for kind, source, target in [
        ("risk_it_service", risk, service), ("risk_control", risk, control), ("control_it_service", control, service),
        ("action_control", action, control), ("processing_it_service", processing, service),
        ("processing_control", processing, control),
    ]:
        request(client, "POST", "/links", 201, headers=admin,
                json={"kind": kind, "from_id": source["id"], "to_id": target["id"], "rationale": "Fachlicher Bezug"})
    service_links = request(client, "GET", "/links", 200, headers=admin, params={"record_id": service["id"]}).json()
    assert {link["kind"] for link in service_links} == {"risk_it_service", "control_it_service", "processing_it_service"}

    request(client, "POST", "/organization/people", 422, headers=admin, json={"display_name": "Falsch\u0000"})
    person = request(client, "POST", "/organization/people", 201, headers=admin,
                     json={"display_name": "A. Beispiel", "scope": "Verwaltung"}).json()
    for role, target in [("it_lead", org), ("asset_owner", service), ("risk_owner", risk)]:
        values = {"person_id": person["id"], "target_id": target["id"], "role": role}
        first = request(client, "POST", "/organization/responsibilities", 201, headers=admin, json=values).json()
        replay = request(client, "POST", "/organization/responsibilities", 201, headers=admin, json=values).json()
        assert first["id"] == replay["id"]
    assigned = request(client, "GET", "/organization/responsibilities", 200, headers=admin).json()
    assert {row["role"] for row in assigned} == {"it_lead", "asset_owner", "risk_owner"}
    assert {row["person_id"] for row in assigned} == {person["id"]}
    assignment = next(row for row in assigned if row["role"] == "asset_owner")
    revoked = request(client, "POST", f"/organization/responsibilities/{assignment['id']}/revoke", 200, headers=admin).json()
    assert revoked["revoked_at"]
    active = request(client, "GET", "/organization/responsibilities", 200, headers=admin).json()
    assert {row["role"] for row in active} == {"it_lead", "risk_owner"}

    request(client, "POST", f"/risks/{risk['id']}/assessments", 201, headers=admin, json={
        "risk_version": risk["version"], "assessed_on": datetime.now(UTC).date().isoformat(),
        "inherent_likelihood": 4, "inherent_impact": 4, "residual_likelihood": 2, "residual_impact": 3,
        "response": "mitigate", "rationale": "Zugriff wird mit MFA abgesichert.",
    })
    evidence = request(client, "POST", "/evidence/upload", 201, headers=admin,
                       files={"file": ("Nachweis.txt", b"Synthetischer Test", "text/plain")},
                       data={"title": "Pruefauszug", "origin": "Systemtest", "scope": "Verwaltung"}).json()
    for row in (control, action):
        request(client, "POST", f"/evidence/{evidence['id']}/links", 200, headers=admin, json={"record_id": row["id"]})
    management = request(client, "GET", "/reports/management", 200, headers=admin).json()
    assert management["record_counts"]["it_service"] == management["evidence"]["total"] == 1
    assert management["risk"]["assessed"] == 1 and management["evidence"]["controls_without_current"] == 0
    privacy = request(client, "GET", "/reports/privacy", 200, headers=admin).json()
    assert {r["relation"]: r["id"] for r in privacy["activities"][0]["relations"]} == {
        "processing_it_service": service["id"], "processing_control": control["id"],
    }
    response = request(client, "GET", "/reports/privacy", 200, headers=admin, params={"format": "csv"})
    row = next(csv.DictReader(io.StringIO(response.content.decode("utf-8-sig")), delimiter=";"))
    assert row["IT-Dienste"] == service["title"] and row["TOM / Controls"] == control["title"]
    assert response.headers["cache-control"] == "no-store"

    request(client, "GET", f"/catalog/{service['id']}", 404, headers=outsider)
    request(client, "GET", f"/evidence/{evidence['id']}/file", 404, headers=outsider)
    assert request(client, "GET", "/reports/privacy", 200, headers=outsider).json()["activities"] == []
    assert request(client, "GET", "/organization/people", 200, headers=outsider).json() == []
    user_id = request(client, "GET", "/auth/me", 200, headers=admin).json()["user_id"]
    audit = request(client, "GET", "/audit-events", 200, headers=admin).json()
    event_types = {(event["action"], event["entity_type"]) for event in audit}
    assert {("responsibility.revoked", "responsibility"), ("report.exported", "privacy_report")} <= event_types
    assert {event["actor_id"] for event in audit} == {user_id}


def test_csv_import_rolls_back_whole_batch_and_replays_safely(platform):
    client, token = platform
    admin = create_tenant(client, token)
    address = "/imports/catalog"
    first_batch = {"kind": "it_service", "content": "title;code\nInventar;CSV1\nAblage;CSV1\n"}
    checked = request(client, "POST", address, 200, headers=admin, json=first_batch).json()
    assert checked == {"status": "validiert", "rows": 2, "record_ids": []}
    assert request(client, "GET", "/catalog", 200, headers=admin).json() == []
    request(client, "POST", address, 422, headers=admin, json={**first_batch, "dry_run": False})
    change_headers = admin | {"Idempotency-Key": "catalog-csv-batch"}
    request(client, "POST", address, 409, headers=change_headers, json={**first_batch, "dry_run": False})
    assert request(client, "GET", "/catalog", 200, headers=admin).json() == []

    correct = {"kind": "it_service", "content": "title;code\nInventar;CSV1\nAblage;CSV2\n", "dry_run": False}
    imported = request(client, "POST", address, 200, headers=change_headers, json=correct).json()
    assert imported["status"] == "importiert" and len(set(imported["record_ids"])) == 2
    assert request(client, "POST", address, 200, headers=change_headers, json=correct).json() == imported
    request(client, "POST", address, 409, headers=change_headers, json={**correct, "content": "title\nNeuer Wert\n"})
    assert len(request(client, "GET", "/catalog", 200, headers=admin).json()) == 2

    invalid = request(client, "POST", "/auth/login", 422, json={
        "tenant_slug": "fixture", "email": "admin@example.test", "password": {"value": "synthetic-private-input"},
    })
    assert "synthetic-private-input" not in invalid.text
