using System.Security.Cryptography;
using D3.Grc.Core;
using Xunit;

namespace D3.Grc.Tests;

public sealed class WorkspaceSwitchTests : IDisposable
{
    private const string FirstPassword = "Synthetic project first passphrase";
    private const string SecondPassword = "Synthetic project second passphrase";
    private readonly string _root = Path.Combine(Path.GetTempPath(), "d3-switch-" + Guid.NewGuid().ToString("N"));

    [Fact]
    public async Task Failed_open_keeps_current_project_and_successful_switch_releases_project_lock()
    {
        var identity = await CreateIdentityAsync();
        var firstPath = Path.Combine(_root, "first.d3grc");
        var secondPath = Path.Combine(_root, "second.d3grc");
        using var active = new Workspace(identity);
        using var other = new Workspace(identity);
        await active.CreateAsync(firstPath, "Erster Mandant", FirstPassword);
        await other.CreateAsync(secondPath, "Zweiter Mandant", SecondPassword);
        var risk = new Entry { Kind = "Risk", Title = "Mandantenspezifisches Risiko", SourceId = "GDPR" };
        await active.SaveEntryAsync(risk, "PRIVACY", "Test des Mandantenwechsels");

        await Assert.ThrowsAnyAsync<IOException>(() => active.SwitchAsync(secondPath, SecondPassword));
        await other.CloseAsync();
        await Assert.ThrowsAnyAsync<CryptographicException>(() => active.SwitchAsync(secondPath, FirstPassword));
        Assert.Equal(firstPath, active.ProjectPath);
        Assert.Equal("Erster Mandant", active.Name);
        Assert.Contains(await active.EntriesAsync("PRIVACY", "Risk"), record => record.Id == risk.Id);
        await Assert.ThrowsAnyAsync<IOException>(() => other.OpenAsync(firstPath, FirstPassword));

        Assert.False(await active.SwitchAsync(secondPath, SecondPassword));
        Assert.Equal(secondPath, active.ProjectPath);
        Assert.Equal("Zweiter Mandant", active.Name);
        Assert.DoesNotContain(await active.EntriesAsync(), record => record.Id == risk.Id);
        await other.OpenAsync(firstPath, FirstPassword);
        Assert.Contains(await other.EntriesAsync("PRIVACY", "Risk"), record => record.Id == risk.Id);
        await other.SaveAsync();
        await other.CloseAsync();

        Assert.False(await active.SwitchAsync(firstPath, FirstPassword));
        Assert.Contains(await active.EntriesAsync("PRIVACY", "Risk"), record => record.Id == risk.Id);
    }

    [Fact]
    public async Task Creation_preserves_current_project_when_file_cannot_be_saved()
    {
        var identity = await CreateIdentityAsync();
        var firstPath = Path.Combine(_root, "first.d3grc");
        var secondPath = Path.Combine(_root, "second.d3grc");
        using var active = new Workspace(identity);
        await active.CreateAsync(firstPath, "Erster Mandant", FirstPassword);
        await active.CloseAsync();
        await active.OpenAsync(firstPath, FirstPassword);

        var blockedBackup = Directory.CreateDirectory(firstPath + ".bak");
        await Assert.ThrowsAnyAsync<IOException>(() => active.CreateAndSwitchAsync(
            secondPath, "Neuer Mandant", SecondPassword, CompanyProfile.Default));
        Assert.Equal(firstPath, active.ProjectPath);
        Assert.False(File.Exists(secondPath));
        Assert.True(active.IsDirty);
        blockedBackup.Delete();

        Assert.False(await active.CreateAndSwitchAsync(secondPath, "Neuer Mandant", SecondPassword, CompanyProfile.Default));
        Assert.Equal(secondPath, active.ProjectPath);
        Assert.Equal("Neuer Mandant", active.Name);
        Assert.NotNull(await active.GetCompanyProfileAsync());
        Assert.False(File.Exists(firstPath + ".recovery"));
        await active.SwitchAsync(firstPath, FirstPassword);
        Assert.Equal("Erster Mandant", active.Name);
        Assert.Null(await active.GetCompanyProfileAsync());
    }

    private async Task<LocalIdentity> CreateIdentityAsync()
    {
        var identity = new LocalIdentity(Path.Combine(_root, "identity"));
        await identity.RegisterAsync("Pruefprofil", FirstPassword);
        Assert.True(await identity.LoginAsync("Pruefprofil", FirstPassword));
        return identity;
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
            Directory.Delete(_root, recursive: true);
    }
}
