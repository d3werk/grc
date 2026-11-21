using D3.Grc.Core;
using Xunit;

namespace D3.Grc.Tests;

public sealed class RiskAssessmentTests : IDisposable
{
    private const string Password = "Synthetic risk assessment password";
    private readonly string _directory = Directory.CreateTempSubdirectory("d3-risk-assessments-").FullName;

    [Theory]
    [InlineData(1, RiskLevel.Low)]
    [InlineData(4, RiskLevel.Low)]
    [InlineData(5, RiskLevel.Medium)]
    [InlineData(9, RiskLevel.Medium)]
    [InlineData(10, RiskLevel.High)]
    [InlineData(15, RiskLevel.High)]
    [InlineData(16, RiskLevel.Critical)]
    [InlineData(25, RiskLevel.Critical)]
    public void Severity_boundaries_are_explicit(int score, RiskLevel expected) =>
        Assert.Equal(expected, RiskAssessmentMethod.LevelForScore(score));

    [Fact]
    public async Task An_unassessed_risk_remains_without_a_score_until_a_valid_assessment_is_recorded()
    {
        using var workspace = await CreateWorkspaceAsync();
        var risk = await CreateRiskAsync(workspace, "Cloud downtime");
        var input = NewAssessment();
        Assert.Empty(await workspace.LatestRiskAssessmentsAsync([risk.Id]));

        await Assert.ThrowsAsync<ArgumentException>(() => workspace.RecordRiskAssessmentAsync(risk.Id, input with { InherentLikelihood = 0 }));
        await Assert.ThrowsAsync<ArgumentException>(() => workspace.RecordRiskAssessmentAsync(risk.Id, input with { InherentImpact = 6 }));
        await Assert.ThrowsAsync<ArgumentException>(() => workspace.RecordRiskAssessmentAsync(risk.Id, input with { AssessedOn = input.AssessedOn.AddDays(1) }));
        await Assert.ThrowsAsync<ArgumentException>(() => workspace.RecordRiskAssessmentAsync(risk.Id, input with { ResidualLikelihood = 3 }));
        await Assert.ThrowsAsync<ArgumentException>(() => workspace.RecordRiskAssessmentAsync(risk.Id, input with { ResidualLikelihood = 3, ResidualImpact = 0 }));
        await Assert.ThrowsAsync<ArgumentException>(() => workspace.RecordRiskAssessmentAsync(risk.Id, input with { Response = (RiskResponse)5 }));
        await Assert.ThrowsAsync<ArgumentException>(() => workspace.RecordRiskAssessmentAsync(risk.Id, input with { Rationale = "\t " }));
        var control = (await workspace.EntriesAsync(kind: "Control"))[0];
        await Assert.ThrowsAsync<ArgumentException>(() => workspace.RecordRiskAssessmentAsync(control.Id, input));
        Assert.Empty(await workspace.RiskAssessmentsAsync([risk.Id, control.Id]));

        var assessed = await workspace.RecordRiskAssessmentAsync(risk.Id, input, risk.Version);
        Assert.Equal(9, assessed.InherentScore);
        Assert.Equal(RiskLevel.Medium, assessed.InherentLevel);
        Assert.Null(assessed.ResidualScore);
        Assert.Null(assessed.ResidualLevel);
        Assert.Equal("TESTER", assessed.Actor);
        Assert.Equal(risk.Version, Assert.Single(await workspace.EntriesAsync(kind: "Risk")).Version);
    }

