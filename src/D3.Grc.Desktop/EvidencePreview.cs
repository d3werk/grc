using System.Buffers.Binary;
using System.Text;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using D3.Grc.Core;

namespace D3.Grc.Desktop;

public sealed partial class MainWindow
{
    private static readonly HashSet<string> PreviewTextExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".txt", ".md", ".csv", ".log", ".json", ".xml"
    };
    private static readonly UTF8Encoding PreviewEncoding = new(false, true);

    private async Task PreviewEvidenceAsync(Proof proof)
    {
        var extension = Path.GetExtension(proof.FileName);
        using var image = extension.Equals(".png", StringComparison.OrdinalIgnoreCase)
            ? DecodeEvidencePng(await _workspace.EvidenceBytesAsync(proof.Id)) : null;
        Control preview;
        if (image is not null)
        {
            preview = Ui.Card(new Image
            {
                Source = image,
                Stretch = Stretch.Uniform,
                MaxHeight = 620,
                HorizontalAlignment = HorizontalAlignment.Center
            });
        }
        else if (PreviewTextExtensions.Contains(extension))
        {
            var text = ReadPreviewText(await _workspace.EvidenceBytesAsync(proof.Id));
            preview = extension.Equals(".md", StringComparison.OrdinalIgnoreCase)
                ? DocumentPreview.Create(text)
                : DocumentPreview.CreatePlain(text);
        }
        else
        {
            preview = Ui.Notice(
                "Die Inhaltsvorschau unterstützt UTF-8-Text, Markdown, CSV, Log, JSON, XML und PNG. Für andere Formate steht der vorhandene Dateiexport zur Verfügung.");
        }

        var window = new Window
        {
            Title = "Nachweisvorschau",
            Width = 960,
            Height = 730,
            MinWidth = 520,
            MinHeight = 440,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Background = Ui.Canvas
        };
        var close = new Button { Content = "Schließen", IsCancel = true, MinHeight = 38 };
        close.Click += (_, _) => window.Close();
        var header = Ui.Stack(Ui.Heading(proof.Title, 23, 1), Ui.Secondary(proof.FileName),
            Ui.Text($"Gültig bis {proof.ValidUntil} · Herkunft {proof.Origin}", 13));
        var container = Ui.Fill(header, Ui.Scroll(preview), Ui.Row(close));
        container.Margin = new Thickness(22);
        window.Content = container;
        await window.ShowDialog(this);
    }

    private static string ReadPreviewText(byte[] bytes)
    {
        if (bytes.Length > 512 * 1024)
            throw new InvalidDataException("Die Vorschau unterstützt Textdateien bis 512 KiB.");
        try { return PreviewEncoding.GetString(bytes).TrimStart('\uFEFF'); }
        catch (DecoderFallbackException ex)
        {
            throw new InvalidDataException("Die Textdatei muss für die Vorschau als UTF-8 gespeichert sein.", ex);
        }
    }

    private static Bitmap DecodeEvidencePng(byte[] bytes)
    {
        ReadOnlySpan<byte> signature = [137, 80, 78, 71, 13, 10, 26, 10];
        if (bytes.Length < 33 || !bytes.AsSpan(0, 8).SequenceEqual(signature) ||
            BinaryPrimitives.ReadUInt32BigEndian(bytes.AsSpan(8, 4)) != 13 ||
            !bytes.AsSpan(12, 4).SequenceEqual("IHDR"u8))
            throw new InvalidDataException("Für die Vorschau ist eine gültige PNG-Datei erforderlich.");

        var width = BinaryPrimitives.ReadUInt32BigEndian(bytes.AsSpan(16, 4));
        var height = BinaryPrimitives.ReadUInt32BigEndian(bytes.AsSpan(20, 4));
        if (width is 0 or > 4096 || height is 0 or > 4096 || (ulong)width * height > 8_000_000)
            throw new InvalidDataException("Die PNG-Vorschau unterstützt bis zu 4096 Pixel je Seite und insgesamt acht Millionen Pixel.");

        using var stream = new MemoryStream(bytes, writable: false);
        return new Bitmap(stream);
    }
}
