using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Layout;
using Avalonia.Media;
using D3.Grc.Core;
using System.Text.RegularExpressions;

namespace D3.Grc.Desktop;

internal static partial class DocumentPreview
{
    private const int MaximumLines = 1200;
    private const int MaximumPlainCharacters = 60000;

    public static Control Create(string markdown)
    {
        var rich = RichTextContent.TryRead(markdown);
        var text = rich?.Text ?? markdown.ReplaceLineEndings("\n");
        if (string.IsNullOrWhiteSpace(text))
            return Ui.Notice("Die Vorschau erscheint, sobald das Dokument Inhalt hat.");

        var lines = text.Split('\n');
        var count = Math.Min(lines.Length, MaximumLines);
        var content = new StackPanel { Spacing = 11 };
        var starts = new int[count];
        for (var line = 1; line < count; line++)
            starts[line] = starts[line - 1] + lines[line - 1].Length + 1;
        var spanIndex = 0;
        for (var index = 0; index < count; index++)
        {
            var untrimmed = lines[index];
            var line = untrimmed.Trim();
            var position = starts[index] + untrimmed.Length - untrimmed.TrimStart().Length;
            if (line.Length == 0)
                continue;

            if (line.StartsWith("```", StringComparison.Ordinal))
            {
                var start = ++index;
                while (index < count && !lines[index].TrimStart().StartsWith("```", StringComparison.Ordinal))
                    index++;
                content.Children.Add(Monospaced(string.Join("\n", lines[start..index])));
                continue;
            }

            if (index + 1 < count && TryTableHeader(line, lines[index + 1], out var headers))
            {
                var rows = new List<string[]> { headers };
                index++;
                while (index + 1 < count && ReadTableRow(lines[index + 1]) is { } row && row.Length == headers.Length)
                {
                    rows.Add(row);
                    index++;
                }
                content.Children.Add(Table(rows));
                continue;
            }

            var heading = line.TakeWhile(character => character == '#').Count();
            if (rich is null && heading is >= 1 and <= 4 && line.Length > heading + 1 && line[heading] == ' ')
            {
                var fontSize = heading switch { 1 => 25, 2 => 19, _ => 16 };
                var block = Paragraph(line[(heading + 1)..]);
                block.FontSize = fontSize;
                block.FontWeight = FontWeight.SemiBold;
                FormatHeading(block, heading);
                content.Children.Add(block);
                continue;
            }

            if (line.StartsWith("> ", StringComparison.Ordinal))
            {
                content.Children.Add(new Border
                {
                    Background = Ui.SoftGreen,
                    BorderBrush = Ui.Green,
                    BorderThickness = new Thickness(3, 0, 0, 0),
                    Padding = new Thickness(12, 8),
                    Child = ReadParagraph(line[2..], 2)
                });
                continue;
            }

            if (line is "---" or "***")
            {
                content.Children.Add(new Border { Height = 1, Background = Ui.Line });
                continue;
            }

            var marker = ListMarker().Match(line);
            if (marker.Success)
            {
                var item = ReadParagraph(line[marker.Length..], marker.Length);
                var label = Ui.Secondary(marker.Groups["number"].Success ? marker.Groups["number"].Value : "•");
                label.Margin = new Thickness(0, 0, 8, 0);
                var row = new DockPanel { Margin = new Thickness(8, 0, 0, 0) };
                DockPanel.SetDock(label, Dock.Left);
                row.Children.Add(label);
                row.Children.Add(item);
                content.Children.Add(row);
                continue;
            }

            content.Children.Add(ReadParagraph(line));

            TextBlock ReadParagraph(string value, int offset = 0) => rich is null ? Paragraph(value) :
                StyledParagraph(value, position + offset, rich.Spans, ref spanIndex);
        }

        if (lines.Length > count)
            content.Children.Add(Ui.Notice($"Die Vorschau zeigt die ersten {MaximumLines} Zeilen. Der gesamte Text steht im Editor."));
        var paper = Paper(content);
        paper.MaxWidth = 1024;
        return paper;
    }

    public static Control CreatePlain(string text)
    {
        var limited = text.Length > MaximumPlainCharacters ? text[..MaximumPlainCharacters] : text;
        var view = new SelectableTextBlock
        {
            Text = limited,
            FontSize = 13,
            FontFamily = FontFamily.Parse("monospace"),
            TextWrapping = TextWrapping.Wrap,
            Foreground = Ui.Ink
        };
        var content = limited.Length == text.Length ? (Control)view :
            Ui.Stack(view, Ui.Notice($"Vorschau auf die ersten {MaximumPlainCharacters:N0} Zeichen begrenzt."));
        return Paper(content);
    }

    private static Border Paper(Control body) => new()
    {
        Background = Ui.Surface,
        BorderBrush = Ui.Line,
        BorderThickness = new Thickness(1),
        CornerRadius = new CornerRadius(10),
        Padding = new Thickness(22, 20),
        HorizontalAlignment = HorizontalAlignment.Stretch,
        Child = body
    };

    private static Control Monospaced(string text) => new Border
    {
        Background = Ui.Canvas,
        Padding = new Thickness(12, 8),
        CornerRadius = new CornerRadius(6),
        Child = new SelectableTextBlock
        {
            Text = text,
            Foreground = Ui.Ink,
            FontSize = 13,
            FontFamily = FontFamily.Parse("monospace"),
            TextWrapping = TextWrapping.Wrap
        }
    };

