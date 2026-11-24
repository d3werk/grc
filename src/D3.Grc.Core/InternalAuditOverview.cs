using System.Text.Json;
using static D3.Grc.Core.Database;

namespace D3.Grc.Core;

public sealed record AuditCoverage(string AuditId, string ControlId, string? Outcome, int? TestedVersion, int ControlVersion,
    bool HasLinkedFinding);

public sealed partial class Workspace
{
    public Task<List<AuditCoverage>> AuditCoverageAsync(IReadOnlyCollection<string> auditIds) => Read(connection => Query(connection, null, """
        SELECT scope.SourceRecordId, scope.TargetRecordId, test.Outcome, criterion.ControlVersion, control.Version,
               EXISTS(SELECT 1 FROM Links findingAudit JOIN Links findingControl
                      ON findingControl.SourceRecordId=findingAudit.SourceRecordId
                      WHERE findingAudit.Type='Finding → Audit' AND findingAudit.TargetRecordId=scope.SourceRecordId
                      AND findingControl.Type='Finding → Control' AND findingControl.TargetRecordId=scope.TargetRecordId)
        FROM Links scope
        JOIN Records control ON control.Id=scope.TargetRecordId
        LEFT JOIN AuditTests test ON test.AuditId=scope.SourceRecordId AND test.ControlId=scope.TargetRecordId
        LEFT JOIN AuditCriteria criterion ON criterion.AuditId=scope.SourceRecordId AND criterion.ControlId=scope.TargetRecordId
        WHERE scope.Type='Audit → Control' AND scope.SourceRecordId IN (SELECT value FROM json_each($p0))
        ORDER BY control.Code, control.Title, control.Id
        """, reader => new AuditCoverage(reader.GetString(0), reader.GetString(1), reader.IsDBNull(2) ? null : reader.GetString(2),
            reader.IsDBNull(3) ? null : reader.GetInt32(3), reader.GetInt32(4), reader.GetBoolean(5)), JsonSerializer.Serialize(auditIds)));
}
