from fastapi import HTTPException


CONTRACT_FIELDS = {
    "contract": (
        ("contract_type", "Vertragsart", "select"),
        ("internal_party", "Interne Vertragspartei", "text"),
        ("counterparty", "Externe Vertragspartei", "text"),
        ("signed_on", "Unterzeichnet am", "date"),
        ("starts_on", "Vertragsbeginn", "date"),
        ("ends_on", "Ende der aktuellen Laufzeit", "date"),
        ("notice_due", "Nächster Kündigungsstichtag", "date"),
        ("review_due", "Nächster Prüftermin", "date"),
        ("terminated_on", "Wirksam beendet am", "date"),
        ("renewal_mode", "Verlängerungsart", "select"),
        ("decision", "Entscheidung zur Fortführung", "select"),
        ("renewal_terms", "Verlängerungs- und Kündigungsbedingungen", "textarea"),
        ("decision_note", "Begründung und nächster Schritt", "textarea"),
        ("obligations", "Pflichten und Abnahmekriterien", "textarea"),
        ("document_reference", "Fundstelle des Vertragsdokuments", "text"),
    ),
}

CONTRACT_OPTIONS = {
    "contract": {
        "contract_type": (
            "Dienstleistung", "Lieferung", "Lizenz oder Abonnement", "Auftragsverarbeitung",
            "Vertraulichkeit", "Rahmenvertrag", "Sonstiger Vertrag",
        ),
        "renewal_mode": ("Keine Verlängerung", "Durch Vereinbarung", "Automatisch", "Unbefristet"),
        "decision": ("Offen", "Verlängern", "Neu verhandeln", "Kündigen", "Auslaufen lassen"),
    },
}


def validate_contract(kind: str, attrs: dict[str, str], status: str, owner: str) -> None:
    if kind != "contract":
        return
    start, end = attrs.get("starts_on"), attrs.get("ends_on")
    if start and end and end < start:
        raise HTTPException(422, "Das Ende der aktuellen Laufzeit muss am oder nach dem Vertragsbeginn liegen.")
    if status == "active" and not (owner.strip() and attrs.get("internal_party", "").strip() and attrs.get("counterparty", "").strip()):
        raise HTTPException(422, "Für einen aktiven Vertrag sind Verantwortung und beide Vertragsparteien erforderlich.")
    if attrs.get("decision") not in (None, "", "Offen") and not attrs.get("decision_note", "").strip():
        raise HTTPException(422, "Dokumentieren Sie die Begründung und den nächsten Schritt zur Fortführungsentscheidung.")
