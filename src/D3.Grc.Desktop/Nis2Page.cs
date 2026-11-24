using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using D3.Grc.Core;

namespace D3.Grc.Desktop;

public sealed partial class MainWindow
{
    private string _nis2PackageKey = "21a";
    private string _nis2Scope = "";

    private async Task<Control> Nis2Page()
    {
        var scopes = await _workspace.ScopeOptionsAsync();
        var prior = _nis2Scope.Length > 0 ? _nis2Scope : EffectiveScope;
        var scopeChoices = new[] { new Choice("", "Bereich wählen") }.Concat(scopes
            .Append(prior).Where(scope => scope.Length > 0).Distinct(StringComparer.Ordinal)
            .OrderBy(scope => scope, StringComparer.CurrentCultureIgnoreCase).Select(scope => new Choice(scope, scope))).ToArray();
        var scopeBox = Ui.Combo();
        scopeBox.ItemsSource = scopeChoices;
        scopeBox.SelectedItem = scopeChoices.FirstOrDefault(choice => choice.Id == prior) ?? scopeChoices[0];

        var catalogue = new ListBox
        {
            ItemsSource = Nis2Catalog.Packages,
            ItemTemplate = new FuncDataTemplate<Nis2Package>((package, _) =>
            {
                if (package is null)
                    return null;
                var row = Ui.Stack(Ui.Text(package.Title, 14, true), Ui.Secondary(package.Reference));
                row.Spacing = 3;
                return row;
            })
        };
        ScrollViewer.SetHorizontalScrollBarVisibility(catalogue, ScrollBarVisibility.Disabled);
        var details = new ContentControl();
        await DrawAsync();

        catalogue.SelectedItem = Nis2Catalog.Get(_nis2PackageKey);
        catalogue.SelectionChanged += async (_, _) =>
        {
            if (catalogue.SelectedItem is not Nis2Package selectedPackage || selectedPackage.Key == _nis2PackageKey)
                return;
            _nis2PackageKey = selectedPackage.Key;
            await Run(DrawAsync);
        };
        scopeBox.SelectionChanged += async (_, _) =>
        {
            _nis2Scope = (scopeBox.SelectedItem as Choice)?.Id ?? "";
            await Run(DrawAsync);
        };
        var newScope = Button("Neuen Bereich benennen", async () =>
        {
            var name = await Ui.Prompt(this, "Geltungsbereich für dieses NIS2-Paket");
            if (name is null)
                return;
            name = name.Trim();
            if (name.Length is < 1 or > 240)
                throw new ArgumentException("Geltungsbereich mit höchstens 240 Zeichen angeben.");
            _nis2Scope = name;
            await Render();
        }, glyph: Glyph.Plus);
        var catalogueCard = Ui.Card(Ui.Fill(Ui.Stack(
            Ui.Heading("Maßnahmenkatalog", 19),
            Ui.Field("Geltungsbereich des Pakets", scopeBox), newScope), catalogue));
        var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("2*,4*"), ColumnSpacing = 12 };
        grid.Children.Add(catalogueCard);
        Grid.SetColumn(details, 1);
        grid.Children.Add(details);
        return Ui.Fill(Ui.Stack(Ui.Heading("NIS2 · Anforderungen und Maßnahmen", 24, 1),
            Ui.Secondary("Anforderungen nach Artikel 21 planen, vorhandene Controls wiederverwenden und Meldeprozesse nach Artikel 23 gesondert bearbeiten.")), grid);

