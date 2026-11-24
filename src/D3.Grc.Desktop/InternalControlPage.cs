using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Media;
using D3.Grc.Core;

namespace D3.Grc.Desktop;

public sealed partial class MainWindow
{
    private sealed record IksOption<T>(T Value, string Label) where T : struct, Enum
    {
        public override string ToString() => Label;
    }

    private static readonly IksOption<IksControlType>[] IksControlTypes =
    [new(IksControlType.Preventive, "Präventiv"), new(IksControlType.Detective, "Aufdeckend"), new(IksControlType.Corrective, "Korrigierend")];
    private static readonly IksOption<IksControlMode>[] IksControlModes =
    [new(IksControlMode.Manual, "Manuell"), new(IksControlMode.Automated, "Automatisiert"), new(IksControlMode.Hybrid, "Kombiniert")];
    private static readonly IksOption<IksFrequency>[] IksFrequencies =
    [new(IksFrequency.EventBased, "Ereignisbezogen"), new(IksFrequency.Daily, "Täglich"), new(IksFrequency.Weekly, "Wöchentlich"),
        new(IksFrequency.Monthly, "Monatlich"), new(IksFrequency.Quarterly, "Vierteljährlich"),
        new(IksFrequency.HalfYearly, "Halbjährlich"), new(IksFrequency.Yearly, "Jährlich")];
    private static readonly IksOption<IksCheckType>[] IksCheckTypes =
    [new(IksCheckType.Execution, "Durchführung"), new(IksCheckType.DesignAssessment, "Selbstbeurteilung der Ausgestaltung"),
        new(IksCheckType.SelfAssessment, "Selbstbeurteilung der Wirksamkeit"), new(IksCheckType.IndependentTest, "Unabhängiger Wirksamkeitstest")];
    private static readonly IksOption<IksCheckOutcome>[] IksOutcomes =
    [new(IksCheckOutcome.Confirmed, "Erfüllt"), new(IksCheckOutcome.Deviation, "Abweichung"), new(IksCheckOutcome.NotApplicable, "Nicht anwendbar")];

