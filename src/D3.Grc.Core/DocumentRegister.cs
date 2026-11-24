using System.Collections.ObjectModel;
using System.Globalization;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using static D3.Grc.Core.Database;

namespace D3.Grc.Core;

public enum DocumentCategory { Policy = 0, Procedure = 1, WorkInstruction = 2, Standard = 3, Guidance = 4, Other = 5 }

public enum DocumentStatus { Draft = 0, InReview = 1, Reviewed = 2, Published = 3, Retired = 4 }

public enum DocumentLinkTargetKind { Requirement = 0, Control = 1 }

public sealed record DocumentDraft(string Title, string TopicId, string Scope, DocumentCategory Category, string Owner, string Content);

public sealed record DocumentStarter(string Title, string Content);

public sealed record DocumentRecord(string Id, DocumentDraft Draft, DocumentStatus Status, int Version, int Revision,
    int? PublishedVersion, string UpdatedUtc, string ReviewedBy, string ReviewedUtc);

public sealed record DocumentVersion(int Version, DocumentDraft Draft, string ReviewedBy, string ReviewedUtc, string ReviewNote,
    string PublishedBy, string PublishedUtc, string PublicationNote);

public sealed record DocumentLink(string Id, DocumentLinkTargetKind Kind, string TargetId, string Title, string Reason);

public static class DocumentRegister
{
    public static IReadOnlyDictionary<DocumentCategory, string> CategoryLabels { get; } =
        new ReadOnlyDictionary<DocumentCategory, string>(new Dictionary<DocumentCategory, string>
        {
            [DocumentCategory.Policy] = "Richtlinie",
            [DocumentCategory.Procedure] = "Verfahrensbeschreibung",
            [DocumentCategory.WorkInstruction] = "Arbeitsanweisung",
            [DocumentCategory.Standard] = "Standard",
            [DocumentCategory.Guidance] = "Leitfaden",
            [DocumentCategory.Other] = "Sonstiges"
        });

    public static IReadOnlyDictionary<DocumentStatus, string> StatusLabels { get; } =
        new ReadOnlyDictionary<DocumentStatus, string>(new Dictionary<DocumentStatus, string>
        {
            [DocumentStatus.Draft] = "Entwurf",
            [DocumentStatus.InReview] = "In Prüfung",
            [DocumentStatus.Reviewed] = "Geprüft",
            [DocumentStatus.Published] = "Veröffentlicht",
            [DocumentStatus.Retired] = "Außer Kraft"
        });

    public static IReadOnlyDictionary<DocumentLinkTargetKind, string> LinkTargetLabels { get; } =
        new ReadOnlyDictionary<DocumentLinkTargetKind, string>(new Dictionary<DocumentLinkTargetKind, string>
        {
            [DocumentLinkTargetKind.Requirement] = "Anforderung",
            [DocumentLinkTargetKind.Control] = "Control"
        });

