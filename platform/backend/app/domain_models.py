import json
import re
from datetime import date, datetime
from typing import Annotated, Literal
from uuid import UUID

from fastapi import HTTPException
from pydantic import BaseModel, ConfigDict, Field, TypeAdapter, ValidationError

from .ai_governance import AI_FIELDS, AI_OPTIONS, validate_ai
from .audit_models import AUDIT_FIELDS, AUDIT_OPTIONS, validate_audit
from .contract_models import CONTRACT_FIELDS, CONTRACT_OPTIONS, validate_contract
from .resilience_models import RESILIENCE_FIELDS, RESILIENCE_OPTIONS, validate_resilience
from .rich_text import RichTextDocument, RichTextFields

CatalogKind = Literal[
    "org_unit", "asset", "it_service", "business_process", "risk", "control", "measure",
    "framework_version", "requirement", "processing_activity", "supplier", "dpia",
    "audit_subject", "audit_engagement", "audit_test", "audit_finding",
    "resilience_service", "resilience_scenario", "continuity_bia", "continuity_plan", "continuity_exercise",
    "ai_system", "contract",
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
    "audit_subject": "Prüfobjekt",
    "audit_engagement": "Internes Audit",
    "audit_test": "Prüfschritt",
    "audit_finding": "Auditfeststellung",
    "resilience_service": "Kritische Geschäftsdienstleistung",
    "resilience_scenario": "Resilienz-Szenario",
    "continuity_bia": "Business-Impact-Analyse",
    "continuity_plan": "Notfall- und Wiederanlaufplan",
    "continuity_exercise": "BCM-Übung",
    "ai_system": "KI-System",
    "contract": "Vertrag",
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
    type: Literal["text", "textarea", "date", "number", "select"]
    options: list[str] = Field(default_factory=list)


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
        ("process_type", "Prozessart", "text"),
        ("purpose", "Zweck des Geschäftsprozesses", "textarea"),
        ("trigger", "Auslöser", "textarea"),
        ("inputs", "Eingaben", "textarea"),
        ("outputs", "Ergebnisse", "textarea"),
        ("process_steps", "Prozessschritte", "text"),
        ("criticality", "Kritikalität", "text"),
        ("recovery_target", "Wiederanlaufziel", "text"),
        ("review_due", "Nächste Überprüfung", "date"),
        ("review_notes", "Prüfvermerk", "textarea"),
    ),
    "risk": (
        ("scenario", "Risikoszenario", "textarea"),
        ("consequence", "Mögliche Auswirkungen", "textarea"),
    ),
    "control": (
        ("control_objective", "Kontrollziel", "textarea"),
        ("control_frequency", "Durchführungs- oder Prüfintervall", "text"),
        ("control_type", "Kontrollart", "select"),
        ("execution_method", "Durchführung und Nachweis", "textarea"),
        ("test_method", "Prüfmethode und Wirksamkeitskriterium", "textarea"),
        ("next_test_due", "Nächste Wirksamkeitsprüfung", "date"),
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
    **AUDIT_FIELDS,
    **RESILIENCE_FIELDS,
    **AI_FIELDS,
    **CONTRACT_FIELDS,
}
ATTRIBUTE_OPTIONS = {
    "control": {"control_type": ("Präventiv", "Detektiv", "Korrektiv")},
    **AUDIT_OPTIONS,
    **RESILIENCE_OPTIONS,
    **AI_OPTIONS,
    **CONTRACT_OPTIONS,
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


class ProcessStep(RequestModel):
    title: Annotated[DatabaseText, Field(min_length=1, max_length=160)]
    role: Annotated[DatabaseText, Field(max_length=160)] = ""
    instruction: Annotated[DatabaseText, Field(max_length=600)] = ""


_PROCESS_STEPS = TypeAdapter(Annotated[list[ProcessStep], Field(max_length=30)])


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
    rich_text: RichTextFields = Field(default_factory=dict)


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
    rich_text: RichTextFields | None = None


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
    rich_text: RichTextFields
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
    rationale_rich_text: RichTextDocument | None = None


class CatalogLinkUpdate(RequestModel):
    expected_version: Version
    rationale: Rationale
    rationale_rich_text: RichTextDocument | None = None


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
    rationale_rich_text: RichTextDocument | None = None
    version: Version
    created_at: datetime
    updated_at: datetime


def validate_attributes(kind: str, attributes: Attributes) -> Attributes:
    fields = {key: (label, field_type) for key, label, field_type in ATTRIBUTE_FIELDS[kind]}
    if attributes.keys() - fields.keys():
        raise HTTPException(422, "Für diesen Datensatztyp gibt es unbekannte Zusatzfelder.")
    for key, value in attributes.items():
        label, field_type = fields[key]
        if not value:
            continue
        if field_type == "date":
            try:
                valid_date = date.fromisoformat(value).isoformat() == value
            except ValueError:
                valid_date = False
            if not valid_date:
                raise HTTPException(422, f"{label} benötigt ein Datum im Format JJJJ-MM-TT.")
        if field_type == "number" and (len(value) > 32 or not re.fullmatch(r"-?[0-9]+(?:\.[0-9]+)?", value)):
            raise HTTPException(422, f"{label} benötigt einen Zahlenwert.")
        if field_type == "select" and value not in ATTRIBUTE_OPTIONS[kind][key]:
            raise HTTPException(422, f"Wählen Sie für {label} einen Wert aus der Liste.")
    result = {key: value for key, value in attributes.items() if value}
    if kind == "business_process" and (steps := result.get("process_steps")):
        try:
            values = _PROCESS_STEPS.validate_json(steps)
        except ValidationError as exc:
            raise HTTPException(422, "Prozessschritte benötigen einen Titel und dürfen höchstens 30 Einträge enthalten.") from exc
        if values:
            result["process_steps"] = json.dumps(
                [step.model_dump(exclude_defaults=True) for step in values], ensure_ascii=False, separators=(",", ":")
            )
        else:
            result.pop("process_steps")
    return result


def validate_catalog_workflow(kind: CatalogKind, attributes: Attributes, status: CatalogStatus, owner: str) -> None:
    if kind in AUDIT_FIELDS:
        validate_audit(kind, attributes, status)
    elif kind in RESILIENCE_FIELDS:
        validate_resilience(kind, attributes, status)
    elif kind in AI_FIELDS:
        validate_ai(kind, attributes, status, owner)
    elif kind in CONTRACT_FIELDS:
        validate_contract(kind, attributes, status, owner)
