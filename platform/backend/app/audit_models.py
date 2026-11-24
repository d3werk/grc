from fastapi import HTTPException


AUDIT_FIELDS = {
    "audit_subject": (
        ("subject_category", "Art der Prüfeinheit", "select"),
        ("inherent_risk", "Inhärentes Risiko (1–5)", "number"),
        ("residual_risk", "Restrisiko (1–5)", "number"),
        ("materiality", "Wesentlichkeit (1–5)", "number"),
        ("risk_rationale", "Begründung der Einstufung", "textarea"),
        ("cycle_months", "Prüfzyklus in Monaten", "number"),
        ("last_audit_on", "Letzte Prüfung", "date"),
        ("next_audit_on", "Nächste Prüfung", "date"),
    ),
    "audit_engagement": (
        ("plan_year", "Planjahr", "number"),
        ("priority", "Priorität", "select"),
        ("starts_on", "Geplanter Prüfungsbeginn", "date"),
        ("ends_on", "Geplanter Prüfungsabschluss", "date"),
        ("budget_hours", "Stundenbudget", "number"),
        ("actual_hours", "Aufgewendete Stunden", "number"),
        ("audit_scope", "Prüfungsumfang und Abgrenzung", "textarea"),
        ("objective", "Prüfungsziel", "textarea"),
        ("criteria", "Prüfkriterien und Quellenstand", "textarea"),
        ("test_program", "Prüfprogramm und Stichprobenansatz", "textarea"),
        ("conclusion", "Berichtsfazit und wesentliche Ergebnisse", "textarea"),
        ("management_response", "Stellungnahme der geprüften Stelle", "textarea"),
        ("quality_review", "Qualitätssicherung und Berichtsfreigabe", "textarea"),
        ("reported_on", "Bericht freigegeben am", "date"),
    ),
    "audit_test": (
        ("criterion", "Prüfkriterium", "textarea"),
        ("procedure", "Prüfschritt und Stichprobe", "textarea"),
        ("tested_on", "Ausgeführt am", "date"),
        ("result", "Prüfergebnis", "select"),
        ("workpaper", "Arbeitspapier und Ergebnisbegründung", "textarea"),
    ),
    "audit_finding": (
        ("severity", "Schweregrad", "select"),
        ("finding_category", "Art der Feststellung", "select"),
        ("criterion", "Sollzustand", "textarea"),
        ("observation", "Beobachtung und Auswirkung", "textarea"),
        ("root_cause", "Ursache", "textarea"),
        ("recommendation", "Empfehlung", "textarea"),
        ("response", "Stellungnahme und vereinbarte Umsetzung", "textarea"),
        ("due_on", "Frist für die Umsetzung", "date"),
        ("verification", "Wirksamkeitsprüfung und Ergebnis", "textarea"),
        ("verified_on", "Wirksamkeit geprüft am", "date"),
    ),
}

AUDIT_OPTIONS = {
    "audit_subject": {
        "subject_category": ("Organisationseinheit", "Geschäftsprozess", "IT-Dienst", "Querschnittsthema"),
    },
    "audit_engagement": {"priority": ("Kritisch", "Hoch", "Mittel", "Niedrig")},
    "audit_test": {"result": ("Bestanden", "Abweichung", "Nicht anwendbar")},
    "audit_finding": {
        "severity": ("Kritisch", "Hoch", "Mittel", "Niedrig"),
        "finding_category": ("Designmangel", "Durchführungsmangel", "Dokumentationsmangel", "Verbesserungspotenzial"),
    },
}

_NUMBER_LIMITS = {
    "inherent_risk": (1, 5),
    "residual_risk": (1, 5),
    "materiality": (1, 5),
    "cycle_months": (1, 120),
    "plan_year": (1900, 9999),
    "budget_hours": (0, 100_000),
    "actual_hours": (0, 100_000),
}

_REQUIRED_BY_STATUS = {
    "audit_subject": (
        ({"active", "in_review", "completed"}, ("subject_category", "inherent_risk", "materiality", "risk_rationale", "cycle_months")),
    ),
    "audit_engagement": (
        ({"active", "in_review", "completed"}, ("plan_year", "priority", "starts_on", "ends_on", "audit_scope", "objective", "criteria", "test_program")),
        ({"in_review", "completed"}, ("conclusion",)),
        ({"completed"}, ("quality_review", "reported_on")),
    ),
    "audit_test": (
        ({"active", "in_review", "completed"}, ("criterion", "procedure")),
        ({"in_review", "completed"}, ("tested_on", "result", "workpaper")),
    ),
    "audit_finding": (
        ({"active", "in_review", "completed"}, ("severity", "criterion", "observation", "recommendation", "due_on")),
        ({"in_review", "completed"}, ("response",)),
        ({"completed"}, ("verification", "verified_on")),
    ),
}

_DATE_PAIRS = {
    "audit_subject": (("last_audit_on", "next_audit_on"),),
    "audit_engagement": (("starts_on", "ends_on"), ("starts_on", "reported_on")),
}


def validate_audit(kind: str, attrs: dict[str, str], status: str) -> None:
    labels = {key: label for key, label, _ in AUDIT_FIELDS[kind]}
    for key in labels.keys() & _NUMBER_LIMITS.keys():
        value = attrs.get(key, "")
        if not value:
            continue
        minimum, maximum = _NUMBER_LIMITS[key]
        if not (value.isascii() and value.isdecimal() and len(value) <= len(str(maximum))
                and minimum <= int(value) <= maximum):
            raise HTTPException(422, f"{labels[key]} muss eine ganze Zahl zwischen {minimum} und {maximum} sein.")

    for first, second in _DATE_PAIRS.get(kind, ()):
        if attrs.get(first) and attrs.get(second) and attrs[first] > attrs[second]:
            raise HTTPException(422, f"{labels[second]} darf zeitlich nicht vor „{labels[first]}“ liegen.")

    missing = [labels[key] for statuses, keys in _REQUIRED_BY_STATUS[kind] if status in statuses
               for key in keys if not attrs.get(key, "").strip()]
    if missing:
        raise HTTPException(422, "Für diesen Bearbeitungsstand fehlen Pflichtangaben. " + ", ".join(missing) + ".")
