using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using D3.Grc.Core;

namespace D3.Grc.Desktop;

public sealed partial class MainWindow
{
    private bool _showResidualRiskMatrix;

    private sealed record RiskMatrixRow(Entry Risk, RiskAssessment? Assessment)
    {
        public string Title => Risk.Code.Length == 0 ? Risk.Title : $"{Risk.Code} · {Risk.Title}";

        public (int Likelihood, int Impact)? Position(bool residual)
        {
            if (Assessment is null)
                return null;
            if (!residual)
                return (Assessment.InherentLikelihood, Assessment.InherentImpact);
            return Assessment.ResidualLikelihood is { } likelihood && Assessment.ResidualImpact is { } impact
                ? (likelihood, impact)
                : null;
        }
    }

    private static readonly IReadOnlyDictionary<RiskLevel, (IBrush Background, IBrush Text)> RiskTones =
        new Dictionary<RiskLevel, (IBrush, IBrush)>
        {
            [RiskLevel.Low] = (Ui.SoftGreen, Ui.Green),
            [RiskLevel.Medium] = (Ui.SoftBlue, Ui.Blue),
            [RiskLevel.High] = (Ui.SoftAmber, Ui.Amber),
            [RiskLevel.Critical] = (Brush.Parse("#FBE1E5"), Brush.Parse("#841E38"))
        };

    private async Task<Control> RiskMatrixPage()
    {
        var risks = _entries.Where(entry => entry.Kind == "Risk").ToArray();
        var assessments = await _workspace.LatestRiskAssessmentsAsync(risks.Select(risk => risk.Id).ToArray());
        var portfolio = risks.Select(risk => new RiskMatrixRow(risk, assessments.GetValueOrDefault(risk.Id))).ToArray();
        var withoutAssessment = portfolio.Where(row => row.Assessment is null).ToArray();
        var withoutResidual = portfolio.Where(row => row.Assessment is { ResidualScore: null }).ToArray();

        var mode = Ui.Combo();
        mode.ItemsSource = new[] { "Ausgangsrisiko", "Restrisiko" };
        mode.SelectedIndex = _showResidualRiskMatrix ? 1 : 0;
        bool Residual() => mode.SelectedIndex == 1;
        int? Score(RiskMatrixRow row) => Residual() ? row.Assessment?.ResidualScore : row.Assessment?.InherentScore;

        var resultLabel = Ui.Heading("Risiken im Filter", 17, 2);
        var resultCount = Ui.Secondary("");
        AutomationProperties.SetLiveSetting(resultCount, AutomationLiveSetting.Polite);
        var list = new ListBox
        {
            ItemTemplate = new FuncDataTemplate<RiskMatrixRow>((row, _) => row is null ? null : Ui.Stack(
                Ui.Text(row.Title, 14, true), Ui.Secondary(Describe(row))))
        };
        AutomationProperties.SetLabeledBy(list, resultLabel);
        var empty = Ui.Secondary("");
        empty.Margin = new Thickness(0, 8);
        empty.VerticalAlignment = VerticalAlignment.Top;
        var results = new Grid { Children = { list, empty } };
        var open = Button("Bewertung öffnen", async () =>
        {
            if (list.SelectedItem is RiskMatrixRow row)
                await OpenRiskAssessmentAsync(row.Risk.Id);
        }, help: "Öffnet das ausgewählte Risiko im Reiter Bewertungen.", glyph: Glyph.Arrow);
        open.IsEnabled = false;
        list.SelectionChanged += (_, _) => open.IsEnabled = list.SelectedItem is RiskMatrixRow;

        string Describe(RiskMatrixRow row)
        {
            if (row.Assessment is null)
                return "Ohne Bewertung";
            var level = Residual() ? row.Assessment.ResidualLevel : row.Assessment.InherentLevel;
            var baseLabel = level is { } band
                ? $"{RiskAssessmentMethod.LevelLabels[band]} · {Score(row)} Punkte"
                : "Ohne Restrisiko";
            var context = row.Assessment.RiskVersion == row.Risk.Version ? "" : " · Früherer Risikostand";
            return $"{baseLabel} · {row.Assessment.AssessedOn:dd.MM.yyyy}{context}";
        }

        Button? activeCell = null;
        void Show(string label, IEnumerable<RiskMatrixRow> rows, Button? cell = null)
        {
            if (activeCell is not null)
            {
                activeCell.BorderThickness = new Thickness(1);
                activeCell.BorderBrush = Ui.Line;
            }
            activeCell = cell;
            if (activeCell is not null)
            {
                activeCell.BorderThickness = new Thickness(2);
                activeCell.BorderBrush = Ui.Ink;
            }
            var visible = rows.OrderByDescending(Score)
                .ThenBy(row => row.Title, StringComparer.CurrentCultureIgnoreCase)
                .ThenBy(row => row.Risk.Id, StringComparer.Ordinal).ToArray();
            resultLabel.Text = label;
            resultCount.Text = $"{visible.Length} von {portfolio.Length} Risiken im aktuellen Filter";
            list.ItemsSource = visible;
            list.SelectedItem = null;
            list.IsVisible = visible.Length > 0;
            empty.IsVisible = visible.Length == 0;
            empty.Text = portfolio.Length == 0
                ? "Für Thema, Geltungsbereich und Suche sind keine Risiken vorhanden."
                : "Für diese Auswahl sind keine Risiken vorhanden.";
            open.IsEnabled = false;
        }

        Button Select(string label, string resultTitle, RiskMatrixRow[] rows, string help) =>
            Button(label, () =>
            {
                Show(resultTitle, rows);
                return Task.CompletedTask;
            }, help: help);

        var showAll = Select($"Alle · {portfolio.Length}", "Risiken im Filter", portfolio,
            "Zeigt alle Risiken im aktuellen Thema, Geltungsbereich und Suchfilter.");
        var showUnassessed = Select($"Ohne Bewertung · {withoutAssessment.Length}", "Risiken ohne Bewertung", withoutAssessment,
            "Zeigt Risiken ohne erfasste Bewertung.");
        var showMissingResidual = Select($"Ohne Restrisiko · {withoutResidual.Length}", "Bewertungen ohne Restrisiko", withoutResidual,
            "Zeigt Risiken mit Ausgangsbewertung und fehlenden Angaben zum Restrisiko.");

        var matrix = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("92,*,*,*,*,*"),
            RowDefinitions = new RowDefinitions("Auto,Auto,Auto,Auto,Auto,Auto"),
            ColumnSpacing = 5,
            RowSpacing = 5
        };
        KeyboardNavigation.SetTabNavigation(matrix, KeyboardNavigationMode.Once);
        var cells = new (Button Button, TextBlock Count, string Description)[5, 5];
        var groups = portfolio.ToLookup(row => row.Position(Residual()));

