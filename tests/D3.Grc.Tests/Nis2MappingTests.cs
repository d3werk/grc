using D3.Grc.Core;
using Xunit;
using static D3.Grc.Core.Database;

namespace D3.Grc.Tests;

public sealed class Nis2MappingTests : IDisposable
{
    private const string Password = "Synthetic scoped NIS2 test password";
    private const string Scope = "Fiktiver IT-Betrieb";
    private readonly string directory = Directory.CreateTempSubdirectory("d3-nis2-mapping-").FullName;

    [Fact]
    public async Task Suggestions_use_exact_references_editions_and_existing_scoped_control_ids()
    {
        using var workspace = await CreateWorkspaceAsync();
        var requirement = await RecordAsync(workspace, "Requirement", "NIS2", "NIS2", "Art. 21 Abs. 2 Buchst. c");
        var iso = await RecordAsync(workspace, "Control", "ISMS", "ISO27001", "ISO/IEC 27001:2022, Anhang A 8.13");
        var bsi = await RecordAsync(workspace, "Control", "ISMS", "BSIGS", "IT-Grundschutz-Kompendium, Edition 2023, CON.3.A4");
        await workspace.AddSourceAsync(iso.Id, "BSIGS", "Ausgabe 2023, CON.3", "Fiktive zweite Quellenreferenz");
        await RecordAsync(workspace, "Control", "ISMS", "BSIGS", "Edition 2022, CON.3.A4");
        await RecordAsync(workspace, "Control", "ISMS", "BSIGS", "CON.3.A4");
        await RecordAsync(workspace, "Control", "ISMS", "BSIGS", "Edition 2023, CON.30");
        await RecordAsync(workspace, "Control", "ISMS", "ISO27001", "A.8.130");
        await RecordAsync(workspace, "Control", "ISMS", "ISO27001", "ISO 27001:2013, A.8.13");
        await RecordAsync(workspace, "Control", "ISMS", "INTERNAL", "ISO/IEC 27001:2022, A.8.13");
        await RecordAsync(workspace, "Control", "ISMS", "ISO27001", "A.8.13", "Fiktiver Nachbarbereich");

        var suggestions = await workspace.Nis2ControlSuggestionsAsync(requirement.Id, Scope);
        Assert.Equal(new[] { iso.Id, bsi.Id }.Order(), suggestions.Select(row => row.Control.Id).Order());
        Assert.Empty(await workspace.Nis2ControlSuggestionsAsync(requirement.Id, "Fiktiver Nachbarbereich"));
        var isoSuggestion = Assert.Single(suggestions, row => row.Control.Id == iso.Id);
        Assert.Equal(new[] { "A.8.13", "CON.3" }, isoSuggestion.References.Select(reference => reference.Identifier).Order());
        Assert.Equal("Teilweise", isoSuggestion.ProposedCoverage);
        var bsiSuggestion = Assert.Single(suggestions, row => row.Control.Id == bsi.Id);
        Assert.Equal("Unterstützend", bsiSuggestion.ProposedCoverage);
        await Assert.ThrowsAsync<ArgumentException>(() => workspace.ConfirmNis2ControlSuggestionAsync(
            requirement.Id, iso.Id, "Unterstützend", Scope, "Veraltete Fundstellen.", isoSuggestion.Control.Version, bsiSuggestion.References));
        Assert.DoesNotContain(await workspace.MappingsAsync(), row => row.From == requirement.Id);

        var id = await workspace.ConfirmNis2ControlSuggestionAsync(
            requirement.Id, iso.Id, "Unterstützend", Scope, "Eigene, geprüfte Begründung.", isoSuggestion.Control.Version, isoSuggestion.References);
        var mapping = Assert.Single(await workspace.MappingsAsync(), row => row.Id == id);
        Assert.Equal("Bestätigt", mapping.Approval);
        Assert.Equal("Unterstützend", mapping.Coverage);
        Assert.Equal("NIS2", mapping.SourceId);
        Assert.Contains("ISO/IEC 27001:2022, Anhang A · A.8.13", mapping.Reason);
        Assert.DoesNotContain(await workspace.Nis2ControlSuggestionsAsync(requirement.Id), row => row.Control.Id == iso.Id);
        await Assert.ThrowsAsync<ArgumentException>(() => workspace.ConfirmNis2ControlSuggestionAsync(
            requirement.Id, iso.Id, "Teilweise", Scope, "Doppelbestätigung."));
    }

