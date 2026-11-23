using System.Text.Json;

namespace D3.Grc.Core;

[Flags]
public enum RichTextStyle : byte
{
    None = 0,
    Bold = 1,
    Italic = 2,
    Underline = 4,
    Heading = 8,
    Subheading = 16,
    Code = 32
}

public sealed record RichTextSpan(int Start, int Length, RichTextStyle Style);

/// <summary>Rich text stored in the existing text fields using only literal text and allowed style flags.</summary>
public sealed record RichTextContent(string Text, RichTextSpan[] Spans)
{
    private const string Prefix = "D3GRC/RichText-v1\n";
    private const int MaximumEncodedLength = 8_000_000;
    private const RichTextStyle AllowedStyles = RichTextStyle.Bold | RichTextStyle.Italic | RichTextStyle.Underline |
        RichTextStyle.Heading | RichTextStyle.Subheading | RichTextStyle.Code;

    public static string Plain(string content) => TryRead(content)?.Text ?? content;

    public static RichTextContent? TryRead(string content)
    {
        if (content.Length > MaximumEncodedLength || !content.StartsWith(Prefix, StringComparison.Ordinal))
            return null;
        try
        {
            var document = JsonSerializer.Deserialize<RichTextContent>(content.AsSpan(Prefix.Length));
            if (document?.Text is null || document.Spans is null)
                return null;

            var previousEnd = 0;
            foreach (var span in document.Spans)
            {
                if (span is null || span.Start < previousEnd || span.Length <= 0 || span.Length > document.Text.Length - span.Start ||
                    span.Style == RichTextStyle.None || (span.Style & ~AllowedStyles) != 0)
                    return null;
                previousEnd = span.Start + span.Length;
            }
            return document;
        }
        catch (JsonException)
        {
            // Old free-text values are never silently replaced by an unreadable document.
            return null;
        }
    }

    public string Serialize() => Spans.Length == 0 && !Text.StartsWith(Prefix, StringComparison.Ordinal)
        ? Text
        : Prefix + JsonSerializer.Serialize(this);
}
