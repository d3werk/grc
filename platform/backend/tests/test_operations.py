import csv
import hashlib
import io
import os
import struct
import zlib
from datetime import UTC, datetime, timedelta
from uuid import uuid4

import pytest
from fastapi import HTTPException
from fastapi.testclient import TestClient

from app.config import Settings, get_settings
from app.main import app
from app.operations import MAX_FILE_BYTES, _csv_text, _media_type

PASSWORD = "Synthetic operations fixture password"
PREFIX = "/api/v1"


@pytest.fixture
def operations_client():
    database_url = os.environ.get("GRC_TEST_DATABASE_URL")
    if not database_url:
        pytest.skip("GRC_TEST_DATABASE_URL requires a dedicated migrated PostgreSQL test database")
    bootstrap_token = uuid4().hex
    app.dependency_overrides[get_settings] = lambda: Settings(database_url, bootstrap_token)
    try:
        with TestClient(app) as client:
            yield client, bootstrap_token
    finally:
        app.dependency_overrides.pop(get_settings, None)


def send(client, method, path, expected, **kwargs):
    response = client.request(method, PREFIX + path, **kwargs)
    assert response.status_code == expected, response.text
    return response


def login(client, slug, email="admin@example.test"):
    token = send(
        client, "POST", "/auth/login", 200,
        json={"tenant_slug": slug, "email": email, "password": PASSWORD},
    ).json()["access_token"]
    return {"Authorization": f"Bearer {token}"}


def tenant(client, bootstrap_token):
    slug = "operations-" + uuid4().hex
    send(
        client, "POST", "/tenants", 201,
        headers={"X-Bootstrap-Token": bootstrap_token},
        json={"slug": slug, "name": "Operations fixture", "admin_email": "admin@example.test", "admin_password": PASSWORD},
    )
    return slug, login(client, slug)


def record(client, headers, kind, title, scope="Nord", **values):
    return send(
        client, "POST", "/catalog", 201, headers=headers,
        json={"kind": kind, "title": title, "scope": scope, **values},
    ).json()


def _png(width: int = 1) -> bytes:
    def chunk(kind: bytes, data: bytes) -> bytes:
        return struct.pack(">I", len(data)) + kind + data + struct.pack(">I", zlib.crc32(kind + data))

    return (
        b"\x89PNG\r\n\x1a\n"
        + chunk(b"IHDR", struct.pack(">IIBBBBB", width, 1, 8, 2, 0, 0, 0))
        + chunk(b"IDAT", zlib.compress(b"\x00\x00\x00\x00"))
        + chunk(b"IEND", b"")
    )


def test_passive_evidence_formats_and_csv_text():
    assert _media_type("evidence.png", _png()) == "image/png"
    assert _media_type("Auszug.txt", "Informationen\n".encode()) == "text/plain"
    for name, payload in [
        ("evidence.png", _png(4097)), ("evidence.png", _png() + b"extra"),
        ("evidence.txt", b"\x00"), ("evidence.txt", b"\xff"), ("evidence.pdf", b"%PDF-1.7"),
    ]:
        with pytest.raises(HTTPException) as rejected:
            _media_type(name, payload)
        assert rejected.value.status_code == 415
    assert _csv_text("  ＝Beispiel") == "'  ＝Beispiel"
    assert _csv_text("Interne Beschreibung") == "Interne Beschreibung"


