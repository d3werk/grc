using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;
using Avalonia.Utilities;
using D3.Grc.Core;

namespace D3.Grc.Desktop;

internal sealed partial class RichTextEditor : StackPanel
{
    public static readonly DirectProperty<RichTextEditor, string> ContentProperty =
        AvaloniaProperty.RegisterDirect<RichTextEditor, string>(nameof(Content), editor => editor.Content);

    private readonly EditorTextBox _input;
    private readonly List<EditorState> _undo = [];
    private readonly List<EditorState> _redo = [];
    private readonly Button _undoButton;
    private readonly Button _redoButton;
    private readonly Control _toolbar;
    private string _content = "";
    private string _original = "";
    private EditorState _initial = new("", [], 0, 0);
    private EditorState _state = new("", [], 0, 0);
    private RichTextStyle? _typingStyle;
    private bool _applying;

    private readonly record struct EditorState(string Text, RichTextStyle[] Styles, int Start, int End);
    private const RichTextStyle HeadingFlags = RichTextStyle.Heading | RichTextStyle.Subheading;

    public string Content
    {
        get => _content;
        set
        {
            _original = value;
            var document = RichTextContent.TryRead(value) ?? LegacyText(value);
            var styles = new RichTextStyle[document.Text.Length];
            foreach (var range in document.Spans)
                Array.Fill(styles, range.Style, range.Start, range.Length);
            _initial = new EditorState(document.Text, styles, 0, 0);
            _undo.Clear();
            _redo.Clear();
            Apply(_initial);
        }
    }

    public string PlainText => _state.Text;

    public bool IsReadOnly
    {
        get => _input.IsReadOnly;
        set
        {
            _input.IsReadOnly = value;
            _toolbar.IsVisible = !value;
        }
    }

    public RichTextEditor(string name, string hint, double editorHeight = 185, int maximumLength = 30000)
    {
        Spacing = 0;
        _input = new EditorTextBox(this)
        {
            Watermark = hint,
            Height = editorHeight,
            MaxLength = maximumLength,
            AcceptsReturn = true,
            ClearSelectionOnLostFocus = false,
            NewLine = "\n",
            IsUndoEnabled = false,
            TextWrapping = TextWrapping.Wrap,
            BorderBrush = Ui.Line,
            BorderThickness = new Thickness(1),
            Padding = new Thickness(12, 9),
            Background = Ui.Surface
        };
        AutomationProperties.SetName(_input, name);
        _input.PropertyChanged += (_, args) =>
        {
            if (args.Property == TextBox.TextProperty && !_applying)
                ChangeText(_input.Text ?? "");
        };
        _input.AddHandler(KeyDownEvent, (_, args) => HandleEditorKey(args), Avalonia.Interactivity.RoutingStrategies.Tunnel);
        _undoButton = Command("↶", "Rückgängig (Strg+Z)", () => MoveHistory(_undo, _redo));
        _redoButton = Command("↷", "Wiederholen (Strg+Umschalt+Z)", () => MoveHistory(_redo, _undo));
        _toolbar = new Border
        {
            Background = Ui.Canvas,
            BorderBrush = Ui.Line,
            BorderThickness = new Thickness(1, 1, 1, 0),
            CornerRadius = new CornerRadius(7, 7, 0, 0),
            Padding = new Thickness(7, 6),
            Child = Ui.Row(
                Command("F", "Fett (Strg+B)", () => ToggleInline(RichTextStyle.Bold), FontWeight.Bold),
                Command("K", "Kursiv (Strg+I)", () => ToggleInline(RichTextStyle.Italic), fontStyle: FontStyle.Italic),
                Command("U", "Unterstrichen (Strg+U)", () => ToggleInline(RichTextStyle.Underline), underline: true),
                Command("¶ Absatz", "Überschrift in einen normalen Absatz umwandeln", SetParagraph),
                Command("Ü1", "Überschrift", () => ToggleHeading(RichTextStyle.Heading)),
                Command("Ü2", "Zwischenüberschrift", () => ToggleHeading(RichTextStyle.Subheading)),
                Command("• Liste", "Aufzählung für diese Zeile oder Auswahl", () => ToggleList(false)),
                Command("1. Liste", "Nummerierte Liste für diese Zeile oder Auswahl", () => ToggleList(true)),
                _undoButton, _redoButton)
        };
        Children.Add(_toolbar);
        Children.Add(_input);
        UpdateButtons();
    }

