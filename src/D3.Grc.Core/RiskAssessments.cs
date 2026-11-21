using System.Collections.ObjectModel;
using System.Globalization;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using static D3.Grc.Core.Database;

namespace D3.Grc.Core;

public enum RiskResponse { Undecided = 0, Mitigate = 1, Accept = 2, Avoid = 3, Transfer = 4 }

public enum RiskLevel { Low, Medium, High, Critical }

public sealed record RiskAssessmentInput(
    DateOnly AssessedOn,
    int InherentLikelihood,
    int InherentImpact,
    int? ResidualLikelihood,
    int? ResidualImpact,
    RiskResponse Response,
    string Rationale);

public sealed record RiskAssessment(
    string Id,
    string RiskId,
    DateOnly AssessedOn,
    int InherentLikelihood,
    int InherentImpact,
    int? ResidualLikelihood,
    int? ResidualImpact,
    RiskResponse Response,
    string Rationale,
    string Actor,
    int RiskVersion,
    string RecordedUtc)
{
    public int InherentScore => InherentLikelihood * InherentImpact;
    public int? ResidualScore => ResidualLikelihood * ResidualImpact;
    public RiskLevel InherentLevel => RiskAssessmentMethod.LevelForScore(InherentScore);
    public RiskLevel? ResidualLevel => ResidualScore is { } score ? RiskAssessmentMethod.LevelForScore(score) : null;
}

public static class RiskAssessmentMethod
{
    public static IReadOnlyDictionary<RiskResponse, string> ResponseLabels { get; } =
        new ReadOnlyDictionary<RiskResponse, string>(new Dictionary<RiskResponse, string>
        {
            [RiskResponse.Undecided] = "Noch offen",
            [RiskResponse.Mitigate] = "Mindern",
            [RiskResponse.Accept] = "Akzeptieren",
            [RiskResponse.Avoid] = "Vermeiden",
            [RiskResponse.Transfer] = "Übertragen"
        });

    public static IReadOnlyDictionary<RiskLevel, string> LevelLabels { get; } =
        new ReadOnlyDictionary<RiskLevel, string>(new Dictionary<RiskLevel, string>
        {
            [RiskLevel.Low] = "Niedrig",
            [RiskLevel.Medium] = "Mittel",
            [RiskLevel.High] = "Hoch",
            [RiskLevel.Critical] = "Kritisch"
        });

    public static IReadOnlyDictionary<int, string> LikelihoodLabels { get; } =
        new ReadOnlyDictionary<int, string>(new Dictionary<int, string>
        {
            [1] = "Sehr unwahrscheinlich", [2] = "Unwahrscheinlich", [3] = "Möglich",
            [4] = "Wahrscheinlich", [5] = "Sehr wahrscheinlich"
        });

    public static IReadOnlyDictionary<int, string> ImpactLabels { get; } =
        new ReadOnlyDictionary<int, string>(new Dictionary<int, string>
        {
            [1] = "Sehr gering", [2] = "Gering", [3] = "Mittel", [4] = "Hoch", [5] = "Sehr hoch"
        });

    public static RiskLevel LevelForScore(int score) => score switch
    {
        < 1 or > 25 => throw new ArgumentOutOfRangeException(nameof(score), "Risikowert muss zwischen 1 und 25 liegen."),
        <= 4 => RiskLevel.Low,
        <= 9 => RiskLevel.Medium,
        <= 15 => RiskLevel.High,
        _ => RiskLevel.Critical
    };

    internal static RiskAssessmentInput Validate(RiskAssessmentInput assessment)
    {
        ArgumentNullException.ThrowIfNull(assessment);
        var rationale = assessment.Rationale?.Trim() ?? "";
        if (assessment.AssessedOn == DateOnly.MinValue || assessment.AssessedOn > DateOnly.FromDateTime(DateTime.Today))
            throw new ArgumentException("Ein gültiges Bewertungsdatum bis einschließlich heute angeben.", nameof(assessment));
        if (assessment.InherentLikelihood is < 1 or > 5 || assessment.InherentImpact is < 1 or > 5)
            throw new ArgumentException("Eintrittswahrscheinlichkeit und Auswirkung des Ausgangsrisikos jeweils von 1 bis 5 bewerten.", nameof(assessment));
        if (assessment.ResidualLikelihood.HasValue != assessment.ResidualImpact.HasValue ||
            assessment.ResidualLikelihood is < 1 or > 5 || assessment.ResidualImpact is < 1 or > 5)
            throw new ArgumentException("Für das Restrisiko beide Werte von 1 bis 5 angeben oder beide leer lassen.", nameof(assessment));
        if (!Enum.IsDefined(assessment.Response))
            throw new ArgumentException("Eine gültige Risikobehandlung auswählen.", nameof(assessment));
        if (rationale.Length is < 1 or > 30000)
            throw new ArgumentException("Bewertung mit 1 bis 30.000 Zeichen begründen.", nameof(assessment));

        return assessment with { Rationale = rationale };
    }
}

