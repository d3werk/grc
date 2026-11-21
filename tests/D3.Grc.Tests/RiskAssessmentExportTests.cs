using System.Globalization;
using System.IO.Compression;
using System.Xml.Linq;
using D3.Grc.Core;
using Xunit;

namespace D3.Grc.Tests;

public sealed class RiskAssessmentExportTests
{
    [Fact]
    public async Task Export_contains_scoped_assessment_history_and_preserves_empty_residual_values()
    {
        const string password = "Synthetic risk export password";
        var directory = Directory.CreateTempSubdirectory("d3-risk-export-");
        try
        {
            var identity = new LocalIdentity(Path.Combine(directory.FullName, "identity"));
            await identity.RegisterAsync("export-tester", password);
            Assert.True(await identity.LoginAsync("export-tester", password));
            using var workspace = new Workspace(identity);
            await workspace.CreateAsync(Path.Combine(directory.FullName, "risks.d3grc"), "Risikoprüfung", password);

            var visible = new Entry
            {
                Kind = "Risk", Code = "R-01", Title = "Ausfall <Nord>", Scope = "Nord",
                Owner = "Betrieb", SourceId = "ISO27001"
            };
            var hidden = new Entry
            {
                Kind = "Risk", Code = "R-02", Title = "Ausfall Süd", Scope = "Süd",
                Owner = "Betrieb", SourceId = "ISO27001"
            };
            var unrated = new Entry
            {
                Kind = "Risk", Code = "R-03", Title = "Ohne Bewertung", Scope = "Nord",
                Owner = "Betrieb", SourceId = "ISO27001"
            };
            foreach (var risk in new[] { visible, hidden, unrated })
                await workspace.SaveEntryAsync(risk, "ISMS", "Synthetischer Exportfall");

            var assessmentDate = DateOnly.FromDateTime(DateTime.Today.AddDays(-1));
            var initial = await workspace.RecordRiskAssessmentAsync(visible.Id,
                new RiskAssessmentInput(assessmentDate, 4, 4, null, null, RiskResponse.Undecided, "Erste Bewertung"));
            var current = await workspace.RecordRiskAssessmentAsync(visible.Id,
                new RiskAssessmentInput(assessmentDate, 4, 5, 2, 3, RiskResponse.Mitigate, "Behandlung abgestimmt"));
            var backdated = await workspace.RecordRiskAssessmentAsync(visible.Id,
                new RiskAssessmentInput(assessmentDate.AddDays(-7), 2, 3, null, null, RiskResponse.Undecided, "Später nacherfasst"));
            await workspace.RecordRiskAssessmentAsync(hidden.Id,
                new RiskAssessmentInput(assessmentDate, 2, 2, null, null, RiskResponse.Undecided, "Vertrauliche Notiz im anderen Scope"));

            var export = await workspace.ExportSnapshotAsync("ISMS", "Nord", "Ausfall");
            var sheet = Assert.Single(export, item => item.Name == "Risikobewertungen");
            Assert.Equal(3, sheet.Rows.Count);
            Assert.All(sheet.Rows, row =>
            {
                Assert.Equal(visible.Id, row[1]);
                Assert.Equal("Nord", row[4]);
                Assert.Equal(identity.User, row[18]);
            });
            var currentRow = Assert.Single(sheet.Rows, row => row[6] == "Ja");
            Assert.Equal(current.Id, currentRow[0]);
            Assert.Equal(current.RiskVersion.ToString(CultureInfo.InvariantCulture), currentRow[7]);
            Assert.Equal(RiskAssessmentMethod.ResponseLabels[RiskResponse.Mitigate], currentRow[16]);
            Assert.Equal("Behandlung abgestimmt", currentRow[17]);
            var initialRow = Assert.Single(sheet.Rows, row => row[0] == initial.Id);
            Assert.Equal("Nein", initialRow[6]);
            Assert.All(initialRow[12..16], value => Assert.Empty(value));
            Assert.Contains(sheet.Rows, row => row[0] == backdated.Id && row[6] == "Nein");

            var html = ExcelExport.Html("Risiken", export);
            Assert.Contains("<h2>Risikobewertungen</h2>", html);
            Assert.Contains("Ausfall &lt;Nord&gt;", html);
            Assert.DoesNotContain("Vertrauliche Notiz im anderen Scope", html);
            using var workbook = new ZipArchive(new MemoryStream(ExcelExport.Build(export)));
            var sheetIndex = export.IndexOf(sheet) + 1;
            using var stream = workbook.GetEntry($"xl/worksheets/sheet{sheetIndex}.xml")!.Open();
            XNamespace ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
            var cells = XDocument.Load(stream).Descendants(ns + "c").ToDictionary(cell => (string)cell.Attribute("r")!);
            var currentExcelRow = sheet.Rows.IndexOf(currentRow) + 2;
            var initialExcelRow = sheet.Rows.IndexOf(initialRow) + 2;

            var date = cells[$"F{currentExcelRow}"];
            Assert.Equal("2", (string?)date.Attribute("s"));
            Assert.Equal(assessmentDate.ToDateTime(TimeOnly.MinValue).ToOADate().ToString(CultureInfo.InvariantCulture),
                (string?)date.Element(ns + "v"));
            Assert.Equal("20", (string?)cells[$"K{currentExcelRow}"].Element(ns + "v"));
            Assert.Equal("6", (string?)cells[$"O{currentExcelRow}"].Element(ns + "v"));
            Assert.Null(cells[$"O{initialExcelRow}"].Element(ns + "v"));

            var withoutAssessment = await workspace.ExportSnapshotAsync("ISMS", "Nord", "Ohne Bewertung");
            Assert.Single(Assert.Single(withoutAssessment, item => item.Name == "Risk").Rows);
            Assert.Equal(19, withoutAssessment.Count);
            Assert.DoesNotContain(withoutAssessment, item => item.Name == "Risikobewertungen");
        }
        finally
        {
            directory.Delete(recursive: true);
        }
    }
}
