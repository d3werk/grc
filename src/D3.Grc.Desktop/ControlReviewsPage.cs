using System.Globalization;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using D3.Grc.Core;

namespace D3.Grc.Desktop;

public sealed partial class MainWindow
{
    private Button? _controlReminderButton;

    private Button ControlReminderButton()
    {
        _controlReminderButton = Button("Kontrollprüfungen", OpenAllReviewRemindersAsync,
            help: "Zeigt überfällige und in den nächsten 14 Tagen anstehende Prüfungen im aktuellen Projekt.", glyph: Glyph.Tasks);
        return _controlReminderButton;
    }

    private async Task UpdateControlReminderAsync()
    {
        if (_controlReminderButton is not { } button || !_workspace.IsOpen)
            return;
        var (overdue, upcoming) = await _workspace.ControlReminderCountsAsync(DateOnly.FromDateTime(DateTime.Today));
        var label = overdue > 0 ? $"Prüfungen · {overdue} überfällig" :
            upcoming > 0 ? $"Prüfungen · {upcoming} demnächst" : "Kontrollprüfungen";
        button.Content = Ui.IconLabel(Glyph.Tasks, label, Ui.Ink);
        AutomationProperties.SetName(button, label);
    }

    private async Task OpenAllReviewRemindersAsync()
    {
        if (!await CanLeaveFormAsync())
            return;
        _page = "ControlReviews";
        _topicId = _scopeValue = _searchValue = _statusFilter = "";
        _search.Text = "";
        await Render();
    }

    private async Task<Control> ControlReviewsPage()
    {
        var now = DateTimeOffset.Now;
        var today = DateOnly.FromDateTime(now.DateTime);
        var contextIds = _entries.Select(entry => entry.Id).ToHashSet();
        var mappedControls = _mappings.Where(mapping => mapping.Type == "Anforderung → Control" &&
            mapping.Approval == "Bestätigt" && contextIds.Contains(mapping.From)).Select(mapping => mapping.To).ToHashSet();
        var all = (await _workspace.ControlReviewItemsAsync(EffectiveScope, now))
            .Where(item => contextIds.Contains(item.Control.Id) || mappedControls.Contains(item.Control.Id)).ToList();
        var overdue = all.Count(item => item.Schedule is { } plan && plan.NextReviewOn < today);
        var upcoming = all.Count(item => item.Schedule is { } plan && plan.NextReviewOn >= today && plan.NextReviewOn <= today.AddDays(14));
        var unplanned = all.Count(item => item.Schedule is null);
        var filters = Ui.Combo();
        filters.ItemsSource = new[] { "Alle Controls", "Überfällig", "In den nächsten 14 Tagen", "Ohne Prüfplan" };
        filters.SelectedIndex = 0;
        var controls = new ListBox();
        var details = Ui.Stack(Ui.Secondary("Wählen Sie ein Control aus, um Prüfplan, Risiken und Historie zu sehen."));
        var filtersBar = Ui.Row(Ui.Text($"{overdue} überfällig", 14, true),
            Ui.Text($"{upcoming} demnächst", 14, true), Ui.Text($"{unplanned} ohne Prüfplan", 14, true));
        var layout = new Grid { ColumnDefinitions = new ColumnDefinitions("2*,3*"), ColumnSpacing = 12 };
        var listPanel = Ui.Card(Ui.Fill(Ui.Stack(filtersBar, Ui.Field("Prüffrist filtern", filters)), controls));
        layout.Children.Add(listPanel);
        var detailPanel = Ui.Card(Ui.Scroll(details));
        Grid.SetColumn(detailPanel, 1);
        layout.Children.Add(detailPanel);

        void LoadList()
        {
            var visible = all.Where(item => filters.SelectedIndex switch
            {
                1 => item.Schedule is { } plan && plan.NextReviewOn < today,
                2 => item.Schedule is { } plan && plan.NextReviewOn >= today && plan.NextReviewOn <= today.AddDays(14),
                3 => item.Schedule is null,
                _ => true
            }).ToArray();
            controls.ItemsSource = visible.Select(item => new Choice(item.Control.Id,
                $"{EntryLabel(item.Control)}\n{item.Control.Status} · {ScheduleLabel(item.Schedule, today)}")).ToArray();
            if (visible.Length > 0)
                controls.SelectedIndex = 0;
            else
            {
                details.Children.Clear();
                details.Children.Add(Ui.Secondary("Für den gewählten Filter gibt es keine Controls."));
            }
        }

        controls.SelectionChanged += async (_, _) =>
        {
            var selection = controls.SelectedItem as Choice;
            var item = selection is null ? null : all.FirstOrDefault(row => row.Control.Id == selection.Id);
            if (item is null)
                return;
            try
            {
                var history = await _workspace.ControlReviewsAsync(item.Control.Id);
                var latest = await _workspace.LatestRiskAssessmentsAsync(item.RiskIds);
                if ((controls.SelectedItem as Choice)?.Id != item.Control.Id)
                    return;
                details.Children.Clear();
                details.Children.AddRange(new Control[]
                {
                    Ui.Heading(EntryLabel(item.Control), 20),
                    Ui.Secondary($"{item.Control.Status} · {ScheduleLabel(item.Schedule, today)}"),
                    Ui.Text("Verantwortung · " + (string.IsNullOrWhiteSpace(item.Control.Owner) ? "Noch zuzuweisen" : item.Control.Owner)),
                    Ui.Row(Button("Prüfplan und Verantwortung", () => OpenControlReviewPlannerAsync(item.Control.Id), true, glyph: Glyph.Settings),
                        Button("Prüfung erfassen", () => RecordControlReviewAsync(item), glyph: Glyph.Audit),
                        Button("Control öffnen", () => OpenModuleEntryAsync("Control", item.Control.Id), glyph: Glyph.Arrow)),
                    Ui.Heading("Verknüpfte IT-Risiken", 16),
                    Button("IT-Risiko zuordnen", () => LinkRiskToControlAsync(item.Control), glyph: Glyph.Mapping)
                });
                foreach (var risk in _all.Where(entry => item.RiskIds.Contains(entry.Id)))
                {
                    var value = latest.GetValueOrDefault(risk.Id);
                    var summary = value is null ? "Ohne Bewertung" :
                        $"Ausgang {value.InherentScore} · Restrisiko {value.ResidualScore?.ToString() ?? "offen"}";
                    details.Children.Add(Ui.Row(Button(EntryLabel(risk), () => OpenModuleEntryAsync("Risk", risk.Id)), Ui.Secondary(summary)));
                }
                if (item.RiskIds.Count == 0)
                    details.Children.Add(Ui.Secondary("Noch kein IT-Risiko verknüpft."));
                details.Children.Add(Ui.Heading("Prüfhistorie und Nachweise", 16));
                if (history.Count == 0)
                    details.Children.Add(Ui.Secondary("Noch keine Control-Prüfung erfasst."));
                foreach (var review in history)
                {
                    var evidence = item.Evidence.FirstOrDefault(proof => proof.Id == review.EvidenceId);
                    details.Children.Add(Ui.Card(Ui.Stack(
                        Ui.Text($"{review.PerformedOn:dd.MM.yyyy} · {review.Outcome} · Control-Fassung {review.ControlVersion}", 14, true),
                        Ui.Secondary($"Geprüft von {review.Reviewer} · zuständig {review.Owner}"),
                        Ui.Text(review.Notes),
                        Ui.Secondary(evidence is null ? "Kein Dateinachweis hinterlegt" :
                            $"{evidence.Title} · SHA-256 {review.EvidenceHash}"))));
                }
            }
            catch (Exception ex)
            {
                details.Children.Clear();
                details.Children.Add(Ui.Notice(ex.Message));
            }
        };
        filters.SelectionChanged += (_, _) => LoadList();
        LoadList();
        return Ui.Fill(Ui.Stack(Ui.Heading("Kontrollprüfungen und Fristen", 24, 1),
            Ui.Secondary("Die Anwendung zeigt anstehende und überfällige Termine beim Öffnen des Projekts und während der Nutzung an.")), layout);
    }

