using System.Globalization;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Layout;
using D3.Grc.Core;

namespace D3.Grc.Desktop;

public sealed partial class MainWindow
{
    private async Task<Control> ProcessOverviewPage()
    {
        var visibleIds = _entries.Where(entry => entry.Kind == ProcessModule.Kind).Select(entry => entry.Id)
            .ToHashSet(StringComparer.Ordinal);
        var rows = (await _workspace.ModuleRowsAsync(ProcessModule.Kind)).Where(row => visibleIds.Contains(row.Entry.Id))
            .OrderBy(row => row.Entry.Code, StringComparer.CurrentCultureIgnoreCase)
            .ThenBy(row => row.Entry.Title, StringComparer.CurrentCultureIgnoreCase).ToArray();
        var parents = _mappings.Where(link => link.Type == ProcessModule.ParentType).ToLookup(link => link.From);
        var dependencies = _mappings.Where(link => link.Type == ProcessModule.DependencyType).ToLookup(link => link.From);
        var processTitles = _all.Where(entry => entry.Kind == ProcessModule.Kind)
            .ToDictionary(entry => entry.Id, ProcessTitle, StringComparer.Ordinal);
        var category = Ui.Combo();
        category.ItemsSource = new[] { "Alle Prozessarten" }.Concat(ProcessModule.Categories).ToArray();
        category.SelectedIndex = 0;
        var status = Ui.Combo();
        status.ItemsSource = new[] { "Alle Status" }.Concat(Entry.Statuses(ProcessModule.Kind)).ToArray();
        status.SelectedIndex = 0;
        var count = Ui.Secondary("");
        AutomationProperties.SetLiveSetting(count, AutomationLiveSetting.Polite);
        var result = new ContentControl();
        var mapContent = new ContentControl();
        var preview = new ContentControl();
        var notice = Ui.Notice("Wählen Sie einen Prozess, um Ablauf, Verknüpfungen und Prüfung zu sehen.");
        var list = new ListBox
        {
            ItemTemplate = new FuncDataTemplate<ModuleRow>((row, _) => row is null ? null : Ui.Stack(
                Ui.Text(ProcessTitle(row.Entry), 14, true),
                Ui.Secondary($"{ProcessCategory(row)} · {row.Entry.Status}"),
                Ui.Secondary(row.Entry.Owner, 12)))
        };
        AutomationProperties.SetName(list, "Geschäftsprozesse im aktuellen Filter");
        var mapTab = Ui.Tab("Prozesslandkarte", Ui.Scroll(mapContent),
            "Ordnet die Prozesse nach Prozessart und zeigt Unterprozesse in ihrer Hierarchie.", Glyph.Mapping);
        var listTab = Ui.Tab("Liste", new Grid { Children = { list, result } },
            "Zeigt die Prozesse im aktuellen Geltungsbereich und in den gewählten Filtern.", Glyph.Tasks);
        var tabs = new TabControl { ItemsSource = new[] { mapTab, listTab }, SelectedItem = mapTab };
        var edit = Button("Prozess bearbeiten", async () =>
        {
            if (list.SelectedItem is ModuleRow row)
                await OpenModuleEntryAsync(ProcessModule.Kind, row.Entry.Id);
        }, glyph: Glyph.Arrow);

        void ShowPreview(ModuleRow? row)
        {
            edit.IsEnabled = row is not null;
            if (row is null)
            {
                preview.Content = notice;
                return;
            }

            var process = row.Entry;
            var dueText = DateOnly.TryParseExact(process.DueDate, "yyyy-MM-dd", CultureInfo.InvariantCulture,
                DateTimeStyles.None, out var due) ? $"Nächste Überprüfung am {due:dd.MM.yyyy}" : "Überprüfungstermin offen";
            if (due != default && due < DateOnly.FromDateTime(DateTime.Today) && process.Status != "Außer Kraft")
                dueText += " · Überfällig";
            var review = row.Fields.GetValueOrDefault("review_note", "");
            var purpose = row.Fields.GetValueOrDefault("purpose", "");
            var upper = parents[process.Id].Select(link => processTitles.GetValueOrDefault(link.To, "Prozess außerhalb der Auswahl")).ToArray();
            var usedProcesses = dependencies[process.Id].Select(link => processTitles.GetValueOrDefault(link.To, "Prozess außerhalb der Auswahl")).ToArray();
            var information = Ui.Stack(
                Ui.Heading(ProcessTitle(process), 20, 2),
                Ui.Secondary($"{ProcessCategory(row)} · {process.Status} · {process.Scope}"),
                Ui.Secondary(string.IsNullOrWhiteSpace(process.Owner) ? "Verantwortung offen" : $"Verantwortung · {process.Owner}"));
            if (purpose.Length > 0)
                information.Children.Add(Ui.Text(purpose));
            if (upper.Length > 0)
                information.Children.Add(Ui.Secondary("Übergeordneter Prozess · " + string.Join(", ", upper)));
            if (usedProcesses.Length > 0)
                information.Children.Add(Ui.Secondary("Abhängigkeiten · " + string.Join(", ", usedProcesses)));

            var handovers = Ui.Stack();
            foreach (var (key, label) in ProcessHandoverFields)
                if (row.Fields.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value))
                    handovers.Children.Add(Ui.Stack(Ui.Heading(label, 13, 3), Ui.Text(value, 13)));

            var reviewCard = Ui.Stack(Ui.Heading("Überprüfung", 16, 3), Ui.Secondary(dueText));
            if (review.Length > 0)
                reviewCard.Children.Add(Ui.Text(review, 13));
            preview.Content = Ui.Stack(information, handovers,
                ProcessFlowDiagram(ProcessModule.ReadSteps(row.Fields.GetValueOrDefault(ProcessModule.StepKey))),
                reviewCard);
        }

