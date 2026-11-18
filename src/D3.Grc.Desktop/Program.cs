using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Themes.Fluent;
using D3.Grc.Core;
namespace D3.Grc.Desktop;
internal static class Program
{
    [STAThread]
    public static void Main(string[] args) => AppBuilder.Configure<App>()
        .UsePlatformDetect()
        .WithInterFont()
        .LogToTrace()
        .StartWithClassicDesktopLifetime(args);
}

public sealed class App : Application
{
    private Workspace? _workspace;

    public override void Initialize()
    {
        var fluent = new FluentTheme();
        fluent.Palettes.Add(ThemeVariant.Light, new ColorPaletteResources { Accent = Color.Parse(Ui.AccentColor) });
        Styles.Add(fluent);
        RequestedThemeVariant = ThemeVariant.Light;

        Styles.Add(new Style(selector => selector.OfType<Window>())
        {
            Setters =
            {
                new Setter(TemplatedControl.FontFamilyProperty, new FontFamily("fonts:Inter#Inter")),
                new Setter(TemplatedControl.BackgroundProperty, Ui.Canvas)
            }
        });
        Styles.Add(new Style(selector => selector.OfType<TabItem>())
        {
            Setters =
            {
                new Setter(Layoutable.MinHeightProperty, 38.0),
                new Setter(TemplatedControl.PaddingProperty, new Thickness(10, 7))
            }
        });
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var identity = new LocalIdentity();
            _workspace = new Workspace(identity);
            desktop.MainWindow = new MainWindow(identity, _workspace);
            desktop.Exit += (_, _) => _workspace.Dispose();
        }
        base.OnFrameworkInitializationCompleted();
    }
}
