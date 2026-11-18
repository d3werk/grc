using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using D3.Grc.Core;
namespace D3.Grc.Desktop;
public sealed partial class MainWindow:Window
{
    private readonly LocalIdentity _identity;private readonly Workspace _workspace;
    private ContentControl _body=new();private TextBlock _status=Ui.Text("Entwicklungsstand 0.7.0 · noch keine Produktionsfreigabe",12);
    private ComboBox _topic=Ui.Combo();private TextBox _scope=Ui.Input("Scope exakt filtern (leer = alle)"),_search=Ui.Input("Suche: Titel, Code, Beschreibung, Owner");
    private TextBlock _context=Ui.Text("",18,true);private readonly DispatcherTimer _timer=new(){Interval=TimeSpan.FromSeconds(30)};
    private Grid? _shell;private bool _busy,_loading,_allowClose;private string _page="Dashboard",_topicId="",_scopeValue="",_searchValue="",_statusFilter="";
    private List<Choice> _topics=[];private List<Entry> _entries=[];private List<Entry> _all=[];private List<Source> _sources=[];private List<Source> _allSources=[];private List<Mapping> _mappings=[];
    private static readonly Dictionary<string,string> Pages=new(){["Dashboard"]="Übersicht",["Requirement"]="Anforderungen",["Control"]="Controls",["Risk"]="Risiken",["Mappings"]="Mappings",["Evidence"]="Evidence",["Audit"]="Audits",["Finding"]="Findings",["Measure"]="Maßnahmen",["AiSystem"]="KI-Systeme",["OssComponent"]="Open Source",["Sources"]="Quellen",["Templates"]="Textvorlagen",["Glossary"]="Glossar",["Journal"]="Aktivitäten"};
    private static readonly (string Title, string[] Pages)[] NavigationGroups =
    [
        ("Arbeitsbereich", ["Dashboard"]),
        ("Governance und Risiken", ["Requirement", "Control", "Risk", "Measure", "Mappings"]),
        ("Prüfung", ["Evidence", "Audit", "Finding"]),
        ("Register und Wissen", ["AiSystem", "OssComponent", "Sources", "Templates", "Glossary", "Journal"])
    ];
    private readonly Dictionary<string, Button> _navigation = new();
    private Control[] _projectActions = [];
    private Control[] _projectOpeningActions = [];
    public MainWindow(LocalIdentity identity,Workspace workspace)
    {
        _identity=identity;_workspace=workspace;Title="D3 GRC 0.7.0 · Datenschutz und Informationssicherheit";Width=1440;Height=960;MinWidth=1040;MinHeight=700;Background=Ui.Canvas;FontSize=14;
        Opened+=(_,_)=>LoginScreen();
        Closing+=async(_,e)=>{if(_allowClose)return;e.Cancel=true;if(_busy)return;await Run(async()=>{if(!await CanLeaveFormAsync())return;await _workspace.LogoutAsync();_allowClose=true;_timer.Stop();Close();});};
        KeyDown+=async(_,e)=>{if(!_workspace.IsOpen)return;var command=e.KeyModifiers.HasFlag(KeyModifiers.Control)||e.KeyModifiers.HasFlag(KeyModifiers.Meta);if(command&&e.Key==Key.S){e.Handled=true;await Run(Save);}else if(command&&e.Key==Key.P){e.Handled=true;await Run(()=>Export(false));}};
        _timer.Tick+=async(_,_)=>{if(!_busy&&_identity.User is not null&&_workspace.IsOpen&&_workspace.IsDirty)await Run(async()=>{await _workspace.AutosaveAsync();if(!HasFormChanges)_status.Text="Wiederherstellungsstand aktualisiert. Hauptdatei noch nicht gespeichert.";});};_timer.Start();
    }
    private Button Button(string label, Func<Task> action, bool primary = false, string? help = null)
    {
        var button = new Button { Content = label, MinHeight = 38, Padding = new Thickness(13, 8), CornerRadius = new CornerRadius(6) };
        if (primary)
        {
            button.Background = Ui.Green;
            button.Foreground = Brushes.White;
        }
        button.Click += async (_, _) => await Run(action);
        return help is null ? button : Ui.Help(button, help);
    }
    private async Task Run(Func<Task> action)
    {
        if(_busy)return;_busy=true;if(_shell is not null)_shell.IsEnabled=false;
        try{await action();}
        catch(WorkspacePersistenceException ex)
        {
            _refreshAfterPersistence = true;
            _status.Text = "Hauptdatei noch nicht gespeichert. Bitte erneut speichern.";
            await Ui.Confirm(this, ex.Message, "Verstanden");
        }
        catch(Exception ex){var text=ex is CryptographicException?"Passwort falsch oder Datei beschädigt.":ex.Message;_status.Text="Fehler: "+text;await Ui.Confirm(this,text,"Verstanden");}
        finally{if(_shell is not null)_shell.IsEnabled=true;_busy=false;}
    }
    private void LoginScreen()
    {
        _shell=null;_body.Content=null;_form=null;_refreshAfterPersistence=false;_entries.Clear();_all.Clear();_sources.Clear();_allSources.Clear();_mappings.Clear();
        var name=Ui.Input("Lokaler Profilname");var password=Ui.Input("Profilpasswort");password.PasswordChar='*';
        var login=Button("Anmelden",async()=>{if(!await _identity.LoginAsync(name.Text??"",password.Text??""))throw new UnauthorizedAccessException("Anmeldung fehlgeschlagen.");password.Text="";BuildShell();await Render();},true);
        var register=Button("Profil erstellen",async()=>{
            var confirm=await Ui.Prompt(this,"Profilpasswort bestätigen (mindestens 12 Zeichen)",true);if(confirm is null)return;if(confirm!=password.Text)throw new ArgumentException("Passwörter stimmen nicht überein.");await _identity.RegisterAsync(name.Text??"",password.Text??"");
            if (!await _identity.LoginAsync(name.Text ?? "", password.Text ?? ""))
                throw new UnauthorizedAccessException("Das Profil wurde eingerichtet. Bitte melde dich damit an.");
            password.Text = "";
            BuildShell();
            await Render();
            await CreateProjectFromSetupAsync();
        }, help: "Legt ein lokales Profil an und startet den Assistenten für die verschlüsselte Projektdatei.");
        var logo = CreateLogo(58);
        logo.HorizontalAlignment = HorizontalAlignment.Left;
        var card=Ui.Card(Ui.Stack(logo,Ui.Heading("Bei D3 GRC anmelden",28,1),Ui.Secondary("Ihr lokaler Arbeitsbereich für Governance, Risiken und Compliance.",14),
            Ui.Field("Profilname", name, "Der Profilname wird im Änderungsjournal als lokale Identität angezeigt."),
            Ui.Field("Profilpasswort", password, "Ein neues Profilpasswort muss mindestens 12 Zeichen haben. Projektdateien haben ein separates Passwort."),
            Ui.Row(login,register),Ui.Secondary("Das Profilpasswort lässt sich nicht zurücksetzen. Ein neues Profil kann einen verlorenen lokalen Zugang ersetzen. Ihr Projektpasswort bleibt erforderlich."),Ui.Secondary(Ui.KeyboardHelp, 12)));
        card.Width=540;card.Padding=new Thickness(30);card.HorizontalAlignment=HorizontalAlignment.Center;card.VerticalAlignment=VerticalAlignment.Center;card.Margin=new Thickness(24);Content=card;
    }
    private void BuildShell()
    {
        // Fresh controls prevent reparenting errors and duplicate event subscriptions after logout/login.
        _body=new ContentControl();_status=Ui.Secondary("Angemeldet",12);_topic=Ui.Combo();_scope=Ui.Input("Genauer Geltungsbereich");_search=Ui.Input("Titel, Code, Beschreibung, Verantwortung");_context=Ui.Secondary("",13);
        _topicId="";_scopeValue="";_searchValue="";_page="Dashboard";_statusFilter="";
        _shell=new Grid{ColumnDefinitions=new ColumnDefinitions("214,*"),RowDefinitions=new RowDefinitions("Auto,*,Auto")};
        AddShellHeader();
        AddShellNavigation();
        var newProject = Button("Neues Projekt", NewProject, help: "Legt eine verschlüsselte Projektdatei mit einem eigenen Passwort an.");
        var openProject = Button("Öffnen", OpenProject, help: "Öffnet eine verschlüsselte .d3grc-Datei. Bei vorhandenem Wiederherstellungsstand erscheint eine Auswahl.");
        _projectOpeningActions = [newProject, openProject];
        _projectActions =
        [
            Button("Speichern", Save, true, "Übernimmt das bearbeitete Formular oder speichert die verschlüsselte Hauptdatei. Tastenkürzel Strg+S oder Cmd+S."),
            Button("Kopie speichern", SaveAs, help: "Erstellt eine neue verschlüsselte Projektkopie mit demselben Passwort."),
            Button("Schließen", CloseProject, help: "Speichert und schließt das Projekt. Das lokale Profil bleibt angemeldet."),
            Button("Unternehmen konfigurieren", OpenCompanyConfigurationAsync, help: "Bearbeitet die Stammdaten und Themen des geöffneten Projekts."),
            Button("Excel exportieren", () => Export(true), help: "Exportiert Thema, Geltungsbereich und Suche als unverschlüsselte Excel-Datei."),
            Button("Drucken / PDF", () => Export(false), help: "Erstellt eine unverschlüsselte HTML-Druckansicht. Im Browser drucken oder als PDF speichern.")
        ];
        var actions = Ui.Row(
            [.. _projectOpeningActions, .. _projectActions]);
        _topic.Width=195;_scope.Width=175;_search.Width=250;
        var filters = Ui.Row(
            Ui.Field("Thema", _topic, "Die Auswahl wirkt sofort. Zum Speichern eines Eintrags ein konkretes Thema auswählen."),
            Ui.Field("Geltungsbereich", _scope, "Filtert nach dem vollständigen Scope-Text. Ein leeres Feld zeigt alle Geltungsbereiche."),
            Ui.Field("Suche", _search, "Findet Teiltexte in Titel, Code, Beschreibung oder Verantwortung der Einträge."),
            Button("Filter anwenden", ApplyFilters, help: "Aktualisiert die Ansicht mit dem eingegebenen Geltungsbereich und Suchtext."),
            Button("Zurücksetzen",async()=>{if(!await CanLeaveFormAsync())return;_scope.Text="";_search.Text="";_scopeValue="";_searchValue="";_statusFilter="";await Render();}, help: "Leert Suche, Geltungsbereich und Listenstatus. Das gewählte Thema bleibt aktiv."));
        _projectActions = [.. _projectActions, filters];
        _topic.SelectionChanged += async (_, _) =>
        {
            if (_loading || _busy)
                return;
            var next = (_topic.SelectedItem as Choice)?.Id ?? "";
            await Run(async () =>
            {
                _topic.SelectedItem = _topics.FirstOrDefault(t => t.Id == _topicId);
                if (next == _topicId || !await CanLeaveFormAsync())
                    return;
                _topicId = next;
                _statusFilter = "";
                await Render();
            });
        };
        var commands = Ui.Card(Ui.Stack(_context, actions, filters));
        commands.Padding = new Thickness(14, 12);
        var main = Ui.Fill(commands, _body);
        main.Margin = new Thickness(18, 16, 18, 12);
        Grid.SetRow(main, 1);
        Grid.SetColumn(main, 1);
        _shell.Children.Add(main);
        var footerContent = new DockPanel();
        var keyboardHelp = Ui.Text(Ui.KeyboardHelp, 12);
        keyboardHelp.Margin = new Thickness(12, 0, 0, 0);
        DockPanel.SetDock(keyboardHelp, Dock.Right);
        footerContent.Children.Add(keyboardHelp);
        footerContent.Children.Add(_status);
        var footer = new Border
        {
            Background = Ui.Surface,
            BorderBrush = Ui.Line,
            BorderThickness = new Thickness(0, 1, 0, 0),
            Padding = new Thickness(16, 9),
            Child = footerContent
        };
        Grid.SetRow(footer,2);Grid.SetColumnSpan(footer,2);_shell.Children.Add(footer);Content=_shell;
    }