        void Place(Control control, int row, int column)
        {
            Grid.SetRow(control, row);
            Grid.SetColumn(control, column);
            matrix.Children.Add(control);
        }

        var axis = Ui.Secondary("Auswirkung", 12);
        axis.VerticalAlignment = VerticalAlignment.Center;
        Place(axis, 0, 0);
        for (var index = 1; index <= 5; index++)
        {
            var label = Ui.Text(index.ToString(), 13, true);
            label.HorizontalAlignment = HorizontalAlignment.Center;
            ToolTip.SetTip(label, RiskAssessmentMethod.LikelihoodLabels[index]);
            Place(label, 0, index);
        }

        for (var row = 0; row < 5; row++)
        {
            var impact = 5 - row;
            var impactLabel = Ui.Secondary($"{impact} {RiskAssessmentMethod.ImpactLabels[impact]}", 12);
            impactLabel.VerticalAlignment = VerticalAlignment.Center;
            Place(impactLabel, row + 1, 0);
            for (var column = 0; column < 5; column++)
            {
                var likelihood = column + 1;
                var score = likelihood * impact;
                var level = RiskAssessmentMethod.LevelForScore(score);
                var levelLabel = RiskAssessmentMethod.LevelLabels[level];
                var tone = RiskTones[level];
                var count = Ui.Text("", 17, true);
                count.HorizontalAlignment = HorizontalAlignment.Center;
                var band = Ui.Text(levelLabel, 11, true);
                band.Foreground = tone.Text;
                band.HorizontalAlignment = HorizontalAlignment.Center;
                var content = Ui.Stack(count, band);
                content.Spacing = 1;
                var cell = new Button
                {
                    Content = content,
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    HorizontalContentAlignment = HorizontalAlignment.Center,
                    VerticalContentAlignment = VerticalAlignment.Center,
                    MinHeight = 50,
                    Padding = new Thickness(3),
                    CornerRadius = new CornerRadius(6),
                    BorderThickness = new Thickness(1),
                    BorderBrush = Ui.Line,
                    Background = tone.Background
                };
                var description = $"Eintrittswahrscheinlichkeit {likelihood} ({RiskAssessmentMethod.LikelihoodLabels[likelihood]}), " +
                    $"Auswirkung {impact} ({RiskAssessmentMethod.ImpactLabels[impact]}), {score} Punkte, {levelLabel}";
                Ui.Help(cell, $"{description}. Wählen Sie die Zelle, um die zugeordneten Risiken anzuzeigen.");
                cell.Click += (_, _) => Show($"Wahrscheinlichkeit {likelihood} · Auswirkung {impact}", groups[(likelihood, impact)], cell);
                var (cellRow, cellColumn) = (row, column);
                cell.KeyDown += (_, e) =>
                {
                    var next = e.Key switch
                    {
                        Key.Left => (cellRow, cellColumn - 1),
                        Key.Right => (cellRow, cellColumn + 1),
                        Key.Up => (cellRow - 1, cellColumn),
                        Key.Down => (cellRow + 1, cellColumn),
                        _ => (cellRow, cellColumn)
                    };
                    if (next != (cellRow, cellColumn) &&
                        next.Item1 is >= 0 and < 5 && next.Item2 is >= 0 and < 5)
                    {
                        cells[next.Item1, next.Item2].Button.Focus(NavigationMethod.Directional);
                        e.Handled = true;
                    }
                };
                cells[row, column] = (cell, count, description);
                Place(cell, row + 1, column + 1);
            }
        }

