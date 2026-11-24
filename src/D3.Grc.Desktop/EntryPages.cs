using Avalonia.Controls;
using Avalonia.Controls.Templates;
using D3.Grc.Core;
namespace D3.Grc.Desktop;
public sealed partial class MainWindow
{
    private async Task<Control> EntryPage(string kind)
    {
        var isSupplier = kind == "Supplier";
        var isRisk = kind == "Risk";
        var module = DesktopModules.Specs.TryGetValue(kind, out var spec)
            ? new NativeModuleForm(spec, () => { if (!_loading) UpdateFormStatus(); })
            : null;
        var list=new ListBox();var statusFilter=Ui.Combo();statusFilter.ItemsSource=new[]{"Alle Status"}.Concat(Entry.Statuses(kind)).ToArray();statusFilter.SelectedItem=_statusFilter==""?"Alle Status":_statusFilter;
        if (isSupplier)
            list.ItemTemplate = new FuncDataTemplate<Entry>((entry, _) => entry is null ? null : Ui.Stack(
                Ui.Text(entry.Title, 14, true),
                Ui.Secondary(string.Join(" · ", new[] { entry.Code, entry.Status,
                    entry.DueDate == "" ? "Kein Prüftermin" : $"Prüfung {entry.DueDate}" }.Where(x => x.Length > 0)))));
        if (statusFilter.SelectedIndex < 0)
            statusFilter.SelectedIndex = 0;
        var sort=Ui.Combo();sort.ItemsSource=isSupplier ? new[]{"Code","Lieferantenname","Nächste Prüfung"} : new[]{"Code","Titel","Termin"};sort.SelectedIndex=0;
        var appliedStatus = (string)statusFilter.SelectedItem!;
        var appliedSort = sort.SelectedIndex;
        void Filter(){var rows=_entries.Where(e=>e.Kind==kind&&(statusFilter.SelectedIndex==0||e.Status==(string?)statusFilter.SelectedItem));rows=sort.SelectedIndex switch{1=>rows.OrderBy(e=>e.Title),2=>rows.OrderBy(e=>e.DueDate==""?"9999":e.DueDate),_=>rows.OrderBy(e=>e.Code)};list.ItemsSource=rows.ToList();}
        Filter();
        Entry? selected=null;string? templateId=null;var code=Ui.Input("Code");var title=Ui.Input("Titel");var owner=Ui.Input("Verantwortliche Rolle / Person");var due=Ui.Input("JJJJ-MM-TT");var scope=Ui.Input("Scope");var locator=Ui.Input("Artikel / Abschnitt / konkrete Fundstelle");var body=new RichTextEditor(isSupplier ? "Interne Notizen" : "Beschreibung / Umsetzung", "Eigene Beschreibung und Ergänzungen");var reason=Ui.Input("Begründung der Änderung / Anwendbarkeitsentscheidung",true);
        var source=SourceCombo();var status=Ui.Combo();status.ItemsSource=Entry.Statuses(kind);status.SelectedIndex=0;var template=Ui.Combo();var templates=await _workspace.TemplatesAsync(_topicId,kind);
        var supplier = isSupplier ? new SupplierFields() : null;
        var reviews = isSupplier ? Ui.Stack() : null;
        Button? addReview = null;
        var riskAssessments = isRisk ? Ui.Stack() : null;
        Button? assessRisk = null;

        void SetStatusChoices(string current)
        {
            if (isSupplier)
                status.ItemsSource = Entry.Statuses(kind).Where(s => s == current || !SupplierRegister.IsReviewStatus(s)).ToArray();
            status.SelectedItem = current;
        }
        void TemplateChoices(){template.ItemsSource=templates.Where(t=>t.SourceId==(source.SelectedItem as Source)?.Id).ToList();template.SelectedIndex=-1;}
        source.SelectionChanged+=(_,_)=>{TemplateChoices();if(!_loading)templateId=null;};TemplateChoices();
        var refs=new ListBox{MinHeight=130};var related=new ListBox{MinHeight=180};var proofs=new ListBox{MinHeight=180};var history=new ListBox{MinHeight=180};var topics=Ui.Text("");
        if (isSupplier)
            addReview = Button("Bewertung erfassen", RecordReview, true, "Dokumentiert ein Prüfergebnis. Die neueste Bewertung aktualisiert Status und Folgetermin.");
        if (isRisk)
            assessRisk = Button("Risiko bewerten", RecordRiskAssessment, true,
                "Erfasst Eintrittswahrscheinlichkeit, Auswirkung, Behandlung und die Gründe für diese Bewertung.", Glyph.Risk);
        void Clear()
        {
            selected=null;templateId=null;source.ItemsSource=_sources;source.SelectedIndex=isSupplier ? -1 : 0;code.Text="";title.Text="";owner.Text="";due.Text="";scope.Text=EffectiveScope;locator.Text="";body.Content="";reason.Text="";refs.ItemsSource=null;related.ItemsSource=null;proofs.ItemsSource=null;history.ItemsSource=null;topics.Text="";
            SetStatusChoices(Entry.Statuses(kind)[0]);
            if (module is not null)
            {
                source.SelectedItem = _sources.FirstOrDefault(item => item.Id == "INTERNAL") ?? _sources.FirstOrDefault();
                module.Load(null);
            }
            supplier?.Load(null);
            if (reviews is not null && addReview is not null)
                PresentSupplierReviews(reviews, addReview, null, []);
            if (riskAssessments is not null && assessRisk is not null)
                PresentRiskAssessments(riskAssessments, assessRisk, null, []);
        }
        Clear();
        async Task LoadEntry(object? obj)
        {
            if(obj is not Entry e){Clear();return;}selected=e;templateId=e.TemplateId;code.Text=e.Code;title.Text=e.Title;owner.Text=e.Owner;due.Text=e.DueDate;scope.Text=e.Scope;locator.Text=e.Locator;body.Content=e.Body;reason.Text="";SetStatusChoices(e.Status);
            // Existing cross-topic records remain readable; edits require a matching concrete topic.
            source.ItemsSource=_sources.Any(s=>s.Id==e.SourceId)?_sources:_sources.Concat(_allSources.Where(s=>s.Id==e.SourceId)).DistinctBy(s=>s.Id).ToList();source.SelectedItem=((IEnumerable<Source>)source.ItemsSource!).First(s=>s.Id==e.SourceId);TemplateChoices();
            refs.ItemsSource=await _workspace.RecordSourcesAsync(e.Id);topics.Text="Themen: "+string.Join(", ",(await _workspace.RecordTopicsAsync(e.Id)).Select(t=>t.Title));
            related.ItemsSource=_mappings.Where(m=>m.From==e.Id||m.To==e.Id).Select(m=>$"{_all.FirstOrDefault(x=>x.Id==m.From)?.Title} → {_all.FirstOrDefault(x=>x.Id==m.To)?.Title}\n{m.Type} · {m.Coverage} · {m.Reason}").ToList();
            proofs.ItemsSource=e.Kind=="Control"?(await _workspace.EvidenceAsync(e.Id)).Cast<object>().ToList():new List<object>{"Nachweise sind über zugeordnete Controls erreichbar."};history.ItemsSource=(await _workspace.JournalAsync()).Where(x=>x.Contains(e.Id,StringComparison.Ordinal)).ToList();
            templateId = e.TemplateId;
            if (module is not null)
                module.Load(await _workspace.ModuleFieldsAsync(e.Id));
            if (supplier is not null && reviews is not null && addReview is not null)
            {
                supplier.Load(await _workspace.SupplierDetailsAsync(e.Id));
                PresentSupplierReviews(reviews, addReview, e, await _workspace.SupplierReviewsAsync(e.Id));
            }
            if (riskAssessments is not null && assessRisk is not null)
                PresentRiskAssessments(riskAssessments, assessRisk, e, await _workspace.RiskAssessmentsAsync(e.Id));
        }
        SafeSelection(list, () => selected, LoadEntry);

        async Task ChangeListView()
        {
            if (_loading || _busy)
                return;
            var nextStatus = (string?)statusFilter.SelectedItem ?? "Alle Status";
            var nextSort = sort.SelectedIndex;
            await Run(async () =>
            {
                statusFilter.SelectedItem = appliedStatus;
                sort.SelectedIndex = appliedSort;
                if (!await CanLeaveFormAsync())
                    return;
                appliedStatus = nextStatus;
                appliedSort = nextSort;
                _statusFilter = nextStatus == "Alle Status" ? "" : nextStatus;
                statusFilter.SelectedItem = nextStatus;
                sort.SelectedIndex = nextSort;
                Filter();
                await LoadEntry(((IEnumerable<Entry>)list.ItemsSource!).FirstOrDefault(e => e.Id == selected?.Id));
                list.SelectedItem = selected;
                AcceptFormState();
            });
        }
        statusFilter.SelectionChanged += async (_, _) => await ChangeListView();
        sort.SelectionChanged += async (_, _) => await ChangeListView();

        void CacheEntry(Entry entry)
        {
            _all.RemoveAll(e => e.Id == entry.Id);
            _all.Add(entry);
            _entries.RemoveAll(e => e.Id == entry.Id);
            if ((EffectiveScope == "" || entry.Scope == EffectiveScope) && Matches(entry))
                _entries.Add(entry);
            Filter();
        }

        async Task RecordReview()
        {
            if (selected is null || !await CanLeaveFormAsync())
                return;
            if (selected is not { Status: not "Beendet" } current)
                return;
            if (_refreshAfterPersistence)
            {
                await _workspace.SaveAsync();
                _refreshAfterPersistence = false;
            }
            var result = await ShowSupplierReviewAsync(current);
            if (!result.Saved)
                return;
            var updated = await _workspace.SupplierEntryAsync(current.Id);
            CacheEntry(updated);
            var visible = ((IEnumerable<Entry>)list.ItemsSource!).Contains(updated);
            await LoadEntry(visible ? updated : null);
            list.SelectedItem = selected;
            AcceptFormState();
            if (result.Failure is not null)
                throw result.Failure;
            _status.Text = visible
                ? $"Bewertung gespeichert · {updated.Title} · {updated.Status}"
                : $"Bewertung gespeichert · {updated.Title} hat jetzt den Status {updated.Status}. Zum Anzeigen den Listenfilter ändern.";
        }

        async Task RecordRiskAssessment()
        {
            if (selected is null || !await CanLeaveFormAsync())
                return;
            if (_refreshAfterPersistence)
            {
                await _workspace.SaveAsync();
                _refreshAfterPersistence = false;
            }
            var current = selected;
            var result = await ShowRiskAssessmentAsync(current);
            if (!result.Saved)
                return;
            if (result.Failure is not null)
                _refreshAfterPersistence = true;
            await LoadEntry(current);
            list.SelectedItem = selected;
            AcceptFormState();
            if (result.Failure is not null)
                throw result.Failure;
            _status.Text = $"Risikobewertung gespeichert · {current.Title}";
        }

        async Task<bool> SaveRecord()
        {
            var entry = new Entry
            {
                Id = selected?.Id ?? Guid.NewGuid().ToString("N"),
                Kind = kind,
                Code = code.Text ?? "",
                Title = title.Text ?? "",
                Owner = owner.Text ?? "",
                DueDate = isSupplier ? due.Text?.Trim() ?? "" : due.Text ?? "",
                Scope = scope.Text ?? "",
                Locator = locator.Text ?? "",
                Body = body.Content,
                SourceId = Sid(source),
                TemplateId = templateId,
                Status = (string?)status.SelectedItem ?? "Entwurf",
                Version = selected?.Version ?? 1
            };
            void ApplySavedEntry()
            {
                if (selected is not null)
                    entry.Version++;
                selected = entry;
                CacheEntry(entry);
                list.SelectedItem = selected;
                reason.Text = "";
            }
            var details = supplier?.Read();
            return await CommitFormChangeAsync(
                () => _workspace.SaveEntryAsync(entry, _topicId == "" && module is not null ? module.Spec.Topic : _topicId,
                    reason.Text ?? "", supplierDetails: details, moduleFields: module?.Read()), ApplySavedEntry);
        }
        Control[] inputs = [code, title, owner, due, scope, locator, body, reason, source, status, .. (supplier?.Inputs ?? []), .. (module?.Inputs ?? [])];
        var supplierOverview = isSupplier && _topicId == "";
        if (supplierOverview)
            foreach (var input in inputs)
            {
                if (input is TextBox text)
                    text.IsReadOnly = true;
                else if (input is RichTextEditor richText)
                    richText.IsReadOnly = true;
                else
                    input.IsEnabled = false;
            }
        TrackForm(() => (selected?.Id, code.Text ?? "", title.Text ?? "", owner.Text ?? "", due.Text ?? "", scope.Text ?? "",
                locator.Text ?? "", body.Content, reason.Text ?? "", (source.SelectedItem as Source)?.Id, status.SelectedItem, templateId,
                supplier?.State, module?.State),
            SaveRecord, async () =>
            {
                if (await RefreshScopeOptionsAsync())
                    await Render();
                else
                    await LoadEntry(selected);
            }, inputs);
        var apply=Button("Vorlage übernehmen",async()=>{if(template.SelectedItem is not TextTemplate t)throw new ArgumentException("Vorlage auswählen.");if(!string.IsNullOrWhiteSpace(body.PlainText)&&!await Ui.Confirm(this,"Vorlage ersetzt den aktuellen Formulartext.","Übernehmen"))return;templateId=t.Id;code.Text=t.Id;title.Text=t.Title;locator.Text=t.Locator;body.Content=t.Effective;},
            help: "Ersetzt Code, Titel, Fundstelle und Beschreibung im Formular. Danach die Felder prüfen und speichern.");
        var addSource=Button("Weiteren Quellenbezug hinzufügen",async()=>{if(selected is null)throw new ArgumentException("Datensatz zuerst speichern.");if(!await CanLeaveFormAsync())return;var pick=await Ui.Pick(this,"Zusätzliche Quelle (bewusster Cross-Bezug)",_allSources.Cast<object>());if(pick is not Source s)return;var loc=await Ui.Prompt(this,"Konkrete Fundstelle");if(loc is null)return;var why=await Ui.Prompt(this,"Fachliche Begründung des zusätzlichen Quellenbezugs");if(why is null)return;await _workspace.AddSourceAsync(selected.Id,s.Id,loc,why);await Render();},
            help: "Ergänzt eine Quelle am gespeicherten Eintrag. Fundstelle und fachliche Begründung werden abgefragt.");
        var addTopic=Button("Weiterem Thema zuordnen",async()=>{if(selected is null)throw new ArgumentException("Datensatz zuerst speichern.");if(!await CanLeaveFormAsync())return;var pick=await Ui.Pick(this,"Weiteres Thema",_topics.Where(t=>t.Id!=""&&t.Id!="REVIEW").Cast<object>());if(pick is not Choice t)return;var why=await Ui.Prompt(this,"Begründung");if(why is null)return;await _workspace.AddTopicAsync(selected.Id,t.Id,why);await Render();},
            help: "Macht denselben gespeicherten Eintrag in einem zusätzlichen Thema auffindbar. Die Zuordnung braucht eine Begründung.");
        var generalFields = Ui.Stack(
            Ui.Notice(module is not null
                ? "Fachdetails und fachliche Verknüpfungen in den zugehörigen Reitern pflegen."
                : _topicId == ""
                ? isSupplier ? "Gesamtansicht. Zum Anlegen oder Ändern oben ein Fachthema auswählen." : "Gesamtansicht. Zum Speichern ein konkretes Thema auswählen."
                : isSupplier ? "Bewertungen und ihre Folgetermine werden im Reiter Bewertungen dokumentiert."
                : isRisk ? "Eintrittswahrscheinlichkeit, Auswirkung und Behandlung werden unter Bewertungen erfasst."
                : "Quellen und Vorlagen sind auf das Thema eingeschränkt. Zusätzliche Bezüge werden separat begründet."),
            topics);
        var codeField = Ui.Field("Code", code, "Kurzes internes Kennzeichen, um den Eintrag in Listen und Berichten wiederzufinden.");
        var titleField = Ui.Field(isSupplier ? "Lieferantenname *" : "Titel *", title);
        var statusField = Ui.Field("Status", status, kind switch
            {
                "Supplier" => "Freigabe, Auflagen und Ablehnung ergeben sich aus der neuesten Bewertung.",
                "Requirement" => "Hält die fachliche Entscheidung zur Anwendbarkeit der Anforderung fest.",
                "Audit" or "Finding" => "Auditabschluss und Finding-Schließung erfolgen unter Bezüge.",
                _ => "Hält den aktuellen Bearbeitungsstand fest."
            });
        var ownerField = Ui.Field(isSupplier ? "Verantwortung *" : "Verantwortung", owner, "Rolle oder Person angeben, die den Eintrag fachlich betreut.");
        var dueField = Ui.Field(kind switch
            {
                "AiSystem" or "BusinessProcess" => "Nächste Überprüfung",
                "Contract" => "Interner Wiedervorlagetermin",
                "Supplier" => "Nächste Prüfung",
                _ => "Termin"
            }, due, isSupplier
                ? "Prüftermin im Format JJJJ-MM-TT. Falls eine Bewertung vorliegt, muss der Termin danach liegen."
                : "Datum im Format JJJJ-MM-TT eingeben, zum Beispiel 2026-11-30.");
        var scopeField = Ui.Field("Geltungsbereich", scope, "Bereich des Eintrags, etwa ein System, eine Einheit oder ein Prozess. Gespeicherte Bereiche erscheinen im Filter.");
        if (supplier is not null)
        {
            generalFields.Children.Add(titleField);
            generalFields.Children.Add(CompanyColumns(codeField, statusField));
            generalFields.Children.Add(CompanyColumns(ownerField, dueField));
            generalFields.Children.Add(CompanyColumns(scopeField, Ui.Field("Primäre Bezugsquelle *", source,
                "Wählen Sie die maßgebliche Quelle aus dem aktuellen Thema aus. Eigene Referenzen können im Bereich Quellen erfasst werden.")));
            generalFields.Children.Add(supplier.Build());
        }
        else
        {
            generalFields.Children.AddRange([codeField, titleField, statusField, ownerField, dueField, scopeField]);
        }
        generalFields.Children.Add(Ui.Field(isSupplier ? "Interne Notizen" : "Beschreibung / Umsetzung", body));
        generalFields.Children.Add(Ui.Field(isSupplier ? "Änderungsgrund *" : "Änderungs-/Bewertungsbegründung *", reason,
            "Den Anlass der Änderung oder die fachlichen Gründe für die Entscheidung dokumentieren."));
        var general = Ui.Scroll(generalFields);

        var sourceFields = Ui.Stack();
        if (!isSupplier)
            sourceFields.Children.Add(Ui.Field("Primäre Anforderungsquelle", source, "Hauptquelle für diesen Eintrag aus dem gewählten Thema auswählen."));
        sourceFields.Children.Add(Ui.Field("Fundstelle", locator, "Die konkrete Stelle in der Hauptquelle angeben, etwa Artikel 32 Absatz 1."));
        if (!isSupplier)
        {
            sourceFields.Children.Add(Ui.Field("Passende Textvorlage", template, "Zeigt Vorlagen für die gewählte Hauptquelle und diesen Eintragstyp."));
            sourceFields.Children.Add(apply);
        }
        sourceFields.Children.Add(Ui.Text("Zugeordnete Quellen", 16, true));
        sourceFields.Children.Add(refs);
        sourceFields.Children.Add(addSource);
        sourceFields.Children.Add(addTopic);
        var sources = Ui.Scroll(sourceFields);
        var workflow=Ui.Stack(Ui.Text("Verknüpfungen",18,true),related,Button("Mappings öffnen",async()=>{if(!await CanLeaveFormAsync())return;_page="Mappings";await Render();}));
        if (module is not null)
            workflow.Children.Insert(1, Button("Bezug anlegen", () => AddModuleRelationAsync(selected), glyph: Glyph.Mapping));
        if (kind == "Control")
            workflow.Children.Insert(1, Button("Prüfplan und Verantwortung", async () =>
            {
                if (selected is null)
                    throw new ArgumentException("Bitte zuerst ein gespeichertes Control auswählen.");
                if (await CanLeaveFormAsync())
                    await OpenControlReviewPlannerAsync(selected.Id);
            }, glyph: Glyph.Tasks));
        if (isRisk)
            workflow.Children.Add(Ui.Notice("Nachweise sind über zugeordnete Controls erreichbar."));
        if (kind == "Requirement")
        {
            workflow.Children.Add(Button("Abdeckung und Nachweise", async () =>
            {
                if (selected is null)
                    throw new ArgumentException("Eine gespeicherte Anforderung auswählen.");
                var trace = new Window
                {
                    Title = "Abdeckung und Nachweise",
                    Width = 1000,
                    Height = 760,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner
                };
                var close = new Button { Content = "Schließen", IsCancel = true };
                close.Click += (_, _) => trace.Close();
                var content = Ui.Fill(Ui.Text(selected.Title, 20, true), Ui.Scroll(await RequirementTraceAsync(selected.Id)), close);
                content.Margin = new Avalonia.Thickness(20);
                trace.Content = content;
                await trace.ShowDialog(this);
            }, help: "Zeigt bestätigte Control-Zuordnungen und deren Nachweise zu dieser Anforderung."));
        }
        if(kind=="Audit")workflow.Children.Add(Button("Prüfungen dokumentieren",async()=>{if(selected is null)throw new ArgumentException("Audit auswählen.");if(!await CanLeaveFormAsync())return;_body.Content=await AuditPage(selected);},true,
            "Öffnet die Prüferfassung für Controls, die dem gespeicherten Audit unter Mappings zugeordnet sind."));
        if(kind=="Finding")workflow.Children.Add(Button("Mit Wirksamkeitsnachweis schließen",async()=>{
            if(selected is null)throw new ArgumentException("Finding auswählen.");if(!await CanLeaveFormAsync())return;var ids=_mappings.Where(m=>m.From==selected.Id&&m.Type=="Finding → Control").Select(m=>m.To);var eligible=new List<Proof>();foreach(var id in ids)eligible.AddRange(await _workspace.EvidenceAsync(id));var pick=await Ui.Pick(this,"Wirksamkeitsnachweis",eligible.DistinctBy(p=>p.Id).Cast<object>());if(pick is not Proof p)return;var review=await Ui.Prompt(this,"Wirksamkeitsprüfung und Ergebnis dokumentieren");if(review is null)return;await _workspace.CloseFindingAsync(selected.Id,p.Id,review);await Render();},
            help: "Das Finding braucht den Status Wirksamkeitsprüfung und einen gültigen Nachweis für alle verknüpften Controls."));
        var tabItems = new List<TabItem> { Ui.Tab("Details", general) };
        TabItem? moduleTab = null;
        if (module is not null)
        {
            moduleTab = Ui.Tab("Fachdetails", module.Content);
            tabItems.Add(moduleTab);
        }
        if (reviews is not null && addReview is not null)
            tabItems.Add(Ui.Tab("Bewertungen", Ui.Fill(Ui.Row(addReview), Ui.Scroll(reviews)),
                "Zeigt Ergebnisse, Begründungen und Folgetermine der Lieferantenprüfungen."));
        TabItem? riskTab = null;
        if (riskAssessments is not null && assessRisk is not null)
        {
            riskTab = Ui.Tab("Bewertungen", Ui.Fill(Ui.Row(assessRisk), Ui.Scroll(riskAssessments)),
                "Zeigt Ausgangsrisiko, Restrisiko, Behandlung und frühere Bewertungen.");
            tabItems.Add(riskTab);
        }
        tabItems.Add(Ui.Tab("Quellen", sources));
        tabItems.Add(Ui.Tab("Bezüge", Ui.Scroll(workflow), "Zeigt gespeicherte Zuordnungen und verfügbare Prüfschritte."));
        if (!isSupplier && !isRisk)
            tabItems.Add(Ui.Tab("Evidence", proofs, "Dateinachweise gehören zu Controls. Bei anderen Einträgen die verknüpften Controls prüfen."));
        tabItems.Add(Ui.Tab("Historie", history, "Zeigt protokollierte Aktivitäten zu diesem Eintrag."));
        var tabs = new TabControl { ItemsSource = tabItems };
        var save=Button("Änderungen speichern",SaveEditorButtonAsync,true, "Speichert den Eintrag mit der angegebenen Begründung im Projekt.");
        save.IsEnabled = !supplierOverview;
        var cancel = Button("Abbrechen", async () =>
        {
            if (!await ConfirmDiscardFormAsync())
                return;
            await LoadEntry(selected);
            AcceptFormState();
        }, help: "Kehrt nach Bestätigung zur gespeicherten Fassung zurück.");
        var duplicate=Button("Duplizieren",async()=>{if(selected is null)return;if(!await CanLeaveFormAsync())return;await LoadEntry(selected);selected=null;list.SelectedItem=null;code.Text=(code.Text??"")+"-KOPIE";SetStatusChoices(Entry.Statuses(kind)[0]);
            if (reviews is not null && addReview is not null){due.Text="";PresentSupplierReviews(reviews,addReview,null,[]);}
            if (riskAssessments is not null && assessRisk is not null)
                PresentRiskAssessments(riskAssessments, assessRisk, null, []);
            UpdateFormStatus();},
            help: "Übernimmt die Formularwerte als Entwurf für einen neuen Eintrag. Erst Speichern legt die Kopie an.");
        duplicate.IsEnabled = !supplierOverview;
        var delete=Button("Löschen",async()=>{if(selected is null)return;if(!await CanLeaveFormAsync())return;var why=await Ui.Prompt(this,"Löschbegründung (verknüpfte Datensätze bleiben geschützt)");if(why is null)return;await _workspace.DeleteAsync(selected.Id,why);await Render();},
            help: "Löscht den ausgewählten Eintrag nach Eingabe einer Begründung. Verknüpfte Einträge sind vor dem Löschen geschützt.");
        var create = Button(isSupplier ? "Lieferant anlegen" : module is not null ? $"{module.Spec.Singular} anlegen" : "Neuer Eintrag", async () =>
        {
            if (!await CanLeaveFormAsync())
                return;
            if (supplierOverview)
            {
                if (await Ui.Pick(this, "Fachthema für den neuen Lieferanten", _topics.Where(t => t.Id is not ("" or "REVIEW")).Cast<object>()) is not Choice topic)
                    return;
                _topicId = topic.Id;
                _statusFilter = "";
                await Render();
                return;
            }
            Clear();
            list.SelectedItem = null;
            AcceptFormState();
            if (isSupplier || isRisk)
            {
                tabs.SelectedIndex = 0;
                title.Focus();
            }
        });
        if (_requestedModuleEntryId is { } requestedId)
        {
            _requestedModuleEntryId = null;
            var requested = ((IEnumerable<Entry>)list.ItemsSource!).FirstOrDefault(entry => entry.Id == requestedId);
            if (requested is not null)
            {
                await LoadEntry(requested);
                list.SelectedItem = selected;
                if (moduleTab is not null)
                    tabs.SelectedItem = moduleTab;
                AcceptFormState();
            }
        }
        if (riskTab is not null && _riskToFocus is { } requestedRiskId &&
            ((IEnumerable<Entry>)list.ItemsSource!).FirstOrDefault(e => e.Id == requestedRiskId) is { } requestedRisk)
        {
            await LoadEntry(requestedRisk);
            list.SelectedItem = selected;
            tabs.SelectedItem = riskTab;
            AcceptFormState();
            _riskToFocus = null;
        }
        return Ui.Split(Ui.Card(Ui.Fill(Ui.Stack(Ui.Row(create),
            Ui.Field("Listenstatus", statusFilter, "Filtert nur diese Liste. Der Export richtet sich nach den globalen Filtern."),
            Ui.Field("Sortierung", sort, "Sortiert diese Liste. Beim Sortieren nach Termin stehen Einträge ohne Datum am Ende.")), list)),
            Ui.Card(Ui.Fill(Ui.Text("Details · "+Pages[kind],18,true),tabs,Ui.Row(save,cancel,duplicate,delete))));
    }
}
