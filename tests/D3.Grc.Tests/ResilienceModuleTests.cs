using D3.Grc.Core;
using Xunit;

namespace D3.Grc.Tests;

public sealed class ResilienceModuleTests
{
    [Theory]
    [InlineData("1,5")]
    [InlineData("1.5")]
    [InlineData("-1")]
    [InlineData("10000000")]
    public void Time_fields_reject_values_outside_the_whole_minute_range(string value)
    {
        var draft = new Entry { Kind = ResilienceModule.Service };
        var fields = new Dictionary<string, string> { ["tolerance_minutes"] = value };
        Assert.Throws<ArgumentException>(() => Validate(draft, fields));
    }

    [Fact]
    public void Bia_keeps_the_recovery_target_within_the_outage_limit_and_allows_zero_data_loss()
    {
        var entry = new Entry { Kind = ResilienceModule.Bia, Status = "Aktiv", Owner = "IT-Betrieb" };
        var fields = new Dictionary<string, string>
        {
            ["assessed_on"] = "2026-11-10", ["criticality"] = "Kritisch", ["impact_timeline"] = "Dringende Fälle stauen sich.",
            ["mtpd_minutes"] = "60", ["rto_minutes"] = "61", ["rpo_minutes"] = "0"
        };
        Assert.Throws<ArgumentException>(() => Validate(entry, fields));
        fields["rto_minutes"] = "60";
        Validate(entry, fields);
    }

    [Fact]
    public void Completed_test_retains_comparable_times_and_a_result_consistent_with_the_tolerance()
    {
        var entry = new Entry { Kind = ResilienceModule.Scenario, Status = "Abgeschlossen", Owner = "IT-Betrieb" };
        var fields = new Dictionary<string, string>
        {
            ["scenario"] = "Synthetischer Portalausfall", ["objectives"] = "Bis 60 Minuten umschalten.",
            ["performed_on"] = "2026-11-10", ["tolerance_snapshot_minutes"] = "60", ["disruption_minutes"] = "72",
            ["result"] = "Ziele erreicht", ["findings"] = "Die Übergabe braucht einen zusätzlichen Freigabeschritt."
        };
        Assert.Throws<ArgumentException>(() => Validate(entry, fields));
        fields["result"] = "Ziele verfehlt";
        Validate(entry, fields);
        fields.Remove("disruption_minutes");
        Assert.Throws<ArgumentException>(() => Validate(entry, fields));
    }

    [Fact]
    public void Demo_covers_supported_registers_and_only_creates_typed_relations()
    {
        var rows = ResilienceModule.DemoRows;
        foreach (var spec in ResilienceModule.Specs)
        {
            var sample = rows.Where(row => row.Entry.Kind == spec.Kind).ToArray();
            Assert.NotEmpty(sample);
            foreach (var row in sample)
                Validate(row.Entry, row.Fields!);
        }
        var kinds = rows.ToDictionary(row => row.Entry.Id, row => row.Entry.Kind);
        kinds["demo-process-service"] = "BusinessProcess";
        kinds["demo-supplier-01"] = "Supplier";
        Assert.All(ResilienceModule.DemoLinks, link => Assert.Contains(ResilienceModule.Relations, relation =>
            relation.Type == link.Type && relation.FromKind == kinds[link.From] && relation.ToKind == kinds[link.To]));
    }

    [Fact]
    public async Task Test_times_and_typed_shared_links_survive_reopening_in_their_exact_scope()
    {
        const string password = "Synthetic resilience test project password";
        const string scope = "Kundenservice";
        var directory = Directory.CreateTempSubdirectory("d3-resilience-");
        var path = Path.Combine(directory.FullName, "resilience.d3grc");
        try
        {
            var identity = new LocalIdentity(Path.Combine(directory.FullName, "identity"));
            await identity.RegisterAsync("tester", password);
            Assert.True(await identity.LoginAsync("tester", password));
            using var workspace = new Workspace(identity);
            await workspace.CreateAsync(path, "Resilienzprüfung", password);
            var service = new Entry { Kind = ResilienceModule.Service, Title = "Testdienst", Scope = scope };
            await workspace.SaveEntryAsync(service, "BCM", "Synthetischer Einrichtungsfall",
                moduleFields: new Dictionary<string, string> { ["tolerance_minutes"] = "60" });
            var scenario = new Entry
            {
                Kind = ResilienceModule.Scenario, Title = "Testausfall", Owner = "IT-Betrieb", Scope = scope, Status = "Abgeschlossen"
            };
            var observations = new Dictionary<string, string>
            {
                ["scenario"] = "Synthetischer Ausfall", ["objectives"] = "Nach 60 Minuten umschalten.",
                ["performed_on"] = "2026-11-10", ["tolerance_snapshot_minutes"] = "60", ["disruption_minutes"] = "72",
                ["result"] = "Ziele verfehlt", ["findings"] = "Die Freigaberolle war im Test verzögert erreichbar."
            };
            await workspace.SaveEntryAsync(scenario, "BCM", "Beobachtete Testzeit dokumentiert", moduleFields: observations);
            await workspace.LinkAsync(scenario.Id, service.Id, "Szenariotest → Geschäftsdienst", "INTERNAL", "Vollständig", scope,
                "Die synthetische Übung prüft den Testdienst.");
            await workspace.CloseAsync();
            await workspace.OpenAsync(path, password);

            var test = Assert.Single(await workspace.ModuleRowsAsync(ResilienceModule.Scenario, scope));
            Assert.Equal("60", test.Fields["tolerance_snapshot_minutes"]);
            Assert.Equal("72", test.Fields["disruption_minutes"]);
            Assert.Empty(await workspace.ModuleRowsAsync(ResilienceModule.Scenario, "Infrastruktur"));
            Assert.Contains(await workspace.MappingsAsync(), link => link.From == test.Entry.Id && link.To == service.Id);
            Assert.True(await workspace.VerifyAsync());
        }
        finally
        {
            directory.Delete(recursive: true);
        }
    }

    private static void Validate(Entry entry, IReadOnlyDictionary<string, string> fields) =>
        ResilienceModule.Specs.Single(spec => spec.Kind == entry.Kind).Validate!(entry, fields);
}
