using System.Globalization;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Media;
using D3.Grc.Core;

namespace D3.Grc.Desktop;

public sealed partial class MainWindow
{
    private async Task<Control> ResilienceOverviewPage()
    {
        var services = await ReadResilienceRows(ResilienceModule.Service);
        var scenarios = await ReadResilienceRows(ResilienceModule.Scenario);
        var comparisons = CompareResilienceDurations(scenarios, "tolerance_snapshot_minutes", "disruption_minutes");
        var header = ResilienceHeading("Operationelle Resilienz",
            "Kritische Geschäftsdienste, Ausfalltoleranz und Szenariotests im gewählten Geltungsbereich.", Glyph.Shield,
            ResilienceRegisterButton("Geschäftsdienste", ResilienceModule.Service),
            ResilienceRegisterButton("Szenariotests", ResilienceModule.Scenario));
        var panels = Ui.AdaptiveCards(3, 155,
            ResilienceMetric("Aktive Geschäftsdienste", services.Count(row => row.Entry.Status == "Aktiv"), Glyph.Shield),
            ResilienceMetric("Offene Szenariotests", scenarios.Count(row => row.Entry.Status != "Abgeschlossen"), Glyph.Audit),
            ResilienceMetric("Toleranz überschritten", comparisons.Count(item => item.Actual > item.Target), Glyph.Risk));
        return Ui.Scroll(Ui.Stack(header, panels,
            ComparisonCard("Unterbrechungsdauer im Szenariotest", "Zeittoleranz", "Beobachtete Unterbrechung", comparisons,
                "Abgeschlossene Szenariotests mit erfasster Zeitmessung erscheinen hier."),
            Ui.AdaptiveCards(2, 258, ServiceDependencies(services), ResilienceDeadlines(services.Concat(scenarios)))));
    }

    private async Task<Control> ContinuityOverviewPage()
    {
        var analyses = await ReadResilienceRows(ResilienceModule.Bia);
        var plans = await ReadResilienceRows(ResilienceModule.Plan);
        var exercises = await ReadResilienceRows(ResilienceModule.Exercise);
        var comparisons = CompareResilienceDurations(exercises, "rto_snapshot_minutes", "recovery_minutes");
        var header = ResilienceHeading("Business Continuity",
            "Ausfallfolgen beurteilen, Wiederanlauf vorbereiten und die Pläne in Übungen erproben.", Glyph.Tasks,
            ResilienceRegisterButton("Auswirkungsanalysen", ResilienceModule.Bia),
            ResilienceRegisterButton("Wiederanlaufpläne", ResilienceModule.Plan),
            ResilienceRegisterButton("Notfallübungen", ResilienceModule.Exercise));
        var panels = Ui.AdaptiveCards(3, 155,
            ResilienceMetric("Aktive Auswirkungsanalysen", analyses.Count(row => row.Entry.Status == "Aktiv"), Glyph.Requirements),
            ResilienceMetric("Aktive Wiederanlaufpläne", plans.Count(row => row.Entry.Status == "Aktiv"), Glyph.Documents),
            ResilienceMetric("Offene Notfallübungen", exercises.Count(row => row.Entry.Status != "Abgeschlossen"), Glyph.Audit));
        return Ui.Scroll(Ui.Stack(header, panels,
            Ui.AdaptiveCards(2, 270, RecoveryObjectives(analyses),
                ComparisonCard("Wiederanlauf in Notfallübungen", "Wiederanlaufziel", "Beobachteter Wiederanlauf", comparisons,
                    "Abgeschlossene Übungen mit erfasster Zeitmessung erscheinen hier.")),
            ResilienceDeadlines(analyses.Concat(plans).Concat(exercises))));
    }

    private async Task<ModuleRow[]> ReadResilienceRows(string kind)
    {
        var rows = await _workspace.ModuleRowsAsync(kind, EffectiveScope);
        var topicIds = _topicId.Length == 0 ? null :
            (await _workspace.EntriesAsync(_topicId, kind, EffectiveScope)).Select(entry => entry.Id).ToHashSet();
        return rows.Where(row => row.Entry.Status != "Archiviert" && (topicIds is null || topicIds.Contains(row.Entry.Id)) &&
            (Matches(row.Entry) || row.Fields.Values.Any(value => value.Contains(_searchValue, StringComparison.CurrentCultureIgnoreCase))))
            .ToArray();
    }

