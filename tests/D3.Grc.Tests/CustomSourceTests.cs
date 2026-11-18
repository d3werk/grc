using D3.Grc.Core;
using Xunit;

namespace D3.Grc.Tests;

public sealed class CustomSourceTests : IDisposable
{
    private const string Password = "Quellentest mit separatem Passwort";
    private readonly string _root = Directory.CreateTempSubdirectory("d3-custom-sources-").FullName;

    [Fact]
    public async Task Custom_source_survives_reopening_and_appears_in_scoped_exports()
    {
        using var workspace = await OpenWorkspaceAsync();
        var projectPath = workspace.ProjectPath!;
        var input = new CustomSourceInput("  Lieferantenvertrag  ", "", "Fassung 2, Nummer 4",
            "Internes Referenzdokument", ["PRIVACY", "ISMS", "PRIVACY"]);
        var id = await workspace.CreateCustomSourceAsync(input);
        await workspace.SaveSourceNoteAsync(id, "Eigenständige fachliche Bewertung");
        var entry = new Entry { Kind = "Control", Title = "Kontrollfrist", SourceId = id };
        await workspace.SaveEntryAsync(entry, "PRIVACY", "Eigene Quelle ist maßgeblich");

        await workspace.CloseAsync();
        await workspace.OpenAsync(projectPath, Password);
        var source = Assert.Single(await workspace.SourcesAsync("PRIVACY"), s => s.Id == id);
        Assert.True(source.IsCustom);
        Assert.Equal("Lieferantenvertrag", source.Title);
        Assert.Equal("", source.Url);
        Assert.Equal("Eigenständige fachliche Bewertung", source.Note);
        Assert.Contains(await workspace.SourcesAsync("ISMS"), s => s.Id == id);
        Assert.DoesNotContain(await workspace.SourcesAsync("AI"), s => s.Id == id);
        Assert.Contains(await workspace.RecordSourceChoicesAsync(entry.Id), s => s.Id == id);

        var sourcesInReport = (await workspace.ExportSnapshotAsync("PRIVACY", "", "")).Single(s => s.Name == "Quellen");
        Assert.Contains(sourcesInReport.Rows, row => row[0] == id && row[1] == "Lieferantenvertrag" && row[4] == "Manuell gepflegt");
        await Assert.ThrowsAsync<ArgumentException>(() => workspace.UpdateCustomSourceAsync(id, input with { TopicIds = ["ISMS"] }));
        await workspace.UpdateCustomSourceAsync(id, input with { Title = "Lieferantenvertrag 2026", TopicIds = ["PRIVACY", "AI"] });
        Assert.Equal("Lieferantenvertrag 2026", (await workspace.GetCustomSourceAsync(id)).Title);
        Assert.DoesNotContain(await workspace.SourcesAsync("ISMS"), s => s.Id == id);
        Assert.Contains(await workspace.SourcesAsync("AI"), s => s.Id == id);
        await workspace.SaveSourceNoteAsync(id, "");
        Assert.Equal("", (await workspace.SourcesAsync("PRIVACY")).Single(s => s.Id == id).Note);
        Assert.True(await workspace.VerifyAsync());
    }

    [Fact]
    public async Task Custom_source_on_curated_host_is_still_manual_and_invalid_topic_is_rejected()
    {
        using var workspace = await OpenWorkspaceAsync();
        var input = new CustomSourceInput("Beispielquelle", "https://eur-lex.europa.eu/", "", "", ["PRIVACY"]);
        var id = await workspace.CreateCustomSourceAsync(input);
        var repeatedTitleId = await workspace.CreateCustomSourceAsync(input);
        Assert.NotEqual(id, repeatedTitleId);
        var source = (await workspace.SourcesAsync()).Single(s => s.Id == id);
        Assert.True(SourceUpdates.Allowed(new Uri(source.Url)));
        var result = await SourceUpdates.ProbeAsync(source);
        Assert.False(result.Success);
        Assert.Contains("manuell", result.Detail, StringComparison.OrdinalIgnoreCase);
        await Assert.ThrowsAsync<ArgumentException>(() => workspace.StoreSourceResultAsync(result));
        await Assert.ThrowsAsync<ArgumentException>(() => workspace.AcceptSourceAsync(id, "Nicht möglich"));
        await Assert.ThrowsAsync<ArgumentException>(() => workspace.CreateCustomSourceAsync(
            input with { Title = "Rollback-Prüfung", TopicIds = ["PRIVACY", "UNKNOWN"] }));
        Assert.DoesNotContain(await workspace.SourcesAsync(), s => s.Title == "Rollback-Prüfung");
    }

    [Theory]
    [InlineData("http://example.org")]
    [InlineData("file:///tmp/example")]
    [InlineData("https://name:password@example.org")]
    [InlineData("https://")]
    public void Invalid_manual_addresses_are_rejected(string url)
    {
        var input = new CustomSourceInput("Beispielquelle", url, "", "", ["PRIVACY"]);
        Assert.Throws<ArgumentException>(() => input.Validated());
    }

    private async Task<Workspace> OpenWorkspaceAsync()
    {
        var identity = new LocalIdentity(Path.Combine(_root, "identity"));
        await identity.RegisterAsync("Tester", Password);
        Assert.True(await identity.LoginAsync("Tester", Password));
        var workspace = new Workspace(identity);
        try
        {
            await workspace.CreateAsync(Path.Combine(_root, "Quellen.d3grc"), "Quellen-Testprojekt", Password);
            return workspace;
        }
        catch
        {
            workspace.Dispose();
            throw;
        }
    }

    public void Dispose() => Directory.Delete(_root, true);
}
