using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Presenters;
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
                new Setter(TemplatedControl.PaddingProperty, new Thickness(11, 7)),
                new Setter(TemplatedControl.FontWeightProperty, FontWeight.Medium)
            }
        });
        Styles.Add(new Style(selector => selector.OfType<Button>().Class("grc-action"))
        {
            Setters =
            {
                new Setter(TemplatedControl.BackgroundProperty, Ui.Surface),
                new Setter(TemplatedControl.BorderBrushProperty, Ui.Line),
                new Setter(TemplatedControl.BorderThicknessProperty, new Thickness(1)),
                new Setter(TemplatedControl.ForegroundProperty, Ui.Ink)
            }
        });
        Styles.Add(new Style(selector => selector.OfType<Button>().Class("grc-nav"))
        {
            Setters =
            {
                new Setter(TemplatedControl.BackgroundProperty, Brushes.Transparent),
                new Setter(TemplatedControl.BorderThicknessProperty, new Thickness(0)),
                new Setter(TemplatedControl.ForegroundProperty, Ui.NavText)
            }
        });
        Styles.Add(new Style(selector => selector.OfType<Button>().Class("grc-nav").Class("grc-current"))
        {
            Setters =
            {
                new Setter(TemplatedControl.BackgroundProperty, Ui.NavSelected),
                new Setter(TemplatedControl.ForegroundProperty, Ui.NavSelectedText),
                new Setter(TemplatedControl.BorderBrushProperty, Ui.Green),
                new Setter(TemplatedControl.BorderThicknessProperty, new Thickness(2, 0, 0, 0)),
                new Setter(TemplatedControl.FontWeightProperty, FontWeight.SemiBold)
            }
        });
        SetButtonState("grc-action", ":pointerover", Ui.SoftBlue, Ui.Line);
        SetButtonState("grc-action", ":pressed", Ui.Line, Ui.Line);
        SetButtonState("grc-nav", ":pointerover", Ui.NavHover, Ui.NavHover);
        SetButtonState("grc-nav", ":pressed", Ui.NavSelected, Ui.NavSelected);
        SetButtonState("grc-nav", ":disabled", Brushes.Transparent, Brushes.Transparent);
    }

    private void SetButtonState(string styleClass, string pseudoClass, IBrush background, IBrush border)
    {
        Styles.Add(new Style(selector => selector.OfType<Button>().Class(styleClass).Class(pseudoClass)
            .Template().OfType<ContentPresenter>().Name("PART_ContentPresenter"))
        {
            Setters =
            {
                new Setter(ContentPresenter.BackgroundProperty, background),
                new Setter(ContentPresenter.BorderBrushProperty, border),
                new Setter(ContentPresenter.ForegroundProperty, styleClass == "grc-nav" ? Ui.NavText : Ui.Ink)
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