    public static DocumentStarter Starter(string topicId, DocumentCategory category)
    {
        var title = $"{CategoryLabels[category]} · {CompanyPlanning.TopicLabels.GetValueOrDefault(topicId, "Organisation und Prüfung")}";
        var (objective, preparation, work, evidence) = topicId switch
        {
            "NIS2" => (
                "Relevante Dienste und Abhängigkeiten erfassen, Risiken priorisieren und die Vorbereitung auf erhebliche Vorfälle organisieren.",
                "Geltungsbereich, betroffene Organisationseinheiten und kritische Leistungen mit der Fachleitung abstimmen. Die Entscheidung zur Betroffenheit im Anforderungsregister belegen.",
                "Die Fachverantwortung gleicht Risiken und Controls mit den freigegebenen Schutzmaßnahmen ab. Der Vorfallkoordinator prüft Meldewege, Kontaktdaten und Stellvertretung in einer Übung.",
                "Betroffenheitsprüfung, Diensteliste, Bewertung offener Risiken und Ergebnis der Eskalationsübung ablegen."),
            "ISMS" => (
                "Schutzbedarf und Risiken für Informationswerte bewerten. Für wesentliche Risiken angemessene Controls auswählen und ihre Wirksamkeit regelmäßig prüfen.",
                "Informationswerte, zugehörige Prozesse und verantwortliche Rollen aus dem Register übernehmen. Den relevanten Bewertungs- und Prüfzeitraum festlegen.",
                "Risiken bewerten, die Behandlungsentscheidung begründen und jedem ausgewählten Control eine verantwortliche Rolle und einen Prüftermin zuweisen. Abweichungen mit Maßnahmen nachverfolgen.",
                "Risikobewertung, geführte Kontrollprüfung und ausgewählte Nachweise mit Datum und verantwortlicher Rolle verknüpfen."),
            "PRIVACY" => (
                "Verarbeitungstätigkeiten transparent führen und vereinbarte Datenschutzmaßnahmen bei Änderungen frühzeitig überprüfen.",
                "Verarbeitungszweck, betroffene Personengruppen, Datenkategorien, Empfänger und Löschregeln mit dem Fachbereich abstimmen.",
                "Verarbeitungstätigkeit aktualisieren und die zugehörigen organisatorischen sowie technischen Maßnahmen zuordnen. Prüfen, ob eine Datenschutz-Folgenabschätzung angezeigt ist, und die Entscheidung dokumentieren.",
                "Aktualisierten Registerstand, dokumentierte Prüfentscheidung und Freigabe der fachverantwortlichen Stelle hinterlegen."),
            "AI" => (
                "KI-Anwendungen mit dokumentiertem Zweck, geprüften Einsatzgrenzen und eindeutig zugewiesener Aufsicht betreiben.",
                "KI-System, Anbieter, vorgesehenen Einsatz, Datenkategorien und menschliche Aufsicht im Inventar erfassen.",
                "Risiken, Schutzmaßnahmen und Prüfkriterien vor Einführung bewerten. Fachliche Freigabe und Auflagen protokollieren. Änderungen an Modell, Zweck oder Eingabedaten erneut beurteilen.",
                "Bewertung, Testergebnisse, Freigabeauflagen und Termin der nächsten Überprüfung mit dem KI-System verknüpfen."),
            "BCM" => (
                "Kritische Leistungen nach einer Störung innerhalb abgestimmter Ziele geordnet wiederherstellen.",
                "Wesentliche Prozesse und ihre Abhängigkeiten erfassen. Ausfallszenarien, Auswirkungen und die benötigten Betriebsressourcen beurteilen.",
                "Wiederanlaufziele und Reihenfolge festlegen. Alarmierung, Notbetrieb und Rückkehr zum Regelbetrieb im Wiederanlaufplan dokumentieren und in einer Übung erproben.",
                "BIA, aktuelle Planfassung, Übungsprotokoll und Verbesserungsmaßnahmen im Projekt hinterlegen."),
            "DORA" => (
                "IKT-Abhängigkeiten kritischer Leistungen erkennen und Wiederanlauf, Vorfallbehandlung sowie Drittparteienrisiken nachvollziehbar steuern.",
                "Betroffene Geschäftsfunktionen, IKT-Dienste und relevante Dienstleister mit ihren jeweils zuständigen Rollen erfassen.",
                "Abhängigkeiten und Risiken bewerten, den vorgesehenen Wiederanlauf erproben und die vereinbarten Reaktionswege mit den beteiligten Teams abstimmen.",
                "Bewertung, Bezüge zu Dienstleistern und Verträgen sowie Ergebnisse von Tests und Maßnahmen dokumentieren."),
            "CLOUD" => (
                "Cloud-Dienste mit klarer Aufgabenverteilung zwischen Anbieter und eigener Organisation sicher betreiben.",
                "Dienste, verwaltete Daten, Betriebsregionen und die für den Dienst geltenden internen Anforderungen erfassen.",
                "Verantwortlichkeit für Konfiguration, Identitäten, Protokollierung, Datensicherung und Wiederherstellung zuweisen. Abweichungen gegenüber den Vorgaben erfassen und nach Frist bearbeiten.",
                "Aktuellen Konfigurationsstand, Prüfung privilegierter Rollen sowie Ergebnis der Wiederherstellungsprobe belegen."),
            "OSS" => (
                "Open-Source-Komponenten, ihre Herkunft und die für ihren Einsatz geltenden Verpflichtungen nachvollziehbar verwalten.",
                "Direkte und transitive Komponenten mit Version, Bezugsquelle und Lizenz erfassen. Auslieferung und internen Einsatz unterscheiden.",
                "Lizenzbedingungen für die konkrete Nutzung prüfen, Verpflichtungen der Auslieferung zuweisen und bekannte Sicherheitsbefunde priorisieren. Aktualisierung und Austausch verfolgen.",
                "Komponentenliste oder SBOM, fachliche Lizenzprüfung und Maßnahmen für offene Befunde ablegen."),
            "PROCESS" => (
                "Abläufe mit einem eindeutigen Auslöser, Zuständigkeiten, Arbeitsschritten und einem prüfbaren Ergebnis beschreiben.",
                "Zweck, Eingaben, Übergabestellen und Abhängigkeiten mit den beteiligten Rollen abstimmen.",
                "Den Ablauf vom Eingang bis zum Abschluss dokumentieren. Rollen, Entscheidungspunkte und Abweichungen je Schritt festhalten. Änderungen mit einem typischen Praxisfall erproben.",
                "Freigegebene Prozessfassung, vereinbarte Übergaben und Ergebnisse der nächsten Überprüfung dokumentieren."),
            _ => (
                "Aufgaben, Verantwortlichkeiten und Prüfkriterien für den betrachteten Geltungsbereich verbindlich beschreiben.",
                "Betroffene Einheiten, beteiligte Rollen und vorhandene Anforderungen im Projekt ermitteln.",
                "Arbeitsschritte mit Eingaben, erwarteten Ergebnissen und Entscheidungsstellen vereinbaren. Abweichungen dokumentieren und einer verantwortlichen Rolle zuweisen.",
                "Relevante Nachweise, offene Maßnahmen und den nächsten Prüftermin mit dem Dokument verknüpfen.")
        };
        var content = $"""
            # Zweck und Geltungsbereich

            {objective} Der konkrete Bereich und die verantwortliche Stelle ergeben sich aus den Stammdaten dieses Dokuments.

            ## Vorbereitung und Zuständigkeit

            {preparation} Die Dokumentverantwortung koordiniert die Bearbeitung und vereinbart die fachliche Prüfung mit den beteiligten Rollen.

            ## Durchführung

            {work}

            ## Nachweise und Pflege

            {evidence}

            - Ergebnisse und offene Punkte vor der Freigabe prüfen.
            - Änderungen am Geltungsbereich oder Verfahren nachvollziehbar einarbeiten.
            - Den nächsten Überprüfungstermin mit der zuständigen Stelle vereinbaren.
            """;
        return new DocumentStarter(title, content);
    }

