using System.Globalization;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using D3.Grc.Core;

namespace D3.Grc.Desktop;

public sealed partial class MainWindow
{
    private string? _riskToFocus;

    private async Task OpenRiskAssessmentAsync(string riskId)
    {
        if (!await CanLeaveFormAsync())
            return;
        _page = "Risk";
        _statusFilter = "";
        _riskToFocus = riskId;
        try
        {
            await Render();
            if (_riskToFocus is not null)
                _status.Text = "Das Risiko liegt außerhalb der aktuellen Registeransicht.";
        }
        finally
        {
            _riskToFocus = null;
        }
    }

    private sealed record RiskScaleOption(int Value, string Label)
    {
        public override string ToString() => $"{Value} · {Label}";
    }

    private static readonly RiskScaleOption[] LikelihoodChoices = RiskAssessmentMethod.LikelihoodLabels.OrderBy(pair => pair.Key)
        .Select(pair => new RiskScaleOption(pair.Key, pair.Value)).ToArray();
    private static readonly RiskScaleOption[] ImpactChoices = RiskAssessmentMethod.ImpactLabels.OrderBy(pair => pair.Key)
        .Select(pair => new RiskScaleOption(pair.Key, pair.Value)).ToArray();

    private static string RiskScoreDescription(int score) =>
        $"{score} von 25 · {RiskAssessmentMethod.LevelLabels[RiskAssessmentMethod.LevelForScore(score)]}";

    private static void PresentRiskAssessments(StackPanel history, Button add, Entry? risk,
        IReadOnlyList<RiskAssessment> assessments)
    {
        add.IsEnabled = risk is not null;
        history.Children.Clear();
        if (risk is null)
        {
            history.Children.Add(Ui.Notice("Wählen Sie ein gespeichertes Risiko aus, um eine Bewertung zu erfassen."));
            return;
        }

        if (assessments.Count == 0)
        {
            history.Children.Add(Ui.Notice("Dieses Risiko wurde noch nicht bewertet."));
            return;
        }
        if (assessments[0].RiskVersion != risk.Version)
            history.Children.Add(Ui.Notice("Die letzte Bewertung bezieht sich auf einen früheren Stand des Risikodatensatzes."));

        for (var index = 0; index < assessments.Count; index++)
        {
            var assessment = assessments[index];
            var day = assessment.AssessedOn.ToString("dd.MM.yyyy", CultureInfo.GetCultureInfo("de-DE"));
            var residual = assessment.ResidualScore is { } score
                ? $"{assessment.ResidualLikelihood} × {assessment.ResidualImpact} · {RiskScoreDescription(score)}"
                : "Offen";
            var row = Ui.Card(Ui.Stack(
                Ui.Heading(index == 0 ? $"Letzte Bewertung · {day}" : day, 16, 3),
                Ui.Secondary($"{assessment.Actor} · Risikodatensatz Version {assessment.RiskVersion}"),
                CompanyColumns(
                    Ui.Stack(Ui.Text("Ausgangsrisiko", 13, true),
                        Ui.Text($"{assessment.InherentLikelihood} × {assessment.InherentImpact} · {RiskScoreDescription(assessment.InherentScore)}")),
                    Ui.Stack(Ui.Text("Restrisiko", 13, true), Ui.Text(residual))),
                Ui.Text($"Behandlung: {RiskAssessmentMethod.ResponseLabels[assessment.Response]}", 13, true),
                Ui.Text(assessment.Rationale)));
            if (index == 0)
                row.Background = Ui.SoftGreen;
            history.Children.Add(row);
        }
    }