    private Button ResilienceRegisterButton(string title, string kind) =>
        Button(title, () => OpenModuleEntryAsync(kind), glyph: Glyph.Arrow);

    private Button ResilienceRowButton(Entry entry) =>
        Button(entry.Code.Length == 0 ? entry.Title : $"{entry.Code} · {entry.Title}",
            () => OpenModuleEntryAsync(entry.Kind, entry.Id), glyph: Glyph.Arrow);

    private static Border ResilienceHeading(string title, string details, Glyph icon, params Control[] actions)
    {
        var headline = Ui.Heading(title, 26, 1);
        headline.VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center;
        var card = Ui.Card(Ui.Stack(Ui.Row(Ui.IconBadge(icon, Ui.SoftGreen, Ui.Green, 40), headline),
            Ui.Secondary(details, 14), Ui.Row(actions)));
        card.Background = Ui.BrandWash;
        return card;
    }

    private static Control ResilienceMetric(string title, int value, Glyph symbol) => Ui.Card(Ui.Stack(
        Ui.Row(Ui.IconBadge(symbol, Ui.SoftBlue, Ui.Blue), Ui.Heading(title, 15, 2)),
        Ui.Text(value.ToString(CultureInfo.CurrentCulture), 30, true)));

    private readonly record struct DurationComparison(ModuleRow Row, int Target, int Actual);

    private static DurationComparison[] CompareResilienceDurations(IEnumerable<ModuleRow> rows, string targetField, string actualField) =>
        rows.Where(row => row.Entry.Status == "Abgeschlossen")
            .Select(row => (Row: row, Target: ResilienceMinutes(row, targetField), Actual: ResilienceMinutes(row, actualField)))
            .Where(item => item.Target.HasValue && item.Actual.HasValue)
            .OrderByDescending(item => ResilienceValue(item.Row, "performed_on"), StringComparer.Ordinal)
            .Select(item => new DurationComparison(item.Row, item.Target!.Value, item.Actual!.Value)).ToArray();

    private Control ComparisonCard(string title, string targetLabel, string actualLabel,
        DurationComparison[] comparisons, string empty)
    {
        var group = Ui.Stack(Ui.Heading(title, 19));
        if (comparisons.Length == 0)
        {
            group.Children.Add(Ui.Secondary(empty));
            return Ui.Card(group);
        }
        var visible = comparisons.Take(5).ToArray();
        var maximum = Math.Max(1, visible.Max(item => Math.Max(item.Target, item.Actual)));
        group.Children.Add(Ui.Secondary($"Letzte {visible.Length} mit Zeitmessung · Skala 0–{maximum:N0} Min."));
        foreach (var item in visible)
        {
            var exceeded = item.Actual > item.Target;
            var record = Ui.Card(Ui.Stack(ResilienceRowButton(item.Row.Entry),
                TimeBar(targetLabel, item.Target, maximum, Ui.Blue),
                TimeBar(actualLabel, item.Actual, maximum, exceeded ? Ui.Amber : Ui.Green),
                Ui.Secondary(exceeded ? $"Zeitgrenze um {item.Actual - item.Target:N0} Min. überschritten" : "Zeitgrenze eingehalten"),
                Ui.Secondary(ResilienceValue(item.Row, "result"))));
            record.Background = exceeded ? Ui.SoftAmber : Ui.Canvas;
            group.Children.Add(record);
        }
        return Ui.Card(group);
    }

    private Control RecoveryObjectives(IEnumerable<ModuleRow> analyses)
    {
        var group = Ui.Stack(Ui.Heading("Ausfallgrenze und Wiederanlaufziel", 19));
        var visible = analyses.Where(row => row.Entry.Status == "Aktiv")
            .Select(row => (Row: row, Maximum: ResilienceMinutes(row, "mtpd_minutes"), Target: ResilienceMinutes(row, "rto_minutes")))
            .Where(item => item.Maximum.HasValue && item.Target.HasValue)
            .OrderByDescending(item => ResilienceValue(item.Row, "assessed_on"), StringComparer.Ordinal)
            .Take(5).ToArray();
        if (visible.Length == 0)
        {
            group.Children.Add(Ui.Secondary("Aktive Auswirkungsanalysen mit Ausfallgrenze und Wiederanlaufziel erscheinen hier."));
            return Ui.Card(group);
        }
        var maximum = Math.Max(1, visible.Max(item => item.Maximum!.Value));
        group.Children.Add(Ui.Secondary($"Letzte {visible.Length} aktive Analysen · Skala 0–{maximum:N0} Min."));
        foreach (var item in visible)
        {
            var itemCard = Ui.Stack(ResilienceRowButton(item.Row.Entry),
                TimeBar("Tolerierbare Ausfallzeit (MTPD)", item.Maximum!.Value, maximum, Ui.Blue),
                TimeBar("Wiederanlaufziel (RTO)", item.Target!.Value, maximum, Ui.Green));
            if (ResilienceMinutes(item.Row, "rpo_minutes") is { } rpo)
                itemCard.Children.Add(Ui.Secondary($"Tolerierbarer Datenverlust (RPO) · {rpo:N0} Min."));
            group.Children.Add(Ui.Card(itemCard));
        }
        return Ui.Card(group);
    }

