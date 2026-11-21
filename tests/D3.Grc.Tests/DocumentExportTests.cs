using System.IO.Compression;
using System.Xml.Linq;
using D3.Grc.Core;
using Xunit;

namespace D3.Grc.Tests;

public sealed class DocumentExportTests
{
    [Fact]
    public async Task Export_contains_current_document_metadata_for_the_selected_context()
    {
        const string password = "Synthetic document export test";
        const string content = "Vertraulicher Entwurfstext nur für die Projektakte";
        const string review = "Vertrauliche Prüfbegründung nur für die Projektakte";
        string directory = Directory.CreateTempSubdirectory("d3-document-export-").FullName;
        try
        {
            var identity = new LocalIdentity(Path.Combine(directory, "identity"));
            await identity.RegisterAsync("tester", password);
            Assert.True(await identity.LoginAsync("tester", password));
            using var workspace = new Workspace(identity);
            await workspace.CreateAsync(Path.Combine(directory, "export.d3grc"), "Dokumentenexport", password);
            var draft = new DocumentDraft("Datenrichtlinie Nord", "PRIVACY", "Nord", DocumentCategory.Policy, "Datenschutz", content);
            var document = await workspace.CreateDocumentAsync(draft, "Erstanlage");
            document = await workspace.SubmitDocumentForReviewAsync(document.Id, document.Revision, "Prüfung angefordert");
            document = await workspace.ApproveDocumentAsync(document.Id, document.Revision, review);
            document = await workspace.PublishDocumentAsync(document.Id, document.Revision, "Fassung freigegeben");
            document = await workspace.StartDocumentRevisionAsync(document.Id, document.Revision, "Überarbeitung");

            await workspace.CreateDocumentAsync(draft with { Title = "Datenrichtlinie Süd", Scope = "Süd" }, "Vergleichseintrag");
            await workspace.CreateDocumentAsync(draft with { Title = "Datenrichtlinie Technik", TopicId = "ISMS" }, "Vergleichseintrag");
            await workspace.CreateDocumentAsync(draft with { Title = "Verfahrensbeschreibung" }, "Vergleichseintrag");

            var export = await workspace.ExportSnapshotAsync("PRIVACY", "Nord", "Datenrichtlinie");
            var sheet = Assert.Single(export, candidate => candidate.Name == "Dokumente");
            var row = Assert.Single(sheet.Rows);
            Assert.Equal(document.Id, row[0]);
            Assert.Equal(draft.Title, row[1]);
            Assert.Equal("Datenschutz", row[3]);
            Assert.Equal("Nord", row[4]);
            Assert.Equal(DocumentRegister.StatusLabels[DocumentStatus.Draft], row[6]);
            Assert.Equal("2", row[7]);
            Assert.Equal("1", row[8]);
            Assert.Contains(Assert.Single(export, candidate => candidate.Name == "Kennzahlen").Rows,
                values => values[0] == "Dokumente im Register" && values[1] == "1");

            var html = ExcelExport.Html(workspace.Name, export);
            Assert.Contains(draft.Title, html);
            Assert.DoesNotContain(content, html);
            Assert.DoesNotContain(review, html);

            using var xlsx = new ZipArchive(new MemoryStream(ExcelExport.Build(export)));
            using var documentSheet = xlsx.GetEntry($"xl/worksheets/sheet{export.IndexOf(sheet) + 1}.xml")!.Open();
            var xml = XDocument.Load(documentSheet);
            XNamespace spreadsheet = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
            Assert.DoesNotContain(content, xml.ToString());
            Assert.DoesNotContain(review, xml.ToString());
            Assert.Equal("2", xml.Descendants(spreadsheet + "c")
                .Single(cell => (string?)cell.Attribute("r") == "H2").Element(spreadsheet + "v")?.Value);
            Assert.Equal("1", xml.Descendants(spreadsheet + "c")
                .Single(cell => (string?)cell.Attribute("r") == "I2").Element(spreadsheet + "v")?.Value);
            Assert.Equal("2", (string?)xml.Descendants(spreadsheet + "c")
                .Single(cell => (string?)cell.Attribute("r") == "J2").Attribute("s"));

            var empty = await workspace.ExportSnapshotAsync("PRIVACY", "Ost", "Datenrichtlinie");
            Assert.DoesNotContain(empty, candidate => candidate.Name == "Dokumente");
            Assert.Equal(19, empty.Count);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }
}
