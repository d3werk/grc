using System.Globalization;
using System.Text.Json;
using D3.Grc.Core;
using Xunit;

namespace D3.Grc.Tests;

public sealed class SupplierRegisterTests : IDisposable
{
    private const string Password = "Synthetic supplier test password";
    private static readonly SupplierDetails Details = new("Managed database service", SupplierCriticality.Critical,
        "Agreement test-12", "Core processes depend on this provider.");
    private readonly string _directory = Directory.CreateTempSubdirectory("d3-suppliers-").FullName;

    [Fact]
    public async Task Supplier_validation_leaves_no_partial_register_entry()
    {
        using var workspace = await CreateWorkspaceAsync();
        var entry = NewSupplier();

        await Assert.ThrowsAsync<ArgumentException>(() => workspace.SaveEntryAsync(entry, "ISMS", "Initial assessment"));
        await Assert.ThrowsAsync<ArgumentException>(() => workspace.SaveEntryAsync(entry, "ISMS", "Initial assessment",
            supplierDetails: Details with { RiskRationale = "  " }));
        entry.Owner = " ";
        await Assert.ThrowsAsync<ArgumentException>(() => workspace.SaveEntryAsync(entry, "ISMS", "Initial assessment", Details));
        entry.Owner = "Operations";
        entry.Status = "Freigegeben";
        await Assert.ThrowsAsync<ArgumentException>(() => workspace.SaveEntryAsync(entry, "ISMS", "Initial assessment", Details));

        Assert.Empty(await workspace.EntriesAsync(kind: "Supplier"));
        Assert.Null(await workspace.SupplierDetailsAsync(entry.Id));
    }

    [Fact]
    public async Task Supplier_details_and_typed_links_survive_encrypted_reopen()
    {
        using var workspace = await CreateWorkspaceAsync();
        var supplier = NewSupplier();
        var risk = new Entry { Kind = "Risk", Title = "Loss of service" };
        var action = new Entry { Kind = "Measure", Title = "Test replacement procedure" };
        var control = (await workspace.EntriesAsync("ISMS", "Control"))[0];
        await workspace.SaveEntryAsync(supplier, "ISMS", "Initial assessment", Details);
        await workspace.SaveEntryAsync(risk, "ISMS", "Document dependency");
        await workspace.SaveEntryAsync(action, "ISMS", "Reduce dependency");

        await workspace.LinkAsync(supplier.Id, control.Id, "Dienstleister → Control", "INTERNAL", "Teilweise", supplier.Scope, "Control for dependency");
        await workspace.LinkAsync(supplier.Id, risk.Id, "Dienstleister → Risiko", "INTERNAL", "Teilweise", supplier.Scope, "Provider outage scenario");
        await workspace.LinkAsync(action.Id, supplier.Id, "Maßnahme → Dienstleister", "INTERNAL", "Teilweise", supplier.Scope, "Planned corrective action");
        await Assert.ThrowsAsync<ArgumentException>(() => workspace.LinkAsync(risk.Id, supplier.Id,
            "Dienstleister → Risiko", "INTERNAL", "Teilweise", supplier.Scope, "Invalid direction"));

        var path = workspace.ProjectPath!;
        await workspace.CloseAsync();
        await workspace.OpenAsync(path, Password);

        var reopened = Assert.Single(await workspace.EntriesAsync("ISMS", "Supplier", supplier.Scope));
        Assert.Equal(supplier.Id, reopened.Id);
        Assert.Equal(supplier.Owner, reopened.Owner);
        Assert.Equal(Details, await workspace.SupplierDetailsAsync(supplier.Id));
        var batch = await workspace.SupplierDetailsAsync(new[] { supplier.Id, supplier.Id, action.Id });
        Assert.Single(batch);
        Assert.Equal(Details, batch[supplier.Id]);
        Assert.Contains(await workspace.MappingsAsync(), mapping => mapping is { Type: "Dienstleister → Risiko" } && mapping.To == risk.Id);
        Assert.Contains(await workspace.MappingsAsync(), mapping => mapping is { Type: "Dienstleister → Control" } && mapping.To == control.Id);
        Assert.Contains(await workspace.MappingsAsync(), mapping => mapping is { Type: "Maßnahme → Dienstleister" } && mapping.To == supplier.Id);
        Assert.True(await workspace.VerifyAsync());
    }