    internal static DocumentDraft ValidateDraft(DocumentDraft draft)
    {
        ArgumentNullException.ThrowIfNull(draft);
        var title = draft.Title?.Trim() ?? "";
        var topic = draft.TopicId?.Trim() ?? "";
        var scope = draft.Scope?.Trim() ?? "";
        var owner = draft.Owner?.Trim() ?? "";
        var content = draft.Content ?? "";
        if (title.Length is < 1 or > 500)
            throw new ArgumentException("Dokumenttitel mit 1 bis 500 Zeichen angeben.", nameof(draft));
        if (topic.Length == 0 || !Enum.IsDefined(draft.Category))
            throw new ArgumentException("Fachthema und Dokumentkategorie auswählen.", nameof(draft));
        if (owner.Length is < 1 or > 240)
            throw new ArgumentException("Verantwortliche Rolle oder Person mit 1 bis 240 Zeichen angeben.", nameof(draft));
        if (scope.Length > 1000 || RichTextContent.Plain(content).Length > 100000)
            throw new ArgumentException("Geltungsbereich auf 1.000 und Dokumenttext auf 100.000 Zeichen begrenzen.", nameof(draft));

        return draft with { Title = title, TopicId = topic, Scope = scope, Owner = owner, Content = content };
    }
}

public sealed partial class Workspace
{
    private const string SelectDocument = """
        SELECT Id, Title, TopicId, Scope, Category, Owner, Content, Status, Version, Revision,
               PublishedVersion, UpdatedUtc, ReviewedBy, ReviewedUtc FROM Documents
        """;