    private async Task<Control> InternalControlPage()
    {
        var controls = _entries.Where(entry => entry.Kind == "Control").ToArray();
        var ids = controls.Select(entry => entry.Id).ToArray();
        var profiles = await _workspace.IksProfilesAsync(ids);
        var records = _all.ToDictionary(entry => entry.Id, StringComparer.Ordinal);
        var risks = _mappings.Where(mapping => mapping.Type == "Risiko → Control").ToLookup(mapping => mapping.To);
        var today = DateOnly.FromDateTime(DateTime.Today);

        var listHeading = Ui.Heading("Kontrollen", 18, 2);
        var count = Ui.Secondary("");
        AutomationProperties.SetLiveSetting(count, AutomationLiveSetting.Polite);
        var filter = Ui.Combo();
        filter.ItemsSource = new[] { "Alle Kontrollen", "Schlüsselkontrollen", "Ohne IKS-Profil", "Überfälliger Termin" };
        filter.SelectedIndex = 0;
        var list = new ListBox
        {
            ItemTemplate = new FuncDataTemplate<Entry>((entry, _) =>
            {
                if (entry is null)
                    return null;
                var profile = profiles.GetValueOrDefault(entry.Id);
                var designation = profile is null ? "Ohne IKS-Profil" : profile.IsKeyControl ? "Schlüsselkontrolle" : "IKS-Profil erfasst";
                return Ui.Stack(Ui.Text(AuditIksTitle(entry), 14, true),
                    Ui.Secondary($"{designation} · {entry.Status} · {risks[entry.Id].Count()} Risikobezüge"));
            })
        };
        AutomationProperties.SetLabeledBy(list, listHeading);
        var detail = new ContentControl();
        var selected = controls.FirstOrDefault();

        async Task LoadSelectedAsync(Entry? entry, bool selectHistory = false)
        {
            selected = entry;
            if (entry is null)
            {
                detail.Content = Ui.Notice("Wählen Sie eine Kontrolle aus dem aktuellen Filter.");
                return;
            }

            var profile = profiles.GetValueOrDefault(entry.Id);
            var checks = await _workspace.IksChecksAsync(entry.Id);
            var proofNames = (await _workspace.EvidenceAsync(entry.Id)).ToDictionary(proof => proof.Id, StringComparer.Ordinal);
            var edit = Button(profile is null ? "IKS-Profil ergänzen" : "IKS-Profil bearbeiten", async () =>
            {
                if (!await ShowIksProfileAsync(entry, profile))
                    return;
                profiles = await _workspace.IksProfilesAsync(ids);
                await RefreshAsync(entry.Id);
            }, primary: profile is null, glyph: Glyph.Settings);
            var record = Button("Prüfung erfassen", async () =>
            {
                if (await ShowIksCheckAsync(entry, proofNames.Values))
                    await LoadSelectedAsync(entry, selectHistory: true);
            }, primary: true, glyph: Glyph.Plus);
            var openControl = Button("Control-Stammdaten öffnen", () => OpenModuleEntryAsync("Control", entry.Id), glyph: Glyph.Arrow);
            var design = new List<Control>
            {
                Ui.Heading("Kontrollprofil", 17, 2), Ui.Secondary($"{entry.Status} · {(entry.Scope.Length == 0 ? "Ohne Geltungsbereich" : entry.Scope)}"),
                Ui.Text($"Verantwortung: {(entry.Owner.Length == 0 ? "nicht festgelegt" : entry.Owner)} · Termin: {AuditDateLabel(entry)}")
            };
            if (profile is not null)
            {
                design.Add(Ui.Text(profile.Objective));
                design.Add(Ui.Secondary($"{IksLabel(IksControlTypes, profile.Type)} · {IksLabel(IksControlModes, profile.Mode)} · " +
                    IksLabel(IksFrequencies, profile.Frequency)));
                if (profile.IsKeyControl)
                    design.Add(Ui.IconLabel(Glyph.Shield, "Schlüsselkontrolle", Ui.Green));
            }
            design.Add(Ui.Row(edit, openControl));
            var matrix = Ui.Stack(Ui.Heading("Risiko-Kontroll-Bezüge", 17, 2));
            foreach (var mapping in risks[entry.Id])
            {
                if (records.GetValueOrDefault(mapping.From) is not { } risk)
                    continue;
                matrix.Children.Add(Ui.Stack(Button(AuditIksTitle(risk), () => OpenModuleEntryAsync("Risk", risk.Id), glyph: Glyph.Risk),
                    Ui.Secondary($"{mapping.Approval} · {mapping.Coverage}"), Ui.Text(mapping.Reason)));
            }
            if (matrix.Children.Count == 1)
                matrix.Children.Add(Ui.Secondary("Für dieses Control besteht noch kein Risiko-Bezug."));
            matrix.Children.Add(Button("Zuordnungen verwalten", () => OpenModuleEntryAsync("Mappings"), glyph: Glyph.Mapping));

            var history = Ui.Stack(Ui.Heading("Durchführungen und Prüfungen", 17, 2), record);
            if (checks.Count == 0)
                history.Children.Add(Ui.Secondary("Zu dieser Kontrolle wurde noch keine Durchführung oder Prüfung erfasst."));
            foreach (var check in checks)
            {
                var data = check.Result;
                var tone = data.Outcome == IksCheckOutcome.Deviation ? Ui.SoftAmber : Ui.SoftGreen;
                var checkBody = Ui.Stack(Ui.Heading(IksLabel(IksCheckTypes, data.Type), 16, 3),
                    Ui.Text($"{IksLabel(IksOutcomes, data.Outcome)} · {data.CheckedOn:dd.MM.yyyy}", 13, true),
                    Ui.Secondary($"{data.Reviewer} · Control-Fassung {check.ControlVersion}"),
                    Ui.Text("Prüfansatz und Stichprobe", 13, true), Ui.Text(data.TestApproach),
                    Ui.Text("Beobachtung und Begründung", 13, true), Ui.Text(data.Notes));
                if (data.EvidenceId is { } evidenceId && proofNames.GetValueOrDefault(evidenceId) is { } proof)
                    checkBody.Children.Add(Button($"Nachweis anzeigen · {proof.Title}", () => PreviewEvidenceAsync(proof), glyph: Glyph.Evidence));
                if (data.MeasureId is { } measureId && records.GetValueOrDefault(measureId) is { } measure)
                    checkBody.Children.Add(Button($"Maßnahme öffnen · {measure.Title}", () => OpenModuleEntryAsync("Measure", measure.Id), glyph: Glyph.Tasks));
                var card = Ui.Card(checkBody);
                card.Background = tone;
                history.Children.Add(card);
            }
            var tabs = new TabControl
            {
                ItemsSource = new[] { Ui.Tab("Kontrollprofil", Ui.Scroll(Ui.Stack(design.ToArray())), glyph: Glyph.Shield),
                    Ui.Tab("Risiko-Zuordnung", Ui.Scroll(matrix), glyph: Glyph.Mapping),
                    Ui.Tab("Prüfverlauf", Ui.Scroll(history), glyph: Glyph.Audit) }
            };
            tabs.SelectedIndex = selectHistory ? 2 : 0;
            detail.Content = Ui.Fill(Ui.Heading(AuditIksTitle(entry), 20, 2), tabs);
        }

        async Task RefreshAsync(string? preferredId = null)
        {
            var visible = controls.Where(entry => filter.SelectedIndex switch
            {
                1 => profiles.GetValueOrDefault(entry.Id)?.IsKeyControl == true,
                2 => !profiles.ContainsKey(entry.Id),
                3 => AuditDate(entry) < today,
                _ => true
            }).ToArray();
            count.Text = $"{visible.Length} von {controls.Length} Kontrollen · {profiles.Values.Count(profile => profile.IsKeyControl)} Schlüsselkontrollen insgesamt";
            list.ItemsSource = visible;
            list.SelectedItem = visible.FirstOrDefault(entry => entry.Id == preferredId) ?? visible.FirstOrDefault();
            await LoadSelectedAsync(list.SelectedItem as Entry);
        }

        list.SelectionChanged += async (_, _) =>
        {
            if (!_loading && !_busy && !ReferenceEquals(selected, list.SelectedItem))
                await Run(() => LoadSelectedAsync(list.SelectedItem as Entry));
        };
        filter.SelectionChanged += async (_, _) =>
        {
            if (!_loading && !_busy)
                await Run(() => RefreshAsync(selected?.Id));
        };
        await RefreshAsync();
        var left = Ui.Card(Ui.Fill(Ui.Stack(listHeading, Ui.Field("Auswahl", filter), count), list,
            Button("Controls verwalten", () => OpenModuleEntryAsync("Control"), glyph: Glyph.Shield)));
        return Ui.Fill(Ui.Stack(Ui.Heading("Internes Kontrollsystem", 24, 1),
                Ui.Secondary("Kontrollprofile, Risiko-Zuordnungen und tatsächliche Durchführungen im gewählten Geltungsbereich.")),
            Ui.Split(left, Ui.Card(detail)));
    }

