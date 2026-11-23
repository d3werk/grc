using System.Diagnostics;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Input;
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
        var templates = await _workspace.TemplatesAsync(_topicId);
        TextTemplate? selected = null;
        var title = Ui.Heading("Vorlage auswählen", 24, 1);
        var meta = Ui.Secondary("");
        var search = Ui.Input("Titel, Kürzel oder Inhalt");
        var types = Ui.Combo();
        var anyType = new Choice("", "Alle Vorlagenarten");
        types.ItemsSource = new[] { anyType }.Concat(templates.Select(template => template.Kind).Distinct()
            .Select(kind => new Choice(kind, TemplateKindLabel(kind)))
            .OrderBy(type => type.Title, StringComparer.CurrentCultureIgnoreCase)).ToArray();
        types.SelectedItem = anyType;
        var total = Ui.Secondary("");
        var list = new ListBox
        {
            ItemTemplate = new FuncDataTemplate<TextTemplate>((template, _) =>
            {
                if (template is null)
                    return null;
                return new StackPanel
                {
                    Spacing = 4,
                    Children =
                    {
                        Ui.Text(template.Title, 14, true),
                        Ui.Secondary($"{TemplateKindLabel(template.Kind)} · {template.Id}")
                    }
                };
            })
        };

        var effectivePreview = new ContentControl();
        var originalPreview = new ContentControl();
        var edit = new RichTextEditor("Eigene Fassung", "Vorlagentext bearbeiten", editorHeight: 315);
        var save = Button("Vorlage speichern", SaveEditorButtonAsync, true, glyph: Glyph.Save);
        var createDocument = Button("Als Dokument verwenden", async () =>
        {
            if (selected is null || !await CanLeaveFormAsync())
                return;
            await StartDocumentFromTemplateAsync(selected);
        }, help: "Öffnet einen neuen Entwurf mit der gespeicherten Fassung dieser Vorlage.", glyph: Glyph.Documents);

        var tabs = new TabControl
        {
            ItemsSource = new[]
            {
                Ui.Tab("Vorschau", Ui.Scroll(effectivePreview), glyph: Glyph.Documents),
                Ui.Tab("Bearbeiten", Ui.Scroll(Ui.Stack(
                    Ui.Notice("Eigene Fassung und Katalogtext bleiben getrennt. Bereits angelegte Dokumente und Einträge behalten ihren Inhalt."),
                    Ui.Field("Eigene Fassung", edit, "Text markieren und formatieren. Die Vorschau zeigt Schriftstile und Textaufbau."),
                    save)), glyph: Glyph.Templates),
                Ui.Tab("Katalogfassung", Ui.Scroll(originalPreview), glyph: Glyph.Sources)
            },
            SelectedIndex = 0
        };

        void RefreshPreview()
        {
            effectivePreview.Content = selected is null
                ? Ui.Notice("Wählen Sie links eine Vorlage. Hier sehen Sie Inhalt und Aufbau.")
                : DocumentPreview.Create(edit.Content);
        }

        void Load(TextTemplate? template)
        {
            selected = template;
            title.Text = selected?.Title ?? "Vorlage auswählen";
            meta.Text = selected is null ? "" :
                $"{TemplateKindLabel(selected.Kind)} · {selected.Id} · "
                + (_allSources.FirstOrDefault(source => source.Id == selected.SourceId)?.Title ?? selected.SourceId);
            meta.IsVisible = selected is not null;
            edit.Content = selected?.Effective ?? "";
            edit.IsEnabled = save.IsEnabled = createDocument.IsEnabled = selected is not null;
            originalPreview.Content = selected is null ? Ui.Notice("Wählen Sie eine Vorlage.") :
                DocumentPreview.Create(selected.Body);
            RefreshPreview();
        }

        var appliedText = "";
        var appliedType = "";
        void FillList()
        {
            var matches = templates.Where(template => (appliedType == "" || template.Kind == appliedType) &&
                (appliedText == "" || new[] { template.Title, template.Id, RichTextContent.Plain(template.Effective) }.Any(
                    text => text.Contains(appliedText, StringComparison.CurrentCultureIgnoreCase))))
                .OrderBy(template => template.Title, StringComparer.CurrentCultureIgnoreCase).ToList();
            list.ItemsSource = matches;
            list.SelectedItem = matches.FirstOrDefault(template => template.Id == selected?.Id);
            total.Text = matches.Count == 1 ? "1 Vorlage" : $"{matches.Count} Vorlagen";
        }

        async Task ApplySearch()
        {
            var text = search.Text?.Trim() ?? "";
            var type = (types.SelectedItem as Choice)?.Id ?? "";
            if ((text == appliedText && type == appliedType) || !await CanLeaveFormAsync())
                return;
            appliedText = text;
            appliedType = type;
            FillList();
            Load(list.SelectedItem as TextTemplate);
            AcceptFormState();
        }

        Load(null);
        FillList();
        SafeSelection(list, () => selected, item =>
        {
            Load(item as TextTemplate);
            return Task.CompletedTask;
        });
        tabs.SelectionChanged += (_, _) =>
        {
            if (tabs.SelectedIndex == 0)
                RefreshPreview();
        };
        search.KeyDown += async (_, e) =>
        {
            if (e.Key != Key.Enter)
                return;
            e.Handled = true;
            await Run(ApplySearch);
        };

        Task<bool> SaveTemplate()
        {
            var template = selected ?? throw new ArgumentException("Vorlage auswählen.");
            var text = edit.Content;
            return CommitFormChangeAsync(() => _workspace.SaveTemplateAsync(template.Id, text), () =>
            {
                selected = template with { CustomBody = text };
                templates[templates.FindIndex(item => item.Id == template.Id)] = selected;
                FillList();
                RefreshPreview();
            });
        }

        TrackForm(() => edit.Content, SaveTemplate, null, edit);
        var filter = Ui.Stack(Ui.Heading("Textvorlagen", 20), Ui.Field("Vorlagen suchen", search),
            Ui.Field("Vorlagenart", types), Button("Filter anwenden", ApplySearch, glyph: Glyph.Filter), total);
        return Ui.Split(Ui.Card(Ui.Fill(filter, list)), Ui.Card(Ui.Fill(Ui.Stack(title, meta), tabs, Ui.Row(createDocument))));
    }

    private static string TemplateKindLabel(string kind) => kind switch
    {
        "Requirement" => "Anforderung",
        "Control" => "Control",
        "Risk" => "Risiko",
        "Audit" => "Prüfung",
        "Evidence" => "Nachweis",
        "Measure" => "Maßnahme",
        "Finding" => "Feststellung",
        "AiSystem" => "KI-System",
        "OssComponent" => "OSS-Komponente",
        "Supplier" => "Lieferant",
        "Policy" => "Richtlinie",
        _ => kind
    };
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
