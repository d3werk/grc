using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using D3.Grc.Core;

namespace D3.Grc.Desktop;

public sealed partial class MainWindow
{
    private static readonly Dictionary<string, (string Title, Glyph Icon)> PageDefinitions = new()
    {
        ["Dashboard"] = ("Übersicht", Glyph.Dashboard),
        ["Requirement"] = ("Anforderungen", Glyph.Requirements),
        ["Control"] = ("Controls", Glyph.Shield),
        ["Risk"] = ("Risiken", Glyph.Risk),
        ["RiskMatrix"] = ("Risikomatrix", Glyph.RiskMatrix),
        ["Supplier"] = ("Lieferanten", Glyph.Supplier),
        ["Measure"] = ("Maßnahmen", Glyph.Tasks),
        ["Mappings"] = ("Mappings", Glyph.Mapping),
        ["Evidence"] = ("Evidence", Glyph.Evidence),
        ["Audit"] = ("Audits", Glyph.Audit),
        ["Finding"] = ("Findings", Glyph.Finding),
        ["AiSystem"] = ("KI-Systeme", Glyph.Ai),
        ["OssComponent"] = ("Open Source", Glyph.Code),
        ["Documents"] = ("Dokumente", Glyph.Documents),
        ["Sources"] = ("Quellen", Glyph.Sources),
        ["Templates"] = ("Textvorlagen", Glyph.Templates),
        ["Glossary"] = ("Glossar", Glyph.Glossary),
        ["Journal"] = ("Aktivitäten", Glyph.History)
    };

    private static readonly Dictionary<string, string> Pages =
        PageDefinitions.ToDictionary(page => page.Key, page => page.Value.Title);

    private static readonly (string Title, string[] Pages)[] NavigationGroups =
    [
        ("Arbeitsbereich", ["Dashboard", "Documents"]),
        ("Governance und Risiken", ["Requirement", "Control", "Risk", "RiskMatrix", "Measure", "Mappings"]),
        ("Prüfung und Nachweise", ["Evidence", "Audit", "Finding"]),
        ("Register", ["Supplier", "AiSystem", "OssComponent"]),
        ("Wissen und Verlauf", ["Sources", "Templates", "Glossary", "Journal"])
    ];

    private readonly Dictionary<string, Button> _navigation = new();
    private Control[] _projectActions = [];
    private Control[] _projectOpeningActions = [];

    private void BuildShell()
    {
        _body = new ContentControl();
        _status = Ui.Secondary("Angemeldet", 12);
        AutomationProperties.SetLiveSetting(_status, AutomationLiveSetting.Polite);
        _context = Ui.Text("", 14, true);
        _context.VerticalAlignment = VerticalAlignment.Center;
        _context.MaxLines = 2;
        _context.TextTrimming = TextTrimming.CharacterEllipsis;
        _topic = Ui.Combo();
        _scope = Ui.Input("Exakter Text");
        _search = Ui.Input("Titel oder Code");
        ResetProjectContext();

        _shell = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("224,*"),
            RowDefinitions = new RowDefinitions("Auto,*,Auto")
        };
        AddShellHeader();
        AddShellNavigation();

