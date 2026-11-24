using System.Globalization;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using static D3.Grc.Core.Database;

namespace D3.Grc.Core;

public enum IksControlType { Preventive, Detective, Corrective }
public enum IksControlMode { Manual, Automated, Hybrid }
public enum IksFrequency { EventBased, Daily, Weekly, Monthly, Quarterly, HalfYearly, Yearly }
public enum IksCheckType { Execution, DesignAssessment, SelfAssessment, IndependentTest }
public enum IksCheckOutcome { Confirmed, Deviation, NotApplicable }

public sealed record IksProfile(string Objective, IksControlType Type, IksControlMode Mode, IksFrequency Frequency, bool IsKeyControl);

public sealed record IksCheckInput(DateOnly CheckedOn, string Reviewer, IksCheckType Type, IksCheckOutcome Outcome,
    string TestApproach, string Notes, string? EvidenceId, string? MeasureId);

public sealed record IksCheck(string Id, string ControlId, int ControlVersion, IksCheckInput Result, string RecordedUtc);

public sealed partial class Workspace
{
    private const string IksDateFormat = "yyyy-MM-dd";

    public Task<IReadOnlyDictionary<string, IksProfile>> IksProfilesAsync(IReadOnlyCollection<string> controlIds) =>
        Read<IReadOnlyDictionary<string, IksProfile>>(connection => Query(connection, null, """
            SELECT ControlId, Objective, ControlType, ControlMode, Frequency, IsKeyControl FROM IksProfiles
            WHERE ControlId IN (SELECT value FROM json_each($p0))
            """, reader => (Id: reader.GetString(0), Profile: new IksProfile(reader.GetString(1),
                (IksControlType)reader.GetInt32(2), (IksControlMode)reader.GetInt32(3), (IksFrequency)reader.GetInt32(4),
                reader.GetBoolean(5))), JsonSerializer.Serialize(controlIds))
            .ToDictionary(row => row.Id, row => row.Profile, StringComparer.Ordinal));

    public Task<List<IksCheck>> IksChecksAsync(string controlId) => Read(connection => Query(connection, null, """
        SELECT Id, ControlId, ControlVersion, CheckedOn, Reviewer, CheckType, Outcome, TestApproach, Notes,
               EvidenceId, MeasureId, RecordedUtc
        FROM IksChecks WHERE ControlId=$p0 ORDER BY CheckedOn DESC, rowid DESC
        """, reader => new IksCheck(reader.GetString(0), reader.GetString(1), reader.GetInt32(2),
            new IksCheckInput(DateOnly.ParseExact(reader.GetString(3), IksDateFormat, CultureInfo.InvariantCulture),
                reader.GetString(4), (IksCheckType)reader.GetInt32(5), (IksCheckOutcome)reader.GetInt32(6),
                reader.GetString(7), reader.GetString(8), reader.IsDBNull(9) ? null : reader.GetString(9),
                reader.IsDBNull(10) ? null : reader.GetString(10)), reader.GetString(11)), controlId));

    public Task SaveIksProfileAsync(string controlId, IksProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);
        profile = profile with { Objective = profile.Objective?.Trim() ?? "" };
        if (profile.Objective.Length is < 1 or > 4000 || !Enum.IsDefined(profile.Type)
            || !Enum.IsDefined(profile.Mode) || !Enum.IsDefined(profile.Frequency))
            throw new ArgumentException("Kontrollziel, Art, Ausführung und Turnus vollständig angeben.");

