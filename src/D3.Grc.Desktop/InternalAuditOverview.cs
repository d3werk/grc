using System.Globalization;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using D3.Grc.Core;

namespace D3.Grc.Desktop;

public sealed partial class MainWindow
{
    private sealed record AuditPlanRow(Entry Audit, IReadOnlyList<AuditCoverage> Coverage, int OpenFindings)
    {
        public int Tested => Coverage.Count(item => item.Outcome is not null);
        public int MissingFollowUp => Coverage.Count(item => item.Outcome == "Abweichung" && !item.HasLinkedFinding);
    }

    private async Task<Control> InternalAuditOverviewPage()
    {
        var audits = _entries.Where(entry => entry.Kind == "Audit").ToArray();
        var findings = _entries.Where(entry => entry.Kind == "Finding" && entry.Status is not ("Geschlossen" or "Entwurf"))
            .OrderBy(entry => AuditDate(entry) ?? DateOnly.MaxValue).ThenBy(entry => entry.Title).ToArray();
        var coverage = (await _workspace.AuditCoverageAsync(audits.Select(audit => audit.Id).ToArray())).ToLookup(row => row.AuditId);
        var openFindingIds = findings.Select(finding => finding.Id).ToHashSet(StringComparer.Ordinal);
        var auditFindings = _mappings.Where(mapping => mapping.Type == "Finding → Audit" && openFindingIds.Contains(mapping.From))
            .ToLookup(mapping => mapping.To);
        var measuresByFinding = _mappings.Where(mapping => mapping.Type == "Maßnahme → Finding").ToLookup(mapping => mapping.To);
        var planRows = audits.OrderBy(audit => AuditDate(audit) ?? DateOnly.MaxValue).ThenBy(audit => audit.Title)
            .Select(audit => new AuditPlanRow(audit, coverage[audit.Id].ToArray(), auditFindings[audit.Id].Count())).ToArray();
        var today = DateOnly.FromDateTime(DateTime.Today);

        var years = audits.Select(AuditDate).OfType<DateOnly>().Select(date => date.Year).Append(today.Year).Distinct().OrderDescending();
        var yearChoices = new[] { new Choice("", "Alle Jahre") }.Concat(years.Select(year => new Choice(
            year.ToString(CultureInfo.InvariantCulture), year.ToString(CultureInfo.CurrentCulture)))).ToArray();
        var yearFilter = Ui.Combo();
        yearFilter.ItemsSource = yearChoices;
        yearFilter.SelectedIndex = 0;
        var planHeading = Ui.Heading("Prüfplan", 18, 2);
        var planCount = Ui.Secondary("");
        AutomationProperties.SetLiveSetting(planCount, AutomationLiveSetting.Polite);
        var planList = new ListBox
        {
            MinHeight = 140,
            ItemTemplate = new FuncDataTemplate<AuditPlanRow>((row, _) => row is null ? null : Ui.Stack(
                Ui.Text(AuditIksTitle(row.Audit), 14, true),
                Ui.Secondary($"{row.Audit.Status} · Termin {AuditDateLabel(row.Audit)} · {row.Tested}/{row.Coverage.Count} Controls geprüft"),
                Ui.Secondary($"{row.OpenFindings} offene Feststellungen · {row.MissingFollowUp} Abweichungen ohne Feststellung")))
        };
        AutomationProperties.SetLabeledBy(planList, planHeading);
        var work = Button("Prüfdossier öffnen", async () =>
        {
            if (planList.SelectedItem is not AuditPlanRow row || !await CanLeaveFormAsync())
                return;
            _body.Content = await AuditPage(row.Audit);
        }, true, glyph: Glyph.Arrow);
        var edit = Button("Prüfauftrag bearbeiten", async () =>
        {
            if (planList.SelectedItem is AuditPlanRow row)
                await OpenModuleEntryAsync("Audit", row.Audit.Id);
        }, glyph: Glyph.Audit);
        planList.SelectionChanged += (_, _) => work.IsEnabled = edit.IsEnabled = planList.SelectedItem is AuditPlanRow;

        void RefreshPlan()
        {
            var selectedYear = (yearFilter.SelectedItem as Choice)?.Id ?? "";
            var visible = planRows.Where(row => selectedYear == "" || AuditDate(row.Audit)?.Year.ToString(CultureInfo.InvariantCulture) == selectedYear)
                .ToArray();
            planList.ItemsSource = visible;
            planList.SelectedItem = visible.FirstOrDefault();
            work.IsEnabled = edit.IsEnabled = visible.Length > 0;
            planCount.Text = visible.Length == 0 ? "Keine Prüfaufträge im aktuellen Filter." :
                $"{visible.Length} Prüfaufträge · {visible.Count(row => row.Audit.Status == "Abgeschlossen")} abgeschlossen";
        }
        yearFilter.SelectionChanged += (_, _) => RefreshPlan();
        RefreshPlan();

        var findingsHeading = Ui.Heading("Offene Feststellungen", 18, 2);
        var findingsList = new ListBox
        {
            MinHeight = 140,
            ItemsSource = findings,
            ItemTemplate = new FuncDataTemplate<Entry>((entry, _) => entry is null ? null : Ui.Stack(
                Ui.Text(AuditIksTitle(entry), 14, true),
                Ui.Secondary($"{entry.Status} · Frist {AuditDateLabel(entry)} · {measuresByFinding[entry.Id].Count()} Maßnahmen"),
                AuditDate(entry) < today ? Ui.Text("Überfällig", 12, true) : Ui.Secondary(entry.Owner)))
        };
        AutomationProperties.SetLabeledBy(findingsList, findingsHeading);
        var openFinding = Button("Feststellung öffnen", async () =>
        {
            if (findingsList.SelectedItem is Entry entry)
                await OpenModuleEntryAsync("Finding", entry.Id);
        }, glyph: Glyph.Arrow);
        openFinding.IsEnabled = false;
        findingsList.SelectionChanged += (_, _) => openFinding.IsEnabled = findingsList.SelectedItem is Entry;
        findingsList.SelectedItem = findings.FirstOrDefault();

        var planCard = Ui.Card(Ui.Fill(Ui.Stack(planHeading, Ui.Field("Planjahr", yearFilter,
            "Der Jahresschnitt verwendet den Termin des Prüfauftrags."), planCount), planList,
            Ui.Row(work, edit, Button("Prüfaufträge verwalten", () => OpenModuleEntryAsync("Audit"), glyph: Glyph.Audit))));
        var findingsCard = Ui.Card(Ui.Fill(Ui.Stack(findingsHeading,
            Ui.Secondary($"{findings.Length} offen · {findings.Count(entry => AuditDate(entry) < today)} überfällig")), findingsList,
            Ui.Row(openFinding, Button("Feststellungen verwalten", () => OpenModuleEntryAsync("Finding"), glyph: Glyph.Finding))));
        var title = Ui.Stack(Ui.Heading("Interne Audits", 24, 1),
            Ui.Secondary("Prüfaufträge, Control-Abdeckung und offene Feststellungen im gewählten Geltungsbereich."));
        return Ui.Fill(title, Ui.Split(planCard, findingsCard));
    }

    private static DateOnly? AuditDate(Entry entry) => DateOnly.TryParseExact(entry.DueDate, "yyyy-MM-dd",
        CultureInfo.InvariantCulture, DateTimeStyles.None, out var value) ? value : null;

    private static string AuditDateLabel(Entry entry) => AuditDate(entry)?.ToString("dd.MM.yyyy", CultureInfo.CurrentCulture) ?? "nicht festgelegt";

    private static string AuditIksTitle(Entry entry) => entry.Code.Length > 0 ? $"{entry.Code} · {entry.Title}" : entry.Title;
}