    [Fact]
    public async Task A_confirmed_draft_keeps_its_id_and_manual_confirmations_can_use_an_internal_control()
    {
        var fixture = Path.Combine(directory, "fixture.db");
        File.Copy(Path.Combine(AppContext.BaseDirectory, "content", "catalog-v05.db"), fixture);
        await MigrateAsync(fixture);
        using (var connection = Open(fixture))
        {
            Exec(connection, null, """
                INSERT INTO Records(Id,Kind,Code,Title,SourceId,Locator,Body,Scope) VALUES
                    ('fixture-nis2','Requirement','NIS2-FIXTURE','Fiktive Meldekette','NIS2','Art. 23 Abs. 1–4','',$p0),
                    ('fixture-iso','Control','ISO-FIXTURE','Fiktive Eskalation','ISO27001','A.5.24','',$p0)
                """, Scope);
            Exec(connection, null, """
                INSERT INTO Links(Id,SourceRecordId,TargetRecordId,Type,Reason,SourceId)
                    VALUES('initial-draft','fixture-nis2','fixture-iso','Anforderung → Control','Alter Entwurf','NIS2');
                INSERT INTO MappingAssessments(LinkId,Coverage,Scope,Approval)
                    VALUES('initial-draft','Teilweise',$p0,'Entwurf');
                """, Scope);
        }

        using var workspace = await CreateWorkspaceAsync(fixture);
        var suggestion = Assert.Single(await workspace.Nis2ControlSuggestionsAsync("fixture-nis2", Scope));
        Assert.Equal("Unterstützend", suggestion.ProposedCoverage);
        Assert.Contains("Fristen", suggestion.Rationale);
        Assert.Equal("initial-draft", await workspace.ConfirmNis2ControlSuggestionAsync(
            "fixture-nis2", "fixture-iso", "Unterstützend", Scope, "Fachlich geprüft."));

        var internalControl = await RecordAsync(workspace, "Control", "NIS2", "INTERNAL", "Kundeneigenes Meldeverfahren");
        await Assert.ThrowsAsync<ArgumentException>(() => workspace.ConfirmNis2ControlSuggestionAsync(
            "fixture-nis2", internalControl.Id, "Teilweise", Scope, "Kein genauer Annex-Bezug."));
        await Assert.ThrowsAsync<ArgumentException>(() => workspace.ConfirmNis2ControlAsync(
            "fixture-nis2", internalControl.Id, "Teilweise", "Anderer Bereich", "Geltungsbereich passt nicht."));
        await Assert.ThrowsAsync<ArgumentException>(() => workspace.ConfirmNis2ControlAsync(
            "fixture-nis2", internalControl.Id, "Teilweise", Scope, "Veralteter Stand.", internalControl.Version + 1));
        var manualId = await workspace.ConfirmNis2ControlAsync(
            "fixture-nis2", internalControl.Id, "Teilweise", Scope, "Manuell geprüfte Fristen und Eskalationswege.");

        var project = workspace.ProjectPath!;
        await workspace.CloseAsync();
        await workspace.OpenAsync(project, Password);
        var mapped = (await workspace.MappingsAsync()).Where(row => row.From == "fixture-nis2").ToArray();
        Assert.Equal(2, mapped.Length);
        Assert.All(mapped, row => Assert.Equal("Bestätigt", row.Approval));
        Assert.Contains(mapped, row => row.Id == manualId && row.To == internalControl.Id);
        Assert.True(await workspace.VerifyAsync());
    }

    private async Task<Workspace> CreateWorkspaceAsync(string? fixture = null)
    {
        var identity = new LocalIdentity(Path.Combine(directory, "profile"));
        await identity.RegisterAsync("tester", Password);
        Assert.True(await identity.LoginAsync("tester", Password));
        var workspace = new Workspace(identity);
        try
        {
            await workspace.CreateAsync(Path.Combine(directory, "test.d3grc"), "Fiktives NIS2-Projekt", Password, catalog: fixture);
            return workspace;
        }
        catch
        {
            workspace.Dispose();
            throw;
        }
    }

    private static async Task<Entry> RecordAsync(Workspace workspace, string kind, string topic,
        string source, string reference, string scope = Scope)
    {
        var record = new Entry
        {
            Kind = kind, Title = $"Synthetischer Eintrag {reference}", Scope = scope,
            SourceId = source, Locator = reference
        };
        await workspace.SaveEntryAsync(record, topic, "Fiktiver Testdatensatz.");
        return record;
    }

    public void Dispose() => Directory.Delete(directory, recursive: true);
}
