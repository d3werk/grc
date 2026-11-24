using System.Text.Json;

namespace D3.Grc.Core;

public sealed record ProcessStep(string Title, string Role = "", string Instruction = "");

public static class ProcessModule
{
    public const string Kind = "BusinessProcess";
    public const string ParentType = "Übergeordneter Prozess";
    public const string DependencyType = "Abhängigkeit von Prozess";
    public const string StepKey = "process_steps";
    public const int MaxSteps = 30;

    public static readonly string[] Categories = ["Führungsprozess", "Kernprozess", "Unterstützungsprozess"];
    public static readonly string[] Criticality = ["Niedrig", "Mittel", "Hoch", "Kritisch"];

    public static readonly ModuleSpec[] Specs =
    [
        new(Kind, "Geschäftsprozesse", "Geschäftsprozess",
            "Prozesse einordnen, Abläufe beschreiben, Verantwortung festhalten und die nächste Überprüfung planen.", "PROCESS",
            [
                new("process_type", "Prozessart", "select", Categories),
                new("criticality", "Kritikalität", "select", Criticality),
                new("purpose", "Zweck und Ergebnis", "long"),
                new("trigger", "Auslöser", "long"),
                new("inputs", "Eingaben und Voraussetzungen", "long"),
                new("outputs", "Ausgaben und Übergabe", "long"),
                new(StepKey, "Prozessschritte", "steps"),
                new("review_note", "Prüf- und Freigabevermerk", "long")
            ], ["Entwurf", "In Prüfung", "Freigegeben", "Außer Kraft"], Validate)
    ];

    public static readonly ModuleRelation[] Relations =
    [
        new(ParentType, Kind, Kind),
        new(DependencyType, Kind, Kind),
        new("Prozess nutzt Lieferantenleistung", Kind, "Supplier"),
        new("Prozessrisiko", Kind, "Risk"),
        new("Prozesskontrolle", Kind, "Control"),
        new("Prozessverbesserung", Kind, "Measure")
    ];

    private static readonly JsonSerializerOptions StepJson = new(JsonSerializerDefaults.Web);

    public static ProcessStep[] ReadSteps(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return [];
        try
        {
            return JsonSerializer.Deserialize<ProcessStep[]>(value, StepJson)
                ?? throw new ArgumentException("Prozessschritte als geordnete Liste speichern.");
        }
        catch (JsonException error)
        {
            throw new ArgumentException("Prozessschritte als geordnete Liste speichern.", error);
        }
    }

    public static string WriteSteps(IEnumerable<ProcessStep> steps) => JsonSerializer.Serialize(steps, StepJson);

    public static string FormatSteps(string? value) => string.Join(Environment.NewLine,
        ReadSteps(value).Select((step, index) => $"{index + 1}. {step.Title}" +
            (string.IsNullOrWhiteSpace(step.Role) ? "" : $" ({step.Role})") +
            (string.IsNullOrWhiteSpace(step.Instruction) ? "" : $" · {step.Instruction}")));

    private static void Validate(Entry entry, IReadOnlyDictionary<string, string> fields)
    {
        var steps = ReadSteps(fields.GetValueOrDefault(StepKey));
        if (steps.Length > MaxSteps || steps.Any(step => step is null || string.IsNullOrWhiteSpace(step.Title) ||
            step.Title.Length > 160 || step.Role is null || step.Role.Length > 160 ||
            step.Instruction is null || step.Instruction.Length > 600))
            throw new ArgumentException("Bis zu 30 Prozessschritte sind möglich. Jeder Schritt benötigt eine Tätigkeit mit höchstens 160 Zeichen.");

        if (entry.Status == "Freigegeben" && (steps.Length == 0 || string.IsNullOrWhiteSpace(entry.Owner) ||
            entry.DueDate.Length == 0 || string.IsNullOrWhiteSpace(fields.GetValueOrDefault("review_note"))))
            throw new ArgumentException("Für eine Prozessfreigabe sind Verantwortung, nächster Prüftermin, Prozessschritte und ein Prüfvermerk erforderlich.");
    }

    private const string DemoScope = "Kundenservice";