    private Button Command(string label, string help, Action action, FontWeight weight = FontWeight.Normal,
        FontStyle fontStyle = FontStyle.Normal, bool underline = false)
    {
        var text = Ui.Text(label, 13);
        text.FontWeight = weight;
        text.FontStyle = fontStyle;
        if (underline)
            text.TextDecorations = TextDecorations.Underline;
        var button = Ui.Help(new Button { Content = text, MinWidth = 34, Padding = new Thickness(8, 5) }, help, help);
        button.Click += (_, _) => { action(); _input.Focus(); };
        return button;
    }

    private void HandleEditorKey(KeyEventArgs eventArgs)
    {
        if (IsReadOnly)
            return;
        if (eventArgs.Key == Key.Enter && eventArgs.KeyModifiers == KeyModifiers.None && ContinueList())
        {
            eventArgs.Handled = true;
            return;
        }
        if ((eventArgs.KeyModifiers & (KeyModifiers.Control | KeyModifiers.Meta)) == 0 ||
            eventArgs.KeyModifiers.HasFlag(KeyModifiers.Alt))
            return;
        var action = eventArgs.Key switch
        {
            Key.B => () => ToggleInline(RichTextStyle.Bold),
            Key.I => () => ToggleInline(RichTextStyle.Italic),
            Key.U => () => ToggleInline(RichTextStyle.Underline),
            Key.Z when !eventArgs.KeyModifiers.HasFlag(KeyModifiers.Shift) => () => MoveHistory(_undo, _redo),
            Key.Y or Key.Z => () => MoveHistory(_redo, _undo),
            _ => (Action?)null
        };
        if (action is null)
            return;
        action();
        eventArgs.Handled = true;
    }

    private (int Start, int End) Selection()
    {
        var first = Math.Clamp(_input.SelectionStart, 0, _state.Text.Length);
        var second = Math.Clamp(_input.SelectionEnd, 0, _state.Text.Length);
        return (Math.Min(first, second), Math.Max(first, second));
    }

    private (int Start, int End) SelectedLines()
    {
        var (start, end) = Selection();
        var first = start == 0 ? 0 : _state.Text.LastIndexOf('\n', start - 1) + 1;
        var lastCharacter = end > start && _state.Text[end - 1] == '\n' ? end - 1 : end;
        var last = _state.Text.IndexOf('\n', lastCharacter);
        return (first, last < 0 ? _state.Text.Length : last);
    }

    private void ToggleInline(RichTextStyle flag)
    {
        var (start, end) = Selection();
        if (start == end)
        {
            _typingStyle = (_typingStyle ?? StyleAtCaret(start)) ^ flag;
            return;
        }
        var styles = (RichTextStyle[])_state.Styles.Clone();
        var remove = styles.AsSpan(start, end - start).ToArray().All(style => (style & flag) != 0);
        for (var index = start; index < end; index++)
            styles[index] = remove ? styles[index] & ~flag : styles[index] | flag;
        ReplaceState(new EditorState(_state.Text, styles, start, end));
    }

