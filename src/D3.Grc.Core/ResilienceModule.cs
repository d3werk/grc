using System.Globalization;

namespace D3.Grc.Core;

public static class ResilienceModule
{
    public const string Service = "ResilienceService";
    public const string Scenario = "ResilienceScenario";
    public const string Bia = "ContinuityBia";
    public const string Plan = "ContinuityPlan";
    public const string Exercise = "ContinuityExercise";

    private const string BusinessProcess = "BusinessProcess";
    private const string Topic = "BCM";
    private const string DemoScope = "Kundenservice";
    private const string Active = "Aktiv";
    private const string Completed = "Abgeschlossen";
    private const string NotEvaluable = "Nicht auswertbar";
    private const string Passed = "Ziele erreicht";
    private const string Partial = "Ziele teilweise erreicht";
    private const string Failed = "Ziele verfehlt";

    private static readonly string[] _exerciseTypes = ["Planspiel", "Technischer Test", "Simulation", "Vollübung"];
    private static readonly string[] _results = [Passed, Partial, Failed, NotEvaluable];
    private static readonly string[] _planStates = ["Entwurf", "In Prüfung", Active, "Archiviert"];
    private static readonly string[] _testStates = ["Entwurf", "Geplant", "In Durchführung", Completed, "Archiviert"];

    public static readonly ModuleSpec[] Specs =
    [
        new(Service, "Kritische Geschäftsdienste", "Geschäftsdienst",
            "Ausfallfolgen, Toleranzgrenzen und betriebliche Abhängigkeiten festhalten.", Topic,
            [
                new("business_impact", "Auswirkungen eines Ausfalls", "long"),
                new("tolerance_minutes", "Tolerierbare Unterbrechung in Minuten", "number"),
                new("tolerance_criteria", "Begründung und weitere Toleranzkriterien", "long"),
                new("affected_parties", "Betroffene und zugesagte Mindestleistung", "long"),
                new("dependencies", "Personal, Standorte und Systeme", "long")
            ], _planStates, Validate),
        new(Scenario, "Szenariotests", "Szenariotest",
            "Kritische Störungen erproben. Beobachtete Dauer mit der Toleranz zum Testzeitpunkt vergleichen.", Topic,
            [
                new("scenario", "Störungsszenario", "long"),
                new("exercise_type", "Art des Szenariotests", "select", _exerciseTypes),
                new("objectives", "Prüfziele und Erfolgskriterien", "long"),
                new("performed_on", "Durchgeführt am", "date"),
                new("tolerance_snapshot_minutes", "Zeittoleranz zum Testzeitpunkt in Minuten", "number"),
                new("disruption_minutes", "Beobachtete Unterbrechung in Minuten", "number"),
                new("result", "Gesamturteil", "select", _results),
                new("findings", "Feststellungen und Verbesserungsbedarf", "long")
            ], _testStates, Validate),
        new(Bia, "Business Impact Analysen", "Auswirkungsanalyse",
            "Auswirkungen auf Prozesse, erforderliche Ressourcen und Wiederanlaufziele beurteilen.", Topic,
            [
                new("assessed_on", "Bewertet am", "date"),
                new("criticality", "Kritikalität", "select", ["Kritisch", "Wesentlich", "Übrige"]),
                new("impact_timeline", "Auswirkungen über die Zeit", "long"),
                new("minimum_service", "Mindestbetrieb und erforderliche Ressourcen", "long"),
                new("mtpd_minutes", "Maximal tolerierbare Ausfallzeit in Minuten (MTPD)", "number"),
                new("rto_minutes", "Zielzeit für den Wiederanlauf in Minuten (RTO)", "number"),
                new("rpo_minutes", "Maximal tolerierbarer Datenverlust in Minuten (RPO)", "number"),
                new("target_rationale", "Herleitung der Wiederanlaufziele", "long")
            ], _planStates, Validate),
        new(Plan, "Wiederanlaufpläne", "Wiederanlaufplan",
            "Den Notfallbetrieb mit Aktivierung, Rollen, Kommunikation, Wiederanlauf und Rückkehr beschreiben.", Topic,
            [
                new("activation", "Auslöser und Entscheidung zur Aktivierung", "long"),
                new("immediate_actions", "Sofortmaßnahmen", "long"),
                new("roles_contacts", "Rollen, Vertretung und Kontaktwege", "long"),
                new("escalation", "Eskalation und Krisenkommunikation", "long"),
                new("recovery_steps", "Wiederanlauf in Reihenfolge", "long"),
                new("return_to_normal", "Prüfung und Rückkehr zum Regelbetrieb", "long"),
                new("released_on", "Freigegeben am", "date")
            ], _planStates, Validate),
        new(Exercise, "Notfallübungen", "Notfallübung",
            "Notfallpläne erproben und Feststellungen mit Maßnahmen zur Verbesserung verbinden.", Topic,
            [
                new("exercise_type", "Übungsform", "select", _exerciseTypes),
                new("scenario", "Übungsszenario", "long"),
                new("objectives", "Übungsziele und Erfolgskriterien", "long"),
                new("performed_on", "Durchgeführt am", "date"),
                new("rto_snapshot_minutes", "Wiederanlaufziel zur Übung in Minuten", "number"),
                new("recovery_minutes", "Beobachteter Wiederanlauf in Minuten", "number"),
                new("result", "Gesamturteil", "select", _results),
                new("findings", "Feststellungen und Verbesserungsbedarf", "long")
            ], _testStates, Validate)
    ];

