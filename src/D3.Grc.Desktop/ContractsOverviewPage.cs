using System.Globalization;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Layout;
using Avalonia.Media;
using D3.Grc.Core;

namespace D3.Grc.Desktop;

public sealed partial class MainWindow
{
    private async Task<Control> ContractsOverviewPage()
    {
        IReadOnlyList<ModuleRow> contracts = await _workspace.ModuleRowsAsync(ContractModule.Kind, EffectiveScope);
        if (_topicId != "")
        {
            var allowed = (await _workspace.EntriesAsync(_topicId, ContractModule.Kind, EffectiveScope)).Select(entry => entry.Id).ToHashSet();
            contracts = contracts.Where(row => allowed.Contains(row.Entry.Id)).ToList();
        }
        if (_searchValue != "")
            contracts = contracts.Where(row => Matches(row.Entry) || row.Fields.Values.Any(value =>
                RichTextContent.Plain(value).Contains(_searchValue, StringComparison.CurrentCultureIgnoreCase))).ToList();

        var today = DateOnly.FromDateTime(DateTime.Today);
        var horizon = today.AddDays(30);
        var deadlines = ContractModule.Deadlines(contracts);
        var overdue = deadlines.Where(deadline => deadline.Date < today).ToArray();
        var upcoming = deadlines.Where(deadline => deadline.Date >= today && deadline.Date <= horizon).ToArray();
        var later = deadlines.Where(deadline => deadline.Date > horizon).ToArray();

        Control Bar(string label, int value, IBrush brush)
        {
            var bar = new ProgressBar
            {
                Minimum = 0, Maximum = Math.Max(deadlines.Count, 1), Value = value, Height = 8,
                Foreground = brush, Background = Ui.Canvas
            };
            AutomationProperties.SetName(bar, $"{label}, {value} von {deadlines.Count} Terminen");
            return Ui.Stack(Ui.Text($"{label} · {value}", 13, true), bar);
        }

        var distribution = Ui.Card(Ui.Stack(
            Ui.Heading("Fälligkeit", 17),
            Ui.Secondary($"{contracts.Count} Verträge · {contracts.Count(row => row.Entry.Status == "In Prüfung")} in Prüfung"),
            Bar("Überfällig", overdue.Length, Ui.Amber),
            Bar("Heute bis in 30 Tagen", upcoming.Length, Ui.Green),
            Bar("Später", later.Length, Ui.Blue),
            Ui.Secondary("Gezählt werden erfasste Termine aktiver Verträge und von Verträgen in Prüfung.")));
        distribution.VerticalAlignment = VerticalAlignment.Top;

        var filters = new[] { "Überfällig und bis 30 Tage", "Überfällig", "Bis 30 Tage", "Später", "Alle Termine" };
        var filter = Ui.Combo();
        filter.ItemsSource = filters;
        filter.SelectedIndex = 0;
        var count = Ui.Secondary("");
        var list = new ListBox
        {
            ItemTemplate = new FuncDataTemplate<ContractDeadline>((deadline, _) => deadline is null ? null : DeadlineCard(deadline, today))
        };
        AutomationProperties.SetName(list, "Vertragstermine im ausgewählten Filter");
        var listHost = new ContentControl();
        var empty = Ui.Notice("Für diese Auswahl sind keine Termine erfasst. Laufzeiten, Kündigungsstichtage und Prüfungen pflegen Sie im Vertragsregister.");
        empty.VerticalAlignment = VerticalAlignment.Top;
        var open = Button("Vertrag öffnen", async () =>
        {
            if (list.SelectedItem is ContractDeadline deadline)
                await OpenModuleEntryAsync(ContractModule.Kind, deadline.Contract.Entry.Id);
        }, glyph: Glyph.Arrow);
        list.SelectionChanged += (_, _) => open.IsEnabled = list.SelectedItem is ContractDeadline;

        void ApplyFilter()
        {
            IReadOnlyList<ContractDeadline> filtered = filter.SelectedIndex switch
            {
                1 => overdue, 2 => upcoming, 3 => later, 4 => deadlines,
                _ => overdue.Concat(upcoming).ToArray()
            };
            list.ItemsSource = filtered;
            list.SelectedItem = null;
            open.IsEnabled = false;
            listHost.Content = filtered.Count == 0 ? empty : list;
            count.Text = filtered.Count == 1 ? "1 Termin" : $"{filtered.Count} Termine";
        }

        filter.SelectionChanged += (_, _) => ApplyFilter();
        ApplyFilter();
        var header = Ui.Card(Ui.Stack(
            Ui.Row(Ui.IconBadge(Glyph.Documents), Ui.Heading("Vertragsmanagement", 23, 1)),
            Ui.Secondary("Kündigungsstichtage, Prüfungen und Laufzeitenden verfolgen."),
            Ui.Row(Button("Vertragsregister öffnen", () => OpenModuleEntryAsync(ContractModule.Kind), true, glyph: Glyph.Arrow))));
        var queue = Ui.Card(Ui.Fill(Ui.Stack(Ui.Heading("Terminliste", 17), Ui.Field("Fälligkeit auswählen", filter), count), listHost, Ui.Row(open)));
        return Ui.Fill(header, Ui.Split(distribution, queue));
    }

    private static Control DeadlineCard(ContractDeadline deadline, DateOnly today)
    {
        var (entry, fields) = deadline.Contract;
        var title = entry.Code == "" ? entry.Title : $"{entry.Code} · {entry.Title}";
        var date = deadline.Date.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture);
        var caption = Ui.Text($"{date} · {deadline.Label}{(deadline.Date < today ? " · Überfällig" : "")}", 13, true);
        caption.Foreground = deadline.Date < today ? Ui.Amber : Ui.Blue;
        var summary = string.Join(" · ", new[] { fields.GetValueOrDefault("counterparty"), entry.Owner, fields.GetValueOrDefault("decision") }
            .Where(value => !string.IsNullOrWhiteSpace(value)));
        var row = Ui.Stack(caption, Ui.Text(title, 14, true));
        row.Spacing = 3;
        if (summary.Length > 0)
            row.Children.Add(Ui.Secondary(summary, 12));
        return row;
    }
}
