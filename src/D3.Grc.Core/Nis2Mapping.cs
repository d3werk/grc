using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Data.Sqlite;
using static D3.Grc.Core.Database;

namespace D3.Grc.Core;

public sealed record Nis2ControlReference(string SourceId, string Edition, string Identifier)
{
    public override string ToString() => $"{Edition} · {Identifier}";
}

public sealed record Nis2ControlSuggestion(string RequirementId, Entry Control,
    IReadOnlyList<Nis2ControlReference> References, string ProposedCoverage, string Rationale);

internal static partial class Nis2Mapping
{
    internal const string LinkType = "Anforderung → Control";
    internal const string Directive = "NIS2";
    private const string IsoSource = "ISO27001";
    private const string BsiSource = "BSIGS";

    private sealed record Rule(string[] Iso, string[] Bsi, string Rationale);

    // These are review pointers at Annex A control or IT-Grundschutz block level.
    // Decisions and observation evidence belong to each organisation's own records.
    private static readonly IReadOnlyDictionary<string, Rule> Rules = new Dictionary<string, Rule>(StringComparer.Ordinal)
    {
        ["21a"] = new(["A.5.1", "A.5.2", "A.5.8"], ["ISMS.1"],
            "Prüfen, ob Sicherheitsvorgaben, Rollen und der Umgang mit Risiken für den gewählten Bereich tatsächlich abgedeckt sind."),
        ["21b"] = new(["A.5.24", "A.5.25", "A.5.26", "A.5.27", "A.5.28", "A.6.8"], ["DER.1", "DER.2.1"],
            "Abgleichen, welche Schritte der Vorfallbehandlung das Control abdeckt und wie Übergaben und Erkenntnisse dokumentiert werden."),
        ["21c"] = new(["A.5.29", "A.5.30", "A.8.13", "A.8.14"], ["CON.3", "DER.4"],
            "Beitrag zu Sicherung, Wiederanlauf oder Krisenbetrieb anhand der Ziele und der tatsächlichen Übungen im Bereich bestimmen."),
        ["21d"] = new(["A.5.19", "A.5.20", "A.5.21", "A.5.22", "A.5.23"], ["OPS.2.2", "OPS.2.3"],
            "Prüfen, welche unmittelbaren Dienstleister oder Lieferanten und welche Auswahl-, Vertrags- oder Überwachungsschritte erfasst sind."),
        ["21e"] = new(["A.8.8", "A.8.25", "A.8.26", "A.8.28", "A.8.29", "A.8.32"], ["CON.8", "OPS.1.1.3", "OPS.1.1.6"],
            "Festhalten, auf welche Systeme und Schritte von Beschaffung bis Wartung das Control wirkt. Den Umgang mit Schwachstellen im Bereich gesondert abgleichen."),
        ["21f"] = new(["A.5.35", "A.5.36", "A.8.34"], ["DER.3.1", "DER.3.2"],
            "Bewerten, ob die Prüfung den gewünschten Wirksamkeitsmaßstab, aussagekräftige Stichproben und die Behandlung von Abweichungen abdeckt."),
        ["21g"] = new(["A.6.3"], ["ORP.3"],
            "Prüfen, welche Rollen, Inhalte und Wiederholungen durch die Schulung abgedeckt sind und wie die Teilnahme belegt wird."),
        ["21h"] = new(["A.8.24"], ["CON.1"],
            "Die Umsetzung für die betroffenen Daten, Systeme und Schlüssel über deren Lebenszyklus mit dem konkreten Control abgleichen."),
        ["21i"] = new(["A.5.9", "A.5.15", "A.5.16", "A.5.17", "A.5.18", "A.6.1", "A.6.2", "A.6.5"], ["ORP.2", "ORP.4"],
            "Bestimmen, welche personellen Sicherheitsregeln, Rechteprozesse oder Inventarobjekte das einzelne Control tatsächlich erfasst."),
        ["21j"] = new(["A.5.14", "A.8.5", "A.8.20", "A.8.21"], ["ORP.4", "CON.9", "NET.3.3"],
            "Prüfen, für welche Anmeldungen oder Kommunikationswege das Control anwendbar ist und wie es in Störungen greift."),
        ["23"] = new(["A.5.5", "A.5.24", "A.5.25", "A.5.26", "A.5.28", "A.6.8"], ["DER.2.1"],
            "Das Control kann Erkennung, Eskalation oder Informationssammlung stützen. Für eine Meldung sind Schwelle, Stelle, Fristen und Inhalte gesondert fachlich zu prüfen.")
    };

