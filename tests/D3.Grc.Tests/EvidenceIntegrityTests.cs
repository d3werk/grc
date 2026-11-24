using System.IO.Compression;
using System.Security.Cryptography;
using System.Text.Json;
using D3.Grc.Core;
using Microsoft.Data.Sqlite;
using Xunit;

namespace D3.Grc.Tests;

public sealed class EvidenceIntegrityTests : IDisposable
{
    private const string Password = "Synthetic evidence fixture password";
    private readonly string _directory = Directory.CreateTempSubdirectory("d3-evidence-integrity-").FullName;

    [Fact]
    public async Task Audit_export_retains_earlier_evidence_when_a_test_is_revised()
    {
        var identity = new LocalIdentity(Path.Combine(_directory, "identity"));
        await identity.RegisterAsync("archive-fixture", Password);
        Assert.True(await identity.LoginAsync("archive-fixture", Password));
        using var workspace = new Workspace(identity);
        var path = Path.Combine(_directory, "archive.d3grc");
        await workspace.CreateAsync(path, "Audit evidence fixture", Password);
        var audit = new Entry { Kind = "Audit", Title = "Control evidence", Status = "In Prüfung", Scope = "Service" };
        var control = new Entry { Kind = "Control", Title = "Initial scope", Scope = "Service" };
        var outside = new Entry { Kind = "Control", Title = "Unrelated control", Scope = "Outside" };
        foreach (var entry in new[] { audit, control, outside })
            await workspace.SaveEntryAsync(entry, "ISMS", "Synthetic file history");
        await workspace.LinkAsync(audit.Id, control.Id, "Audit → Control", "INTERNAL", "Vollständig", "Service", "Included in audit");

        byte[] data = [6, 5, 4, 3, 2, 1];
        await workspace.ImportEvidenceAsync(control.Id, "INTERNAL", "Recorded evidence", "Synthetic", "test.txt", data, DateTimeOffset.UtcNow.AddDays(1));
        await workspace.ImportEvidenceAsync(outside.Id, "INTERNAL", "Unrelated evidence", "Synthetic", "outside.txt", [0], DateTimeOffset.UtcNow.AddDays(1));
        var proof = Assert.Single(await workspace.EvidenceAsync(control.Id));
        await workspace.TestAsync(audit.Id, control.Id, "INTERNAL", proof.Id, "Bestanden", "First complete review.");

        var current = Assert.Single(await workspace.EntriesAsync(kind: "Control", scope: "Service"));
        current.Title = "Updated control title";
        await workspace.SaveEntryAsync(current, "ISMS", "Updated control design");
        await workspace.TestAsync(audit.Id, control.Id, "INTERNAL", null, "Abweichung", "Second review found a gap.");
        await workspace.CloseAsync();
        await workspace.OpenAsync(path, Password);

        var versions = await workspace.AuditTestSnapshotsAsync(audit.Id, control.Id);
        Assert.Equal(new[] { "Updated control title", "Initial scope" }, versions.Select(snapshot => snapshot.ControlTitle));
        Assert.Equal(new int?[] { 2, 1 }, versions.Select(snapshot => snapshot.ControlVersion));
        Assert.All(versions, snapshot => Assert.False(snapshot.IsLegacy));
        Assert.Equal(proof.Hash, versions[1].EvidenceSha256);
        Assert.Null(versions[0].EvidenceId);
        Assert.Equal("Abweichung", Assert.Single(await workspace.TestsAsync(audit.Id)).Outcome);
        var review = await workspace.CheckEvidenceIntegrityAsync(proof.Id);
        Assert.True(review.FileHashMatches);
        Assert.True(review.ImportHashMatches);
        Assert.Equal(1, review.AuditRevisions);
        Assert.Equal(control.Id, Assert.Single(review.Controls).Id);

        var archive = await workspace.CreateAuditEvidenceArchiveAsync(audit.Id);
        Assert.Equal(2, archive.Revisions);
        Assert.Equal(1, archive.Files);
        using var zip = new ZipArchive(new MemoryStream(archive.Content));
        using var manifest = JsonDocument.Parse(zip.GetEntry("audit-manifest.json")!.Open());
        Assert.Equal(2, manifest.RootElement.GetProperty("revisions").GetArrayLength());
        var evidence = manifest.RootElement.GetProperty("evidence")[0];
        using var archivedFile = zip.GetEntry(evidence.GetProperty("archivePath").GetString()!)!.Open();
        Assert.Equal(proof.Hash, Convert.ToHexString(SHA256.HashData(archivedFile)));
        Assert.DoesNotContain("Unrelated", manifest.RootElement.GetRawText());
    }

    [Fact]
    public void Existing_audit_test_is_migrated_without_adding_a_verification_claim()
    {
        var dbPath = Path.Combine(_directory, "legacy.db");
        File.Copy(Path.Combine(AppContext.BaseDirectory, "content", "catalog-v05.db"), dbPath);
        using var database = Database.Open(dbPath);
        Database.Exec(database, null, Database.Resource("006"));
        Database.Exec(database, null, """
            INSERT INTO Records(Id,Kind,Code,Title,SourceId,Body) VALUES
                ('legacy-audit','Audit','AUD','Legacy audit','INTERNAL',''),
                ('legacy-control','Control','CON','Legacy control','INTERNAL','');
            INSERT INTO Evidence(Id,Title,ControlId,SourceId,SourceDescription,FileName,Hash,Content,CollectedUtc,ValidUntil)
            VALUES ('legacy-proof','Legacy proof','legacy-control','INTERNAL','Fixture','legacy.txt','old stored hash',X'0102','2025-11-01','2027-11-01');
            INSERT INTO AuditTests VALUES ('legacy-test','legacy-audit','legacy-control','INTERNAL','legacy-proof','Bestanden','Old note','2025-11-02');
            """);

        Database.Exec(database, null, Database.Resource("014-evidence-integrity"));
        Assert.Equal("1", Database.Scalar(database, null, "SELECT IsLegacy FROM AuditTestSnapshots WHERE TestId='legacy-test'"));
        Assert.Equal("old stored hash", Database.Scalar(database, null, "SELECT EvidenceSha256 FROM AuditTestSnapshots WHERE TestId='legacy-test'"));
        Assert.Throws<SqliteException>(() => Database.Exec(database, null, "UPDATE Evidence SET Hash='changed' WHERE Id='legacy-proof'"));
        Assert.Throws<SqliteException>(() => Database.Exec(database, null, "DELETE FROM AuditTestSnapshots"));
        Assert.Throws<SqliteException>(() => Database.Exec(database, null, """
            INSERT OR REPLACE INTO Evidence SELECT * FROM Evidence WHERE Id='legacy-proof'
            """));
        Assert.Equal("Old note", Database.Scalar(database, null, "SELECT Notes FROM AuditTestSnapshots WHERE TestId='legacy-test'"));
    }

    public void Dispose() => Directory.Delete(_directory, recursive: true);
}
