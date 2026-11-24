using D3.Grc.Core;
using Xunit;

namespace D3.Grc.Tests;

public sealed class Nis2PackageTests : IDisposable
{
    private const string Password = "Synthetic NIS2 package workspace password";
    private const string Scope = "Kundenservice";
    private readonly string _directory = Directory.CreateTempSubdirectory("d3-nis2-").FullName;

    [Fact]
    public void Catalog_has_all_ten_article_21_groups_and_separate_reporting_measures()
    {
        Assert.Equal("abcdefghij".Select(letter => "21" + letter).Append("23"), Nis2Catalog.Packages.Select(package => package.Key));
        Assert.Equal(11, Nis2Catalog.Packages.Select(package => package.Reference).Distinct().Count());
        Assert.All(Nis2Catalog.Packages, package =>
        {
            Assert.NotEmpty(package.Requirement);
            Assert.True(package.Controls.Count >= 2);
            Assert.Equal(package.Controls.Count, package.Controls.Select(control => control.Key).Distinct().Count());
            Assert.All(package.Controls, control =>
            {
                Assert.NotEmpty(control.Title);
                Assert.NotEmpty(control.OwnerRole);
                Assert.NotEmpty(control.Implementation);
                Assert.NotEmpty(control.Evidence);
                Assert.NotEmpty(control.RiskScenario);
                Assert.InRange(control.ReviewMonths, 1, 24);
                Assert.NotEmpty(control.ReviewTriggers);
                Assert.NotEmpty(control.Measures);
                Assert.Equal(control.Measures.Count, control.Measures.Select(measure => measure.Key).Distinct().Count());
                Assert.All(control.Measures, measure =>
                {
                    Assert.NotEmpty(measure.Instructions);
                    Assert.NotEmpty(measure.Acceptance);
                });
            });
        });
        var reporting = Nis2Catalog.Get("23");
        Assert.StartsWith("Art. 23", reporting.Reference);
        var rules = reporting.Controls.Single(control => control.Key == "REPORT").Implementation;
        Assert.Contains("24 Stunden", rules);
        Assert.Contains("72 Stunden", rules);
        Assert.Contains("Vertrauensdiensten", rules);
        Assert.Contains("einen Monat nach der Vorfallmeldung", rules);
    }

    [Fact]
    public async Task Package_creation_is_repeatable_and_retains_edits_and_draft_links()
    {
        using var workspace = await CreateWorkspaceAsync();
        var requirementOnly = await workspace.Nis2ApplyRequirementAsync("21a", Scope);
        Assert.Equal(1, requirementOnly.RequirementsCreated);
        Assert.Empty(requirementOnly.State.ControlIds);

        var applied = await workspace.Nis2ApplyPackageAsync("21a", Scope, true, controlKeys: ["RISK"]);
        Assert.Equal(requirementOnly.State.RequirementId, applied.State.RequirementId);
        Assert.Equal(1, applied.ControlsCreated);
        Assert.Equal(2, applied.MeasuresCreated);
        var id = Assert.Single(applied.State.ControlIds).Value;
        var control = Assert.Single(await workspace.EntriesAsync(Nis2Catalog.Topic, "Control", Scope));
        Assert.Equal(id, control.Id);
        Assert.Empty(control.DueDate);
        Assert.Empty(control.Owner);
        control.Title = "Eigene Umsetzung der Risikobewertung";
        await workspace.SaveEntryAsync(control, Nis2Catalog.Topic, "Intern benannter Control");
        var same = await workspace.Nis2ApplyPackageAsync("21a", Scope, true, controlKeys: ["RISK"]);
        Assert.Equal(0, same.RequirementsCreated);
        Assert.Equal(0, same.ControlsCreated);
        Assert.Equal(0, same.MeasuresCreated);
        Assert.Equal(id, same.State.ControlIds["RISK"]);
        Assert.Equal(2, same.State.MeasureIds.Count);
        Assert.Equal("Entwurf", Assert.Single(same.State.LinkedControls).Approval);
        Assert.Equal(control.Title, Assert.Single(await workspace.EntriesAsync(Nis2Catalog.Topic, "Control", Scope)).Title);

        var mappings = await workspace.MappingsAsync();
        Assert.Single(mappings, mapping => mapping.From == same.State.RequirementId && mapping.To == id);
        Assert.Equal(2, mappings.Count(mapping => mapping.Type == "Maßnahme → Control" && mapping.To == id));
        Assert.Null((await workspace.Nis2PackageStateAsync("21a", "Personal")).RequirementId);
        Assert.Null((await workspace.Nis2PackageStateAsync("23", Scope)).RequirementId);
        Assert.True(await workspace.VerifyAsync());
    }

    [Fact]
    public async Task A_linked_iso_control_is_reused_with_its_existing_link_and_approval()
    {
        using var workspace = await CreateWorkspaceAsync();
        var requirement = (await workspace.Nis2ApplyRequirementAsync("21a", Scope)).State.RequirementId!;
        var control = new Entry { Kind = "Control", Title = "Interne Risikoanalyse", SourceId = "ISO27001", Scope = Scope };
        await workspace.SaveEntryAsync(control, "ISMS", "Vorhandener interner Control");
        await workspace.LinkAsync(requirement, control.Id, "Anforderung → Control", Nis2Catalog.Source, "Teilweise", Scope,
            "Bestehender Control behandelt die Risikoanalyse.");
        await Assert.ThrowsAsync<ArgumentException>(() => workspace.Nis2ApplyPackageAsync("21a", Scope, false));

        var applied = await workspace.Nis2ApplyPackageAsync("21a", Scope, true,
            new Dictionary<string, string> { ["RISK"] = control.Id }, controlKeys: ["RISK"]);
        Assert.Equal(0, applied.ControlsCreated);
        Assert.Equal(1, applied.ControlsReused);
        Assert.Equal(control.Id, applied.State.ControlIds["RISK"]);
        var mapping = Assert.Single(applied.State.LinkedControls);
        Assert.Equal("Bestätigt", mapping.Approval);
        Assert.Equal("ISO27001", mapping.SourceId);
        Assert.Empty(await workspace.EntriesAsync(Nis2Catalog.Topic, "Control", Scope));
    }

    [Fact]
    public async Task Foreign_scope_reuse_rolls_back_the_entire_package()
    {
        using var workspace = await CreateWorkspaceAsync();
        var wrongScope = new Entry { Kind = "Control", Title = "Anderer Bereich", Scope = "Personal" };
        await workspace.SaveEntryAsync(wrongScope, "ISMS", "Bestehender Control im anderen Bereich");
        await Assert.ThrowsAsync<ArgumentException>(() => workspace.Nis2ApplyPackageAsync("21a", Scope, true,
            new Dictionary<string, string> { ["POLICY"] = wrongScope.Id }));
        var state = await workspace.Nis2PackageStateAsync("21a", Scope);
        Assert.Null(state.RequirementId);
        Assert.Empty(state.ControlIds);
        Assert.Empty(state.MeasureIds);
        Assert.Empty(await workspace.EntriesAsync(Nis2Catalog.Topic, scope: Scope));
    }

    private async Task<Workspace> CreateWorkspaceAsync()
    {
        var identity = new LocalIdentity(Path.Combine(_directory, "identity"));
        await identity.RegisterAsync("tester", Password);
        Assert.True(await identity.LoginAsync("tester", Password));
        var workspace = new Workspace(identity);
        try
        {
            await workspace.CreateAsync(Path.Combine(_directory, "nis2.d3grc"), "Test NIS2", Password);
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