    private static TextBlock Paragraph(string value)
    {
        var matches = InlineMarkup().Matches(value);
        var block = Ui.Text(matches.Count == 0 ? value : "");
        block.LineHeight = 21;
        if (matches.Count == 0)
            return block;

        var inlines = block.Inlines ??= new InlineCollection();
        var offset = 0;
        foreach (Match match in matches)
        {
            if (match.Index > offset)
                inlines.Add(new Run(value[offset..match.Index]));
            if (match.Groups["strong"].Success)
                inlines.Add(new Run(match.Groups["strong"].Value) { FontWeight = FontWeight.SemiBold });
            else if (match.Groups["italic"].Success)
                inlines.Add(new Run(match.Groups["italic"].Value) { FontStyle = FontStyle.Italic });
            else
                inlines.Add(new Run(match.Groups["code"].Value) { FontFamily = FontFamily.Parse("monospace") });
            offset = match.Index + match.Length;
        }
        if (offset < value.Length)
            inlines.Add(new Run(value[offset..]));
        return block;
    }

    private static TextBlock StyledParagraph(string value, int start, IReadOnlyList<RichTextSpan> spans, ref int spanIndex)
    {
        var end = start + value.Length;
        var block = Ui.Text("");
        block.LineHeight = 21;
        while (spanIndex < spans.Count && spans[spanIndex].Start + spans[spanIndex].Length <= start)
            spanIndex++;
        if (spanIndex < spans.Count && spans[spanIndex].Start <= start)
        {
            var style = spans[spanIndex].Style;
            if (style.HasFlag(RichTextStyle.Heading))
                FormatHeading(block, 1);
            else if (style.HasFlag(RichTextStyle.Subheading))
                FormatHeading(block, 2);
        }
        var runs = block.Inlines ??= new InlineCollection();
        var cursor = start;
        while (spanIndex < spans.Count && spans[spanIndex].Start < end)
        {
            var span = spans[spanIndex];
            var spanEnd = span.Start + span.Length;
            if (spanEnd <= cursor)
            {
                spanIndex++;
                continue;
            }
            if (cursor < span.Start)
                runs.Add(new Run(value[(cursor - start)..(span.Start - start)]));
            var from = Math.Max(cursor, span.Start);
            var to = Math.Min(end, spanEnd);
            var style = RichTextEditor.TypefaceFor(span.Style, block.FontFamily, block.FontStretch);
            runs.Add(new Run(value[(from - start)..(to - start)])
            {
                FontFamily = style.FontFamily,
                FontWeight = style.Weight,
                FontStyle = style.Style,
                FontSize = RichTextEditor.FontSizeFor(span.Style, block.FontSize),
                TextDecorations = span.Style.HasFlag(RichTextStyle.Underline) ? TextDecorations.Underline : null
            });
            cursor = to;
            if (spanEnd > end)
                break;
            spanIndex++;
        }
        if (cursor < end)
            runs.Add(new Run(value[(cursor - start)..]));
        return block;
    }

    private static void FormatHeading(TextBlock block, int level)
    {
        block.LineHeight = double.NaN;
        block.Foreground = Ui.Green;
        block.Margin = new Thickness(0, level == 1 ? 4 : 7, 0, 0);
        AutomationProperties.SetHeadingLevel(block, level);
    }

    private static string[]? ReadTableRow(string value)
    {
        var trimmed = value.Trim();
        return trimmed.Length > 2 && trimmed[0] == '|' && trimmed[^1] == '|'
            ? trimmed[1..^1].Split('|', StringSplitOptions.TrimEntries) : null;
    }

    private static bool TryTableHeader(string value, string next, out string[] headers)
    {
        headers = ReadTableRow(value) ?? [];
        var divider = ReadTableRow(next);
        return headers.Length is >= 2 and <= 8 && divider is not null && divider.Length == headers.Length &&
            divider.All(cell => cell.Trim(':') is { Length: >= 3 } dashes && dashes.All(character => character == '-'));
    }

    private static Grid Table(IReadOnlyList<string[]> rows)
    {
        var table = new Grid();
        foreach (var _ in rows[0])
            table.ColumnDefinitions.Add(new ColumnDefinition(1, GridUnitType.Star));

        for (var row = 0; row < rows.Count; row++)
        {
            table.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
            for (var column = 0; column < rows[row].Length; column++)
            {
                var cell = new Border
                {
                    Background = row == 0 ? Ui.SoftGreen : row % 2 == 0 ? Ui.Canvas : Ui.Surface,
                    BorderBrush = Ui.Line,
                    BorderThickness = new Thickness(0, 0, 1, 1),
                    Padding = new Thickness(8, 6),
                    Child = row == 0 ? Ui.Text(rows[row][column], 13, true) : Paragraph(rows[row][column])
                };
                Grid.SetColumn(cell, column);
                Grid.SetRow(cell, row);
                table.Children.Add(cell);
            }
        }
        return table;
    }

    [GeneratedRegex(@"^(?:(?<number>[0-9]+[.)])|[-*•])\s+")]
    private static partial Regex ListMarker();

    [GeneratedRegex(@"\*\*(?<strong>[^*]+)\*\*|(?<!\*)\*(?<italic>[^*]+)\*(?!\*)|`(?<code>[^`]+)`")]
    private static partial Regex InlineMarkup();
}
