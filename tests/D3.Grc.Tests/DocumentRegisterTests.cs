using D3.Grc.Core;
using Xunit;

namespace D3.Grc.Tests;

public sealed class DocumentRegisterTests : IDisposable
{
    private const string Password = "Synthetic document test password";
    private const string InitialText = "# Incident response\n\nDisconnect affected systems according to the documented procedure.\n";
    private readonly string _directory = Directory.CreateTempSubdirectory("d3-documents-").FullName;

    [Fact]
    public async Task Publication_versions_survive_revision_retirement_and_encrypted_reopen()
    {
        using var workspace = await CreateWorkspaceAsync();
        var created = await workspace.CreateDocumentAsync(NewDraft(), "Initial policy draft");
        Assert.Equal(DocumentStatus.Draft, created.Status);
        Assert.Equal(1, created.Version);
        Assert.Null(created.PublishedVersion);
        await Assert.ThrowsAsync<ArgumentException>(() => workspace.PublishDocumentAsync(created.Id, created.Revision, "Missing review"));

        var reviewed = await ReviewAsync(workspace, created);
        Assert.Equal(DocumentStatus.Reviewed, reviewed.Status);
        Assert.Equal("TESTER", reviewed.ReviewedBy);
        Assert.Empty(await workspace.DocumentVersionsAsync(created.Id));
        var published = await workspace.PublishDocumentAsync(reviewed.Id, reviewed.Revision, "Policy approved for operations");
        Assert.Equal(DocumentStatus.Published, published.Status);
        Assert.Equal(1, published.PublishedVersion);

        var nextDraft = await workspace.StartDocumentRevisionAsync(published.Id, published.Revision, "Clarify scope and responsibilities");
        Assert.Equal(2, nextDraft.Version);
        Assert.Equal(1, nextDraft.PublishedVersion);
        Assert.Empty(nextDraft.ReviewedBy);
        var editedDraft = await workspace.SaveDocumentDraftAsync(nextDraft.Id, nextDraft.Revision,
            nextDraft.Draft with { Scope = "Central IT", Content = "# Updated response\n\nEscalate to the incident lead." }, "Updated instructions");
        Assert.Equal(InitialText, Assert.Single(await workspace.DocumentVersionsAsync(editedDraft.Id)).Draft.Content);

        var reviewedRevision = await ReviewAsync(workspace, editedDraft);
        var publishedRevision = await workspace.PublishDocumentAsync(reviewedRevision.Id, reviewedRevision.Revision, "Replace version one");
        var retired = await workspace.RetireDocumentAsync(publishedRevision.Id, publishedRevision.Revision, "Superseded internally");
        Assert.Equal(DocumentStatus.Retired, retired.Status);
        Assert.Equal(2, retired.PublishedVersion);
        await Assert.ThrowsAsync<ArgumentException>(() => workspace.StartDocumentRevisionAsync(retired.Id, retired.Revision, "Retired is read only"));

        var path = workspace.ProjectPath!;
        await workspace.CloseAsync();
        await workspace.OpenAsync(path, Password);
        Assert.Equal(retired, await workspace.DocumentAsync(retired.Id));
        var versions = await workspace.DocumentVersionsAsync(retired.Id);
        Assert.Equal(new[] { 2, 1 }, versions.Select(version => version.Version));
        Assert.Equal(editedDraft.Draft, versions[0].Draft);
        Assert.Equal(created.Draft, versions[1].Draft);
        Assert.Equal("Policy approved for operations", versions[1].PublicationNote);
        Assert.NotEmpty(versions[1].ReviewedUtc);
        Assert.True(await workspace.VerifyAsync());
    }