    [Fact]
    public async Task Latest_batch_reads_deduplicate_ids_and_keep_stable_business_date_history()
    {
        using var workspace = await CreateWorkspaceAsync();
        var risk = await CreateRiskAsync(workspace, "Cloud downtime");
        var other = await CreateRiskAsync(workspace, "Independent risk", "PRIVACY");
        var unassessed = await CreateRiskAsync(workspace, "Undocumented risk");
        var input = NewAssessment();
        var first = await workspace.RecordRiskAssessmentAsync(risk.Id, input, risk.Version);
        risk.Body = "New processing dependencies";
        await workspace.SaveEntryAsync(risk, "ISMS", "Update the risk after architecture review");
        await Assert.ThrowsAsync<ArgumentException>(() => workspace.RecordRiskAssessmentAsync(risk.Id, input, risk.Version));

        var newest = await workspace.RecordRiskAssessmentAsync(risk.Id,
            input with { ResidualLikelihood = 4, ResidualImpact = 5, Rationale = "Additional dependencies increase the residual exposure." }, risk.Version + 1);
        var historical = await workspace.RecordRiskAssessmentAsync(risk.Id,
            input with { AssessedOn = input.AssessedOn.AddDays(-14), Response = RiskResponse.Transfer, Rationale = "Earlier supplier mitigation." });
        await workspace.RecordRiskAssessmentAsync(other.Id, input);
        Assert.Equal(20, newest.ResidualScore);
        Assert.Equal(RiskLevel.Critical, newest.ResidualLevel);
        Assert.Equal(risk.Version + 1, newest.RiskVersion);
        Assert.Equal(new[] { newest.Id, first.Id, historical.Id }, (await workspace.RiskAssessmentsAsync(risk.Id)).Select(row => row.Id));

        var ids = Enumerable.Range(0, 1500).Select(index => $"absent-{index}").Concat([risk.Id, risk.Id, unassessed.Id]).ToArray();
        var latest = await workspace.LatestRiskAssessmentsAsync(ids);
        Assert.Equal(newest, Assert.Single(latest).Value);
        Assert.False(latest.ContainsKey(other.Id));
        Assert.Equal(3, (await workspace.RiskAssessmentsAsync(ids)).Count);
        Assert.Empty(await workspace.LatestRiskAssessmentsAsync([]));
        Assert.True(await workspace.VerifyAsync());
    }

    [Fact]
    public async Task Assessed_risks_preserve_history_and_support_typed_measure_links()
    {
        using var workspace = await CreateWorkspaceAsync();
        var risk = await CreateRiskAsync(workspace, "Cloud downtime");
        var draft = await CreateRiskAsync(workspace, "Unneeded draft");
        await workspace.DeleteAsync(draft.Id, "Duplicate draft");
        await workspace.RecordRiskAssessmentAsync(risk.Id, NewAssessment());
        await Assert.ThrowsAsync<ArgumentException>(() => workspace.DeleteAsync(risk.Id, "Assessment history must remain"));

        var measure = new Entry { Kind = "Measure", Title = "Test switching to backup services" };
        await workspace.SaveEntryAsync(measure, "ISMS", "Reduce downtime risk");
        await Assert.ThrowsAsync<ArgumentException>(() => workspace.LinkAsync(risk.Id, measure.Id, "Maßnahme → Risiko",
            "INTERNAL", "Teilweise", "Cloud", "Invalid direction"));
        await workspace.LinkAsync(measure.Id, risk.Id, "Maßnahme → Risiko", "INTERNAL", "Teilweise", "Cloud", "Practice recovery");
        var link = Assert.Single(await workspace.MappingsAsync(), mapping => mapping.Type == "Maßnahme → Risiko");
        Assert.Equal(measure.Id, link.From);
        Assert.Equal(risk.Id, link.To);
        Assert.Single(await workspace.RiskAssessmentsAsync(risk.Id));
    }

    [Fact]
    public async Task A_failed_project_write_can_retry_without_duplicating_the_assessment()
    {
        using var workspace = await CreateWorkspaceAsync();
        var risk = await CreateRiskAsync(workspace, "Cloud downtime");
        var path = workspace.ProjectPath!;
        var backupDirectory = path + ".bak";
        File.Delete(backupDirectory);
        Directory.CreateDirectory(backupDirectory);

        await Assert.ThrowsAsync<WorkspacePersistenceException>(() => workspace.RecordRiskAssessmentAsync(risk.Id, NewAssessment(), risk.Version));
        var recorded = Assert.Single(await workspace.RiskAssessmentsAsync(risk.Id));
        Directory.Delete(backupDirectory);
        await workspace.SaveAsync();
        await workspace.CloseAsync();
        await workspace.OpenAsync(path, Password);
        Assert.Equal(recorded, Assert.Single(await workspace.RiskAssessmentsAsync(risk.Id)));
        Assert.Single(await workspace.JournalAsync(), line => line.Contains("RiskAssessmentRecorded"));
    }

