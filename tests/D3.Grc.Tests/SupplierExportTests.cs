using System.Globalization;
using System.IO.Compression;
using System.Xml.Linq;
using D3.Grc.Core;
using Xunit;

namespace D3.Grc.Tests;

public sealed class SupplierExportTests
{
    [Fact]
    public async Task Export_includes_only_filtered_suppliers_and_reviews_with_excel_dates()
    {
        const string password = "Synthetic supplier export password";
        var directory = Directory.CreateTempSubdirectory("d3-supplier-export-");
        try
        {
            var identity = new LocalIdentity(Path.Combine(directory.FullName, "identity"));
            await identity.RegisterAsync("export-tester", password);
            Assert.True(await identity.LoginAsync("export-tester", password));
            using var workspace = new Workspace(identity);
            await workspace.CreateAsync(Path.Combine(directory.FullName, "register.d3grc"), "Testregister", password);

            var today = DateOnly.FromDateTime(DateTime.Today);
            var reviewedOn = today.AddDays(-20);
            var nextReview = today.AddDays(-1);
            var included = new Entry
            {
                Kind = "Supplier", Code = "POOL-A", Title = "Plattform <Nord>", SourceId = "GDPR", Scope = "Nord",
                Owner = "Einkauf"
            };
            var excluded = new Entry
            {
                Kind = "Supplier", Code = "POOL-B", Title = "Plattform Süd", SourceId = "GDPR", Scope = "Süd",
                Owner = "Einkauf"
            };
            var details = new SupplierDetails("Betrieb der Plattform", SupplierCriticality.Critical, "V-2026-004", "Hohe Abhängigkeit");
            await workspace.SaveEntryAsync(included, "PRIVACY", "Synthetischer Dienstleister", details);
            await workspace.SaveEntryAsync(excluded, "PRIVACY", "Synthetischer Dienstleister", details);
            await workspace.RecordSupplierReviewAsync(included.Id, new SupplierReviewInput(
                reviewedOn, "Fachprüfung", SupplierReviewOutcome.Approved, "Weiterbetrieb dokumentiert", nextReview));
            await workspace.RecordSupplierReviewAsync(excluded.Id, new SupplierReviewInput(
                reviewedOn, "Fachprüfung", SupplierReviewOutcome.Approved, "Ausgeschlossene Notiz", today.AddDays(10)));

            var export = await workspace.ExportSnapshotAsync("PRIVACY", "Nord", "Plattform");
            var register = Assert.Single(export, sheet => sheet.Name == "Lieferanten");
            var supplier = Assert.Single(register.Rows);
            Assert.Equal(included.Id, supplier[0]);
            Assert.Contains("V-2026-004", supplier);
            Assert.Contains("Hohe Abhängigkeit", supplier);
            var history = Assert.Single(export, sheet => sheet.Name == "Lieferantenprüfungen");
            var review = Assert.Single(history.Rows);
            Assert.Equal(included.Id, review[1]);
            Assert.Contains("Weiterbetrieb dokumentiert", review);

            var metrics = Assert.Single(export, sheet => sheet.Name == "Kennzahlen").Rows;
            Assert.Contains(metrics, row => row[0] == "Lieferanten im Register" && row[1] == "1");
            Assert.Contains(metrics, row => row[0] == "Kritisch eingestufte Lieferanten (laufend)" && row[1] == "1");
            Assert.Contains(metrics, row => row[0] == "Überfällige Lieferantenprüfungen" && row[1] == "1");

            var html = ExcelExport.Html("Prüfbericht", export);
            Assert.Contains("<h2>Lieferanten</h2>", html);
            Assert.Contains("Plattform &lt;Nord&gt;", html);
            Assert.DoesNotContain("Ausgeschlossene Notiz", html);
            using var workbook = new ZipArchive(new MemoryStream(ExcelExport.Build(export)));
            AssertExcelDate(workbook, export.IndexOf(register) + 1, "H2", nextReview);
            AssertExcelDate(workbook, export.IndexOf(history) + 1, "D2", reviewedOn);
            AssertExcelDate(workbook, export.IndexOf(history) + 1, "G2", nextReview);

            var empty = await workspace.ExportSnapshotAsync("PRIVACY", "Nord", "Kein Treffer");
            Assert.Equal(19, empty.Count);
            Assert.DoesNotContain(empty, sheet => sheet.Name.StartsWith("Lieferanten", StringComparison.Ordinal));
            await workspace.SaveCompanyProfileAsync(CompanyProfile.Default);
            var configured = await workspace.ExportSnapshotAsync("PRIVACY", "Nord", "Kein Treffer");
            Assert.Equal(20, configured.Count);
            Assert.Contains(configured, sheet => sheet.Name == "Umsetzungsplan");
        }
        finally
        {
            directory.Delete(recursive: true);
        }
    }

    private static void AssertExcelDate(ZipArchive workbook, int sheet, string cellReference, DateOnly expected)
    {
        using var stream = workbook.GetEntry($"xl/worksheets/sheet{sheet}.xml")!.Open();
        XNamespace ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
        var cell = XDocument.Load(stream).Descendants(ns + "c").Single(element => (string?)element.Attribute("r") == cellReference);
        Assert.Equal("2", (string?)cell.Attribute("s"));
        Assert.Equal(expected.ToDateTime(TimeOnly.MinValue).ToOADate().ToString(CultureInfo.InvariantCulture),
            (string?)cell.Element(ns + "v"));
    }
}