        var summary = Ui.Secondary("");
        void UpdateMatrix()
        {
            for (var row = 0; row < 5; row++)
            {
                for (var column = 0; column < 5; column++)
                {
                    var (button, count, description) = cells[row, column];
                    int number = groups[(column + 1, 5 - row)].Count();
                    count.Text = number.ToString();
                    AutomationProperties.SetName(button, $"{description}. Anzahl Risiken {number}.");
                }
            }
            var countWithPosition = portfolio.Length - groups[null].Count();
            summary.Text = $"{countWithPosition} von {portfolio.Length} Risiken in dieser Bewertungssicht";
            Show("Risiken im Filter", portfolio);
        }
        mode.SelectionChanged += (_, _) =>
        {
            _showResidualRiskMatrix = Residual();
            groups = portfolio.ToLookup(row => row.Position(_showResidualRiskMatrix));
            UpdateMatrix();
        };
        UpdateMatrix();

        var legend = Ui.Row(Enumerable.Range(1, 25).GroupBy(RiskAssessmentMethod.LevelForScore).Select(group =>
        {
            var tone = RiskTones[group.Key];
            var label = Ui.Text($"{RiskAssessmentMethod.LevelLabels[group.Key]} · {group.Min()}–{group.Max()}", 11, true);
            label.Foreground = tone.Text;
            return (Control)new Border
            {
                Background = tone.Background,
                CornerRadius = new CornerRadius(5),
                Padding = new Thickness(8, 4),
                Child = label
            };
        }).ToArray());
        var heading = Ui.Heading("Risikomatrix", 25, 1);
        heading.VerticalAlignment = VerticalAlignment.Center;
        var modeField = Ui.Field("Bewertungssicht", mode,
            "Ausgangsrisiko vor Maßnahmen. Das Restrisiko verwendet separat dokumentierte Werte nach Maßnahmen.");
        var left = Ui.Card(Ui.Scroll(Ui.Stack(
            Ui.Row(Ui.IconBadge(Glyph.RiskMatrix), heading),
            modeField,
            summary,
            Ui.Secondary("Eintrittswahrscheinlichkeit (1 sehr unwahrscheinlich · 5 sehr wahrscheinlich)", 12),
            matrix,
            legend,
            Ui.Secondary("Stufe aus Eintrittswahrscheinlichkeit × Auswirkung. Die Zahl in der Zelle gibt die Anzahl der Risiken an.", 12))));
        var right = Ui.Card(Ui.Fill(Ui.Stack(resultLabel, resultCount, Ui.Row(showAll, showUnassessed, showMissingResidual)), results, open));
        var layout = new Grid { ColumnDefinitions = new ColumnDefinitions("1.55*,*"), ColumnSpacing = 12 };
        layout.Children.Add(left);
        Grid.SetColumn(right, 1);
        layout.Children.Add(right);
        return layout;
    }
}