    public Task<List<DocumentRecord>> DocumentsAsync(string topic = "", string scope = "", string search = "") =>
        Read(connection => ReadDocuments(connection, topic, scope, search));

    public Task<DocumentRecord> DocumentAsync(string id) => Read(connection => GetDocument(connection, null, id));

    public Task<List<Choice>> TemplateTopicChoicesAsync(string templateId) => Read(connection =>
        Query(connection, null, """
            SELECT topic.Id, topic.Title FROM Topics topic
            WHERE EXISTS (SELECT 1 FROM DesktopTemplateTopics mapping WHERE mapping.TemplateId=$p0 AND mapping.TopicId=topic.Id)
                OR (NOT EXISTS (SELECT 1 FROM DesktopTemplateTopics WHERE TemplateId=$p0) AND EXISTS (
                    SELECT 1 FROM Templates template JOIN TopicSources source ON source.SourceId=template.SourceId
                    WHERE template.Id=$p0 AND source.TopicId=topic.Id
                ))
            ORDER BY topic.Title, topic.Id
            """, reader => new Choice(reader.GetString(0), reader.GetString(1)), templateId));

    public Task<List<DocumentVersion>> DocumentVersionsAsync(string id) => Read(connection =>
    {
        _ = GetDocument(connection, null, id);
        return Query(connection, null, """
            SELECT Version, Title, TopicId, Scope, Category, Owner, Content,
                   ReviewedBy, ReviewedUtc, ReviewNote, PublishedBy, PublishedUtc, PublicationNote
            FROM PublishedDocumentVersions WHERE DocumentId=$p0 ORDER BY Version DESC
            """, reader => new DocumentVersion(reader.GetInt32(0), ReadDocumentDraft(reader), reader.GetString(7),
                reader.GetString(8), reader.GetString(9), reader.GetString(10), reader.GetString(11), reader.GetString(12)), id);
    });

    public Task<DocumentRecord> CreateDocumentAsync(DocumentDraft draft, string reason, string? id = null)
    {
        draft = DocumentRegister.ValidateDraft(draft);
        Reason(reason);
        id ??= Guid.NewGuid().ToString("N");
        if (string.IsNullOrWhiteSpace(id) || id.Length > 100)
            throw new ArgumentException("Dokumentkennung ungültig.", nameof(id));

        return Change("DocumentCreated", id, JsonSerializer.Serialize(new { reason, draft.Title }), (connection, transaction) =>
        {
            EnsureDocumentTopic(connection, transaction, draft.TopicId);
            if (Scalar(connection, transaction, "SELECT Id FROM Documents WHERE Id=$p0", id) != "")
                throw new ArgumentException("Die Dokumentkennung ist bereits vergeben.", nameof(id));

            Exec(connection, transaction, """
                INSERT INTO Documents (Id, Title, TopicId, Scope, Category, Owner, Content, Status, Version, Revision, UpdatedUtc)
                VALUES ($p0, $p1, $p2, $p3, $p4, $p5, $p6, $p7, 1, 1, $p8)
                """, id, draft.Title, draft.TopicId, draft.Scope, (int)draft.Category, draft.Owner, draft.Content,
                (int)DocumentStatus.Draft, DocumentUtc());

            return GetDocument(connection, transaction, id);
        });
    }