    public static readonly ModuleRelation[] Relations =
    [
        new("Geschäftsdienst → Geschäftsprozess", Service, BusinessProcess),
        new("Geschäftsdienst → Lieferant", Service, "Supplier"),
        new("Geschäftsdienst → Risiko", Service, "Risk"),
        new("Geschäftsdienst → Control", Service, "Control"),
        new("Szenariotest → Geschäftsdienst", Scenario, Service),
        new("Szenariotest → Maßnahme", Scenario, "Measure"),
        new("BIA → Geschäftsprozess", Bia, BusinessProcess),
        new("BIA → Geschäftsdienst", Bia, Service),
        new("BIA → Lieferant", Bia, "Supplier"),
        new("Wiederanlaufplan → BIA", Plan, Bia),
        new("Wiederanlaufplan → Geschäftsdienst", Plan, Service),
        new("Wiederanlaufplan → Control", Plan, "Control"),
        new("Notfallübung → Wiederanlaufplan", Exercise, Plan),
        new("Notfallübung → Szenariotest", Exercise, Scenario),
        new("Notfallübung → Maßnahme", Exercise, "Measure")
    ];

    public static readonly ModuleLink[] DemoLinks =
    [
        new("demo-resilience-service-portal", "demo-process-service", "Geschäftsdienst → Geschäftsprozess", "Die Auftragsannahme nutzt denselben Kundenserviceprozess."),
        new("demo-resilience-service-portal", "demo-supplier-01", "Geschäftsdienst → Lieferant", "Der externe Support unterstützt die Wiederherstellung des Kundenportals."),
        new("demo-resilience-service-portal", "demo-resilience-risk-service", "Geschäftsdienst → Risiko", "Ein vollständiger Portalausfall gefährdet die fristgerechte Auftragsannahme."),
        new("demo-resilience-scenario-portal", "demo-resilience-service-portal", "Szenariotest → Geschäftsdienst", "Der Ausfalltest prüft den Ersatzkanal des Portals."),
        new("demo-resilience-scenario-login", "demo-resilience-service-identity", "Szenariotest → Geschäftsdienst", "Der Test prüft die Anmeldung über den Ersatzdienst."),
        new("demo-resilience-scenario-provider", "demo-resilience-service-portal", "Szenariotest → Geschäftsdienst", "Die Simulation betrifft Auftragsannahme und Rückstau."),
        new("demo-resilience-scenario-portal", "demo-bcm-measure-failover", "Szenariotest → Maßnahme", "Die Überschreitung erfordert eine schnellere Umschaltung."),
        new("demo-bcm-bia-portal", "demo-process-service", "BIA → Geschäftsprozess", "Die Analyse bewertet den Prozess von Annahme bis Bestätigung."),
        new("demo-bcm-bia-portal", "demo-resilience-service-portal", "BIA → Geschäftsdienst", "Die BIA vertieft die Ausfallfolgen des Portals."),
        new("demo-bcm-bia-portal", "demo-supplier-01", "BIA → Lieferant", "Fristgerechte Störungsunterstützung ist eine Abhängigkeit der Wiederanlaufplanung."),
        new("demo-bcm-bia-identity", "demo-resilience-service-identity", "BIA → Geschäftsdienst", "Der Zugriff auf Arbeitsaufträge ist für den Mindestbetrieb nötig."),
        new("demo-bcm-plan-portal", "demo-bcm-bia-portal", "Wiederanlaufplan → BIA", "Der Plan übernimmt die Referenzziele der Portalanalyse."),
        new("demo-bcm-plan-portal", "demo-resilience-service-portal", "Wiederanlaufplan → Geschäftsdienst", "Der Plan aktiviert einen alternativen Auftragskanal."),
        new("demo-bcm-plan-identity", "demo-bcm-bia-identity", "Wiederanlaufplan → BIA", "Der Plan adressiert den Einsatz vorbereiteter Notfallzugänge."),
        new("demo-bcm-exercise-portal", "demo-bcm-plan-portal", "Notfallübung → Wiederanlaufplan", "Umschaltung und Rückkehr wurden gemeinsam geprobt."),
        new("demo-bcm-exercise-portal", "demo-resilience-scenario-portal", "Notfallübung → Szenariotest", "Beide Aufzeichnungen gehören zum selben synthetischen Probelauf."),
        new("demo-bcm-exercise-portal", "demo-bcm-measure-failover", "Notfallübung → Maßnahme", "Kontaktliste und Fallübergabe werden überarbeitet."),
        new("demo-bcm-exercise-identity", "demo-bcm-plan-identity", "Notfallübung → Wiederanlaufplan", "Die vorbereiteten Zugänge wurden kontrolliert aktiviert."),
        new("demo-bcm-exercise-followup", "demo-bcm-plan-portal", "Notfallübung → Wiederanlaufplan", "Die Folgeübung prüft die überarbeitete Umschaltung.")
    ];