    [Fact]
    public async Task Reviews_preserve_history_without_rolling_back_the_newest_schedule()
    {
        using var workspace = await CreateWorkspaceAsync();
        var supplier = NewSupplier();
        await workspace.SaveEntryAsync(supplier, "ISMS", "Initial assessment", Details);
        var today = DateOnly.FromDateTime(DateTime.Today);
        var review = new SupplierReviewInput(today, "Risk manager", SupplierReviewOutcome.ApprovedWithActions,
            "Require exit procedure test.", today.AddDays(30));
        await Assert.ThrowsAsync<ArgumentException>(() => workspace.RecordSupplierReviewAsync(supplier.Id, review with { NextReview = null }));
        await Assert.ThrowsAsync<ArgumentException>(() => workspace.RecordSupplierReviewAsync(supplier.Id, review with { NextReview = today }));
        await Assert.ThrowsAsync<ArgumentException>(() => workspace.RecordSupplierReviewAsync(supplier.Id, review with { ReviewedOn = today.AddDays(1) }));

        await workspace.RecordSupplierReviewAsync(supplier.Id, review);
        await workspace.RecordSupplierReviewAsync(supplier.Id, new SupplierReviewInput(today.AddDays(-7), "Previous reviewer",
            SupplierReviewOutcome.Rejected, "Missing agreement.", null));

        var current = await workspace.SupplierEntryAsync(supplier.Id);
        Assert.Equal("Maßnahmen offen", current.Status);
        Assert.Equal(DateText(today.AddDays(30)), current.DueDate);
        Assert.Equal(2, current.Version);
        var history = await workspace.SupplierReviewsAsync(supplier.Id);
        Assert.Equal(new[] { SupplierReviewOutcome.ApprovedWithActions, SupplierReviewOutcome.Rejected }, history.Select(row => row.Outcome));
        Assert.Equal(review.Reviewer, history[0].Reviewer);
        Assert.Null(history[1].NextReview);

        current.DueDate = DateText(today);
        await Assert.ThrowsAsync<ArgumentException>(() => workspace.SaveEntryAsync(current, "ISMS", "Postpone review", Details));
        current.DueDate = DateText(today.AddDays(45));
        current.Owner = "Vendor management";
        await workspace.SaveEntryAsync(current, "ISMS", "Postpone review", Details with { AgreementReference = "Agreement test-13" });
        await Assert.ThrowsAsync<ArgumentException>(() => workspace.DeleteAsync(supplier.Id, "Review history stays available"));

        var updated = Assert.Single(await workspace.EntriesAsync(kind: "Supplier"));
        Assert.Equal(DateText(today.AddDays(45)), updated.DueDate);
        Assert.Equal("Agreement test-13", (await workspace.SupplierDetailsAsync(supplier.Id))!.AgreementReference);
        Assert.Equal(history, await workspace.SupplierReviewsAsync(supplier.Id));
        Assert.Equal(2, (await workspace.JournalAsync()).Count(row => row.Contains("SupplierReviewRecorded")));
    }

    [Fact]
    public async Task Failed_project_write_can_retry_without_appending_the_review_again()
    {
        using var workspace = await CreateWorkspaceAsync();
        var supplier = NewSupplier();
        await workspace.SaveEntryAsync(supplier, "ISMS", "Initial assessment", Details);
        var project = workspace.ProjectPath!;
        var backup = project + ".bak";
        File.Delete(backup);
        Directory.CreateDirectory(backup);
        var today = DateOnly.FromDateTime(DateTime.Today);
        var review = new SupplierReviewInput(today, "Risk manager", SupplierReviewOutcome.Approved, "Restore verified.", today.AddDays(90));

        await Assert.ThrowsAsync<WorkspacePersistenceException>(() => workspace.RecordSupplierReviewAsync(supplier.Id, review));
        Assert.Single(await workspace.SupplierReviewsAsync(supplier.Id));
        Directory.Delete(backup);
        await workspace.SaveAsync();
        await workspace.CloseAsync();
        await workspace.OpenAsync(project, Password);

        Assert.Equal("Freigegeben", Assert.Single(await workspace.EntriesAsync(kind: "Supplier")).Status);
        Assert.Equal(review.Notes, Assert.Single(await workspace.SupplierReviewsAsync(supplier.Id)).Notes);
    }

    [Fact]
    public async Task Opening_a_project_with_the_previous_schema_preserves_an_encrypted_backup_and_company_profile()
    {
        var catalog = Path.Combine(AppContext.BaseDirectory, "content", "catalog-v05.db");
        var previousDb = Path.Combine(_directory, "previous.db");
        File.Copy(catalog, previousDb);
        var profile = CompanyProfile.Default with { Industry = CompanyIndustry.Health };
        using (var db = Database.Open(previousDb))
        {
            Database.Exec(db, null, Database.Resource("006"));
            Database.Exec(db, null, "INSERT INTO __EFMigrationsHistory VALUES('20261118210000_TopicsAndSessions','10.0.14')");
            Database.Exec(db, null, "UPDATE Metadata SET Value = 'Previous project name' WHERE Key = 'ProjectName'");
            Database.Exec(db, null, "INSERT INTO Metadata (Key, Value) VALUES ('CompanyProfile', $p0)",
                JsonSerializer.Serialize(new { Version = 1, Profile = profile }));
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
        var backup = Assert.Single(Directory.EnumerateFiles(_directory, "previous.d3grc.pre08-*.bak"));
        Assert.Equal(original, await File.ReadAllBytesAsync(backup));
        Assert.Equal(original, await File.ReadAllBytesAsync(path));
        Assert.Equal("Previous project name", workspace.Name);
        Assert.Equal(profile.Industry, (await workspace.GetCompanyProfileAsync())!.Industry);
        Assert.Empty(await workspace.EntriesAsync(kind: "Supplier"));
        await workspace.SaveEntryAsync(NewSupplier(), "ISMS", "Initial assessment", Details);
        await workspace.CloseAsync();
        await workspace.OpenAsync(path, Password);
        Assert.Single(await workspace.EntriesAsync(kind: "Supplier"));
        Assert.Single(Directory.EnumerateFiles(_directory, "previous.d3grc.pre08-*.bak"));
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
                await workspace.CreateAsync(Path.Combine(_directory, "test.d3grc"), "Supplier project", Password);
            return workspace;
        }
        catch
        {
            workspace.Dispose();
            throw;
        }
    }

    private static Entry NewSupplier() => new()
    {
        Kind = "Supplier", Title = "Example provider", Owner = "Operations", Scope = "Hosted production systems"
    };

    private static string DateText(DateOnly date) => date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    public void Dispose() => Directory.Delete(_directory, recursive: true);
}