        void Refresh()
        {
            var priorId = (list.SelectedItem as ModuleRow)?.Entry.Id;
            var shown = rows.Where(row => (category.SelectedIndex <= 0 || ProcessCategory(row) == category.SelectedItem as string)
                && (status.SelectedIndex <= 0 || row.Entry.Status == status.SelectedItem as string)).ToArray();
            count.Text = $"{shown.Length} von {rows.Length} Prozessen";
            list.ItemsSource = shown;
            list.SelectedItem = shown.FirstOrDefault(row => row.Entry.Id == priorId) ?? shown.FirstOrDefault();
            result.IsVisible = shown.Length == 0;
            result.Content = shown.Length == 0 ? Ui.Notice("Im aktuellen Filter sind keine Prozesse vorhanden.") : null;
            mapContent.Content = shown.Length == 0 ? Ui.Notice("Im aktuellen Filter sind keine Prozesse vorhanden.") :
                BuildProcessMap(shown, parents, selected => list.SelectedItem = selected);
            ShowPreview(list.SelectedItem as ModuleRow);
        }

        list.SelectionChanged += (_, _) => ShowPreview(list.SelectedItem as ModuleRow);
        category.SelectionChanged += (_, _) => Refresh();
        status.SelectionChanged += (_, _) => Refresh();
        Refresh();