    public static ModuleSeed[] DemoRows =>
    [
        Example("demo-process-service", "BP-100", "Kundenanliegen steuern", "Führungsprozess", "Hoch", "Leitung Kundenservice",
            "Kundenanliegen werden nach Dringlichkeit geplant, abgearbeitet und ausgewertet.", "Neue Anliegen im Serviceportal oder wiederkehrende Fristüberschreitungen.",
            "Offene Tickets, Kapazitäten, Qualitätskennzahlen", "Priorisierte Fallliste und Verbesserungsaufträge",
            new("Auslastung und Rückstände bewerten", "Teamleitung", "Offene Fälle und vereinbarte Servicetermine abgleichen."),
            new("Prioritäten festlegen", "Teamleitung", "Engpässe protokollieren und die Verteilung der Fälle freigeben.")),
        Example("demo-process-intake", "BP-110", "Serviceanfrage erfassen", "Kernprozess", "Mittel", "Service Desk",
            "Vollständige und eindeutig zugeordnete Anfrage für die nachgelagerte Bearbeitung.", "Anfrage über das Serviceportal.",
            "Kundenkennung, Kontaktweg, Beschreibung des Anliegens", "Ticket mit Kategorie, Priorität und Zieltermin",
            new("Anfrage prüfen", "Service Desk", "Kontaktweg und Mindestangaben prüfen. Fehlende Angaben gezielt nachfragen."),
            new("Kategorie und Priorität vergeben", "Service Desk", "Anfrage thematisch einordnen und je Dringlichkeit die Bearbeitungsfrist setzen.")),
        Example("demo-process-case", "BP-120", "Serviceanfrage bearbeiten", "Kernprozess", "Hoch", "Fallverantwortung",
            "Die Anfrage wird mit nachvollziehbarer Entscheidung und Rückmeldung abgeschlossen.", "Zugewiesenes Ticket ist bearbeitungsbereit.",
            "Erfasstes Ticket und Bearbeitungsfrist", "Entscheidung, Rückmeldung und dokumentierter Abschluss",
            new("Sachverhalt klären", "Fallverantwortung", "Unterlagen und Bearbeitungshistorie abgleichen."),
            new("Bei Abweichung eskalieren", "Fallverantwortung", "Bei überschrittenem Zieltermin die Teamleitung einbeziehen."),
            new("Ergebnis kommunizieren", "Service Desk", "Rückmeldung über den vereinbarten Kanal senden und Ticket schließen.")),
        Example("demo-process-portal", "BP-200", "Serviceportal betreiben", "Unterstützungsprozess", "Kritisch", "IT-Betrieb",
            "Das Serviceportal steht für Erfassung, Weiterleitung und Wiederaufnahme bereit.", "Betriebsüberwachung oder Störungsmeldung.",
            "Verfügbarkeitsüberwachung, offene IT-Störungen", "Störungsakte und Nachweis zum Wiederanlauf",
            new("Verfügbarkeit überwachen", "IT-Betrieb", "Wiederkehrende Prüfungen und aktuelle Störungsmeldungen auswerten."),
            new("Ersatzkanal aktivieren", "Serviceleitung", "Interne Fallannahme nach Notfallplan starten und Rückkehr zum Portal koordinieren."))
    ];

    public static readonly ModuleLink[] DemoLinks =
    [
        new("demo-process-intake", "demo-process-service", ParentType, "Die Annahme ist Teil des übergeordneten Serviceablaufs."),
        new("demo-process-case", "demo-process-service", ParentType, "Die Fallbearbeitung ist Teil des übergeordneten Serviceablaufs."),
        new("demo-process-case", "demo-process-intake", DependencyType, "Die Fallbearbeitung beginnt mit einer vollständig erfassten Anfrage."),
        new("demo-process-intake", "demo-process-portal", DependencyType, "Das Portal liefert die Daten für die Anfrageerfassung.")
    ];

    private static ModuleSeed Example(string id, string code, string title, string category, string criticality,
        string owner, string purpose, string trigger, string inputs, string outputs, params ProcessStep[] steps) =>
        new(new Entry
        {
            Id = id, Kind = Kind, Code = code, Title = title, Owner = owner, Scope = DemoScope, Status = "Freigegeben",
            Body = purpose, DueDate = DateOnly.FromDateTime(DateTime.Today).AddMonths(2).ToString("yyyy-MM-dd",
                System.Globalization.CultureInfo.InvariantCulture)
        }, "PROCESS", new Dictionary<string, string>
        {
            ["process_type"] = category, ["criticality"] = criticality, ["purpose"] = purpose, ["trigger"] = trigger,
            ["inputs"] = inputs, ["outputs"] = outputs, [StepKey] = WriteSteps(steps),
            ["review_note"] = "Synthetische Freigabe für die Prozess-Demo. Zuständigkeiten und Übergaben wurden anhand des Beispiels durchgespielt."
        });
}