    public Task<DocumentRecord> SaveDocumentDraftAsync(string id, int expectedRevision, DocumentDraft draft, string reason)
    {
        draft = DocumentRegister.ValidateDraft(draft);
        return ChangeDocumentAsync(id, expectedRevision, reason, "DocumentDraftSaved", (connection, transaction, current, _) =>
        {
            RequireDocumentStatus(current, DocumentStatus.Draft);
            EnsureDocumentTopic(connection, transaction, draft.TopicId);
            Exec(connection, transaction, """
                UPDATE Documents SET Title=$p0, TopicId=$p1, Scope=$p2, Category=$p3, Owner=$p4, Content=$p5 WHERE Id=$p6
                """, draft.Title, draft.TopicId, draft.Scope, (int)draft.Category, draft.Owner, draft.Content, id);
        });
    }

    public Task<DocumentRecord> SubmitDocumentForReviewAsync(string id, int expectedRevision, string reason) =>
        ChangeDocumentAsync(id, expectedRevision, reason, "DocumentSubmitted", (connection, transaction, current, _) =>
        {
            RequireDocumentStatus(current, DocumentStatus.Draft);
            if (string.IsNullOrWhiteSpace(RichTextContent.Plain(current.Draft.Content)))
                throw new ArgumentException("Für die Prüfung den Dokumenttext ausarbeiten.");

            SetDocumentStatus(connection, transaction, id, DocumentStatus.InReview);
        });

    public Task<DocumentRecord> ApproveDocumentAsync(string id, int expectedRevision, string reason) =>
        ChangeDocumentAsync(id, expectedRevision, reason, "DocumentReviewed", (connection, transaction, current, utc) =>
        {
            RequireDocumentStatus(current, DocumentStatus.InReview);
            Exec(connection, transaction, "UPDATE Documents SET Status=$p0, ReviewedBy=$p1, ReviewedUtc=$p2, ReviewNote=$p3 WHERE Id=$p4",
                (int)DocumentStatus.Reviewed, identity.User, utc, reason, id);
        });

    public Task<DocumentRecord> ReturnDocumentToDraftAsync(string id, int expectedRevision, string reason) =>
        ChangeDocumentAsync(id, expectedRevision, reason, "DocumentReturned", (connection, transaction, current, _) =>
        {
            RequireDocumentStatus(current, DocumentStatus.InReview, DocumentStatus.Reviewed);
            Exec(connection, transaction, "UPDATE Documents SET Status=$p0, ReviewedBy='', ReviewedUtc='', ReviewNote='' WHERE Id=$p1",
                (int)DocumentStatus.Draft, id);
        });

    public Task<DocumentRecord> PublishDocumentAsync(string id, int expectedRevision, string reason) =>
        ChangeDocumentAsync(id, expectedRevision, reason, "DocumentPublished", (connection, transaction, current, utc) =>
        {
            RequireDocumentStatus(current, DocumentStatus.Reviewed);
            Exec(connection, transaction, """
                INSERT INTO PublishedDocumentVersions
                    (DocumentId, Version, Title, TopicId, Scope, Category, Owner, Content,
                     ReviewedBy, ReviewedUtc, ReviewNote, PublishedBy, PublishedUtc, PublicationNote)
                SELECT Id, Version, Title, TopicId, Scope, Category, Owner, Content,
                       ReviewedBy, ReviewedUtc, ReviewNote, $p0, $p1, $p2
                FROM Documents WHERE Id=$p3
                """, identity.User, utc, reason, id);
            Exec(connection, transaction, "UPDATE Documents SET Status=$p0, PublishedVersion=Version WHERE Id=$p1",
                (int)DocumentStatus.Published, id);
        });

