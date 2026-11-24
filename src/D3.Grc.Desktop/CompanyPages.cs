using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using D3.Grc.Core;

namespace D3.Grc.Desktop;

public sealed partial class MainWindow
{
    private async Task CreateProjectFromSetupAsync()
    {
        if (!await CanLeaveFormAsync())
            return;

        var result = await ShowCompanySetupAsync(createProject: true, CompanyProfile.Default);
        if (result.Completed)
            await CompleteProjectSwitchAsync(result.CleanupPending);
    }

    private async Task OpenCompanyConfigurationAsync()
    {
        if (!_workspace.IsOpen || !await CanLeaveFormAsync())
            return;

        var profile = await _workspace.GetCompanyProfileAsync() ?? CompanyProfile.Default;
        if ((await ShowCompanySetupAsync(createProject: false, profile)).Completed)
            await Render();
    }

    private async Task<(bool Completed, bool CleanupPending)> ShowCompanySetupAsync(bool createProject, CompanyProfile profile)
    {
        var title = createProject ? "Unternehmen und Mandant einrichten" : "Unternehmensprofil konfigurieren";
        var window = new Window
        {
            Title = title,
            Width = 840,
            Height = 680,
            MinWidth = 660,
            MinHeight = 500,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Background = Ui.Canvas
        };
        var stepTitle = Ui.Heading("", 25, 1);
        var progress = Ui.Secondary("");
        var explanation = Ui.Secondary("", 14);
        var body = new ContentControl();
        var inlineError = Ui.Text("", 14, true);
        inlineError.Foreground = Avalonia.Media.Brush.Parse("#A42739");
        inlineError.IsVisible = false;

        var name = Ui.Input("z. B. Muster GmbH");
        name.Text = createProject ? "" : _workspace.Name;
        var size = CompanyChoice(CompanyPlanning.SizeLabels, profile.Size);
        var industry = CompanyChoice(CompanyPlanning.IndustryLabels, profile.Industry);
        var focus = CompanyChoice(CompanyPlanning.RiskFocusLabels, profile.RiskFocus);
        var siteCount = Ui.Input();
        siteCount.Text = profile.SiteCount.ToString(CultureInfo.CurrentCulture);
        var unitCount = Ui.Input();
        unitCount.Text = profile.OrganizationalUnitCount.ToString(CultureInfo.CurrentCulture);
        var companyFields = Ui.Stack(
            createProject ? Ui.Field("Unternehmen oder Projekt *", name) : Ui.Text(_workspace.Name, 19, true),
            CompanyColumns(
                Ui.Field("Unternehmensgröße", size, "Die Größe verändert den Umfang des empfohlenen Einstiegs."),
                Ui.Field("Unternehmensart / Branche", industry, "Passt die Vorschläge für anklickbare Merkmale und Themen an.")),
            CompanyColumns(
                Ui.Field("Standorte *", siteCount),
                Ui.Field("Organisationseinheiten *", unitCount)),
            Ui.Field("Risikoschwerpunkt", focus, "Bestimmt, welche fachlichen Aufgaben im Plan priorisiert werden."));

        var factors = CompanyPlanning.FactorLabels
            .Where(pair => pair.Key != CompanyFactors.None)
            .ToDictionary(pair => pair.Key, pair => CompanyCheckBox(pair.Value, profile.Factors.HasFlag(pair.Key)));
        var topicInputs = CompanyPlanning.TopicLabels.ToDictionary(pair => pair.Key,
            pair => CompanyCheckBox(pair.Value, profile.Topics.Contains(pair.Key, StringComparer.Ordinal)), StringComparer.Ordinal);
        var manuallySetFactors = new HashSet<CompanyFactors>();
        var manuallySetTopics = new HashSet<string>(StringComparer.Ordinal);
        var applyingSuggestions = false;
        var suggestionHint = Ui.Secondary("Vorgeschlagene Optionen können Sie anpassen.");
        foreach (var (factor, input) in factors)
            input.PropertyChanged += (_, args) =>
            {
                if (args.Property == ToggleButton.IsCheckedProperty && !applyingSuggestions)
                    manuallySetFactors.Add(factor);
            };
        foreach (var (topicId, input) in topicInputs)
            input.PropertyChanged += (_, args) =>
            {
                if (args.Property == ToggleButton.IsCheckedProperty && !applyingSuggestions)
                    manuallySetTopics.Add(topicId);
            };

        void RefreshSuggestions()
        {
            if (industry.SelectedItem is not CompanyOption<CompanyIndustry> company)
                return;
            var suggestion = CompanyPlanning.SuggestForIndustry(company.Value);
            applyingSuggestions = true;
            try
            {
                foreach (var (factor, input) in factors)
                {
                    var recommended = suggestion.Factors.HasFlag(factor);
                    input.Content = CompanyPlanning.FactorLabels[factor] + (recommended ? " · Vorschlag" : "");
                    if (createProject && !manuallySetFactors.Contains(factor))
                        input.IsChecked = recommended;
                }
                foreach (var (topicId, input) in topicInputs)
                {
                    var recommended = suggestion.Topics.Contains(topicId, StringComparer.Ordinal);
                    input.Content = CompanyPlanning.TopicLabels[topicId] + (recommended ? " · Vorschlag" : "");
                    if (createProject && !manuallySetTopics.Contains(topicId))
                        input.IsChecked = recommended;
                }
            }
            finally { applyingSuggestions = false; }
        }
        industry.SelectionChanged += (_, _) => RefreshSuggestions();
        RefreshSuggestions();

        var scopeFields = Ui.Stack(suggestionHint, CompanyColumns(
            Ui.Card(Ui.Stack([Ui.Text("Merkmale des Unternehmens", 17, true), .. factors.Values])),
            Ui.Card(Ui.Stack([Ui.Text("Fachthemen zur Prüfung", 17, true), .. topicInputs.Values]))));

        var plan = new ContentControl();
        var demo = new Button { Content = "Demo-Unternehmen öffnen", MinHeight = 40 };
        var back = new Button { Content = "Zurück", MinHeight = 40 };
        var cancel = new Button { Content = "Abbrechen", IsCancel = true, MinHeight = 40 };
        var next = new Button
        {
            Content = "Weiter",
            IsDefault = true,
            MinHeight = 40,
            Background = Ui.Green,
            Foreground = Avalonia.Media.Brushes.White
        };
        var footer = new DockPanel { LastChildFill = true, Margin = new Thickness(0, 12, 0, 0) };
        var actions = Ui.Row(demo, cancel, back, next);
        DockPanel.SetDock(actions, Dock.Right);
        footer.Children.Add(actions);
        footer.Children.Add(inlineError);
        var dialog = Ui.Card(Ui.Fill(Ui.Stack(progress, stepTitle, explanation), Ui.Scroll(body), footer));
        dialog.Margin = new Thickness(16);
        window.Content = dialog;

        var step = 0;
        var running = false;
        var pendingFileSave = false;
        var cleanupPending = false;

        void SetError(string message)
        {
            inlineError.Text = message;
            inlineError.IsVisible = message.Length > 0;
        }

        CompanyProfile ReadProfile() => CompanyPlanning.Validate(new CompanyProfile(
            ((CompanyOption<CompanySize>)size.SelectedItem!).Value,
            ((CompanyOption<CompanyIndustry>)industry.SelectedItem!).Value,
            factors.Where(pair => pair.Value.IsChecked == true)
                .Aggregate(CompanyFactors.None, (all, pair) => all | pair.Key),
            topicInputs.Where(pair => pair.Value.IsChecked == true).Select(pair => pair.Key).ToArray(),
            ReadCompanyCount(siteCount, "Standorte"),
            ReadCompanyCount(unitCount, "Organisationseinheiten"),
            ((CompanyOption<CompanyRiskFocus>)focus.SelectedItem!).Value));

        void SetStep(int index)
        {
            step = index;
            SetError("");
            progress.Text = $"Schritt {step + 1} von 3";
            back.IsEnabled = step > 0;
            next.Content = step == 2 ? (createProject ? "Mandant anlegen" : "Profil speichern") : "Weiter";
            demo.IsVisible = createProject && step == 0;
            switch (step)
            {
                case 0:
                    stepTitle.Text = title;
                    explanation.Text = "Unternehmensgröße, Branche und Organisation bestimmen den Umfang des Einstiegs.";
                    body.Content = companyFields;
                    break;
                case 1:
                    stepTitle.Text = "Themen und Merkmale";
                    explanation.Text = "Wählen Sie relevante Merkmale und Themen aus. Die fachliche Anwendbarkeit wird im Projekt geprüft.";
                    body.Content = scopeFields;
                    break;
                default:
                    stepTitle.Text = "Ihr Umsetzungsplan";
                    explanation.Text = createProject
                        ? "Prüfen Sie die vorgeschlagenen Aufgaben. Der Mandant wird verschlüsselt in Ihrem Profil gespeichert."
                        : "Prüfen und speichern Sie die Priorisierung. Bestehende Einträge und Themen bleiben verfügbar.";
                    plan.Content = CompanyPlanPreview(CompanyPlanning.Build(ReadProfile()));
                    body.Content = plan;
                    break;
            }
        }

        cancel.Click += (_, _) => window.Close(pendingFileSave);
        back.Click += (_, _) => SetStep(step - 1);
        window.Closing += (_, e) => e.Cancel = running;
        window.Opened += (_, _) =>
        {
            if (createProject)
                name.Focus();
            else
                size.Focus();
        };

        demo.Click += async (_, _) =>
        {
            if (running)
                return;
            running = true;
            dialog.IsEnabled = false;
            try
            {
                cleanupPending = await CreateManagedTenantAsync(DemoTenant.Name, DemoTenant.Profile, demo: true);
                running = false;
                window.Close(true);
            }
            catch (Exception ex)
            {
                SetError(ex.Message);
            }
            finally
            {
                running = false;
                dialog.IsEnabled = true;
            }
        };

        next.Click += async (_, _) =>
        {
            if (running)
                return;
            SetError("");
            try
            {
                if (step == 0)
                {
                    if (createProject && (string.IsNullOrWhiteSpace(name.Text) || name.Text.Length > 240))
                        throw new ArgumentException("Bitte einen Projektnamen mit bis zu 240 Zeichen eingeben.");
                    ReadProfile();
                    SetStep(1);
                    return;
                }
                if (step == 1)
                {
                    SetStep(2);
                    return;
                }

                running = true;
                dialog.IsEnabled = false;
                var completed = false;
                try
                {
                    if (pendingFileSave)
                    {
                        await _workspace.SaveAsync();
                    }
                    else if (createProject)
                    {
                        cleanupPending = await CreateManagedTenantAsync(name.Text!, ReadProfile());
                    }
                    else
                    {
                        await _workspace.SaveCompanyProfileAsync(ReadProfile());
                    }
                    completed = true;
                }
                finally
                {
                    running = false;
                    dialog.IsEnabled = true;
                    if (completed)
                        window.Close(true);
                }
            }
            catch (WorkspacePersistenceException ex)
            {
                pendingFileSave = true;
                next.Content = "Datei erneut speichern";
                back.IsEnabled = false;
                SetError(ex.Message);
            }
            catch (Exception ex)
            {
                SetError(ex.Message);
            }
        };

        SetStep(0);
        return (await window.ShowDialog<bool>(this) || pendingFileSave, cleanupPending);
    }

