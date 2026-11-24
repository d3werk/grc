import pytest

from test_platform_flow import create_tenant, platform, request


pytestmark = pytest.mark.integration


def test_ai_system_review_approval_and_shared_controls(platform):
    client, token = platform
    admin = create_tenant(client, token)
    outsider = create_tenant(client, token)
    scope = "Synthetisches KI-Pilotprojekt"

    def entry(kind, title, **values):
        return request(
            client, "POST", "/catalog", 201, headers=admin,
            json={"kind": kind, "title": title, "scope": scope, **values},
        ).json()

    system = entry("ai_system", "Assistent für interne Wissenssuche")
    path = f"/catalog/{system['id']}"
    request(client, "PUT", path, 422, headers=admin, json={
        "expected_version": system["version"], "status": "active",
    })
    review_fields = {
        "intended_use": "Interne Arbeitsanweisungen auffindbar machen",
        "model_version": "Beispielmodell 1.2",
        "provider": "Synthetischer Anbieter",
        "user_groups": "Interner IT-Support",
        "data_categories": "Geprüfte Arbeitsanweisungen",
        "risk_class": "Mittel",
        "classification_rationale": "Antworten werden zur Unterstützung angezeigt und vor Nutzung geprüft.",
        "regulatory_assessment": "Fachliche Einordnung im Pilot zur Prüfung dokumentiert.",
    }
    in_review = request(client, "PUT", path, 200, headers=admin, json={
        "expected_version": system["version"], "status": "in_review", "owner": "IT-Serviceleitung",
        "attributes": review_fields,
    }).json()

    approved_fields = review_fields | {
        "decision": "Mit Auflagen freigegeben",
        "decision_by": "Pilotverantwortung",
        "decision_on": "2026-11-01",
        "decision_rationale": "Beschränkt auf die freigegebene interne Dokumentation.",
        "monitoring_plan": "Stichprobe von 20 Antworten je Monat. Prüfung ab zwei falschen Referenzen.",
        "review_due": "2026-12-01",
    }
    oversight = {"type": "doc", "content": [{
        "type": "paragraph", "content": [{
            "type": "text", "text": "Antworten vor der Übernahme prüfen.", "marks": [{"type": "bold"}],
        }],
    }]}
    approval = {
        "expected_version": in_review["version"], "status": "active", "attributes": approved_fields,
        "rich_text": {"human_oversight": oversight},
    }
    request(client, "PUT", path, 422, headers=admin, json=approval)
    applied = request(client, "PUT", path, 200, headers=admin, json=approval | {
        "attributes": approved_fields | {"decision_conditions": "Zugang auf das Pilotteam begrenzen."},
    }).json()
    assert applied["attributes"]["human_oversight"] == "Antworten vor der Übernahme prüfen."
    assert applied["rich_text"]["human_oversight"] == oversight
    request(client, "PUT", path, 409, headers=admin, json=approval)

    control = entry("control", "Menschliche Antwortprüfung")
    request(client, "POST", "/links", 201, headers=admin, json={
        "kind": "ai_system_control", "from_id": system["id"], "to_id": control["id"],
        "rationale": "Fachliche Aufsicht im Pilotbetrieb",
    })
    relations = request(client, "GET", "/links", 200, headers=admin, params={"record_id": system["id"]}).json()
    assert {link["to_id"] for link in relations} == {control["id"]}

    evidence = request(client, "POST", "/evidence", 201, headers=admin, json={
        "title": "Synthetische Freigabenotiz", "origin": "Prüfprotokoll", "scope": scope,
        "url": "https://example.test/ki-freigabe",
    }).json()
    request(client, "POST", f"/evidence/{evidence['id']}/links", 200, headers=admin, json={"record_id": system["id"]})
    request(client, "GET", path, 404, headers=outsider)
    request(client, "GET", "/links", 404, headers=outsider, params={"record_id": system["id"]})

    live = request(client, "GET", path, 200, headers=admin).json()
    assert live["status"] == "active" and live["attributes"]["review_due"] == "2026-12-01"
    catalog = request(client, "GET", "/catalog", 200, headers=admin, params={"kind": "ai_system", "scope": scope}).json()
    assert [record["id"] for record in catalog] == [system["id"]]