    public static ModuleSeed[] DemoRows
    {
        get
        {
            var today = DateOnly.FromDateTime(DateTime.Today);
            string Day(int offset) => today.AddDays(offset).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            return
            [
                Seed("demo-resilience-service-portal", Service, "RES-01", "Digitale Auftragsannahme", "Leitung Kundenservice", Active, Day(75),
                    "Beispieldienst für die Annahme und Bestätigung von Kundenaufträgen.",
                    ("business_impact", "Anfragen stauen sich. Fristkritische Aufträge können ohne Ersatzkanal unbestätigt bleiben."),
                    ("tolerance_minutes", "120"), ("tolerance_criteria", "Innerhalb von 120 Minuten steht ein Ersatzkanal für dringende Aufträge bereit."),
                    ("affected_parties", "Bestandskunden und Supportteam. Dringende Aufträge bleiben priorisiert erfassbar."),
                    ("dependencies", "Kundenportal, Hosting, Supportteam und exportierte Auftragsliste.")),
                Seed("demo-resilience-service-identity", Service, "RES-02", "Anmeldung im Kundenservice", "IT-Betrieb", Active, Day(101),
                    "Beispieldienst für Anmeldung und Zugang zu den Arbeitsaufträgen des Supports.",
                    ("business_impact", "Die Mitarbeitenden können ohne Zugang keine Kundenaufträge bearbeiten."),
                    ("tolerance_minutes", "60"), ("tolerance_criteria", "Eine benannte Rufbereitschaft erhält innerhalb von 60 Minuten Zugriff."),
                    ("affected_parties", "Supportteam und IT-Bereitschaft."), ("dependencies", "Identitätsdienst, Notfallkonten und Rufbereitschaft.")),
                Seed("demo-resilience-scenario-portal", Scenario, "RST-01", "Portalausfall mit Ersatzkanal", "Krisenkoordination", Completed, Day(-14),
                    "Synthetischer Störungstest außerhalb der Produktion.",
                    ("scenario", "Im Test wird die Auftragsannahme im Kundenportal als nicht verfügbar angenommen."),
                    ("exercise_type", "Simulation"), ("objectives", "Dringende Aufträge spätestens nach 120 Minuten über den Ersatzkanal bearbeiten."),
                    ("performed_on", Day(-14)), ("tolerance_snapshot_minutes", "120"), ("disruption_minutes", "145"), ("result", Failed),
                    ("findings", "25 Minuten Überschreitung. Die manuelle Fallübergabe dauerte zu lange. Eskalation und Übergabe vereinfachen.")),
                Seed("demo-resilience-scenario-login", Scenario, "RST-02", "Ausfall der Standardanmeldung", "IT-Betrieb", Completed, Day(-8),
                    "Synthetischer Techniktest mit vorbereitetem Notfallkonto.",
                    ("scenario", "Im Test steht der primäre Identitätsdienst nicht zur Verfügung."),
                    ("exercise_type", "Technischer Test"), ("objectives", "Benannte Mitarbeitende erhalten innerhalb von 60 Minuten Zugriff."),
                    ("performed_on", Day(-8)), ("tolerance_snapshot_minutes", "60"), ("disruption_minutes", "28"), ("result", Passed),
                    ("findings", "Der kontrollierte Notfallzugriff stand nach 28 Minuten bereit.")),
                Seed("demo-resilience-scenario-provider", Scenario, "RST-03", "Ausfall des Hosting-Anbieters", "Leitung Kundenservice", "Geplant", Day(-3),
                    "Ausstehendes Planspiel zur Kommunikation bei kurzfristiger Nichtverfügbarkeit des Dienstleisters.",
                    ("scenario", "Das Hosting-Team ist während eines Portalausfalls vorübergehend nicht erreichbar."),
                    ("exercise_type", "Planspiel"), ("objectives", "Interne Eskalation, Priorisierung und Kommunikation am Ersatzkanal nachvollziehen.")),
                Seed("demo-bcm-bia-portal", Bia, "BIA-01", "Ausfallfolgen der Auftragsannahme", "Leitung Kundenservice", Active, Day(116),
                    "Synthetische BIA für Portal, Ersatzkanal und Auftragsbestätigung.",
                    ("assessed_on", Day(-37)), ("criticality", "Kritisch"), ("impact_timeline", "Bis 30 Minuten Rückstau. Ab 90 Minuten gefährdete Reaktionsfrist. Ab 120 Minuten erheblich verzögerte dringende Aufträge."),
                    ("minimum_service", "Telefonischer Eingang, Fallliste, Erfassung dringender Fälle durch mindestens zwei Mitarbeitende."),
                    ("mtpd_minutes", "120"), ("rto_minutes", "90"), ("rpo_minutes", "15"),
                    ("target_rationale", "30 Minuten Reserve für Übergabe und Kundenkommunikation.")),
                Seed("demo-bcm-bia-identity", Bia, "BIA-02", "Arbeitszugang für Support und Betrieb", "IT-Betrieb", Active, Day(123),
                    "Synthetische BIA für die Anmeldung der Rufbereitschaft.",
                    ("assessed_on", Day(-29)), ("criticality", "Wesentlich"), ("impact_timeline", "Binnen 30 Minuten müssen die vorrangigen Arbeitsplätze wieder erreichbar sein."),
                    ("minimum_service", "Vier berechtigte Arbeitsplätze und eine verfügbare Freigaberolle."),
                    ("mtpd_minutes", "60"), ("rto_minutes", "35"), ("rpo_minutes", "10"),
                    ("target_rationale", "Für Zugriffskontrolle und Protokollprüfung verbleiben 25 Minuten.")),
                Seed("demo-bcm-plan-portal", Plan, "BCP-01", "Auftragsannahme im Notbetrieb", "Krisenkoordination", Active, Day(42),
                    "Der Ersatzkanal sichert fristkritische Aufträge bei Ausfall des Portals.",
                    ("activation", "Die Krisenkoordination entscheidet bei bestätigtem Portalausfall und absehbarer Überschreitung des Wiederanlaufziels."),
                    ("immediate_actions", "Störung bestätigen, Ersatzkanal aktivieren, laufende Fälle sichern."),
                    ("roles_contacts", "Krisenkoordination aktiviert. Supportleitung steuert die Warteschlange. IT-Betrieb koordiniert die Störungsbehebung."),
                    ("escalation", "Im Beispiel informieren Supportleitung und IT-Betrieb die betroffenen Rollen über den internen Krisenkanal."),
                    ("recovery_steps", "1. Ersatzkanal prüfen. 2. Warteschlange priorisieren. 3. Bestätigungen versenden. 4. Portalstatus testen."),
                    ("return_to_normal", "Testauftrag nach Freigabe. Rückstände abgleichen und Krisenkommunikation beenden."), ("released_on", Day(-32))),
                Seed("demo-bcm-plan-identity", Plan, "BCP-02", "Freigegebene Notfallzugänge", "IT-Betrieb", Active, Day(68),
                    "Beispielplan für überprüfte und zeitlich beschränkte Arbeitszugänge.",
                    ("activation", "Die IT-Bereitschaft meldet den Ausfall. Die Teamleitung genehmigt vorbereitete Notfallzugänge."),
                    ("roles_contacts", "IT-Bereitschaft richtet den Zugang ein. Freigaberolle dokumentiert Zeitpunkt und Umfang."),
                    ("recovery_steps", "Vorbereitete Zugänge aktivieren und Verbindung sowie protokollierte Berechtigungen prüfen."),
                    ("return_to_normal", "Notfallzugänge nach Prüfung des Regelbetriebs deaktivieren."), ("released_on", Day(-25))),
                Seed("demo-bcm-exercise-portal", Exercise, "BCÜ-01", "Notbetrieb bei Portalausfall", "Krisenkoordination", Completed, Day(-14),
                    "Synthetische Übung mit protokolliertem Umschalt- und Wiederanlaufzeitpunkt.",
                    ("exercise_type", "Simulation"), ("scenario", "Während der Auftragsannahme fällt das Portal aus."),
                    ("objectives", "Im Ersatzkanal innerhalb des RTO drei synthetische Fälle bestätigen."),
                    ("performed_on", Day(-14)), ("rto_snapshot_minutes", "90"), ("recovery_minutes", "145"), ("result", Failed),
                    ("findings", "Das Wiederanlaufziel wurde um 55 Minuten verfehlt. Arbeitsübergabe und Freigabeschritt verkürzen.")),
                Seed("demo-bcm-exercise-identity", Exercise, "BCÜ-02", "Anmeldung über Notfallzugang", "IT-Betrieb", Completed, Day(-8),
                    "Synthetische Übung zur Aktivierung und Aufhebung des Notfallzugangs.",
                    ("exercise_type", "Technischer Test"), ("scenario", "Die Standardanmeldung steht im Übungsfenster nicht zur Verfügung."),
                    ("objectives", "Zugänge für die Rufbereitschaft innerhalb von 35 Minuten aktivieren und den Entzug prüfen."),
                    ("performed_on", Day(-8)), ("rto_snapshot_minutes", "35"), ("recovery_minutes", "28"), ("result", Passed),
                    ("findings", "Zugriff und anschließender Entzug wurden in der Übung beobachtet.")),
                Seed("demo-bcm-exercise-followup", Exercise, "BCÜ-03", "Wiederholung der Portalumschaltung", "Leitung Kundenservice", "Geplant", Day(36),
                    "Geplante Folgeübung mit der überarbeiteten Fallübergabe.",
                    ("exercise_type", "Vollübung"), ("scenario", "Portalausfall mit Wiederanlauf während einer angenommenen erhöhten Anfragelast."),
                    ("objectives", "Ersatzkanal innerhalb von 90 Minuten. Fallpriorisierung und Kundeninformation nachweisen.")),
                Seed("demo-resilience-risk-service", "Risk", "R-RES-01", "Ausfall der digitalen Auftragsannahme", "Kundenservice", "Offen", Day(60),
                    "Ein längerer Portal- oder Hostingausfall verzögert dringende Aufträge. Der Ersatzkanal reduziert die Betriebsunterbrechung."),
                Seed("demo-bcm-measure-failover", "Measure", "M-BCM-01", "Übergabe an Ersatzkanal vereinfachen", "Krisenkoordination", "In Bearbeitung", Day(17),
                    "Kontaktliste prüfen, Übergabe automatisieren und die Verbesserung in einer Folgeübung mit synthetischen Fällen überprüfen.")
            ];
        }
    }