    private static Control TimeBar(string label, int minutes, int maximum, IBrush tone)
    {
        var caption = Ui.Text($"{label} · {minutes:N0} Min.", 12, true);
        var bar = new ProgressBar { Minimum = 0, Maximum = maximum, Value = minutes, Height = 8, Foreground = tone, Background = Ui.Line };
        AutomationProperties.SetName(bar, caption.Text);
        var container = Ui.Stack(caption, bar);
        container.Spacing = 5;
        return container;
    }

    private Control ServiceDependencies(IEnumerable<ModuleRow> services)
    {
        var group = Ui.Stack(Ui.Heading("Abhängigkeiten der Geschäftsdienste", 19));
        var list = services.Take(5).ToArray();
        var objects = _all.ToDictionary(entry => entry.Id);
        var kinds = new HashSet<string>(StringComparer.Ordinal)
            { "Geschäftsdienst → Geschäftsprozess", "Geschäftsdienst → Lieferant", "Geschäftsdienst → Risiko", "Geschäftsdienst → Control" };
        foreach (var item in list)
        {
            var details = Ui.Stack(ResilienceRowButton(item.Entry));
            var links = _mappings.Where(link => link.From == item.Entry.Id && kinds.Contains(link.Type) && objects.ContainsKey(link.To)).ToArray();
            foreach (var link in links.Take(5))
                details.Children.Add(Ui.Text($"{link.Type} · {objects[link.To].Title}", 12));
            if (links.Length > 5)
                details.Children.Add(Ui.Secondary($"Weitere Verknüpfungen · {links.Length - 5}"));
            if (links.Length == 0)
                details.Children.Add(Ui.Secondary("Noch keine Abhängigkeiten verknüpft."));
            group.Children.Add(Ui.Card(details));
        }
        if (list.Length == 0)
            group.Children.Add(Ui.Secondary("Keine Geschäftsdienste im aktuellen Filter."));
        return Ui.Card(group);
    }

    private Control ResilienceDeadlines(IEnumerable<ModuleRow> rows)
    {
        var group = Ui.Stack(Ui.Heading("Anstehende und überfällige Termine", 19));
        var today = DateOnly.FromDateTime(DateTime.Today);
        var dates = rows.Where(row => row.Entry.Status != "Abgeschlossen")
            .Select(row => (Row: row, Due: DateOnly.TryParseExact(row.Entry.DueDate, "yyyy-MM-dd", CultureInfo.InvariantCulture,
                DateTimeStyles.None, out var date) ? (DateOnly?)date : null))
            .Where(item => item.Due.HasValue).OrderBy(item => item.Due).Take(8).ToArray();
        foreach (var item in dates)
        {
            var due = item.Due!.Value;
            var dateText = Ui.Secondary(due.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture) + (due < today ? " · Überfällig" : ""), 13);
            dateText.VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center;
            if (due < today)
                dateText.Foreground = Ui.Amber;
            group.Children.Add(Ui.Row(dateText, ResilienceRowButton(item.Row.Entry)));
        }
        if (dates.Length == 0)
            group.Children.Add(Ui.Secondary("Keine offenen Termine im aktuellen Filter."));
        return Ui.Card(group);
    }

    private static string ResilienceValue(ModuleRow row, string key) => row.Fields.GetValueOrDefault(key, "");

    private static int? ResilienceMinutes(ModuleRow row, string key) =>
        int.TryParse(ResilienceValue(row, key), NumberStyles.None, CultureInfo.InvariantCulture, out var value) ? value : null;
}
