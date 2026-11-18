using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using D3.Grc.Core;

namespace D3.Grc.Desktop;

internal static class CustomSourceDialog
{
    public static Task<CustomSourceInput?> ShowAsync(
        Window owner, IReadOnlyList<Choice> topics, CustomSourceInput? existing, string activeTopic,
        Func<CustomSourceInput, Task> submit)
    {
        var heading = existing is null ? "Eigene Quelle hinzufügen" : "Eigene Quelle bearbeiten";
        var window = new Window
        {
            Title = heading,
            Width = 660,
            Height = 690,
            MinHeight = 430,
            MinWidth = 440,
            WindowStartupLocation = WindowStartupLocation.CenterOwner
        };

        var title = Ui.Input("Zum Beispiel Interne Richtlinie für Datenschutz");
        title.Text = existing?.Title ?? "";
        var url = Ui.Input("https://…");
        url.Text = existing?.Url ?? "";
        var reference = Ui.Input("Zum Beispiel Dokumenten-ID, Fassung und Abschnitt");
        reference.Text = existing?.Reference ?? "";
        var summary = Ui.Input("Inhalt und fachliche Relevanz", true);
        summary.Text = existing?.Summary ?? "";

        var selectedTopics = existing?.TopicIds ?? [activeTopic];
        var checks = topics.Select(topic => (topic.Id, Box: new CheckBox
        {
            Content = topic.Title,
            IsChecked = selectedTopics.Contains(topic.Id, StringComparer.Ordinal)
        })).ToArray();
        var topicChecks = Ui.Row(checks.Select(check => (Control)check.Box).ToArray());
        var topicLabel = Ui.Text("Themen · mindestens eines auswählen", 13, true);
        AutomationProperties.SetLabeledBy(topicChecks, topicLabel);
        var fields = Ui.Stack(
            Ui.Field("Quellentitel *", title),
            Ui.Field("HTTPS-Adresse (optional)", url, "Wird als Referenz gespeichert. Die Anwendung ruft diese Adresse nicht automatisch ab."),
            Ui.Field("Fundstelle (optional)", reference, "Zum Beispiel Dokumenten-ID, Vertrag, Fassung oder Abschnitt."),
            topicLabel, topicChecks,
            Ui.Field("Beschreibung (optional)", summary, "Erklärt Inhalt und Bedeutung der Quelle für das Unternehmen."));

        var error = Ui.Text("", 13);
        error.Foreground = Brush.Parse("#9D242A");
        error.IsVisible = false;
        var save = new Button
        {
            Content = existing is null ? "Quelle hinzufügen" : "Änderungen speichern",
            IsDefault = true,
            MinHeight = 38,
            Foreground = Brushes.White,
            Background = Ui.Green
        };
        var cancel = new Button { Content = "Abbrechen", IsCancel = true, MinHeight = 38 };
        var submitting = false;
        window.Closing += (_, e) => e.Cancel = submitting;
        cancel.Click += (_, _) => window.Close();
        save.Click += async (_, _) =>
        {
            if (submitting)
                return;
            submitting = true;
            save.IsEnabled = false;
            cancel.IsEnabled = false;
            fields.IsEnabled = false;
            error.IsVisible = false;
            CustomSourceInput? saved = null;
            try
            {
                var input = new CustomSourceInput(title.Text ?? "", url.Text ?? "", reference.Text ?? "",
                    summary.Text ?? "", checks.Where(check => check.Box.IsChecked == true).Select(check => check.Id).ToArray());
                var validated = input.Validated();
                await submit(validated);
                saved = validated;
            }
            catch (Exception ex)
            {
                error.Text = ex.Message;
                error.IsVisible = true;
            }
            finally
            {
                submitting = false;
                save.IsEnabled = true;
                cancel.IsEnabled = true;
                fields.IsEnabled = true;
            }
            if (saved is not null)
                window.Close(saved);
        };

        var footer = Ui.Stack(error, new StackPanel { HorizontalAlignment = HorizontalAlignment.Right, Children = { Ui.Row(cancel, save) } });
        window.Content = new Border
        {
            Padding = new Thickness(24),
            Child = Ui.Fill(Ui.Stack(Ui.Text(heading, 24, true),
                Ui.Text("Für interne Vorgaben, Verträge und zusätzliche externe Referenzen.")),
                Ui.Scroll(fields), footer)
        };
        window.Opened += (_, _) => title.Focus();
        return window.ShowDialog<CustomSourceInput?>(owner);
    }
}

public sealed partial class MainWindow
{
    private async Task EditCustomSourceAsync(Source? selected)
    {
        if (!await CanLeaveFormAsync())
            return;
        var existing = selected is null ? null : await _workspace.GetCustomSourceAsync(selected.Id);
        var topics = await _workspace.TopicsAsync();
        WorkspacePersistenceException? persistenceFailure = null;
        var input = await CustomSourceDialog.ShowAsync(this, topics, existing, _topicId, async draft =>
        {
            try
            {
                if (selected is null)
                    await _workspace.CreateCustomSourceAsync(draft);
                else
                    await _workspace.UpdateCustomSourceAsync(selected.Id, draft);
            }
            catch (WorkspacePersistenceException ex)
            {
                // The change is already committed; showing the form again would allow a duplicate.
                persistenceFailure = ex;
            }
        });
        if (input is null)
            return;

        await Render();
        if (persistenceFailure is not null)
            throw persistenceFailure;
        _status.Text = _topicId == "" || input.TopicIds.Contains(_topicId, StringComparer.Ordinal)
            ? "Eigene Quelle gespeichert."
            : "Eigene Quelle gespeichert. Sie liegt außerhalb des aktuellen Themenfilters.";
    }
}
