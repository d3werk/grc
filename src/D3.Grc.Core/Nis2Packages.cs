using System.Text.Json;
using Microsoft.Data.Sqlite;
using static D3.Grc.Core.Database;

namespace D3.Grc.Core;

public sealed record Nis2LinkedControl(string Id, string Code, string Title, string SourceId, string Approval = "Entwurf");

public sealed record Nis2PackageState(string? RequirementId, IReadOnlyDictionary<string, string> ControlIds,
    IReadOnlyDictionary<string, string> MeasureIds, IReadOnlyList<Nis2LinkedControl> LinkedControls,
    IReadOnlyList<Entry> RequirementChoices);

public sealed record Nis2PackageResult(Nis2PackageState State, int RequirementsCreated, int ControlsCreated,
    int MeasuresCreated, int ControlsReused);

public sealed partial class Workspace
{
    private const string RequirementItem = "requirement";
    private const string ControlItemPrefix = "control/";
    private const string MeasureItemPrefix = "measure/";
    private const string RequirementControlRelation = "Anforderung → Control";
    private const string MeasureControlRelation = "Maßnahme → Control";

    public Task<Nis2PackageState> Nis2PackageStateAsync(string packageKey, string scope)
    {
        var package = Nis2Catalog.Get(packageKey);
        ValidateNis2Scope(scope);
        return Read(connection => Nis2State(connection, null, package, scope));
    }

    public Task<Nis2PackageResult> Nis2ApplyRequirementAsync(string packageKey, string scope, string? reuseRequirementId = null) =>
        Nis2ApplyPackageAsync(packageKey, scope, false, reuseRequirementId: reuseRequirementId, controlKeys: []);

    public Task<Nis2PackageResult> Nis2ApplyPackageAsync(string packageKey, string scope, bool createMeasures,
        IReadOnlyDictionary<string, string>? reuseControlIds = null, string? reuseRequirementId = null,
        IReadOnlyCollection<string>? controlKeys = null)
    {
        var package = Nis2Catalog.Get(packageKey);
        ValidateNis2Scope(scope);
        var requested = (controlKeys ?? package.Controls.Select(control => control.Key).ToArray()).ToHashSet(StringComparer.Ordinal);
        var overrides = reuseControlIds?.ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal) ?? [];
        if (requested.Except(package.Controls.Select(control => control.Key), StringComparer.Ordinal).Any() ||
            overrides.Keys.Any(key => !requested.Contains(key)) || overrides.Values.Any(string.IsNullOrWhiteSpace))
            throw new ArgumentException("Die ausgewählte Control-Vorlage oder Zuordnung passt nicht zum Maßnahmenpaket.");
        var items = package.Controls.Where(control => requested.Contains(control.Key)).ToArray();