    private async Task<(bool Saved, WorkspacePersistenceException? Failure)> ShowRiskAssessmentAsync(Entry risk)
    {
        var window = new Window
        {
            Title = "Risikobewertung erfassen",
            Width = 760,
            Height = 820,
            MinWidth = 520,
            MinHeight = 520,
            WindowStartupLocation = WindowStartupLocation.CenterOwner
        };
        var date = Ui.Input("JJJJ-MM-TT");
        date.Text = DateOnly.FromDateTime(DateTime.Today).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        date.MaxLength = 10;
        var likelihood = Ui.Combo();
        likelihood.ItemsSource = LikelihoodChoices;
        var impact = Ui.Combo();
        impact.ItemsSource = ImpactChoices;
        var residualLikelihood = Ui.Combo();
        residualLikelihood.ItemsSource = LikelihoodChoices;
        var residualImpact = Ui.Combo();
        residualImpact.ItemsSource = ImpactChoices;
        var inherentSummary = Ui.Secondary("");
        var residualSummary = Ui.Secondary("");
        AutomationProperties.SetLiveSetting(inherentSummary, AutomationLiveSetting.Polite);
        AutomationProperties.SetLiveSetting(residualSummary, AutomationLiveSetting.Polite);
        var includeResidual = Ui.Help(new CheckBox { Content = "Restrisiko zusätzlich bewerten", IsChecked = false },
            "Aktiviert beide Skalen für die Bewertung nach berücksichtigten Kontrollen und Maßnahmen.");
        var residualFields = CompanyColumns(
            Ui.Field("Eintrittswahrscheinlichkeit", residualLikelihood,
                "Schätzen Sie die verbleibende Eintrittswahrscheinlichkeit nach den berücksichtigten Maßnahmen ein."),
            Ui.Field("Auswirkung", residualImpact,
                "Schätzen Sie die verbleibende Auswirkung nach den berücksichtigten Maßnahmen ein."));
        AutomationProperties.SetName(residualLikelihood, "Eintrittswahrscheinlichkeit nach Maßnahmen");
        AutomationProperties.SetName(residualImpact, "Auswirkung nach Maßnahmen");
        var response = Ui.Combo();
        response.ItemsSource = RiskAssessmentMethod.ResponseLabels.Values.ToList();
        response.SelectedItem = RiskAssessmentMethod.ResponseLabels[RiskResponse.Undecided];
        var rationale = Ui.Input("Szenario, Bewertungsgrundlage und berücksichtigte Maßnahmen", true);
        rationale.MaxLength = 30000;

        var fields = Ui.Stack(
            CompanyColumns(
                Ui.Field("Bewertungsdatum *", date, "Datum der tatsächlichen Bewertung im Format JJJJ-MM-TT, bis einschließlich heute."),
                Ui.Field("Risikobehandlung", response,
                    "Hält die fachliche Entscheidung fest, ob das Risiko etwa reduziert oder akzeptiert werden soll.")),
            Ui.Card(Ui.Stack(
                Ui.Heading("Ausgangsrisiko", 17, 2),
                Ui.Secondary("Einschätzung vor Berücksichtigung von Kontrollen und Maßnahmen."),
                CompanyColumns(
                    Ui.Field("Eintrittswahrscheinlichkeit *", likelihood,
                        "Wählen Sie eine Stufe von 1 für sehr unwahrscheinlich bis 5 für sehr wahrscheinlich."),
                    Ui.Field("Auswirkung *", impact,
                        "Wählen Sie eine Stufe von 1 für sehr geringe bis 5 für sehr hohe Auswirkungen.")),
                inherentSummary)),
            Ui.Card(Ui.Stack(
                Ui.Heading("Restrisiko", 17, 2),
                Ui.Secondary("Einschätzung nach Berücksichtigung von Kontrollen und Maßnahmen."),
                includeResidual, residualFields, residualSummary)),
            Ui.Field("Begründung *", rationale,
                "Begründen Sie beide Einschätzungen und die gewählte Behandlung. Nennen Sie Annahmen, Kontrollen und deren erwartete Wirkung."));

        void UpdateScore()
        {
            inherentSummary.Text = likelihood.SelectedItem is RiskScaleOption beforeLikelihood &&
                impact.SelectedItem is RiskScaleOption beforeImpact
                ? $"{beforeLikelihood.Value} × {beforeImpact.Value} · {RiskScoreDescription(beforeLikelihood.Value * beforeImpact.Value)}"
                : "Wählen Sie die Eintrittswahrscheinlichkeit und Auswirkung.";
            var withResidual = includeResidual.IsChecked == true;
            residualFields.IsEnabled = withResidual;
            residualSummary.Text = !withResidual
                ? "Restrisiko bleibt offen."
                : residualLikelihood.SelectedItem is RiskScaleOption afterLikelihood &&
                    residualImpact.SelectedItem is RiskScaleOption afterImpact
                    ? $"{afterLikelihood.Value} × {afterImpact.Value} · {RiskScoreDescription(afterLikelihood.Value * afterImpact.Value)}"
                    : "Wählen Sie beide Werte für das Restrisiko.";
        }
        likelihood.SelectionChanged += (_, _) => UpdateScore();
        impact.SelectionChanged += (_, _) => UpdateScore();
        residualLikelihood.SelectionChanged += (_, _) => UpdateScore();
        residualImpact.SelectionChanged += (_, _) => UpdateScore();
        includeResidual.IsCheckedChanged += (_, _) => UpdateScore();
        UpdateScore();

        var error = Ui.Text("", 13, true);
        error.Foreground = Brush.Parse("#9D242A");
        error.IsVisible = false;
        var cancel = new Button { Content = "Abbrechen", IsCancel = true, MinHeight = 38 };
        var save = new Button
        {
            Content = "Bewertung speichern",
            IsDefault = true,
            MinHeight = 38,
            Background = Ui.Green,
            Foreground = Brushes.White
        };
        var footer = Ui.Stack(error, new StackPanel { HorizontalAlignment = HorizontalAlignment.Right, Children = { Ui.Row(cancel, save) } });
        window.Content = new Border
        {
            Padding = new Thickness(24),
            Child = Ui.Fill(Ui.Stack(Ui.Heading("Risikobewertung erfassen", 23, 1), Ui.Secondary($"{risk.Title} · Registerstatus {risk.Status}")),
                Ui.Scroll(fields), footer)
        };

        var saving = false;
        WorkspacePersistenceException? persistenceFailure = null;
        window.Closing += (_, e) => e.Cancel = saving;
        window.Opened += (_, _) => date.Focus();
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
                if (!DateOnly.TryParseExact(date.Text?.Trim(), "yyyy-MM-dd", CultureInfo.InvariantCulture,
                        DateTimeStyles.None, out var assessedOn))
                    throw new ArgumentException("Bewertungsdatum im Format JJJJ-MM-TT eingeben.");
                if (likelihood.SelectedItem is not RiskScaleOption beforeLikelihood ||
                    impact.SelectedItem is not RiskScaleOption beforeImpact)
                    throw new ArgumentException("Eintrittswahrscheinlichkeit und Auswirkung des Ausgangsrisikos auswählen.");
                var afterLikelihood = includeResidual.IsChecked == true
                    ? (residualLikelihood.SelectedItem as RiskScaleOption)?.Value : null;
                var afterImpact = includeResidual.IsChecked == true
                    ? (residualImpact.SelectedItem as RiskScaleOption)?.Value : null;
                if (includeResidual.IsChecked == true && (afterLikelihood is null || afterImpact is null))
                    throw new ArgumentException("Beide Werte für das Restrisiko auswählen.");
                var treatment = RiskAssessmentMethod.ResponseLabels.Single(option => Equals(option.Value, response.SelectedItem)).Key;
                await _workspace.RecordRiskAssessmentAsync(risk.Id,
                    new RiskAssessmentInput(assessedOn, beforeLikelihood.Value, beforeImpact.Value, afterLikelihood, afterImpact,
                        treatment, rationale.Text ?? ""), risk.Version);
                committed = true;
            }
            catch (WorkspacePersistenceException ex)
            {
                committed = true;
                persistenceFailure = ex;
            }
            catch (Exception ex)
            {
                error.Text = ex.Message;
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
        return (await window.ShowDialog<bool>(this), persistenceFailure);
    }
}