@pytest.mark.integration
def test_risk_history_and_matrix_distinguish_missing_stale_and_residual(operations_client):
    client, bootstrap_token = operations_client
    slug, admin = tenant(client, bootstrap_token)
    _, stranger = tenant(client, bootstrap_token)
    current = record(client, admin, "risk", "Unterbrechung")
    record(client, admin, "risk", "Unbewertet")
    record(client, admin, "risk", "Anderer Bereich", "Süd")
    path = f"/risks/{current['id']}/assessments"
    data = {
        "risk_version": current["version"], "assessed_on": datetime.now(UTC).date().isoformat(),
        "inherent_likelihood": 5, "inherent_impact": 4, "response": "mitigate", "rationale": "Historische Beobachtung",
    }
    send(client, "POST", path, 422, headers=admin, json={**data, "residual_likelihood": 2})
    send(client, "POST", path, 422, headers=admin, json={**data, "inherent_likelihood": True})
    send(client, "POST", path, 422, headers=admin, json={**data, "assessed_on": 0})
    send(client, "POST", path, 422, headers=admin, json={**data, "rationale": "Ungültig\x00"})
    first = send(client, "POST", path, 201, headers=admin, json=data).json()
    assert first["inherent_score"] == 20 and first["residual_score"] is None
    second = send(
        client, "POST", path, 201, headers=admin,
        json={**data, "residual_likelihood": 2, "residual_impact": 3},
    ).json()
    assert second["residual_score"] == 6
    history = send(client, "GET", path, 200, headers=admin).json()
    assert [row["id"] for row in history] == [second["id"], first["id"]]
    matrix = send(
        client, "GET", "/risk-matrix", 200, headers=admin, params={"scope": "Nord", "stage": "residual"}
    ).json()
    assert len(matrix["cells"]) == 25
    assert matrix["total"] == 2 and matrix["unassessed"] == 1
    assert next(cell["count"] for cell in matrix["cells"] if (cell["likelihood"], cell["impact"]) == (2, 3)) == 1
    dashboard = send(client, "GET", "/dashboard", 200, headers=admin, params={"scope": "Nord"}).json()
    assert dashboard["risk"] == {"total": 2, "assessed": 1, "unassessed": 1, "stale": 0}
    send(client, "GET", "/dashboard", 422, headers=admin, params={"scope": "Ungültig\x00"})

    send(
        client, "PUT", f"/catalog/{current['id']}", 200, headers=admin,
        json={"expected_version": current["version"], "title": "Geänderter Kontext"},
    )
    send(client, "POST", path, 409, headers=admin, json=data)
    stale = send(client, "GET", "/risk-matrix", 200, headers=admin, params={"scope": "Nord"}).json()
    assert stale["stale"] == 1 and sum(cell["count"] for cell in stale["cells"]) == 0
    send(client, "GET", path, 404, headers=stranger)
    send(
        client, "POST", "/members", 201, headers=admin,
        json={"email": "viewer@example.test", "password": PASSWORD, "role": "viewer"},
    )
    send(client, "POST", path, 403, headers=login(client, slug, "viewer@example.test"), json=data)


@pytest.mark.integration
def test_evidence_upload_download_reuse_and_safe_scope_move(operations_client):
    client, bootstrap_token = operations_client
    _, admin = tenant(client, bootstrap_token)
    _, stranger = tenant(client, bootstrap_token)
    north = record(client, admin, "control", "Kontrolltest")
    north_two = record(client, admin, "control", "Zweiter Kontrolltest")
    measure = record(client, admin, "measure", "Nacharbeit")
    south = record(client, admin, "measure", "Anderer Bereich", "Süd")
    other_kind = record(client, admin, "risk", "Anderer Datensatztyp")
    meta = {"title": "Prüfauszug", "origin": "Systemtest", "scope": "Nord"}
    send(client, "POST", "/evidence", 422, headers=admin, json={
        **meta, "url": "https://docs.example.test/unzulässig", "record_id": south["id"],
    })
    assert send(client, "GET", "/evidence", 200, headers=admin).json() == []
    body = "Auszug mit Umlaut ä\n".encode()
    upload = send(
        client, "POST", "/evidence/upload", 201, headers=admin, data=meta,
        files={"file": ("Prüfung.txt", body, "text/plain")},
    ).json()
    assert upload["sha256"] == hashlib.sha256(body).hexdigest() and upload["size_bytes"] == len(body)
    assert "content" not in upload
    file_path = f"/evidence/{upload['id']}/file"
    download = send(client, "GET", file_path, 200, headers=admin)
    assert download.content == body
    assert download.headers["content-disposition"].startswith("attachment")
    assert download.headers["x-content-type-options"] == "nosniff"
    send(client, "GET", file_path, 404, headers=stranger)
    send(client, "POST", "/evidence", 422, headers=admin, json={**meta, "url": "https://user@docs.example.test"})
    send(
        client, "POST", "/evidence", 422, headers=admin,
        json={**meta, "origin": "Ungültig\x00", "url": "https://docs.example.test/reference"},
    )
    external = send(
        client, "POST", "/evidence", 201, headers=admin, json={**meta, "url": "https://docs.example.test/reference"}
    ).json()
    assert external["sha256"] is None and external["kind"] == "external"
    send(
        client, "POST", "/evidence/upload", 413, headers=admin, data=meta,
        files={"file": ("ZuGross.txt", b"x" * (MAX_FILE_BYTES + 1), "text/plain")},
    )
    links = f"/evidence/{upload['id']}/links"
    for target in (north, north_two, measure, north):
        send(client, "POST", links, 200, headers=admin, json={"record_id": target["id"]})
    send(client, "POST", links, 422, headers=admin, json={"record_id": south["id"]})
    send(client, "POST", links, 404, headers=admin, json={"record_id": other_kind["id"]})
    items = send(client, "GET", "/evidence", 200, headers=admin, params={"scope": "Nord"}).json()
    uploaded = next(item for item in items if item["id"] == upload["id"])
    assert set(uploaded["linked_record_ids"]) == {north["id"], north_two["id"], measure["id"]}
    linked_to_measure = send(client, "GET", "/evidence", 200, headers=admin, params={"record_id": measure["id"]}).json()
    assert [item["id"] for item in linked_to_measure] == [upload["id"]]
    report = send(client, "GET", "/reports/management", 200, headers=admin, params={"scope": "Nord"}).json()
    assert report["evidence"] == {"total": 2, "files": 1, "external": 1, "expired": 0, "controls_without_current": 0}
    move = {"scope": "Süd", "expected_version": north["version"]}
    send(client, "PUT", f"/catalog/{north['id']}", 409, headers=admin, json=move)
    send(client, "DELETE", f"{links}/{north['id']}", 204, headers=admin)
    send(client, "DELETE", f"{links}/{north['id']}", 204, headers=admin)
    send(client, "PUT", f"/catalog/{north['id']}", 200, headers=admin, json=move)


