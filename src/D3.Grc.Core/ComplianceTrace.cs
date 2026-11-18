using System.Globalization;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using static D3.Grc.Core.Database;

namespace D3.Grc.Core;

public enum TraceEvidenceStatus
{
    Current,
    Expired,
    NotYetCurrent,
    InvalidDates
}

public sealed record TraceEvidence(
    string Id, string Title, string FileName, DateTimeOffset? ValidUntil, TraceEvidenceStatus Status);

public sealed record RequirementControlTrace(
    string Code, string Title, string Status, string Coverage, string Scope, string SourceTitle,
    string Reason, IReadOnlyList<TraceEvidence> Evidence);

public sealed record RequirementTrace(
    string Code, string Title, string Status, string SourceTitle, string Locator,
    IReadOnlyList<RequirementControlTrace> Controls);

public sealed partial class Workspace
{
    private const string ConfirmedRequirementControls = """
        SELECT control.Id, control.Code, control.Title, control.Status, assessment.Coverage,
               assessment.Scope, source.Title AS SourceTitle, link.Reason
          FROM Links AS link
          JOIN MappingAssessments AS assessment ON assessment.LinkId = link.Id
          JOIN Records AS control ON control.Id = link.TargetRecordId
          JOIN Sources AS source ON source.Id = link.SourceId
         WHERE link.SourceRecordId = $p0 AND link.Type = 'Anforderung → Control'
           AND control.Kind = 'Control' AND assessment.Approval = 'Bestätigt'
        """;

    public Task<RequirementTrace> RequirementTraceAsync(string requirementId) => Read(connection =>
    {
        var requirement = Get(connection, null, requirementId);
        RequireKind(requirement, "Requirement");
        var sourceTitle = Scalar(connection, null, "SELECT Title FROM Sources WHERE Id = $p0", requirement.SourceId);

        var controls = Query(connection, null, ConfirmedRequirementControls + "\nORDER BY control.Code, control.Title, control.Id",
            reader => (
                Id: reader.GetString(0), Code: reader.GetString(1), Title: reader.GetString(2), Status: reader.GetString(3),
                Coverage: reader.GetString(4), Scope: reader.GetString(5), SourceTitle: reader.GetString(6), Reason: reader.GetString(7)),
            requirementId);

        var evidence = ReadControlEvidence(connection, ConfirmedRequirementControls, requirementId, DateTimeOffset.UtcNow);

        return new RequirementTrace(requirement.Code, requirement.Title, requirement.Status, sourceTitle, requirement.Locator,
            controls.Select(control => new RequirementControlTrace(
                control.Code, control.Title, control.Status, control.Coverage, control.Scope, control.SourceTitle,
                control.Reason, evidence[control.Id].ToArray())).ToArray());
    });

    public Task<ILookup<string, TraceEvidence>> ControlEvidenceTraceAsync(IReadOnlySet<string> controlIds, DateTimeOffset asOf) =>
        Read(connection => ReadControlEvidence(connection, "SELECT value AS Id FROM json_each($p0)",
            JsonSerializer.Serialize(controlIds), asOf));

    private static ILookup<string, TraceEvidence> ReadControlEvidence(
        SqliteConnection connection, string controlSelection, string parameter, DateTimeOffset asOf)
    {
        return Query(connection, null, $"""
            WITH Controls AS ({controlSelection}),
            EvidenceRefs AS (
                SELECT e.ControlId, e.Id AS EvidenceId
                  FROM Evidence AS e JOIN Controls AS c ON c.Id = e.ControlId
                UNION
                SELECT link.ControlId, link.EvidenceId
                  FROM EvidenceLinks AS link JOIN Controls AS c ON c.Id = link.ControlId
            )
            SELECT reference.ControlId, e.Id, e.Title, e.FileName, e.CollectedUtc, e.ValidUntil
              FROM EvidenceRefs AS reference JOIN Evidence AS e ON e.Id = reference.EvidenceId
             ORDER BY e.Title, e.Id
            """,
            reader => (
                ControlId: reader.GetString(0),
                Evidence: DescribeTraceEvidence(reader.GetString(1), reader.GetString(2), reader.GetString(3),
                    reader.GetString(4), reader.GetString(5), asOf)),
            parameter).ToLookup(row => row.ControlId, row => row.Evidence);
    }

    private static TraceEvidence DescribeTraceEvidence(
        string id, string title, string fileName, string collected, string validUntil, DateTimeOffset asOf)
    {
        var hasExpiry = DateTimeOffset.TryParse(validUntil, CultureInfo.InvariantCulture, DateTimeStyles.None, out var expiry);
        var hasCollection = DateTimeOffset.TryParse(collected, CultureInfo.InvariantCulture, DateTimeStyles.None, out var collection);
        var status = (!hasExpiry || !hasCollection || collection >= expiry, expiry <= asOf, collection > asOf) switch
        {
            (true, _, _) => TraceEvidenceStatus.InvalidDates,
            (_, true, _) => TraceEvidenceStatus.Expired,
            (_, _, true) => TraceEvidenceStatus.NotYetCurrent,
            _ => TraceEvidenceStatus.Current
        };
        return new TraceEvidence(id, title, fileName, hasExpiry ? expiry : null, status);
    }
}
