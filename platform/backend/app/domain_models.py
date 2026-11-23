from datetime import date, datetime
from typing import Annotated, Literal
from uuid import UUID

from fastapi import HTTPException
from pydantic import BaseModel, ConfigDict, Field

CatalogKind = Literal[
    "org_unit", "asset", "it_service", "business_process", "risk", "control", "measure",
    "framework_version", "requirement", "processing_activity", "supplier", "dpia",
]
CatalogStatus = Literal["draft", "active", "in_review", "completed", "archived"]
DatabaseText = Annotated[str, Field(pattern=r"^[^\x00]*$")]
Title = Annotated[DatabaseText, Field(min_length=1, max_length=300)]
Code = Annotated[DatabaseText, Field(max_length=80)]
Description = Annotated[DatabaseText, Field(max_length=30000)]
Owner = Annotated[DatabaseText, Field(max_length=240)]
Scope = Annotated[DatabaseText, Field(max_length=240)]
Rationale = Annotated[DatabaseText, Field(min_length=1, max_length=5000)]
Version = Annotated[int, Field(ge=1, strict=True)]
Attributes = dict[str, Annotated[DatabaseText, Field(max_length=5000)]]

KIND_LABELS = {
    "org_unit": "Organisationseinheit",
    "asset": "Informationswert",
    "it_service": "IT-Dienst",
    "business_process": "Geschäftsprozess",
    "risk": "Risiko",
    "control": "Control",
    "measure": "Maßnahme",
    "framework_version": "Regelwerksfassung",
    "requirement": "Anforderung",
    "processing_activity": "Verarbeitungstätigkeit",
    "supplier": "Lieferant",
    "dpia": "Datenschutz-Folgenabschätzung",
}
STATUS_LABELS = {
    "draft": "Entwurf",
    "active": "Aktiv",
    "in_review": "In Prüfung",
    "completed": "Abgeschlossen",
    "archived": "Archiviert",
}


class AttributeField(BaseModel):
    key: str
    label: str
    type: Literal["text", "textarea", "date"]


ATTRIBUTE_FIELDS: dict[str, tuple[tuple[str, str, str], ...]] = {
    "org_unit": (
        ("unit_type", "Art der Einheit", "text"),
        ("external_reference", "Referenz im Organigramm", "text"),
    ),
    "asset": (
        ("asset_type", "Art des Informationswerts", "text"),
        ("classification", "Schutzklasse", "text"),
        ("location", "Ablageort oder Standort", "text"),
    ),
    "it_service": (
        ("service_type", "Art des Dienstes", "text"),
        ("criticality", "Kritikalität", "text"),
        ("service_level", "Vereinbartes Leistungsniveau", "textarea"),
        ("recovery_target", "Wiederanlaufziel", "text"),
    ),
    "business_process": (
        ("purpose", "Zweck des Geschäftsprozesses", "textarea"),
        ("criticality", "Kritikalität", "text"),
        ("recovery_target", "Wiederanlaufziel", "text"),
    ),
    "risk": (
        ("scenario", "Risikoszenario", "textarea"),
        ("consequence", "Mögliche Auswirkungen", "textarea"),
    ),
    "control": (
        ("control_objective", "Kontrollziel", "textarea"),
        ("control_frequency", "Durchführungs- oder Prüfintervall", "text"),
    ),
    "measure": (
        ("target_date", "Zieltermin", "date"),
        ("success_criterion", "Erfolgskriterium", "textarea"),
    ),
    "framework_version": (
        ("framework", "Name des Regelwerks", "text"),
        ("edition", "Ausgabe oder Versionsbezeichnung", "text"),
        ("publication_date", "Veröffentlichungsdatum", "date"),
        ("source_reference", "Quelle oder Fundstelle", "text"),
    ),
    "requirement": (
        ("reference", "Fundstelle im Regelwerk", "text"),
        ("applicability", "Bewertung der Anwendbarkeit", "textarea"),
    ),
    "processing_activity": (
        ("controller", "Verantwortliche Stelle", "text"),
        ("purpose", "Zweck der Verarbeitung", "textarea"),
        ("data_subjects", "Kategorien betroffener Personen", "textarea"),
        ("personal_data_categories", "Kategorien personenbezogener Daten", "textarea"),
        ("recipients", "Empfänger oder Empfängerkategorien", "textarea"),
        ("transfers", "Drittlandübermittlungen und Garantien", "textarea"),
        ("retention", "Aufbewahrungs- und Löschregeln", "textarea"),
        ("technical_measures", "Technische und organisatorische Maßnahmen", "textarea"),
        ("legal_basis", "Rechtsgrundlage", "textarea"),
    ),
    "supplier": (
        ("legal_name", "Juristische Bezeichnung", "text"),
        ("criticality", "Kritikalität", "text"),
        ("agreement_reference", "Vertragsreferenz", "text"),
        ("review_due", "Nächster Prüftermin", "date"),
    ),
    "dpia": (
        ("screening_reason", "Anlass und Prüferfordernis", "textarea"),
        ("necessity", "Erforderlichkeit und Verhältnismäßigkeit", "textarea"),
        ("risks_to_individuals", "Risiken für Rechte und Freiheiten", "textarea"),
        ("safeguards", "Schutzmaßnahmen", "textarea"),
        ("review_result", "Bewertung des verbleibenden Risikos und Entscheidung", "textarea"),
    ),
}
DPIA_KEYS = tuple(field[0] for field in ATTRIBUTE_FIELDS["dpia"])


