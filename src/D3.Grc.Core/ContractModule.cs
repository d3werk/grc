using System.Globalization;

namespace D3.Grc.Core;

public sealed record ContractDeadline(ModuleRow Contract, string Label, DateOnly Date);

public static class ContractModule
{
    public const string Kind = "Contract";
    private const string Topic = "ISMS";
    private const string Scope = "Kundenservice";
    private static readonly (string Key, string Label)[] DeadlineFields =
    [
        ("notice_due", "Kündigungsstichtag"),
        ("review_due", "Prüftermin"),
        ("ends_on", "Laufzeitende")
    ];

    public static ModuleSpec[] Specs { get; } =
    [
        new(Kind, "Vertragsregister", "Vertrag",
            "Vertragsparteien, Laufzeiten, Pflichten und Entscheidungen zur Fortführung pflegen.", Topic,
            [
                new("contract_type", "Vertragsart", "select", ["Dienstleistung", "Lieferung", "Lizenz oder Abonnement",
                    "Auftragsverarbeitung", "Vertraulichkeit", "Rahmenvertrag", "Sonstiger Vertrag"]),
                new("internal_party", "Interne Vertragspartei"),
                new("counterparty", "Externe Vertragspartei"),
                new("signed_on", "Unterzeichnet am", "date"),
                new("starts_on", "Vertragsbeginn", "date"),
                new("ends_on", "Ende der aktuellen Laufzeit", "date"),
                new("notice_due", "Nächster Kündigungsstichtag", "date"),
                new("review_due", "Nächster Prüftermin", "date"),
                new("terminated_on", "Wirksam beendet am", "date"),
                new("renewal_mode", "Verlängerungsart", "select", ["Keine Verlängerung", "Durch Vereinbarung", "Automatisch", "Unbefristet"]),
                new("decision", "Entscheidung zur Fortführung", "select", ["Offen", "Verlängern", "Neu verhandeln", "Kündigen", "Auslaufen lassen"]),
                new("decision_note", "Begründung und nächster Schritt", "long"),
                new("renewal_terms", "Verlängerungs- und Kündigungsbedingungen", "long"),
                new("obligations", "Pflichten und Abnahmekriterien", "long"),
                new("document_reference", "Fundstelle des Vertragsdokuments")
            ],
            ["Entwurf", "In Prüfung", "Aktiv", "Beendet", "Archiviert"], Validate)
    ];

    public static ModuleRelation[] Relations { get; } =
    [
        new("Vertrag → Lieferant", Kind, "Supplier"),
        new("Vertrag → Geschäftsprozess", Kind, "BusinessProcess"),
        new("Vertrag → Risiko", Kind, "Risk"),
        new("Vertrag → Control", Kind, "Control"),
        new("Vertrag → Maßnahme", Kind, "Measure")
    ];

    public static ModuleLink[] DemoLinks { get; } =
    [
        new("demo-contract-support", "demo-supplier-01", "Vertrag → Lieferant", "Die externe Vertragspartei erbringt den vereinbarten Support."),
        new("demo-contract-support", "demo-process-service", "Vertrag → Geschäftsprozess", "Die vereinbarte Supportleistung unterstützt den Kundenservice.")
    ];