    [Fact]
    public async Task Draft_validation_and_revision_guard_protect_reviewed_content()
    {
        using var workspace = await CreateWorkspaceAsync();
        await Assert.ThrowsAsync<ArgumentException>(() => workspace.CreateDocumentAsync(NewDraft() with { TopicId = "UNKNOWN" }, "Invalid topic"));
        await Assert.ThrowsAsync<ArgumentException>(() => workspace.CreateDocumentAsync(NewDraft() with { Owner = " " }, "Missing owner"));
        var draft = await workspace.CreateDocumentAsync(NewDraft() with { Content = "" }, "Prepare outline");
        await Assert.ThrowsAsync<ArgumentException>(() => workspace.SubmitDocumentForReviewAsync(draft.Id, draft.Revision, "Empty document"));
        var updated = await workspace.SaveDocumentDraftAsync(draft.Id, draft.Revision, NewDraft(), "Add the actual document text");
        await Assert.ThrowsAsync<ArgumentException>(() => workspace.SaveDocumentDraftAsync(draft.Id, draft.Revision, NewDraft(), "Stale edit"));
        var submitted = await workspace.SubmitDocumentForReviewAsync(updated.Id, updated.Revision, "Prepare review");
        await Assert.ThrowsAsync<ArgumentException>(() => workspace.SaveDocumentDraftAsync(submitted.Id, submitted.Revision, NewDraft(), "Locked pending review"));
        var returned = await workspace.ReturnDocumentToDraftAsync(submitted.Id, submitted.Revision, "Clarification needed");
        Assert.Equal(DocumentStatus.Draft, returned.Status);
        Assert.Equal(1, returned.Version);
        Assert.Single(await workspace.DocumentsAsync("ISMS", "Hosted systems", "AFFECTED SYSTEMS"));
        Assert.Empty(await workspace.DocumentsAsync("PRIVACY"));
        Assert.Empty(await workspace.DocumentsAsync(scope: "Other scope"));
    }

    [Fact]
    public async Task Document_links_enforce_target_kinds_and_remain_available_after_retirement()
    {
        using var workspace = await CreateWorkspaceAsync();
        var draft = await workspace.CreateDocumentAsync(NewDraft(), "Create policy");
        var requirement = (await workspace.EntriesAsync(kind: "Requirement"))[0];
        var control = (await workspace.EntriesAsync(kind: "Control"))[0];
        await Assert.ThrowsAsync<ArgumentException>(() => workspace.LinkDocumentAsync(draft.Id, DocumentLinkTargetKind.Control, requirement.Id, "Wrong kind"));
        await workspace.LinkDocumentAsync(draft.Id, DocumentLinkTargetKind.Requirement, requirement.Id, "Relevant obligation");
        await workspace.LinkDocumentAsync(draft.Id, DocumentLinkTargetKind.Control, control.Id, "Operational implementation");
        await Assert.ThrowsAsync<ArgumentException>(() => workspace.LinkDocumentAsync(draft.Id, DocumentLinkTargetKind.Control, control.Id, "Duplicate link"));
        await Assert.ThrowsAsync<ArgumentException>(() => workspace.DeleteAsync(control.Id, "Linked control"));

        var links = await workspace.DocumentLinksAsync(draft.Id);
        var requirementLink = Assert.Single(links, link => link.Kind == DocumentLinkTargetKind.Requirement);
        Assert.Equal(requirement.Id, requirementLink.TargetId);
        var controlLink = Assert.Single(links, link => link.Kind == DocumentLinkTargetKind.Control);
        Assert.Equal(control.Id, controlLink.TargetId);
        await workspace.UnlinkDocumentAsync(draft.Id, requirementLink.Id, "Policy covers a narrower scope");
        Assert.Equal(controlLink, Assert.Single(await workspace.DocumentLinksAsync(draft.Id)));

        var reviewed = await ReviewAsync(workspace, draft);
        var published = await workspace.PublishDocumentAsync(reviewed.Id, reviewed.Revision, "Release policy");
        await workspace.RetireDocumentAsync(published.Id, published.Revision, "Historical reference");
        await Assert.ThrowsAsync<ArgumentException>(() => workspace.UnlinkDocumentAsync(draft.Id, controlLink.Id, "Historical link stays"));
        Assert.Equal(controlLink, Assert.Single(await workspace.DocumentLinksAsync(draft.Id)));
    }

