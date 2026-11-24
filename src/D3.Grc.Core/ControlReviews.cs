using System.Globalization;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using static D3.Grc.Core.Database;

namespace D3.Grc.Core;

public sealed record ControlReviewSchedule(int IntervalMonths, DateOnly NextReviewOn, int Version);
public sealed record ControlReview(
    string Id, string ControlId, DateOnly PerformedOn, int ControlVersion, string Owner, string Reviewer,
    string Outcome, string Notes, string? EvidenceId, string EvidenceHash, string RecordedUtc);
public sealed record ControlReviewItem(Entry Control, ControlReviewSchedule? Schedule,
    IReadOnlyList<string> RiskIds, IReadOnlyList<TraceEvidence> Evidence);

public sealed partial class Workspace
{
    public static IReadOnlyList<string> ControlReviewOutcomes { get; } =
        Array.AsReadOnly(new[] { "Wirksam", "Teilweise wirksam", "Verbesserungsbedarf" });

    private static string ReviewDate(DateOnly date) => date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
    private static DateOnly ReviewDate(string date) => DateOnly.ParseExact(date, "yyyy-MM-dd", CultureInfo.InvariantCulture);

    public Task<List<ControlReviewItem>> ControlReviewItemsAsync(string scope, DateTimeOffset asOf) => Read(connection =>
    {
        var entries = Query(connection, null, """
            SELECT r.*, s.IntervalMonths, s.NextReviewOn, s.Version
            FROM Records r LEFT JOIN ControlReviewSchedules s ON s.ControlId=r.Id
            WHERE r.Kind='Control' AND ($p0='' OR r.Scope=$p0)
            ORDER BY (s.NextReviewOn IS NULL),s.NextReviewOn,r.Code,r.Title
            """, reader => (Control: ReadEntry(reader),
            Schedule: reader.IsDBNull(14) ? null : new ControlReviewSchedule(
                reader.GetInt32(14), ReviewDate(reader.GetString(15)), reader.GetInt32(16))), scope);
        var risks = Query(connection, null, """
            SELECT l.TargetRecordId, l.SourceRecordId
            FROM Links l JOIN Records r ON r.Id=l.TargetRecordId
            WHERE l.Type='Risiko → Control' AND ($p0='' OR r.Scope=$p0)
            """, reader => (Target: reader.GetString(0), Source: reader.GetString(1)), scope)
            .ToLookup(link => link.Target, link => link.Source);
        var evidence = ReadControlEvidence(connection, "SELECT Id FROM Records WHERE Kind='Control' AND ($p0='' OR Scope=$p0)", scope, asOf);
        return entries.Select(row => new ControlReviewItem(row.Control, row.Schedule, risks[row.Control.Id].ToArray(),
            evidence[row.Control.Id].ToArray())).ToList();
    });

    public Task<(int Overdue, int Upcoming)> ControlReminderCountsAsync(DateOnly today, int days = 14)
    {
        if (days is < 0 or > 365)
            throw new ArgumentOutOfRangeException(nameof(days));
        var upcoming = today.AddDays(days);
        return Read(connection => Query(connection, null, """
            SELECT COALESCE(SUM(NextReviewOn < $p0),0),
                   COALESCE(SUM(NextReviewOn >= $p0 AND NextReviewOn <= $p1),0)
            FROM ControlReviewSchedules
            """, row => (row.GetInt32(0), row.GetInt32(1)), ReviewDate(today), ReviewDate(upcoming)).Single());
    }

    public Task<List<ControlReview>> ControlReviewsAsync(string controlId) => Read(connection =>
    {
        RequireKind(Get(connection, null, controlId), "Control");
        return Query(connection, null, """
            SELECT Id,ControlId,PerformedOn,ControlVersion,Owner,Reviewer,Outcome,Notes,EvidenceId,EvidenceHash,RecordedUtc
            FROM ControlReviews WHERE ControlId=$p0 ORDER BY Sequence DESC
            """, reader => new ControlReview(reader.GetString(0), reader.GetString(1), ReviewDate(reader.GetString(2)),
                reader.GetInt32(3), reader.GetString(4), reader.GetString(5), reader.GetString(6), reader.GetString(7),
                reader.IsDBNull(8) ? null : reader.GetString(8), reader.GetString(9), reader.GetString(10)), controlId);
    });

