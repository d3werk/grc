using System.Globalization;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Layout;
using D3.Grc.Core;

namespace D3.Grc.Desktop;

public sealed partial class MainWindow
{
    private TextTemplate? _documentStarter;

    private async Task StartDocumentFromTemplateAsync(TextTemplate template)
    {
        _documentStarter = template;
        try
        {
            _page = "Documents";
            _statusFilter = "";
            await Render();
            UpdateFormStatus();
        }
        finally
        {
            _documentStarter = null;
        }
    }

    private async Task<Control> DocumentsPage()
    {
        var records = new List<DocumentRecord>();
        var documentList = new ListBox
        {
            ItemTemplate = new FuncDataTemplate<DocumentRecord>((record, _) => record is null ? null : Ui.Stack(
                Ui.Text(record.Draft.Title, 14, true),
                Ui.Secondary($"{DocumentRegister.CategoryLabels[record.Draft.Category]} · {DocumentRegister.StatusLabels[record.Status]}"),
                Ui.Secondary($"Version {record.Version} · {record.Draft.Owner}", 12)))
        };
        AutomationProperties.SetName(documentList, "Dokumente im aktuellen Filter");
        var stateFilter = Ui.Combo();
        const string allStates = "Alle Status";
        stateFilter.ItemsSource = new[] { allStates }.Concat(DocumentRegister.StatusLabels.Values).ToArray();
        stateFilter.SelectedItem = _statusFilter.Length == 0 ? allStates : _statusFilter;
        if (stateFilter.SelectedIndex < 0)
            stateFilter.SelectedIndex = 0;
        var appliedState = stateFilter.SelectedItem as string ?? allStates;
        var listContent = new ContentControl();
        var count = Ui.Secondary("");
        var emptyList = Ui.Notice("Keine Dokumente im aktuellen Filter. Ändern Sie den Filter oder legen Sie ein Dokument an.");
        emptyList.VerticalAlignment = VerticalAlignment.Top;

        var title = Ui.Input("z. B. Richtlinie zur Informationssicherheit");
        var category = Ui.Combo();
        category.ItemsSource = DocumentRegister.CategoryLabels.Values.ToList();
        var topic = Ui.Combo();
        var availableTopics = _topics.Where(choice => choice.Id.Length > 0).ToList();
        topic.ItemsSource = availableTopics;
        var scope = Ui.Input("z. B. Produktentwicklung");
        var owner = Ui.Input("Name oder Rolle");
        var sourceText = new RichTextEditor("Dokumenttext", "Dokumenttext eingeben", editorHeight: 350, maximumLength: 100000);
        title.MaxLength = 500;
        scope.MaxLength = 1000;
        owner.MaxLength = 240;
        var editorFields = Ui.Stack(
            Ui.Field("Dokumenttitel *", title),
            CompanyColumns(Ui.Field("Dokumentart *", category, "Ordnet den Entwurf als Richtlinie, Verfahren oder anderen Dokumenttyp ein."),
                Ui.Field("Thema *", topic, "Bestimmt, in welchem Fachthema dieses Dokument im Register erscheint.")),
            CompanyColumns(Ui.Field("Verantwortung *", owner, "Wählen Sie die fachlich verantwortliche Person oder Rolle."),
                Ui.Field("Geltungsbereich", scope, "Gespeicherte Bereiche erscheinen im Projektfilter.")),
            Ui.Field("Dokumenttext", sourceText,
                "Text direkt gestalten. Die Vorschau zeigt die aktuelle Fassung auch vor dem Speichern."));

        var heading = Ui.Heading("Dokument auswählen");
        var lifecycle = Ui.Secondary("");
        var auditState = Ui.Secondary("");
        var workflow = Ui.Row();
        var preview = new ContentControl();
        var previewTitle = Ui.Heading("Vorschau", 17, 3);
        var editorArea = new ContentControl();
        var versionList = new ListBox
        {
            ItemTemplate = new FuncDataTemplate<DocumentVersion>((version, _) => version is null ? null : Ui.Stack(
                Ui.Text($"Version {version.Version} · {version.Draft.Title}", 14, true),
                Ui.Secondary($"Veröffentlicht von {version.PublishedBy} · {DocumentTimestamp(version.PublishedUtc)}", 12)))
        };
        AutomationProperties.SetName(versionList, "Veröffentlichte Dokumentversionen");
        var versionDetails = new ContentControl();
        var versionArea = new ContentControl();
        var linkList = new ListBox
        {
            ItemTemplate = new FuncDataTemplate<DocumentLink>((link, _) => link is null ? null : Ui.Stack(
                Ui.Text(link.Title, 14, true),
                Ui.Secondary(DocumentRegister.LinkTargetLabels[link.Kind], 12),
                Ui.Secondary(link.Reason)))
        };
        AutomationProperties.SetName(linkList, "Zugeordnete Anforderungen und Controls");
        var linkArea = new ContentControl();
        var tabControl = new TabControl();
        var editorTab = Ui.Tab("Text", editorArea, "Stammdaten und Dokumenttext anzeigen oder im Entwurf bearbeiten.");
        var previewTab = Ui.Tab("Vorschau", Ui.Scroll(Ui.Stack(previewTitle, preview)),
            "Zeigt die aktuell angezeigte Fassung einschließlich noch ungespeicherter Entwurfsänderungen.");
        var versionsTab = Ui.Tab("Versionen", versionArea, "Gespeicherte Veröffentlichungen auswählen und mit ihren Freigabeangaben ansehen.");
        var linksTab = Ui.Tab("Bezüge", linkArea, "Anforderungen und Controls mit diesem Dokument verknüpfen.");
        tabControl.ItemsSource = new[] { previewTab, editorTab, versionsTab, linksTab };
        tabControl.SelectedItem = previewTab;
        var save = Button("Entwurf speichern", SaveEditorButtonAsync, true,
            "Speichert die bearbeitete Fassung. Die Veröffentlichung erfolgt nach der fachlichen Prüfung.", Glyph.Save);
        var cancel = Ui.Help(new Button { Content = "Abbrechen", MinHeight = 37 },
            "Verwirft nach Bestätigung ungespeicherte Änderungen und öffnet die gespeicherte Fassung.");
        cancel.Classes.Add("grc-action");
        var editorActions = Ui.Row(save, cancel);
        editorActions.Margin = new Thickness(0, 12, 0, 0);
        DockPanel.SetDock(editorActions, Dock.Bottom);
        var editorView = new DockPanel { Children = { editorActions, Ui.Scroll(editorFields) } };
        var versionRows = new Grid
        {
            RowDefinitions = new RowDefinitions("145,*"),
            RowSpacing = 8
        };
        versionRows.Children.Add(versionList);
        Grid.SetRow(versionDetails, 1);
        versionRows.Children.Add(versionDetails);
        var fullVersionArea = Ui.Fill(Ui.Heading("Veröffentlichte Fassungen", 17, 3), versionRows);

        DocumentRecord? selected = null;
        var editingNew = false;
        DocumentStarter? generated = null;
        var addRequirement = Ui.Help(new Button { Content = "Anforderung zuordnen", MinHeight = 38 },
            "Verknüpft eine vorhandene Anforderung aus dem Projekt und protokolliert die Begründung.");
        var addControl = Ui.Help(new Button { Content = "Control zuordnen", MinHeight = 38 },
            "Verknüpft ein vorhandenes Control aus dem Projekt und protokolliert die Begründung.");
        var removeLink = Ui.Help(new Button { Content = "Zuordnung entfernen", MinHeight = 38 },
            "Löst die ausgewählte Beziehung. Die Anforderung oder das Control bleibt erhalten.");
        var linkActions = Ui.Row(addRequirement, addControl, removeLink);
        var noLinks = Ui.Notice("Diesem Dokument sind noch keine Anforderungen oder Controls zugeordnet.");
        noLinks.VerticalAlignment = VerticalAlignment.Top;
        var fullLinkArea = Ui.Fill(linkActions, new Panel { Children = { linkList, noLinks } });
        var tabsHost = new ContentControl();
        var emptySelection = Ui.Notice("Wählen Sie links ein Dokument aus oder legen Sie ein neues an.");
        emptySelection.VerticalAlignment = VerticalAlignment.Top;
        var create = Button("Neues Dokument", async () =>
        {
            if (!await CanLeaveFormAsync())
                return;
            await BeginDraftAsync();
            AcceptFormState();
        }, true, "Beginnt einen Entwurf mit einem ausformulierten Gerüst zum gewählten Fachthema.", Glyph.Plus);
        var fromTemplate = Button("Aus Vorlage", async () =>
        {
            var template = await ChooseDocumentTemplateAsync();
            if (template is null || !await CanLeaveFormAsync())
                return;
            await BeginDraftAsync(template);
            AcceptFormState();
        }, help: "Vorschau öffnen und eine bearbeitbare Kopie des ausgewählten Vorlagentexts anlegen.", glyph: Glyph.Templates);

        void PopulateList()
        {
            var visible = records.Where(record => appliedState == allStates ||
                DocumentRegister.StatusLabels[record.Status] == appliedState).ToList();
            documentList.ItemsSource = visible;
            documentList.SelectedItem = visible.FirstOrDefault(record => record.Id == selected?.Id);
            count.Text = visible.Count == 1 ? "1 Dokument" : $"{visible.Count} Dokumente";
            listContent.Content = visible.Count > 0 ? documentList : emptyList;
        }

        async Task RefreshListAsync()
        {
            records = await _workspace.DocumentsAsync(_topicId, EffectiveScope, _searchValue);
            PopulateList();
        }

        void PresentDraft(DocumentRecord? document, TextTemplate? starter = null, string? starterTopic = null)
        {
            selected = document;
            generated = null;
            var draft = document?.Draft;
            var isEditable = editingNew || document?.Status == DocumentStatus.Draft;
            var visible = document is not null || editingNew;
            heading.Text = document?.Draft.Title ?? (editingNew ? "Neues Dokument" : "Dokument auswählen");
            lifecycle.Text = document is not null
                ? DescribeDocument(document)
                : editingNew ? "Entwurf · noch nicht gespeichert" : "";
            lifecycle.IsVisible = visible;
            auditState.IsVisible = !string.IsNullOrWhiteSpace(document?.ReviewedBy);
            auditState.Text = document is not null && auditState.IsVisible
                ? $"Geprüft von {document.ReviewedBy} · {DocumentTimestamp(document.ReviewedUtc)}"
                : "";
            tabsHost.Content = visible ? tabControl : emptySelection;
            editorArea.Content = editorView;
            var initialCategory = TemplateDocumentCategory(starter?.Kind);
            var selectedCategory = draft?.Category ?? initialCategory;
            var selectedTopic = draft?.TopicId ?? starterTopic ?? _topicId;
            category.SelectedItem = DocumentRegister.CategoryLabels[selectedCategory];
            topic.SelectedItem = availableTopics.FirstOrDefault(choice => choice.Id == selectedTopic);
            scope.Text = draft?.Scope ?? (editingNew ? EffectiveScope : "");
            owner.Text = draft?.Owner ?? "";
            if (editingNew && string.IsNullOrWhiteSpace(RichTextContent.Plain(starter?.Effective ?? "")))
                generated = DocumentRegister.Starter(selectedTopic, selectedCategory);
            title.Text = draft?.Title ?? starter?.Title ?? generated?.Title ?? "";
            sourceText.Content = draft?.Content ?? generated?.Content ?? starter?.Effective ?? "";
            title.IsReadOnly = scope.IsReadOnly = owner.IsReadOnly = sourceText.IsReadOnly = !isEditable;
            category.IsEnabled = topic.IsEnabled = isEditable;
            save.IsVisible = cancel.IsVisible = isEditable;
            addRequirement.IsVisible = addControl.IsVisible = removeLink.IsVisible = document is { Status: not DocumentStatus.Retired };
            BuildActions(document?.Status);
        }

        async Task BeginDraftAsync(TextTemplate? template = null)
        {
            var templateTopics = template is null ? [] : await _workspace.TemplateTopicChoicesAsync(template.Id);
            var current = availableTopics.FirstOrDefault(choice => choice.Id == _topicId);
            var starterTopic = templateTopics.FirstOrDefault(choice => choice.Id == _topicId)
                ?? templateTopics.FirstOrDefault()
                ?? current
                ?? availableTopics.FirstOrDefault(choice => choice.Id == "ISMS")
                ?? availableTopics.FirstOrDefault();
            editingNew = true;
            documentList.SelectedItem = null;
            PresentDraft(null, template, starterTopic?.Id);
            versionArea.Content = Ui.Notice("Veröffentlichte Fassungen erscheinen hier nach der ersten Veröffentlichung.");
            linkArea.Content = Ui.Notice("Bezüge können nach dem Speichern des Dokuments erfasst werden.");
            tabControl.SelectedItem = editorTab;
        }

        void UpdateGeneratedStarter()
        {
            if (generated is not { } previous || sourceText.Content != previous.Content ||
                topic.SelectedItem is not Choice selectedTopic || category.SelectedItem is not string categoryLabel)
                return;
            var next = DocumentRegister.Starter(selectedTopic.Id,
                DocumentRegister.CategoryLabels.Single(option => option.Value == categoryLabel).Key);
            generated = next;
            if (title.Text == previous.Title)
                title.Text = next.Title;
            sourceText.Content = next.Content;
        }

        void UpdatePreview()
        {
            if (!ReferenceEquals(tabControl.SelectedItem, previewTab))
                return;
            previewTitle.Text = selected?.Status switch
            {
                DocumentStatus.Published => $"Veröffentlichte Fassung · Version {selected.Version}",
                DocumentStatus.Retired => $"Außer Kraft · Version {selected.Version}",
                DocumentStatus.Reviewed => $"Geprüfte Fassung · Version {selected.Version}",
                DocumentStatus.InReview => $"Fassung in Prüfung · Version {selected.Version}",
                _ => HasFormChanges ? "Vorschau · Ungespeicherter Entwurf" : "Entwurfsvorschau"
            };
            preview.Content = string.IsNullOrWhiteSpace(sourceText.PlainText)
                ? Ui.Notice("Für diese Fassung ist noch kein Dokumenttext vorhanden.")
                : DocumentPreview.Create(sourceText.Content);
        }

        void ShowVersion(DocumentVersion? version)
        {
            if (!ReferenceEquals(tabControl.SelectedItem, versionsTab))
                return;
            if (version is null)
            {
                versionDetails.Content = Ui.Notice("Wählen Sie eine veröffentlichte Fassung aus.");
                return;
            }
            var draft = version.Draft;
            versionDetails.Content = Ui.Scroll(Ui.Stack(
                Ui.Heading(draft.Title, 19, 3),
                Ui.Secondary(string.Join(" · ", new[] { DocumentRegister.CategoryLabels[draft.Category],
                    availableTopics.FirstOrDefault(t => t.Id == draft.TopicId)?.Title ?? draft.TopicId,
                    draft.Scope, draft.Owner }.Where(value => value.Length > 0))),
                DocumentPreview.Create(draft.Content),
                Ui.Card(Ui.Stack(
                    Ui.Heading("Prüfung und Veröffentlichung", 15, 4),
                    Ui.Secondary($"Geprüft von {version.ReviewedBy} · {DocumentTimestamp(version.ReviewedUtc)}"),
                    Ui.Text(version.ReviewNote),
                    Ui.Secondary($"Veröffentlicht von {version.PublishedBy} · {DocumentTimestamp(version.PublishedUtc)}"),
                    Ui.Text(version.PublicationNote)))));
        }

        async Task RefreshVersionsAsync(DocumentRecord document)
        {
            var requestedVersion = (versionList.SelectedItem as DocumentVersion)?.Version;
            var versions = await _workspace.DocumentVersionsAsync(document.Id);
            versionArea.Content = versions.Count == 0
                ? Ui.Notice("Für dieses Dokument wurde noch keine Fassung veröffentlicht.")
                : fullVersionArea;
            versionList.ItemsSource = versions;
            versionList.SelectedItem = versions.FirstOrDefault(v => v.Version == requestedVersion) ?? versions.FirstOrDefault();
            ShowVersion(versionList.SelectedItem as DocumentVersion);
        }

        async Task RefreshLinksAsync(DocumentRecord document)
        {
            var links = await _workspace.DocumentLinksAsync(document.Id);
            linkList.ItemsSource = links;
            linkList.IsVisible = links.Count > 0;
            noLinks.IsVisible = links.Count == 0;
            removeLink.IsEnabled = false;
            linkArea.Content = fullLinkArea;
        }

        async Task LoadDocumentAsync(object? item)
        {
            editingNew = false;
            var document = item is DocumentRecord record ? await _workspace.DocumentAsync(record.Id) : null;
            PresentDraft(document);
            if (document is null)
                return;
            versionList.SelectedItem = null;
            await RefreshVersionsAsync(document);
            await RefreshLinksAsync(document);
            AcceptFormState();
            tabControl.SelectedItem = previewTab;
            UpdatePreview();
        }

        async Task DiscardDocumentEditsAsync()
        {
            if (!await ConfirmDiscardFormAsync())
                return;
            await LoadDocumentAsync(selected);
            AcceptFormState();
        }

        async Task ApplyDocumentChangeAsync(string id, Func<Task<DocumentRecord>> change, TabItem nextTab)
        {
            WorkspacePersistenceException? persistenceFailure = null;
            DocumentRecord document;
            try
            {
                document = await change();
            }
            catch (WorkspacePersistenceException error)
            {
                _refreshAfterPersistence = true;
                persistenceFailure = error;
                document = await _workspace.DocumentAsync(id);
            }

            editingNew = false;
            PresentDraft(document);
            AcceptFormState();
            tabControl.SelectedItem = nextTab;
            UpdatePreview();
            await RefreshScopeOptionsAsync();
            await RefreshListAsync();
            await RefreshVersionsAsync(document);
            await RefreshLinksAsync(document);
            if (documentList.SelectedItem is null)
            {
                lifecycle.Text += " · Außerhalb des aktuellen Listenfilters";
            }
            if (persistenceFailure is not null)
                throw persistenceFailure;
        }

        async Task<bool> SaveDraftAsync()
        {
            if (selected is not null && selected.Status != DocumentStatus.Draft)
                throw new InvalidOperationException("Nur ein Entwurf kann bearbeitet werden.");
            if (string.IsNullOrWhiteSpace(title.Text) || string.IsNullOrWhiteSpace(owner.Text))
                throw new ArgumentException("Dokumenttitel und Verantwortung eingeben.");
            if (category.SelectedItem is not string categoryLabel || topic.SelectedItem is not Choice selectedTopic)
                throw new ArgumentException("Dokumentart und Fachthema auswählen.");
            var selectedCategory = DocumentRegister.CategoryLabels.Single(option => option.Value == categoryLabel).Key;
            var draft = new DocumentDraft(title.Text ?? "", selectedTopic.Id, scope.Text ?? "", selectedCategory,
                owner.Text ?? "", sourceText.Content);
            var reason = await Ui.Prompt(this, selected is null ? "Anlass für das neue Dokument" : "Grund für die Änderungen am Entwurf");
            if (reason is null)
                return false;
            var existing = selected;
            var id = existing?.Id ?? Guid.NewGuid().ToString("N");
            await ApplyDocumentChangeAsync(id, () => existing is null
                ? _workspace.CreateDocumentAsync(draft, reason, id)
                : _workspace.SaveDocumentDraftAsync(existing.Id, existing.Revision, draft, reason), editorTab);
            return true;
        }

        async Task SavePendingDocumentFileAsync()
        {
            if (!_refreshAfterPersistence)
                return;
            await _workspace.SaveAsync();
            _refreshAfterPersistence = false;
        }

        void BuildActions(DocumentStatus? status)
        {
            workflow.Children.Clear();
            void Add(string label, string reasonPrompt, Func<string, int, string, Task<DocumentRecord>> action, TabItem target,
                bool primary, string help)
            {
                workflow.Children.Add(Button(label, async () =>
                {
                    if (selected is null || !await CanLeaveFormAsync())
                        return;
                    var document = selected;
                    var reason = await Ui.Prompt(this, reasonPrompt);
                    if (reason is null)
                        return;
                    await SavePendingDocumentFileAsync();
                    await ApplyDocumentChangeAsync(document.Id, () => action(document.Id, document.Revision, reason), target);
                }, primary, help));
            }

            switch (status)
            {
                case DocumentStatus.Draft:
                    Add("Zur Prüfung einreichen", "Anlass für die Prüfung", _workspace.SubmitDocumentForReviewAsync, previewTab, true,
                        "Gibt den gespeicherten Text zur fachlichen Prüfung frei. Während der Prüfung ist die Fassung schreibgeschützt.");
                    break;
                case DocumentStatus.InReview:
                    Add("Prüfung bestätigen", "Prüfergebnis und fachliche Freigabe begründen", _workspace.ApproveDocumentAsync, previewTab, true,
                        "Hält die fachliche Prüfung mit Ihrem lokalen Profil fest. Die Veröffentlichung erfolgt separat.");
                    break;
                case DocumentStatus.Reviewed:
                    Add("Veröffentlichen", "Grund für die Veröffentlichung dieser Version", _workspace.PublishDocumentAsync, previewTab, true,
                        "Erstellt eine unveränderliche veröffentlichte Fassung und übernimmt sie als gültige Version.");
                    break;
                case DocumentStatus.Published:
                    Add("Neue Version beginnen", "Grund für die Überarbeitung", _workspace.StartDocumentRevisionAsync, editorTab, true,
                        "Eröffnet einen neuen Entwurf. Die bisherige Veröffentlichung bleibt verfügbar.");
                    Add("Außer Kraft setzen", "Grund für die Außerkraftsetzung", _workspace.RetireDocumentAsync, previewTab, false,
                        "Setzt das Dokument außer Kraft. Die bisherigen Versionen bleiben im Register sichtbar.");
                    break;
            }
            if (status is DocumentStatus.InReview or DocumentStatus.Reviewed)
                Add("Zurück in den Entwurf", "Welche Änderungen sind noch erforderlich?", _workspace.ReturnDocumentToDraftAsync, editorTab, false,
                    "Erlaubt wieder Änderungen am Dokumenttext. Die weitere Bearbeitung beginnt im Entwurfsstatus.");
            workflow.IsVisible = workflow.Children.Count > 0;
        }

        async Task AddLinkAsync(DocumentLinkTargetKind kind)
        {
            if (selected is not { Status: not DocumentStatus.Retired } document)
                return;
            var entries = await _workspace.EntriesAsync(kind: kind == DocumentLinkTargetKind.Requirement ? "Requirement" : "Control");
            var linked = (await _workspace.DocumentLinksAsync(document.Id)).Where(link => link.Kind == kind)
                .Select(link => link.TargetId).ToHashSet();
            var newChoices = entries.Where(entry => !linked.Contains(entry.Id)).Select(entry => new Choice(entry.Id,
                string.Join(" · ", new[] { entry.Code, entry.Title, entry.Scope }.Where(value => value.Length > 0)))).ToList();
            if (newChoices.Count == 0)
            {
                await Ui.Confirm(this, "Für diesen Typ stehen keine weiteren Einträge zur Zuordnung bereit.", "Verstanden");
                return;
            }
            if (await Ui.Pick(this, $"{DocumentRegister.LinkTargetLabels[kind]} auswählen", newChoices.Cast<object>()) is not Choice target)
                return;
            var reason = await Ui.Prompt(this, "Fachlichen Bezug zum Dokument begründen");
            if (reason is null)
                return;
            await ChangeLinksAsync(document, () => _workspace.LinkDocumentAsync(document.Id, kind, target.Id, reason));
        }

        async Task RemoveLinkAsync()
        {
            if (selected is not { Status: not DocumentStatus.Retired } document || linkList.SelectedItem is not DocumentLink link)
                return;
            var reason = await Ui.Prompt(this, $"Warum soll die Zuordnung zu „{link.Title}“ entfernt werden?");
            if (reason is null)
                return;
            await ChangeLinksAsync(document, () => _workspace.UnlinkDocumentAsync(document.Id, link.Id, reason));
        }

        async Task ChangeLinksAsync(DocumentRecord document, Func<Task> change)
        {
            await SavePendingDocumentFileAsync();
            try
            {
                await change();
            }
            catch (WorkspacePersistenceException)
            {
                await RefreshLinksAsync(document);
                throw;
            }
            await RefreshLinksAsync(document);
            UpdateFormStatus();
        }

        SafeSelection(documentList, () => selected, LoadDocumentAsync);
        category.SelectionChanged += (_, _) => UpdateGeneratedStarter();
        topic.SelectionChanged += (_, _) => UpdateGeneratedStarter();
        TrackForm(() => (title.Text ?? "", category.SelectedItem, (topic.SelectedItem as Choice)?.Id,
                scope.Text ?? "", owner.Text ?? "", sourceText.Content), SaveDraftAsync, null,
            title, category, topic, scope, owner, sourceText);
        stateFilter.SelectionChanged += async (_, _) =>
        {
            if (_busy || _loading)
                return;
            var next = stateFilter.SelectedItem as string ?? allStates;
            await Run(async () =>
            {
                stateFilter.SelectedItem = appliedState;
                if (next == appliedState || !await CanLeaveFormAsync())
                    return;
                appliedState = next;
                _statusFilter = next == allStates ? "" : next;
                stateFilter.SelectedItem = appliedState;
                PopulateList();
                await LoadDocumentAsync(documentList.SelectedItem);
                AcceptFormState();
            });
        };
        cancel.Click += async (_, _) => await Run(DiscardDocumentEditsAsync);
        addRequirement.Click += async (_, _) => await Run(() => AddLinkAsync(DocumentLinkTargetKind.Requirement));
        addControl.Click += async (_, _) => await Run(() => AddLinkAsync(DocumentLinkTargetKind.Control));
        removeLink.Click += async (_, _) => await Run(RemoveLinkAsync);
        tabControl.SelectionChanged += (_, e) =>
        {
            if (!ReferenceEquals(e.Source, tabControl))
                return;
            UpdatePreview();
            ShowVersion(versionList.SelectedItem as DocumentVersion);
        };
        versionList.SelectionChanged += (_, _) => ShowVersion(versionList.SelectedItem as DocumentVersion);
        linkList.SelectionChanged += (_, _) => removeLink.IsEnabled = linkList.SelectedItem is DocumentLink;
        PresentDraft(null);
        AcceptFormState();
        await RefreshListAsync();
        if (_documentStarter is { } starter)
        {
            await BeginDraftAsync(starter);
            AcceptFormState();
        }
        UpdateFormStatus();
        return Ui.Split(
            Ui.Card(Ui.Fill(Ui.Stack(Ui.Heading("Dokumentenregister"), count, Ui.Row(create, fromTemplate),
                Ui.Field("Status", stateFilter, "Beschränkt die Liste auf einen Status. Thema, Geltungsbereich und Suche gelten zusätzlich.")), listContent)),
            Ui.Card(Ui.Fill(Ui.Stack(heading, lifecycle, auditState, workflow), tabsHost)));
    }

