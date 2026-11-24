using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using D3.Grc.Core;

namespace D3.Grc.Desktop;

public sealed partial class MainWindow
{
    private enum FormExit { Cancel, Save, Discard }

    private sealed class FormState(Func<object?> read, Func<Task<bool>> save, Func<Task>? refresh)
    {
        private object? _baseline = read();

        public Func<Task<bool>> Save { get; } = save;
        public Func<Task>? Refresh { get; } = refresh;
        public bool HasChanges => !Equals(read(), _baseline);
        public void Accept() => _baseline = read();
    }

    private FormState? _form;
    private bool _refreshAfterPersistence;
    private bool HasFormChanges => _form?.HasChanges == true;

    private void TrackForm(Func<object?> read, Func<Task<bool>> save, Func<Task>? refresh, params Control[] inputs)
    {
        var form = _form = new FormState(read, save, refresh);
        foreach (var input in inputs)
        {
            input.PropertyChanged += (_, e) =>
            {
                if (_loading || !ReferenceEquals(form, _form))
                    return;
                if (e.Property == TextBox.TextProperty || e.Property == RichTextEditor.ContentProperty ||
                    e.Property == SelectingItemsControl.SelectedItemProperty ||
                    e.Property == ToggleButton.IsCheckedProperty ||
                    e.Property == NumericUpDown.ValueProperty)
                    UpdateFormStatus();
            };
        }
    }

    private void AcceptFormState()
    {
        _form?.Accept();
        UpdateFormStatus();
    }

    private void UpdateFormStatus()
    {
        if (HasFormChanges)
            _status.Text = "Nicht gespeicherte Formularänderungen";
        else if (_workspace.IsOpen)
            _status.Text = _workspace.IsDirty
                ? "Projektänderungen noch nicht in der Hauptdatei gespeichert."
                : "Alle übernommenen Änderungen gespeichert · " + _workspace.Name;
    }

    private static async Task<bool> CommitFormChangeAsync(Func<Task> change, Action? accept = null)
    {
        try
        {
            await change();
        }
        catch (WorkspacePersistenceException)
        {
            accept?.Invoke();
            throw;
        }
        accept?.Invoke();
        return true;
    }

    private async Task<bool> SaveFormAsync(bool refresh = true)
    {
        var form = _form;
        if (form is null || !form.HasChanges)
            return true;

        if (_refreshAfterPersistence)
        {
            await _workspace.SaveAsync();
            _refreshAfterPersistence = false;
        }
        try
        {
            if (!await form.Save())
                return false;
        }
        catch (WorkspacePersistenceException)
        {
            form.Accept();
            throw;
        }
        form.Accept();
        if (refresh && form.Refresh is not null)
        {
            await form.Refresh();
            form.Accept();
        }
        UpdateFormStatus();
        return true;
    }

    private async Task SaveEditorButtonAsync()
    {
        if (_refreshAfterPersistence)
        {
            await RetryPersistenceAsync();
            return;
        }
        await SaveFormAsync(refresh: true);
    }

    private async Task RetryPersistenceAsync()
    {
        await _workspace.SaveAsync();
        _refreshAfterPersistence = false;
        if (!HasFormChanges)
            await Render();
        UpdateFormStatus();
    }

    private async Task<bool> ConfirmDiscardFormAsync() =>
        !HasFormChanges || await Ui.Confirm(this, "Möchten Sie die ungespeicherten Änderungen verwerfen?", "Verwerfen");

    private async Task<bool> CanLeaveFormAsync()
    {
        if (!HasFormChanges)
            return true;

        var dialog = new Window
        {
            Title = "Ungespeicherte Änderungen",
            Width = 520,
            SizeToContent = SizeToContent.Height,
            WindowStartupLocation = WindowStartupLocation.CenterOwner
        };
        var cancel = new Button { Content = "Abbrechen", IsCancel = true, MinHeight = 38 };
        var discard = new Button { Content = "Verwerfen", MinHeight = 38 };
        var save = new Button { Content = "Speichern", MinHeight = 38 };
        cancel.Click += (_, _) => dialog.Close(FormExit.Cancel);
        discard.Click += (_, _) => dialog.Close(FormExit.Discard);
        save.Click += (_, _) => dialog.Close(FormExit.Save);
        var content = Ui.Stack(Ui.Text("Die Änderungen im aktuellen Formular speichern?", 16), Ui.Row(cancel, discard, save));
        content.Margin = new Thickness(24);
        dialog.Content = content;
        dialog.Opened += (_, _) => cancel.Focus();
        return (await dialog.ShowDialog<FormExit>(this)) switch
        {
            FormExit.Save => await SaveFormAsync(),
            FormExit.Discard => true,
            _ => false
        };
    }

    private void SafeSelection(ListBox list, Func<object?> current, Func<object?, Task> load)
    {
        list.SelectionChanged += async (_, _) =>
        {
            if (_loading || _busy)
                return;
            var next = list.SelectedItem;
            var previous = current();
            if (Equals(next, previous))
                return;
            await Run(async () =>
            {
                list.SelectedItem = previous;
                if (!await CanLeaveFormAsync())
                    return;
                await load(next);
                list.SelectedItem = current();
                AcceptFormState();
            });
        };
    }
}
