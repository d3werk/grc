using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Themes.Fluent;
using D3.Grc.Core;
namespace D3.Grc.Desktop;
internal static class Program
{
    [STAThread]public static void Main(string[] args)=>AppBuilder.Configure<App>().UsePlatformDetect().WithInterFont().LogToTrace().StartWithClassicDesktopLifetime(args);
}
public sealed class App:Avalonia.Application
{
    private Workspace? _workspace;
    public override void Initialize(){Styles.Add(new FluentTheme());RequestedThemeVariant=Avalonia.Styling.ThemeVariant.Light;}
    public override void OnFrameworkInitializationCompleted()
    {
        if(ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop){var identity=new LocalIdentity();_workspace=new Workspace(identity);desktop.MainWindow=new MainWindow(identity,_workspace);desktop.Exit+=(_,_)=>_workspace.Dispose();}base.OnFrameworkInitializationCompleted();
    }
}