        async Task DrawAsync()
        {
            var scope = (scopeBox.SelectedItem as Choice)?.Id ?? "";
            var package = Nis2Catalog.Get(_nis2PackageKey);
            var state = scope.Length == 0 ? null : await _workspace.Nis2PackageStateAsync(package.Key, scope);
            var panel = Ui.Stack(Ui.Heading(package.Title, 22), Ui.Secondary(package.Reference), Ui.Text(package.Requirement));
            if (state is { RequirementChoices.Count: > 1 })
            {
                var pick = Ui.Combo();
                pick.ItemsSource = state.RequirementChoices.Select(entry => new Choice(entry.Id, EntryLabel(entry))).ToArray();
                var select = Button("Anforderung für das Paket auswählen", async () =>
                {
                    if (pick.SelectedItem is Choice choice)
                    {
                        await _workspace.Nis2ApplyRequirementAsync(package.Key, scope, choice.Id);
                        await Render();
                        _status.Text = "Vorhandene Anforderung für das Paket ausgewählt.";
                    }
                }, true);
                select.IsEnabled = false;
                pick.SelectionChanged += (_, _) => select.IsEnabled = pick.SelectedItem is Choice;
                panel.Children.Add(Ui.Card(Ui.Stack(
                    Ui.Secondary("Für diese Fundstelle gibt es mehrere Anforderungen im gewählten Bereich."),
                    Ui.Field("Vorhandene Anforderung", pick), select)));
                details.Content = Ui.Card(Ui.Scroll(panel));
                return;
            }
            var reuse = Button("Vorhandenes Control zuordnen", () => PickNis2ControlMappingAsync(package, scope),
                help: state?.RequirementId is null ? "Übernehmen Sie zuerst die Anforderung." :
                    "Ein vorhandenes Control wählen und seine Abdeckung begründen.", glyph: Glyph.Mapping);
            reuse.IsEnabled = state?.RequirementId is not null;
            var actions = Ui.Row(
                Button("Anforderung übernehmen", async () =>
                {
                    await _workspace.Nis2ApplyRequirementAsync(package.Key, scope);
                    await Render();
                    _status.Text = "Anforderung für " + scope + " verfügbar.";
                }, true, glyph: Glyph.Plus),
                Button("Paket übernehmen", () => ApplyNis2PackageAsync(package, scope), glyph: Glyph.Tasks), reuse);
            actions.IsEnabled = scope.Length > 0;
            panel.Children.Add(actions);
            if (state?.RequirementId is { } requirementId)
                panel.Children.Add(Button("Anforderung öffnen", () => OpenModuleEntryAsync("Requirement", requirementId), glyph: Glyph.Arrow));
            else
                panel.Children.Add(Ui.Secondary(scope.Length == 0
                    ? "Wählen Sie einen Projektbereich für die Übernahme und die Zuordnungen."
                    : "Die Übernahme erstellt eine bearbeitbare Anforderung im Entwurfsstatus."));
            if (state is not null)
            {
                panel.Children.Add(Ui.Heading("Verknüpfte Controls", 16));
                if (state.LinkedControls.Count == 0)
                    panel.Children.Add(Ui.Secondary("Für diese Anforderung besteht noch keine Zuordnung."));
                foreach (var linked in state.LinkedControls)
                {
                    var mapped = _mappings.FirstOrDefault(link => link.Type == "Anforderung → Control" &&
                        link.From == state.RequirementId && link.To == linked.Id);
                    var template = package.Controls.FirstOrDefault(control => state.ControlIds.GetValueOrDefault(control.Key) == linked.Id);
                    var label = linked.Code.Length == 0 ? linked.Title : $"{linked.Code} · {linked.Title}";
                    panel.Children.Add(Ui.Card(Ui.Stack(
                        Ui.Text(label, 14, true),
                        Ui.Secondary(mapped is null ? "Zuordnung aus dem Paket" :
                            $"{mapped.Approval} · {mapped.Coverage} · {mapped.Scope}"),
                        Ui.Row(Button("Control öffnen", () => OpenModuleEntryAsync("Control", linked.Id), glyph: Glyph.Arrow),
                            Button("Prüfplan und Verantwortung", () => OpenControlReviewPlannerAsync(linked.Id, template?.ReviewMonths), glyph: Glyph.Settings),
                            Button("IT-Risiko zuordnen", () => LinkRiskForNis2ControlAsync(linked.Id), glyph: Glyph.Mapping)))));
                }
                if (state.MeasureIds.Count > 0)
                {
                    panel.Children.Add(Ui.Heading("Maßnahmen im Projekt", 16));
                    foreach (var measure in _all.Where(entry => state.MeasureIds.Values.Contains(entry.Id)))
                        panel.Children.Add(Button(EntryLabel(measure), () => OpenModuleEntryAsync("Measure", measure.Id), glyph: Glyph.Arrow));
                }
            }
            panel.Children.Add(Ui.Heading("Vorbereitete Controls und Aufgaben", 17));
            foreach (var control in package.Controls)
            {
                var measures = Ui.Stack(Ui.Heading("Maßnahmen und Abnahmekriterien", 15));
                foreach (var measure in control.Measures)
                {
                    measures.Children.Add(Ui.Text(measure.Title, 14, true));
                    measures.Children.Add(Ui.Text(measure.Instructions));
                    measures.Children.Add(Ui.Secondary("Abnahme · " + measure.Acceptance));
                }
                panel.Children.Add(Ui.Card(Ui.Stack(Ui.Heading(control.Title, 17),
                    Ui.Secondary($"Empfohlene Rolle · {control.OwnerRole} · Prüfturnus {control.ReviewMonths} Monate"),
                    Ui.Text(control.Implementation),
                    Ui.Text("Erwarteter Nachweis", 14, true), Ui.Text(control.Evidence),
                    Ui.Text("Risikoszenario und Prüftrigger", 14, true), Ui.Text(control.RiskScenario),
                    Ui.Secondary(control.ReviewTriggers), measures)));
            }
            details.Content = Ui.Card(Ui.Scroll(panel));
        }
    }

    private async Task ApplyNis2PackageAsync(Nis2Package package, string scope)
    {
        var state = await _workspace.Nis2PackageStateAsync(package.Key, scope);
        var existing = (await _workspace.EntriesAsync(kind: "Control", scope: scope)).Where(control => !control.IsSeed).ToArray();
        var candidates = new[] { new Choice("", "Nicht übernehmen"), new Choice("new", "Neues Control als Entwurf") }
            .Concat(existing.Select(control => new Choice(control.Id, "Vorhanden · " + EntryLabel(control)))).ToArray();
        var fields = Ui.Stack(Ui.Secondary($"Wählen Sie pro Vorlage, ob und welches Control in „{scope}“ übernommen wird. " +
            "Vorhandene Controls behalten ihre Daten. Neue Zuordnungen beginnen als Entwurf."));
        var choices = new Dictionary<string, ComboBox>();
        foreach (var control in package.Controls)
        {
            var pick = Ui.Combo();
            if (state.ControlIds.TryGetValue(control.Key, out var existingId))
            {
                pick.ItemsSource = new[] { new Choice(existingId, "Bereits übernommen") };
                pick.SelectedIndex = 0;
                pick.IsEnabled = false;
            }
            else
            {
                pick.ItemsSource = candidates;
                pick.SelectedIndex = state.LinkedControls.Count == 0 ? 1 : 0;
            }
            choices.Add(control.Key, pick);
            fields.Children.Add(Ui.Field(control.Title, pick));
        }
        var measures = new CheckBox { Content = "Vorbereitete Maßnahmen als Entwürfe hinzufügen", IsChecked = true };
        fields.Children.Add(measures);
        var selected = new List<string>();
        var reuse = new Dictionary<string, string>();
        string? Validate()
        {
            selected.Clear();
            reuse.Clear();
            foreach (var (key, pick) in choices)
            {
                if (pick.SelectedItem is not Choice { Id.Length: > 0 } choice)
                    continue;
                selected.Add(key);
                if (choice.Id != "new" && !state.ControlIds.ContainsKey(key))
                    reuse.Add(key, choice.Id);
            }
            if (selected.Count == 0)
                return "Wählen Sie mindestens ein Control oder übernehmen Sie die Anforderung einzeln.";
            return null;
        }
        if (!await ShowReviewDialogAsync("Maßnahmenpaket übernehmen", fields, Validate))
            return;
        var result = await _workspace.Nis2ApplyPackageAsync(package.Key, scope, measures.IsChecked == true,
            reuseControlIds: reuse, controlKeys: selected);
        await Render();
        _status.Text = $"Paket übernommen · {result.ControlsCreated} Controls und {result.MeasuresCreated} Maßnahmen neu · {result.ControlsReused} wiederverwendet";
    }

    private async Task PickNis2ControlMappingAsync(Nis2Package package, string scope)
    {
        var controls = (await _workspace.EntriesAsync(kind: "Control", scope: scope))
            .Where(control => !control.IsSeed).ToArray();
        if (controls.Length == 0)
            throw new InvalidOperationException("In diesem Geltungsbereich sind noch keine nutzbaren Controls gespeichert.");
        var state = await _workspace.Nis2PackageStateAsync(package.Key, scope);
        if (state.RequirementId is null)
            throw new InvalidOperationException("Übernehmen Sie zuerst die Anforderung. Danach können Sie ein vorhandenes Control zuordnen.");
        var requirementId = state.RequirementId;
        var suggestions = (await _workspace.Nis2ControlSuggestionsAsync(requirementId, scope))
            .ToDictionary(suggestion => suggestion.Control.Id);
        var confirmed = _mappings.Where(link => link.Type == "Anforderung → Control" && link.From == requirementId &&
            link.Approval == "Bestätigt").Select(link => link.To).ToHashSet();
        var options = controls.Where(control => !confirmed.Contains(control.Id))
            .OrderByDescending(control => suggestions.ContainsKey(control.Id))
            .Select(control => new Choice(control.Id, suggestions.TryGetValue(control.Id, out var suggestion)
                ? $"{EntryLabel(control)} · Hinweis {string.Join(", ", suggestion.References.Select(reference => reference.Identifier))}"
                : EntryLabel(control))).ToArray();
        if (options.Length == 0)
            throw new InvalidOperationException("Alle verfügbaren Controls sind bereits bestätigt zugeordnet.");

        var pick = Ui.Combo();
        pick.ItemsSource = options;
        var coverage = Ui.Combo();
        coverage.ItemsSource = new[] { "Unterstützend", "Teilweise", "Vollständig" };
        coverage.SelectedIndex = 0;
        var reason = Ui.Input("Umsetzung, Abdeckungsgrenzen und benötigte Nachweise festhalten", true);
        var hint = Ui.Secondary("Bestätigen Sie die Abdeckung für den Bereich anhand Ihrer tatsächlichen Umsetzung.");
        pick.SelectionChanged += (_, _) =>
        {
            var id = (pick.SelectedItem as Choice)?.Id;
            var suggestion = id is null ? null : suggestions.GetValueOrDefault(id);
            coverage.SelectedItem = suggestion?.ProposedCoverage ?? "Unterstützend";
            reason.Text = suggestion?.Rationale ?? "";
            hint.Text = suggestion is null ? "Manuelle Zuordnung. Abdeckung und Abweichungen fachlich beurteilen." :
                string.Join(" · ", suggestion.References.Select(reference => $"{reference.Edition} {reference.Identifier}"));
        };
        if (!await ShowReviewDialogAsync("NIS2-Zuordnung bestätigen", Ui.Stack(
                Ui.Secondary($"{package.Reference} · {scope}"), Ui.Field("Control *", pick), hint,
                Ui.Field("Abdeckung *", coverage), Ui.Field("Begründung *", reason)),
            () => pick.SelectedItem is not Choice || string.IsNullOrWhiteSpace(reason.Text)
                ? "Control auswählen und die Abdeckung begründen." : null))
            return;

        var selected = controls.Single(control => control.Id == ((Choice)pick.SelectedItem!).Id);
        if (suggestions.TryGetValue(selected.Id, out var confirmedSuggestion))
            await _workspace.ConfirmNis2ControlSuggestionAsync(requirementId, selected.Id, (string)coverage.SelectedItem!,
                scope, reason.Text!, selected.Version, confirmedSuggestion.References);
        else
            await _workspace.ConfirmNis2ControlAsync(requirementId, selected.Id, (string)coverage.SelectedItem!,
                scope, reason.Text!, selected.Version);
        await Render();
        _status.Text = "Zuordnung bestätigt · das vorhandene Control und seine Nachweise werden wiederverwendet.";
    }

    private Task LinkRiskForNis2ControlAsync(string controlId) =>
        LinkRiskToControlAsync(_all.FirstOrDefault(entry => entry.Kind == "Control" && entry.Id == controlId)
            ?? throw new ArgumentException("Control fehlt."));
}
