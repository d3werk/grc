using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Platform.Storage;
using D3.Grc.Core;

namespace D3.Grc.Desktop;

public sealed partial class MainWindow
{
    private async Task CreateProjectFromSetupAsync()
    {
        if (_workspace.IsOpen)
            throw new InvalidOperationException("Das aktuelle Projekt zuerst schließen.");

        if (!await ShowCompanySetupAsync(createProject: true, CompanyProfile.Default))
            return;

        ResetProjectContext();
        await Render();
    }

    private async Task OpenCompanyConfigurationAsync()
    {
        if (!_workspace.IsOpen || !await CanLeaveFormAsync())
            return;

        var profile = await _workspace.GetCompanyProfileAsync() ?? CompanyProfile.Default;
        if (await ShowCompanySetupAsync(createProject: false, profile))
            await Render();
    }

    private async Task<bool> ShowCompanySetupAsync(bool createProject, CompanyProfile profile)
    {
        var title = createProject ? "Unternehmen und Projekt einrichten" : "Unternehmensprofil konfigurieren";
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
                Ui.Field("Branche", industry, "Ergänzt die Vorschläge um branchentypische Schwerpunkte.")),
            CompanyColumns(
                Ui.Field("Standorte *", siteCount),
                Ui.Field("Organisationseinheiten *", unitCount)),
            Ui.Field("Risikoschwerpunkt", focus, "Bestimmt, welche fachlichen Aufgaben im Plan priorisiert werden."));

        var factors = CompanyPlanning.FactorLabels
            .Where(pair => pair.Key != CompanyFactors.None)
            .ToDictionary(pair => pair.Key, pair => CompanyCheckBox(pair.Value, profile.Factors.HasFlag(pair.Key)));
        var topicInputs = CompanyPlanning.TopicLabels.ToDictionary(pair => pair.Key,
            pair => CompanyCheckBox(pair.Value, profile.Topics.Contains(pair.Key, StringComparer.Ordinal)), StringComparer.Ordinal);
        var scopeFields = CompanyColumns(
            Ui.Card(Ui.Stack([Ui.Text("Merkmale des Unternehmens", 17, true), .. factors.Values])),
            Ui.Card(Ui.Stack([Ui.Text("Weitere Themen zur Prüfung", 17, true), .. topicInputs.Values])));

        var plan = new ContentControl();
        var password = Ui.Input("Mindestens 12 Zeichen");
        password.PasswordChar = '●';
        password.MaxLength = 1024;
        var confirmation = Ui.Input("Projektpasswort wiederholen");
        confirmation.PasswordChar = '●';
        confirmation.MaxLength = 1024;
        var planPage = createProject
            ? Ui.Stack(Ui.Card(CompanyColumns(
                Ui.Field("Projektpasswort *", password, "Schützt die verschlüsselte Projektdatei."),
                Ui.Field("Projektpasswort bestätigen *", confirmation))), plan)
            : Ui.Stack(plan);

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
        var actions = Ui.Row(cancel, back, next);
        DockPanel.SetDock(actions, Dock.Right);
        footer.Children.Add(actions);
        footer.Children.Add(inlineError);
        var dialog = Ui.Card(Ui.Fill(Ui.Stack(progress, stepTitle, explanation), Ui.Scroll(body), footer));
        dialog.Margin = new Thickness(16);
        window.Content = dialog;

        var step = 0;
        var running = false;
        var pendingFileSave = false;

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
            next.Content = step == 2 ? (createProject ? "Projektdatei anlegen" : "Profil speichern") : "Weiter";
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
                        ? "Prüfen Sie die vorgeschlagenen Aufgaben. Legen Sie anschließend die verschlüsselte Projektdatei an."
                        : "Prüfen und speichern Sie die Priorisierung. Bestehende Einträge und Themen bleiben verfügbar.";
                    plan.Content = CompanyPlanPreview(CompanyPlanning.Build(ReadProfile()));
                    body.Content = planPage;
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
                        if ((password.Text?.Length ?? 0) < 12)
                            throw new ArgumentException("Das Projektpasswort muss mindestens 12 Zeichen haben.");
                        if (password.Text != confirmation.Text)
                            throw new ArgumentException("Die Projektpasswörter stimmen nicht überein.");

                        var file = await window.StorageProvider.SaveFilePickerAsync(new Avalonia.Platform.Storage.FilePickerSaveOptions
                        {
                            Title = "Verschlüsselte Projektdatei anlegen",
                            SuggestedFileName = "organisation.d3grc",
                            DefaultExtension = "d3grc",
                            FileTypeChoices = [new("D3 GRC-Projekt") { Patterns = ["*.d3grc"] }]
                        });
                        if (file is null)
                            return;
                        var path = file.TryGetLocalPath()
                            ?? throw new IOException("Bitte einen lokalen Speicherort für das Projekt wählen.");
                        await _workspace.CreateAsync(path, name.Text!, password.Text!, companyProfile: ReadProfile());
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
        try { return await window.ShowDialog<bool>(this) || pendingFileSave; }
        finally
        {
            password.Text = "";
            confirmation.Text = "";
        }
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
