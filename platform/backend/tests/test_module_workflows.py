import json
from datetime import UTC, datetime, timedelta

import pytest

from test_platform_flow import PASSWORD, create_tenant, platform, request

pytestmark = pytest.mark.integration


def make_entry(client, admin, kind, title, expected=201, **fields):
    payload = {"kind": kind, "title": title, "scope": "Betrieb"} | fields
    return request(client, "POST", "/catalog", expected, headers=admin, json=payload).json()


def make_link(client, admin, kind, source, target, expected=201):
    return request(client, "POST", "/links", expected, headers=admin, json={
        "kind": kind, "from_id": source["id"], "to_id": target["id"], "rationale": "Fachlicher Testbezug.",
    }).json()


def test_scope_options_from_registry_and_legacy_data_respect_mandates(platform):
    client, token = platform
    admin = create_tenant(client, token)
    profile = request(client, "GET", "/auth/me", 200, headers=admin).json()
    request(client, "POST", "/scopes", 201, headers=admin, json={"scope": "Neue Auswahl"})
    request(client, "POST", "/scopes", 201, headers=admin, json={"scope": "Neue Auswahl"})
    make_entry(client, admin, "asset", "Altbestand", scope="Bisheriger Freitext")
    request(client, "POST", "/evidence", 201, headers=admin, json={
        "scope": "Nur Nachweise", "title": "Alte Ablage", "origin": "Historischer Bestand", "url": "https://example.invalid/",
    })
    scopes = request(client, "GET", "/scopes", 200, headers=admin).json()
    assert set(scopes["scopes"]) == {"Neue Auswahl", "Bisheriger Freitext", "Nur Nachweise"}
    assert scopes["can_create"] and scopes["catalog_write"] is None
    outsider = create_tenant(client, token)
    assert request(client, "GET", "/scopes", 200, headers=outsider).json()["scopes"] == []

    advisor = request(client, "POST", "/members", 201, headers=admin, json={
        "email": "scope-advisor@example.test", "password": PASSWORD, "role": "advisor",
    }).json()
    request(client, "POST", "/advisor-mandates", 201, headers=admin, json={
        "advisor_id": advisor["id"], "scope": "Neue Auswahl",
        "permissions": ["records.read", "records.write"], "expires_at": (datetime.now(UTC) + timedelta(days=7)).isoformat(),
    })
    login = request(client, "POST", "/auth/login", 200, json={
        "tenant_slug": profile["tenant_slug"], "email": "scope-advisor@example.test", "password": PASSWORD,
    }).json()
    restricted = {"Authorization": f"Bearer {login['access_token']}"}
    allowed = request(client, "GET", "/scopes", 200, headers=restricted).json()
    assert allowed["scopes"] == allowed["catalog_write"] == ["Neue Auswahl"]
    assert not allowed["can_create"]
    request(client, "POST", "/scopes", 403, headers=restricted, json={"scope": "Fremder Bereich"})


def test_process_hierarchy_and_ordered_steps(platform):
    client, token = platform
    admin = create_tenant(client, token)
    parent = make_entry(client, admin, "business_process", "Anfragen bearbeiten")
    child = make_entry(client, admin, "business_process", "Anfrage klassifizieren", attributes={
        "process_steps": json.dumps([{"title": "Prüfen", "role": "Service"}, {"title": "Rückmelden"}]),
    })
    assert [step["title"] for step in json.loads(child["attributes"]["process_steps"])] == ["Prüfen", "Rückmelden"]
    make_entry(client, admin, "business_process", "Unvollständig", 422, attributes={"process_steps": '[{"role":"IT"}]'})
    other = make_entry(client, admin, "business_process", "Anderer Standort", scope="Standort B")
    make_link(client, admin, "process_parent", child, other, 422)
    link = make_link(client, admin, "process_parent", child, parent)
    make_link(client, admin, "process_parent", parent, child, 422)
    child_path = f"/catalog/{child['id']}"
    move = {"expected_version": child["version"], "scope": "Standort B"}
    request(client, "PUT", child_path, 409, headers=admin, json=move)
    request(client, "DELETE", f"/links/{link['id']}", 204, headers=admin, json={
        "expected_version": link["version"], "reason": "Neue Prozessstruktur.",
    })
    request(client, "PUT", child_path, 200, headers=admin, json=move)


