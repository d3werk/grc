using System.IO.Compression;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using static D3.Grc.Core.Database;

namespace D3.Grc.Core;

public sealed record EvidenceIntegrityReview(Proof Proof, string ComputedSha256, bool FileHashMatches, bool? ImportHashMatches,
    IReadOnlyList<Choice> Controls, int AuditRevisions, int ControlChecks, int FindingClosures);

public sealed record AuditTestSnapshot(long Id, string TestId, string AuditScope, string ControlId, string ControlCode,
    string ControlTitle, int? ControlVersion, string SourceId, string? CriteriaJson, string? EvidenceId,
    string? EvidenceSha256, string Outcome, string Notes, string TestedUtc, bool IsLegacy);

public sealed record AuditEvidenceArchive(byte[] Content, int Revisions, int Files);

public sealed partial class Workspace
{
    private const int AuditArchiveMaximumRevisions = 2000;
    private const long AuditArchiveMetadataLimit = 16 * 1024 * 1024;

    public Task<EvidenceIntegrityReview> CheckEvidenceIntegrityAsync(string id) => Read(connection =>
    {
        var proof = ReadProof(connection, id);
        var actualHash = Convert.ToHexString(SHA256.HashData(ReadProofBytes(connection, id)));
        var recordedHash = Scalar(connection, null, """
            SELECT CASE WHEN json_valid(Detail) THEN json_extract(Detail,'$.sha256') END
            FROM Journal WHERE Action='EvidenceImported' AND EntityId=$p0 ORDER BY Id DESC LIMIT 1
            """, id);
        var controls = Query(connection, null, """
            SELECT control.Id, control.Title FROM Records control WHERE control.Id=$p0 OR EXISTS (
                SELECT 1 FROM EvidenceLinks links WHERE links.EvidenceId=$p1 AND links.ControlId=control.Id
            ) ORDER BY control.Title, control.Id
            """, reader => new Choice(reader.GetString(0), reader.GetString(1)), proof.ControlId, id);
        var revisions = CountProofReferences(connection, "SELECT COUNT(*) FROM AuditTestSnapshots WHERE EvidenceId=$p0", id);
        var controlChecks = CountProofReferences(connection, """
            SELECT (SELECT COUNT(*) FROM IksChecks WHERE EvidenceId=$p0) +
                   (SELECT COUNT(*) FROM ControlReviews WHERE EvidenceId=$p0)
            """, id);
        var findings = CountProofReferences(connection, """
            SELECT COUNT(*) FROM Journal WHERE Action='FindingClosed'
            AND CASE WHEN json_valid(Detail) THEN json_extract(Detail,'$.evidence') END=$p0
            """, id);
        return new EvidenceIntegrityReview(proof, actualHash, HashesEqual(actualHash, proof.Hash),
            recordedHash.Length == 0 ? null : HashesEqual(actualHash, recordedHash) && Verify(connection), controls, revisions, controlChecks, findings);
    });

    public Task<List<AuditTestSnapshot>> AuditTestSnapshotsAsync(string auditId, string controlId, int limit = 100) => Read(connection =>
    {
        RequireKind(Get(connection, null, auditId), "Audit");
        return ReadAuditTestSnapshots(connection, auditId, controlId, Math.Clamp(limit, 1, 200));
    });