        _projectOpeningActions =
        [
            Button("Neues Projekt", NewProject, true,
                "Legt eine verschlüsselte Projektdatei mit einem eigenen Passwort an.", Glyph.Plus),
            Button("Öffnen", OpenProject, help: "Öffnet eine verschlüsselte .d3grc-Datei. Bei vorhandenem Wiederherstellungsstand erscheint eine Auswahl.",
                glyph: Glyph.Open)
        ];
        var filters = BuildFilters();
        _projectActions =
        [
            Button("Speichern", Save, true,
                "Übernimmt das bearbeitete Formular oder speichert die Hauptdatei. Tastenkürzel Strg+S oder Cmd+S.", Glyph.Save),
            Button("Kopie speichern", SaveAs, help: "Erstellt eine verschlüsselte Projektkopie mit demselben Passwort.", glyph: Glyph.Copy),
            Button("Schließen", CloseProject, help: "Speichert und schließt das Projekt. Das lokale Profil bleibt angemeldet.", glyph: Glyph.Close),
            Button("Unternehmen konfigurieren", OpenCompanyConfigurationAsync,
                help: "Bearbeitet die Stammdaten und Themen des geöffneten Projekts.", glyph: Glyph.Settings),
            Button("Excel exportieren", () => Export(true),
                help: "Exportiert Thema, Geltungsbereich und Suche als unverschlüsselte Excel-Datei.", glyph: Glyph.Export),
            Button("Drucken / PDF", () => Export(false),
                help: "Erstellt eine unverschlüsselte HTML-Druckansicht zum Drucken oder Speichern als PDF im Browser.", glyph: Glyph.Print)
        ];
        var commands = Ui.Card(Ui.Stack(Ui.Row([.. _projectOpeningActions, .. _projectActions]), filters));
        commands.Padding = new Thickness(14, 12);
        _projectActions = [.. _projectActions, filters];
        var main = Ui.Fill(commands, _body);
        main.Margin = new Thickness(18, 14, 18, 12);
        Grid.SetRow(main, 1);
        Grid.SetColumn(main, 1);
        _shell.Children.Add(main);
        AddShellFooter();
        Content = _shell;
    }

    private Control BuildFilters()
    {
        var filterActions = Ui.Row(
            Button("Filter anwenden", ApplyFilters,
                help: "Aktualisiert die Ansicht mit dem eingegebenen Geltungsbereich und Suchtext.", glyph: Glyph.Filter),
            Button("Zurücksetzen", async () =>
            {
                if (!await CanLeaveFormAsync())
                    return;
                _scope.Text = _search.Text = _scopeValue = _searchValue = _statusFilter = "";
                await Render();
            }, help: "Leert Suche, Geltungsbereich und Listenstatus. Das gewählte Thema bleibt aktiv.", glyph: Glyph.Reset));
        filterActions.VerticalAlignment = VerticalAlignment.Bottom;
        var filters = new Grid { ColumnDefinitions = new ColumnDefinitions("1.1*,*,1.25*,Auto"), ColumnSpacing = 9 };
        var fields = new Control[]
        {
            Ui.Field("Thema", _topic, "Die Auswahl wirkt sofort. Zum Speichern eines Eintrags ein konkretes Thema auswählen."),
            Ui.Field("Geltungsbereich", _scope, "Filtert nach dem vollständigen Scope-Text. Ein leeres Feld zeigt alle Geltungsbereiche."),
            Ui.Field("Suche", _search, "Findet Teiltexte in Titel, Code, Beschreibung oder Verantwortung der Einträge."),
            filterActions
        };
        for (var i = 0; i < fields.Length; i++)
        {
            Grid.SetColumn(fields[i], i);
            filters.Children.Add(fields[i]);
        }

        _topic.SelectionChanged += async (_, _) =>
        {
            if (_loading || _busy)
                return;
            var next = (_topic.SelectedItem as Choice)?.Id ?? "";
            await Run(async () =>
            {
                _topic.SelectedItem = _topics.FirstOrDefault(topic => topic.Id == _topicId);
                if (next == _topicId || !await CanLeaveFormAsync())
                    return;
                _topicId = next;
                _statusFilter = "";
                await Render();
            });
        };
        return filters;
    }

    private static Image CreateLogo(double height) => new()
    {
        Source = new Bitmap(Path.Combine(AppContext.BaseDirectory, "content", "logo.png")),
        Height = height,
        Stretch = Stretch.Uniform
    };

    private void AddShellHeader()
    {
        var profile = Ui.IconLabel(Glyph.Person, _identity.User ?? "", Ui.Muted);
        var profileName = (TextBlock)profile.Children[1];
        profileName.MaxWidth = 180;
        profileName.TextWrapping = TextWrapping.NoWrap;
        profileName.TextTrimming = TextTrimming.CharacterEllipsis;
        var profileBadge = new Border
        {
            Background = Ui.SoftBlue,
            CornerRadius = new CornerRadius(16),
            Padding = new Thickness(10, 7),
            VerticalAlignment = VerticalAlignment.Center,
            Child = profile
        };
        ToolTip.SetTip(profileBadge, _identity.User);
        var user = Ui.Row(profileBadge, Button("Abmelden", Logout,
            help: "Speichert das Projekt und kehrt zur Anmeldung zurück.", glyph: Glyph.Logout));
        user.VerticalAlignment = VerticalAlignment.Center;
        var headerContent = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto"), ColumnSpacing = 16 };
        headerContent.Children.Add(_context);
        Grid.SetColumn(user, 1);
        headerContent.Children.Add(user);
        var header = new Border
        {
            Background = Ui.Surface,
            BorderBrush = Ui.Line,
            BorderThickness = new Thickness(0, 0, 0, 1),
            Padding = new Thickness(20, 10, 18, 10),
            Child = headerContent
        };
        Grid.SetColumn(header, 1);
        _shell!.Children.Add(header);
    }

    private void AddShellNavigation()
    {
        _navigation.Clear();
        var nav = new StackPanel { Spacing = 2 };
        foreach (var group in NavigationGroups)
        {
            var heading = Ui.Text(group.Title, 11, true);
            heading.Foreground = Ui.NavMuted;
            heading.Margin = new Thickness(9, 9, 0, 4);
            nav.Children.Add(heading);
            foreach (var page in group.Pages)
            {
                var (title, glyph) = PageDefinitions[page];
                var item = Button(title, async () =>
                {
                    if (!await CanLeaveFormAsync())
                        return;
                    _page = page;
                    _statusFilter = "";
                    await Render();
                }, glyph: glyph);
                StyleNavButton(item);
                _navigation.Add(page, item);
                nav.Children.Add(item);
            }
        }

        var cleanup = Button("Arbeitskopien bereinigen", async () =>
        {
            if (await Ui.Confirm(this, "Verwaiste Klartext-Arbeitskopien endgültig löschen? Vorher Wiederherstellungsstände prüfen.", "Bereinigen"))
                _status.Text = Workspace.CleanupOrphans() + " Arbeitskopien entfernt.";
        }, help: "Entfernt unverschlüsselte Kopien aus beendeten Sitzungen.", glyph: Glyph.Cleanup);
        StyleNavButton(cleanup);

        var name = Ui.Heading("D3 GRC", 21, 2);
        name.Foreground = Brushes.White;
        name.VerticalAlignment = VerticalAlignment.Center;
        var brand = Ui.Row(Ui.IconBadge(Glyph.Shield, Ui.NavySelected, Ui.Mint, 37), name);
        brand.Margin = new Thickness(8, 2, 0, 0);
        var sidebar = new Border
        {
            Background = Ui.Navy,
            Padding = new Thickness(10, 11),
            Child = Ui.Fill(brand, Ui.Scroll(nav), cleanup)
        };
        Grid.SetRowSpan(sidebar, 2);
        _shell!.Children.Add(sidebar);
    }

    private static void StyleNavButton(Button button)
    {
        button.Classes.Remove("grc-action");
        button.Classes.Add("grc-nav");
        button.HorizontalAlignment = HorizontalAlignment.Stretch;
        button.HorizontalContentAlignment = HorizontalAlignment.Left;
        button.MinHeight = 32;
        button.Padding = new Thickness(9, 5);
        TintNavButton(button, Ui.NavText);
        button.FocusAdorner = new FuncTemplate<Control>(() => new Border
        {
            BorderBrush = Ui.Mint,
            BorderThickness = new Thickness(2),
            CornerRadius = new CornerRadius(7)
        });
    }

    private static void TintNavButton(Button button, IBrush color)
    {
        if (button.Content is StackPanel { Children: [PathIcon icon, TextBlock label] })
        {
            icon.Foreground = color;
            label.Foreground = color;
        }
    }

    private void AddShellFooter()
    {
        var help = Ui.Secondary("F1 · Hinweise", 12);
        help.Margin = new Thickness(12, 0, 0, 0);
        var info = Icons.Draw(Glyph.Info, 14, Ui.Muted);
        info.Margin = new Thickness(0, 0, 7, 0);
        DockPanel.SetDock(help, Dock.Right);
        DockPanel.SetDock(info, Dock.Left);
        var footer = new Border
        {
            Background = Ui.Surface,
            BorderBrush = Ui.Line,
            BorderThickness = new Thickness(0, 1, 0, 0),
            Padding = new Thickness(16, 7),
            Child = new DockPanel { Children = { help, info, _status } }
        };
        Grid.SetRow(footer, 2);
        Grid.SetColumnSpan(footer, 2);
        _shell!.Children.Add(footer);
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
            item.Classes.Set("grc-current", active);
            TintNavButton(item, active ? Ui.Mint : _workspace.IsOpen ? Ui.NavText : Ui.NavMuted);
        }
    }
}
