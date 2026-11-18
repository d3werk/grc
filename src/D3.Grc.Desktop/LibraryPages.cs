using System.Diagnostics;
using Avalonia.Controls;
using Avalonia.Input.Platform;
using D3.Grc.Core;
namespace D3.Grc.Desktop;
public sealed partial class MainWindow
{
    private Control SourcePage()
    {
        var list = new ListBox { ItemsSource = _sources };
        var title = Ui.Text("Quelle auswählen", 20, true);
        var original = Ui.Text("");
        var state = Ui.Text("");
        var note = Ui.Input("Eigene fachliche Bewertung", true);
        var history = new ListBox { MinHeight = 180 };
        var savedNotes = new Dictionary<string, string>(StringComparer.Ordinal);
        Source? selected = null;

        var add = Button("Eigene Quelle hinzufügen", () => EditCustomSourceAsync(null), true,
            "Interne Vorgaben, Verträge und weitere Referenzen selbst anlegen und einem oder mehreren Themen zuordnen.");
        var edit = Button("Quelle bearbeiten", () => EditCustomSourceAsync(selected),
            help: "Titel, Adresse, Fundstelle, Beschreibung und Themenzuordnung der eigenen Quelle ändern.");
        var copy = Button("Adresse kopieren", async () =>
        {
            if (selected is not { IsCustom: true } source || source.Url.Length == 0)
                return;
            var clipboard = Clipboard ?? throw new InvalidOperationException("Die Zwischenablage ist nicht verfügbar.");
            await ClipboardExtensions.SetTextAsync(clipboard, source.Url);
            _status.Text = "Adresse in die Zwischenablage kopiert.";
        }, help: "Kopiert die Adresse der eigenen Quelle zur manuellen Verwendung.");
        var check = Button("Quelle aktualisieren / prüfen", async () =>
        {
            if (selected is not { IsCustom: false } source || !await CanLeaveFormAsync())
                return;
            if (!await Ui.Confirm(this, "Offizielle Website direkt kontaktieren? URL und übliche Verbindungsdaten werden übertragen, keine Projektinhalte. Nur technischer Inhaltsvergleich.", "Jetzt prüfen"))
                return;
            var result = await SourceUpdates.ProbeAsync(source);
            await _workspace.StoreSourceResultAsync(result);
            await Render();
            _status.Text = result.Detail;
        }, true, "Ruft die offizielle Seite nach Bestätigung ab und vergleicht den Inhalt mit dem letzten technischen Stand.");
        var accept = Button("Referenzstand bestätigen", async () =>
        {
            if (selected is not { IsCustom: false } source || !await CanLeaveFormAsync())
                return;
            var why = await Ui.Prompt(this, "Prüfbegründung; technische Bestätigung ersetzt keine Rechtsprüfung");
            if (why is null)
                return;
            await _workspace.AcceptSourceAsync(source.Id, why);
            await Render();
        }, help: "Dokumentiert die begründete Bestätigung des zuletzt abgerufenen technischen Stands.");
        var open = Button("Offizielle Quelle öffnen", () =>
        {
            var uri = OfficialSourceUri(selected);
            if (uri is not null)
                Process.Start(new ProcessStartInfo(uri.AbsoluteUri) { UseShellExecute = true });
            return Task.CompletedTask;
        });
        var saveNote = Button("Bewertung speichern", SaveEditorButtonAsync, true);

        void PresentSource(Source? source)
        {
            title.Text = source?.Title ?? "Quelle auswählen";
            original.Text = source is null ? "" :
                string.Join("\n\n", new[] { source.Reference, source.Url, source.Summary }.Where(text => text.Length > 0));
            state.Text = source is null ? "" : source.LastSuccess.Length == 0 ?
                source.State : source.State + "\nLetzter technischer Erfolg: " + source.LastSuccess;
            note.IsEnabled = saveNote.IsEnabled = source is not null;
            note.Text = source is null ? "" : savedNotes.GetValueOrDefault(source.Id, source.Note);
            edit.IsVisible = source?.IsCustom == true;
            copy.IsVisible = source is { IsCustom: true } && source.Url.Length > 0;
            check.IsVisible = open.IsVisible = OfficialSourceUri(source) is not null;
            accept.IsVisible = check.IsVisible && source!.LastSuccess.Length > 0;
        }

        async Task<bool> SaveNote()
        {
            var source = selected ?? throw new ArgumentException("Quelle auswählen.");
            var text = note.Text ?? "";
            return await CommitFormChangeAsync(() => _workspace.SaveSourceNoteAsync(source.Id, text),
                () => savedNotes[source.Id] = text);
        }

        PresentSource(null);
        TrackForm(() => note.Text ?? "", SaveNote, null, note);
        SafeSelection(list, () => selected, async item =>
        {
            selected = item as Source;
            PresentSource(selected);
            history.ItemsSource = selected is { IsCustom: false } source
                ? await _workspace.SourceHistoryAsync(source.Id)
                : Array.Empty<string>();
        });

        var tabs = new TabControl { ItemsSource = new[]
        {
            Ui.Tab("Referenz", Ui.Scroll(Ui.Stack(title, original, state, Ui.Row(edit, copy, check, open, accept),
                Ui.Notice("Eigene Quellen werden manuell gepflegt. Der Themenfilter begrenzt die angezeigten Quellen.")))),
            Ui.Tab("Eigene Bewertung", Ui.Scroll(Ui.Stack(
                Ui.Field("Fachliche Bewertung", note, "Notizen zur Relevanz, zur Auslegung oder zu offenen Prüfpunkten der gewählten Quelle."),
                saveNote))),
            Ui.Tab("Prüfhistorie", history, "Zeigt technische Abrufe und bestätigte Referenzstände offizieller Quellen.")
        }};
        return Ui.Split(
            Ui.Card(Ui.Fill(Ui.Stack(Ui.Text(_sources.Count + " Quellen im Thema", 18, true), add), list)),
            Ui.Card(tabs));
    }

