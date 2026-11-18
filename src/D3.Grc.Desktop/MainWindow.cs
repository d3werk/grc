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
    private ContentControl _body=new();private TextBlock _status=Ui.Text("Entwicklungsstand 0.6 · noch keine Produktionsfreigabe",12);
    private ComboBox _topic=Ui.Combo();private TextBox _scope=Ui.Input("Scope exakt filtern (leer = alle)"),_search=Ui.Input("Suche: Titel, Code, Beschreibung, Owner");
    private TextBlock _context=Ui.Text("",18,true);private readonly DispatcherTimer _timer=new(){Interval=TimeSpan.FromSeconds(30)};
    private Grid? _shell;private bool _busy,_loading,_dirty,_allowClose;private Func<Task>? _saveForm;private string _page="Dashboard",_topicId="",_scopeValue="",_searchValue="",_statusFilter="";
    private List<Choice> _topics=[];private List<Entry> _entries=[];private List<Entry> _all=[];private List<Source> _sources=[];private List<Source> _allSources=[];private List<Mapping> _mappings=[];
    private static readonly Dictionary<string,string> Pages=new(){["Dashboard"]="Übersicht",["Requirement"]="Anforderungen",["Control"]="Controls",["Risk"]="Risiken",["Mappings"]="Mappings",["Evidence"]="Evidence",["Audit"]="Audits",["Finding"]="Findings",["Measure"]="Maßnahmen",["AiSystem"]="KI-Systeme",["OssComponent"]="Open Source",["Sources"]="Quellen",["Templates"]="Textvorlagen",["Glossary"]="Glossar",["Journal"]="Aktivitäten"};
    public MainWindow(LocalIdentity identity,Workspace workspace)
    {
        _identity=identity;_workspace=workspace;Title="D3 GRC 0.6 · Datenschutz und Informationssicherheit";Width=1500;Height=1000;MinWidth=1200;MinHeight=800;Background=Brush.Parse("#F4F6F5");FontSize=14;
        Opened+=(_,_)=>LoginScreen();
        Closing+=async(_,e)=>{if(_allowClose)return;e.Cancel=true;if(_busy)return;await Run(async()=>{if(!await Discard())return;if(_workspace.IsOpen)await _workspace.SaveAsync();await _workspace.LogoutAsync();_allowClose=true;_timer.Stop();Close();});};
        KeyDown+=async(_,e)=>{var command=e.KeyModifiers.HasFlag(KeyModifiers.Control)||e.KeyModifiers.HasFlag(KeyModifiers.Meta);if(command&&e.Key==Key.S){e.Handled=true;await Run(Save);}else if(command&&e.Key==Key.P){e.Handled=true;await Run(()=>Export(false));}};
        _timer.Tick+=async(_,_)=>{if(!_busy&&_identity.User is not null&&_workspace.IsOpen&&_workspace.IsDirty)await Run(async()=>{await _workspace.AutosaveAsync();_status.Text="Nur Recovery aktualisiert; Hauptdatei noch nicht gespeichert.";});};_timer.Start();
    }
    private Button Button(string label,Func<Task> action,bool primary=false)
    {var b=new Button{Content=label,MinHeight=38,Padding=new Thickness(13,8),CornerRadius=new CornerRadius(6)};if(primary){b.Background=Ui.Green;b.Foreground=Brushes.White;}b.Click+=async(_,_)=>await Run(action);return b;}
    private async Task Run(Func<Task> action)
    {
        if(_busy)return;_busy=true;if(_shell is not null)_shell.IsEnabled=false;
        try{await action();}catch(Exception ex){var text=ex is CryptographicException?"Passwort falsch oder Datei beschädigt.":ex.Message;_status.Text="Fehler: "+text;await Ui.Confirm(this,text,"Verstanden");}
        finally{if(_shell is not null)_shell.IsEnabled=true;_busy=false;}
    }
    private void Watch(TextBox box)=>box.TextChanged+=(_,_)=>{if(!_loading){_dirty=true;_status.Text="Nicht gespeicherte Formularänderungen";}};
    private void Watch(ComboBox box)=>box.SelectionChanged+=(_,_)=>{if(!_loading){_dirty=true;_status.Text="Nicht gespeicherte Formularänderungen";}};
    private async Task<bool> Discard(){if(!_dirty)return true;if(!await Ui.Confirm(this,"Nicht gespeicherte Formularänderungen verwerfen?","Verwerfen"))return false;_dirty=false;return true;}
    private void LoginScreen()
    {
        _shell=null;_body.Content=null;_saveForm=null;_entries.Clear();_all.Clear();_sources.Clear();_allSources.Clear();_mappings.Clear();_dirty=false;
        var name=Ui.Input("Lokaler Profilname");var password=Ui.Input("Profilpasswort");password.PasswordChar='*';
        var login=Button("Anmelden",async()=>{if(!await _identity.LoginAsync(name.Text??"",password.Text??""))throw new UnauthorizedAccessException("Anmeldung fehlgeschlagen.");password.Text="";BuildShell();await Render();},true);
        var register=Button("Registrieren / lokales Profil einrichten",async()=>{
            var confirm=await Ui.Prompt(this,"Profilpasswort bestätigen (mindestens 12 Zeichen)",true);if(confirm is null)return;if(confirm!=password.Text)throw new ArgumentException("Passwörter stimmen nicht überein.");await _identity.RegisterAsync(name.Text??"",password.Text??"");password.Text="";await Ui.Confirm(this,"Lokales Profil eingerichtet. Jetzt anmelden. Es wurde kein Onlinekonto erstellt.","OK");
        });
        var logo=new Image{Source=new Bitmap(Path.Combine(AppContext.BaseDirectory,"content","logo.png")),Height=65,Stretch=Stretch.Uniform};
        var card=Ui.Card(Ui.Stack(logo,Ui.Text("D3 GRC · Anmeldung",26,true),Ui.Notice("Lokales Profil auf diesem Rechner. Kein Cloudkonto, keine E-Mail-Registrierung und keine serverseitigen Teamrechte. Projektpasswort wird separat abgefragt."),Ui.Field("Profilname",name),Ui.Field("Profilpasswort",password),Ui.Row(login,register),Ui.Text("Kein Passwort-Reset. Bei Profilverlust kann ein neues lokales Profil eingerichtet werden; Projektdateien bleiben durch ihr eigenes Passwort geschützt.",12)));
        card.MaxWidth=700;card.HorizontalAlignment=HorizontalAlignment.Center;card.VerticalAlignment=VerticalAlignment.Center;card.Margin=new Thickness(24);Content=card;
    }
    private void BuildShell()
    {
        // Fresh controls prevent reparenting errors and duplicate event subscriptions after logout/login.
        _body=new ContentControl();_status=Ui.Text("Angemeldet",12);_topic=Ui.Combo();_scope=Ui.Input("Scope exakt filtern (leer = alle)");_search=Ui.Input("Suche: Titel, Code, Beschreibung, Owner");_context=Ui.Text("",18,true);
        _topicId="";_scopeValue="";_searchValue="";_page="Dashboard";_statusFilter="";
        _shell=new Grid{ColumnDefinitions=new ColumnDefinitions("210,*"),RowDefinitions=new RowDefinitions("Auto,*,Auto")};
        var header=Ui.Row(new Image{Source=new Bitmap(Path.Combine(AppContext.BaseDirectory,"content","logo.png")),Width=390,Height=65,Stretch=Stretch.Uniform},Ui.Text("Profil: "+_identity.User,14,true),Button("Sitzung sperren",Logout),Button("Logout / Abmelden",Logout));header.Margin=new Thickness(16);Grid.SetColumnSpan(header,2);_shell.Children.Add(header);
        var nav=Ui.Stack(Ui.Text("ARBEITSBEREICH",12,true));foreach(var p in Pages){var key=p.Key;nav.Children.Add(Button(p.Value,async()=>{if(!await Discard())return;_page=key;_statusFilter="";await Render();}));}nav.Children.Add(Button("Arbeitskopien bereinigen",async()=>{if(await Ui.Confirm(this,"Erst Recovery prüfen. Verwaiste Klartext-Arbeitskopien endgültig löschen?","Bereinigen"))_status.Text=Workspace.CleanupOrphans()+" Arbeitskopien entfernt.";}));nav.Margin=new Thickness(12);var side=Ui.Scroll(nav);Grid.SetRow(side,1);_shell.Children.Add(side);
        var actions=Ui.Row(Button("Neues Projekt",NewProject),Button("Öffnen",OpenProject),Button("Speichern",Save,true),Button("Speichern unter",SaveAs),Button("Projekt schließen",CloseProject),Button("Excel exportieren",()=>Export(true)),Button("Drucken / PDF",()=>Export(false)));
        _topic.MinWidth=250;_scope.Width=230;_search.Width=300;
        var filters=Ui.Row(Ui.Field("Thema",_topic),Ui.Field("Geltungsbereich",_scope),Ui.Field("Suche",_search),Button("Filter anwenden",ApplyFilters),Button("Zurücksetzen",async()=>{if(!await Discard())return;_scope.Text="";_search.Text="";_scopeValue="";_searchValue="";_statusFilter="";await Render();}));
        _topic.SelectionChanged+=async(_,_)=>{if(_loading||_busy)return;await Run(async()=>{var next=(_topic.SelectedItem as Choice)?.Id??"";if(!await Discard()){_loading=true;_topic.SelectedItem=_topics.FirstOrDefault(t=>t.Id==_topicId);_loading=false;return;}_topicId=next;_statusFilter="";await Render();});};
        var top=Ui.Stack(_context,actions,filters);var main=Ui.Fill(top,_body);main.Margin=new Thickness(20,8,20,16);Grid.SetRow(main,1);Grid.SetColumn(main,1);_shell.Children.Add(main);var footer=Ui.Card(_status);Grid.SetRow(footer,2);Grid.SetColumnSpan(footer,2);_shell.Children.Add(footer);Content=_shell;
    }
    private async Task ApplyFilters(){if(!await Discard())return;_scopeValue=_scope.Text?.Trim()??"";_searchValue=_search.Text?.Trim()??"";await Render();}
    private async Task Render()
    {
        _loading=true;_saveForm=null;try{
            if(!_workspace.IsOpen){_body.Content=Ui.Card(Ui.Stack(Ui.Text("Projekt öffnen oder anlegen",24,true),Ui.Text("Anmeldung und Projektentsperrung sind getrennt. Bestehende 0.5-Dateien werden auf einer Arbeitskopie migriert; vorab wird ein verschlüsseltes Backup erstellt."),Ui.Notice("Entwicklungsstand. Native Kompilation und Plattformabnahme noch offen. Aktive SQLite-Arbeitskopie ist Klartext; Datenträgerverschlüsselung verwenden.")));_context.Text="Kein Projekt geöffnet · Profil "+_identity.User;return;}
            _topics=new[]{new Choice("","Alle Themen")}.Concat(await _workspace.TopicsAsync()).ToList();_topic.ItemsSource=_topics;_topic.SelectedItem=_topics.FirstOrDefault(t=>t.Id==_topicId)??_topics[0];
            _all=await _workspace.EntriesAsync();_entries=(await _workspace.EntriesAsync(_topicId,"",_scopeValue)).Where(Matches).ToList();_sources=await _workspace.SourcesAsync(_topicId);_allSources=await _workspace.SourcesAsync();_mappings=await _workspace.MappingsAsync();
            _context.Text=$"{_workspace.Name} / {(_topic.SelectedItem as Choice)?.Title} / {Pages[_page]}";
            _body.Content=_page switch{"Dashboard"=>await Dashboard(),"Mappings"=>await MappingPage(),"Evidence"=>await EvidencePage(),"Sources"=>SourcePage(),"Templates"=>await TemplatePage(),"Glossary"=>await GlossaryPage(),"Journal"=>await JournalPage(),_=>await EntryPage(_page)};
            _status.Text=_workspace.IsDirty?"Recovery gesichert; Hauptdatei noch nicht aktualisiert.":"Alle übernommenen Änderungen gespeichert · "+_workspace.ProjectPath;
        }finally{_loading=false;_dirty=false;}
    }
    private bool Matches(Entry e)=>_searchValue==""||new[]{e.Title,e.Code,e.Body,e.Owner}.Any(s=>s.Contains(_searchValue,StringComparison.CurrentCultureIgnoreCase));
    private async Task Save(){if(_dirty&&_saveForm is not null){await _saveForm();return;}if(_dirty)throw new InvalidOperationException("Formular zuerst über seine Speicheraktion übernehmen.");await _workspace.SaveAsync();_status.Text="Alle übernommenen Änderungen gespeichert.";}
    private async Task Logout(){if(!await Discard())return;await _workspace.LogoutAsync();LoginScreen();}
    private async Task CloseProject(){if(!await Discard())return;if(_workspace.IsOpen){await _workspace.SaveAsync();await _workspace.CloseAsync();}await Render();}
    private async Task NewProject()
    {
        if(_workspace.IsOpen)throw new InvalidOperationException("Aktuelles Projekt zuerst schließen.");var name=await Ui.Prompt(this,"Projektname");if(name is null)return;var password=await Ui.Prompt(this,"Projektpasswort (mindestens 12 Zeichen)",true);if(password is null)return;var confirm=await Ui.Prompt(this,"Projektpasswort bestätigen",true);if(confirm is null)return;if(password!=confirm)throw new ArgumentException("Passwörter stimmen nicht überein.");
        var path=await SavePath("organisation.d3grc","d3grc");if(path is null)return;await _workspace.CreateAsync(path,name,password);_page="Dashboard";await Render();
    }
    private async Task OpenProject()
    {
        if(_workspace.IsOpen)throw new InvalidOperationException("Aktuelles Projekt zuerst schließen.");var files=await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions{AllowMultiple=false,FileTypeFilter=[new("D3 GRC"){Patterns=["*.d3grc"]}]});var path=files.FirstOrDefault()?.TryGetLocalPath();if(path is null)return;
        var recovery=File.Exists(path+".recovery")&&await Ui.Confirm(this,"Recovery vorhanden. Wiederherstellen? Abbrechen öffnet die Hauptdatei.","Wiederherstellen");var password=await Ui.Prompt(this,"Projektpasswort",true);if(password is null)return;await _workspace.OpenAsync(path,password,recovery);_page="Dashboard";await Render();
    }
    private async Task SaveAs(){if(_dirty){if(_saveForm is null)throw new InvalidOperationException("Formular zuerst speichern.");await _saveForm();if(_dirty)return;}var path=await SavePath("organisation-kopie.d3grc","d3grc");if(path is null)return;await _workspace.SaveAsAsync(path);_status.Text="Neue Projektkopie mit gleichem Passwort gespeichert. Alte Datei bleibt erhalten.";}
    private async Task<string?> SavePath(string name,string extension){var file=await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions{SuggestedFileName=name,DefaultExtension=extension});return file?.TryGetLocalPath();}
    private async Task Export(bool excel)
    {
        if(!await Discard())return;if(!await Ui.Confirm(this,$"Exportiert werden das Thema '{(_topic.SelectedItem as Choice)?.Title}', Scope '{_scopeValue}' und Suche '{_searchValue}'. Alle Objekttypen im Kontext werden exportiert; lokale Statusfilter der Liste nicht. Der Export enthält Klartextdaten. Fortfahren?","Exportieren"))return;
        var sheets=await _workspace.ExportSnapshotAsync(_topicId,_scopeValue,_searchValue);var path=await SavePath(excel?"D3-GRC-Bericht.xlsx":"D3-GRC-Druckansicht.html",excel?"xlsx":"html");if(path is null)return;
        await Files.AtomicAsync(path,excel?ExcelExport.Build(sheets):Encoding.UTF8.GetBytes(ExcelExport.Html(_workspace.Name,sheets)));await _workspace.RecordExportAsync(path,_topicId,_scopeValue,excel?"xlsx":"html");
        if(!excel)Process.Start(new ProcessStartInfo(new Uri(path).AbsoluteUri){UseShellExecute=true});_status.Text="Export erfolgreich geschrieben (Klartext).";
    }
    private ComboBox SourceCombo(IEnumerable<Source>? sources=null){var box=Ui.Combo();box.ItemsSource=(sources??_sources).ToList();box.SelectedIndex=0;return box;}
    private static string Sid(ComboBox box)=>(box.SelectedItem as Source)?.Id??throw new ArgumentException("Quelle auswählen.");
    private void SafeSelection(ListBox list,Func<object?,Task> load)
    {
        object? previous=null;bool guard=false;list.SelectionChanged+=async(_,_)=>{if(guard||_loading||_busy)return;await Run(async()=>{if(!await Discard()){guard=true;list.SelectedItem=previous;guard=false;return;}_loading=true;try{await load(list.SelectedItem);previous=list.SelectedItem;}finally{_loading=false;_dirty=false;}});};
    }
}
