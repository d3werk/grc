"""Fields and completion checks for operational resilience and continuity."""

from collections.abc import Mapping

from fastapi import HTTPException


RESILIENCE_FIELDS = {
    "resilience_service": (
        ("business_impact", "Auswirkungen eines Ausfalls", "textarea"),
        ("tolerance_minutes", "Maximal tolerierbare Unterbrechung (Minuten)", "number"),
        ("tolerance_criteria", "Weitere Toleranzgrenzen und Begründung", "textarea"),
        ("affected_parties", "Betroffene und zugesagte Mindestleistung", "textarea"),
        ("dependency_notes", "Personal, Standorte und weitere Abhängigkeiten", "textarea"),
        ("next_review_on", "Nächste Überprüfung", "date"),
    ),
    "resilience_scenario": (
        ("scenario", "Störungsszenario", "textarea"),
        ("exercise_type", "Art des Szenariotests", "select"),
        ("test_objectives", "Prüfziele und Erfolgskriterien", "textarea"),
        ("scheduled_on", "Geplanter Testtermin", "date"),
        ("performed_on", "Durchgeführt am", "date"),
        ("tolerance_snapshot_minutes", "Toleranz zum Prüfzeitpunkt (Minuten)", "number"),
        ("disruption_minutes", "Beobachtete Unterbrechung (Minuten)", "number"),
        ("result", "Gesamturteil", "select"),
        ("findings", "Feststellungen und Verbesserungsbedarf", "textarea"),
    ),
    "continuity_bia": (
        ("assessed_on", "Bewertet am", "date"),
        ("criticality", "Kritikalität", "select"),
        ("impact_timeline", "Auswirkungen über die Zeit", "textarea"),
        ("minimum_service", "Mindestbetrieb und erforderliche Ressourcen", "textarea"),
        ("mtpd_minutes", "Maximal tolerierbare Ausfallzeit (MTPD, Minuten)", "number"),
        ("rto_minutes", "Zielzeit für den Wiederanlauf (RTO, Minuten)", "number"),
        ("rpo_minutes", "Maximal tolerierbarer Datenverlust (RPO, Minuten)", "number"),
        ("target_rationale", "Herleitung der Wiederanlaufziele", "textarea"),
        ("next_review_on", "Nächste Überprüfung", "date"),
    ),
    "continuity_plan": (
        ("activation", "Auslöser und Entscheidung zur Aktivierung", "textarea"),
        ("immediate_actions", "Sofortmaßnahmen", "textarea"),
        ("roles_contacts", "Rollen, Vertretung und Kontaktwege", "textarea"),
        ("escalation", "Eskalation und Krisenkommunikation", "textarea"),
        ("recovery_steps", "Wiederanlauf in Reihenfolge", "textarea"),
        ("return_to_normal", "Prüfung und Rückkehr zum Regelbetrieb", "textarea"),
        ("released_on", "Freigegeben am", "date"),
        ("next_review_on", "Nächste Überprüfung", "date"),
    ),
    "continuity_exercise": (
        ("exercise_type", "Übungsform", "select"),
        ("scenario", "Übungsszenario", "textarea"),
        ("test_objectives", "Übungsziele und Erfolgskriterien", "textarea"),
        ("scheduled_on", "Geplanter Übungstermin", "date"),
        ("performed_on", "Durchgeführt am", "date"),
        ("rto_snapshot_minutes", "Wiederanlaufziel zur Übung (Minuten)", "number"),
        ("recovery_minutes", "Beobachteter Wiederanlauf (Minuten)", "number"),
        ("result", "Gesamturteil", "select"),
        ("findings", "Feststellungen und Verbesserungsbedarf", "textarea"),
    ),
}

_EXERCISE_TYPES = ("Planspiel", "Technischer Test", "Simulation", "Vollübung")
_RESULTS = ("Ziele erreicht", "Ziele teilweise erreicht", "Ziele verfehlt", "Nicht auswertbar")
RESILIENCE_OPTIONS = {
    "resilience_scenario": {"exercise_type": _EXERCISE_TYPES, "result": _RESULTS},
    "continuity_bia": {"criticality": ("Kritisch", "Wesentlich", "Übrige")},
    "continuity_exercise": {"exercise_type": _EXERCISE_TYPES, "result": _RESULTS},
}

_REQUIRED_FIELDS = {
    ("resilience_service", "active"): ("business_impact", "tolerance_minutes", "tolerance_criteria"),
    ("resilience_scenario", "completed"): ("scenario", "test_objectives", "performed_on", "result", "findings"),
    ("continuity_bia", "active"): ("assessed_on", "criticality", "impact_timeline", "mtpd_minutes", "rto_minutes"),
    ("continuity_plan", "active"): ("activation", "roles_contacts", "recovery_steps", "released_on"),
    ("continuity_exercise", "completed"): ("scenario", "test_objectives", "performed_on", "result", "findings"),
}

_DURATION_PAIRS = {
    "resilience_scenario": ("tolerance_snapshot_minutes", "disruption_minutes"),
    "continuity_exercise": ("rto_snapshot_minutes", "recovery_minutes"),
}


def validate_resilience(kind: str, attrs: Mapping[str, str], status: str) -> None:
    fields = RESILIENCE_FIELDS.get(kind)
    if fields is None:
        return
    labels = {key: label for key, label, _ in fields}
    for key, label, field_type in fields:
        value = attrs.get(key, "")
        if field_type == "number" and value:
            if not value.isascii() or not value.isdecimal() or len(value) > 7:
                raise HTTPException(422, f"{label} benötigt eine nichtnegative ganze Minutenzahl mit höchstens sieben Stellen.")
    missing = [labels[key] for key in _REQUIRED_FIELDS.get((kind, status), ()) if not attrs.get(key, "").strip()]
    if missing:
        raise HTTPException(422, "Für diesen Bearbeitungsstand bitte ergänzen: " + ", ".join(missing) + ".")
    review_start_key = {"continuity_bia": "assessed_on", "continuity_plan": "released_on"}.get(kind)
    if review_start_key and attrs.get(review_start_key) and attrs.get("next_review_on"):
        if attrs["next_review_on"] < attrs[review_start_key]:
            raise HTTPException(422, "Die nächste Überprüfung darf zeitlich nicht vor dem Ausgangsdatum liegen.")
    if kind == "continuity_bia" and "mtpd_minutes" in attrs and "rto_minutes" in attrs:
        if int(attrs["rto_minutes"]) > int(attrs["mtpd_minutes"]):
            raise HTTPException(422, "Die Wiederanlaufzeit (RTO) darf die maximal tolerierbare Ausfallzeit (MTPD) nicht überschreiten.")
    if kind in _DURATION_PAIRS and status == "completed":
        limit_key, duration_key = _DURATION_PAIRS[kind]
        if bool(attrs.get(limit_key)) != bool(attrs.get(duration_key)):
            raise HTTPException(422, "Für den Zeitvergleich bitte Zielzeit und beobachtete Dauer gemeinsam erfassen.")
        if kind == "resilience_scenario" and attrs.get("result") != "Nicht auswertbar" and not attrs.get(limit_key):
            raise HTTPException(422, "Für einen auswertbaren Szenariotest sind Zeittoleranz und beobachtete Unterbrechung erforderlich.")
        if attrs.get(limit_key) and int(attrs[duration_key]) > int(attrs[limit_key]) and attrs.get("result") == "Ziele erreicht":
            raise HTTPException(422, "Bei überschrittener Zielzeit bitte ein passendes Gesamturteil wählen.")
