using System.Security.Cryptography;
using D3.Grc.Core;
using Xunit;

namespace D3.Grc.Tests;

public sealed class ComplianceTraceTests
{
    [Fact]
    public async Task Trace_shows_confirmed_mappings_and_the_current_state_of_direct_and_reused_evidence()
    {
        const string password = "Synthetic trace fixture password";
        const string requirementId = "seed-REQ-001";
        const string originalControlId = "seed-CTRL-001";
        const string legacyLinkId = "legacy-mapping";
        const string linkType = "Anforderung → Control";
        var directory = Directory.CreateTempSubdirectory("d3-trace-test-");
        try
        {
            var catalog = Path.Combine(directory.FullName, "legacy.db");
            File.Copy(Path.Combine(AppContext.BaseDirectory, "content", "catalog-v05.db"), catalog);
            using (var connection = Database.Open(catalog))
            {
                Database.Exec(connection, null, "UPDATE Records SET Locator = 'Art. 5' WHERE Id = $p0", requirementId);
                Database.Exec(connection, null, "INSERT INTO Links VALUES($p0,$p1,$p2,$p3,$p4,$p5)",
                    legacyLinkId, requirementId, originalControlId, linkType, "Legacy Zuordnung", "GDPR");
                byte[] historicalContent = [1, 2, 3];
                Database.Exec(connection, null, """
                    INSERT INTO Evidence (Id, Title, ControlId, SourceId, SourceDescription, FileName, Hash, Content, CollectedUtc, ValidUntil)
                    VALUES ($p0, $p1, $p2, 'GDPR', 'Historischer Testnachweis', 'historisch.txt', $p3, $p4, $p5, $p6)
                    """, "historical-proof", "Historisch", originalControlId,
                    Convert.ToHexString(SHA256.HashData(historicalContent)), historicalContent,
                    DateTimeOffset.UtcNow.AddDays(-2).ToString("O"), DateTimeOffset.UtcNow.AddDays(-1).ToString("O"));
            }

            var identity = new LocalIdentity(Path.Combine(directory.FullName, "identity"));
            await identity.RegisterAsync("trace-tester", password);
            Assert.True(await identity.LoginAsync("trace-tester", password));
            using var workspace = new Workspace(identity);
            await workspace.CreateAsync(Path.Combine(directory.FullName, "trace.d3grc"), "Nachverfolgung", password, catalog);
            var draftTrace = await workspace.RequirementTraceAsync(requirementId);
            Assert.Equal("DSGVO", draftTrace.SourceTitle);
            Assert.Equal("Art. 5", draftTrace.Locator);
            Assert.Empty(draftTrace.Controls);

            var reuseControl = new Entry { Code = "TRACE-REUSE", Title = "Mitverwender", SourceId = "GDPR" };
            var uncoveredControl = new Entry { Code = "TRACE-GAP", Title = "Ohne Nachweis", SourceId = "GDPR" };
            await workspace.SaveEntryAsync(reuseControl, "PRIVACY", "Synthetischer Mitverwender");
            await workspace.SaveEntryAsync(uncoveredControl, "PRIVACY", "Synthetische Lücke");

            await workspace.UnlinkAsync(legacyLinkId);
            foreach (var controlId in new[] { originalControlId, reuseControl.Id, uncoveredControl.Id })
            {
                await workspace.LinkAsync(requirementId, controlId, linkType, "GDPR",
                    "Teilweise", "Testprozess", "Begründete Zuordnung");
            }

            await workspace.ImportEvidenceAsync(originalControlId, "GDPR", "Aktuell", "Synthetischer Nachweis",
                "aktuell.txt", [4, 5, 6], DateTimeOffset.UtcNow.AddDays(1));
            var currentProof = (await workspace.EvidenceAsync(originalControlId)).Single(proof => proof.Title == "Aktuell");
            await workspace.ReuseEvidenceAsync(currentProof.Id, reuseControl.Id);

            var trace = await workspace.RequirementTraceAsync(requirementId);
            var controls = trace.Controls.ToDictionary(control => control.Code);
            Assert.Equal(3, controls.Count);
            Assert.All(controls.Values, control =>
            {
                Assert.Equal("Teilweise", control.Coverage);
                Assert.Equal("Testprozess", control.Scope);
                Assert.Equal("Begründete Zuordnung", control.Reason);
            });
            Assert.Empty(controls[uncoveredControl.Code].Evidence);
            var reused = Assert.Single(controls[reuseControl.Code].Evidence);
            Assert.Equal(currentProof.Id, reused.Id);
            Assert.Equal(TraceEvidenceStatus.Current, reused.Status);
            Assert.Collection(controls["CTRL-001"].Evidence,
                proof => Assert.Equal(reused, proof),
                proof =>
                {
                    Assert.Equal("historical-proof", proof.Id);
                    Assert.Equal(TraceEvidenceStatus.Expired, proof.Status);
                });

            var scope = new HashSet<string> { reuseControl.Id, uncoveredControl.Id };
            var dashboardEvidence = await workspace.ControlEvidenceTraceAsync(scope, DateTimeOffset.UtcNow);
            Assert.Equal(reused, Assert.Single(dashboardEvidence[reuseControl.Id]));
            Assert.Empty(dashboardEvidence[uncoveredControl.Id]);
            Assert.Empty(dashboardEvidence[originalControlId]);
            var expiredEvidence = await workspace.ControlEvidenceTraceAsync(scope, DateTimeOffset.UtcNow.AddDays(2));
            Assert.Equal(TraceEvidenceStatus.Expired, Assert.Single(expiredEvidence[reuseControl.Id]).Status);

            await Assert.ThrowsAsync<ArgumentException>(() => workspace.RequirementTraceAsync(reuseControl.Id));
        }
        finally
        {
            directory.Delete(recursive: true);
        }
    }
}