    private void ToggleHeading(RichTextStyle flag)
    {
        var (start, end) = SelectedLines();
        if (start == end)
        {
            var current = _typingStyle ?? StyleAtCaret(start);
            _typingStyle = (current & ~HeadingFlags) | ((current & flag) != 0 ? RichTextStyle.None : flag);
            return;
        }
        var styles = (RichTextStyle[])_state.Styles.Clone();
        var remove = styles[start..end].All(style => (style & flag) != 0);
        for (var index = start; index < end; index++)
            styles[index] = (styles[index] & ~HeadingFlags) | (remove ? RichTextStyle.None : flag);
        ReplaceState(new EditorState(_state.Text, styles, _input.SelectionStart, _input.SelectionEnd));
    }

    private void SetParagraph()
    {
        var (start, end) = SelectedLines();
        _typingStyle = (_typingStyle ?? StyleAtCaret(start)) & ~HeadingFlags;
        if (!_state.Styles[start..end].Any(style => (style & HeadingFlags) != 0))
            return;
        var styles = (RichTextStyle[])_state.Styles.Clone();
        for (var index = start; index < end; index++)
            styles[index] &= ~HeadingFlags;
        ReplaceState(new EditorState(_state.Text, styles, _input.SelectionStart, _input.SelectionEnd));
    }

    private void ToggleList(bool numbered)
    {
        var (start, end) = SelectedLines();
        var lines = _state.Text[start..end].Split('\n');
        var nonBlank = lines.Where(line => !string.IsNullOrWhiteSpace(line)).ToArray();
        var remove = nonBlank.Length > 0 && nonBlank.All(line =>
        {
            var match = EditorListMarker().Match(line);
            return match.Success && char.IsAsciiDigit(match.Groups["marker"].Value[0]) == numbered;
        });
        var text = new StringBuilder(_state.Text[..start]);
        var styles = new List<RichTextStyle>(_state.Styles[..start]);
        var offset = start;
        var number = 1;
        foreach (var line in lines)
        {
            if (offset > start)
            {
                text.Append('\n');
                styles.Add(_state.Styles[offset - 1]);
            }
            var match = EditorListMarker().Match(line);
            var indent = match.Success ? match.Groups["indent"].Length : line.Length - line.TrimStart(' ', '\t').Length;
            text.Append(line.AsSpan(0, indent));
            styles.AddRange(_state.Styles[offset..(offset + indent)]);
            if (!remove && (lines.Length == 1 || !string.IsNullOrWhiteSpace(line)))
            {
                var marker = numbered ? $"{(number++).ToString(CultureInfo.InvariantCulture)}. " : "• ";
                text.Append(marker);
                styles.AddRange(Enumerable.Repeat(RichTextStyle.None, marker.Length));
            }
            var contentStart = match.Success ? match.Length : indent;
            text.Append(line.AsSpan(contentStart));
            var contentStyles = _state.Styles[(offset + contentStart)..(offset + line.Length)];
            styles.AddRange(remove ? contentStyles : contentStyles.Select(style => style & ~HeadingFlags));
            offset += line.Length + 1;
        }
        var selectionEnd = text.Length;
        text.Append(_state.Text.AsSpan(end));
        styles.AddRange(_state.Styles[end..]);
        if (text.Length <= _input.MaxLength)
            ReplaceState(new EditorState(text.ToString(), styles.ToArray(), start, selectionEnd));
    }

    private bool ContinueList()
    {
        var (start, end) = Selection();
        if (start != end || (end < _state.Text.Length && _state.Text[end] != '\n'))
            return false;
        var (lineStart, _) = SelectedLines();
        var line = _state.Text[lineStart..end];
        var match = EditorListMarker().Match(line);
        if (!match.Success)
            return false;
        var indent = match.Groups["indent"].Value;
        if (string.IsNullOrWhiteSpace(line[match.Length..]))
            start = lineStart + indent.Length;
        var insert = "";
        if (start == end)
        {
            var marker = match.Groups["marker"].Value;
            if (char.IsAsciiDigit(marker[0]))
            {
                if (!int.TryParse(marker.AsSpan(0, marker.Length - 1), NumberStyles.None, CultureInfo.InvariantCulture,
                        out var number) || number == int.MaxValue)
                    return false;
                marker = $"{(number + 1).ToString(CultureInfo.InvariantCulture)}{marker[^1]}";
            }
            insert = $"\n{indent}{marker} ";
        }
        var text = _state.Text[..start] + insert + _state.Text[end..];
        if (text.Length > _input.MaxLength)
            return false;
        var styles = new RichTextStyle[text.Length];
        Array.Copy(_state.Styles, styles, start);
        var caret = start + insert.Length;
        Array.Copy(_state.Styles, end, styles, caret, _state.Text.Length - end);
        ReplaceState(new EditorState(text, styles, caret, caret));
        return true;
    }