        var title = Ui.Stack(Ui.Heading("Prozessmanagement", 24, 1),
            Ui.Secondary("Geschäftsprozesse in der Landkarte finden und Abläufe, Übergaben und Verantwortlichkeiten nachvollziehen."));
        var filters = Ui.AdaptiveCards(2, 155, Ui.Field("Prozessart", category), Ui.Field("Status", status));
        var register = Ui.Card(Ui.Fill(Ui.Stack(filters, count), tabs,
            Button("Prozessregister öffnen", () => OpenModuleEntryAsync(ProcessModule.Kind), true, glyph: Glyph.Mapping)));
        var detail = Ui.Card(Ui.Fill(Ui.Heading("Prozessdetails", 17, 2), Ui.Scroll(preview), edit));
        return Ui.Fill(title, Ui.Split(register, detail));
    }

    private static readonly (string Key, string Label)[] ProcessHandoverFields =
        [("trigger", "Auslöser"), ("inputs", "Eingaben und Voraussetzungen"), ("outputs", "Ausgaben und Übergabe")];

    private static string ProcessCategory(ModuleRow row) => row.Fields.GetValueOrDefault("process_type", "Ohne Prozessart");

    private static string ProcessTitle(Entry entry) => entry.Code.Length > 0 ? $"{entry.Code} · {entry.Title}" : entry.Title;

    private static Control BuildProcessMap(ModuleRow[] rows, ILookup<string, Mapping> parents, Action<ModuleRow> select)
    {
        var ids = rows.Select(row => row.Entry.Id).ToHashSet(StringComparer.Ordinal);
        var links = rows.SelectMany(row => parents[row.Entry.Id]).Where(link => ids.Contains(link.To)).ToArray();
        var children = links.ToLookup(link => link.To, link => link.From);
        var linkedIds = links.Select(link => link.From).ToHashSet(StringComparer.Ordinal);
        var rowById = rows.ToDictionary(row => row.Entry.Id, StringComparer.Ordinal);
        var map = Ui.Stack();
        var displayed = new HashSet<string>(StringComparer.Ordinal);

        Control Node(ModuleRow row, int depth)
        {
            displayed.Add(row.Entry.Id);
            var label = Ui.Stack(Ui.Text(ProcessTitle(row.Entry), 13, true),
                Ui.Secondary($"{row.Entry.Status} · {row.Fields.GetValueOrDefault("criticality", "Kritikalität offen")}", 12));
            label.Spacing = 3;
            var button = new Button
            {
                Content = label, MinHeight = 45, HorizontalAlignment = HorizontalAlignment.Stretch,
                HorizontalContentAlignment = HorizontalAlignment.Left, Padding = new Thickness(8, 5)
            };
            AutomationProperties.SetName(button, $"{ProcessTitle(row.Entry)} · {row.Entry.Status}");
            button.Click += (_, _) => select(row);
            var branch = Ui.Stack(button);
            branch.Spacing = 5;
            foreach (var child in children[row.Entry.Id])
                if (!displayed.Contains(child))
                    branch.Children.Add(Node(rowById[child], depth + 1));
            return depth == 0 ? branch : new Border
            {
                BorderBrush = Ui.Line, BorderThickness = new Thickness(2, 0, 0, 0),
                Margin = new Thickness(depth <= 4 ? 10 : 2, 0, 0, 0),
                Padding = new Thickness(depth <= 4 ? 7 : 2, 0, 0, 0), Child = branch
            };
        }

        void AddLane(string lane, ModuleRow[] roots)
        {
            var group = Ui.Stack(Ui.Heading(lane, 14, 3));
            group.Spacing = 6;
            foreach (var row in roots)
                if (!displayed.Contains(row.Entry.Id))
                    group.Children.Add(Node(row, 0));
            if (group.Children.Count == 1)
                return;
            var card = Ui.Card(group);
            card.Padding = new Thickness(10);
            card.Background = Ui.NavSurface;
            map.Children.Add(card);
        }

        foreach (var category in ProcessModule.Categories)
            AddLane(category, rows.Where(row => !linkedIds.Contains(row.Entry.Id) && ProcessCategory(row) == category).ToArray());
        AddLane("Ohne Prozessart", rows.Where(row => !linkedIds.Contains(row.Entry.Id) && !displayed.Contains(row.Entry.Id)).ToArray());
        AddLane("Weitere Prozesse", rows.Where(row => !displayed.Contains(row.Entry.Id)).ToArray());
        return map;
    }

    private static Control ProcessFlowDiagram(IReadOnlyList<ProcessStep> steps)
    {
        var flow = Ui.Stack(Ui.Heading("Prozessablauf", 16, 3));
        if (steps.Count == 0)
        {
            flow.Children.Add(Ui.Notice("Für diesen Prozess sind noch keine Schritte erfasst."));
            return flow;
        }

        for (var index = 0; index < steps.Count; index++)
        {
            var number = Ui.Text((index + 1).ToString(CultureInfo.CurrentCulture), 14, true);
            number.HorizontalAlignment = HorizontalAlignment.Center;
            number.VerticalAlignment = VerticalAlignment.Center;
            number.Foreground = Ui.Green;
            var marker = new Border
            {
                Width = 30, Height = 30, CornerRadius = new CornerRadius(15), Background = Ui.SoftGreen,
                Child = number, HorizontalAlignment = HorizontalAlignment.Center
            };
            var step = steps[index];
            var description = Ui.Stack(Ui.Heading(step.Title, 14, 4));
            description.Spacing = 4;
            if (!string.IsNullOrWhiteSpace(step.Role))
                description.Children.Add(Ui.IconLabel(Glyph.Person, step.Role, Ui.Blue));
            if (!string.IsNullOrWhiteSpace(step.Instruction))
                description.Children.Add(Ui.Text(step.Instruction, 13));
            var card = Ui.Card(description);
            card.Padding = new Thickness(10);
            card.Background = Ui.NavSurface;
            var line = new Border
            {
                Width = 2, Background = Ui.Line, HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, index == 0 ? 15 : 0, 0, index == steps.Count - 1 ? 15 : -12),
                IsVisible = steps.Count > 1
            };
            var track = new Grid { Children = { line, marker } };
            marker.VerticalAlignment = VerticalAlignment.Center;
            var row = new Grid { ColumnDefinitions = new ColumnDefinitions("32,*"), ColumnSpacing = 6, Children = { track } };
            Grid.SetColumn(card, 1);
            row.Children.Add(card);
            flow.Children.Add(row);
        }

        return flow;
    }
}
