using D3.Grc.Core;
using Xunit;

namespace D3.Grc.Tests;

public sealed class RichTextPersistenceTests : IDisposable
{
    private const string Password = "Synthetic rich-text test passphrase";
    private readonly string _directory = Directory.CreateTempSubdirectory("d3-rich-text-").FullName;

    [Fact]
    public async Task Formatted_entries_templates_and_documents_survive_encrypted_reopen()
    {
        var path = Path.Combine(_directory, "rich-text.d3grc");
        var identity = new LocalIdentity(Path.Combine(_directory, "identity"));
        await identity.RegisterAsync("Tester", Password);
        Assert.True(await identity.LoginAsync("Tester", Password));
        using var workspace = new Workspace(identity);
        await workspace.CreateAsync(path, "Rich text test", Password);

        const string search = "Umsetzung geprüft";
        var plain = search.PadRight(30_000, 'ä');
        var highlight = new RichTextSpan(0, search.Length, RichTextStyle.Bold | RichTextStyle.Underline);
        var rich = new RichTextContent(plain, [highlight]).Serialize();
        Assert.True(rich.Length > 30_000);
        var entry = new Entry { Title = "Control test", Body = rich };
        await workspace.SaveEntryAsync(entry, "ISMS", "Formatted implementation");
        var template = (await workspace.TemplatesAsync("ISMS", "Control"))[0];
        await workspace.SaveTemplateAsync(template.Id, rich);

        const string policyText = "Prüfprozess\nAufzeichnungen dokumentieren.";
        var policy = new RichTextContent(policyText, [new(0, "Prüfprozess".Length, RichTextStyle.Heading)]).Serialize();
        var draft = new DocumentDraft("Prüfrichtlinie", "ISMS", "IT", DocumentCategory.Policy, "Prüfung", policy);
        var created = await workspace.CreateDocumentAsync(draft, "Formatted policy draft");
        var submitted = await workspace.SubmitDocumentForReviewAsync(created.Id, created.Revision, "Submit policy");
        var approved = await workspace.ApproveDocumentAsync(created.Id, submitted.Revision, "Approve policy");
        await workspace.PublishDocumentAsync(created.Id, approved.Revision, "Publish policy");

        await workspace.CloseAsync();
        await workspace.OpenAsync(path, Password);

        var control = Assert.Single(await workspace.EntriesAsync("ISMS", "Control"), item => item.Id == entry.Id);
        Assert.Equal(rich, control.Body);
        Assert.Equal(highlight, Assert.Single(RichTextContent.TryRead(control.Body)!.Spans));
        var savedTemplate = Assert.Single(await workspace.TemplatesAsync("ISMS", "Control"), item => item.Id == template.Id);
        Assert.Equal(template.Body, savedTemplate.Body);
        Assert.Equal(rich, savedTemplate.Effective);
        var document = Assert.Single(await workspace.DocumentsAsync("ISMS", "IT", "Aufzeichnungen"));
        Assert.Equal(policy, document.Draft.Content);
        var version = Assert.Single(await workspace.DocumentVersionsAsync(document.Id));
        Assert.Equal(policyText, RichTextContent.Plain(version.Draft.Content));
        Assert.Equal(RichTextStyle.Heading, Assert.Single(RichTextContent.TryRead(version.Draft.Content)!.Spans).Style);

        var export = Assert.Single(await workspace.ExportSnapshotAsync("ISMS", "", search), item => item.Name == "Control");
        Assert.Equal(plain, Assert.Single(export.Rows)[^1]);
        Assert.True(await workspace.VerifyAsync());
    }

    [Fact]
    public async Task Formatting_does_not_make_blank_document_content_reviewable()
    {
        var identity = new LocalIdentity(Path.Combine(_directory, "identity"));
        await identity.RegisterAsync("Tester", Password);
        Assert.True(await identity.LoginAsync("Tester", Password));
        using var workspace = new Workspace(identity);
        await workspace.CreateAsync(Path.Combine(_directory, "blank.d3grc"), "Rich text test", Password);
        var whitespace = new RichTextContent("  ", [new(0, 2, RichTextStyle.Bold)]).Serialize();
        var draft = new DocumentDraft("Prüfrichtlinie", "ISMS", "", DocumentCategory.Policy, "Prüfung", whitespace);
        var document = await workspace.CreateDocumentAsync(draft, "Formatted blank draft");

        await Assert.ThrowsAsync<ArgumentException>(() =>
            workspace.SubmitDocumentForReviewAsync(document.Id, document.Revision, "Submit blank policy"));
        Assert.Equal(DocumentStatus.Draft, (await workspace.DocumentAsync(document.Id)).Status);
    }

    public void Dispose() => Directory.Delete(_directory, recursive: true);
}
