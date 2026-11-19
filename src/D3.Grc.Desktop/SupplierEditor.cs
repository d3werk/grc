using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using D3.Grc.Core;

namespace D3.Grc.Desktop;

public sealed partial class MainWindow
{
    private sealed class SupplierFields
    {
        private readonly TextBox _service = Ui.Input("z. B. Betrieb der Cloud-Plattform");
        private readonly ComboBox _criticality = SupplierChoices(SupplierRegister.CriticalityLabels);
        private readonly TextBox _agreement = Ui.Input("z. B. Vertragsnummer oder Ablageort");
        private readonly TextBox _rationale = Ui.Input("Abhängigkeiten und mögliche Auswirkungen", true);

        public SupplierFields()
        {
            _service.MaxLength = _agreement.MaxLength = 1000;
            _rationale.MaxLength = 30000;
        }

        public IEnumerable<Control> Inputs => [_service, _criticality, _agreement, _rationale];
        public object State => (_service.Text ?? "", _criticality.SelectedItem, _agreement.Text ?? "", _rationale.Text ?? "");

        public Control Build() => Ui.Stack(
            Ui.Heading("Leistung und Risiko", 17, 3),
            CompanyColumns(
                Ui.Field("Leistung *", _service, "Beschreibt die konkrete Leistung, die der Lieferant für das Unternehmen erbringt."),
                Ui.Field("Kritikalität *", _criticality, "Bewertet die Bedeutung der Leistung und die Auswirkungen eines Ausfalls.")),
            Ui.Field("Vertragsreferenz", _agreement, "Verweist auf den Vertrag oder seinen geschützten Ablageort."),
            Ui.Field("Risikobegründung *", _rationale, "Begründet die Einstufung anhand von Abhängigkeiten, Daten und Folgen bei einem Ausfall."));

        public void Load(SupplierDetails? details)
        {
            _service.Text = details?.Service ?? "";
            _criticality.SelectedItem = details is null ? null : SupplierRegister.CriticalityLabels[details.Criticality];
            _agreement.Text = details?.AgreementReference ?? "";
            _rationale.Text = details?.RiskRationale ?? "";
        }

        public SupplierDetails Read() => new(_service.Text ?? "",
            ReadSupplierChoice(_criticality, SupplierRegister.CriticalityLabels, "Bitte eine Kritikalität auswählen."),
            _agreement.Text ?? "", _rationale.Text ?? "");
    }

    private static ComboBox SupplierChoices<T>(IReadOnlyDictionary<T, string> labels) where T : notnull
    {
        var combo = Ui.Combo();
        combo.ItemsSource = labels.Values.ToList();
        return combo;
    }

    private static T ReadSupplierChoice<T>(ComboBox combo, IReadOnlyDictionary<T, string> labels, string error) where T : notnull
    {
        foreach (var option in labels)
            if (Equals(option.Value, combo.SelectedItem))
                return option.Key;
        throw new ArgumentException(error);
    }

    private static string SupplierDate(DateOnly date) => date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    private static DateOnly ReadSupplierDate(TextBox input, string label)
    {
        if (DateOnly.TryParseExact(input.Text?.Trim(), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
            return date;
        throw new ArgumentException($"{label} im Format JJJJ-MM-TT eingeben.");
    }

    private static void PresentSupplierReviews(StackPanel history, Button add, Entry? supplier, IReadOnlyList<SupplierReview> reviews)
    {
        add.IsEnabled = supplier is not null && supplier.Status != "Beendet";
        history.Children.Clear();
        if (supplier is null)
        {
            history.Children.Add(Ui.Notice("Bewertungen können nach dem Speichern des Lieferanten erfasst werden."));
            return;
        }

        if (supplier.Status == "Beendet")
            history.Children.Add(Ui.Notice("Die Geschäftsbeziehung ist beendet. Vor einer neuen Bewertung den Status unter Details ändern."));
        if (reviews.Count == 0)
            history.Children.Add(Ui.Notice("Für diesen Lieferanten liegt noch keine dokumentierte Bewertung vor."));
        foreach (var review in reviews)
        {
            var outcome = SupplierRegister.ReviewOutcomeLabels[review.Outcome];
            var next = review.NextReview is { } date ? SupplierDate(date) : "Kein Folgetermin";
            history.Children.Add(Ui.Card(Ui.Stack(
                Ui.Heading($"{SupplierDate(review.ReviewedOn)} · {outcome}", 16, 3),
                Ui.Secondary($"{review.Reviewer} · Nächste Prüfung: {next}"),
                Ui.Text(review.Notes))));
        }
    }

    private async Task<(bool Saved, WorkspacePersistenceException? Failure)> ShowSupplierReviewAsync(Entry supplier)
    {
        var window = new Window
        {
            Title = "Lieferantenbewertung erfassen",
            Width = 680,
            Height = 700,
            MinWidth = 460,
            MinHeight = 470,
            WindowStartupLocation = WindowStartupLocation.CenterOwner
        };
        var reviewedOn = Ui.Input("JJJJ-MM-TT");
        reviewedOn.Text = SupplierDate(DateOnly.FromDateTime(DateTime.Today));
        var reviewer = Ui.Input("Name oder Rolle");
        reviewer.Text = _identity.User;
        reviewer.MaxLength = 240;
        var outcome = SupplierChoices(SupplierRegister.ReviewOutcomeLabels);
        var nextReview = Ui.Input("JJJJ-MM-TT");
        var notes = Ui.Input("Prüfgrundlage, Beobachtungen und vereinbarte Schritte", true);
        notes.MaxLength = 30000;
        var fields = Ui.Stack(
            CompanyColumns(
                Ui.Field("Prüfdatum *", reviewedOn, "Das tatsächliche Prüfdatum eingeben. Ältere Bewertungen ergänzen die Historie."),
                Ui.Field("Geprüft von *", reviewer, "Die prüfende Person oder Rolle für diese Bewertung angeben.")),
            CompanyColumns(
                Ui.Field("Ergebnis *", outcome, "Die zeitlich neueste Bewertung bestimmt den Status des Lieferanten."),
                Ui.Field("Nächste Prüfung", nextReview, "Bei einer Freigabe oder offenen Maßnahmen ist ein Folgetermin nach dem Prüfdatum erforderlich.")),
            Ui.Field("Prüfnotiz *", notes, "Dokumentiert die Bewertungsgrundlage und begründet das Ergebnis. Bei Auflagen die erforderlichen Maßnahmen festhalten."));
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
            Child = Ui.Fill(Ui.Stack(Ui.Heading("Lieferantenbewertung erfassen", 23, 1), Ui.Secondary(supplier.Title)),
                Ui.Scroll(fields), footer)
        };
        var saving = false;
        WorkspacePersistenceException? persistenceFailure = null;
        window.Closing += (_, e) => e.Cancel = saving;
        window.Opened += (_, _) => reviewedOn.Focus();
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
                var input = new SupplierReviewInput(
                    ReadSupplierDate(reviewedOn, "Prüfdatum"),
                    reviewer.Text ?? "",
                    ReadSupplierChoice(outcome, SupplierRegister.ReviewOutcomeLabels, "Bitte ein Ergebnis auswählen."),
                    notes.Text ?? "",
                    string.IsNullOrWhiteSpace(nextReview.Text) ? null : ReadSupplierDate(nextReview, "Nächste Prüfung"));
                await _workspace.RecordSupplierReviewAsync(supplier.Id, input);
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
