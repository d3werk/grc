using System.Globalization;

namespace D3.Grc.Core;

public static class AiModule
{
    public const string Kind = "AiSystem";
    public const string ReviewStatus = "In Prüfung";
    public const string ActiveStatus = "Aktiv";
    public const string SuspendedStatus = "Ausgesetzt";
    public const string RetiredStatus = "Außer Betrieb";
    public const string Unassessed = "Ungeprüft";
    public const string PendingDecision = "Offen";
    public const string Approved = "Freigegeben";
    public const string ApprovedWithConditions = "Mit Auflagen freigegeben";

    public static ModuleSpec[] Specs { get; } =
    [
        new(Kind, "KI-Systeme", "KI-System",
            "Einsätze, Modelle und Daten erfassen sowie interne Einstufung, Aufsicht und Einsatzentscheidungen dokumentieren.",
            "AI",
            [
                new("intended_use", "Bestimmungsgemäße Verwendung und Einsatzgrenzen", "long"),
                new("model_version", "Modell oder KI-Dienst und Version"),
                new("provider", "Anbieter"),
                new("user_groups", "Nutzergruppen und betroffene Personen", "long"),
                new("data_categories", "Eingabe- und Ausgabedaten", "long"),
                new("legal_role", "Rolle der Organisation nach fachlicher Prüfung", "select",
                    ["Noch zu prüfen", "Anbieter", "Betreiber", "Einführer", "Händler", "Mehrere Rollen", "Andere"]),
                new("risk_class", "Interne Risikoklasse", "select", [Unassessed, "Gering", "Mittel", "Hoch", "Kritisch"]),
                new("classification_rationale", "Begründung der internen Risikoklasse", "long"),
                new("regulatory_assessment", "Regulatorische Einordnung, Fundstellen und offene Fragen", "long"),
                new("human_oversight", "Menschliche Aufsicht, Eingriff und Abschaltung", "long"),
                new("monitoring_plan", "Betriebsüberwachung, Stichproben und Schwellenwerte", "long"),
                new("decision", "Einsatzentscheidung", "select",
                    [PendingDecision, Approved, ApprovedWithConditions, "Abgelehnt", SuspendedStatus]),
                new("decision_by", "Entscheidung verantwortet durch"),
                new("decision_on", "Datum der Entscheidung", "date"),
                new("decision_rationale", "Begründung der Einsatzentscheidung", "long"),
                new("decision_conditions", "Auflagen", "long"),
                new("last_review_on", "Datum der letzten Überprüfung", "date"),
                new("review_result", "Ergebnis der letzten Überprüfung", "long")
            ],
            ["Entwurf", ReviewStatus, ActiveStatus, SuspendedStatus, RetiredStatus, "Offen", "In Bearbeitung", "Erledigt"],
            Validate)
    ];

    public static ModuleRelation[] Relations { get; } =
    [
        new("KI-System → Risiko", Kind, "Risk"),
        new("KI-System → Anforderung", Kind, "Requirement"),
        new("KI-System → Dienstleister", Kind, "Supplier"),
        new("KI-System → Geschäftsprozess", Kind, "BusinessProcess"),
        new("KI-System → Maßnahme", Kind, "Measure")
    ];