    public Task<AuditEvidenceArchive> CreateAuditEvidenceArchiveAsync(string auditId) => Read(connection =>
    {
        var audit = Get(connection, null, auditId);
        RequireKind(audit, "Audit");
        var revisions = ReadAuditTestSnapshots(connection, auditId, null, AuditArchiveMaximumRevisions + 1);
        if (revisions.Count == 0)
            throw new ArgumentException("Für diesen Prüfauftrag ist noch kein Prüfstand vorhanden.");
        if (revisions.Count > AuditArchiveMaximumRevisions || revisions.Sum(SnapshotCharacters) > AuditArchiveMetadataLimit / 6)
            throw new ArgumentException("Der Prüfauftrag überschreitet die Größenbegrenzung für einen Dossierexport.");

        var evidence = revisions.Where(revision => revision.EvidenceId is not null)
            .GroupBy(revision => revision.EvidenceId!, StringComparer.Ordinal).ToArray();
        var metadata = new List<object>();
        using var content = new MemoryStream();
        using (var zip = new ZipArchive(content, ZipArchiveMode.Create, leaveOpen: true))
        {
            foreach (var snapshots in evidence)
            {
                var proof = ReadProof(connection, snapshots.Key);
                var bytes = ReadProofBytes(connection, snapshots.Key);
                var actualHash = Convert.ToHexString(SHA256.HashData(bytes));
                if (!HashesEqual(actualHash, proof.Hash) || snapshots.Any(revision => !HashesEqual(actualHash, revision.EvidenceSha256)))
                    throw new InvalidDataException($"Die SHA-256-Prüfung für „{proof.Title}“ ist fehlgeschlagen. Das Dossier wurde nicht erstellt.");
                var archivePath = $"nachweise/{metadata.Count + 1:D4}.bin";
                using (var output = zip.CreateEntry(archivePath, CompressionLevel.Optimal).Open())
                    output.Write(bytes);
                metadata.Add(new { archivePath, proof, size = bytes.Length });
            }
            using var manifest = zip.CreateEntry("audit-manifest.json", CompressionLevel.Optimal).Open();
            JsonSerializer.Serialize(manifest, new
            {
                formatVersion = 1,
                exportedUtc = DateTimeOffset.UtcNow,
                projectName = Name,
                audit = new { audit.Id, audit.Code, audit.Title, audit.Scope, audit.Status },
                revisions,
                evidence = metadata,
                verification = "SHA-256 der archivierten Bytes entspricht dem Dateihash und jedem exportierten Prüfstand.",
                migratedRecords = "IsLegacy kennzeichnet beim Upgrade übernommene Prüfstände. Frühere Überschreibungen lassen sich daraus nicht rekonstruieren."
            }, new JsonSerializerOptions { WriteIndented = true });
        }
        return new AuditEvidenceArchive(content.ToArray(), revisions.Count, evidence.Length);
    });

    private static long SnapshotCharacters(AuditTestSnapshot revision) => (revision.CriteriaJson?.Length ?? 0) +
        (long)revision.Notes.Length + revision.AuditScope.Length + revision.ControlTitle.Length + revision.ControlCode.Length;

    private static int CountProofReferences(SqliteConnection connection, string sql, string id)
    {
        using var command = Command(connection, null, sql, id);
        return Convert.ToInt32(command.ExecuteScalar());
    }

    private static Proof ReadProof(SqliteConnection connection, string id) => Query(connection, null, """
        SELECT Id,Title,ControlId,SourceId,FileName,Hash,CollectedUtc,ValidUntil,SourceDescription FROM Evidence WHERE Id=$p0
        """, reader => new Proof(reader.GetString(0), reader.GetString(1), reader.GetString(2), reader.GetString(3), reader.GetString(4),
        reader.GetString(5), reader.GetString(6), reader.GetString(7), reader.GetString(8)), id).SingleOrDefault()
        ?? throw new ArgumentException("Nachweis ist in diesem Projekt nicht verfügbar.");

    private static byte[] ReadProofBytes(SqliteConnection connection, string id)
    {
        using var command = Command(connection, null, "SELECT Content FROM Evidence WHERE Id=$p0", id);
        return command.ExecuteScalar() as byte[] ?? throw new ArgumentException("Nachweis ist in diesem Projekt nicht verfügbar.");
    }

    private static bool HashesEqual(string actual, string? recorded) => string.Equals(actual, recorded, StringComparison.OrdinalIgnoreCase);

    private static List<AuditTestSnapshot> ReadAuditTestSnapshots(SqliteConnection connection, string auditId, string? controlId, int limit) =>
        Query(connection, null, """
            SELECT Id,TestId,AuditScope,ControlId,ControlCode,ControlTitle,ControlVersion,SourceId,CriteriaJson,
                   EvidenceId,EvidenceSha256,Outcome,Notes,TestedUtc,IsLegacy
            FROM AuditTestSnapshots WHERE AuditId=$p0 AND ($p1 IS NULL OR ControlId=$p1)
            ORDER BY Id DESC LIMIT $p2
            """, reader => new AuditTestSnapshot(reader.GetInt64(0), reader.GetString(1), reader.GetString(2),
            reader.GetString(3), reader.GetString(4), reader.GetString(5), reader.IsDBNull(6) ? null : reader.GetInt32(6),
            reader.GetString(7), reader.IsDBNull(8) ? null : reader.GetString(8), reader.IsDBNull(9) ? null : reader.GetString(9),
            reader.IsDBNull(10) ? null : reader.GetString(10), reader.GetString(11), reader.GetString(12), reader.GetString(13),
            reader.GetBoolean(14)), auditId, controlId, limit);
}
