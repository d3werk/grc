using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using static D3.Grc.Core.Database;

namespace D3.Grc.Core;

public static class AuditIksModule
{
    public static ModuleSeed[] DemoRows =>
    [
        Demo("demo-audit-01", "Audit", "AUD-01", "Prüfung der Support-Berechtigungen", "In Prüfung", "Interne Revision",
            "Ziel: nachvollziehbare Freigaben und regelmäßige Rezertifizierung. Stichprobe: zwei synthetische Support-Konten.", 14),
        Demo("demo-audit-02", "Audit", "AUD-02", "Planprüfung der Rezertifizierung", "Geplant", "Interne Revision",
            "Prüft die Umsetzung und die erneute Teilnahme aller zuständigen Freigabestellen.", 90),
        Demo("demo-finding-01", "Finding", "F-01", "Rezertifizierungsbeleg fehlt", "Offen", "IT-Betrieb",
            "Soll: Jede Stichprobe enthält einen verknüpften Freigabebeleg. Beobachtung: Bei einem fiktiven Testkonto fehlt der Beleg. Empfehlung: Belegablage und Vertretung regeln.", -4),
        Demo("demo-iks-action-01", "Measure", "M-IKS-01", "Rezertifizierungsbeleg nachführen", "In Bearbeitung", "IT-Betrieb",
            "Den Beleg dem fiktiven Vorgang zuordnen. Bei der nächsten Stichprobe prüfen, ob alle Bezüge nachvollziehbar sind.", 12)
    ];

    public static ModuleLink[] DemoLinks =>
    [
        new("demo-audit-01", "demo-control-01", "Audit → Control", "Berechtigungskontrolle ist das Prüfobjekt."),
        new("demo-audit-02", "demo-control-01", "Audit → Control", "Die Folgeprüfung verwendet dieselbe Kontrolle."),
        new("demo-finding-01", "demo-audit-01", "Finding → Audit", "Feststellung aus der synthetischen Stichprobe."),
        new("demo-finding-01", "demo-control-01", "Finding → Control", "Für die Kontrolle fehlte ein Beleg."),
        new("demo-iks-action-01", "demo-finding-01", "Maßnahme → Finding", "Beleg ergänzen und den Ablauf künftig prüfen.")
    ];

    private static ModuleSeed Demo(string id, string kind, string code, string title, string status, string owner, string body, int days) =>
        new(new Entry
        {
            Id = id, Kind = kind, Code = code, Title = title, Status = status, Owner = owner, Scope = "Kundenservice",
            SourceId = "INTERNAL", Body = "Synthetische Beispieldaten. " + body,
            DueDate = DateOnly.FromDateTime(DateTime.Today).AddDays(days).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
        }, "ISMS");
}

public sealed partial class Workspace
{
    private void SeedDemoAuditIks(SqliteConnection connection, SqliteTransaction transaction)
    {
        var now = DateTimeOffset.UtcNow;
        var today = DateOnly.FromDateTime(DateTime.Today);
        var control = Get(connection, transaction, "demo-control-01");
        var evidenceId = "demo-audit-evidence";
        var content = Encoding.UTF8.GetBytes("Synthetischer Nachweis für D3 GRC Demo\nTestkonto 001: Freigabe vorhanden.\nTestkonto 002: Rezertifizierungsbeleg fehlt.\n");
        Exec(connection, transaction, """
            INSERT INTO Evidence (Id, Title, ControlId, SourceId, SourceDescription, FileName, Hash, Content, CollectedUtc, ValidUntil)
            VALUES ($p0, $p1, $p2, $p3, $p4, $p5, $p6, $p7, $p8, $p9)
            """, evidenceId, "Demo · Stichprobe Support-Konten", control.Id, control.SourceId, "Synthetische Audit-Stichprobe",
            "demo-support-stichprobe.txt", Convert.ToHexString(SHA256.HashData(content)), content, now.AddMinutes(-1).ToString("O"), now.AddDays(120).ToString("O"));
        Exec(connection, transaction, """
            INSERT INTO AuditTests (Id, AuditId, ControlId, SourceId, EvidenceId, Outcome, Notes, TestedUtc)
            VALUES ('demo-audit-test-01', 'demo-audit-01', $p0, $p1, $p2, 'Abweichung', $p3, $p4)
            """, control.Id, control.SourceId, evidenceId, "Zwei fiktive Konten geprüft; ein Rezertifizierungsbeleg fehlt.", now.ToString("O"));
        var sources = Query(connection, transaction, "SELECT SourceId, Locator, Role FROM RecordSources WHERE RecordId=$p0",
            reader => new[] { reader.GetString(0), reader.GetString(1), reader.GetString(2) }, control.Id);
        Exec(connection, transaction, """
            INSERT INTO AuditCriteria (AuditId, ControlId, ControlVersion, CriterionJson)
            VALUES ('demo-audit-01', $p0, $p1, $p2)
            """, control.Id, control.Version, JsonSerializer.Serialize(new { control, sources }));
        SaveIksProfile(connection, transaction, control.Id, new IksProfile("Nur freigegebene Zugriffe auf Kundenservice-Daten erlauben.",
            IksControlType.Preventive, IksControlMode.Hybrid, IksFrequency.Quarterly, true));
        InsertIksCheck(connection, transaction, "demo-iks-check-01", control,
            new IksCheckInput(today, "Demo · Interne Revision", IksCheckType.IndependentTest, IksCheckOutcome.Deviation,
                "Zwei fiktive Vorgänge gegen Freigabe- und Rezertifizierungsbeleg vergleichen.",
                "Bei einem Vorgang fehlt der Rezertifizierungsbeleg.", evidenceId, "demo-iks-action-01"), now);
    }
}