    private static void Validate(Entry entry, IReadOnlyDictionary<string, string> fields)
    {
        string Field(string key) => fields.GetValueOrDefault(key, "").Trim();

        var riskClass = Field("risk_class");
        var decision = Field("decision");
        var hasIntendedUse = !string.IsNullOrWhiteSpace(entry.Owner) && Field("intended_use").Length > 0;
        var classified = riskClass.Length > 0 && riskClass != Unassessed;
        var approved = decision is Approved or ApprovedWithConditions;
        if (classified && Field("classification_rationale").Length == 0)
            throw new ArgumentException("Begründen Sie die gewählte interne Risikoklasse.");
        if ((entry.Status is ReviewStatus or ActiveStatus) && !hasIntendedUse)
            throw new ArgumentException("Für die KI-Prüfung sind Verantwortung und bestimmungsgemäße Verwendung erforderlich.");

        var decisionDate = ReadPastDate(Field("decision_on"), "Entscheidungsdatum");
        var reviewDate = ReadPastDate(Field("last_review_on"), "Datum der letzten Überprüfung");
        if (decision is not ("" or PendingDecision) &&
            (decisionDate is null || Field("decision_by").Length == 0 || Field("decision_rationale").Length == 0))
            throw new ArgumentException("Dokumentieren Sie Verantwortung, Datum und Begründung der Einsatzentscheidung.");
        if (decision == ApprovedWithConditions && Field("decision_conditions").Length == 0)
            throw new ArgumentException("Erfassen Sie die Auflagen des freigegebenen Einsatzes.");
        if (approved && (!hasIntendedUse || !classified || Field("human_oversight").Length == 0 ||
            Field("monitoring_plan").Length == 0 || string.IsNullOrWhiteSpace(entry.DueDate)))
            throw new ArgumentException("Für eine Freigabe sind Verantwortung, Verwendungszweck, begründete Risikoklasse, " +
                "Aufsicht, Betriebsüberwachung und nächster Prüftermin erforderlich.");
        if (entry.Status == ActiveStatus && !approved)
            throw new ArgumentException("Ein aktives KI-System braucht eine dokumentierte Freigabe.");
        if ((reviewDate is not null) != (Field("review_result").Length > 0))
            throw new ArgumentException("Erfassen Sie Datum und Ergebnis der letzten Überprüfung gemeinsam.");
        if (DateOnly.TryParseExact(entry.DueDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var due) &&
            (due < decisionDate || due < reviewDate))
            throw new ArgumentException("Der nächste Prüftermin darf nicht vor der Einsatzentscheidung oder letzten Überprüfung liegen.");
    }