def test_audit_close_and_finding_verification(platform):
    client, token = platform
    admin = create_tenant(client, token)
    subject = make_entry(client, admin, "audit_subject", "Freigabeverfahren")
    engagement_fields = {
        "plan_year": "2026", "priority": "Hoch", "starts_on": "2026-01-20", "ends_on": "2026-02-01",
        "audit_scope": "Synthetischer Ablauf vom Antrag bis zur Freigabe", "objective": "Wirksamkeit der Freigabe prüfen",
        "criteria": "Freigabe muss vor Durchführung vorliegen.", "test_program": "Zehn Tickets stichprobenartig vergleichen.",
    }
    audit = make_entry(client, admin, "audit_engagement", "Interner Auftrag", status="active", attributes=engagement_fields)
    make_link(client, admin, "audit_engagement_subject", audit, subject)
    test = make_entry(client, admin, "audit_test", "Freigabezeitpunkt vergleichen", status="in_review", attributes={
        "criterion": "Freigabe erfolgt vor Ausführung.", "procedure": "Antrags- und Freigabezeitstempel vergleichen.",
        "tested_on": "2026-01-25", "result": "Abweichung", "workpaper": "In einem Testdatensatz fehlt die Freigabe.",
    })
    make_link(client, admin, "audit_test_engagement", test, audit)
    finding_fields = {
        "severity": "Hoch", "criterion": "Freigabe protokolliert", "observation": "Eine Freigabe fehlt",
        "recommendation": "Freigabeschritt verpflichtend konfigurieren.", "response": "Der Fachbereich ergänzt den Freigabeschritt.",
        "due_on": "2026-03-01",
    }
    finding = make_entry(client, admin, "audit_finding", "Fehlende Freigabe", status="in_review", attributes=finding_fields)
    make_link(client, admin, "audit_finding_test", finding, test)
    measure = make_entry(client, admin, "measure", "Freigabeschritt ergänzen")
    make_link(client, admin, "audit_finding_measure", finding, measure)
    finding_path = f"/catalog/{finding['id']}"
    finish = {"expected_version": finding["version"], "status": "completed"}
    request(client, "PUT", finding_path, 422, headers=admin, json=finish)
    request(client, "PUT", finding_path, 200, headers=admin, json=finish | {
        "attributes": finding_fields | {"verification": "Neue Stichprobe vollständig.", "verified_on": "2026-02-23"},
    })
    report = {"expected_version": audit["version"], "status": "completed", "attributes": engagement_fields | {
        "conclusion": "Eine Feststellung wurde bearbeitet.", "reported_on": "2026-02-24",
    }}
    path = f"/catalog/{audit['id']}"
    request(client, "PUT", path, 422, headers=admin, json=report)
    report["attributes"]["quality_review"] = "Bericht fachlich durchgesehen und freigegeben."
    request(client, "PUT", path, 200, headers=admin, json=report)


def test_resilience_and_bcm_quantitative_targets(platform):
    client, token = platform
    admin = create_tenant(client, token)
    make_entry(client, admin, "resilience_service", "Teilminuten unzulässig", 422,
               attributes={"tolerance_minutes": "1.5"})
    service = make_entry(client, admin, "resilience_service", "Kundendienst", status="active", attributes={
        "business_impact": "Anfragen können nicht bearbeitet werden.", "tolerance_minutes": "90",
        "tolerance_criteria": "Spätestens nach 90 Minuten wieder für Anfragen erreichbar.",
    })
    scenario_fields = {
        "scenario": "Synthetischer Ausfall des Eingangskanals", "test_objectives": "Zeit bis zur Wiederaufnahme messen.",
        "performed_on": "2026-02-23", "tolerance_snapshot_minutes": "90", "disruption_minutes": "100",
        "result": "Ziele erreicht", "findings": "Ersatzkanal war nach 100 Minuten verfügbar.",
    }
    make_entry(client, admin, "resilience_scenario", "Falsches Urteil", 422, status="completed", attributes=scenario_fields)
    scenario = make_entry(client, admin, "resilience_scenario", "Unterbrechungsprobe", status="completed",
                          attributes=scenario_fields | {"result": "Ziele verfehlt"})
    make_link(client, admin, "resilience_scenario_service", scenario, service)
    bia_fields = {"mtpd_minutes": "90", "rto_minutes": "120", "rpo_minutes": "5"}
    make_entry(client, admin, "continuity_bia", "Unplausibles Wiederanlaufziel", 422, attributes=bia_fields)
    bia = make_entry(client, admin, "continuity_bia", "Auswirkungsanalyse",
                     attributes=bia_fields | {"rto_minutes": "60"})
    make_link(client, admin, "continuity_bia_service", bia, service)
    plan = make_entry(client, admin, "continuity_plan", "Ausweichbetrieb")
    make_link(client, admin, "continuity_plan_bia", plan, bia)
    exercise = make_entry(client, admin, "continuity_exercise", "Ausweichbetrieb üben")
    make_link(client, admin, "continuity_exercise_plan", exercise, plan)