    private static ModuleSeed Seed(string id, string kind, string code, string title, string owner, string status,
        string due, string description, params (string Key, string Value)[] fields) => new(
        new Entry { Id = id, Kind = kind, Code = code, Title = title, Owner = owner, Status = status,
            DueDate = due, Scope = DemoScope, Body = description },
        Topic, fields.Length == 0 ? null : fields.ToDictionary(field => field.Key, field => field.Value));

    private static void Validate(Entry entry, IReadOnlyDictionary<string, string> fields)
    {
        var spec = Specs.Single(item => item.Kind == entry.Kind);
        foreach (var field in spec.Fields.Where(field => field.Type == "number"))
            if (Value(fields, field.Key) is { Length: > 0 } number && !ParseMinutes(number, out _))
                throw new ArgumentException($"{field.Label} muss eine ganze Zahl zwischen 0 und 9.999.999 sein.");

        switch (entry.Kind)
        {
            case Service when entry.Status == Active:
                Require(entry, fields, "business_impact", "tolerance_minutes", "tolerance_criteria");
                break;
            case Bia:
                if (entry.Status == Active)
                    Require(entry, fields, "assessed_on", "criticality", "impact_timeline", "mtpd_minutes", "rto_minutes");
                if (ParseMinutes(Value(fields, "mtpd_minutes"), out var mtpd) &&
                    ParseMinutes(Value(fields, "rto_minutes"), out var rto) && rto > mtpd)
                    throw new ArgumentException("Die Wiederanlaufzeit (RTO) darf die tolerierbare Ausfallzeit (MTPD) nicht überschreiten.");
                break;
            case Plan when entry.Status == Active:
                Require(entry, fields, "activation", "roles_contacts", "recovery_steps", "released_on");
                break;
            case Scenario or Exercise when entry.Status == Completed:
                Require(entry, fields, "scenario", "objectives", "performed_on", "result", "findings");
                var result = Value(fields, "result");
                var (target, actual) = entry.Kind == Scenario
                    ? ("tolerance_snapshot_minutes", "disruption_minutes")
                    : ("rto_snapshot_minutes", "recovery_minutes");
                var hasTarget = ParseMinutes(Value(fields, target), out var targetMinutes);
                var hasActual = ParseMinutes(Value(fields, actual), out var actualMinutes);
                if (hasTarget != hasActual)
                    throw new ArgumentException("Für den Zeitvergleich bitte Zielzeit und beobachtete Dauer gemeinsam erfassen.");
                if (entry.Kind == Scenario && result != NotEvaluable && !hasTarget)
                    throw new ArgumentException("Für einen auswertbaren Szenariotest sind Zeittoleranz und beobachtete Unterbrechung erforderlich.");
                if (hasTarget && actualMinutes > targetMinutes && result == Passed)
                    throw new ArgumentException("Bei überschrittener Zielzeit bitte ein passendes Gesamturteil wählen.");
                break;
        }
    }

    private static void Require(Entry entry, IReadOnlyDictionary<string, string> fields, params string[] keys)
    {
        var spec = Specs.Single(item => item.Kind == entry.Kind);
        var labels = spec.Fields.Where(field => keys.Contains(field.Key) && string.IsNullOrWhiteSpace(Value(fields, field.Key)))
            .Select(field => field.Label).ToList();
        if (string.IsNullOrWhiteSpace(entry.Owner))
            labels.Insert(0, "Verantwortung");
        if (labels.Count > 0)
            throw new ArgumentException("Für diesen Bearbeitungsstand bitte ergänzen: " + string.Join(", ", labels) + ".");
    }

    private static string Value(IReadOnlyDictionary<string, string> fields, string key) => fields.GetValueOrDefault(key, "");

    private static bool ParseMinutes(string text, out int minutes) =>
        int.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out minutes) && minutes <= 9_999_999;
}
