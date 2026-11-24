using System.Text.Json;
using Avalonia.Controls;
using D3.Grc.Core;

namespace D3.Grc.Desktop;

internal sealed class NativeModuleForm
{
    private readonly Dictionary<string, Control> _inputs = new(StringComparer.Ordinal);
    public ModuleSpec Spec { get; }
    public Control Content { get; }
    public Control[] Inputs => _inputs.Values.ToArray();

    public NativeModuleForm(ModuleSpec spec, Action changed)
    {
        Spec = spec;
        var fields = Ui.Stack(Ui.Heading(spec.Title, 18), Ui.Secondary(spec.Description));
        foreach (var field in spec.Fields)
        {
            Control input;
            if (field.Type == "select")
            {
                var options = Ui.Combo();
                options.ItemsSource = new[] { "Keine Angabe" }.Concat(field.Options ?? []).ToArray();
                options.SelectedIndex = 0;
                input = options;
            }
            else if (field.Type == "steps")
            {
                var steps = new ProcessStepsEditor();
                steps.ValueChanged += (_, _) => changed();
                input = steps;
            }
            else
            {
                var text = Ui.Input(field.Type switch { "date" => "JJJJ-MM-TT", "number" => "Zahl", _ => "" },
                    field.Type == "long");
                text.MaxLength = field.Type == "number" ? 30 : 5000;
                input = text;
            }
            _inputs.Add(field.Key, input);
            fields.Children.Add(Ui.Field(field.Label, input));
        }
        Content = Ui.Scroll(fields);
    }

    public void Load(IReadOnlyDictionary<string, string>? fields)
    {
        foreach (var (key, input) in _inputs)
        {
            var value = fields?.GetValueOrDefault(key) ?? "";
            switch (input)
            {
                case TextBox text: text.Text = value; break;
                case ComboBox options: options.SelectedItem = value.Length == 0 ? "Keine Angabe" : value; break;
                case ProcessStepsEditor steps: steps.Value = value; break;
            }
        }
    }

    public Dictionary<string, string> Read() => _inputs.ToDictionary(pair => pair.Key, pair => pair.Value switch
    {
        TextBox text => text.Text ?? "",
        ComboBox { SelectedIndex: > 0, SelectedItem: string option } => option,
        ProcessStepsEditor steps => steps.Value,
        _ => ""
    }, StringComparer.Ordinal);

    public string State => JsonSerializer.Serialize(Read());
}

public sealed partial class MainWindow
{
    private string? _requestedModuleEntryId;

    private async Task OpenModuleEntryAsync(string kind, string? id = null)
    {
        if (!await CanLeaveFormAsync())
            return;
        if (id is not null && _all.FirstOrDefault(entry => entry.Id == id && entry.Kind == kind) is { } entry)
        {
            var topics = await _workspace.RecordTopicsAsync(id);
            if (_topicId != "" && !topics.Any(topic => topic.Id == _topicId))
                _topicId = "";
            if (EffectiveScope != "" && EffectiveScope != entry.Scope)
                _scopeValue = entry.Scope;
            if (!Matches(entry))
                _searchValue = _search.Text = "";
        }
        else if (DesktopModules.Specs.TryGetValue(kind, out var spec) && _topicId != "" && _topicId != spec.Topic)
        {
            _topicId = spec.Topic;
        }
        _requestedModuleEntryId = id;
        _statusFilter = "";
        _page = kind;
        await Render();
    }

    private async Task AddModuleRelationAsync(Entry? entry)
    {
        if (entry is null)
            throw new ArgumentException("Bitte zuerst einen gespeicherten Eintrag auswählen.");
        if (!await CanLeaveFormAsync())
            return;
        var types = Workspace.LinkTypes.Where(pair => pair.Value.From == entry.Kind)
            .Select(pair => new Choice(pair.Key, pair.Key)).Cast<object>();
        if (await Ui.Pick(this, "Fachlichen Bezug auswählen", types) is not Choice type)
            return;
        var options = _all.Where(item => item.Kind == Workspace.LinkTypes[type.Id].To && item.Id != entry.Id &&
            (type.Id != ProcessModule.ParentType || item.Scope == entry.Scope)).Cast<object>().ToArray();
        if (options.Length == 0)
            throw new ArgumentException("Für diese Verknüpfungsart ist noch kein passender Zieleintrag vorhanden.");
        if (await Ui.Pick(this, "Eintrag verknüpfen", options) is not Entry target)
            return;
        var relationReason = await Ui.Prompt(this, "Fachliche Begründung für den Bezug");
        if (relationReason is null)
            return;
        await _workspace.LinkAsync(entry.Id, target.Id, type.Id, entry.SourceId, "Unterstützend", entry.Scope, relationReason);
        await OpenModuleEntryAsync(entry.Kind, entry.Id);
    }
}