    private static Uri? OfficialSourceUri(Source? source) =>
        source is { IsCustom: false } && Uri.TryCreate(source.Url, UriKind.Absolute, out var uri) && SourceUpdates.Allowed(uri)
            ? uri : null;
    private async Task<Control> TemplatePage()
    {
        var templates=await _workspace.TemplatesAsync(_topicId);var list=new ListBox{ItemsSource=templates};TextTemplate? selected=null;var title=Ui.Text("Vorlage auswählen",20,true);var original=Ui.Input("Original",true);original.IsReadOnly=true;var edit=Ui.Input("Eigene Fassung",true);
        var save = Button("Vorlage speichern", SaveEditorButtonAsync, true);
        edit.IsEnabled = save.IsEnabled = false;
        SafeSelection(list, () => selected, obj =>
        {
            selected = obj as TextTemplate;
            title.Text = selected?.Title ?? "Vorlage auswählen";
            original.Text = selected?.Body ?? "";
            edit.Text = selected?.Effective ?? "";
            edit.IsEnabled = save.IsEnabled = selected is not null;
            return Task.CompletedTask;
        });
        Task<bool> SaveTemplate()
        {
            var template = selected ?? throw new ArgumentException("Vorlage auswählen.");
            var text = edit.Text ?? "";
            return CommitFormChangeAsync(() => _workspace.SaveTemplateAsync(template.Id, text), () =>
            {
                selected = template with { CustomBody = text };
                templates[templates.FindIndex(t => t.Id == template.Id)] = selected;
                list.ItemsSource = templates.ToList();
                list.SelectedItem = selected;
            });
        }
        TrackForm(() => edit.Text ?? "", SaveTemplate, null, edit);
        return Ui.Split(Ui.Card(Ui.Fill(Ui.Text("Themenbezogene Textvorlagen",18,true),list)),Ui.Card(Ui.Scroll(Ui.Stack(title,Ui.Notice("Eigene Fassung und Original bleiben getrennt. Bereits angelegte Datensätze werden nicht rückwirkend überschrieben."),Ui.Field("Original",original),
            Ui.Field("Eigene Fassung", edit, "Die eigene Fassung wird künftig beim Übernehmen dieser Vorlage verwendet."),
            save))));
    }
    private async Task<Control> GlossaryPage()
    {
        var rows=(await _workspace.GlossaryAsync()).Where(g=>_searchValue==""||g.Title.Contains(_searchValue,StringComparison.CurrentCultureIgnoreCase)).ToList();var list=new ListBox{ItemsSource=rows};Choice? selected=null;var edit=Ui.Input("Eigene Erläuterung",true);
        var save = Button("Definition speichern", SaveEditorButtonAsync, true);
        edit.IsEnabled = save.IsEnabled = false;
        SafeSelection(list, () => selected, obj =>
        {
            selected = obj as Choice;
            edit.Text = selected?.Title.Split('\n', 2).LastOrDefault() ?? "";
            edit.IsEnabled = save.IsEnabled = selected is not null;
            return Task.CompletedTask;
        });
        Task<bool> SaveGlossary()
        {
            var row = selected ?? throw new ArgumentException("Begriff auswählen.");
            var text = edit.Text ?? "";
            return CommitFormChangeAsync(() => _workspace.SaveGlossaryAsync(row.Id, text), () =>
            {
                selected = row with { Title = row.Title.Split('\n', 2)[0] + "\n" + text };
                rows[rows.FindIndex(g => g.Id == row.Id)] = selected;
                list.ItemsSource = rows.ToList();
                list.SelectedItem = selected;
            });
        }
        TrackForm(() => edit.Text ?? "", SaveGlossary, null, edit);
        var print=Button("Glossar drucken",async()=>{if(!await Ui.Confirm(this,"Gefiltertes Glossar als Klartext-Druckansicht speichern?","Erstellen"))return;var path=await SavePath("D3-GRC-Glossar.html","html");if(path is null)return;var sheets=new[]{new ExportSheet("Glossar",["Begriff und Definition"],rows.Select(g=>new[]{g.Title}).ToList())};await Files.AtomicAsync(path,System.Text.Encoding.UTF8.GetBytes(ExcelExport.Html("Glossar",sheets)));await _workspace.RecordExportAsync(path,"","","Glossar HTML");Process.Start(new ProcessStartInfo(new Uri(path).AbsoluteUri){UseShellExecute=true});},
            help: "Erstellt für die angezeigten Begriffe eine unverschlüsselte HTML-Druckansicht im Browser.");
        return Ui.Split(Ui.Card(Ui.Fill(Ui.Text(rows.Count+" Begriffe",18,true),list,print)),Ui.Card(Ui.Stack(Ui.Notice("Kurze Arbeitserläuterungen, keine verbindlichen Legaldefinitionen. Das Glossar ist themenübergreifend."),
            Ui.Field("Eigene Erläuterung", edit, "Arbeitsdefinition für den links ausgewählten Begriff im Projekt hinterlegen."),
            save)));
    }
    private async Task<Control> JournalPage()=>Ui.Card(Ui.Fill(Ui.Stack(Ui.Notice("Lokales Profil, keine verifizierte Organisationsidentität. Die Hashkette ist nicht extern verankert und keine revisionssichere Signatur."),Button("Hashkette prüfen",async()=>{_status.Text=await _workspace.VerifyAsync()?"Hashkette konsistent.":"Hashkette inkonsistent!";},
        help: "Prüft, ob die gespeicherten Prüfsummen des lokalen Journals zusammenpassen. Das Ergebnis erscheint in der Statuszeile.")),new ListBox{ItemsSource=await _workspace.JournalAsync()}));
}
