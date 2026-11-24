using D3.Grc.Core;
using Xunit;

namespace D3.Grc.Tests;

public sealed class AiModuleTests
{
    [Fact]
    public void Active_ai_requires_an_explained_decision_and_a_review_schedule()
    {
        var spec = Assert.Single(AiModule.Specs);
        var validate = spec.Validate!;
        var active = Assert.Single(AiModule.DemoRows, seed => seed.Entry is { Kind: AiModule.Kind, Status: AiModule.ActiveStatus });
        var fields = active.Fields!;
        validate(active.Entry, fields);

        var removedConditions = new Dictionary<string, string>(fields);
        removedConditions.Remove("decision_conditions");
        var noConditions = Assert.Throws<ArgumentException>(() => validate(active.Entry, removedConditions));
        Assert.Contains("Auflagen", noConditions.Message);

        var noApproval = new Dictionary<string, string>(fields) { ["decision"] = AiModule.PendingDecision };
        Assert.Throws<ArgumentException>(() => validate(active.Entry, noApproval));

        active.Entry.DueDate = fields["decision_on"];
        var invalidDue = Assert.Throws<ArgumentException>(() => validate(active.Entry, fields));
        Assert.Contains("Prüftermin", invalidDue.Message);
    }

    [Fact]
    public void Legacy_ai_register_rows_remain_valid_without_structured_details()
    {
        var spec = Assert.Single(AiModule.Specs);
        foreach (var legacyStatus in new[] { "Entwurf", "Offen", "In Bearbeitung", "Erledigt" })
        {
            Assert.Contains(legacyStatus, spec.Statuses!);
            spec.Validate!(new Entry { Kind = AiModule.Kind, Status = legacyStatus }, new Dictionary<string, string>());
        }
    }

    [Fact]
    public async Task Ai_system_and_linked_risk_survive_encrypted_reopen()
    {
        const string password = "Synthetic AI portfolio test password";
        var directory = Directory.CreateTempSubdirectory("d3-ai-module-");
        try
        {
            var identity = new LocalIdentity(Path.Combine(directory.FullName, "identity"));
            await identity.RegisterAsync("tester", password);
            Assert.True(await identity.LoginAsync("tester", password));
            using var workspace = new Workspace(identity);
            var project = Path.Combine(directory.FullName, "ai.d3grc");
            await workspace.CreateAsync(project, "KI-Beispielprojekt", password);
            var demo = AiModule.DemoRows;
            var system = Assert.Single(demo, seed => seed.Entry.Id == "demo-ai-knowledge");
            var risk = Assert.Single(demo, seed => seed.Entry.Id == "demo-ai-risk");
            await Assert.ThrowsAsync<ArgumentException>(() =>
                workspace.SaveEntryAsync(system.Entry, "AI", "Fehlende KI-Freigabe prüfen", moduleFields: new Dictionary<string, string>()));
            Assert.Empty(await workspace.ModuleRowsAsync(AiModule.Kind));

            await workspace.SaveEntryAsync(system.Entry, system.Topic, "Synthetisches KI-System speichern", moduleFields: system.Fields);
            await workspace.SaveEntryAsync(risk.Entry, risk.Topic, "Synthetisches Risiko speichern");
            await workspace.LinkAsync(system.Entry.Id, risk.Entry.Id, "KI-System → Risiko", "INTERNAL",
                "Teilweise", system.Entry.Scope, "Synthetischer fachlicher Bezug");
            await workspace.CloseAsync();
            await workspace.OpenAsync(project, password);

            var reopened = Assert.Single(await workspace.ModuleRowsAsync(AiModule.Kind, system.Entry.Scope));
            Assert.Equal(system.Entry.Id, reopened.Entry.Id);
            Assert.Equal(AiModule.ApprovedWithConditions, reopened.Fields["decision"]);
            Assert.Equal(system.Entry.DueDate, reopened.Entry.DueDate);
            Assert.Contains(await workspace.MappingsAsync(), mapping => mapping.From == system.Entry.Id && mapping.To == risk.Entry.Id);
            Assert.True(await workspace.VerifyAsync());
        }
        finally
        {
            directory.Delete(recursive: true);
        }
    }
}
