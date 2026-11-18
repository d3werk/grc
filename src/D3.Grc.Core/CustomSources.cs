using System.Text.Json;
using Microsoft.Data.Sqlite;
using static D3.Grc.Core.Database;

namespace D3.Grc.Core;

public sealed record CustomSourceInput(
    string Title, string Url, string Reference, string Summary, IReadOnlyList<string> TopicIds)
{
    public CustomSourceInput Validated()
    {
        var title = Title.Trim();
        if (title.Length is < 1 or > 240)
            throw new ArgumentException("Quellentitel erforderlich (maximal 240 Zeichen).");

        var url = Url.Trim();
        if (url.Length > 2048 || (url.Length > 0 &&
            (!Uri.TryCreate(url, UriKind.Absolute, out var uri) ||
             uri.Scheme != Uri.UriSchemeHttps || string.IsNullOrEmpty(uri.Host) ||
             uri.UserInfo.Length > 0 || !uri.IsWellFormedOriginalString())))
            throw new ArgumentException("Eine vollständige HTTPS-Adresse ohne Zugangsdaten eingeben oder das Feld leer lassen.");

        var reference = Reference.Trim();
        if (reference.Length > 2000)
            throw new ArgumentException("Die Fundstelle darf höchstens 2000 Zeichen enthalten.");
        var summary = Summary.Trim();
        if (summary.Length > 30000)
            throw new ArgumentException("Die Beschreibung darf höchstens 30000 Zeichen enthalten.");

        var topics = TopicIds.Where(id => !string.IsNullOrWhiteSpace(id))
            .Select(id => id.Trim()).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray();
        if (topics.Length == 0)
            throw new ArgumentException("Mindestens ein Thema auswählen.");
        return new CustomSourceInput(title, url, reference, summary, topics);
    }
}

public sealed partial class Workspace
{
    private const string ManualSourceState = "Manuell gepflegt";

    public Task<string> CreateCustomSourceAsync(CustomSourceInput input)
    {
        var source = input.Validated();
        var id = Source.CustomIdPrefix + Guid.NewGuid().ToString("N");
        return Change("CustomSourceCreated", id, SourceDetails(source), (c, t) =>
        {
            EnsureSourceTopics(c, t, source.TopicIds);
            Exec(c, t,
                "INSERT INTO Sources(Id,Title,Publisher,Url,Category,Summary,Reference,CheckState) VALUES($p0,$p1,'',$p2,'Eigene Quelle',$p3,$p4,$p5)",
                id, source.Title, source.Url, source.Summary, source.Reference, ManualSourceState);
            foreach (var topic in source.TopicIds)
                Exec(c, t, "INSERT INTO TopicSources(TopicId,SourceId) VALUES($p0,$p1)", topic, id);
            return id;
        });
    }

    public Task<CustomSourceInput> GetCustomSourceAsync(string id)
    {
        DemandCustomSource(id);
        return Read(c =>
        {
            var topics = Query(c, null,
                "SELECT TopicId FROM TopicSources WHERE SourceId=$p0 ORDER BY TopicId", r => r.GetString(0), id);
            return Query(c, null,
                "SELECT Title,Url,Reference,Summary FROM Sources WHERE Id=$p0",
                r => new CustomSourceInput(r.GetString(0), r.GetString(1), r.GetString(2), r.GetString(3), topics), id)
                .SingleOrDefault() ?? throw new ArgumentException("Eigene Quelle fehlt.");
        });
    }

    public Task UpdateCustomSourceAsync(string id, CustomSourceInput input)
    {
        DemandCustomSource(id);
        var source = input.Validated();
        return Change("CustomSourceUpdated", id, SourceDetails(source), (c, t) =>
        {
            EnsureSourceTopics(c, t, source.TopicIds);
            var usedTopics = Query(c, t,
                """
                SELECT DISTINCT rt.TopicId FROM Records r
                JOIN RecordTopics rt ON rt.RecordId=r.Id
                JOIN TopicSources ts ON ts.TopicId=rt.TopicId AND ts.SourceId=r.SourceId
                WHERE r.SourceId=$p0
                """,
                r => r.GetString(0), id);
            if (usedTopics.Except(source.TopicIds, StringComparer.Ordinal).Any())
                throw new ArgumentException("Das Thema kann nicht entfernt werden, solange ein Eintrag dort diese Quelle als Hauptquelle verwendet.");

            if (Exec(c, t, "UPDATE Sources SET Title=$p0,Url=$p1,Summary=$p2,Reference=$p3 WHERE Id=$p4",
                    source.Title, source.Url, source.Summary, source.Reference, id) != 1)
                throw new ArgumentException("Eigene Quelle fehlt.");
            Exec(c, t, "DELETE FROM TopicSources WHERE SourceId=$p0", id);
            foreach (var topic in source.TopicIds)
                Exec(c, t, "INSERT INTO TopicSources(TopicId,SourceId) VALUES($p0,$p1)", topic, id);
            return true;
        });
    }

    private static void EnsureSourceTopics(
        SqliteConnection connection,
        SqliteTransaction transaction,
        IReadOnlyList<string> topicIds)
    {
        var available = Query(connection, transaction, "SELECT Id FROM Topics", r => r.GetString(0));
        if (topicIds.Except(available, StringComparer.Ordinal).Any())
            throw new ArgumentException("Mindestens eines der gewählten Themen ist im Projekt nicht vorhanden.");
    }

    private static void DemandCustomSource(string id)
    {
        if (!Source.IsCustomId(id))
            throw new ArgumentException("Nur selbst angelegte Quellen können hier bearbeitet werden.");
    }

    private static string SourceDetails(CustomSourceInput source) =>
        JsonSerializer.Serialize(new { source.Title, source.Reference, source.TopicIds });
}
