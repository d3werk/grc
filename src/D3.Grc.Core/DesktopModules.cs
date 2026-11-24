using System.Globalization;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using static D3.Grc.Core.Database;

namespace D3.Grc.Core;

public sealed record ModuleField(string Key, string Label, string Type = "text", string[]? Options = null);

public sealed record ModuleSpec(string Kind, string Title, string Singular, string Description, string Topic,
    ModuleField[] Fields, string[]? Statuses = null, Action<Entry, IReadOnlyDictionary<string, string>>? Validate = null);

public sealed record ModuleSeed(Entry Entry, string Topic, Dictionary<string, string>? Fields = null);
public sealed record ModuleLink(string From, string To, string Type, string Reason);
public sealed record ModuleRelation(string Type, string FromKind, string ToKind);
public sealed record ModuleRow(Entry Entry, IReadOnlyDictionary<string, string> Fields);

public static class DesktopModules
{
    public static IReadOnlyDictionary<string, ModuleSpec> Specs { get; } =
        ProcessModule.Specs.Concat(ResilienceModule.Specs).Concat(AiModule.Specs).Concat(ContractModule.Specs)
            .ToDictionary(spec => spec.Kind, StringComparer.Ordinal);

    public static IEnumerable<ModuleRelation> Relations =>
        ProcessModule.Relations.Concat(ResilienceModule.Relations).Concat(AiModule.Relations).Concat(ContractModule.Relations);

    internal static Dictionary<string, string> ValidateFields(Entry entry, IReadOnlyDictionary<string, string> input)
    {
        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        if (!Specs.TryGetValue(entry.Kind, out var spec))
        {
            if (input.Count > 0)
                throw new ArgumentException("Dieser Eintrag unterstützt keine Fachfelder.");
            return result;
        }

        foreach (var (key, raw) in input)
        {
            var field = spec.Fields.SingleOrDefault(candidate => candidate.Key == key)
                ?? throw new ArgumentException("Unbekanntes Fachfeld.");
            var value = raw.Trim();
            if (value.Length == 0)
                continue;
            if (value.Length > (field.Type == "steps" ? 60_000 : 5000) || value.Contains('\0'))
                throw new ArgumentException($"{field.Label} überschreitet die zulässige Länge oder enthält ein ungültiges Zeichen.");
            if (field.Type == "date" && !DateOnly.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
                throw new ArgumentException($"{field.Label} benötigt ein Datum im Format JJJJ-MM-TT.");
            if (field.Type == "number" && !decimal.TryParse(value, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint,
                    CultureInfo.InvariantCulture, out _))
                throw new ArgumentException($"{field.Label} benötigt eine Zahl. Dezimalstellen mit Punkt eingeben.");
            if (field.Type == "select" && field.Options?.Contains(value, StringComparer.Ordinal) != true)
                throw new ArgumentException($"Bitte für {field.Label} einen Wert aus der Liste auswählen.");
            result.Add(key, value);
        }
        spec.Validate?.Invoke(entry, result);
        return result;
    }
}

public sealed partial class Workspace
{
    public Task<IReadOnlyDictionary<string, string>> ModuleFieldsAsync(string id) =>
        Read<IReadOnlyDictionary<string, string>>(connection => ReadModuleFields(connection, null, id));

    public Task<List<ModuleRow>> ModuleRowsAsync(string kind, string scope = "") => Read(connection =>
        Query(connection, null, """
            SELECT r.*, coalesce(m.Data, '{}') FROM Records r LEFT JOIN DesktopModuleFields m ON m.RecordId=r.Id
            WHERE r.Kind=$p0 AND ($p1='' OR r.Scope=$p1) ORDER BY r.Code,r.Title
            """, reader => new ModuleRow(ReadEntry(reader), DecodeModuleFields(reader.GetString(14))), kind, scope));

    private static Dictionary<string, string> DecodeModuleFields(string data) =>
        data.Length == 0 ? [] : JsonSerializer.Deserialize<Dictionary<string, string>>(data) ?? [];

    private static Dictionary<string, string> ReadModuleFields(SqliteConnection connection, SqliteTransaction? transaction, string id) =>
        DecodeModuleFields(Scalar(connection, transaction, "SELECT Data FROM DesktopModuleFields WHERE RecordId=$p0", id));

    private static void WriteModuleFields(SqliteConnection connection, SqliteTransaction transaction, string id,
        Dictionary<string, string> fields)
    {
        if (fields.Count == 0)
        {
            Exec(connection, transaction, "DELETE FROM DesktopModuleFields WHERE RecordId=$p0", id);
            return;
        }

        Exec(connection, transaction, """
            INSERT INTO DesktopModuleFields(RecordId,Data) VALUES($p0,$p1)
            ON CONFLICT(RecordId) DO UPDATE SET Data=excluded.Data
            """, id, JsonSerializer.Serialize(fields));
    }

    private static void ValidateProcessLink(SqliteConnection connection, SqliteTransaction transaction, Entry from, Entry to)
    {
        if (from.Scope != to.Scope)
            throw new ArgumentException("Über- und Unterprozess gehören zum selben Geltungsbereich.");
        if (Scalar(connection, transaction,
            "SELECT Id FROM Links WHERE SourceRecordId=$p0 AND Type=$p1", from.Id, ProcessModule.ParentType) != "")
            throw new ArgumentException("Der Prozess hat bereits einen übergeordneten Prozess.");
        if (Scalar(connection, transaction, """
            WITH RECURSIVE Ancestors(Id) AS (
                VALUES ($p0) UNION SELECT l.TargetRecordId FROM Links l JOIN Ancestors a ON a.Id=l.SourceRecordId
                WHERE l.Type=$p2
            )
            SELECT Id FROM Ancestors WHERE Id=$p1
            """, to.Id, from.Id, ProcessModule.ParentType) != "")
            throw new ArgumentException("Diese Zuordnung würde einen Kreis in der Prozesslandkarte erzeugen.");
    }

    private static void ValidateProcessScopeChange(SqliteConnection connection, SqliteTransaction transaction, Entry entry, Entry? old)
    {
        if (entry.Kind != ProcessModule.Kind || old is null || entry.Scope == old.Scope)
            return;
        if (Scalar(connection, transaction, """
            SELECT l.Id FROM Links l JOIN Records other ON other.Id=
                CASE WHEN l.SourceRecordId=$p0 THEN l.TargetRecordId ELSE l.SourceRecordId END
            WHERE l.Type=$p1 AND (l.SourceRecordId=$p0 OR l.TargetRecordId=$p0) AND other.Scope<>$p2 LIMIT 1
            """, entry.Id, ProcessModule.ParentType, entry.Scope) != "")
            throw new ArgumentException("Lösen Sie zuerst die Prozesshierarchie, um den Geltungsbereich zu ändern.");
    }

    internal async Task CopyWithPasswordAsync(string path, string password)
    {
        await _gate.WaitAsync();
        try
        {
            Demand();
            if (File.Exists(path))
                throw new IOException("Die Zieldatei existiert.");
            using var cipher = new Cipher();
            await cipher.InitializeAsync(password);
            await Snapshot(path, cipher);
        }
        finally { _gate.Release(); }
    }
}