    private static string IksLabel<T>(IksOption<T>[] choices, T value) where T : struct, Enum =>
        choices.First(choice => EqualityComparer<T>.Default.Equals(choice.Value, value)).Label;

    private static ComboBox IksCombo<T>(IksOption<T>[] options, T? initial = null) where T : struct, Enum
    {
        var input = Ui.Combo();
        input.ItemsSource = options;
        input.SelectedItem = initial is { } value ? options.First(option => EqualityComparer<T>.Default.Equals(option.Value, value)) : null;
        return input;
    }

    private static T IksValue<T>(ComboBox input, string message) where T : struct, Enum =>
        input.SelectedItem is IksOption<T> option ? option.Value : throw new ArgumentException(message);

    private Task<bool> ShowIksProfileAsync(Entry control, IksProfile? profile)
    {
        var objective = Ui.Input("Welches Ergebnis soll die Kontrolle sicherstellen?", true);
        objective.MaxLength = 4000;
        objective.Text = profile?.Objective ?? "";
        var type = IksCombo(IksControlTypes, profile?.Type);
        var mode = IksCombo(IksControlModes, profile?.Mode);
        var frequency = IksCombo(IksFrequencies, profile?.Frequency);
        var keyControl = new CheckBox { Content = "Schlüsselkontrolle", IsChecked = profile?.IsKeyControl == true };
        var fields = Ui.Stack(Ui.Field("Kontrollziel *", objective),
            CompanyColumns(Ui.Field("Kontrollart *", type), Ui.Field("Ausführung *", mode)),
            Ui.Field("Turnus *", frequency), keyControl);
        return IksEditDialogAsync("IKS-Profil bearbeiten", control, fields, objective, () => _workspace.SaveIksProfileAsync(control.Id,
            new IksProfile(objective.Text ?? "", IksValue<IksControlType>(type, "Kontrollart auswählen."),
                IksValue<IksControlMode>(mode, "Ausführung auswählen."), IksValue<IksFrequency>(frequency, "Turnus auswählen."), keyControl.IsChecked == true)));
    }