    internal static IEnumerable<string> Keys(string locator)
    {
        foreach (Match match in Article21().Matches(locator))
            yield return "21" + match.Groups["letter"].Value.ToLowerInvariant();
        if (Article23().IsMatch(locator))
            yield return "23";
    }

    internal static (IReadOnlyList<Nis2ControlReference> References, string ProposedCoverage, string Rationale) Review(
        IReadOnlyCollection<string> keys, IEnumerable<(string Source, string Locator)> locators)
    {
        var references = locators.SelectMany(locator => References(locator.Source, locator.Locator)).Distinct().ToArray();
        var matches = keys.Where(Rules.ContainsKey)
            .Select(key => (Key: key, Rule: Rules[key]))
            .Select(entry => (entry.Key, entry.Rule, References: references.Where(reference => reference.SourceId == IsoSource
                ? entry.Rule.Iso.Contains(reference.Identifier, StringComparer.Ordinal)
                : entry.Rule.Bsi.Contains(reference.Identifier.Split(".A", 2, StringSplitOptions.None)[0],
                    StringComparer.Ordinal)).ToArray()))
            .Where(entry => entry.References.Length > 0).ToArray();
        var matchedReferences = matches.SelectMany(entry => entry.References).Distinct()
            .OrderBy(reference => reference.SourceId, StringComparer.Ordinal)
            .ThenBy(reference => reference.Identifier, StringComparer.Ordinal).ToArray();
        var proposed = matches.Any(entry => entry.Key != "23" && entry.References.Any(reference => reference.SourceId == IsoSource))
            ? "Teilweise"
            : "Unterstützend";
        return (matchedReferences, proposed, string.Join(" ", matches.Select(entry => entry.Rule.Rationale)));
    }

    private static IEnumerable<Nis2ControlReference> References(string source, string locator)
    {
        if (source == IsoSource)
        {
            var editions = IsoEdition().Matches(locator);
            if (editions.Any(match => match.Groups["year"].Value != "2022") ||
                (editions.Count == 0 && ObsoleteIsoEdition().IsMatch(locator)))
                yield break;

            var annexContext = ExplicitAnnex().IsMatch(locator);
            foreach (Match match in IsoControl().Matches(locator))
                if (match.Groups["annex"].Success || annexContext)
                    yield return new(IsoSource, "ISO/IEC 27001:2022, Anhang A", "A." + match.Groups["number"].Value);
        }
        else if (source == BsiSource)
        {
            var editions = BsiEdition().Matches(locator).Select(match => match.Groups["year"].Value).Distinct().ToArray();
            if (editions is ["2023"])
                foreach (Match match in BsiBlock().Matches(locator))
                    yield return new(BsiSource, "BSI IT-Grundschutz-Kompendium, Edition 2023", match.Value.ToUpperInvariant());
        }
    }

