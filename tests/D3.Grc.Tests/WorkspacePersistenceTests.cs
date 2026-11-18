using D3.Grc.Core;
using Xunit;

namespace D3.Grc.Tests;

public sealed class WorkspacePersistenceTests : IDisposable
{
    private const string Password = "Test project passphrase";
    private readonly string _root = Path.Combine(Path.GetTempPath(), "d3-persistence-" + Guid.NewGuid().ToString("N"));

    [Theory]
    [InlineData(".bak")]
    [InlineData(".recovery")]
    public async Task Retrying_project_write_preserves_a_committed_change_exactly_once(string blockedPathSuffix)
    {
        using var workspace = await CreateProjectAsync();
        var path = workspace.ProjectPath!;
        var entry = new Entry { Kind = "Risk", Title = "Schreibfehler-Test", SourceId = "GDPR" };
        var blockingDirectory = Directory.CreateDirectory(path + blockedPathSuffix);

        await Assert.ThrowsAsync<WorkspacePersistenceException>(
            () => workspace.SaveEntryAsync(entry, "PRIVACY", "Teständerung"));
        Assert.True(workspace.IsDirty);
        Assert.Single(await workspace.EntriesAsync("PRIVACY", "Risk"), record => record.Id == entry.Id);

        if (blockedPathSuffix == ".bak")
            blockingDirectory.Delete();
        await workspace.SaveAsync();
        if (blockedPathSuffix == ".recovery")
            blockingDirectory.Delete();
        Assert.False(workspace.IsDirty);
        await workspace.CloseAsync();
        await workspace.OpenAsync(path, Password);
        Assert.Single(await workspace.EntriesAsync("PRIVACY", "Risk"), record => record.Title == entry.Title);
    }

    [Fact]
    public async Task Opening_main_file_preserves_newer_recovery_through_later_saves()
    {
        using var workspace = await CreateProjectAsync();
        var path = workspace.ProjectPath!;
        var entry = new Entry { Kind = "Risk", Title = "Nur im Wiederherstellungsstand", SourceId = "GDPR" };
        var blockingDirectory = Directory.CreateDirectory(path + ".bak");

        await Assert.ThrowsAsync<WorkspacePersistenceException>(
            () => workspace.SaveEntryAsync(entry, "PRIVACY", "Teständerung"));
        Assert.True(File.Exists(path + ".recovery"));
        blockingDirectory.Delete();
        await workspace.CloseAsync();

        await workspace.OpenAsync(path, Password, recovery: false);
        var preserved = Assert.IsType<string>(workspace.PreservedRecoveryPath);
        Assert.EndsWith(".d3grc", preserved);
        Assert.DoesNotContain(await workspace.EntriesAsync(), record => record.Id == entry.Id);
        await workspace.AutosaveAsync();
        await workspace.SaveAsync();
        await workspace.CloseAsync();

        await workspace.OpenAsync(preserved, Password);
        Assert.Contains(await workspace.EntriesAsync(), record => record.Id == entry.Id);
    }

    private async Task<Workspace> CreateProjectAsync()
    {
        Directory.CreateDirectory(_root);
        var identity = new LocalIdentity(Path.Combine(_root, "identity"));
        await identity.RegisterAsync("testprofil", Password);
        Assert.True(await identity.LoginAsync("testprofil", Password));
        var workspace = new Workspace(identity);
        try
        {
            await workspace.CreateAsync(Path.Combine(_root, "test.d3grc"), "Testorganisation", Password);
            return workspace;
        }
        catch
        {
            workspace.Dispose();
            throw;
        }
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
            Directory.Delete(_root, recursive: true);
    }
}