@pytest.mark.integration
def test_privacy_report_reuses_authorized_catalog_data_and_sanitizes_csv(operations_client):
    client, bootstrap_token = operations_client
    slug, admin = tenant(client, bootstrap_token)
    activity = record(client, admin, "processing_activity", "=Testformula", attributes={"purpose": "Synthetische Beschäftigtendaten"})
    record(client, admin, "processing_activity", "Außerhalb des Mandats", "Süd")
    asset = record(client, admin, "asset", "Personalsoftware")
    tom = record(client, admin, "control", "Zugriffsprüfung")
    hidden_control = record(client, admin, "control", "Südprojektdaten", "Süd")
    dpia = record(client, admin, "dpia", "Prüfung Beschäftigungsdaten")
    for relationship, source, target in (
        ("processing_asset", activity, asset), ("processing_control", activity, tom),
        ("processing_control", activity, hidden_control), ("dpia_processing_activity", dpia, activity),
    ):
        send(
            client, "POST", "/links", 201, headers=admin,
            json={"kind": relationship, "from_id": source["id"], "to_id": target["id"], "rationale": "Nachvollziehbare Prüfung"},
        )
    advisor = send(
        client, "POST", "/members", 201, headers=admin,
        json={"email": "advisor@example.test", "password": PASSWORD, "role": "advisor"},
    ).json()
    grant = {"advisor_id": advisor["id"], "expires_at": (datetime.now(UTC) + timedelta(days=1)).isoformat()}
    send(
        client, "POST", "/advisor-mandates", 201, headers=admin,
        json={**grant, "scope": "Nord", "permissions": ["records.read", "exports.read"]},
    )
    send(
        client, "POST", "/advisor-mandates", 201, headers=admin,
        json={**grant, "scope": "Süd", "permissions": ["records.read"]},
    )
    scoped = login(client, slug, "advisor@example.test")
    snapshot = send(client, "GET", "/reports/privacy", 200, headers=scoped).json()
    assert len(snapshot["activities"]) == 1
    actual = snapshot["activities"][0]
    assert actual["id"] == activity["id"] and actual["attributes"] == {"purpose": "Synthetische Beschäftigtendaten"}
    assert {item["id"] for item in actual["relations"]} == {asset["id"], tom["id"], dpia["id"]}
    send(client, "GET", "/reports/privacy", 403, headers=scoped, params={"scope": "Süd"})
    exported = send(
        client, "GET", "/reports/privacy", 200, headers=scoped, params={"scope": "Nord", "format": "csv"}
    )
    content = csv.DictReader(io.StringIO(exported.content.decode("utf-8-sig")), delimiter=";")
    row = next(content)
    assert row["Verarbeitungstätigkeit"] == "'=Testformula"
    assert row["Informationswerte"] == "Personalsoftware" and row["TOM / Controls"] == "Zugriffsprüfung"
    assert list(content) == []
