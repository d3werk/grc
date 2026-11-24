using System.Globalization;
using static D3.Grc.Core.Database;

namespace D3.Grc.Core;

public static class DemoTenant
{
    public const string Name = "Nordlicht Service GmbH · Demo";
    public static CompanyProfile Profile => new(CompanySize.SmallMedium, CompanyIndustry.DigitalServices,
        CompanyFactors.PersonalData | CompanyFactors.CloudServices | CompanyFactors.CriticalProcesses |
        CompanyFactors.AiSystems | CompanyFactors.ExternalSuppliers | CompanyFactors.OpenSourceSoftware,
        ["PRIVACY", "ISMS", "NIS2", "AI", "CLOUD", "BCM", "OSS", "PROCESS"],
        2, 4, CompanyRiskFocus.OperationalResilience);
}

public sealed partial class Workspace
{
    internal async Task SeedDemoAsync()
    {
        const string reason = "Fiktiver Mandant mit synthetischen Beispieldaten.";
        await Change("DemoSeeded", "", reason, (connection, transaction) =>
        {
            if (Scalar(connection, transaction, "SELECT Value FROM Metadata WHERE Key='DemoSeeded'") != "")
                return false;

            var today = DateOnly.FromDateTime(DateTime.Today);
            var recorded = DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture);
            string Date(int days) => today.AddDays(days).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            ModuleSeed Example(string id, string kind, string code, string title, string status, string topic,
                string body, string scope = "Kundenservice") => new(new Entry
                {
                    Id = id, Kind = kind, Code = code, Title = title, Status = status, Body = body,
                    SourceId = "INTERNAL", Owner = "Demo-Fachverantwortung", Scope = scope, DueDate = Date(30)
                }, topic);

            var examples = new[]
            {
                Example("demo-risk-01", "Risk", "R-01", "Unberechtigter Zugriff auf Support-Daten", "In Bearbeitung", "ISMS",
                    "Freigaben und Rezertifizierungen für Support-Konten werden stichprobenartig geprüft."),
                Example("demo-risk-02", "Risk", "R-02", "Passwortweitergabe für Notfallkonten", "Offen", "ISMS",
                    "Eine Ausgangsbewertung liegt vor. Das Restrisiko wird nach dem Test erfasst.", "IT-Betrieb"),
                Example("demo-risk-03", "Risk", "R-03", "Ausfall der fiktiven Telefonanlage", "Offen", "BCM",
                    "Ersteinschätzung steht noch aus."),
                Example("demo-control-01", "Control", "C-01", "Rezertifizierung von Support-Konten", "Umgesetzt", "ISMS",
                    "Kontozugriffe mit Freigabedaten abgleichen und Abweichungen dokumentieren."),
                Example("demo-privacy-01", "Requirement", "DS-01", "Auskunftsersuchen dokumentieren", "Anwendbar", "PRIVACY",
                    "Arbeitsbeispiel. Eingang, Prüfung und Antwort anhand einer fiktiven Anfrage dokumentieren."),
                Example("demo-privacy-02", "Control", "DS-02", "Testdaten im Personalbereich löschen", "Geplant", "PRIVACY",
                    "Reine Beispieldaten. Fachliche Verantwortlichkeiten und Löschfrist überprüfen.", "Personal"),
                Example("demo-supplier-01", "Supplier", "L-01", "Beispiel Hosting KG", "In Prüfung", "ISMS",
                    "Fiktiver Hostingpartner für das Kundenportal."),
                Example("demo-oss-01", "OssComponent", "OSS-01", "Beispielbibliothek für Protokolle", "In Bearbeitung", "OSS",
                    "Eigene fiktive Komponentenbezeichnung. Lizenz und Verantwortlichkeit vor Einsatz prüfen.", "IT-Betrieb")
            };
            var rows = examples.Concat(ProcessModule.DemoRows).Concat(ResilienceModule.DemoRows)
                .Concat(AiModule.DemoRows).Concat(ContractModule.DemoRows).Concat(AuditIksModule.DemoRows);
            foreach (var sample in rows)
            {
                var entry = sample.Entry;
                entry.Validate();
                var fields = DesktopModules.ValidateFields(entry, sample.Fields ?? []);
                if (!SourceFits(connection, transaction, entry.SourceId, sample.Topic))
                    throw new InvalidDataException($"Demo-Quelle für {entry.Code} passt nicht zum Thema.");
                Exec(connection, transaction, """
                    INSERT INTO Records(Id,Kind,Code,Title,SourceId,TemplateId,Locator,Body,Owner,Status,DueDate,Scope,Version,IsSeed)
                    VALUES($p0,$p1,$p2,$p3,$p4,NULL,'',$p5,$p6,$p7,$p8,$p9,1,0)
                    """, entry.Id, entry.Kind, entry.Code, entry.Title, entry.SourceId, entry.Body, entry.Owner,
                    entry.Status, entry.DueDate, entry.Scope);
                Exec(connection, transaction, "INSERT INTO RecordTopics VALUES($p0,$p1)", entry.Id, sample.Topic);
                Exec(connection, transaction, "INSERT INTO RecordSources VALUES($p0,$p1,'','Primär')", entry.Id, entry.SourceId);
                WriteModuleFields(connection, transaction, entry.Id, fields);
            }

            var links = new[] { new ModuleLink("demo-risk-01", "demo-control-01", "Risiko → Control", "Beispielhafte Risikobehandlung.") }
                .Concat(ProcessModule.DemoLinks).Concat(ResilienceModule.DemoLinks).Concat(AiModule.DemoLinks)
                .Concat(ContractModule.DemoLinks).Concat(AuditIksModule.DemoLinks);
            foreach (var link in links)
            {
                var from = Get(connection, transaction, link.From);
                var to = Get(connection, transaction, link.To);
                var expected = LinkTypes[link.Type];
                RequireKind(from, expected.From);
                RequireKind(to, expected.To);
                if (link.Type == ProcessModule.ParentType)
                    ValidateProcessLink(connection, transaction, from, to);
                var linkId = Guid.NewGuid().ToString("N");
                Exec(connection, transaction, "INSERT INTO Links VALUES($p0,$p1,$p2,$p3,$p4,$p5)",
                    linkId, from.Id, to.Id, link.Type, link.Reason, from.SourceId);
                Exec(connection, transaction, "INSERT INTO MappingAssessments VALUES($p0,'Unterstützend',$p1,'Bestätigt')", linkId, from.Scope);
            }

            SeedDemoAuditIks(connection, transaction);
            foreach (var (id, probability, impact, remainingProbability, remainingImpact) in new[]
            {
                ("demo-risk-01", 4, 5, (int?) 2, (int?) 4),
                ("demo-risk-02", 3, 4, null, null)
            })
            {
                Exec(connection, transaction, """
                    INSERT INTO RiskAssessments (Id, RiskId, AssessedOn, InherentLikelihood, InherentImpact,
                        ResidualLikelihood, ResidualImpact, Response, Rationale, Actor, RiskVersion, RecordedUtc)
                    VALUES ($p0,$p1,$p2,$p3,$p4,$p5,$p6,$p7,$p8,$p9,1,$p10)
                    """, Guid.NewGuid().ToString("N"), id, Date(0), probability, impact, remainingProbability, remainingImpact,
                    (int)RiskResponse.Mitigate, "Fiktive Bewertung zur Demonstration der Risikomatrix.", identity.User!, recorded);
            }

            Exec(connection, transaction, """
                INSERT INTO Documents(Id,Title,TopicId,Scope,Category,Owner,Content,Status,Version,Revision,UpdatedUtc)
                VALUES('demo-document-01','Demo · Ablauf zur Rezertifizierung','ISMS','Kundenservice',0,'IT-Betrieb',
                    'Synthetischer Entwurf. Verantwortliche prüfen regelmäßig die Freigaben der Beispielkonten.',0,1,1,$p0)
                """, recorded);
            Exec(connection, transaction, """
                INSERT INTO Documents(Id,Title,TopicId,Scope,Category,Owner,Content,Status,Version,Revision,UpdatedUtc)
                VALUES('demo-document-02','Demo · Prüfung eines Löschantrags','PRIVACY','Personal',0,'Datenschutzkoordination',
                    'Fiktiver Fall. Anfrage bestätigen, Identität prüfen, Entscheidung und Nachweise dokumentieren.',0,1,1,$p0)
                """, recorded);
            Exec(connection, transaction, "INSERT INTO Metadata(Key,Value) VALUES('DemoSeeded','1')");
            return true;
        });
    }
}
