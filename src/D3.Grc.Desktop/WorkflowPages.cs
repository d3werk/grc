using Avalonia.Controls;
using Avalonia.Platform.Storage;
using D3.Grc.Core;
namespace D3.Grc.Desktop;
public sealed partial class MainWindow
{
    private async Task<Control> MappingPage()
    {
        var ids = _entries.Select(e => e.Id).ToHashSet();
        var list = new ListBox();
        void PopulateMappings()
        {
            var currentId = (list.SelectedItem as Choice)?.Id;
            var choices = _mappings.Where(m => ids.Contains(m.From) || ids.Contains(m.To))
                .Select(m => new Choice(m.Id, $"{_all.FirstOrDefault(e => e.Id == m.From)?.Title} → {_all.FirstOrDefault(e => e.Id == m.To)?.Title}\n{m.Type} · {m.Coverage} · {m.Approval}\n{m.Reason}"))
                .ToList();
            list.ItemsSource = choices;
            list.SelectedItem = choices.FirstOrDefault(c => c.Id == currentId);
        }
        PopulateMappings();
        var type=Ui.Combo();type.ItemsSource=Workspace.LinkTypes.Keys.ToList();type.SelectedIndex=0;var from=Ui.Combo();var to=Ui.Combo();var source=SourceCombo(Array.Empty<Source>());
        var cross = Ui.Help(new CheckBox { Content = "Zielobjekte aus anderen Themen/Scopes einbeziehen" },
            "Erweitert die Zielauswahl über das aktuelle Thema, den Geltungsbereich und die Suche hinaus. Den fachlichen Bezug begründen.");
        var coverage=Ui.Combo();coverage.ItemsSource=new[]{"Teilweise","Vollständig","Unterstützend"};coverage.SelectedIndex=0;var reason=Ui.Input("Abdeckung, Grenzen und fachliche Begründung",true);
        void Choices()
        {
            if (type.SelectedItem is not string relation)
                return;
            var pair = Workspace.LinkTypes[relation];
            var fromChoice = from.SelectedItem as Entry;
            var toChoice = to.SelectedItem as Entry;
            var fromChoices = _entries.Where(e => e.Kind == pair.From).ToList();
            var toChoices = (cross.IsChecked == true ? _all : _entries).Where(e => e.Kind == pair.To).ToList();
            from.ItemsSource = fromChoices;
            from.SelectedItem = fromChoices.FirstOrDefault(e => e.Id == fromChoice?.Id);
            to.ItemsSource = toChoices;
            to.SelectedItem = toChoices.FirstOrDefault(e => e.Id == toChoice?.Id);
        }
        async Task RefreshMappingSources()
        {
            var previous = (source.SelectedItem as Source)?.Id;
            IReadOnlyList<Source> choices = from.SelectedItem is Entry entry
                ? await _workspace.RecordSourceChoicesAsync(entry.Id)
                : Array.Empty<Source>();
            source.ItemsSource = choices;
            source.SelectedItem = choices.FirstOrDefault(s => s.Id == previous) ?? choices.FirstOrDefault();
        }
        async Task ChangeMappingChoices()
        {
            if (_loading || _busy)
                return;
            await Run(async () =>
            {
                Choices();
                await RefreshMappingSources();
            });
        }
        Choices();
        type.SelectionChanged += async (_, _) => await ChangeMappingChoices();
        cross.IsCheckedChanged += async (_, _) => await ChangeMappingChoices();
        from.SelectionChanged += async (_, _) =>
        {
            if (_loading || _busy)
                return;
            await Run(RefreshMappingSources);
        };
        Task<bool> SaveMapping()
        {
            if (from.SelectedItem is not Entry a || to.SelectedItem is not Entry b)
                throw new ArgumentException("Ausgangsobjekt und Ziel auswählen.");
            return CommitFormChangeAsync(() => _workspace.LinkAsync(a.Id, b.Id, (string)type.SelectedItem!, Sid(source),
                (string)coverage.SelectedItem!, _scopeValue, reason.Text ?? ""));
        }
        async Task RefreshMappings()
        {
            _mappings = await _workspace.MappingsAsync();
            PopulateMappings();
        }
        TrackForm(() => (type.SelectedItem, (from.SelectedItem as Entry)?.Id, (to.SelectedItem as Entry)?.Id,
            (source.SelectedItem as Source)?.Id, cross.IsChecked, coverage.SelectedItem, reason.Text ?? ""),
            SaveMapping, RefreshMappings, type, from, to, source, cross, coverage, reason);
        var suggestions=await _workspace.MappingSuggestionsAsync(_topicId);
        var loadSuggestion=Button("Vorbereiteten Vorschlag laden",async()=>{
            if(!await CanLeaveFormAsync())return;var pick=await Ui.Pick(this,"Zuordnungsvorschläge (noch nicht bestätigt)",suggestions.Cast<object>());if(pick is not Choice suggestion)return;
            var parts=suggestion.Id.Split('|');var a=_entries.FirstOrDefault(e=>e.TemplateId==parts[0]&&e.Kind=="Requirement");var b=_all.FirstOrDefault(e=>e.TemplateId==parts[1]&&e.Kind=="Control");
            if(a is null||b is null)throw new ArgumentException("Passende Datensätze sind nicht im aktuellen Kontext. Filter prüfen.");
            type.SelectedItem="Anforderung → Control";cross.IsChecked=!_entries.Any(e=>e.Id==b.Id);Choices();from.SelectedItem=a;to.SelectedItem=b;source.ItemsSource=await _workspace.RecordSourceChoicesAsync(a.Id);source.SelectedIndex=0;coverage.SelectedIndex=0;reason.Text=suggestion.Title.Split('\n',2).Last();
        }, help: "Füllt das Formular aus, wenn die passenden Datensätze bereits angelegt sind. Erst Speichern bestätigt die Zuordnung.");
        var remove=Button("Auswahl entfernen",async()=>{if(list.SelectedItem is not Choice c)return;if(!await CanLeaveFormAsync())return;if(!await Ui.Confirm(this,"Zuordnung entfernen?","Entfernen"))return;await _workspace.UnlinkAsync(c.Id);await Render();});
        return Ui.Split(Ui.Card(Ui.Fill(Ui.Text("Bestätigte und übernommene Zuordnungen",18,true),list,remove)),
            Ui.Card(Ui.Scroll(Ui.Stack(Ui.Text("Zuordnung erstellen",20,true),
                Ui.Notice("Standardmäßig nur Objekte im Thema. Cross-Mappings müssen bewusst aktiviert und begründet werden. Vollständige Abdeckung ist eine fachliche Bewertung, keine automatische Aussage."),
                loadSuggestion,
                Ui.Field("Beziehung", type, "Legt die Richtung und die Eintragstypen der Zuordnung fest."),
                Ui.Field("Ausgangsobjekt", from, "Wählt den Ursprung der Beziehung aus dem aktuell gefilterten Arbeitsbereich."),
                cross,
                Ui.Field("Zielobjekt", to, "Wählt den Eintrag, auf den die Beziehung verweist."),
                Ui.Field("Quellenbezug des Ausgangsobjekts", source, "Die Quelle muss am Ausgangsobjekt hinterlegt sein."),
                Ui.Field("Abdeckung", coverage, "Fachlich bewerten, in welchem Umfang das Ziel zum Ausgangsobjekt beiträgt."),
                Ui.Field("Begründung *", reason, "Erklären, worauf die Zuordnung und die gewählte Abdeckung beruhen."),
                Button("Zuordnung speichern", SaveEditorButtonAsync, true)))));
    }
    private async Task<Control> EvidencePage()
    {
        var controls = _entries.Where(e => e.Kind == "Control").ToList();
        var list = new ListBox();
        var heading = Ui.Text("", 18, true);
        async Task RefreshEvidence()
        {
            var selected = (list.SelectedItem as Proof)?.Id;
            var visible = new Dictionary<string, Proof>();
            foreach (var controlEntry in controls)
                foreach (var proof in await _workspace.EvidenceAsync(controlEntry.Id))
                    visible[proof.Id] = proof;
            list.ItemsSource = visible.Values.ToList();
            list.SelectedItem = selected is null ? null : visible.GetValueOrDefault(selected);
            heading.Text = visible.Count + " tatsächliche Nachweise";
        }
        await RefreshEvidence();
        var control=Ui.Combo();control.ItemsSource=controls;var source=SourceCombo(Array.Empty<Source>());var title=Ui.Input("Titel des tatsächlichen Nachweises");var origin=Ui.Input("Herkunft, Scope, Erhebungsmethode",true);var days=new NumericUpDown{Minimum=1,Maximum=3650,Value=90};var details=Ui.Text("");var template=Ui.Combo();var templates=await _workspace.TemplatesAsync(_topicId,"Evidence");source.SelectionChanged+=(_,_)=>{template.ItemsSource=templates.Where(t=>t.SourceId==(source.SelectedItem as Source)?.Id).ToList();template.SelectedIndex=-1;};
        control.SelectionChanged += async (_, _) =>
        {
            if (_loading || _busy)
                return;
            await Run(async () =>
            {
                source.ItemsSource = control.SelectedItem is Entry entry
                    ? await _workspace.RecordSourceChoicesAsync(entry.Id)
                    : Array.Empty<Source>();
                source.SelectedIndex = 0;
            });
        };
        list.SelectionChanged+=(_,_)=>details.Text=list.SelectedItem is Proof p?$"Datei: {p.FileName}\nErhoben: {p.Collected}\nGültig bis: {p.ValidUntil}\nSHA256: {p.Hash}\nHerkunft: {p.Origin}":"";
        async Task<bool> Import()
        {
            if (control.SelectedItem is not Entry entry)
                throw new ArgumentException("Control auswählen.");
            var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions { AllowMultiple = false });
            var path = files.FirstOrDefault()?.TryGetLocalPath();
            if (path is null)
                return false;
            var bytes = await Files.ReadAsync(path, 8388608);
            return await CommitFormChangeAsync(() => _workspace.ImportEvidenceAsync(entry.Id, Sid(source), title.Text ?? "",
                origin.Text ?? "", Path.GetFileName(path), bytes, DateTimeOffset.UtcNow.AddDays((double)(days.Value ?? 90))));
        }
        TrackForm(() => ((control.SelectedItem as Entry)?.Id, (source.SelectedItem as Source)?.Id,
            title.Text ?? "", origin.Text ?? "", days.Value), Import, RefreshEvidence, control, source, title, origin, days);
        var apply=Button("Nachweisanforderung übernehmen",async()=>{if(template.SelectedItem is not TextTemplate t)throw new ArgumentException("Vorlage auswählen.");if(!string.IsNullOrWhiteSpace(origin.Text)&&!await Ui.Confirm(this,"Text ersetzen?","Übernehmen"))return;title.Text=t.Title;origin.Text=t.Effective;},
            help: "Füllt Titel und Herkunft mit Vorlagentext. Vor dem Dateiimport die tatsächliche Herkunft ergänzen.");
        var reuse = Button("Auswahl für Control wiederverwenden", async () =>
        {
            if (list.SelectedItem is not Proof proof)
                throw new ArgumentException("Einen vorhandenen Nachweis auswählen.");
            var candidates = controls.Where(c => c.Id != proof.ControlId).Cast<object>();
            if (await Ui.Pick(this, "Zusätzliches Control", candidates) is not Entry target)
                return;
            await _workspace.ReuseEvidenceAsync(proof.Id, target.Id);
            await RefreshEvidence();
            UpdateFormStatus();
        }, help: "Verknüpft den ausgewählten Nachweis mit einem weiteren Control aus dem aktuellen Arbeitsbereich.");
        var export=Button("Datei exportieren",async()=>{if(list.SelectedItem is not Proof p)return;if(!await Ui.Confirm(this,"Evidence als unverschlüsselte Datei exportieren?","Exportieren"))return;var path=await SavePath(p.FileName,Path.GetExtension(p.FileName).TrimStart('.'));if(path is null)return;await Files.AtomicAsync(path,await _workspace.EvidenceBytesAsync(p.Id));await _workspace.RecordExportAsync(path,_topicId,_scopeValue,"Evidence");},
            help: "Speichert den links ausgewählten Nachweis als unverschlüsselte Datei.");
        var preview=Button("Vorschau",async()=>
        {
            if (list.SelectedItem is Proof proof)
                await PreviewEvidenceAsync(proof);
        },help:"Zeigt Text- und PNG-Nachweise in der App. Unterstützte Inhalte bleiben im Projekt.", glyph: Glyph.Documents);
        preview.IsEnabled = list.SelectedItem is Proof;
        list.SelectionChanged += (_, _) => preview.IsEnabled = list.SelectedItem is Proof;
        return Ui.Split(Ui.Card(Ui.Fill(heading,list,Ui.Row(preview, export))),
            Ui.Card(Ui.Scroll(Ui.Stack(Ui.Text("Evidence erfassen",20,true),
                Ui.Notice("Quellen stammen vom ausgewählten Control. Nachweisherkunft ist ein separates Feld. Vorlagen erzeugen keine Evidence ohne tatsächlichen Dateiimport."),
                Ui.Field("Control", control, "Ordnet die Nachweisdatei einem Control aus dem aktuellen Arbeitsbereich zu."),
                Ui.Field("Anforderungsquelle", source, "Fachliche Referenz aus den Quellenbezügen des ausgewählten Controls."),
                Ui.Field("Nachweisvorlage", template, "Zeigt Erfassungshilfen für die ausgewählte Quelle."),
                apply, Ui.Field("Titel *",title),
                Ui.Field("Herkunft / Scope *", origin, "Dokumentiere, wo und wie der Nachweis erhoben wurde, etwa System, Umgebung und Erhebungsmethode."),
                Ui.Field("Gültigkeit ab heute in Tagen", days, "Legt fest, bis wann der Nachweis für neue Prüfungen verwendet werden kann. Zulässig sind 1 bis 3650 Tage."),
                Button("Datei importieren und speichern", SaveEditorButtonAsync, true, "Wählt eine tatsächliche Nachweisdatei bis 8 MiB und speichert sie im verschlüsselten Projekt."),
                reuse, details))));
    }
    private async Task<Control> AuditPage(Entry audit)
    {
        var ids = _mappings.Where(m => m.From == audit.Id && m.Type == "Audit → Control").Select(m => m.To).ToHashSet();
        var control = Ui.Combo();
        control.ItemsSource = _all.Where(e => ids.Contains(e.Id)).ToList();
        var source = SourceCombo(Array.Empty<Source>());
        var evidence = Ui.Combo();
        var outcome = Ui.Combo();
        outcome.ItemsSource = new[] { "Ergebnis auswählen", "Bestanden", "Abweichung", "Nicht anwendbar" };
        outcome.SelectedIndex = 0;
        var notes = Ui.Input("Prüfhandlung, Stichprobe, objektiver Sachverhalt und Ergebnis", true);
        var results = new ListBox { MinHeight = 180, ItemsSource = await _workspace.TestsAsync(audit.Id) };
        var templates = await _workspace.TemplatesAsync(_topicId, "AuditTest");
        var template = Ui.Combo();
        source.SelectionChanged += (_, _) =>
        {
            template.ItemsSource = templates.Where(t => t.SourceId == (source.SelectedItem as Source)?.Id).ToList();
            template.SelectedIndex = -1;
        };
        Entry? activeControl = null;
        control.SelectionChanged += async (_, _) =>
        {
            if (_loading || _busy || ReferenceEquals(control.SelectedItem, activeControl))
                return;
            var next = control.SelectedItem as Entry;
            await Run(async () =>
            {
                control.SelectedItem = activeControl;
                if (!await CanLeaveFormAsync())
                    return;
                IReadOnlyList<Source> sources = next is null ? Array.Empty<Source>() : await _workspace.RecordSourceChoicesAsync(next.Id);
                var proofs = next is null ? new List<Proof>() : await _workspace.EvidenceAsync(next.Id);
                activeControl = next;
                control.SelectedItem = activeControl;
                source.ItemsSource = sources;
                source.SelectedIndex = 0;
                evidence.ItemsSource = new object[] { "Kein Nachweis" }.Concat(proofs
                    .Where(p => DateTimeOffset.TryParse(p.ValidUntil, out var until) && until > DateTimeOffset.UtcNow)).ToList();
                evidence.SelectedIndex = 0;
                outcome.SelectedIndex = 0;
                notes.Text = "";
                AcceptFormState();
            });
        };
        Task<bool> SaveTest()
        {
            if (activeControl is null || outcome.SelectedIndex == 0)
                throw new ArgumentException("Control und tatsächliches Ergebnis auswählen.");
            return CommitFormChangeAsync(() => _workspace.TestAsync(audit.Id, activeControl.Id, Sid(source),
                (evidence.SelectedItem as Proof)?.Id, (string)outcome.SelectedItem!, notes.Text ?? ""));
        }
        TrackForm(() => ((source.SelectedItem as Source)?.Id, (evidence.SelectedItem as Proof)?.Id, outcome.SelectedItem,
                notes.Text ?? ""), SaveTest, async () => results.ItemsSource = await _workspace.TestsAsync(audit.Id),
            source, evidence, outcome, notes);
        var complete=Button("Audit abschließen",async()=>{if(!await CanLeaveFormAsync())return;var why=await Ui.Prompt(this,"Abschlussbegründung");if(why is null)return;await _workspace.CompleteAuditAsync(audit.Id,why);await Render();},
            help: "Für jedes Control wird ein Prüfergebnis benötigt. Abweichungen brauchen ein mit Audit und Control verknüpftes Finding.");
        var editor = Ui.Stack(Ui.Text("Prüfung dokumentieren",20,true),
            Ui.Field("Control im Audit-Scope", control, "Zeigt Controls, die unter Mappings mit diesem Audit verknüpft sind."),
            Ui.Field("Prüfquelle", source, "Wählt den Quellenbezug des Controls aus, der für diese Prüfung maßgeblich ist."),
            Ui.Field("Passende gültige Evidence", evidence, "Für das Ergebnis Bestanden ist ein am Prüfzeitpunkt gültiger Nachweis erforderlich."),
            Ui.Field("Prüfvorlage", template, "Zeigt Prüftexte für die ausgewählte Quelle."),
            Button("Prüftext übernehmen",async()=>{if(template.SelectedItem is not TextTemplate t)return;if(!string.IsNullOrWhiteSpace(notes.Text)&&!await Ui.Confirm(this,"Prüftext ersetzen?","Ersetzen"))return;notes.Text=t.Effective;},
                help: "Kopiert den Vorlagentext in die Prüfnotiz. Die tatsächliche Beobachtung anschließend ergänzen."),
            Ui.Field("Ergebnis", outcome, "Tatsächliches Ergebnis auswählen. Bei Nicht anwendbar die Gründe in der Prüfnotiz festhalten."),
            Ui.Field("Prüfnotiz *", notes, "Prüfhandlung, Stichprobe und beobachtetes Ergebnis so festhalten, dass andere die Entscheidung nachvollziehen können."),
            Button("Prüfergebnis speichern", SaveEditorButtonAsync, true), complete);
        return Ui.Split(Ui.Card(Ui.Scroll(Ui.Stack(Ui.Text(audit.Title,22,true),Ui.Text(audit.Body),Ui.Notice(ids.Count+" Controls im Scope. Scope unter Mappings pflegen. Bestanden benötigt gültige Evidence; Abweichung benötigt vor Abschluss ein verknüpftes Finding. Auditabschluss lässt offene Findings ausdrücklich bestehen."),results,Button("Zurück zu Audits",async()=>{if(!await CanLeaveFormAsync())return;await Render();})))),Ui.Card(Ui.Scroll(editor)));
    }
}