    public Task<ControlReviewSchedule> ConfigureControlReviewAsync(string controlId, int expectedControlVersion,
        int expectedScheduleVersion, string owner, int intervalMonths, DateOnly nextReviewOn, string reason)
    {
        owner = owner.Trim();
        Reason(reason);
        if (owner.Length is < 1 or > 240 || owner.Contains('\0') || intervalMonths is < 1 or > 60 ||
            nextReviewOn.Year is < 1900 or > 9900)
            throw new ArgumentException("Verantwortung, Prüftermin und einen Prüfturnus von 1 bis 60 Monaten angeben.");

        return Change("ControlReviewConfigured", controlId, JsonSerializer.Serialize(new { owner, intervalMonths, nextReviewOn, reason }),
            (connection, transaction) =>
            {
                var control = RequireControlVersion(connection, transaction, controlId, expectedControlVersion);
                var schedule = ReadReviewSchedule(connection, transaction, controlId);
                if ((schedule?.Version ?? 0) != expectedScheduleVersion)
                    throw new ArgumentException("Der Prüfplan wurde geändert. Bitte neu laden.");
                if (control.Owner != owner)
                    Exec(connection, transaction, "UPDATE Records SET Owner=$p0,Version=Version+1 WHERE Id=$p1", owner, controlId);
                var next = new ControlReviewSchedule(intervalMonths, nextReviewOn, expectedScheduleVersion + 1);
                Exec(connection, transaction, """
                    INSERT INTO ControlReviewSchedules(ControlId,IntervalMonths,NextReviewOn,Version) VALUES($p0,$p1,$p2,$p3)
                    ON CONFLICT(ControlId) DO UPDATE SET IntervalMonths=excluded.IntervalMonths,NextReviewOn=excluded.NextReviewOn,Version=excluded.Version
                    """, controlId, next.IntervalMonths, ReviewDate(next.NextReviewOn), next.Version);
                return next;
            });
    }

    public Task<ControlReviewSchedule> CompleteControlReviewAsync(string controlId, int expectedControlVersion,
        int expectedScheduleVersion, DateOnly performedOn, string outcome, string notes, string? evidenceId)
    {
        notes = notes.Trim();
        Reason(notes);
        var now = DateTimeOffset.Now;
        var today = DateOnly.FromDateTime(now.DateTime);
        if (performedOn.Year < 1900 || performedOn > today || !ControlReviewOutcomes.Contains(outcome, StringComparer.Ordinal))
            throw new ArgumentException("Prüfdatum und Prüfergebnis prüfen. Die Prüfung darf nicht in der Zukunft liegen.");
        if (outcome == "Wirksam" && string.IsNullOrWhiteSpace(evidenceId))
            throw new ArgumentException("Für „Wirksam“ ist ein gültiger Nachweis erforderlich.");

        return Change("ControlReviewRecorded", controlId, JsonSerializer.Serialize(new { performedOn, outcome, notes, evidenceId }),
            (connection, transaction) =>
            {
                var control = RequireControlVersion(connection, transaction, controlId, expectedControlVersion);
                var schedule = ReadReviewSchedule(connection, transaction, controlId);
                if (schedule is null || schedule.Version != expectedScheduleVersion || string.IsNullOrWhiteSpace(control.Owner))
                    throw new ArgumentException("Prüfplan und Verantwortung prüfen. Wurde der Plan geändert, bitte neu laden.");
                var last = Scalar(connection, transaction, """
                    SELECT PerformedOn FROM ControlReviews WHERE ControlId=$p0 ORDER BY Sequence DESC LIMIT 1
                    """, controlId);
                if (last.Length > 0 && performedOn < ReviewDate(last))
                    throw new ArgumentException("Das Prüfdatum liegt vor der letzten gespeicherten Prüfung.");
                var hash = "";
                if (evidenceId is not null)
                {
                    var at = performedOn == today ? now : new DateTimeOffset(performedOn.ToDateTime(TimeOnly.MaxValue),
                        TimeZoneInfo.Local.GetUtcOffset(performedOn.ToDateTime(TimeOnly.MaxValue)));
                    RequireProof(connection, transaction, evidenceId, controlId, at);
                    hash = Scalar(connection, transaction, "SELECT Hash FROM Evidence WHERE Id=$p0", evidenceId);
                }
                Exec(connection, transaction, """
                    INSERT INTO ControlReviews(Id,ControlId,PerformedOn,ControlVersion,Owner,Reviewer,Outcome,Notes,
                        EvidenceId,EvidenceHash,RecordedUtc) VALUES($p0,$p1,$p2,$p3,$p4,$p5,$p6,$p7,$p8,$p9,$p10)
                    """, Guid.NewGuid().ToString("N"), controlId, ReviewDate(performedOn), control.Version,
                    control.Owner, identity.User!, outcome, notes, evidenceId, hash, now.UtcDateTime.ToString("O", CultureInfo.InvariantCulture));
                var next = schedule with { NextReviewOn = performedOn.AddMonths(schedule.IntervalMonths), Version = schedule.Version + 1 };
                Exec(connection, transaction, "UPDATE ControlReviewSchedules SET NextReviewOn=$p0,Version=$p1 WHERE ControlId=$p2",
                    ReviewDate(next.NextReviewOn), next.Version, controlId);
                return next;
            });
    }

    private static Entry RequireControlVersion(SqliteConnection connection, SqliteTransaction transaction, string id, int version)
    {
        var control = Get(connection, transaction, id);
        RequireKind(control, "Control");
        if (control.Version != version)
            throw new ArgumentException("Das Control wurde geändert. Bitte den aktuellen Stand laden.");
        return control;
    }

    private static ControlReviewSchedule? ReadReviewSchedule(SqliteConnection connection, SqliteTransaction transaction, string id) =>
        Query(connection, transaction, """
            SELECT IntervalMonths,NextReviewOn,Version FROM ControlReviewSchedules WHERE ControlId=$p0
            """, reader => new ControlReviewSchedule(reader.GetInt32(0), ReviewDate(reader.GetString(1)), reader.GetInt32(2)), id)
            .SingleOrDefault();
}
