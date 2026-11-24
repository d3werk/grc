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
    private ComboBox _topic=Ui.Combo(),_scope=Ui.Combo();private TextBox _search=Ui.Input("Suche: Titel, Code, Beschreibung, Owner");
    private TextBlock _context=Ui.Text("",18,true);private readonly DispatcherTimer _timer=new(){Interval=TimeSpan.FromSeconds(30)};
    private Grid? _shell;private bool _busy,_loading,_allowClose;private string _page="Dashboard",_topicId="",_scopeValue="",_searchValue="",_statusFilter="";
    private List<Choice> _topics=[];private List<Entry> _entries=[];private List<Entry> _all=[];private List<Source> _sources=[];private List<Source> _allSources=[];private List<Mapping> _mappings=[];
    public MainWindow(LocalIdentity identity,Workspace workspace)
    {
        _identity=identity;_workspace=workspace;Title=$"D3 GRC {AppVersion} · Governance, Risiken und Compliance";Width=1440;Height=950;MinWidth=1040;MinHeight=700;Background=Ui.Canvas;FontSize=14;
        Opened+=(_,_)=>LoginScreen();
        Closing+=async(_,e)=>{if(_allowClose)return;e.Cancel=true;if(_busy)return;await Run(async()=>{if(!await CanLeaveFormAsync())return;await _workspace.LogoutAsync();_allowClose=true;_timer.Stop();Close();});};
        KeyDown+=async(_,e)=>{if(!_workspace.IsOpen)return;var command=e.KeyModifiers.HasFlag(KeyModifiers.Control)||e.KeyModifiers.HasFlag(KeyModifiers.Meta);if(command&&e.Key==Key.S){e.Handled=true;await Run(Save);}else if(command&&e.Key==Key.P){e.Handled=true;await Run(()=>Export(false));}};
        _timer.Tick += async (_, _) =>
        {
            if (_busy || _identity.User is null || !_workspace.IsOpen)
                return;
            if (_workspace.IsDirty)
                await Run(async () =>
                {
                    await _workspace.AutosaveAsync();
                    if (!HasFormChanges)
                        _status.Text = "Wiederherstellungsstand aktualisiert. Hauptdatei noch nicht gespeichert.";
                });
            try { await UpdateControlReminderAsync(); }
            catch (Exception ex) when (ex is IOException or InvalidOperationException)
            {
                if (_workspace.IsOpen && !HasFormChanges)
                    _status.Text = "Kontrolltermine konnten gerade nicht aktualisiert werden.";
            }
        };
        _timer.Start();
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
        _activeTenantId = null;
        _tenants = [];
        _shell=null;_body.Content=null;_form=null;_refreshAfterPersistence=false;_entries.Clear();_all.Clear();_sources.Clear();_allSources.Clear();_mappings.Clear();
        var name=Ui.Input("Lokaler Profilname");var password=Ui.Input("Profilpasswort");password.PasswordChar='*';
        var login=Button("Anmelden",async()=>{if(!await _identity.LoginAsync(name.Text??"",password.Text??""))throw new UnauthorizedAccessException("Anmeldung fehlgeschlagen.");password.Text="";await StartProfileAsync();},true,glyph:Glyph.Arrow);
        login.IsDefault = true;
        var register=Button("Profil erstellen",async()=>{
            var confirm=await Ui.Prompt(this,"Profilpasswort bestätigen (mindestens 12 Zeichen)",true);if(confirm is null)return;if(confirm!=password.Text)throw new ArgumentException("Passwörter stimmen nicht überein.");await _identity.RegisterAsync(name.Text??"",password.Text??"");
            if (!await _identity.LoginAsync(name.Text ?? "", password.Text ?? ""))
                throw new UnauthorizedAccessException("Das Profil wurde eingerichtet. Bitte melde dich damit an.");
            password.Text = "";
            await StartProfileAsync();
        }, help: "Legt ein lokales Profil an und startet den Assistenten für den lokal gespeicherten Mandanten.", glyph: Glyph.Person);
        var logo = CreateLogo(43);
        logo.HorizontalAlignment = HorizontalAlignment.Left;
        var card=Ui.Card(Ui.Stack(logo,Ui.Heading("Bei D3 GRC anmelden",28,1),Ui.Secondary("Anmeldung mit einem lokalen Profil.",14),
            Ui.Field("Profilname", name, "Der Profilname wird im Änderungsjournal als lokale Identität angezeigt."),
            Ui.Field("Profilpasswort", password, "Ein neues Profilpasswort muss mindestens 12 Zeichen haben. Es öffnet auch Ihre intern verwalteten Mandanten."),
            Ui.Row(login,register),Ui.Secondary("Das Profilpasswort schützt Ihren lokalen Mandantenspeicher. Für übertragbare Sicherungen erhalten Sie einen separaten Sicherungsschlüssel."),Ui.Secondary(Ui.KeyboardHelp, 12)));
        card.Width=500;card.Padding=new Thickness(30);card.HorizontalAlignment=HorizontalAlignment.Center;card.VerticalAlignment=VerticalAlignment.Center;card.Margin=new Thickness(24);
        Content = new Border
        {
            Background = Ui.BrandWash,
            Child = card
        };
    }
    private async Task ApplyFilters()
    {
        var scope = IsProjectWidePrivacy ? _scopeValue : (_scope.SelectedItem as Choice)?.Id ?? "";
        var search = _search.Text?.Trim() ?? "";
        if (scope == _scopeValue && search == _searchValue)
            return;
        if (!await CanLeaveFormAsync())
        {
            if (!IsProjectWidePrivacy && _scope.ItemsSource is IEnumerable<Choice> options)
                _scope.SelectedItem = options.FirstOrDefault(option => option.Id == _scopeValue);
            return;
        }
        _scopeValue = scope;
        _searchValue = search;
        await Render();
    }

    private bool IsProjectWidePrivacy => _topicId == "PRIVACY";
    private string EffectiveScope => IsProjectWidePrivacy ? "" : _scopeValue;

    private async Task<bool> RefreshScopeOptionsAsync()
    {
        if (IsProjectWidePrivacy)
        {
            _scope.ItemsSource = new[] { new Choice("", "Projektweit") };
            _scope.SelectedIndex = 0;
            _scope.IsEnabled = false;
            return false;
        }

        var scopes = await _workspace.ScopeOptionsAsync();
        var scopeRemoved = _scopeValue != "" && !scopes.Contains(_scopeValue, StringComparer.Ordinal);
        if (scopeRemoved)
            _scopeValue = "";
        var options = new[] { new Choice("", "Alle Bereiche") }.Concat(scopes
            .OrderBy(scope => scope, StringComparer.CurrentCultureIgnoreCase)
            .ThenBy(scope => scope, StringComparer.Ordinal)
            .Select(scope => new Choice(scope, scope))).ToList();
        _scope.ItemsSource = options;
        _scope.SelectedItem = options.First(option => option.Id == _scopeValue);
        _scope.IsEnabled = true;
        return scopeRemoved;
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
                _scope.ItemsSource = null;
                _body.Content = Ui.Card(Ui.Stack(
                    Ui.Heading("Mandant auswählen oder anlegen", 24, 1),
                    Ui.Secondary("Wählen Sie oben einen Mandanten oder legen Sie einen neuen im Unternehmensassistenten an.", 14),
                    Button("Demo-Unternehmen öffnen", OpenDemoTenantAsync, true, glyph: Glyph.Open)));
                _context.Text = "Kein Projekt geöffnet";
                _status.Text = "Profil angemeldet";
                UpdateShellNavigation();
                return;
            }

            _topics = new[] { new Choice("", "Alle Themen") }.Concat(await _workspace.TopicsAsync()).ToList();
            _topic.ItemsSource = _topics;
            _topic.SelectedItem = _topics.FirstOrDefault(topic => topic.Id == _topicId) ?? _topics[0];
            await RefreshScopeOptionsAsync();
            _all = await _workspace.EntriesAsync();
            var contextEntries = _topicId == "" && EffectiveScope == ""
                ? _all
                : await _workspace.EntriesAsync(_topicId, "", EffectiveScope);
            _entries = contextEntries.Where(Matches).ToList();
            _sources = await _workspace.SourcesAsync(_topicId);
            _allSources = _topicId == "" ? _sources : await _workspace.SourcesAsync();
            _mappings = await _workspace.MappingsAsync();
            _body.Content = _page switch
            {
                "Dashboard" => await Dashboard(),
                "ProcessOverview" => await ProcessOverviewPage(),
                "InternalAudit" => await InternalAuditOverviewPage(),
                "InternalControl" => await InternalControlPage(),
                "ControlReviews" => await ControlReviewsPage(),
                "Nis2" => await Nis2Page(),
                "ResilienceOverview" => await ResilienceOverviewPage(),
                "ContinuityOverview" => await ContinuityOverviewPage(),
                "AiOverview" => await AiOverviewPage(),
                "ContractsOverview" => await ContractsOverviewPage(),
                "Documents" => await DocumentsPage(),
                "RiskMatrix" => await RiskMatrixPage(),
                "Mappings" => await MappingPage(),
                "Evidence" => await EvidencePage(),
                "Sources" => SourcePage(),
                "Templates" => await TemplatePage(),
                "Glossary" => await GlossaryPage(),
                "Journal" => await JournalPage(),
                _ => await EntryPage(_page)
            };
            var tenantName = _tenants.FirstOrDefault(tenant => tenant.Id == _activeTenantId)?.Name ?? _workspace.Name;
            _context.Text = $"{tenantName} / {(_topic.SelectedItem as Choice)?.Title} / {Pages[_page]}";
            UpdateShellNavigation();
            await UpdateControlReminderAsync();
            UpdateFormStatus();
        }
        catch
        {
            _form = previousForm;
            throw;
        }
        finally { _loading = false; }
    }
    private bool Matches(Entry e)=>_searchValue==""||new[]{e.Title,e.Code,RichTextContent.Plain(e.Body),e.Owner}.Any(s=>s.Contains(_searchValue,StringComparison.CurrentCultureIgnoreCase));
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
    private async Task CloseProject()
    {
        if (!await CanLeaveFormAsync())
            return;
        if (_workspace.IsOpen)
        {
            if (_workspace.IsDirty)
                await _workspace.SaveAsync();
            await _workspace.CloseAsync();
        }
        _activeTenantId = null;
        SelectActiveTenant();
        _refreshAfterPersistence = false;
        ResetProjectContext();
        await Render();
    }
    private Task NewProject() => CreateProjectFromSetupAsync();
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
        _showResidualRiskMatrix = false;
        _nis2PackageKey = "21a";
        _nis2Scope = "";
        _scope.ItemsSource = null;
        _search.Text = "";
    }
    private Task SaveAs() => ExportTenantsAsync(all: false);
    private async Task<string?> SavePath(string name,string extension){var file=await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions{SuggestedFileName=name,DefaultExtension=extension});return file?.TryGetLocalPath();}
    private async Task Export(bool excel)
    {
        if (!_workspace.IsOpen)
            throw new InvalidOperationException("Projekt öffnen.");
        var draftNotice = HasFormChanges ? " Ungespeicherte Formularänderungen bleiben in der Bearbeitung." : "";
        var scopeLabel = IsProjectWidePrivacy ? "Projektweit" : EffectiveScope == "" ? "Alle Geltungsbereiche" : EffectiveScope;
        if(!await Ui.Confirm(this,$"Exportiert werden das Thema '{(_topic.SelectedItem as Choice)?.Title}', Geltungsbereich '{scopeLabel}' und Suche '{_searchValue}'. Der Export enthält die übernommenen Objekte im Kontext als Klartext und verwendet keine lokalen Listenstatusfilter.{draftNotice}","Exportieren"))return;
        var sheets=await _workspace.ExportSnapshotAsync(_topicId,EffectiveScope,_searchValue);var path=await SavePath(excel?"D3-GRC-Bericht.xlsx":"D3-GRC-Druckansicht.html",excel?"xlsx":"html");if(path is null)return;
        await Files.AtomicAsync(path,excel?ExcelExport.Build(sheets):Encoding.UTF8.GetBytes(ExcelExport.Html(_workspace.Name,sheets)));await _workspace.RecordExportAsync(path,_topicId,EffectiveScope,excel?"xlsx":"html");
        if(!excel)Process.Start(new ProcessStartInfo(new Uri(path).AbsoluteUri){UseShellExecute=true});_status.Text="Export erfolgreich geschrieben (Klartext).";
    }
    private ComboBox SourceCombo(IEnumerable<Source>? sources=null){var box=Ui.Combo();box.ItemsSource=(sources??_sources).ToList();box.SelectedIndex=0;return box;}
    private static string Sid(ComboBox box)=>(box.SelectedItem as Source)?.Id??throw new ArgumentException("Quelle auswählen.");
}
