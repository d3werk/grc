using D3.Grc.Core;
using Xunit;

namespace D3.Grc.Tests;

public sealed class ProcessModuleTests : IDisposable
{
    private readonly string _folder = Directory.CreateTempSubdirectory("d3-process-").FullName;

    [Fact]
    public void Ordered_process_steps_keep_roles_and_export_as_readable_text()
    {
        ProcessStep[] steps = [new("Anfrage prüfen", "Service Desk", "Pflichtangaben prüfen."), new("Ticket weitergeben", "Fachteam")];
        var serialized = ProcessModule.WriteSteps(steps);
        Assert.Equal(steps, ProcessModule.ReadSteps(serialized));
        Assert.Equal(string.Join(Environment.NewLine, "1. Anfrage prüfen (Service Desk) · Pflichtangaben prüfen.",
            "2. Ticket weitergeben (Fachteam)"), ProcessModule.FormatSteps(serialized));
    }

    [Fact]
    public void Released_process_requires_complete_review_and_valid_steps()
    {
        var validate = Assert.Single(ProcessModule.Specs).Validate!;
        var demo = Assert.Single(ProcessModule.DemoRows, row => row.Entry.Id == "demo-process-service");
        var fields = new Dictionary<string, string>(demo.Fields!);
        validate(demo.Entry, fields);

        fields[ProcessModule.StepKey] = "[{\"title\":\" \"}]";
        Assert.Throws<ArgumentException>(() => validate(demo.Entry, fields));
        fields[ProcessModule.StepKey] = "[null]";
        Assert.Throws<ArgumentException>(() => validate(demo.Entry, fields));
        fields[ProcessModule.StepKey] = "[]";
        Assert.Throws<ArgumentException>(() => validate(demo.Entry, fields));
        fields[ProcessModule.StepKey] = demo.Fields![ProcessModule.StepKey];
        Assert.True(fields.Remove("review_note"));
        Assert.Throws<ArgumentException>(() => validate(demo.Entry, fields));

        demo.Entry.Status = "Entwurf";
        validate(demo.Entry, fields);
    }

    [Fact]
    public async Task Encrypted_workspace_preserves_steps_and_rejects_inconsistent_process_hierarchy()
    {
        const string password = "Prozess-Testpasswort 123";
        var identity = new LocalIdentity(Path.Combine(_folder, "identity"));
        await identity.RegisterAsync("bpm", password);
        Assert.True(await identity.LoginAsync("bpm", password));
        using var workspace = new Workspace(identity);
        var path = Path.Combine(_folder, "process.d3grc");
        await workspace.CreateAsync(path, "Prozess-Test", password);
        var entries = ProcessModule.DemoRows;
        foreach (var seed in entries)
            await workspace.SaveEntryAsync(seed.Entry, seed.Topic, "Fachworkflow prüfen", moduleFields: seed.Fields);

        var root = entries.Single(seed => seed.Entry.Id == "demo-process-service").Entry;
        var child = entries.Single(seed => seed.Entry.Id == "demo-process-intake").Entry;
        var unrelated = entries.Single(seed => seed.Entry.Id == "demo-process-portal").Entry;
        var otherScope = new Entry { Kind = ProcessModule.Kind, Scope = "Lager", Title = "Andere Organisationseinheit" };
        await workspace.SaveEntryAsync(otherScope, "PROCESS", "Anderen Geltungsbereich prüfen");

        Task SetParent(Entry from, Entry to) => workspace.LinkAsync(from.Id, to.Id, ProcessModule.ParentType,
            from.SourceId, "Vollständig", from.Scope, "Hierarchie prüfen");

        await SetParent(child, root);
        await Assert.ThrowsAsync<ArgumentException>(() => SetParent(root, child));
        await Assert.ThrowsAsync<ArgumentException>(() => SetParent(child, unrelated));
        await Assert.ThrowsAsync<ArgumentException>(() => SetParent(unrelated, otherScope));
        root.Scope = "Neuer Geltungsbereich";
        await Assert.ThrowsAsync<ArgumentException>(() => workspace.SaveEntryAsync(root, "PROCESS", "Verschieben prüfen"));

        await workspace.CloseAsync();
        await workspace.OpenAsync(path, password);
        var restored = Assert.Single(await workspace.ModuleRowsAsync(ProcessModule.Kind, child.Scope), row => row.Entry.Id == child.Id);
        var restoredSteps = ProcessModule.ReadSteps(restored.Fields[ProcessModule.StepKey]);
        Assert.Equal("Kategorie und Priorität vergeben", restoredSteps[1].Title);
        var link = Assert.Single(await workspace.MappingsAsync(), mapping => mapping.Type == ProcessModule.ParentType);
        Assert.Equal(child.Id, link.From);
        Assert.Equal(root.Id, link.To);
        Assert.True(await workspace.VerifyAsync());
    }

    public void Dispose() => Directory.Delete(_folder, recursive: true);
}