    private RichTextStyle StyleAtCaret(int start) => start > 0 && _state.Text[start - 1] != '\n'
        ? _state.Styles[start - 1]
        : start < _state.Text.Length ? _state.Styles[start] : RichTextStyle.None;

    private void ChangeText(string current)
    {
        var previous = _state.Text;
        if (previous == current)
            return;
        var prefix = 0;
        while (prefix < Math.Min(previous.Length, current.Length) && previous[prefix] == current[prefix])
            prefix++;
        var suffix = 0;
        while (suffix < Math.Min(previous.Length, current.Length) - prefix && previous[^(suffix + 1)] == current[^(suffix + 1)])
            suffix++;
        var styles = new RichTextStyle[current.Length];
        Array.Copy(_state.Styles, styles, prefix);
        var insertStyle = _typingStyle ?? StyleAtCaret(prefix);
        for (var index = prefix; index < current.Length - suffix; index++)
        {
            if (current[index] == '\n')
                insertStyle = RichTextStyle.None;
            styles[index] = insertStyle;
        }
        Array.Copy(_state.Styles, previous.Length - suffix, styles, current.Length - suffix, suffix);
        Remember(_undo);
        _redo.Clear();
        _state = new EditorState(current, styles, _input.SelectionStart, _input.SelectionEnd);
        Publish();
    }

    private void ReplaceState(EditorState next)
    {
        Remember(_undo);
        _redo.Clear();
        Apply(next);
    }

    private void Apply(EditorState next)
    {
        _applying = true;
        try
        {
            _state = next;
            _typingStyle = null;
            _input.Text = next.Text;
            _input.CaretIndex = next.End;
            _input.SelectionStart = next.Start;
            _input.SelectionEnd = next.End;
            Publish();
        }
        finally { _applying = false; }
    }

    private void Publish()
    {
        _input.RefreshStyle();
        UpdateButtons();
        var encoded = _state.Text == _initial.Text && _state.Styles.AsSpan().SequenceEqual(_initial.Styles)
            ? _original : new RichTextContent(_state.Text, Spans(_state.Styles)).Serialize();
        SetAndRaise(ContentProperty, ref _content, encoded);
    }

    private void UpdateButtons()
    {
        _undoButton.IsEnabled = _undo.Count != 0;
        _redoButton.IsEnabled = _redo.Count != 0;
    }

    private void Remember(List<EditorState> history)
    {
        if (history.Count == 30)
            history.RemoveAt(0);
        var (start, end) = Selection();
        history.Add(_state with { Start = start, End = end });
    }

    private void MoveHistory(List<EditorState> from, List<EditorState> to)
    {
        if (from.Count == 0)
            return;
        var state = from[^1];
        from.RemoveAt(from.Count - 1);
        Remember(to);
        Apply(state);
    }

    private static RichTextSpan[] Spans(RichTextStyle[] styles)
    {
        var runs = new List<RichTextSpan>();
        for (var start = 0; start < styles.Length;)
        {
            var end = start + 1;
            while (end < styles.Length && styles[end] == styles[start])
                end++;
            if (styles[start] != RichTextStyle.None)
                runs.Add(new RichTextSpan(start, end - start, styles[start]));
            start = end;
        }
        return runs.ToArray();
    }

