using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using D3.Grc.Core;
namespace D3.Grc.Desktop;
public sealed partial class MainWindow:Window
{
    private static readonly string AppVersion = typeof(MainWindow).Assembly.GetName().Version?.ToString(3) ?? "";
    private readonly LocalIdentity _identity;private readonly Workspace _workspace;
    private ContentControl _body=new();private TextBlock _status=Ui.Text("",12);
    private ComboBox _topic=Ui.Combo();private TextBox _scope=Ui.Input("Scope exakt filtern (leer = alle)"),_search=Ui.Input("Suche: Titel, Code, Beschreibung, Owner");
    private TextBlock _context=Ui.Text("",18,true);private readonly DispatcherTimer _timer=new(){Interval=TimeSpan.FromSeconds(30)};
    private Grid? _shell;private bool _busy,_loading,_allowClose;private string _page="Dashboard",_topicId="",_scopeValue="",_searchValue="",_statusFilter="";
    private List<Choice> _topics=[];private List<Entry> _entries=[];private List<Entry> _all=[];private List<Source> _sources=[];private List<Source> _allSources=[];private List<Mapping> _mappings=[];
    public MainWindow(LocalIdentity identity,Workspace workspace)
    {
        _identity=identity;_workspace=workspace;Title=$"D3 GRC {AppVersion} · Datenschutz und Informationssicherheit";Width=1440;Height=950;MinWidth=1040;MinHeight=700;Background=Ui.Canvas;FontSize=14;
        Opened+=(_,_)=>LoginScreen();
        Closing+=async(_,e)=>{if(_allowClose)return;e.Cancel=true;if(_busy)return;await Run(async()=>{if(!await CanLeaveFormAsync())return;await _workspace.LogoutAsync();_allowClose=true;_timer.Stop();Close();});};
        KeyDown+=async(_,e)=>{if(!_workspace.IsOpen)return;var command=e.KeyModifiers.HasFlag(KeyModifiers.Control)||e.KeyModifiers.HasFlag(KeyModifiers.Meta);if(command&&e.Key==Key.S){e.Handled=true;await Run(Save);}else if(command&&e.Key==Key.P){e.Handled=true;await Run(()=>Export(false));}};
        _timer.Tick+=async(_,_)=>{if(!_busy&&_identity.User is not null&&_workspace.IsOpen&&_workspace.IsDirty)await Run(async()=>{await _workspace.AutosaveAsync();if(!HasFormChanges)_status.Text="Wiederherstellungsstand aktualisiert. Hauptdatei noch nicht gespeichert.";});};_timer.Start();
    }
    private Button Button(string label, Func<Task> action, bool primary = false, string? help = null, Glyph? glyph = null)
    {
        var button = new Button
        {
            Content = glyph is { } symbol ? Ui.IconLabel(symbol, label, primary ? Brushes.White : Ui.Ink) : label,
            FontSize = 13,
            MinHeight = 37,
            Padding = new Thickness(12, 7),
            CornerRadius = new CornerRadius(7)
        };
        if (glyph is not null)
            AutomationProperties.SetName(button, label);
        if (primary)
        {
            button.Classes.Add("accent");
            button.Background = Ui.Green;
            button.Foreground = Brushes.White;
        }
        else
            button.Classes.Add("grc-action");
        button.Click += async (_, _) => await Run(action);
        return help is null ? button : Ui.Help(button, help);
    }
    private async Task Run(Func<Task> action)
    {
        if(_busy)return;_busy=true;
        var origin = FocusManager?.GetFocusedElement() as Control;
        if(_shell is not null)_shell.IsEnabled=false;
        try{await action();}
        catch(WorkspacePersistenceException ex)
        {
            _refreshAfterPersistence = true;
            _status.Text = "Hauptdatei noch nicht gespeichert. Bitte erneut speichern.";
            await Ui.Confirm(this, ex.Message, "Verstanden");
        }
        catch(Exception ex){var text=ex is CryptographicException?"Passwort falsch oder Datei beschädigt.":ex.Message;_status.Text="Fehler: "+text;await Ui.Confirm(this,text,"Verstanden");}
        finally
        {
            if (_shell is not null)
                _shell.IsEnabled = true;
            _busy = false;
            if (origin is { IsEffectivelyEnabled: true, IsEffectivelyVisible: true } && TopLevel.GetTopLevel(origin) == this)
                origin.Focus();
        }
    }
    private void LoginScreen()
    {
        _shell=null;_body.Content=null;_form=null;_refreshAfterPersistence=false;_entries.Clear();_all.Clear();_sources.Clear();_allSources.Clear();_mappings.Clear();
        var name=Ui.Input("Lokaler Profilname");var password=Ui.Input("Profilpasswort");password.PasswordChar='*';
        var login=Button("Anmelden",async()=>{if(!await _identity.LoginAsync(name.Text??"",password.Text??""))throw new UnauthorizedAccessException("Anmeldung fehlgeschlagen.");password.Text="";BuildShell();await Render();},true,glyph:Glyph.Arrow);
        login.IsDefault = true;
        var register=Button("Profil erstellen",async()=>{
            var confirm=await Ui.Prompt(this,"Profilpasswort bestätigen (mindestens 12 Zeichen)",true);if(confirm is null)return;if(confirm!=password.Text)throw new ArgumentException("Passwörter stimmen nicht überein.");await _identity.RegisterAsync(name.Text??"",password.Text??"");
            if (!await _identity.LoginAsync(name.Text ?? "", password.Text ?? ""))
                throw new UnauthorizedAccessException("Das Profil wurde eingerichtet. Bitte melde dich damit an.");
            password.Text = "";
            BuildShell();
            await Render();
            await CreateProjectFromSetupAsync();
        }, help: "Legt ein lokales Profil an und startet den Assistenten für die verschlüsselte Projektdatei.", glyph: Glyph.Person);
        var logo = CreateLogo(58);
        logo.HorizontalAlignment = HorizontalAlignment.Left;
        var card=Ui.Card(Ui.Stack(logo,Ui.Heading("Bei D3 GRC anmelden",28,1),Ui.Secondary("Anmeldung mit einem lokalen Profil.",14),
            Ui.Field("Profilname", name, "Der Profilname wird im Änderungsjournal als lokale Identität angezeigt."),
            Ui.Field("Profilpasswort", password, "Ein neues Profilpasswort muss mindestens 12 Zeichen haben. Projektdateien haben ein separates Passwort."),
            Ui.Row(login,register),Ui.Secondary("Das Profilpasswort lässt sich nicht zurücksetzen. Ein neues Profil kann einen verlorenen lokalen Zugang ersetzen. Ihr Projektpasswort bleibt erforderlich."),Ui.Secondary(Ui.KeyboardHelp, 12)));
        card.Width=500;card.Padding=new Thickness(30);card.HorizontalAlignment=HorizontalAlignment.Center;card.VerticalAlignment=VerticalAlignment.Center;card.Margin=new Thickness(24);
        Content = new Border
        {
            Background = new LinearGradientBrush
            {
                StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
                EndPoint = new RelativePoint(1, 1, RelativeUnit.Relative),
                GradientStops =
                {
                    new GradientStop(Color.Parse("#10283E"), 0),
                    new GradientStop(Color.Parse("#17364C"), 0.63),
                    new GradientStop(Color.Parse("#126A68"), 1)
                }
            },
            Child = card
        };
    }
    private async Task ApplyFilters()
    {
        var scope = _scope.Text?.Trim() ?? "";
        var search = _search.Text?.Trim() ?? "";
        if (scope == _scopeValue && search == _searchValue || !await CanLeaveFormAsync())
            return;
        _scopeValue = scope;
        _searchValue = search;
        await Render();
    }
    private async Task Render()
    {
        var previousForm = _form;
        _loading = true;
        _form = null;
        try
        {
            if (!_workspace.IsOpen)
            {
                _body.Content = Ui.Card(Ui.Stack(
                    Ui.Heading("Projekt öffnen oder anlegen", 24, 1),
                    Ui.Secondary("Ein neues Projekt beginnt mit dem Unternehmensassistenten. Zum Weiterarbeiten können Sie eine vorhandene Projektdatei öffnen.", 14),
                    Ui.Notice("Die aktive SQLite-Arbeitskopie liegt im Klartext vor. Datenträgerverschlüsselung verwenden.")));
                _context.Text = "Kein Projekt geöffnet";
                _status.Text = "Profil angemeldet";
                UpdateShellNavigation();
                return;
            }

            _topics = new[] { new Choice("", "Alle Themen") }.Concat(await _workspace.TopicsAsync()).ToList();
            _topic.ItemsSource = _topics;
            _topic.SelectedItem = _topics.FirstOrDefault(topic => topic.Id == _topicId) ?? _topics[0];
            _all = await _workspace.EntriesAsync();
            var contextEntries = _topicId == "" && _scopeValue == ""
                ? _all
                : await _workspace.EntriesAsync(_topicId, "", _scopeValue);
            _entries = contextEntries.Where(Matches).ToList();
            _sources = await _workspace.SourcesAsync(_topicId);
            _allSources = _topicId == "" ? _sources : await _workspace.SourcesAsync();
            _mappings = await _workspace.MappingsAsync();
            _body.Content = _page switch
            {
                "Dashboard" => await Dashboard(),
                "Documents" => await DocumentsPage(),
                "Mappings" => await MappingPage(),
                "Evidence" => await EvidencePage(),
                "Sources" => SourcePage(),
                "Templates" => await TemplatePage(),
                "Glossary" => await GlossaryPage(),
                "Journal" => await JournalPage(),
                _ => await EntryPage(_page)
            };
            _context.Text = $"{_workspace.Name} / {(_topic.SelectedItem as Choice)?.Title} / {Pages[_page]}";
            UpdateShellNavigation();
            UpdateFormStatus();
        }
        catch
        {
            _form = previousForm;
            throw;
        }
        finally { _loading = false; }
    }
    private bool Matches(Entry e)=>_searchValue==""||new[]{e.Title,e.Code,e.Body,e.Owner}.Any(s=>s.Contains(_searchValue,StringComparison.CurrentCultureIgnoreCase));
    private async Task Save()
    {
        if (_refreshAfterPersistence)
        {
            await RetryPersistenceAsync();
            return;
        }
        if (HasFormChanges)
        {
            await SaveFormAsync(refresh: true);
            return;
        }
        await _workspace.SaveAsync();
        UpdateFormStatus();
    }
    private async Task Logout(){if(!await CanLeaveFormAsync())return;await _workspace.LogoutAsync();LoginScreen();}
    private async Task CloseProject(){if(!await CanLeaveFormAsync())return;if(_workspace.IsOpen){if(_workspace.IsDirty)await _workspace.SaveAsync();await _workspace.CloseAsync();}_refreshAfterPersistence=false;await Render();}
    private Task NewProject() => CreateProjectFromSetupAsync();
    private async Task OpenProject()
    {
        if (_workspace.IsOpen)
            throw new InvalidOperationException("Aktuelles Projekt zuerst schließen.");
        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            AllowMultiple = false,
            FileTypeFilter = [new("D3 GRC") { Patterns = ["*.d3grc"] }]
        });
        var path = files.FirstOrDefault()?.TryGetLocalPath();
        if (path is null)
            return;
        bool? recovery = File.Exists(path + ".recovery") ? await ChooseRecoveryAsync() : false;
        if (recovery is null)
            return;
        var password = await Ui.Prompt(this, "Projektpasswort", true);
        if (password is null)
            return;
        await _workspace.OpenAsync(path, password, recovery.Value);
        ResetProjectContext();
        await Render();
        if (_workspace.PreservedRecoveryPath is { } preserved)
            _status.Text = "Vorheriger Wiederherstellungsstand gesichert · " + preserved;
    }

    private async Task<bool?> ChooseRecoveryAsync()
    {
        var dialog = new Window
        {
            Title = "Projektstand auswählen",
            Width = 650,
            SizeToContent = SizeToContent.Height,
            WindowStartupLocation = WindowStartupLocation.CenterOwner
        };
        var stop = new Button { Content = "Abbrechen", IsCancel = true };
        var original = new Button { Content = "Hauptdatei öffnen" };
        var recovered = new Button { Content = "Wiederherstellungsstand öffnen" };
        stop.Click += (_, _) => dialog.Close(null);
        original.Click += (_, _) => dialog.Close(false);
        recovered.Click += (_, _) => dialog.Close(true);
        dialog.Opened += (_, _) => stop.Focus();
        var contents = Ui.Card(Ui.Stack(
            Ui.Heading("Ein Wiederherstellungsstand ist vorhanden", 19),
            Ui.Text("Beim Öffnen der Hauptdatei bleibt der Wiederherstellungsstand als separate verschlüsselte Projektdatei erhalten."),
            Ui.Row(stop, original, recovered)));
        contents.Margin = new Thickness(16);
        dialog.Content = contents;
        return await dialog.ShowDialog<bool?>(this);
    }

    private void ResetProjectContext()
    {
        _page = "Dashboard";
        _topicId = "";
        _scopeValue = "";
        _searchValue = "";
        _statusFilter = "";
        _scope.Text = "";
        _search.Text = "";
    }
    private async Task SaveAs()
    {
        if (!_workspace.IsOpen)
            throw new InvalidOperationException("Projekt öffnen.");
        var path = await SavePath("organisation-kopie.d3grc", "d3grc");
        if (path is null || !await SaveFormAsync())
            return;
        await _workspace.SaveAsAsync(path);
        _refreshAfterPersistence = false;
        await Render();
        _status.Text = "Neue Projektkopie mit gleichem Passwort gespeichert. Alte Datei bleibt erhalten.";
    }
    private async Task<string?> SavePath(string name,string extension){var file=await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions{SuggestedFileName=name,DefaultExtension=extension});return file?.TryGetLocalPath();}
    private async Task Export(bool excel)
    {
        if (!_workspace.IsOpen)
            throw new InvalidOperationException("Projekt öffnen.");
        var draftNotice = HasFormChanges ? " Ungespeicherte Formularänderungen bleiben in der Bearbeitung." : "";
        if(!await Ui.Confirm(this,$"Exportiert werden das Thema '{(_topic.SelectedItem as Choice)?.Title}', Scope '{_scopeValue}' und Suche '{_searchValue}'. Der Export enthält die übernommenen Objekte im Kontext als Klartext und verwendet keine lokalen Listenstatusfilter.{draftNotice}","Exportieren"))return;
        var sheets=await _workspace.ExportSnapshotAsync(_topicId,_scopeValue,_searchValue);var path=await SavePath(excel?"D3-GRC-Bericht.xlsx":"D3-GRC-Druckansicht.html",excel?"xlsx":"html");if(path is null)return;
        await Files.AtomicAsync(path,excel?ExcelExport.Build(sheets):Encoding.UTF8.GetBytes(ExcelExport.Html(_workspace.Name,sheets)));await _workspace.RecordExportAsync(path,_topicId,_scopeValue,excel?"xlsx":"html");
        if(!excel)Process.Start(new ProcessStartInfo(new Uri(path).AbsoluteUri){UseShellExecute=true});_status.Text="Export erfolgreich geschrieben (Klartext).";
    }
    private ComboBox SourceCombo(IEnumerable<Source>? sources=null){var box=Ui.Combo();box.ItemsSource=(sources??_sources).ToList();box.SelectedIndex=0;return box;}
    private static string Sid(ComboBox box)=>(box.SelectedItem as Source)?.Id??throw new ArgumentException("Quelle auswählen.");
}