    private Task<bool> ShowIksCheckAsync(Entry control, IEnumerable<Proof> proofs)
    {
        var date = new DatePicker { SelectedDate = DateTimeOffset.Now, HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch };
        var reviewer = Ui.Input("Name oder verantwortliche Rolle");
        reviewer.MaxLength = 240;
        var type = IksCombo(IksCheckTypes);
        var outcome = IksCombo(IksOutcomes);
        var method = Ui.Input("Prüfhandlung, Kriterien und Stichprobe", true);
        method.MaxLength = 4000;
        var notes = Ui.Input("Tatsächliche Beobachtung und Begründung", true);
        notes.MaxLength = 30_000;
        var evidence = Ui.Combo();
        evidence.ItemsSource = new object[] { "Kein Nachweis ausgewählt" }.Concat(proofs).ToArray();
        evidence.SelectedIndex = 0;
        var measure = Ui.Combo();
        measure.ItemsSource = new object[] { "Keine Maßnahme ausgewählt" }.Concat(_all.Where(entry => entry.Kind == "Measure" && entry.Scope == control.Scope)).ToArray();
        measure.SelectedIndex = 0;
        var fields = Ui.Stack(CompanyColumns(Ui.Field("Prüfdatum *", date), Ui.Field("Geprüft von *", reviewer)),
            CompanyColumns(Ui.Field("Prüfart *", type), Ui.Field("Ergebnis *", outcome)), Ui.Field("Prüfansatz *", method),
            Ui.Field("Beobachtung und Begründung *", notes), Ui.Field("Nachweis", evidence,
                "Für eine bestätigte Durchführung und einen bestätigten unabhängigen Test muss ein am Prüftag gültiger Nachweis ausgewählt sein."),
            Ui.Field("Korrekturmaßnahme", measure));
        return IksEditDialogAsync("Kontrollprüfung erfassen", control, fields, reviewer, () => _workspace.RecordIksCheckAsync(control.Id,
            new IksCheckInput(date.SelectedDate is { } selected ? DateOnly.FromDateTime(selected.Date) : DateOnly.MinValue,
                reviewer.Text ?? "", IksValue<IksCheckType>(type, "Prüfart auswählen."), IksValue<IksCheckOutcome>(outcome, "Ergebnis auswählen."),
                method.Text ?? "", notes.Text ?? "", (evidence.SelectedItem as Proof)?.Id, (measure.SelectedItem as Entry)?.Id)));
    }

    private async Task<bool> IksEditDialogAsync(string title, Entry control, Control fields, Control focus, Func<Task> write)
    {
        var window = new Window { Title = title, Width = 700, Height = 690, MinHeight = 450, MinWidth = 470,
            WindowStartupLocation = WindowStartupLocation.CenterOwner };
        var error = Ui.Text("", 13, true);
        error.Foreground = Ui.Amber;
        error.IsVisible = false;
        var cancel = new Button { Content = "Abbrechen", IsCancel = true, MinHeight = 38 };
        var save = new Button { Content = "Speichern", IsDefault = true, MinHeight = 38, Background = Ui.Green, Foreground = Brushes.White };
        var saving = false;
        WorkspacePersistenceException? persistenceFailure = null;
        window.Closing += (_, eventArgs) => eventArgs.Cancel = saving;
        window.Opened += (_, _) => focus.Focus();
        cancel.Click += (_, _) => window.Close(false);
        save.Click += async (_, _) =>
        {
            if (saving)
                return;
            saving = true;
            fields.IsEnabled = save.IsEnabled = cancel.IsEnabled = false;
            error.IsVisible = false;
            var committed = false;
            try
            {
                await write();
                committed = true;
            }
            catch (WorkspacePersistenceException exception)
            {
                persistenceFailure = exception;
                committed = true;
            }
            catch (Exception exception)
            {
                error.Text = exception.Message;
                error.IsVisible = true;
            }
            finally
            {
                saving = false;
                fields.IsEnabled = save.IsEnabled = cancel.IsEnabled = true;
            }
            if (committed)
                window.Close(true);
        };
        window.Content = new Border { Padding = new Thickness(24), Child = Ui.Fill(
            Ui.Stack(Ui.Heading(title, 23, 1), Ui.Secondary(AuditIksTitle(control))), Ui.Scroll(fields), Ui.Stack(error, Ui.Row(cancel, save))) };
        var result = await window.ShowDialog<bool>(this);
        if (persistenceFailure is not null)
            throw persistenceFailure;
        return result;
    }
}