    [GeneratedRegex(@"(?<![\w/])(?:Art(?:ikel|icle)?\.?\s*)?21\s*(?:Abs(?:atz)?\.?\s*2|\(\s*2\s*\))\s*(?:(?:Buchst(?:abe)?|lit)\.?\s*|\(\s*)(?<letter>[a-j])\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex Article21();

    [GeneratedRegex(@"(?:\bArt(?:ikel|icle)?\.?\s*23\b|^\s*23\s*(?:Abs\b|\(|$))", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex Article23();

    [GeneratedRegex(@"\b27001\s*[:/-]\s*(?<year>20[0-9]{2})\b", RegexOptions.CultureInvariant)]
    private static partial Regex IsoEdition();

    [GeneratedRegex(@"\b(?:2005|2013)\b", RegexOptions.CultureInvariant)]
    private static partial Regex ObsoleteIsoEdition();

    [GeneratedRegex(@"\b(?:Anhang|Annex)\s+A\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex ExplicitAnnex();

    [GeneratedRegex(@"(?<![\w.])(?<annex>A\.)?(?<number>[5-8]\.[1-9][0-9]?)(?!\w|\.[\w.])", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex IsoControl();

    [GeneratedRegex(@"\b(?:Edition|Ausgabe|Stand(?:\s+Februar)?|IT-Grundschutz(?:-Kompendium)?)(?:\s*[:(]\s*|\s+)(?<year>20[0-9]{2})\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex BsiEdition();

    [GeneratedRegex(@"(?<![\w.])(?:ISMS|ORP|CON|OPS|DER|NET)\.[1-9][0-9]*(?:\.[1-9][0-9]*){0,2}(?:\.A[1-9][0-9]*)?(?!\w|\.[\w.])", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex BsiBlock();
}

public sealed partial class Workspace
{
    public Task<List<Nis2ControlSuggestion>> Nis2ControlSuggestionsAsync(string requirementId, string scope = "") =>
        Read(connection =>
        {
            var requirement = Get(connection, null, requirementId);
            var locators = RequireNis2Requirement(connection, null, requirement);
            var effectiveScope = scope.Length == 0 ? requirement.Scope : scope;
            if (effectiveScope.Length == 0 || requirement.Scope != effectiveScope)
                return [];

            var keys = locators.SelectMany(Nis2Mapping.Keys).Distinct(StringComparer.Ordinal).ToArray();
            if (keys.Length == 0)
                return [];

            return Query(connection, null, """
                WITH ReferencesForControls(RecordId,SourceId,Locator) AS (
                    SELECT Id,SourceId,Locator FROM Records WHERE Kind='Control' AND Scope=$p0
                    UNION
                    SELECT RecordId,SourceId,Locator FROM RecordSources
                        WHERE SourceId IN ('ISO27001','BSIGS')
                )
                SELECT r.*,refs.SourceId,refs.Locator
                FROM Records r JOIN ReferencesForControls refs ON refs.RecordId=r.Id
                WHERE r.Kind='Control' AND r.Scope=$p0 AND refs.SourceId IN ('ISO27001','BSIGS')
                    AND NOT EXISTS (
                        SELECT 1 FROM Links l JOIN MappingAssessments a ON a.LinkId=l.Id
                        WHERE l.SourceRecordId=$p1 AND l.TargetRecordId=r.Id
                            AND l.Type=$p2 AND a.Approval='Bestätigt'
                    )
                ORDER BY r.Code,r.Title,r.Id
                """, reader => (Control: ReadEntry(reader), Source: reader.GetString(14), Locator: reader.GetString(15)),
                    effectiveScope, requirementId, Nis2Mapping.LinkType)
                .GroupBy(entry => entry.Control.Id, StringComparer.Ordinal)
                .Select(group =>
                {
                    var (refs, coverage, rationale) = Nis2Mapping.Review(keys, group.Select(item => (item.Source, item.Locator)));
                    return new Nis2ControlSuggestion(requirementId, group.First().Control, refs, coverage, rationale);
                })
                .Where(suggestion => suggestion.References.Count > 0).ToList();
        });

    public Task<string> ConfirmNis2ControlSuggestionAsync(
        string requirementId, string controlId, string coverage, string scope, string reason,
        int? expectedControlVersion = null, IReadOnlyCollection<Nis2ControlReference>? expectedReferences = null) =>
        SaveNis2ControlMappingAsync(requirementId, controlId, coverage, scope, reason,
            expectedControlVersion, requireSuggestion: true, expectedReferences);

    public Task<string> ConfirmNis2ControlAsync(
        string requirementId, string controlId, string coverage, string scope, string reason, int? expectedControlVersion = null) =>
        SaveNis2ControlMappingAsync(requirementId, controlId, coverage, scope, reason,
            expectedControlVersion, requireSuggestion: false, expectedReferences: null);

    private Task<string> SaveNis2ControlMappingAsync(
        string requirementId, string controlId, string coverage, string scope, string reason,
        int? expectedControlVersion, bool requireSuggestion, IReadOnlyCollection<Nis2ControlReference>? expectedReferences)
    {
        Reason(reason);
        if (coverage is not ("Teilweise" or "Unterstützend" or "Vollständig"))
            throw new ArgumentException("Wählen Sie eine Abdeckung aus.", nameof(coverage));
        if (string.IsNullOrWhiteSpace(scope))
            throw new ArgumentException("Wählen Sie einen Geltungsbereich für die Zuordnung aus.", nameof(scope));

        return Change("Nis2ControlMappingConfirmed", requirementId,
            JsonSerializer.Serialize(new { controlId, coverage, scope, reason, fromSuggestion = requireSuggestion }),
            (connection, transaction) =>
            {
                var requirement = Get(connection, transaction, requirementId);
                var locators = RequireNis2Requirement(connection, transaction, requirement);
                var control = Get(connection, transaction, controlId);
                RequireKind(control, "Control");
                if (scope != requirement.Scope || scope != control.Scope)
                    throw new ArgumentException("Anforderung und Control müssen zum gewählten Geltungsbereich gehören.");
                if (expectedControlVersion is { } version && version != control.Version)
                    throw new ArgumentException("Das Control wurde seit der Auswahl geändert. Laden Sie die Vorschläge erneut.");

                var confirmedReason = reason.Trim();
                if (requireSuggestion)
                {
                    var keys = locators.SelectMany(Nis2Mapping.Keys).Distinct(StringComparer.Ordinal).ToArray();
                    var controlLocators = ReadNis2Locators(connection, transaction, control);
                    var (references, _, _) = Nis2Mapping.Review(keys, controlLocators);
                    if (references.Count == 0)
                        throw new ArgumentException("Für dieses Control liegt kein passender Vorschlag mehr vor. Prüfen Sie Quelle, Fundstelle und Ausgabe.");
                    if (expectedReferences is not null && !references.ToHashSet().SetEquals(expectedReferences))
                        throw new ArgumentException("Die Fundstellen für diesen Vorschlag wurden geändert. Laden Sie die Vorschläge erneut.");
                    confirmedReason += $"\nBezug des Vorschlags: {string.Join(", ", references)}.";
                    Reason(confirmedReason);
                }

                var existing = Query(connection, transaction, """
                    SELECT l.Id,coalesce(a.Approval,'Entwurf') FROM Links l
                    LEFT JOIN MappingAssessments a ON a.LinkId=l.Id
                    WHERE l.SourceRecordId=$p0 AND l.TargetRecordId=$p1 AND l.Type=$p2
                    """, reader => (Id: reader.GetString(0), Approval: reader.GetString(1)),
                        requirementId, controlId, Nis2Mapping.LinkType).SingleOrDefault();
                if (existing.Approval == "Bestätigt")
                    throw new ArgumentException("Diese Zuordnung ist bereits bestätigt.");

                var linkId = existing.Id ?? Guid.NewGuid().ToString("N");
                if (existing.Id is null)
                    Exec(connection, transaction, """
                        INSERT INTO Links(Id,SourceRecordId,TargetRecordId,Type,Reason,SourceId)
                        VALUES($p0,$p1,$p2,$p3,$p4,$p5)
                        """, linkId, requirementId, controlId, Nis2Mapping.LinkType, confirmedReason, Nis2Mapping.Directive);
                else
                    Exec(connection, transaction, "UPDATE Links SET Reason=$p0,SourceId=$p1 WHERE Id=$p2",
                        confirmedReason, Nis2Mapping.Directive, linkId);

                Exec(connection, transaction, """
                    INSERT INTO MappingAssessments(LinkId,Coverage,Scope,Approval) VALUES($p0,$p1,$p2,'Bestätigt')
                    ON CONFLICT(LinkId) DO UPDATE SET Coverage=excluded.Coverage,Scope=excluded.Scope,Approval=excluded.Approval
                    """, linkId, coverage, scope);
                return linkId;
            });
    }

    private static string[] RequireNis2Requirement(SqliteConnection connection, SqliteTransaction? transaction, Entry requirement)
    {
        RequireKind(requirement, "Requirement");
        var locators = ReadNis2Locators(connection, transaction, requirement)
            .Where(reference => reference.Source == Nis2Mapping.Directive).Select(reference => reference.Locator).ToArray();
        if (locators.Length == 0)
            throw new ArgumentException("Die Anforderung benötigt einen Quellenbezug zur NIS2-Richtlinie.");
        return locators;
    }

    private static List<(string Source, string Locator)> ReadNis2Locators(
        SqliteConnection connection, SqliteTransaction? transaction, Entry entry) =>
        Query(connection, transaction, """
            SELECT SourceId,Locator FROM RecordSources WHERE RecordId=$p0
            UNION SELECT $p1,$p2
            """, reader => (reader.GetString(0), reader.GetString(1)), entry.Id, entry.SourceId, entry.Locator);
}