    public Task<DocumentRecord> StartDocumentRevisionAsync(string id, int expectedRevision, string reason) =>
        ChangeDocumentAsync(id, expectedRevision, reason, "DocumentRevisionStarted", (connection, transaction, current, _) =>
        {
            RequireDocumentStatus(current, DocumentStatus.Published);
            Exec(connection, transaction, """
                UPDATE Documents SET Version=Version+1, Status=$p0, ReviewedBy='', ReviewedUtc='', ReviewNote='' WHERE Id=$p1
                """, (int)DocumentStatus.Draft, id);
        });

    public Task<DocumentRecord> RetireDocumentAsync(string id, int expectedRevision, string reason) =>
        ChangeDocumentAsync(id, expectedRevision, reason, "DocumentRetired", (connection, transaction, current, _) =>
        {
            RequireDocumentStatus(current, DocumentStatus.Published);
            SetDocumentStatus(connection, transaction, id, DocumentStatus.Retired);
        });

    public Task<List<DocumentLink>> DocumentLinksAsync(string documentId) => Read(connection =>
    {
        _ = GetDocument(connection, null, documentId);
        return ReadDocumentLinks(connection, documentId);
    });

    public Task LinkDocumentAsync(string documentId, DocumentLinkTargetKind kind, string targetId, string reason)
    {
        Reason(reason);
        if (!Enum.IsDefined(kind))
            throw new ArgumentException("Anforderung oder Control zum Verknüpfen auswählen.", nameof(kind));
        var linkId = Guid.NewGuid().ToString("N");
        return Change("DocumentLinkAdded", documentId, JsonSerializer.Serialize(new { linkId, kind, targetId, reason }),
            (connection, transaction) =>
            {
                EnsureDocumentLinksEditable(connection, transaction, documentId);
                var record = Get(connection, transaction, targetId);
                RequireKind(record, kind == DocumentLinkTargetKind.Requirement ? "Requirement" : "Control");
                if (Scalar(connection, transaction, "SELECT Id FROM DocumentLinks WHERE DocumentId=$p0 AND RecordId=$p1",
                        documentId, targetId) != "")
                    throw new ArgumentException("Der Datensatz ist bereits mit dem Dokument verknüpft.");

                Exec(connection, transaction, "INSERT INTO DocumentLinks (Id, DocumentId, Kind, RecordId, Reason) VALUES ($p0, $p1, $p2, $p3, $p4)",
                    linkId, documentId, (int)kind, targetId, reason);

                return true;
            });
    }

    public Task UnlinkDocumentAsync(string documentId, string linkId, string reason)
    {
        Reason(reason);
        return Change("DocumentLinkRemoved", documentId, JsonSerializer.Serialize(new { linkId, reason }), (connection, transaction) =>
        {
            EnsureDocumentLinksEditable(connection, transaction, documentId);
            if (Exec(connection, transaction, "DELETE FROM DocumentLinks WHERE DocumentId=$p0 AND Id=$p1", documentId, linkId) == 0)
                throw new ArgumentException("Verknüpfung im gewählten Dokument fehlt.");

            return true;
        });
    }

    private Task<DocumentRecord> ChangeDocumentAsync(string id, int expectedRevision, string reason, string action,
        Action<SqliteConnection, SqliteTransaction, DocumentRecord, string> change)
    {
        Reason(reason);
        return Change(action, id, reason, (connection, transaction) =>
        {
            var current = GetDocument(connection, transaction, id);
            if (current.Revision != expectedRevision)
                throw new ArgumentException("Das Dokument wurde geändert. Bitte den aktuellen Stand laden.");

            var utc = DocumentUtc();
            change(connection, transaction, current, utc);
            Exec(connection, transaction, "UPDATE Documents SET Revision=Revision+1, UpdatedUtc=$p0 WHERE Id=$p1", utc, id);
            return GetDocument(connection, transaction, id);
        });
    }