    public static ModuleSeed[] DemoRows
    {
        get
        {
            var today = DateOnly.FromDateTime(DateTime.Today);
            return
            [
                Seed("demo-contract-support", "VT-DEMO-01", "Support für das Serviceportal", "Aktiv", "Vertragsmanagement",
                    "Der Vertrag regelt die Störungsannahme und Eskalation für das fiktive Serviceportal.", new()
                    {
                        ["contract_type"] = "Dienstleistung", ["internal_party"] = "D3 Demo GmbH",
                        ["counterparty"] = "Fiktiver Supportpartner", ["signed_on"] = Iso(today.AddDays(-180)),
                        ["starts_on"] = Iso(today.AddDays(-165)), ["ends_on"] = Iso(today.AddDays(61)),
                        ["notice_due"] = Iso(today.AddDays(12)), ["review_due"] = Iso(today.AddDays(4)),
                        ["renewal_mode"] = "Durch Vereinbarung", ["decision"] = "Offen",
                        ["renewal_terms"] = "Vor einer Verlängerung den Leistungsumfang und die Eskalationskontakte schriftlich bestätigen.",
                        ["obligations"] = "Zu jedem Störungsticket Zeit bis Rückmeldung und Zeit bis Lösung erfassen.",
                        ["document_reference"] = "Synthetischer Mustervertrag VT-DEMO-01"
                    }),
                Seed("demo-contract-license", "VT-DEMO-02", "Lizenz für die Wissensdatenbank", "In Prüfung", "Einkauf",
                    "Die Fachverantwortung prüft die Nutzung vor einer möglichen Verlängerung.", new()
                    {
                        ["contract_type"] = "Lizenz oder Abonnement", ["internal_party"] = "D3 Demo GmbH",
                        ["counterparty"] = "Fiktiver Wissensanbieter", ["starts_on"] = Iso(today.AddDays(-310)),
                        ["ends_on"] = Iso(today.AddDays(55)), ["notice_due"] = Iso(today.AddDays(25)),
                        ["review_due"] = Iso(today.AddDays(-6)), ["renewal_mode"] = "Automatisch",
                        ["decision"] = "Neu verhandeln", ["decision_note"] = "Nutzungszahlen auswerten und ein Angebot für die benötigten Lizenzen einholen."
                    }),
                Seed("demo-contract-ended", "VT-DEMO-03", "Übergangsvertrag für Datenübernahme", "Beendet", "Kundenservice",
                    "Die Übergangsleistung wurde nach dem vereinbarten Datentransfer beendet.", new()
                    {
                        ["contract_type"] = "Dienstleistung", ["internal_party"] = "D3 Demo GmbH",
                        ["counterparty"] = "Fiktiver Migrationsdienst", ["starts_on"] = Iso(today.AddDays(-160)),
                        ["ends_on"] = Iso(today.AddDays(-48)), ["terminated_on"] = Iso(today.AddDays(-48)),
                        ["renewal_mode"] = "Keine Verlängerung", ["decision"] = "Auslaufen lassen",
                        ["decision_note"] = "Datentransfer abgenommen. Vertragsende und Rückgabe der Zugänge dokumentiert."
                    })
            ];
        }
    }

    public static IReadOnlyList<ContractDeadline> Deadlines(IEnumerable<ModuleRow> contracts) => contracts
        .Where(row => row.Entry.Kind == Kind && row.Entry.Status is "Aktiv" or "In Prüfung")
        .SelectMany(row => DeadlineFields.Select(field => (field.Label, Date: ReadDate(row.Fields, field.Key)))
            .Where(field => field.Date.HasValue)
            .Select(field => new ContractDeadline(row, field.Label, field.Date!.Value)))
        .OrderBy(deadline => deadline.Date)
        .ThenBy(deadline => deadline.Contract.Entry.Title, StringComparer.CurrentCultureIgnoreCase)
        .ThenBy(deadline => deadline.Label, StringComparer.CurrentCultureIgnoreCase)
        .ToArray();

    private static void Validate(Entry entry, IReadOnlyDictionary<string, string> fields)
    {
        var start = ReadDate(fields, "starts_on");
        var end = ReadDate(fields, "ends_on");
        if (start.HasValue && end.HasValue && end.Value < start.Value)
            throw new ArgumentException("Das Ende der aktuellen Laufzeit muss am oder nach dem Vertragsbeginn liegen.");
        if (entry.Status == "Aktiv" && (string.IsNullOrWhiteSpace(entry.Owner) || !HasValue(fields, "internal_party") || !HasValue(fields, "counterparty")))
            throw new ArgumentException("Für einen aktiven Vertrag sind Verantwortung und beide Vertragsparteien erforderlich.");
        if (entry.Status == "Beendet" && !end.HasValue && !ReadDate(fields, "terminated_on").HasValue)
            throw new ArgumentException("Für einen beendeten Vertrag das Laufzeitende oder das wirksame Beendigungsdatum erfassen.");
        if (HasValue(fields, "decision") && fields["decision"] != "Offen" && !HasValue(fields, "decision_note"))
            throw new ArgumentException("Begründung und nächsten Schritt zur Fortführungsentscheidung erfassen.");
    }

    private static bool HasValue(IReadOnlyDictionary<string, string> fields, string key) =>
        fields.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(RichTextContent.Plain(value));

    private static DateOnly? ReadDate(IReadOnlyDictionary<string, string> fields, string key) =>
        DateOnly.TryParseExact(fields.GetValueOrDefault(key), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)
            ? date : null;

    private static string Iso(DateOnly date) => date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    private static ModuleSeed Seed(string id, string code, string title, string status, string owner, string body, Dictionary<string, string> fields) =>
        new(new Entry { Id = id, Kind = Kind, Code = code, Title = title, Status = status, Owner = owner, Body = body, Scope = Scope }, Topic, fields);
}
