using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Platform.Storage;

namespace D3.Grc.Desktop;

public sealed partial class MainWindow
{
    private const int RecentProjectLimit = 8;
    private sealed record RecentProject(string Name, string Path);
    private enum ChooseProjectAction { Recent, File, Create }
    private sealed record ProjectSelection(ChooseProjectAction Action, string? Path = null);

    // The list belongs to the current desktop login and is cleared on logout.
    private readonly List<RecentProject> _recentProjects = [];
    private static readonly StringComparer ProjectPathComparer = OperatingSystem.IsWindows()
        ? StringComparer.OrdinalIgnoreCase
        : StringComparer.Ordinal;

    private void RememberActiveProject()
    {
        if (_workspace.ProjectPath is not { } path)
            return;

        _recentProjects.RemoveAll(project => ProjectPathComparer.Equals(project.Path, path));
        _recentProjects.Insert(0, new RecentProject(_workspace.Name, path));
        if (_recentProjects.Count > RecentProjectLimit)
            _recentProjects.RemoveAt(RecentProjectLimit);
    }

    private async Task ChooseProjectAsync()
    {
        var dialog = new Window
        {
            Title = "Mandant wechseln",
            Width = 620,
            SizeToContent = SizeToContent.Height,
            WindowStartupLocation = WindowStartupLocation.CenterOwner
        };

        var recent = Ui.Stack(Ui.Heading("Zuletzt verwendet", 16));
        foreach (var project in _recentProjects.Where(project => !_workspace.IsCurrentProject(project.Path)))
        {
            var label = Ui.Stack(Ui.Text(project.Name, 14, true), Ui.Secondary(Path.GetFileName(project.Path), 12));
            label.Spacing = 3;
            var pick = new Button
            {
                Content = label,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                HorizontalContentAlignment = HorizontalAlignment.Left,
                Padding = new Thickness(12, 8),
                MinHeight = 48
            };
            pick.Classes.Add("grc-action");
            pick.Click += (_, _) => dialog.Close(new ProjectSelection(ChooseProjectAction.Recent, project.Path));
            recent.Children.Add(Ui.Help(pick, project.Path, $"{project.Name} öffnen"));
        }
        if (recent.Children.Count == 1)
            recent.Children.Add(Ui.Secondary("Weitere in dieser Anmeldung geöffnete Projekte erscheinen hier."));
        var recentList = Ui.Scroll(recent);
        recentList.MaxHeight = 290;

        var open = new Button { Content = "Projektdatei öffnen", IsDefault = true, MinHeight = 38 };
        var create = new Button { Content = "Neuen Mandanten anlegen", MinHeight = 38 };
        var cancel = new Button { Content = "Abbrechen", IsCancel = true, MinHeight = 38 };
        open.Click += (_, _) => dialog.Close(new ProjectSelection(ChooseProjectAction.File));
        create.Click += (_, _) => dialog.Close(new ProjectSelection(ChooseProjectAction.Create));
        cancel.Click += (_, _) => dialog.Close(null);
        dialog.Opened += (_, _) => open.Focus();

        var current = _workspace.IsOpen ? $"Aktiver Mandant · {_workspace.Name}" : "Kein Mandant geöffnet";
        var contents = Ui.Card(Ui.Stack(
            Ui.Heading("Mandant wechseln", 23, 1), Ui.Text(current, 14, true),
            Ui.Secondary("Jede Projektdatei wird mit ihrem eigenen Passwort geöffnet."),
            recentList, Ui.Row(open, create, cancel)));
        contents.Margin = new Thickness(16);
        dialog.Content = contents;

        switch (await dialog.ShowDialog<ProjectSelection?>(this))
        {
            case { Action: ChooseProjectAction.Recent, Path: { } path }:
                await OpenChosenProjectAsync(path);
                break;
            case { Action: ChooseProjectAction.File }:
                await OpenProject();
                break;
            case { Action: ChooseProjectAction.Create }:
                await CreateProjectFromSetupAsync();
                break;
        }
    }

    private async Task OpenProject()
    {
        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Verschlüsselte Projektdatei öffnen",
            AllowMultiple = false,
            FileTypeFilter = [new("D3 GRC") { Patterns = ["*.d3grc"] }]
        });
        if (files.FirstOrDefault() is not { } file)
            return;
        var path = file.TryGetLocalPath() ?? throw new IOException("Bitte eine lokale Projektdatei wählen.");
        await OpenChosenProjectAsync(path);
    }

    private async Task OpenChosenProjectAsync(string path)
    {
        if (_workspace.IsCurrentProject(path))
            return;
        bool? recovery = File.Exists(path + ".recovery") ? await ChooseRecoveryAsync() : false;
        if (recovery is null || !await CanLeaveFormAsync())
            return;
        var password = await Ui.Prompt(this, $"Projektpasswort · {Path.GetFileName(path)}", true);
        if (password is null)
            return;
        var cleanupPending = await _workspace.SwitchAsync(path, password, recovery.Value);
        await CompleteProjectSwitchAsync(cleanupPending);
    }

    private async Task CompleteProjectSwitchAsync(bool cleanupPending)
    {
        RememberActiveProject();
        _refreshAfterPersistence = false;
        ResetProjectContext();
        await Render();
        if (_workspace.PreservedRecoveryPath is { } preserved)
            _status.Text = "Vorheriger Wiederherstellungsstand gesichert · " + preserved;
        if (cleanupPending)
            await Ui.Confirm(this, "Der Mandant wurde gewechselt. Die bisherige Arbeitskopie kann über „Arbeitskopien bereinigen“ entfernt werden.", "Verstanden");
    }
}
