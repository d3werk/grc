using D3.Grc.Core;
using Xunit;

namespace D3.Grc.Tests;

public sealed class AuditIksTests : IDisposable
{
    private const string Password = "Synthetic audit and IKS test password";
    private readonly string _directory = Directory.CreateTempSubdirectory("d3-audit-iks-").FullName;

    [Fact]
    public async Task Control_review_history_requires_applicable_evidence_and_survives_reopening()
    {
        using var workspace = await CreateWorkspaceAsync();
        var control = new Entry { Kind = "Control", Title = "Approval evidence", Owner = "Operations", Scope = "A" };
        var other = new Entry { Kind = "Control", Title = "Different control", Scope = "B" };
        var action = new Entry { Kind = "Measure", Title = "Recheck approvals", Scope = "A" };
        var wrongAction = new Entry { Kind = "Measure", Title = "Unrelated action", Scope = "B" };
        foreach (var entry in new[] { control, other, action, wrongAction })
            await workspace.SaveEntryAsync(entry, "ISMS", "Synthetic fixture");

        var profile = new IksProfile("All approvals have a record", IksControlType.Preventive, IksControlMode.Hybrid,
            IksFrequency.Quarterly, true);
        await workspace.SaveIksProfileAsync(control.Id, profile);
        await Assert.ThrowsAsync<ArgumentException>(() => workspace.SaveIksProfileAsync(action.Id, profile));
        await workspace.ImportEvidenceAsync(control.Id, "INTERNAL", "Synthetic checklist", "Integration fixture", "check.txt",
            [1, 2, 3], DateTimeOffset.UtcNow.AddDays(10));
        var proof = Assert.Single(await workspace.EvidenceAsync(control.Id));
        var today = DateOnly.FromDateTime(DateTime.Today);
        var input = new IksCheckInput(today, "Internal audit", IksCheckType.IndependentTest, IksCheckOutcome.Confirmed,
            "Compare selected approvals.", "Approvals were recorded.", proof.Id, action.Id);

        await Assert.ThrowsAsync<ArgumentException>(() => workspace.RecordIksCheckAsync(other.Id, input with { MeasureId = null }));
        await Assert.ThrowsAsync<ArgumentException>(() => workspace.RecordIksCheckAsync(control.Id, input with { EvidenceId = null }));
        await Assert.ThrowsAsync<ArgumentException>(() => workspace.RecordIksCheckAsync(control.Id, input with { CheckedOn = today.AddDays(-1) }));
        await Assert.ThrowsAsync<ArgumentException>(() => workspace.RecordIksCheckAsync(control.Id, input with { Reviewer = "operations" }));
        await Assert.ThrowsAsync<ArgumentException>(() => workspace.RecordIksCheckAsync(control.Id, input with { MeasureId = wrongAction.Id }));
        Assert.Empty(await workspace.IksChecksAsync(control.Id));
        await workspace.RecordIksCheckAsync(control.Id, input);
        var self = input with { Type = IksCheckType.DesignAssessment, EvidenceId = null, MeasureId = null, Reviewer = "Operations" };
        await workspace.RecordIksCheckAsync(control.Id, self);
        await Assert.ThrowsAsync<InvalidOperationException>(() => workspace.DeleteAsync(control.Id, "Retain audited control"));

        var path = workspace.ProjectPath!;
        await workspace.CloseAsync();
        await workspace.OpenAsync(path, Password);
        Assert.Equal(profile, (await workspace.IksProfilesAsync([control.Id]))[control.Id]);
        Assert.Equal(new[] { self, input }, (await workspace.IksChecksAsync(control.Id)).Select(check => check.Result));
        Assert.True(await workspace.VerifyAsync());
    }

    [Fact]
    public async Task Audit_coverage_reads_scope_and_linked_follow_up_from_existing_workflow()
    {
        using var workspace = await CreateWorkspaceAsync();
        var audit = new Entry { Kind = "Audit", Title = "Control audit", Status = "In Prüfung" };
        var control = new Entry { Kind = "Control", Title = "Approval", Owner = "Operations" };
        var finding = new Entry { Kind = "Finding", Title = "Evidence ineffective", Status = "Offen" };
        foreach (var entry in new[] { audit, control, finding })
            await workspace.SaveEntryAsync(entry, "ISMS", "Synthetic fixture");

        Task Link(Entry from, Entry to, string kind) => workspace.LinkAsync(from.Id, to.Id, kind, "INTERNAL", "Teilweise", "", "Test link");
        await Link(audit, control, "Audit → Control");
        Assert.Null(Assert.Single(await workspace.AuditCoverageAsync([audit.Id])).Outcome);
        await workspace.TestAsync(audit.Id, control.Id, "INTERNAL", null, "Abweichung", "No evidence was available.");
        var incomplete = Assert.Single(await workspace.AuditCoverageAsync([audit.Id]));
        Assert.Equal("Abweichung", incomplete.Outcome);
        Assert.Equal(incomplete.ControlVersion, incomplete.TestedVersion);
        await Link(finding, audit, "Finding → Audit");
        Assert.False(Assert.Single(await workspace.AuditCoverageAsync([audit.Id])).HasLinkedFinding);
        await Assert.ThrowsAsync<ArgumentException>(() => workspace.CompleteAuditAsync(audit.Id, "Partial finding link"));
        await Link(finding, control, "Finding → Control");

        Assert.True(Assert.Single(await workspace.AuditCoverageAsync([audit.Id])).HasLinkedFinding);
        await workspace.CompleteAuditAsync(audit.Id, "Scope is complete and follow-up is assigned.");
        Assert.Equal("Abgeschlossen", Assert.Single(await workspace.EntriesAsync(kind: "Audit")).Status);
    }

    private async Task<Workspace> CreateWorkspaceAsync()
    {
        var identity = new LocalIdentity(Path.Combine(_directory, "identity"));
        await identity.RegisterAsync("audit-fixture", Password);
        Assert.True(await identity.LoginAsync("audit-fixture", Password));
        var workspace = new Workspace(identity);
        try
        {
            await workspace.CreateAsync(Path.Combine(_directory, "test.d3grc"), "Audit and IKS", Password);
            return workspace;
        }
        catch
        {
            workspace.Dispose();
            throw;
        }
    }

    public void Dispose() => Directory.Delete(_directory, recursive: true);
}