class CatalogKindInfo(BaseModel):
    kind: CatalogKind
    label: str
    attribute_fields: list[AttributeField]


class CatalogStatusInfo(BaseModel):
    status: CatalogStatus
    label: str


class RequestModel(BaseModel):
    model_config = ConfigDict(extra="forbid", str_strip_whitespace=True)


class CatalogCreate(RequestModel):
    kind: CatalogKind
    title: Title
    code: Code = ""
    description: Description = ""
    status: CatalogStatus = "draft"
    owner: Owner = ""
    scope: Scope = ""
    org_unit_id: UUID | None = None
    attributes: Attributes = Field(default_factory=dict)


class CatalogUpdate(RequestModel):
    expected_version: Version
    title: Title | None = None
    code: Code | None = None
    description: Description | None = None
    status: CatalogStatus | None = None
    owner: Owner | None = None
    scope: Scope | None = None
    org_unit_id: UUID | None = None
    attributes: Attributes | None = None


class CatalogRecord(BaseModel):
    id: UUID
    kind: CatalogKind
    code: str
    title: str
    description: str
    status: CatalogStatus
    owner: str
    scope: str
    org_unit_id: UUID | None
    attributes: Attributes
    version: Version
    created_at: datetime
    updated_at: datetime


class CatalogLinkType(BaseModel):
    kind: str
    from_kind: CatalogKind
    to_kind: CatalogKind
    label: str


class CatalogLinkCreate(RequestModel):
    kind: Annotated[DatabaseText, Field(min_length=1, max_length=80)]
    from_id: UUID
    to_id: UUID
    rationale: Rationale


class CatalogLinkUpdate(RequestModel):
    expected_version: Version
    rationale: Rationale


class CatalogLinkRemoval(RequestModel):
    expected_version: Version
    reason: Rationale


class CatalogLink(BaseModel):
    id: UUID
    kind: str
    from_id: UUID
    from_kind: CatalogKind
    to_id: UUID
    to_kind: CatalogKind
    rationale: str
    version: Version
    created_at: datetime
    updated_at: datetime


def validate_attributes(kind: str, attributes: Attributes) -> Attributes:
    fields = {key: (label, field_type) for key, label, field_type in ATTRIBUTE_FIELDS[kind]}
    if attributes.keys() - fields.keys():
        raise HTTPException(422, "Für diesen Datensatztyp gibt es unbekannte Zusatzfelder.")
    for key, value in attributes.items():
        label, field_type = fields[key]
        if field_type != "date" or not value:
            continue
        try:
            if date.fromisoformat(value).isoformat() == value:
                continue
        except ValueError:
            pass
        raise HTTPException(422, f"{label} benötigt ein Datum im Format JJJJ-MM-TT.")
    return {key: value for key, value in attributes.items() if value}
