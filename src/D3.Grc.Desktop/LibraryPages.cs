using System.Diagnostics;
using Avalonia.Controls;
using D3.Grc.Core;
namespace D3.Grc.Desktop;
public sealed partial class MainWindow
{
    private Control SourcePage()
    {
        var list=new ListBox{ItemsSource=_sources};Source? selected=null;var title=Ui.Text("Quelle auswählen",20,true);var original=Ui.Text("");var note=Ui.Input("Eigene fachliche Bewertung",true);var state=Ui.Text("");var history=new ListBox{MinHeight=180};Watch(note);
        SafeSelection(list,async obj=>{selected=obj as Source;if(selected is null){note.Text="";return;}title.Text=selected.Title;original.Text=selected.Reference+"\n"+selected.Url+"\n\n"+selected.Summary;note.Text=selected.Note;state.Text=selected.State+"\nLetzter technischer Erfolg: "+selected.LastSuccess;history.ItemsSource=await _workspace.SourceHistoryAsync(selected.Id);});
        async Task SaveNote(){if(selected is null)throw new ArgumentException("Quelle auswählen.");await _workspace.SaveSourceNoteAsync(selected.Id,note.Text??"");_dirty=false;await Render();}_saveForm=SaveNote;
        var check=Button("Quelle aktualisieren / prüfen",async()=>{if(selected is null)return;if(!await Discard())return;if(!await Ui.Confirm(this,"Offizielle Website direkt kontaktieren? URL und übliche Verbindungsdaten werden übertragen, keine Projektinhalte. Nur technischer Inhaltsvergleich.","Jetzt prüfen"))return;var result=await SourceUpdates.ProbeAsync(selected);await _workspace.StoreSourceResultAsync(result);await Render();_status.Text=result.Detail;},true);
        var accept=Button("Referenzstand bestätigen",async()=>{if(selected is null)return;if(!await Discard())return;var why=await Ui.Prompt(this,"Prüfbegründung; technische Bestätigung ersetzt keine Rechtsprüfung");if(why is null)return;await _workspace.AcceptSourceAsync(selected.Id,why);await Render();});
        var open=Button("Offizielle Quelle öffnen",()=>{if(selected is not null&&selected.Url!=""){var uri=new Uri(selected.Url);if(!SourceUpdates.Allowed(uri))throw new ArgumentException("Quelle nicht freigegeben.");Process.Start(new ProcessStartInfo(uri.AbsoluteUri){UseShellExecute=true});}return Task.CompletedTask;});
        var tabs=new TabControl{ItemsSource=new[]{Ui.Tab("Referenz",Ui.Scroll(Ui.Stack(title,original,state,Ui.Row(check,open),accept,Ui.Notice("Themenfilter begrenzt die Quellen. Änderungen überschreiben keine eigenen Texte. ISO-Normtexte sind nicht enthalten.")))),Ui.Tab("Eigene Bewertung",Ui.Scroll(Ui.Stack(note,Button("Bewertung speichern",SaveNote,true)))),Ui.Tab("Prüfhistorie",history)}};
        return Ui.Split(Ui.Card(Ui.Fill(Ui.Text(_sources.Count+" Quellen im Thema",18,true),list)),Ui.Card(tabs));
    }
    private async Task<Control> TemplatePage()
    {
        var templates=await _workspace.TemplatesAsync(_topicId);var list=new ListBox{ItemsSource=templates};TextTemplate? selected=null;var title=Ui.Text("Vorlage auswählen",20,true);var original=Ui.Input("Original",true);original.IsReadOnly=true;var edit=Ui.Input("Eigene Fassung",true);Watch(edit);
        SafeSelection(list,obj=>{selected=obj as TextTemplate;title.Text=selected?.Title??"Vorlage auswählen";original.Text=selected?.Body??"";edit.Text=selected?.Effective??"";return Task.CompletedTask;});
        async Task SaveTemplate(){if(selected is null)throw new ArgumentException("Vorlage auswählen.");await _workspace.SaveTemplateAsync(selected.Id,edit.Text??"");_dirty=false;await Render();}_saveForm=SaveTemplate;
        return Ui.Split(Ui.Card(Ui.Fill(Ui.Text("Themenbezogene Textvorlagen",18,true),list)),Ui.Card(Ui.Scroll(Ui.Stack(title,Ui.Notice("Eigene Fassung und Original bleiben getrennt. Bereits angelegte Datensätze werden nicht rückwirkend überschrieben."),Ui.Field("Original",original),Ui.Field("Eigene Fassung",edit),Button("Vorlage speichern",SaveTemplate,true)))));
    }
    private async Task<Control> GlossaryPage()
    {
        var rows=(await _workspace.GlossaryAsync()).Where(g=>_searchValue==""||g.Title.Contains(_searchValue,StringComparison.CurrentCultureIgnoreCase)).ToList();var list=new ListBox{ItemsSource=rows};Choice? selected=null;var edit=Ui.Input("Eigene Erläuterung",true);Watch(edit);
        SafeSelection(list,obj=>{selected=obj as Choice;edit.Text=selected?.Title.Split('\n',2).LastOrDefault()??"";return Task.CompletedTask;});
        async Task SaveGlossary(){if(selected is null)return;await _workspace.SaveGlossaryAsync(selected.Id,edit.Text??"");_dirty=false;await Render();}_saveForm=SaveGlossary;
        var print=Button("Glossar drucken",async()=>{if(!await Ui.Confirm(this,"Gefiltertes Glossar als Klartext-Druckansicht speichern?","Erstellen"))return;var path=await SavePath("D3-GRC-Glossar.html","html");if(path is null)return;var sheets=new[]{new ExportSheet("Glossar",["Begriff und Definition"],rows.Select(g=>new[]{g.Title}).ToList())};await Files.AtomicAsync(path,System.Text.Encoding.UTF8.GetBytes(ExcelExport.Html("Glossar",sheets)));await _workspace.RecordExportAsync(path,"","","Glossar HTML");Process.Start(new ProcessStartInfo(new Uri(path).AbsoluteUri){UseShellExecute=true});});
        return Ui.Split(Ui.Card(Ui.Fill(Ui.Text(rows.Count+" Begriffe",18,true),list,print)),Ui.Card(Ui.Stack(Ui.Notice("Kurze Arbeitserläuterungen, keine verbindlichen Legaldefinitionen. Das Glossar ist themenübergreifend."),edit,Button("Definition speichern",SaveGlossary,true))));
    }
    private async Task<Control> JournalPage()=>Ui.Card(Ui.Fill(Ui.Stack(Ui.Notice("Lokales Profil, keine verifizierte Organisationsidentität. Die Hashkette ist nicht extern verankert und keine revisionssichere Signatur."),Button("Hashkette prüfen",async()=>{_status.Text=await _workspace.VerifyAsync()?"Hashkette konsistent.":"Hashkette inkonsistent!";})),new ListBox{ItemsSource=await _workspace.JournalAsync()}));
}