    private static DateOnly? ReadPastDate(string value, string label)
    {
        if (value.Length == 0)
            return null;
        if (DateOnly.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date) &&
            date != DateOnly.MinValue && date <= DateOnly.FromDateTime(DateTime.Today))
            return date;
        throw new ArgumentException($"{label} als Datum im Format JJJJ-MM-TT bis einschließlich heute angeben.");
    }

    public static ModuleSeed[] DemoRows
    {
        get
        {
            var today = DateOnly.FromDateTime(DateTime.Today);
            string DateText(int days) => today.AddDays(days).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            return
            [
                new(new Entry
                {
                    Id = "demo-ai-knowledge", Kind = Kind, Code = "KI-001", Title = "Interne Wissenssuche",
                    Scope = "Kundenservice", Owner = "Serviceleitung (Demo)", Status = ActiveStatus, DueDate = DateText(-1),
                    Body = "Synthetisches Beispiel. Ein interner Assistent findet freigegebene Anleitungen für Servicefälle."
                }, "AI", new()
                {
                    ["intended_use"] = "Unterstützt Mitarbeitende beim Finden geprüfter Arbeitsanweisungen. Keine automatischen Antworten an Kunden.",
                    ["model_version"] = "Beispielmodell Wissenssuche 1.2",
                    ["provider"] = "Fiktive Modellwerkstatt",
                    ["user_groups"] = "Pilotgruppe im Kundenservice",
                    ["data_categories"] = "Freigegebene Arbeitsanweisungen, Suchbegriffe und Fundstellen. Keine Fall- oder Kundendaten.",
                    ["legal_role"] = "Noch zu prüfen",
                    ["risk_class"] = "Mittel",
                    ["classification_rationale"] = "Unpassende oder veraltete Fundstellen können die Fallbearbeitung verzögern. Die Pilotgruppe prüft jede Fundstelle vor Verwendung.",
                    ["regulatory_assessment"] = "Interne Simulation. Rolle und anwendbare Anforderungen sind für einen realen Einsatz fachlich zu prüfen.",
                    ["human_oversight"] = "Mitarbeitende prüfen die Quelle. Bei falschen Ergebnissen melden sie den Fall und nutzen die manuelle Suche. Die Serviceleitung kann den Pilotzugang aussetzen.",
                    ["monitoring_plan"] = "Monatlich 20 Antworten prüfen. Ab zwei falschen Fundstellen in der Stichprobe Softwarestand und Quellen prüfen und den Betrieb gegebenenfalls aussetzen.",
                    ["decision"] = ApprovedWithConditions,
                    ["decision_by"] = "KI-Koordinationsstelle (Demo)",
                    ["decision_on"] = DateText(-45),
                    ["decision_rationale"] = "Befristeter Piloteinsatz mit überschaubarer Nutzergruppe und manueller Quellenprüfung.",
                    ["decision_conditions"] = "Nur freigegebene Wissensbasis anbinden. Pilotzugriff auf die Serviceleitung und das ausgewählte Team begrenzen.",
                    ["last_review_on"] = DateText(-30),
                    ["review_result"] = "Simulierte Erstprüfung: Eine unzutreffende Quellenreferenz in 20 synthetischen Antworten. Verbesserungsmaßnahme zum Quellenabgleich angelegt."
                }),
                new(new Entry
                {
                    Id = "demo-ai-triage", Kind = Kind, Code = "KI-002", Title = "Vorsortierung von Supportanfragen",
                    Scope = "Kundenservice", Owner = "Supportkoordination (Demo)", Status = ReviewStatus, DueDate = DateText(14),
                    Body = "Synthetisches Beispiel. Das Vorhaben wird auf zulässige Eingangsdaten, Fehlzuordnungen und Rückfallverfahren geprüft."
                }, "AI", new()
                {
                    ["intended_use"] = "Schlägt eine Warteschlange vor. Mitarbeitende prüfen die Zuordnung vor der Weiterleitung.",
                    ["model_version"] = "Beispielklassifikator 0.3",
                    ["user_groups"] = "Support und Schichtleitung",
                    ["data_categories"] = "Synthetische Betreffzeilen und Ticketkategorien",
                    ["risk_class"] = Unassessed,
                    ["decision"] = PendingDecision
                }),
                new(new Entry
                {
                    Id = "demo-ai-risk", Kind = "Risk", Code = "R-KI-01", Title = "Irreführende Antwort der KI-Wissenssuche",
                    Scope = "Kundenservice", Owner = "Serviceleitung (Demo)", Status = "Offen",
                    Body = "Synthetisches Szenario. Eine veraltete Arbeitsanweisung könnte übernommen werden. Folgen sind fehlerhafte Auskünfte und zusätzlicher Bearbeitungsaufwand."
                }, "AI"),
                new(new Entry
                {
                    Id = "demo-ai-control", Kind = "Control", Code = "C-KI-01", Title = "Quellenprüfung vor Übernahme",
                    Scope = "Kundenservice", Owner = "Supportkoordination (Demo)", Status = "Umgesetzt",
                    Body = "Synthetisches Control. Nutzer öffnen die zitierte Arbeitsanweisung, prüfen deren Aktualität und übernehmen die Information nach fachlicher Prüfung."
                }, "AI"),
                new(new Entry
                {
                    Id = "demo-ai-measure", Kind = "Measure", Code = "M-KI-01", Title = "Quellenabgleich der Wissenssuche verbessern",
                    Scope = "Kundenservice", Owner = "KI-Koordinationsstelle (Demo)", Status = "In Bearbeitung",
                    DueDate = DateText(7), Body = "Synthetische Maßnahme. Drei problematische Fundstellen analysieren und die nächsten 20 Antworten erneut prüfen."
                }, "AI")
            ];
        }
    }

    public static ModuleLink[] DemoLinks { get; } =
    [
        new("demo-ai-knowledge", "demo-ai-risk", "KI-System → Risiko", "Die interne Wissenssuche kann irreführende Antworten erzeugen."),
        new("demo-ai-knowledge", "demo-ai-control", "KI-System → Control", "Vor einer Verwendung muss die zitierte Arbeitsanweisung geprüft werden."),
        new("demo-ai-knowledge", "demo-ai-measure", "KI-System → Maßnahme", "Die Maßnahme verfolgt die in der simulierten Überprüfung erfasste Abweichung."),
        new("demo-ai-risk", "demo-ai-control", "Risiko → Control", "Eine Prüfung der Quelle reduziert die Wahrscheinlichkeit einer Falschauskunft.")
    ];
}