public sealed partial class Workspace
{
    private const string RiskAssessmentDateFormat = "yyyy-MM-dd";
    private const string RiskAssessmentColumns = """
        a.Id, a.RiskId, a.AssessedOn, a.InherentLikelihood, a.InherentImpact,
        a.ResidualLikelihood, a.ResidualImpact, a.Response, a.Rationale, a.Actor, a.RiskVersion, a.RecordedUtc
        """;

    public Task<RiskAssessment> RecordRiskAssessmentAsync(string riskId, RiskAssessmentInput assessment, int? expectedRiskVersion = null)
    {
        assessment = RiskAssessmentMethod.Validate(assessment);
        var id = Guid.NewGuid().ToString("N");
        var assessedOn = assessment.AssessedOn.ToString(RiskAssessmentDateFormat, CultureInfo.InvariantCulture);
        return Change("RiskAssessmentRecorded", riskId, JsonSerializer.Serialize(new { assessmentId = id, assessment }),
            (connection, transaction) =>
            {
                var risk = Get(connection, transaction, riskId);
                RequireKind(risk, "Risk");
                if (expectedRiskVersion is int expected && risk.Version != expected)
                    throw new ArgumentException("Das Risiko wurde geändert. Für die Bewertung den aktuellen Stand laden.");

                var actor = identity.User!;
                var recordedUtc = DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture);
                Exec(connection, transaction, """
                    INSERT INTO RiskAssessments (Id, RiskId, AssessedOn, InherentLikelihood, InherentImpact,
                        ResidualLikelihood, ResidualImpact, Response, Rationale, Actor, RiskVersion, RecordedUtc)
                    VALUES ($p0, $p1, $p2, $p3, $p4, $p5, $p6, $p7, $p8, $p9, $p10, $p11)
                    """, id, riskId, assessedOn, assessment.InherentLikelihood, assessment.InherentImpact,
                    assessment.ResidualLikelihood, assessment.ResidualImpact, (int)assessment.Response, assessment.Rationale,
                    actor, risk.Version, recordedUtc);

                return new RiskAssessment(id, riskId, assessment.AssessedOn, assessment.InherentLikelihood, assessment.InherentImpact,
                    assessment.ResidualLikelihood, assessment.ResidualImpact, assessment.Response, assessment.Rationale, actor,
                    risk.Version, recordedUtc);
            });
    }

    public Task<List<RiskAssessment>> RiskAssessmentsAsync(string riskId) =>
        Read(connection => ReadRiskAssessments(connection, [riskId]));

    public Task<List<RiskAssessment>> RiskAssessmentsAsync(IReadOnlyCollection<string> riskIds)
    {
        ArgumentNullException.ThrowIfNull(riskIds);
        var ids = riskIds.ToArray();
        return Read(connection => ReadRiskAssessments(connection, ids));
    }

    public Task<IReadOnlyDictionary<string, RiskAssessment>> LatestRiskAssessmentsAsync(IReadOnlyCollection<string> riskIds)
    {
        ArgumentNullException.ThrowIfNull(riskIds);
        var ids = riskIds.ToArray();
        return Read(connection => ReadLatestRiskAssessments(connection, ids));
    }

    private static List<RiskAssessment> ReadRiskAssessments(SqliteConnection connection, IEnumerable<string> riskIds) =>
        Query(connection, null, $"""
            SELECT {RiskAssessmentColumns} FROM RiskAssessments a
            WHERE a.RiskId IN (SELECT value FROM json_each($p0))
            ORDER BY a.AssessedOn DESC, a.Sequence DESC
            """, ReadRiskAssessment, JsonSerializer.Serialize(riskIds));

    private static IReadOnlyDictionary<string, RiskAssessment> ReadLatestRiskAssessments(SqliteConnection connection, IEnumerable<string> riskIds) =>
        Query(connection, null, $"""
            WITH Requested AS (SELECT DISTINCT value AS RiskId FROM json_each($p0))
            SELECT {RiskAssessmentColumns} FROM Requested r
            JOIN RiskAssessments a ON a.Sequence = (
                SELECT Sequence FROM RiskAssessments WHERE RiskId=r.RiskId ORDER BY AssessedOn DESC, Sequence DESC LIMIT 1
            )
            ORDER BY a.RiskId
            """, ReadRiskAssessment, JsonSerializer.Serialize(riskIds)).ToDictionary(assessment => assessment.RiskId, StringComparer.Ordinal);

    private static RiskAssessment ReadRiskAssessment(SqliteDataReader reader) => new(
        reader.GetString(0), reader.GetString(1),
        DateOnly.ParseExact(reader.GetString(2), RiskAssessmentDateFormat, CultureInfo.InvariantCulture),
        reader.GetInt32(3), reader.GetInt32(4), reader.IsDBNull(5) ? null : reader.GetInt32(5),
        reader.IsDBNull(6) ? null : reader.GetInt32(6), (RiskResponse)reader.GetInt32(7), reader.GetString(8),
        reader.GetString(9), reader.GetInt32(10), reader.GetString(11));
}