    private static string EntryLabel(Entry entry) => entry.Code.Length == 0 ? entry.Title : $"{entry.Code} · {entry.Title}";

    private static string ScheduleLabel(ControlReviewSchedule? schedule, DateOnly today)
    {
        if (schedule is null)
            return "Ohne Prüfplan";
        var next = schedule.NextReviewOn;
        var state = next < today ? "Überfällig seit" : next == today ? "Fällig am" : "Nächste Prüfung";
        return $"{state} {next:dd.MM.yyyy} · alle {schedule.IntervalMonths} Monate";
    }

    private async Task OpenControlReviewPlannerAsync(string controlId, int? suggestedMonths = null)
    {
        var item = (await _workspace.ControlReviewItemsAsync("", DateTimeOffset.Now)).FirstOrDefault(row => row.Control.Id == controlId)
            ?? throw new ArgumentException("Control fehlt.");
        var owner = Ui.Input("Verantwortliche Rolle / Person");
        owner.Text = item.Control.Owner;
        owner.MaxLength = 240;
        var months = Ui.Input("1 bis 60");
        months.Text = (item.Schedule?.IntervalMonths ?? suggestedMonths ?? 12).ToString(CultureInfo.InvariantCulture);
        var date = Ui.Input("JJJJ-MM-TT");
        date.Text = item.Schedule?.NextReviewOn.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? "";
        var reason = Ui.Input("Begründung für die Planung oder Änderung", true);
        int interval = 0;
        DateOnly due = default;
        string? Validate()
        {
            if (string.IsNullOrWhiteSpace(owner.Text) || string.IsNullOrWhiteSpace(reason.Text))
                return "Zuständigkeit und Begründung angeben.";
            if (!int.TryParse(months.Text, out interval) || interval is < 1 or > 60 ||
                !DateOnly.TryParseExact(date.Text, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out due))
                return "Prüfturnus (1 bis 60 Monate) und Datum (JJJJ-MM-TT) prüfen.";
            return null;
        }
        var fields = Ui.Stack(Ui.Secondary(EntryLabel(item.Control)), Ui.Field("Verantwortung *", owner),
            CompanyColumns(Ui.Field("Prüfturnus in Monaten *", months), Ui.Field("Nächste Prüfung *", date)),
            Ui.Field("Begründung *", reason));
        if (!await ShowReviewDialogAsync("Prüfplan bearbeiten", fields, Validate))
            return;
        await _workspace.ConfigureControlReviewAsync(item.Control.Id, item.Control.Version, item.Schedule?.Version ?? 0,
            owner.Text!, interval, due, reason.Text!);
        await Render();
    }

