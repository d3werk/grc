from datetime import date

from fastapi import HTTPException


AI_OPTIONS = {
    "ai_system": {
        "risk_class": ("Ungeprüft", "Gering", "Mittel", "Hoch", "Kritisch"),
        "decision": ("Offen", "Freigegeben", "Mit Auflagen freigegeben", "Abgelehnt", "Ausgesetzt"),
    },
}
AI_FIELDS = {
    "ai_system": (
        ("intended_use", "Bestimmungsgemäße Verwendung", "textarea"),
        ("model_version", "Modell und Version", "text"),
        ("provider", "Anbieter", "text"),
        ("user_groups", "Nutzergruppen", "textarea"),
        ("data_categories", "Eingabe- und Ausgabedaten", "textarea"),
        ("risk_class", "Interne Risikoklasse", "select"),
        ("classification_rationale", "Begründung der Risikoklasse", "textarea"),
        ("regulatory_assessment", "Regulatorische Einordnung und Fundstelle", "textarea"),
        ("human_oversight", "Menschliche Aufsicht und Eingriffsmöglichkeiten", "textarea"),
        ("monitoring_plan", "Betriebsüberwachung, Kennzahlen und Schwellenwerte", "textarea"),
        ("decision", "Einsatzentscheidung", "select"),
        ("decision_by", "Entscheidung verantwortet durch", "text"),
        ("decision_on", "Datum der Entscheidung", "date"),
        ("decision_rationale", "Begründung der Einsatzentscheidung", "textarea"),
        ("decision_conditions", "Auflagen", "textarea"),
        ("last_review_on", "Letzte Überprüfung", "date"),
        ("review_due", "Nächste Überprüfung", "date"),
        ("review_result", "Ergebnis der letzten Überprüfung", "textarea"),
    ),
}


def validate_ai(kind: str, attributes: dict[str, str], status: str, owner: str) -> None:
    if kind != "ai_system":
        return
    risk_class = attributes.get("risk_class", "Ungeprüft")
    decision = attributes.get("decision", "Offen")

    def present(*fields: str) -> bool:
        return all(attributes.get(field, "").strip() for field in fields)

    has_classification = risk_class != "Ungeprüft" and present("classification_rationale")
    approved = decision in {"Freigegeben", "Mit Auflagen freigegeben"}
    if risk_class != "Ungeprüft" and not has_classification:
        raise HTTPException(422, "Begründen Sie die gewählte interne Risikoklasse.")
    if status in {"in_review", "active"} and not (owner.strip() and present("intended_use")):
        raise HTTPException(422, "Für die Prüfung sind Verantwortung und bestimmungsgemäße Verwendung erforderlich.")
    if decision != "Offen" and not present("decision_by", "decision_on", "decision_rationale"):
        raise HTTPException(422, "Dokumentieren Sie für die Einsatzentscheidung Verantwortung, Datum und Begründung.")
    if decision == "Mit Auflagen freigegeben" and not present("decision_conditions"):
        raise HTTPException(422, "Dokumentieren Sie die Auflagen für einen bedingt freigegebenen Einsatz.")
    if approved and not (
        owner.strip()
        and has_classification
        and present("intended_use", "human_oversight", "monitoring_plan", "review_due")
    ):
        raise HTTPException(
            422,
            "Für eine Freigabe sind Verantwortung, Verwendungszweck, begründete Risikoklasse, "
            "Aufsicht, Betriebsüberwachung und nächster Prüftermin erforderlich.",
        )
    if status == "active" and not approved:
        raise HTTPException(422, "Für ein aktives KI-System ist eine dokumentierte Freigabe erforderlich.")
    if present("last_review_on") != present("review_result"):
        raise HTTPException(422, "Hinterlegen Sie das Datum und das Ergebnis der letzten Überprüfung gemeinsam.")
    if present("review_due"):
        review_due = date.fromisoformat(attributes["review_due"])
        if any(
            date.fromisoformat(attributes[field]) > review_due
            for field in ("decision_on", "last_review_on") if present(field)
        ):
            raise HTTPException(422, "Der nächste Prüftermin darf nicht vor Entscheidung oder letzter Überprüfung liegen.")