    private static RichTextContent LegacyText(string value)
    {
        var text = new StringBuilder();
        var ranges = new List<RichTextSpan>();
        void Append(string piece, RichTextStyle style)
        {
            if (piece.Length > 0 && style != RichTextStyle.None)
                ranges.Add(new RichTextSpan(text.Length, piece.Length, style));
            text.Append(piece);
        }

        var fenced = false;
        foreach (var line in value.ReplaceLineEndings("\n").Split('\n'))
        {
            if (line.TrimStart().StartsWith("```", StringComparison.Ordinal))
            {
                fenced = !fenced;
                Append(line, RichTextStyle.None);
            }
            else if (fenced || line.TrimStart().StartsWith('|'))
                Append(line, RichTextStyle.None);
            else
            {
                var heading = HeadingMarker().Match(line);
                var style = heading.Success
                    ? heading.Groups["level"].Length == 1 ? RichTextStyle.Heading : RichTextStyle.Subheading
                    : RichTextStyle.None;
                var body = line[heading.Length..];
                var cursor = 0;
                foreach (Match match in LegacyInline().Matches(body))
                {
                    Append(body[cursor..match.Index], style);
                    var italic = match.Groups["italic"];
                    var code = match.Groups["code"];
                    var strong = match.Groups["bold"];
                    Append(italic.Success ? italic.Value : code.Success ? code.Value : strong.Value,
                        style | (italic.Success ? RichTextStyle.Italic : code.Success ? RichTextStyle.Code : RichTextStyle.Bold));
                    cursor = match.Index + match.Length;
                }
                Append(body[cursor..], style);
            }
            text.Append('\n');
        }
        text.Length--;
        return new RichTextContent(text.ToString(), ranges.ToArray());
    }

    [GeneratedRegex(@"^(?<level>#{1,4}) +(?=\S)")]
    private static partial Regex HeadingMarker();

    [GeneratedRegex(@"\*\*(?<bold>[^*]+)\*\*|(?<!\*)\*(?<italic>[^*]+)\*(?!\*)|`(?<code>[^`]+)`")]
    private static partial Regex LegacyInline();

    [GeneratedRegex(@"^(?<indent>[ \t]*)(?<marker>[0-9]+[.)]|[-*•])[ \t]+")]
    private static partial Regex EditorListMarker();

    private sealed class EditorTextBox : TextBox
    {
        private StyledPresenter? _presenter;

        public EditorTextBox(RichTextEditor editor)
        {
            Template = new FuncControlTemplate<EditorTextBox>((parent, names) =>
            {
                var presenter = new StyledPresenter(editor) { Name = "PART_TextPresenter", VerticalAlignment = VerticalAlignment.Top };
                _presenter = presenter;
                void Bind(AvaloniaProperty property, string path, BindingMode mode = BindingMode.OneWay) =>
                    presenter.Bind(property, new Binding(path) { Source = parent, Mode = mode });
                Bind(TextPresenter.TextProperty, nameof(Text), BindingMode.TwoWay);
                Bind(TextPresenter.CaretIndexProperty, nameof(CaretIndex));
                Bind(TextPresenter.SelectionStartProperty, nameof(SelectionStart));
                Bind(TextPresenter.SelectionEndProperty, nameof(SelectionEnd));
                Bind(TextPresenter.SelectionBrushProperty, nameof(SelectionBrush));
                Bind(TextPresenter.CaretBrushProperty, nameof(CaretBrush));
                Bind(TextPresenter.TextWrappingProperty, nameof(TextWrapping));
                Bind(TextElement.ForegroundProperty, nameof(Foreground));
                names.Register(presenter.Name, presenter);
                var watermark = Ui.Secondary(parent.Watermark ?? "");
                watermark.IsHitTestVisible = false;
                void RefreshWatermark(object? sender, AvaloniaPropertyChangedEventArgs args)
                {
                    if (args.Property == TextPresenter.TextProperty || args.Property == TextPresenter.PreeditTextProperty)
                        watermark.IsVisible = string.IsNullOrEmpty(presenter.Text) && string.IsNullOrEmpty(presenter.PreeditText);
                }
                presenter.PropertyChanged += RefreshWatermark;
                watermark.IsVisible = string.IsNullOrEmpty(parent.Text);
                var scroll = new ScrollViewer
                {
                    Name = "PART_ScrollViewer",
                    HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                    VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                    Cursor = new Cursor(StandardCursorType.Ibeam),
                    Content = new Panel { Children = { watermark, presenter } }
                };
                names.Register(scroll.Name, scroll);
                var border = new Border { Name = "PART_BorderElement", Child = scroll };
                names.Register(border.Name, border);
                border.Bind(Border.BackgroundProperty, new Binding(nameof(Background)) { Source = parent });
                border.Bind(Border.BorderBrushProperty, new Binding(nameof(BorderBrush)) { Source = parent });
                border.Bind(Border.BorderThicknessProperty, new Binding(nameof(BorderThickness)) { Source = parent });
                border.Bind(Border.PaddingProperty, new Binding(nameof(Padding)) { Source = parent });
                return border;
            });
        }