    private async Task RecordControlReviewAsync(ControlReviewItem item)
    {
        if (item.Schedule is not { } schedule)
        {
            await OpenControlReviewPlannerAsync(item.Control.Id);
            return;
        }
        var date = Ui.Input("JJJJ-MM-TT");
        date.Text = DateTime.Today.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        var outcome = Ui.Combo();
        outcome.ItemsSource = Workspace.ControlReviewOutcomes;
        var notes = Ui.Input("Stichprobe, Beobachtung und Folgeschritt", true);
        var evidence = Ui.Combo();
        evidence.ItemsSource = new[] { new Choice("", "Ohne Dateinachweis") }.Concat((await _workspace.EvidenceAsync(item.Control.Id))
            .Select(proof => new Choice(proof.Id, proof.Title))).ToArray();
        evidence.SelectedIndex = 0;
        DateOnly reviewedOn = default;
        string? Validate()
        {
            if (!DateOnly.TryParseExact(date.Text, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out reviewedOn) ||
                outcome.SelectedItem is not string result || string.IsNullOrWhiteSpace(notes.Text))
                return "Prüfdatum, Ergebnis und tatsächliche Beobachtung angeben.";
            return result == "Wirksam" && (evidence.SelectedItem as Choice)?.Id == "" ? "Einen gültigen Nachweis auswählen." : null;
        }
        var fields = Ui.Stack(Ui.Secondary(EntryLabel(item.Control)), CompanyColumns(Ui.Field("Durchgeführt am *", date),
            Ui.Field("Prüfergebnis *", outcome)),
            Ui.Field("Nachweis", evidence, "Für „Wirksam“ prüft die Anwendung die Dateiintegrität und Gültigkeit am Prüfdatum."),
            Ui.Field("Stichprobe und Beobachtung *", notes));
        if (!await ShowReviewDialogAsync("Control-Prüfung erfassen", fields, Validate))
            return;
        var proofId = (evidence.SelectedItem as Choice)?.Id;
        await _workspace.CompleteControlReviewAsync(item.Control.Id, item.Control.Version, schedule.Version, reviewedOn,
            (string)outcome.SelectedItem!, notes.Text!, proofId == "" ? null : proofId);
        await Render();
    }

    private async Task LinkRiskToControlAsync(Entry control)
    {
        var linked = _mappings.Where(mapping => mapping.Type == "Risiko → Control" && mapping.To == control.Id)
            .Select(mapping => mapping.From).ToHashSet();
        var risks = _all.Where(entry => entry.Kind == "Risk" && entry.Scope == control.Scope && !linked.Contains(entry.Id)).ToArray();
        if (risks.Length == 0)
            throw new InvalidOperationException("Für diesen Geltungsbereich ist kein weiteres IT-Risiko verfügbar.");
        if (await Ui.Pick(this, "IT-Risiko auswählen", risks) is not Entry selected)
            return;
        var reason = await Ui.Prompt(this, "Wie behandelt das Control dieses Risiko?");
        if (reason is null)
            return;
        await _workspace.LinkAsync(selected.Id, control.Id, "Risiko → Control", selected.SourceId,
            "Unterstützend", control.Scope, reason);
        await Render();
    }

    private async Task<bool> ShowReviewDialogAsync(string title, Control fields, Func<string?> validate)
    {
        var dialog = new Window
        {
            Title = title, Width = 680, SizeToContent = SizeToContent.Height, MaxHeight = 790,
            WindowStartupLocation = WindowStartupLocation.CenterOwner
        };
        var cancel = new Button { Content = "Abbrechen", IsCancel = true };
        var confirm = new Button { Content = "Übernehmen", IsDefault = true };
        cancel.Click += (_, _) => dialog.Close(false);
        var error = new ContentControl();
        confirm.Click += (_, _) =>
        {
            var message = validate();
            if (message is null)
                dialog.Close(true);
            else
                error.Content = Ui.Notice(message);
        };
        dialog.KeyDown += (_, args) =>
        {
            if (args.Key != Key.Escape)
                return;
            args.Handled = true;
            dialog.Close(false);
        };
        var card = Ui.Card(Ui.Stack(Ui.Heading(title, 21), Ui.Scroll(fields), error, Ui.Row(cancel, confirm)));
        card.Margin = new Thickness(12);
        dialog.Content = card;
        return await dialog.ShowDialog<bool>(this);
    }
}