    private static Image CreateLogo(double height) => new()
    {
        Source = new Bitmap(Path.Combine(AppContext.BaseDirectory, "content", "logo.png")),
        Height = height,
        Stretch = Stretch.Uniform
    };

    private void AddShellHeader()
    {
        var profileLabel = Ui.Secondary(_identity.User ?? "");
        profileLabel.VerticalAlignment = VerticalAlignment.Center;
        var user = Ui.Row(profileLabel, Button("Abmelden", Logout,
            help: "Speichert das Projekt und kehrt zur Anmeldung zurück."));
        user.VerticalAlignment = VerticalAlignment.Center;
        var headerContent = new DockPanel();
        DockPanel.SetDock(user, Dock.Right);
        headerContent.Children.Add(user);
        var logo = CreateLogo(45);
        logo.HorizontalAlignment = HorizontalAlignment.Left;
        headerContent.Children.Add(logo);
        var header = new Border
        {
            Background = Ui.Surface,
            BorderBrush = Ui.Line,
            BorderThickness = new Thickness(0, 0, 0, 1),
            Padding = new Thickness(20, 12),
            Child = headerContent
        };
        Grid.SetColumnSpan(header, 2);
        _shell!.Children.Add(header);
    }

    private void AddShellNavigation()
    {
        _navigation.Clear();
        var nav = new StackPanel { Spacing = 3, Margin = new Thickness(10, 14) };
        foreach (var group in NavigationGroups)
        {
            var heading = Ui.Secondary(group.Title, 12);
            heading.Margin = new Thickness(8, 9, 0, 3);
            nav.Children.Add(heading);
            foreach (var page in group.Pages)
            {
                var item = Button(Pages[page], async () =>
                {
                    if (!await CanLeaveFormAsync())
                        return;
                    _page = page;
                    _statusFilter = "";
                    await Render();
                });
                item.HorizontalAlignment = HorizontalAlignment.Stretch;
                item.HorizontalContentAlignment = HorizontalAlignment.Left;
                item.MinHeight = 34;
                item.Padding = new Thickness(10, 6);
                _navigation.Add(page, item);
                nav.Children.Add(item);
            }
        }

        nav.Children.Add(Button("Arbeitskopien bereinigen", async () =>
        {
            if (await Ui.Confirm(this, "Verwaiste Klartext-Arbeitskopien endgültig löschen? Vorher Wiederherstellungsstände prüfen.", "Bereinigen"))
                _status.Text = Workspace.CleanupOrphans() + " Arbeitskopien entfernt.";
        }, help: "Entfernt unverschlüsselte Kopien aus beendeten Sitzungen."));

        var sidebar = new Border
        {
            Background = Ui.Surface,
            BorderBrush = Ui.Line,
            BorderThickness = new Thickness(0, 0, 1, 0),
            Child = Ui.Scroll(nav)
        };
        Grid.SetRow(sidebar, 1);
        _shell!.Children.Add(sidebar);
    }

    private void UpdateShellNavigation()
    {
        foreach (var action in _projectActions)
            action.IsVisible = _workspace.IsOpen;
        foreach (var action in _projectOpeningActions)
            action.IsVisible = !_workspace.IsOpen;
        foreach (var (page, item) in _navigation)
        {
            var active = _workspace.IsOpen && page == _page;
            item.IsEnabled = _workspace.IsOpen;
            item.Background = active ? Ui.SoftGreen : Brushes.Transparent;
            item.Foreground = active ? Ui.Green : Ui.Ink;
            item.FontWeight = active ? FontWeight.SemiBold : FontWeight.Normal;
        }
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
