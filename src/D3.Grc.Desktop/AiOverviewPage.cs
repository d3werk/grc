using System.Globalization;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using D3.Grc.Core;

namespace D3.Grc.Desktop;

public sealed partial class MainWindow
{
    private async Task<Control> AiOverviewPage()
    {
        var visibleIds = _entries.Where(entry => entry.Kind == AiModule.Kind)
            .Select(entry => entry.Id).ToHashSet(StringComparer.Ordinal);
        var systems = (await _workspace.ModuleRowsAsync(AiModule.Kind)).Where(row => visibleIds.Contains(row.Entry.Id))
            .OrderBy(row => ReviewDate(row.Entry) ?? DateOnly.MaxValue)
            .ThenBy(row => row.Entry.Title, StringComparer.CurrentCultureIgnoreCase).ToArray();
        var ongoing = systems.Where(row => row.Entry.Status is not (AiModule.RetiredStatus or "Erledigt")).ToArray();
        var today = DateOnly.FromDateTime(DateTime.Today);
        var underReview = ongoing.Where(row => row.Entry.Status == AiModule.ReviewStatus).ToArray();
        var highRisk = ongoing.Where(row => row.Fields.GetValueOrDefault("risk_class") is "Hoch" or "Kritisch").ToArray();
        var overdue = ongoing.Where(row => ReviewDate(row.Entry) is { } date && date < today).ToArray();

        var heading = Ui.Heading("KI-Systeme im aktuellen Filter", 18, 2);
        var empty = Ui.Secondary("");
        var count = Ui.Secondary("");
        AutomationProperties.SetLiveSetting(count, AutomationLiveSetting.Polite);
        var list = new ListBox
        {
            MinHeight = 240,
            ItemTemplate = new FuncDataTemplate<ModuleRow>((row, _) => row is null ? null : Ui.Stack(
                Ui.Text(string.IsNullOrEmpty(row.Entry.Code) ? row.Entry.Title : $"{row.Entry.Code} · {row.Entry.Title}", 14, true),
                Ui.Secondary(AiRowSummary(row, today))))
        };
        AutomationProperties.SetLabeledBy(list, heading);
        var open = Button("Ausgewähltes KI-System öffnen", async () =>
        {
            if (list.SelectedItem is ModuleRow row)
                await OpenModuleEntryAsync(AiModule.Kind, row.Entry.Id);
        }, glyph: Glyph.Arrow);
        open.IsEnabled = false;
        list.SelectionChanged += (_, _) => open.IsEnabled = list.SelectedItem is ModuleRow;

        void Show(string title, ModuleRow[] rows)
        {
            heading.Text = title;
            count.Text = $"{rows.Length} von {systems.Length} Einträgen im aktuellen Filter";
            list.ItemsSource = rows;
            list.SelectedItem = null;
            list.IsVisible = rows.Length > 0;
            empty.IsVisible = rows.Length == 0;
            empty.Text = systems.Length == 0
                ? "Für das aktuelle Thema, den Geltungsbereich und die Suche sind keine KI-Systeme vorhanden."
                : "Für diese Auswahl sind keine KI-Systeme vorhanden.";
        }

        Button Select(string label, string title, ModuleRow[] rows, Glyph icon) =>
            Button($"{label} · {rows.Length}", () =>
            {
                Show(title, rows);
                return Task.CompletedTask;
            }, glyph: icon);

        Show("KI-Systeme im aktuellen Filter", systems);
        return Ui.Card(Ui.Fill(
            Ui.Stack(
                Ui.Heading("KI-Governance", 24, 1),
                Ui.Secondary("Interne Risikoklassen, Einsatzentscheidungen und anstehende Überprüfungen im Blick behalten."),
                Ui.Row(
                    Select("Alle", "KI-Systeme im aktuellen Filter", systems, Glyph.Ai),
                    Select("In Prüfung", "KI-Systeme in Prüfung", underReview, Glyph.Audit),
                    Select("Hoch oder kritisch", "KI-Systeme mit hoher oder kritischer interner Risikoklasse", highRisk, Glyph.Risk),
                    Select("Überfällige Prüfung", "KI-Systeme mit überfälliger Überprüfung", overdue, Glyph.Tasks)),
                heading, count),
            new Grid { Children = { list, empty } },
            Ui.Row(open, Button("KI-Register öffnen", () => OpenModuleEntryAsync(AiModule.Kind), primary: true, glyph: Glyph.Ai))));
    }

    private static DateOnly? ReviewDate(Entry entry) =>
        DateOnly.TryParseExact(entry.DueDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)
            ? date
            : null;

    private static string AiRowSummary(ModuleRow row, DateOnly today)
    {
        var risk = row.Fields.GetValueOrDefault("risk_class", AiModule.Unassessed);
        var decision = row.Fields.GetValueOrDefault("decision", AiModule.PendingDecision);
        var due = ReviewDate(row.Entry);
        var schedule = due is { } date
            ? $"Nächste Prüfung {date:dd.MM.yyyy}" + (date < today && row.Entry.Status is not (AiModule.RetiredStatus or "Erledigt")
                ? " · Überfällig"
                : "")
            : "Kein Prüftermin";
        var owner = row.Entry.Owner.Length == 0 ? "Verantwortung offen" : row.Entry.Owner;
        return $"{row.Entry.Status} · Risikoklasse {risk} · Entscheidung {decision} · {owner} · {schedule}";
    }
}
