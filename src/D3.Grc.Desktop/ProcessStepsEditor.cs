using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Threading;
using D3.Grc.Core;

namespace D3.Grc.Desktop;

public sealed class ProcessStepsEditor : UserControl
{
    private readonly List<ProcessStep> _steps = [];
    private readonly StackPanel _rows = Ui.Stack();
    private readonly Button _add;
    private string _value = "";

    public ProcessStepsEditor()
    {
        _add = EditorButton("Schritt hinzufügen", Glyph.Plus, () =>
        {
            _steps.Add(new ProcessStep(""));
            Publish();
            RenderRows(_steps.Count - 1);
        });
        Content = Ui.Stack(_rows, _add);
        RenderRows();
    }

    public string Value
    {
        get => _value;
        set
        {
            if (value == _value)
                return;
            var steps = ProcessModule.ReadSteps(value);
            _steps.Clear();
            _steps.AddRange(steps);
            _value = value;
            RenderRows();
        }
    }

    public event EventHandler? ValueChanged;

    private void Publish()
    {
        _value = _steps.Count == 0 ? "" : ProcessModule.WriteSteps(_steps);
        ValueChanged?.Invoke(this, EventArgs.Empty);
    }

    private void RenderRows(int focus = -1)
    {
        _rows.Children.Clear();
        _add.IsEnabled = _steps.Count < ProcessModule.MaxSteps;
        if (_steps.Count == 0)
            _rows.Children.Add(Ui.Secondary("Beginnen Sie mit der ersten Tätigkeit des Prozesses."));

        for (var i = 0; i < _steps.Count; i++)
        {
            var index = i;
            var step = _steps[index];
            var number = index + 1;
            var title = Ui.Input("Was wird getan?");
            var role = Ui.Input("Wer übernimmt die Tätigkeit?");
            var instruction = Ui.Input("Wie wird der Schritt ausgeführt?", true);
            instruction.MinHeight = 68;
            title.MaxLength = role.MaxLength = 160;
            instruction.MaxLength = 600;
            title.Text = step.Title;
            role.Text = step.Role;
            instruction.Text = step.Instruction;
            title.TextChanged += (_, _) => Edit(index, current => current with { Title = title.Text ?? "" });
            role.TextChanged += (_, _) => Edit(index, current => current with { Role = role.Text ?? "" });
            instruction.TextChanged += (_, _) => Edit(index, current => current with { Instruction = instruction.Text ?? "" });

            var up = EditorButton("Nach oben", null, () => Move(index, index - 1));
            var down = EditorButton("Nach unten", null, () => Move(index, index + 1));
            var remove = EditorButton("Entfernen", Glyph.Close, () =>
            {
                _steps.RemoveAt(index);
                Publish();
                RenderRows(Math.Min(index, _steps.Count - 1));
                if (_steps.Count == 0)
                    Dispatcher.UIThread.Post(() => _add.Focus());
            });
            AutomationProperties.SetName(up, $"Schritt {number} nach oben verschieben");
            AutomationProperties.SetName(down, $"Schritt {number} nach unten verschieben");
            AutomationProperties.SetName(remove, $"Schritt {number} entfernen");
            up.IsEnabled = index > 0;
            down.IsEnabled = index < _steps.Count - 1;
            var card = Ui.Card(Ui.Stack(
                Ui.Heading($"Schritt {number}", 15, 3),
                Ui.AdaptiveCards(2, 210, Ui.Field("Tätigkeit *", title), Ui.Field("Rolle", role)),
                Ui.Field("Ausführung", instruction), Ui.Row(up, down, remove)));
            card.Padding = new Thickness(12);
            card.Background = Ui.NavSurface;
            _rows.Children.Add(card);
            if (index == focus)
                Dispatcher.UIThread.Post(() => title.Focus());
        }
    }

    private void Edit(int index, Func<ProcessStep, ProcessStep> change)
    {
        var step = change(_steps[index]);
        if (step == _steps[index])
            return;
        _steps[index] = step;
        Publish();
    }

    private void Move(int source, int target)
    {
        (_steps[source], _steps[target]) = (_steps[target], _steps[source]);
        Publish();
        RenderRows(target);
    }

    private static Button EditorButton(string label, Glyph? glyph, Action action)
    {
        var button = new Button
        {
            Content = glyph is { } icon ? Ui.IconLabel(icon, label) : label,
            MinHeight = 34,
            HorizontalAlignment = HorizontalAlignment.Left,
            Padding = new Thickness(9, 5)
        };
        AutomationProperties.SetName(button, label);
        button.Click += (_, _) => action();
        return button;
    }
}
