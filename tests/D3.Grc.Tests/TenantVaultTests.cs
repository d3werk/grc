using System.IO.Compression;
using System.Security.Cryptography;
using D3.Grc.Core;
using Xunit;

namespace D3.Grc.Tests;

public sealed class TenantVaultTests : IDisposable
{
    private const string Password = "Synthetic profile test password";
    private readonly string _root = Directory.CreateTempSubdirectory("d3-tenants-").FullName;

    [Fact]
    public async Task Accounts_switch_without_project_password_and_full_backup_restores_settings_and_documents()
    {
        var identity = new LocalIdentity(Path.Combine(_root, "profile"));
        await RegisterAsync(identity, "Betrieb");
        var vault = new TenantVault(identity);
        using var active = new Workspace(identity);
        var profile = CompanyProfile.Default with { Industry = CompanyIndustry.Health, SiteCount = 3 };
        var first = await vault.CreateAsync("Betrieb Nord", profile);
        await vault.OpenAsync(active, first.Id);
        var document = await active.CreateDocumentAsync(
            new("Prüfbogen Nord", "ISMS", "Nord", DocumentCategory.Procedure, "IT", "Synthetisches Dokument"),
            "Testanlage");
        var second = await vault.CreateAsync("Betrieb Süd", CompanyProfile.Default);
        await vault.OpenAsync(active, second.Id);
        Assert.DoesNotContain(await active.DocumentsAsync(), value => value.Id == document.Id);
        await active.LogoutAsync();
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => vault.ListAsync());
        Assert.True(await identity.LoginAsync("Betrieb", Password));
        await vault.OpenAsync(active, first.Id);
        Assert.Equal(document.Draft, (await active.DocumentAsync(document.Id)).Draft);

        var backup = Path.Combine(_root, "alle.d3grcbackup");
        var key = await vault.ExportAsync(active, backup);
        using (var archive = ZipFile.OpenRead(backup))
        {
            Assert.Equal(3, archive.Entries.Count);
            Assert.All(archive.Entries, entry => Assert.DoesNotContain("Betrieb", entry.FullName));
        }
        var individual = Path.Combine(_root, "einzeln.d3grcbackup");
        var individualKey = await vault.ExportAsync(active, individual, first.Id);
        await active.LogoutAsync();

        await RegisterAsync(identity, "Wiederherstellung");
        Assert.Empty(await vault.ListAsync());
        await Assert.ThrowsAnyAsync<CryptographicException>(() => vault.RestoreAsync(backup, new string('A', 48)));
        Assert.Empty(await vault.ListAsync());
        var restored = await vault.RestoreAsync(backup, key);
        Assert.Equal(2, restored.Count);
        var restoredFirst = Assert.Single(restored, tenant => tenant.Name == first.Name);
        Assert.NotEqual(first.Id, restoredFirst.Id);
        await vault.OpenAsync(active, restoredFirst.Id);
        var restoredProfile = await active.GetCompanyProfileAsync();
        Assert.Equal(profile.Industry, restoredProfile?.Industry);
        Assert.Equal(profile.SiteCount, restoredProfile?.SiteCount);
        Assert.Equal(document.Draft, (await active.DocumentAsync(document.Id)).Draft);
        Assert.True(await active.VerifyAsync());
        Assert.Single(await vault.RestoreAsync(individual, individualKey));
        Assert.Equal(3, (await vault.ListAsync()).Count);
    }

    [Fact]
    public async Task Import_reads_a_legacy_copy_and_demo_reopens_with_all_requested_modules()
    {
        var identity = new LocalIdentity(Path.Combine(_root, "profile"));
        await RegisterAsync(identity, "Prüfprofil");
        var vault = new TenantVault(identity);
        using var project = new Workspace(identity);
        var source = Path.Combine(_root, "historisch.d3grc");
        await project.CreateAsync(source, "Historisch", Password, companyProfile: CompanyProfile.Default);
        await project.CloseAsync();
        var previous = await File.ReadAllBytesAsync(source);
        var imported = await vault.ImportLegacyAsync(source, Password);
        Assert.Equal(previous, await File.ReadAllBytesAsync(source));
        Assert.False(File.Exists(source + ".recovery"));
        await vault.OpenAsync(project, imported.Id);
        Assert.Equal("Historisch", project.Name);

        var demo = await vault.CreateAsync(DemoTenant.Name, DemoTenant.Profile, demo: true);
        Assert.Equal(demo.Id, (await vault.CreateAsync(DemoTenant.Name, DemoTenant.Profile, demo: true)).Id);
        await vault.OpenAsync(project, demo.Id);
        foreach (var kind in DesktopModules.Specs.Keys)
            Assert.NotEmpty(await project.ModuleRowsAsync(kind));
        Assert.NotEmpty(await project.EntriesAsync(kind: "Audit"));
        Assert.NotEmpty(await project.EntriesAsync(kind: "Finding"));
        Assert.NotEmpty(await project.EvidenceAsync("demo-control-01"));
        Assert.Equal(2, (await project.LatestRiskAssessmentsAsync(["demo-risk-01", "demo-risk-02", "demo-risk-03"])).Count);
        Assert.Contains(await project.DocumentsAsync(), value => value.Id == "demo-document-02");
        Assert.True(await project.VerifyAsync());
    }

    private static async Task RegisterAsync(LocalIdentity identity, string name)
    {
        await identity.RegisterAsync(name, Password);
        Assert.True(await identity.LoginAsync(name, Password));
    }

    public void Dispose() => Directory.Delete(_root, recursive: true);
}