    [Fact]
    public async Task Retrying_a_failed_project_write_does_not_repeat_publication()
    {
        using var workspace = await CreateWorkspaceAsync();
        var draft = await workspace.CreateDocumentAsync(NewDraft(), "Create policy");
        var reviewed = await ReviewAsync(workspace, draft);
        var path = workspace.ProjectPath!;
        var backup = path + ".bak";
        File.Delete(backup);
        Directory.CreateDirectory(backup);

        await Assert.ThrowsAsync<WorkspacePersistenceException>(() => workspace.PublishDocumentAsync(reviewed.Id, reviewed.Revision, "Release policy"));
        Assert.Equal(DocumentStatus.Published, (await workspace.DocumentAsync(reviewed.Id)).Status);
        Assert.Single(await workspace.DocumentVersionsAsync(reviewed.Id));
        Directory.Delete(backup);
        await workspace.SaveAsync();
        await workspace.CloseAsync();
        await workspace.OpenAsync(path, Password);

        Assert.Single(await workspace.DocumentVersionsAsync(reviewed.Id));
        Assert.Single(await workspace.JournalAsync(), line => line.Contains("DocumentPublished"));
    }

    [Fact]
    public async Task Opening_a_supplier_schema_project_preserves_original_encrypted_backup()
    {
        var previousDb = Path.Combine(_directory, "previous.db");
        File.Copy(Path.Combine(AppContext.BaseDirectory, "content", "catalog-v05.db"), previousDb);
        using (var connection = Database.Open(previousDb))
        {
            Database.Exec(connection, null, Database.Resource("006"));
            Database.Exec(connection, null, Database.Resource("007"));
            Database.Exec(connection, null, """
                INSERT INTO __EFMigrationsHistory VALUES
                    ('20261118210000_TopicsAndSessions', '10.0.14'), ('20261119090000_SupplierRegister', '10.0.14');
                INSERT INTO Records(Id, Kind, Code, Title, SourceId, Body, Owner)
                    VALUES ('prior-supplier', 'Supplier', '', 'Existing supplier', 'INTERNAL', '', 'Operations');
                INSERT INTO SupplierDetails VALUES ('prior-supplier', 'Hosting', 3, '', 'Primary production dependency');
                """);
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
        var archived = Assert.Single(Directory.EnumerateFiles(_directory, "previous.d3grc.pre09-*.bak"));
        Assert.Equal(original, await File.ReadAllBytesAsync(archived));
        Assert.Equal(original, await File.ReadAllBytesAsync(path));
        Assert.Empty(Directory.EnumerateFiles(_directory, "previous.d3grc.pre08-*.bak"));
        Assert.Equal("Hosting", (await workspace.SupplierDetailsAsync("prior-supplier"))!.Service);
        Assert.Empty(await workspace.DocumentsAsync());
        var document = await workspace.CreateDocumentAsync(NewDraft(), "First document after upgrade");
        await workspace.CloseAsync();
        await workspace.OpenAsync(path, Password);
        Assert.Equal(document, Assert.Single(await workspace.DocumentsAsync()));
        Assert.Single(Directory.EnumerateFiles(_directory, "previous.d3grc.pre09-*.bak"));
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
                await workspace.CreateAsync(Path.Combine(_directory, "project.d3grc"), "Document project", Password);
            return workspace;
        }
        catch
        {
            workspace.Dispose();
            throw;
        }
    }

    private static async Task<DocumentRecord> ReviewAsync(Workspace workspace, DocumentRecord draft)
    {
        var submitted = await workspace.SubmitDocumentForReviewAsync(draft.Id, draft.Revision, "Ready for specialist review");
        return await workspace.ApproveDocumentAsync(submitted.Id, submitted.Revision, "Responsibilities and procedure verified");
    }

    private static DocumentDraft NewDraft() => new("Incident management", "ISMS", "Hosted systems",
        DocumentCategory.Policy, "Operations", InitialText);

    public void Dispose() => Directory.Delete(_directory, recursive: true);
}