    private static List<DocumentRecord> ReadDocuments(SqliteConnection connection, string topic, string scope, string search)
    {
        var documents = Query(connection, null,
            SelectDocument + " WHERE ($p0='' OR TopicId=$p0) AND ($p1='' OR Scope=$p1) ORDER BY Title, Id", ReadDocument, topic, scope);
        if (search.Length == 0)
            return documents;

        return documents.Where(document => document.Draft.Title.Contains(search, StringComparison.CurrentCultureIgnoreCase) ||
            document.Draft.Owner.Contains(search, StringComparison.CurrentCultureIgnoreCase) ||
            RichTextContent.Plain(document.Draft.Content).Contains(search, StringComparison.CurrentCultureIgnoreCase)).ToList();
    }

    private static List<DocumentLink> ReadDocumentLinks(SqliteConnection connection, string documentId) =>
        Query(connection, null, """
            SELECT l.Id, l.Kind, l.RecordId, CASE WHEN r.Code='' THEN r.Title ELSE r.Code || ' · ' || r.Title END, l.Reason
            FROM DocumentLinks l JOIN Records r ON r.Id=l.RecordId WHERE l.DocumentId=$p0 ORDER BY l.Kind, r.Title, r.Id
            """, reader => new DocumentLink(reader.GetString(0), (DocumentLinkTargetKind)reader.GetInt32(1), reader.GetString(2),
                reader.GetString(3), reader.GetString(4)), documentId);

    private static DocumentRecord GetDocument(SqliteConnection connection, SqliteTransaction? transaction, string id) =>
        Query(connection, transaction, SelectDocument + " WHERE Id=$p0", ReadDocument, id).SingleOrDefault()
        ?? throw new ArgumentException("Dokument fehlt.", nameof(id));

    private static DocumentRecord ReadDocument(SqliteDataReader reader) => new(reader.GetString(0), ReadDocumentDraft(reader),
        (DocumentStatus)reader.GetInt32(7), reader.GetInt32(8), reader.GetInt32(9),
        reader.IsDBNull(10) ? null : reader.GetInt32(10), reader.GetString(11), reader.GetString(12), reader.GetString(13));

    private static DocumentDraft ReadDocumentDraft(SqliteDataReader reader) => new(reader.GetString(1), reader.GetString(2),
        reader.GetString(3), (DocumentCategory)reader.GetInt32(4), reader.GetString(5), reader.GetString(6));

    private static void EnsureDocumentTopic(SqliteConnection connection, SqliteTransaction transaction, string topic)
    {
        if (Scalar(connection, transaction, "SELECT Id FROM Topics WHERE Id=$p0", topic) == "")
            throw new ArgumentException("Ein vorhandenes Fachthema für das Dokument auswählen.");
    }

    private static void RequireDocumentStatus(DocumentRecord current, params DocumentStatus[] states)
    {
        if (!states.Contains(current.Status))
            throw new ArgumentException($"Aktion im Status „{DocumentRegister.StatusLabels[current.Status]}“ nicht verfügbar.");
    }

    private static void SetDocumentStatus(SqliteConnection connection, SqliteTransaction transaction, string id, DocumentStatus status) =>
        Exec(connection, transaction, "UPDATE Documents SET Status=$p0 WHERE Id=$p1", (int)status, id);

    private static void EnsureDocumentLinksEditable(SqliteConnection connection, SqliteTransaction transaction, string documentId)
    {
        if (GetDocument(connection, transaction, documentId).Status == DocumentStatus.Retired)
            throw new ArgumentException("Verknüpfungen eines außer Kraft gesetzten Dokuments bleiben erhalten.");
    }

    private static string DocumentUtc() => DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture);
}