    private async Task<TextTemplate?> ChooseDocumentTemplateAsync()
    {
        var templates = await _workspace.TemplatesAsync(_topicId);
        var window = new Window
        {
            Title = "Dokumentvorlage auswählen", Width = 940, Height = 650, MinWidth = 780, MinHeight = 520,
            WindowStartupLocation = WindowStartupLocation.CenterOwner
        };
        var search = Ui.Input("Titel, Dokumentart oder Kennung");
        search.MaxLength = 250;
        var count = Ui.Secondary("");
        var list = new ListBox
        {
            ItemTemplate = new FuncDataTemplate<TextTemplate>((item, _) => item is null ? null : Ui.Stack(
                Ui.Text(item.Title, 14, true), Ui.Secondary(DocumentRegister.CategoryLabels[TemplateDocumentCategory(item.Kind)], 12)))
        };
        AutomationProperties.SetName(list, "Gefundene Dokumentvorlagen");
        var preview = new ContentControl();
        var use = new Button { Content = "Als Entwurf übernehmen", IsDefault = true, MinHeight = 38 };
        var cancel = new Button { Content = "Abbrechen", IsCancel = true, MinHeight = 38 };
        use.Click += (_, _) =>
        {
            if (list.SelectedItem is TextTemplate template)
                window.Close(template);
        };
        cancel.Click += (_, _) => window.Close(null);

        void ShowPreview()
        {
            var selectedTemplate = list.SelectedItem as TextTemplate;
            use.IsEnabled = selectedTemplate is not null;
            preview.Content = selectedTemplate is null
                ? Ui.Notice("Wählen Sie eine Vorlage aus, um den Text zu lesen.")
                : Ui.Stack(Ui.Heading(selectedTemplate.Title, 18), DocumentPreview.Create(selectedTemplate.Effective));
        }

        void Filter()
        {
            var term = search.Text?.Trim() ?? "";
            var current = (list.SelectedItem as TextTemplate)?.Id;
            var filtered = templates.Where(item => term.Length == 0 ||
                new[] { item.Title, item.Id, DocumentRegister.CategoryLabels[TemplateDocumentCategory(item.Kind)] }
                    .Any(value => value.Contains(term, StringComparison.CurrentCultureIgnoreCase))).ToList();
            list.ItemsSource = filtered;
            list.SelectedItem = filtered.FirstOrDefault(item => item.Id == current) ?? filtered.FirstOrDefault();
            count.Text = filtered.Count == 1 ? "1 Vorlage" : $"{filtered.Count} Vorlagen";
        }

        search.TextChanged += (_, _) => Filter();
        list.SelectionChanged += (_, _) => ShowPreview();
        ShowPreview();
        Filter();
        var templateList = Ui.Card(Ui.Fill(Ui.Stack(Ui.Field("Vorlagen durchsuchen", search), count), list));
        var body = Ui.Fill(Ui.Heading("Dokument aus Vorlage", 22, 1), Ui.Split(templateList, Ui.Scroll(preview)), Ui.Row(cancel, use));
        body.Margin = new Thickness(18);
        window.Content = body;
        window.Opened += (_, _) => search.Focus();
        return await window.ShowDialog<TextTemplate?>(this);
    }

    private static DocumentCategory TemplateDocumentCategory(string? kind) => kind switch
    {
        "Procedure" or "Control" or "Measure" => DocumentCategory.Procedure,
        "WorkInstruction" or "Audit" or "AuditTest" => DocumentCategory.WorkInstruction,
        "Guidance" or "Risk" or "AiSystem" => DocumentCategory.Guidance,
        "Standard" => DocumentCategory.Standard,
        "Other" => DocumentCategory.Other,
        _ => DocumentCategory.Policy
    };

    private static string DescribeDocument(DocumentRecord document)
    {
        var state = DocumentRegister.StatusLabels[document.Status];
        var current = $"{state} · Version {document.Version} · Geändert {DocumentTimestamp(document.UpdatedUtc)}";
        return document.PublishedVersion is { } published && published != document.Version
            ? $"{current} · Weiterhin veröffentlicht: Version {published}"
            : current;
    }

    private static string DocumentTimestamp(string timestamp) =>
        DateTimeOffset.TryParse(timestamp, CultureInfo.InvariantCulture, DateTimeStyles.None, out var time)
            ? time.ToLocalTime().ToString("dd.MM.yyyy HH:mm", CultureInfo.GetCultureInfo("de-DE"))
            : timestamp;
}