    [Fact]
    public async Task Migration_from_document_schema_preserves_an_encrypted_backup_without_scoring_existing_risks()
    {
        var previousDb = Path.Combine(_directory, "previous.db");
        File.Copy(Path.Combine(AppContext.BaseDirectory, "content", "catalog-v05.db"), previousDb);
        using (var connection = Database.Open(previousDb))
        {
            foreach (string resource in new[] { "006", "007", "008", "008-templates" })
                Database.Exec(connection, null, Database.Resource(resource));

            Database.Exec(connection, null, """
                INSERT INTO __EFMigrationsHistory VALUES
                    ('20261118210000_TopicsAndSessions', '10.0.14'),
                    ('20261119090000_SupplierRegister', '10.0.14'),
                    ('20261120090000_DocumentRegister', '10.0.14');
                INSERT INTO Records(Id, Kind, Code, Title, SourceId, Body, Owner)
                    VALUES ('prior-risk', 'Risk', '', 'Manual risk from 0.9', 'INTERNAL', '', 'Operations');
                INSERT INTO RecordTopics VALUES ('prior-risk', 'ISMS');
                INSERT INTO Documents (Id, Title, TopicId, Scope, Category, Owner, Content, Status, Version, Revision, UpdatedUtc)
                    VALUES ('prior-policy', 'Prior policy', 'ISMS', '', 0, 'IT', 'Existing text', 0, 1, 1, '2026-11-20T07:30:00Z');
                """);
        }
        var path = Path.Combine(_directory, "previous.d3grc");
        byte[] original;
        using (var cipher = new Cipher())
        {
            await cipher.InitializeAsync(Password);
            original = cipher.Encrypt(await File.ReadAllBytesAsync(previousDb));
        }
        await File.WriteAllBytesAsync(path, original);

        using var workspace = await CreateWorkspaceAsync(createProject: false);
        await workspace.OpenAsync(path, Password);
        var backup = Assert.Single(Directory.EnumerateFiles(_directory, "previous.d3grc.pre010-*.bak"));
        Assert.Equal(original, await File.ReadAllBytesAsync(backup));
        Assert.Equal(original, await File.ReadAllBytesAsync(path));
        Assert.Empty(Directory.EnumerateFiles(_directory, "previous.d3grc.pre09-*.bak"));
        Assert.Equal("Existing text", (await workspace.DocumentAsync("prior-policy")).Draft.Content);
        Assert.Equal("prior-risk", Assert.Single(await workspace.EntriesAsync("ISMS", "Risk")).Id);
        Assert.Empty(await workspace.LatestRiskAssessmentsAsync(["prior-risk"]));

        var assessed = await workspace.RecordRiskAssessmentAsync("prior-risk", NewAssessment());
        await workspace.CloseAsync();
        await workspace.OpenAsync(path, Password);
        Assert.Equal(assessed, Assert.Single(await workspace.RiskAssessmentsAsync("prior-risk")));
        Assert.Single(Directory.EnumerateFiles(_directory, "previous.d3grc.pre010-*.bak"));
    }

    private async Task<Workspace> CreateWorkspaceAsync(bool createProject = true)
    {
        var identity = new LocalIdentity(Path.Combine(_directory, "identity"));
        await identity.RegisterAsync("tester", Password);
        Assert.True(await identity.LoginAsync("tester", Password));
        var workspace = new Workspace(identity);
        try
        {
            if (createProject)
                await workspace.CreateAsync(Path.Combine(_directory, "project.d3grc"), "Risk assessment project", Password);
            return workspace;
        }
        catch
        {
            workspace.Dispose();
            throw;
        }
    }

    private static async Task<Entry> CreateRiskAsync(Workspace workspace, string title, string topic = "ISMS")
    {
        var risk = new Entry { Kind = "Risk", Title = title, Scope = "Service example", Owner = "Operations" };
        await workspace.SaveEntryAsync(risk, topic, "Risk identification");
        return risk;
    }

    private static RiskAssessmentInput NewAssessment() => new(DateOnly.FromDateTime(DateTime.Today), 3, 3, null, null,
        RiskResponse.Mitigate, "A service outage would interrupt business operations.");

    public void Dispose() => Directory.Delete(_directory, recursive: true);
}