    private static Control CompanyPlanPreview(IReadOnlyList<CompanyModuleRecommendation> modules)
    {
        StackPanel ModuleRows(CompanyModulePriority priority)
        {
            var group = new StackPanel { Spacing = 12 };
            foreach (var module in modules.Where(item => item.Priority == priority))
                group.Children.Add(new StackPanel
                {
                    Spacing = 3,
                    Children = { Ui.Text(module.Title, 14, true), Ui.Secondary(module.Why) }
                });
            return group;
        }
        var key = Ui.Card(Ui.Stack(Ui.Heading("Wesentliche Aufgaben", 17), ModuleRows(CompanyModulePriority.Key)));
        var optionalCount = modules.Count(item => item.Priority == CompanyModulePriority.Optional);
        return optionalCount == 0 ? key : Ui.Stack(key, new Expander
        {
            Header = $"Optionale Ergänzungen ({optionalCount})",
            Content = ModuleRows(CompanyModulePriority.Optional),
            HorizontalAlignment = HorizontalAlignment.Stretch
        });
    }

    private static int ReadCompanyCount(TextBox input, string label)
    {
        if (!int.TryParse(input.Text, NumberStyles.None, CultureInfo.CurrentCulture, out var result) || result is < 1 or > 10000)
            throw new ArgumentException($"{label} als ganze Zahl von 1 bis 10.000 eingeben.");
        return result;
    }

    private sealed record CompanyOption<T>(T Value, string Label) where T : struct, Enum
    {
        public override string ToString() => Label;
    }

    private static ComboBox CompanyChoice<T>(IReadOnlyDictionary<T, string> labels, T selected) where T : struct, Enum
    {
        var choices = labels.Select(pair => new CompanyOption<T>(pair.Key, pair.Value)).ToArray();
        var box = Ui.Combo();
        box.ItemsSource = choices;
        box.SelectedItem = choices.Single(item => item.Value.Equals(selected));
        return box;
    }

    private static CheckBox CompanyCheckBox(string label, bool isChecked)
    {
        var checkBox = new CheckBox { Content = Ui.Text(label, 13), IsChecked = isChecked };
        Avalonia.Automation.AutomationProperties.SetName(checkBox, label);
        return checkBox;
    }

    private static Grid CompanyColumns(Control left, Control right)
    {
        var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("*,*") };
        left.Margin = new Thickness(0, 0, 12, 0);
        grid.Children.Add(left);
        Grid.SetColumn(right, 1);
        grid.Children.Add(right);
        return grid;
    }
}
