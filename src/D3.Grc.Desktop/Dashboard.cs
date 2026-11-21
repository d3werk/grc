using System.Globalization;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using D3.Grc.Core;

namespace D3.Grc.Desktop;

public sealed partial class MainWindow
{
    private async Task<Control> Dashboard()
    {
        var now = DateTimeOffset.UtcNow;
        var today = DateOnly.FromDateTime(now.ToLocalTime().DateTime);
        var controls = _entries.Where(entry => entry.Kind == "Control").ToArray();
        var requirements = _entries.Where(entry => entry.Kind == "Requirement").ToArray();
        var findings = _entries.Where(entry => entry.Kind == "Finding").ToArray();
        var measures = _entries.Where(entry => entry.Kind == "Measure").ToArray();
        var audits = _entries.Where(entry => entry.Kind == "Audit").ToArray();
        var suppliers = _entries.Where(entry => entry.Kind == "Supplier").ToArray();

        var controlIds = controls.Select(control => control.Id).ToHashSet();
        var evidence = await _workspace.ControlEvidenceTraceAsync(controlIds, now);
        var evidenceCount = evidence.SelectMany(group => group.Select(proof => proof.Id)).Distinct().Count();
        var controlsWithCurrentEvidence = evidence.Where(group => group.Any(proof => proof.Status == TraceEvidenceStatus.Current))
            .Select(group => group.Key).ToHashSet();
        var requirementIds = requirements.Select(requirement => requirement.Id).ToHashSet();
        var requirementMappings = _mappings.Where(mapping =>
            mapping.Type == "Anforderung → Control" && mapping.Approval == "Bestätigt" &&
            requirementIds.Contains(mapping.From) && controlIds.Contains(mapping.To)).ToArray();
        var mappedRequirements = requirementMappings.Select(mapping => mapping.From).Distinct().Count();
        var requirementsWithEvidence = requirementMappings
            .Where(mapping => controlsWithCurrentEvidence.Contains(mapping.To))
            .Select(mapping => mapping.From).Distinct().Count();

        var openFindingCount = findings.Count(finding => finding.Status != "Geschlossen");
        var missingEvidenceCount = controls.Length - controlsWithCurrentEvidence.Count;
        var overdueCount = measures.Count(measure =>
            measure.Status != "Erledigt" && DateOnly.TryParse(measure.DueDate, out var due) && due < today);
        var missingOwnerCount = _entries.Count(entry => string.IsNullOrWhiteSpace(entry.Owner));
        var onlineSources = _sources.Where(source => OfficialSourceUri(source) is not null).ToArray();
        var sourcesNeedingReview = onlineSources.Count(source =>
            source.State.Contains("prüfen") || source.State.Contains("fehlgeschlagen"));
        var sourcesNeverChecked = onlineSources.Count(source => source.LastSuccess == "");

        var profile = await _workspace.GetCompanyProfileAsync();
        var recommendations = profile is null ? [] : CompanyPlanning.Build(profile);

        async Task Drill(string page, string status = "", string? topicId = null)
        {
            if (topicId is not null)
                _topicId = topicId;
            _page = page;
            _statusFilter = status;
            await Render();
        }

        Button OpenRegister(string label, string page, string status = "") =>
            Ui.Help(Button(label, () => Drill(page, status), glyph: Glyph.Arrow),
                status.Length == 0
                    ? $"Öffnet {Pages[page]} mit dem aktuellen Thema, Geltungsbereich und Suchtext."
                    : $"Zeigt in {Pages[page]} die Datensätze mit Status {status}.",
                status.Length == 0 ? $"{Pages[page]} öffnen" : $"{Pages[page]} mit Status {status} öffnen");

        Control SectionHeading(string label, Glyph glyph)
        {
            var heading = Ui.Heading(label, 19);
            heading.VerticalAlignment = VerticalAlignment.Center;
            return Ui.Row(Ui.IconBadge(glyph, size: 31), heading);
        }

        Control Metric(string label, int count, string page, string description, IBrush? tint = null, IBrush? iconColor = null)
        {
            var number = Ui.Text(count.ToString(), 32, true);
            var heading = Ui.Heading(label, 15, 2);
            heading.VerticalAlignment = VerticalAlignment.Center;
            var tile = Ui.Stack(Ui.Row(Ui.IconBadge(PageDefinitions[page].Icon, tint, iconColor), heading),
                number, Ui.Secondary(description));
            tile.Spacing = 7;
            return Ui.Card(Ui.Fill(tile, new Border(), OpenRegister("Öffnen", page)));
        }

        Control Ratio(string label, int count, int total, string? description = null)
        {
            var countLabel = total == 0 ? "Keine Einträge" : $"{count} von {total}";
            var countText = Ui.Text(countLabel, 13, true);
            countText.HorizontalAlignment = HorizontalAlignment.Right;
            var header = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto"), ColumnSpacing = 12 };
            header.Children.Add(Ui.Text(label, 14, true));
            Grid.SetColumn(countText, 1);
            header.Children.Add(countText);
            var bar = new ProgressBar
            {
                Minimum = 0,
                Maximum = Math.Max(total, 1),
                Value = count,
                Height = 8,
                Foreground = Ui.Green,
                Background = Ui.SoftGreen
            };
            AutomationProperties.SetName(bar, $"{label}, {countLabel}");
            var content = Ui.Stack(header);
            content.Spacing = 6;
            if (total > 0)
                content.Children.Add(bar);
            if (description is not null)
                content.Children.Add(Ui.Secondary(description));
            return content;
        }

        Control Distribution(string title, string kind, IReadOnlyCollection<Entry> entries)
        {
            if (entries.Count == 0)
                return Ui.Card(Ui.Stack(SectionHeading(title, PageDefinitions[kind].Icon), Ui.Secondary("Keine Datensätze im aktuellen Filter."),
                    OpenRegister("Register öffnen", kind)));

            var rows = Ui.Stack(SectionHeading(title, PageDefinitions[kind].Icon),
                Ui.Secondary($"{entries.Count} Datensätze im aktuellen Filter."));
            foreach (var status in Entry.Statuses(kind))
            {
                var count = entries.Count(entry => entry.Status == status);
                var open = OpenRegister("Ansehen", kind, status);
                open.IsEnabled = count > 0;
                var row = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto"), ColumnSpacing = 12 };
                row.Children.Add(Ratio(status, count, entries.Count));
                Grid.SetColumn(open, 1);
                open.VerticalAlignment = VerticalAlignment.Center;
                row.Children.Add(open);
                rows.Children.Add(row);
            }
            return Ui.Card(rows);
        }

        Control NextSteps()
        {
            var configure = Ui.Help(Button(profile is null ? "Firmenprofil einrichten" : "Firmenprofil und Module",
                    OpenCompanyConfigurationAsync, profile is null, glyph: Glyph.Settings),
                "Öffnet Unternehmensmerkmale und die dazu abgeleiteten Module.");

            if (profile is null)
            {
                var setup = Ui.Card(Ui.Stack(SectionHeading("Nächste Schritte", Glyph.Tasks),
                    Ui.Secondary("Hinterlegen Sie Branche, Größe und besondere Merkmale für passende Modulvorschläge."),
                    configure));
                setup.Background = Ui.SoftGreen;
                return setup;
            }

            var steps = Ui.Stack(SectionHeading("Nächste Schritte", Glyph.Tasks),
                Ui.Secondary("Vorschläge aus dem gespeicherten Firmenprofil."));
            var stepCards = new List<Control>();
            var keyRecommendations = recommendations
                .Where(recommendation => recommendation.Priority == CompanyModulePriority.Key).Take(3).ToArray();
            foreach (var recommendation in keyRecommendations)
            {
                var topic = _topics.FirstOrDefault(choice => choice.Id == recommendation.TopicId);
                var action = Ui.Help(Button("Im Register bearbeiten",
                        () => Drill(recommendation.TargetPage, topicId: topic?.Id), glyph: Glyph.Arrow),
                    $"Öffnet {Pages[recommendation.TargetPage]} im Thema {topic?.Title ?? "der aktuellen Auswahl"}. " +
                    "Geltungsbereich und Suchtext bleiben erhalten.",
                    $"{recommendation.Title} im Register bearbeiten");
                var step = Ui.Card(Ui.Stack(Ui.Heading(recommendation.Title, 16, 3),
                    Ui.Secondary(recommendation.Why), Ui.Text(recommendation.NextStep), action));
                step.Background = Ui.Canvas;
                stepCards.Add(step);
            }
            if (stepCards.Count > 0)
                steps.Children.Add(Ui.AdaptiveCards(3, 240, stepCards.ToArray()));
            steps.Children.Add(configure);
            return Ui.Card(steps);
        }

        async Task<Control> SupplierReviews()
        {
            var active = suppliers.Where(supplier => supplier.Status != "Beendet").ToArray();
            var details = await _workspace.SupplierDetailsAsync(active.Select(supplier => supplier.Id).ToArray());
            var critical = details.Values.Count(supplier => supplier.Criticality == SupplierCriticality.Critical);
            var high = details.Values.Count(supplier => supplier.Criticality == SupplierCriticality.High);
            var deadlines = active.Select(supplier =>
                DateOnly.TryParseExact(supplier.DueDate, "yyyy-MM-dd", CultureInfo.InvariantCulture,
                    DateTimeStyles.None, out var date) ? (DateOnly?)date : null).ToArray();
            var overdue = deadlines.Count(date => date is { } due && due < today);
            var dueToday = deadlines.Count(date => date == today);
            var unscheduled = deadlines.Count(date => date is null);

            Control Count(string label, int value, string? description = null)
            {
                var count = Ui.Stack(Ui.Text(value.ToString(), 24, true), Ui.Text(label, 14, true));
                count.Spacing = 4;
                if (description is not null)
                    count.Children.Add(Ui.Secondary(description));
                return count;
            }

            return Ui.Card(Ui.Stack(
                SectionHeading("Lieferantenprüfungen", Glyph.Supplier),
                Ui.Secondary($"Prüfbestand im aktuellen Filter · {active.Length} von {suppliers.Length} Einträgen"),
                Ui.AdaptiveCards(3, 150,
                    Count("Kritisch eingestuft", critical, $"{high} hoch eingestuft"),
                    Count("Fällige Prüfungen", overdue + dueToday, $"{dueToday} heute fällig · {overdue} überfällig"),
                    Count("Ohne Prüftermin", unscheduled)),
                OpenRegister("Lieferantenregister öffnen", "Supplier")));
        }

        var metrics = Ui.AdaptiveCards(4, 170,
            Metric("Anforderungen", requirements.Length, "Requirement", "Einträge für die Anwendbarkeitsprüfung", Ui.SoftBlue, Ui.Blue),
            Metric("Controls", controls.Length, "Control", "Kontrollen im gewählten Kontext"),
            Metric("Offene Findings", openFindingCount, "Finding", "Feststellungen vor dem Abschluss", Ui.SoftAmber, Ui.Amber),
            Metric("Nachweise", evidenceCount, "Evidence", "Dateien aus zugeordneten Controls", Ui.SoftBlue, Ui.Blue));

        var coverage = Ui.Card(Ui.Stack(
            SectionHeading("Anforderungen, Controls und Nachweise", Glyph.Mapping),
            Ui.Secondary("Gezählt werden bestätigte Verknüpfungen zwischen Datensätzen im aktuellen Filter."),
            Ratio("Anforderungen mit verknüpftem Control", mappedRequirements, requirements.Length),
            Ratio("Verknüpfte Anforderungen mit Nachweis", requirementsWithEvidence, mappedRequirements,
                "Mindestens ein zugeordnetes Control hat einen zeitlich gültigen Nachweis."),
            Ui.Row(OpenRegister("Mappings öffnen", "Mappings"), OpenRegister("Nachweise öffnen", "Evidence"))));

        Control WorkItem(string label, int count)
        {
            var number = Ui.Text(count.ToString(), 13, true);
            number.Foreground = count == 0 ? Ui.Muted : Ui.Amber;
            number.HorizontalAlignment = HorizontalAlignment.Center;
            var badge = new Border
            {
                MinWidth = 34,
                Background = count == 0 ? Ui.Canvas : Ui.SoftAmber,
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(7, 4),
                Child = number
            };
            var caption = Ui.Text(label);
            caption.VerticalAlignment = VerticalAlignment.Center;
            return Ui.Row(badge, caption);
        }
        var attention = Ui.Card(Ui.Stack(SectionHeading("Offene Arbeit", Glyph.Tasks),
            WorkItem("Controls ohne zeitlich gültigen Nachweis", missingEvidenceCount),
            WorkItem("Datensätze ohne Verantwortung", missingOwnerCount),
            WorkItem("Überfällige Maßnahmen", overdueCount),
            Ui.Row(OpenRegister("Nachweise öffnen", "Evidence"), OpenRegister("Maßnahmen öffnen", "Measure"))));

        var heroTitle = Ui.Heading("Projektüberblick", 31, 1);
        heroTitle.Foreground = Brushes.White;
        var heroDate = Ui.Secondary($"Aktueller Filterbestand am {today:dd.MM.yyyy}", 14);
        heroDate.Foreground = Ui.NavText;
        var heroText = Ui.Stack(heroTitle, heroDate);
        heroText.Spacing = 4;
        var docs = Ui.Help(Button("Dokumente öffnen", () => Drill("Documents"), glyph: Glyph.Documents),
            "Öffnet das Dokumentenregister mit Vorschau und Versionsverlauf im aktuellen Projekt.");
        docs.VerticalAlignment = VerticalAlignment.Center;
        var heroContent = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto"), ColumnSpacing = 16 };
        heroContent.Children.Add(heroText);
        Grid.SetColumn(docs, 1);
        heroContent.Children.Add(docs);
        var hero = new Border
        {
            Background = new LinearGradientBrush
            {
                StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
                EndPoint = new RelativePoint(1, 0.75, RelativeUnit.Relative),
                GradientStops =
                {
                    new GradientStop(Color.Parse("#132C45"), 0),
                    new GradientStop(Color.Parse("#184758"), 0.65),
                    new GradientStop(Color.Parse("#136562"), 1)
                }
            },
            CornerRadius = new CornerRadius(12),
            Padding = new Thickness(23, 19),
            Child = heroContent
        };
        var overviewContent = Ui.Stack(hero, metrics, Ui.AdaptiveCards(2, 342, attention, coverage));
        if (suppliers.Length > 0)
            overviewContent.Children.Add(await SupplierReviews());
        overviewContent.Children.Add(NextSteps());
        var overview = Ui.Scroll(overviewContent);

        var implementation = Ui.Scroll(Ui.AdaptiveCards(2, 340,
            Distribution("Anwendbarkeit der Anforderungen", "Requirement", requirements),
            Distribution("Umsetzungsstand der Controls", "Control", controls)));

        var proofCoverage = Ui.Card(Ui.Stack(SectionHeading("Nachweisabdeckung", Glyph.Evidence),
            Ratio("Controls mit zeitlich gültigem Nachweis", controlsWithCurrentEvidence.Count, controls.Length,
                "Für die Verwendung in einer Prüfung wird zusätzlich die Dateiintegrität geprüft."),
            Ui.Text($"{missingEvidenceCount} Controls ohne zeitlich gültigen Nachweis"),
            OpenRegister("Nachweise öffnen", "Evidence")));
        var auditsAndEvidence = Ui.Scroll(Ui.Stack(proofCoverage, Ui.AdaptiveCards(2, 340,
            Distribution("Audits nach Status", "Audit", audits),
            Distribution("Findings nach Status", "Finding", findings))));

        var sourceQuality = Ui.Scroll(Ui.Stack(Ui.Card(Ui.Stack(SectionHeading("Quellen im gewählten Thema", Glyph.Sources),
            Ui.Text($"{_sources.Count} Quellen im Kontext"),
            Ui.Text($"{sourcesNeedingReview} online prüfbare Quellen mit Prüfhinweis"),
            Ui.Text($"{sourcesNeverChecked} online prüfbare Quellen ohne erfolgreichen technischen Abruf"),
            Ui.Secondary("Der Abruf prüft den technischen Stand. Für die fachliche Einordnung ist eine begründete Prüfung erforderlich."),
            OpenRegister("Quellen öffnen", "Sources")))));

        return new TabControl
        {
            ItemsSource = new[]
            {
                Ui.Tab("Übersicht", overview, "Kennzahlen, offene Arbeit und konkrete nächste Schritte im Projekt.", Glyph.Dashboard),
                Ui.Tab("Umsetzung", implementation, "Status von Anforderungen und Controls mit direkter Navigation in die Register.", Glyph.Tasks),
                Ui.Tab("Nachweise & Audits", auditsAndEvidence, "Nachweisabdeckung sowie Status von Audits und Findings.", Glyph.Audit),
                Ui.Tab("Quellen & Qualität", sourceQuality, "Quellen und technischer Prüfbedarf im gewählten Thema.", Glyph.Sources)
            }
        };
    }
}