        return Change("IksProfileSaved", controlId, JsonSerializer.Serialize(profile), (connection, transaction) =>
        {
            RequireKind(Get(connection, transaction, controlId), "Control");
            SaveIksProfile(connection, transaction, controlId, profile);
            return true;
        });
    }

    private static void SaveIksProfile(SqliteConnection connection, SqliteTransaction transaction, string controlId, IksProfile profile) =>
        Exec(connection, transaction, """
            INSERT INTO IksProfiles (ControlId, Objective, ControlType, ControlMode, Frequency, IsKeyControl)
            VALUES ($p0, $p1, $p2, $p3, $p4, $p5)
            ON CONFLICT(ControlId) DO UPDATE SET Objective=excluded.Objective, ControlType=excluded.ControlType,
                ControlMode=excluded.ControlMode, Frequency=excluded.Frequency, IsKeyControl=excluded.IsKeyControl
            """, controlId, profile.Objective, (int)profile.Type, (int)profile.Mode, (int)profile.Frequency, profile.IsKeyControl);

    public Task RecordIksCheckAsync(string controlId, IksCheckInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        input = input with
        {
            Reviewer = input.Reviewer?.Trim() ?? "", TestApproach = input.TestApproach?.Trim() ?? "", Notes = input.Notes?.Trim() ?? ""
        };
        if (input.CheckedOn == DateOnly.MinValue || input.CheckedOn > DateOnly.FromDateTime(DateTime.Today))
            throw new ArgumentException("Ein gültiges Prüfdatum bis einschließlich heute angeben.");
        if (input.Reviewer.Length is < 1 or > 240 || input.TestApproach.Length is < 1 or > 4000
            || input.Notes.Length is < 1 or > 30_000 || !Enum.IsDefined(input.Type) || !Enum.IsDefined(input.Outcome))
            throw new ArgumentException("Person, Prüfart, Prüfansatz, Ergebnis und Begründung vollständig angeben.");
        if (input.EvidenceId is null && input.Outcome == IksCheckOutcome.Confirmed
            && input.Type is IksCheckType.Execution or IksCheckType.IndependentTest)
            throw new ArgumentException("Für eine bestätigte Durchführung und einen bestätigten unabhängigen Test einen Nachweis wählen.");

        var id = Guid.NewGuid().ToString("N");
        return Change("IksCheckRecorded", controlId, JsonSerializer.Serialize(new { id, input }), (connection, transaction) =>
        {
            var control = Get(connection, transaction, controlId);
            RequireKind(control, "Control");
            if (input.Type == IksCheckType.IndependentTest
                && string.Equals(control.Owner.Trim(), input.Reviewer, StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("Für einen unabhängigen Test eine andere Person als die Kontrollverantwortung eintragen.");
            if (input.EvidenceId is { } evidenceId)
                RequireIksProof(connection, transaction, evidenceId, controlId, input.CheckedOn);
            if (input.MeasureId is { } measureId)
            {
                var measure = Get(connection, transaction, measureId);
                RequireKind(measure, "Measure");
                if (control.Scope != measure.Scope)
                    throw new ArgumentException("Control und Maßnahme müssen im selben Geltungsbereich liegen.");
            }
            InsertIksCheck(connection, transaction, id, control, input, DateTimeOffset.UtcNow);
            return true;
        });
    }

    private static void InsertIksCheck(SqliteConnection connection, SqliteTransaction transaction, string id,
        Entry control, IksCheckInput input, DateTimeOffset recordedAt) =>
        Exec(connection, transaction, """
            INSERT INTO IksChecks (Id, ControlId, ControlVersion, CheckedOn, Reviewer, CheckType, Outcome, TestApproach,
                Notes, EvidenceId, MeasureId, RecordedUtc)
            VALUES ($p0, $p1, $p2, $p3, $p4, $p5, $p6, $p7, $p8, $p9, $p10, $p11)
            """, id, control.Id, control.Version, input.CheckedOn.ToString(IksDateFormat, CultureInfo.InvariantCulture),
            input.Reviewer, (int)input.Type, (int)input.Outcome, input.TestApproach, input.Notes, input.EvidenceId,
            input.MeasureId, recordedAt.ToString("O", CultureInfo.InvariantCulture));

    private static void RequireIksProof(SqliteConnection connection, SqliteTransaction transaction, string evidenceId,
        string controlId, DateOnly on)
    {
        var proof = Proofs(connection, transaction, controlId).SingleOrDefault(item => item.Id == evidenceId)
            ?? throw new ArgumentException("Nachweis passt nicht zum Control.");
        var start = new DateTimeOffset(on.ToDateTime(TimeOnly.MinValue, DateTimeKind.Local));
        var end = new DateTimeOffset(on.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Local));
        if (!DateTimeOffset.TryParse(proof.Collected, CultureInfo.InvariantCulture, DateTimeStyles.None, out var collected)
            || collected >= end || collected > DateTimeOffset.UtcNow)
            throw new ArgumentException("Nachweis war am Prüftag noch nicht erhoben.");
        RequireProof(connection, transaction, evidenceId, controlId, collected > start ? collected : start);
    }
}