        protected override Type StyleKeyOverride => typeof(TextBox);
        public void RefreshStyle() => _presenter?.RefreshStyle();
    }

    private sealed class StyledPresenter(RichTextEditor editor) : TextPresenter
    {
        private double _width = double.PositiveInfinity;

        public void RefreshStyle() => InvalidateTextLayout();

        protected override Size MeasureOverride(Size availableSize)
        {
            _width = availableSize.Width > 0 ? availableSize.Width : double.PositiveInfinity;
            return base.MeasureOverride(availableSize);
        }

        protected override TextLayout CreateTextLayout()
        {
            var styleFlags = editor._state.Styles;
            if (string.IsNullOrEmpty(Text) || styleFlags.Length != Text.Length || !string.IsNullOrEmpty(PreeditText))
                return base.CreateTextLayout();
            var styled = new List<ValueSpan<TextRunProperties>>();
            for (var start = 0; start < styleFlags.Length;)
            {
                var end = start + 1;
                while (end < styleFlags.Length && styleFlags[end] == styleFlags[start])
                    end++;
                var style = styleFlags[start];
                if (style != RichTextStyle.None)
                    styled.Add(new ValueSpan<TextRunProperties>(start, end - start,
                        new GenericTextRunProperties(TypefaceFor(style, FontFamily, FontStretch), FontFeatures,
                            FontSizeFor(style, FontSize), foregroundBrush: Foreground,
                            textDecorations: style.HasFlag(RichTextStyle.Underline) ? TextDecorations.Underline : null)));
                start = end;
            }
            return styled.Count == 0 ? base.CreateTextLayout() : new TextLayout(Text,
                new Typeface(FontFamily, FontStyle, FontWeight, FontStretch), FontFeatures, FontSize, Foreground,
                TextAlignment, TextWrapping, maxWidth: _width, flowDirection: FlowDirection,
                lineHeight: LineHeight, letterSpacing: LetterSpacing, textStyleOverrides: styled);
        }
    }

    internal static Typeface TypefaceFor(RichTextStyle style, FontFamily family, FontStretch stretch) => new(
        style.HasFlag(RichTextStyle.Code) ? FontFamily.Parse("monospace") : family,
        style.HasFlag(RichTextStyle.Italic) ? FontStyle.Italic : FontStyle.Normal,
        (style & (RichTextStyle.Bold | HeadingFlags)) != 0 ? FontWeight.Bold : FontWeight.Normal,
        stretch);

    internal static double FontSizeFor(RichTextStyle style, double normal) => style.HasFlag(RichTextStyle.Heading)
        ? normal + 6 : style.HasFlag(RichTextStyle.Subheading) ? normal + 3 : normal;
}
