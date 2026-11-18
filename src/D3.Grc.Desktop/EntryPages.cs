using Avalonia.Controls;
using D3.Grc.Core;
namespace D3.Grc.Desktop;
public sealed partial class MainWindow
{
    private async Task<Control> EntryPage(string kind)
    {
        var list=new ListBox();var statusFilter=Ui.Combo();statusFilter.ItemsSource=new[]{"Alle Status"}.Concat(Entry.Statuses(kind)).ToArray();statusFilter.SelectedItem=_statusFilter==""?"Alle Status":_statusFilter;
        var sort=Ui.Combo();sort.ItemsSource=new[]{"Code","Titel","Termin"};sort.SelectedIndex=0;
        void Filter(){var rows=_entries.Where(e=>e.Kind==kind&&(statusFilter.SelectedIndex==0||e.Status==(string?)statusFilter.SelectedItem));rows=sort.SelectedIndex switch{1=>rows.OrderBy(e=>e.Title),2=>rows.OrderBy(e=>e.DueDate==""?"9999":e.DueDate),_=>rows.OrderBy(e=>e.Code)};list.ItemsSource=rows.ToList();}
        statusFilter.SelectionChanged+=(_,_)=>{if(!_loading)Filter();};sort.SelectionChanged+=(_,_)=>{if(!_loading)Filter();};Filter();
        Entry? selected=null;string? templateId=null;var code=Ui.Input("Code");var title=Ui.Input("Titel");var owner=Ui.Input("Verantwortliche Rolle / Person");var due=Ui.Input("JJJJ-MM-TT");var scope=Ui.Input("Scope");var locator=Ui.Input("Artikel / Abschnitt / konkrete Fundstelle");var body=Ui.Input("Eigene Beschreibung und Ergänzungen",true);var reason=Ui.Input("Begründung der Änderung / Anwendbarkeitsentscheidung",true);
        var source=SourceCombo();var status=Ui.Combo();status.ItemsSource=Entry.Statuses(kind);status.SelectedIndex=0;var template=Ui.Combo();var templates=await _workspace.TemplatesAsync(_topicId,kind);
        void TemplateChoices(){template.ItemsSource=templates.Where(t=>t.SourceId==(source.SelectedItem as Source)?.Id).ToList();template.SelectedIndex=-1;}
        source.SelectionChanged+=(_,_)=>{TemplateChoices();if(!_loading)templateId=null;};TemplateChoices();
        var refs=new ListBox{MinHeight=130};var related=new ListBox{MinHeight=180};var proofs=new ListBox{MinHeight=180};var history=new ListBox{MinHeight=180};var topics=Ui.Text("");
        void Clear(){selected=null;templateId=null;source.ItemsSource=_sources;source.SelectedIndex=0;code.Text="";title.Text="";owner.Text="";due.Text="";scope.Text=_scopeValue;locator.Text="";body.Text="";reason.Text="";status.SelectedIndex=0;refs.ItemsSource=null;related.ItemsSource=null;proofs.ItemsSource=null;history.ItemsSource=null;topics.Text="";}
        SafeSelection(list,async obj=>{
            if(obj is not Entry e){Clear();return;}selected=e;templateId=e.TemplateId;code.Text=e.Code;title.Text=e.Title;owner.Text=e.Owner;due.Text=e.DueDate;scope.Text=e.Scope;locator.Text=e.Locator;body.Text=e.Body;reason.Text="";status.SelectedItem=e.Status;
            // Existing cross-topic records remain readable; edits require a matching concrete topic.
            source.ItemsSource=_sources.Any(s=>s.Id==e.SourceId)?_sources:_sources.Concat(_allSources.Where(s=>s.Id==e.SourceId)).DistinctBy(s=>s.Id).ToList();source.SelectedItem=((IEnumerable<Source>)source.ItemsSource!).First(s=>s.Id==e.SourceId);TemplateChoices();
            refs.ItemsSource=await _workspace.RecordSourcesAsync(e.Id);topics.Text="Themen: "+string.Join(", ",(await _workspace.RecordTopicsAsync(e.Id)).Select(t=>t.Title));
            related.ItemsSource=_mappings.Where(m=>m.From==e.Id||m.To==e.Id).Select(m=>$"{_all.FirstOrDefault(x=>x.Id==m.From)?.Title} → {_all.FirstOrDefault(x=>x.Id==m.To)?.Title}\n{m.Type} · {m.Coverage} · {m.Reason}").ToList();
            proofs.ItemsSource=e.Kind=="Control"?(await _workspace.EvidenceAsync(e.Id)).Cast<object>().ToList():new List<object>{"Nachweise sind über zugeordnete Controls erreichbar."};history.ItemsSource=(await _workspace.JournalAsync()).Where(x=>x.Contains(e.Id,StringComparison.Ordinal)).ToList();
        });
        foreach(var b in new[]{code,title,owner,due,scope,locator,body,reason})Watch(b);Watch(source);Watch(status);
        async Task SaveRecord(){var e=new Entry{Id=selected?.Id??Guid.NewGuid().ToString("N"),Kind=kind,Code=code.Text??"",Title=title.Text??"",Owner=owner.Text??"",DueDate=due.Text??"",Scope=scope.Text??"",Locator=locator.Text??"",Body=body.Text??"",SourceId=Sid(source),TemplateId=templateId,Status=(string?)status.SelectedItem??"Entwurf",Version=selected?.Version??1};await _workspace.SaveEntryAsync(e,_topicId,reason.Text??"");_dirty=false;await Render();}
        _saveForm=SaveRecord;
        var apply=Button("Vorlage übernehmen",async()=>{if(template.SelectedItem is not TextTemplate t)throw new ArgumentException("Vorlage auswählen.");if(!string.IsNullOrWhiteSpace(body.Text)&&!await Ui.Confirm(this,"Vorlage ersetzt den aktuellen Formulartext.","Übernehmen"))return;templateId=t.Id;code.Text=t.Id;title.Text=t.Title;locator.Text=t.Locator;body.Text=t.Effective;},
            help: "Ersetzt Code, Titel, Fundstelle und Beschreibung im Formular. Danach die Felder prüfen und speichern.");
        var addSource=Button("Weiteren Quellenbezug hinzufügen",async()=>{if(selected is null)throw new ArgumentException("Datensatz zuerst speichern.");if(!await Discard())return;var pick=await Ui.Pick(this,"Zusätzliche Quelle (bewusster Cross-Bezug)",_allSources.Cast<object>());if(pick is not Source s)return;var loc=await Ui.Prompt(this,"Konkrete Fundstelle");if(loc is null)return;var why=await Ui.Prompt(this,"Fachliche Begründung des zusätzlichen Quellenbezugs");if(why is null)return;await _workspace.AddSourceAsync(selected.Id,s.Id,loc,why);await Render();},
            help: "Ergänzt eine Quelle am gespeicherten Eintrag. Fundstelle und fachliche Begründung werden abgefragt.");
        var addTopic=Button("Weiterem Thema zuordnen",async()=>{if(selected is null)throw new ArgumentException("Datensatz zuerst speichern.");if(!await Discard())return;var pick=await Ui.Pick(this,"Weiteres Thema",_topics.Where(t=>t.Id!=""&&t.Id!="REVIEW").Cast<object>());if(pick is not Choice t)return;var why=await Ui.Prompt(this,"Begründung");if(why is null)return;await _workspace.AddTopicAsync(selected.Id,t.Id,why);await Render();},
            help: "Macht denselben gespeicherten Eintrag in einem zusätzlichen Thema auffindbar. Die Zuordnung braucht eine Begründung.");
        var general = Ui.Scroll(Ui.Stack(
            Ui.Notice(_topicId == "" ? "Gesamtansicht. Zum Speichern ein konkretes Thema auswählen." : "Quellen und Vorlagen sind auf das Thema eingeschränkt. Zusätzliche Bezüge werden separat begründet."),
            topics,
            Ui.Field("Code", code, "Kurzes internes Kennzeichen, um den Eintrag in Listen und Berichten wiederzufinden."),
            Ui.Field("Titel *", title),
            Ui.Field("Status", status, kind == "Requirement"
                ? "Hält die fachliche Entscheidung zur Anwendbarkeit der Anforderung fest."
                : "Hält den Bearbeitungsstand fest. Auditabschluss und Finding-Schließung erfolgen unter Bezüge."),
            Ui.Field("Verantwortung", owner, "Rolle oder Person angeben, die den Eintrag fachlich betreut."),
            Ui.Field("Termin", due, "Datum im Format JJJJ-MM-TT eingeben, zum Beispiel 2026-11-30."),
            Ui.Field("Scope", scope, "Geltungsbereich des Eintrags, etwa ein System, eine Einheit oder ein Prozess. Der globale Filter sucht den exakten Text."),
            Ui.Field("Beschreibung / Umsetzung", body),
            Ui.Field("Änderungs-/Bewertungsbegründung *", reason, "Den Anlass der Änderung oder die fachlichen Gründe für die Entscheidung dokumentieren.")));
        var sources = Ui.Scroll(Ui.Stack(
            Ui.Field("Primäre Anforderungsquelle", source, "Hauptquelle für diesen Eintrag aus dem gewählten Thema auswählen."),
            Ui.Field("Fundstelle", locator, "Die konkrete Stelle in der Hauptquelle angeben, etwa Artikel 32 Absatz 1."),
            Ui.Field("Passende Textvorlage", template, "Zeigt Vorlagen für die gewählte Hauptquelle und diesen Eintragstyp."),
            apply, Ui.Text("Zugeordnete Quellen", 16, true), refs, addSource, addTopic));
        var workflow=Ui.Stack(Ui.Text("Verknüpfungen",18,true),related,Button("Mappings öffnen",async()=>{if(!await Discard())return;_page="Mappings";await Render();}));
        if(kind=="Audit")workflow.Children.Add(Button("Prüfungen dokumentieren",async()=>{if(selected is null)throw new ArgumentException("Audit auswählen.");if(!await Discard())return;_body.Content=await AuditPage(selected);},true,
            "Öffnet die Prüferfassung für Controls, die dem gespeicherten Audit unter Mappings zugeordnet sind."));
        if(kind=="Finding")workflow.Children.Add(Button("Mit Wirksamkeitsnachweis schließen",async()=>{
            if(selected is null)throw new ArgumentException("Finding auswählen.");if(!await Discard())return;var ids=_mappings.Where(m=>m.From==selected.Id&&m.Type=="Finding → Control").Select(m=>m.To);var eligible=new List<Proof>();foreach(var id in ids)eligible.AddRange(await _workspace.EvidenceAsync(id));var pick=await Ui.Pick(this,"Wirksamkeitsnachweis",eligible.DistinctBy(p=>p.Id).Cast<object>());if(pick is not Proof p)return;var review=await Ui.Prompt(this,"Wirksamkeitsprüfung und Ergebnis dokumentieren");if(review is null)return;await _workspace.CloseFindingAsync(selected.Id,p.Id,review);await Render();},
            help: "Das Finding braucht den Status Wirksamkeitsprüfung und einen gültigen Nachweis für alle verknüpften Controls."));
        var tabs=new TabControl{ItemsSource=new[]{Ui.Tab("Details",general),Ui.Tab("Quellen",sources),
            Ui.Tab("Bezüge", Ui.Scroll(workflow), "Zeigt gespeicherte Zuordnungen und verfügbare Prüfschritte."),
            Ui.Tab("Evidence", proofs, "Dateinachweise gehören zu Controls. Bei anderen Einträgen die verknüpften Controls prüfen."),
            Ui.Tab("Historie", history, "Zeigt protokollierte Aktivitäten zu diesem Eintrag.")}};
        var save=Button("Änderungen speichern",SaveRecord,true, "Speichert den Eintrag mit der angegebenen Begründung im Projekt.");
        var cancel=Button("Abbrechen",async()=>{if(!await Discard())return;await Render();});
        var duplicate=Button("Duplizieren",async()=>{if(selected is null)return;if(!await Discard())return;selected=null;code.Text=(code.Text??"")+"-KOPIE";status.SelectedIndex=0;_dirty=true;},
            help: "Übernimmt die Formularwerte als Entwurf für einen neuen Eintrag. Erst Speichern legt die Kopie an.");
        var delete=Button("Löschen",async()=>{if(selected is null)return;if(!await Discard())return;var why=await Ui.Prompt(this,"Löschbegründung (verknüpfte Datensätze bleiben geschützt)");if(why is null)return;await _workspace.DeleteAsync(selected.Id,why);await Render();},
            help: "Löscht den ausgewählten Eintrag nach Eingabe einer Begründung. Verknüpfte Einträge sind vor dem Löschen geschützt.");
        var create=Button("Neuer Eintrag",async()=>{if(!await Discard())return;_loading=true;try{Clear();}finally{_loading=false;_dirty=false;}});
        return Ui.Split(Ui.Card(Ui.Fill(Ui.Stack(Ui.Row(create),
            Ui.Field("Listenstatus", statusFilter, "Filtert nur diese Liste. Der Export richtet sich nach den globalen Filtern."),
            Ui.Field("Sortierung", sort, "Sortiert diese Liste. Beim Sortieren nach Termin stehen Einträge ohne Datum am Ende.")), list)),
            Ui.Card(Ui.Fill(Ui.Text("Details · "+Pages[kind],18,true),tabs,Ui.Row(save,cancel,duplicate,delete))));
    }
}