        return Change("Nis2PackageApplied", package.Key,
            JsonSerializer.Serialize(new { scope, createMeasures, controlKeys = items.Select(control => control.Key),
                reuseControlIds = overrides, reuseRequirementId }),
            (connection, transaction) => ApplyNis2Package(connection, transaction, package, scope, createMeasures,
                overrides, reuseRequirementId, items, controlKeys is not null));
    }

    private static Nis2PackageResult ApplyNis2Package(SqliteConnection connection, SqliteTransaction transaction,
        Nis2Package package, string scope, bool createMeasures, IReadOnlyDictionary<string, string> reuseControlIds,
        string? reuseRequirementId, IReadOnlyList<Nis2ControlTemplate> controls, bool explicitControlSelection)
    {
        var state = Nis2State(connection, transaction, package, scope);
        if (state.RequirementChoices.Count > 1 && reuseRequirementId is null)
            throw new ArgumentException("Mehrere Anforderungen verwenden diese NIS2-Fundstelle im selben Geltungsbereich. Wählen Sie die passende Anforderung aus.");
        if (state.RequirementId is not null && reuseRequirementId is not null && state.RequirementId != reuseRequirementId)
            throw new ArgumentException("Dieses Paket ist bereits mit einer anderen Anforderung verbunden.");
        if (reuseRequirementId is not null)
        {
            var candidate = Nis2GetReusable(connection, transaction, reuseRequirementId, "Requirement", scope);
            if (candidate.Locator != package.Reference || candidate.SourceId != Nis2Catalog.Source)
                throw new ArgumentException("Die vorhandene Anforderung muss die passende NIS2-Fundstelle als Primärquelle führen.");
        }
        var known = state.ControlIds.Values.Concat(reuseControlIds.Values).ToHashSet(StringComparer.Ordinal);
        if (!explicitControlSelection && state.LinkedControls.Any(control => !known.Contains(control.Id)) &&
            controls.Any(control => !state.ControlIds.ContainsKey(control.Key) && !reuseControlIds.ContainsKey(control.Key)))
            throw new ArgumentException("Die Anforderung hat bereits Controls. Vorlagen einzeln auswählen oder vorhandene Controls zur Wiederverwendung zuordnen.");

        var requirementId = state.RequirementId ?? reuseRequirementId;
        var requirementCreated = requirementId is null;
        if (requirementId is null)
            requirementId = InsertNis2Record(connection, transaction, package, scope, "Requirement", package.Code, package.Title,
                $"{package.Requirement}\n\nAnwendbarkeit und konkrete Umsetzung für diesen Geltungsbereich fachlich bewerten.\n\nQuelle\n{Nis2Catalog.OfficialSource}");
        TrackNis2Record(connection, transaction, package, scope, RequirementItem, requirementId);

        var controlsCreated = 0;
        var controlsReused = 0;
        var measuresCreated = 0;
        foreach (var control in controls)
        {
            state.ControlIds.TryGetValue(control.Key, out var tracked);
            reuseControlIds.TryGetValue(control.Key, out var explicitReuse);
            if (tracked is not null && explicitReuse is not null && tracked != explicitReuse)
                throw new ArgumentException($"Für „{control.Title}“ ist bereits ein anderer Control zugeordnet.");

            var controlId = tracked ?? explicitReuse;
            if (controlId is null)
            {
                controlId = InsertNis2Record(connection, transaction, package, scope, "Control", $"{package.Code}-{control.Key}",
                    control.Title, Nis2ControlBody(control));
                controlsCreated++;
            }
            else if (tracked is null)
            {
                Nis2GetReusable(connection, transaction, controlId, "Control", scope);
                controlsReused++;
            }

            TrackNis2Record(connection, transaction, package, scope, ControlItemPrefix + control.Key, controlId);
            EnsureNis2Link(connection, transaction, requirementId, controlId, RequirementControlRelation, scope,
                $"Zuordnung zu {package.Reference}: Fachliche Abdeckung und Wirksamkeit für „{control.Title}“ gesondert prüfen.");

            if (!createMeasures)
                continue;
            foreach (var measure in control.Measures)
            {
                var key = $"{control.Key}/{measure.Key}";
                if (!state.MeasureIds.TryGetValue(key, out var measureId))
                {
                    measureId = InsertNis2Record(connection, transaction, package, scope, "Measure",
                        $"{package.Code}-{control.Key}-{measure.Key}", measure.Title,
                        $"Umsetzung\n{measure.Instructions}\n\nAbnahmekriterium\n{measure.Acceptance}\n\nVorgeschlagene Rolle\n{control.OwnerRole}");
                    TrackNis2Record(connection, transaction, package, scope, MeasureItemPrefix + key, measureId);
                    measuresCreated++;
                }
                EnsureNis2Link(connection, transaction, measureId, controlId, MeasureControlRelation, scope,
                    $"Umsetzungsvorschlag zu „{control.Title}“ gemäß {package.Reference}.");
            }
        }

        return new Nis2PackageResult(Nis2State(connection, transaction, package, scope), requirementCreated ? 1 : 0,
            controlsCreated, measuresCreated, controlsReused);
    }

    private static Nis2PackageState Nis2State(SqliteConnection connection, SqliteTransaction? transaction,
        Nis2Package package, string scope)
    {
        var materializations = Query(connection, transaction, """
            SELECT m.ItemKey, r.Id, r.Scope FROM Nis2Materializations m JOIN Records r ON r.Id=m.RecordId
            WHERE m.Scope=$p0 AND m.PackageKey=$p1 ORDER BY m.ItemKey
            """, reader => (Key: reader.GetString(0), Id: reader.GetString(1), Scope: reader.GetString(2)), scope, package.Key);
        if (materializations.Any(item => item.Scope != scope))
            throw new ArgumentException("Ein zugeordnetes Paketobjekt wurde in einen anderen Geltungsbereich verschoben.");
        var requirementId = materializations.FirstOrDefault(item => item.Key == RequirementItem).Id;
        if (requirementId is not null && Scalar(connection, transaction, """
            SELECT Id FROM Records WHERE Id=$p0 AND Kind='Requirement' AND SourceId=$p1 AND Locator=$p2
            """, requirementId, Nis2Catalog.Source, package.Reference) == "")
            throw new ArgumentException("Die Primärquelle oder Fundstelle der zugeordneten NIS2-Anforderung wurde geändert.");
        IReadOnlyList<Entry> candidates = [];
        if (requirementId is null)
        {
            candidates = Query(connection, transaction, """
                SELECT * FROM Records WHERE Kind='Requirement' AND SourceId=$p0 AND Locator=$p1 AND Scope=$p2 ORDER BY Code,Title,Id
                """, ReadEntry, Nis2Catalog.Source, package.Reference, scope);
            if (candidates.Count == 1)
                requirementId = candidates[0].Id;
        }
        var controlIds = MaterializationIds(materializations, ControlItemPrefix);
        var measureIds = MaterializationIds(materializations, MeasureItemPrefix);
        IReadOnlyList<Nis2LinkedControl> linked = requirementId is null ? [] : Query(connection, transaction, """
            SELECT r.Id,r.Code,r.Title,r.SourceId,coalesce(a.Approval,'Entwurf')
            FROM Links l JOIN Records r ON r.Id=l.TargetRecordId LEFT JOIN MappingAssessments a ON a.LinkId=l.Id
            WHERE l.SourceRecordId=$p0 AND l.Type=$p1 AND r.Kind='Control' AND r.Scope=$p2
            ORDER BY r.Code,r.Title,r.Id
            """, reader => new Nis2LinkedControl(reader.GetString(0), reader.GetString(1), reader.GetString(2), reader.GetString(3), reader.GetString(4)),
            requirementId, RequirementControlRelation, scope);
        return new Nis2PackageState(requirementId, controlIds, measureIds, linked, candidates);
    }

    private static void ValidateNis2PackageEdit(SqliteConnection connection, SqliteTransaction transaction, Entry entry, Entry? previous)
    {
        if (previous is null)
            return;
        if (entry.Scope != previous.Scope && Scalar(connection, transaction,
                "SELECT RecordId FROM Nis2Materializations WHERE RecordId=$p0 LIMIT 1", entry.Id) != "")
            throw new ArgumentException("Der Datensatz gehört zu einem NIS2-Paket in diesem Geltungsbereich.");
        if (entry.Kind == "Requirement" && (entry.SourceId != previous.SourceId || entry.Locator != previous.Locator) &&
            Scalar(connection, transaction, "SELECT RecordId FROM Nis2Materializations WHERE RecordId=$p0 AND ItemKey=$p1 LIMIT 1",
                entry.Id, RequirementItem) != "")
            throw new ArgumentException("Die Primärquelle und Fundstelle der NIS2-Paketanforderung werden im Katalog verwendet.");
    }

    private static Dictionary<string, string> MaterializationIds(IEnumerable<(string Key, string Id, string Scope)> items, string prefix) =>
        items.Where(item => item.Key.StartsWith(prefix, StringComparison.Ordinal))
            .ToDictionary(item => item.Key[prefix.Length..], item => item.Id, StringComparer.Ordinal);

    private static Entry Nis2GetReusable(SqliteConnection connection, SqliteTransaction transaction, string id, string kind, string scope)
    {
        var entry = Get(connection, transaction, id);
        if (entry.Kind != kind || entry.Scope != scope)
            throw new ArgumentException("Für die Wiederverwendung den passenden Datensatz aus demselben Geltungsbereich wählen.");
        return entry;
    }

    private static string InsertNis2Record(SqliteConnection connection, SqliteTransaction transaction, Nis2Package package,
        string scope, string kind, string code, string title, string body)
    {
        var entry = new Entry { Kind = kind, Code = code, SourceId = Nis2Catalog.Source, Locator = package.Reference,
            Title = title, Body = body, Scope = scope };
        entry.Validate();
        Exec(connection, transaction, """
            INSERT INTO Records(Id,Kind,Code,Title,SourceId,Locator,Body,Scope) VALUES($p0,$p1,$p2,$p3,$p4,$p5,$p6,$p7)
            """, entry.Id, entry.Kind, entry.Code, entry.Title, entry.SourceId, entry.Locator, entry.Body, entry.Scope);
        Exec(connection, transaction, "INSERT INTO RecordTopics VALUES ($p0,$p1)", entry.Id, Nis2Catalog.Topic);
        Exec(connection, transaction, "INSERT INTO RecordSources VALUES ($p0,$p1,$p2,'Primär')", entry.Id, entry.SourceId, entry.Locator);
        return entry.Id;
    }

    private static void TrackNis2Record(SqliteConnection connection, SqliteTransaction transaction, Nis2Package package,
        string scope, string key, string recordId) => Exec(connection, transaction, """
        INSERT INTO Nis2Materializations(Scope,PackageKey,ItemKey,RecordId) VALUES ($p0,$p1,$p2,$p3)
        ON CONFLICT(Scope,PackageKey,ItemKey) DO NOTHING
        """, scope, package.Key, key, recordId);

    private static void EnsureNis2Link(SqliteConnection connection, SqliteTransaction transaction, string from, string to,
        string type, string scope, string reason)
    {
        var id = Guid.NewGuid().ToString("N");
        if (Exec(connection, transaction, """
            INSERT INTO Links(Id,SourceRecordId,TargetRecordId,Type,Reason,SourceId) VALUES ($p0,$p1,$p2,$p3,$p4,$p5)
            ON CONFLICT(SourceRecordId,TargetRecordId,Type) DO NOTHING
            """, id, from, to, type, reason, Nis2Catalog.Source) > 0)
            Exec(connection, transaction, "INSERT INTO MappingAssessments VALUES ($p0,'Unterstützend',$p1,'Entwurf')", id, scope);
    }

    private static string Nis2ControlBody(Nis2ControlTemplate template) =>
        $"Umsetzung\n{template.Implementation}\n\nErwartete Nachweise und Prüfkriterien\n{template.Evidence}" +
        $"\n\nVorgeschlagene Rolle\n{template.OwnerRole}\n\nPrüfvorschlag\nAlle {template.ReviewMonths} Monate. " +
        $"Anlassbezogen prüfen: {template.ReviewTriggers}\n\nBeispielrisiko\n{template.RiskScenario}";

    private static void ValidateNis2Scope(string scope)
    {
        if (string.IsNullOrWhiteSpace(scope) || scope != scope.Trim() || scope.Length > 1000 || scope.Contains('\0'))
            throw new ArgumentException("Bitte einen konkreten, gültigen Geltungsbereich für das NIS2-Paket wählen.", nameof(scope));
    }
}
