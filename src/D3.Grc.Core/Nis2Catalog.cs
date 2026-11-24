namespace D3.Grc.Core;

public sealed record Nis2MeasureTemplate(string Key, string Title, string Instructions, string Acceptance);

public sealed record Nis2ControlTemplate(string Key, string Title, string OwnerRole, string Implementation,
    string Evidence, int ReviewMonths, string ReviewTriggers, string RiskScenario, IReadOnlyList<Nis2MeasureTemplate> Measures);

public sealed record Nis2Package(string Key, string Reference, string Title, string Requirement,
    IReadOnlyList<Nis2ControlTemplate> Controls)
{
    public string Code => "NIS2-" + Key.ToUpperInvariant();
}

public static class Nis2Catalog
{
    public const string Topic = "NIS2";
    public const string Source = "NIS2";
    public const string OfficialSource = "https://eur-lex.europa.eu/eli/dir/2022/2555/oj";

    public static IReadOnlyList<Nis2Package> Packages { get; } =
    [
        Category("a", "Risikoanalyse und Sicherheitsrichtlinien",
            "Einrichtung und Betrieb der Informationssicherheit risikobasiert regeln. Die Bewertung berücksichtigt technische und physische Gefahren, Anforderungen, Dienstabhängigkeiten sowie die Verhältnismäßigkeit der Maßnahmen.",
            Control("RISK", "Sicherheitsrisiken systematisch bewerten", "Informationssicherheitsleitung",
                "Je kritischem Dienst Prozess, Systeme, Informationen, Standorte und externe Abhängigkeiten erfassen. Gefahren wie Ausfall, Fehlbedienung, kompromittierte Zugänge und Umwelteinflüsse bewerten. Ausgangs- und Restrisiko mit Kriterien, Datenstand und verantwortlicher Rolle festhalten. Freigabe und Eskalation oberhalb der Toleranz regeln.",
                "Freigegebene Bewertungsmethode und exportierte Stichprobe mit Bewertung, Verknüpfung zum Dienst, Behandlung und verantwortlicher Rolle. Zwei Bewertungen mit Eingaben und Ergebnis nachrechnen.", 12,
                "Neuer Dienst, erhebliche Architektur- oder Bedrohungsänderung oder relevanter Vorfall.",
                "Ein unbekannter Dienstleister oder Standort fällt aus. Ohne bewertete Abhängigkeit bleiben Ersatzmaßnahmen unvorbereitet.",
                Measure("SCOPE", "Kritische Dienste und Bewertungsgrenzen aufnehmen", "Dienstliste mit Fachbereichen abgleichen und für jeden Dienst Eigentümer, interne und externe Abhängigkeiten dokumentieren.", "Alle im Geltungsbereich liegenden kritischen Dienste sind einer verantwortlichen Rolle und einer nachvollziehbaren Bewertung zugeordnet."),
                Measure("METHOD", "Bewertungsmethode und Freigabeweg vereinbaren", "Einheitliche Skalen, Kriterien, Schwellenwerte und Eskalation definieren. Mit unterschiedlichen Diensten erproben und Unterschiede besprechen.", "Die Ergebnisse der Pilotbewertungen lassen sich aus den dokumentierten Eingaben reproduzieren. Offene Entscheidungen sind zugewiesen.")),
            Control("POLICY", "Sicherheitsrichtlinien freigeben und überwachen", "Informationssicherheitsleitung",
                "Festlegen, wer Richtlinien erstellt, prüft, freigibt und bekannt macht. Mindestregeln für den Geltungsbereich, dokumentierte Ausnahmen und ihren Ablauf pflegen. Verantwortliche bei wesentlichen Änderungen einbinden.",
                "Aktuelle veröffentlichte Richtlinie, Freigabeentscheidung, Bekanntmachung und Stichprobe befristeter Ausnahmen mit Entscheidung und nächster Prüfung.", 12,
                "Anpassung des Regelwerks, organisatorische Änderung oder neue Risikotoleranz.",
                "Teams arbeiten nach widersprüchlichen Mindestregeln. Unbefristete Ausnahmen führen zur dauerhaften Absenkung des Schutzes.",
                Measure("POLICY", "Richtlinie und Ausnahmeprozess ausarbeiten", "Verantwortung, Mindestschutz, Ausnahmeentscheidung und jährliche Überprüfung dokumentieren. Veröffentlichungsweg mit zwei Zielgruppen erproben.", "Die freigegebene Fassung ist erreichbar. Eine Testausnahme enthält Zweck, Ersatzschutz, Entscheidung und Ablaufdatum."))),

        Category("b", "Behandlung von Sicherheitsvorfällen",
            "Sicherheitsereignisse erkennen, zuverlässig aufnehmen und einordnen. Betroffene Dienste schützen, den Vorfall bearbeiten und Erkenntnisse in dauerhafte Verbesserungen überführen.",
            Control("TRIAGE", "Sicherheitsereignisse erfassen und einstufen", "Incident-Response-Leitung",
                "Verlässliche Meldestelle und Vertretung definieren. Vergleichbare Zeitstempel für Eingang, Erstbewertung und Eskalation erfassen. Schwere anhand Dienst-, Integritäts-, Vertraulichkeits- und Datenfolgen einschätzen. Relevante Beobachtungen und Entscheidungsgrundlage zugriffsbeschränkt dokumentieren.",
                "Freigegebener Meldeweg, aktuelle Kontakt- und Vertretungsliste sowie zwei anonymisierte Fälle mit Empfang, Einstufung, Zeitstempeln und Übergabe.", 6,
                "Wesentlicher Vorfall, Änderung von Dienst, Monitoring oder Bereitschaft.",
                "Warnungen bleiben in einem unbetreuten Kanal liegen. Die verspätete Einordnung erhöht die Auswirkung auf Dienste.",
                Measure("CHANNEL", "Eingang, Bereitschaft und Eskalation testen", "Kanäle und Erreichbarkeit festlegen. Einen synthetischen Vorfall außerhalb der Kernzeit einspeisen und Übergabe sowie Rollenrückmeldung dokumentieren.", "Im Test sind Empfang und fachliche Eskalation mit Uhrzeit und verantwortlicher Rolle nachvollziehbar.")),
            Control("RESPONSE", "Vorfallsreaktion und Wiederherstellung dokumentieren", "Incident-Response-Leitung",
                "Bearbeitungsschritte, Zuständigkeiten und Freigaben für Eindämmung, Bereinigung und Wiederherstellung definieren. Veränderungen und gesicherte Belege protokollieren. Ein Abschlussreview überträgt Ursachen und Verbesserungen in Maßnahmen mit Termin und Wirksamkeitskriterium.",
                "Reaktionsablauf, Zeitlinie einer Übung, genehmigte Wiederherstellung und Protokoll der abgeleiteten Maßnahmen.", 6,
                "Nach schwerwiegendem Vorfall, Übung oder Änderung kritischer Systeme.",
                "Ein Service wird ohne Prüfung der zugrunde liegenden Störung freigegeben. Der Vorfall tritt erneut auf und Befunde gehen verloren.",
                Measure("EXERCISE", "Reaktionsablauf mit synthetischem Vorfall üben", "Ausgangslage, Rollen und Schutzgrenzen vorab festhalten. Übergaben, Wiederherstellungsfreigabe und Lessons Learned beobachten.", "Geprüfte Schritte, Abweichungen und Folgeaufgaben sind mit Verantwortlichen und Erfolgskriterien dokumentiert."))),

        Category("c", "Geschäftskontinuität und Krisenmanagement",
            "Die Fortführung und Wiederaufnahme wichtiger Dienste planen. Datensicherungen, Wiederherstellung und Krisenentscheidungen auf ermittelte Betriebsziele ausrichten und erproben.",
            Control("RECOVERY", "Sicherungen und Wiederherstellung erproben", "IT-Betrieb",
                "Sicherungsumfang je Dienst und Datenklasse festlegen. Sicherungen und Zugriffe so trennen, dass eine Störung der Produktionsumgebung Wiederherstellungsdaten nicht gleichzeitig unbrauchbar macht. Wiederherstellung in isolierter Umgebung inklusive Integritäts- und Funktionsprüfung üben. Ist-Zeit und Alter des wiederhergestellten Datenstands mit Zielwerten vergleichen.",
                "Sicherungsinventar, nachweisbare Überwachung und signiertes oder genehmigtes Wiederherstellungsprotokoll mit Dienst, Datenstand, Messwerten und Abweichungen.", 6,
                "Wechsel der Sicherungstechnik, Dienstmigration, fehlgeschlagener Restore oder geänderte Ziele.",
                "Sicherungen sind vorhanden, der Wiederanlauf scheitert jedoch an fehlenden Abhängigkeiten oder zu alten Datenständen.",
                Measure("RESTORE", "Wiederherstellung eines kritischen Dienstes durchführen", "Geplanten Datenstand auswählen, in abgeschotteter Testumgebung wiederherstellen und vereinbarte Kernfunktionen prüfen. Beginn, Dauer und verwendeten Datenstand festhalten.", "Hardware, Identitäten und externe Abhängigkeiten sind in der Dokumentation berücksichtigt. Zeit und Datenverlust sind gegen RTO und RPO bewertet.")),
            Control("CRISIS", "Krisen- und Notbetrieb organisieren", "Krisenkoordination",
                "Kritische Prozesse mit Wiederanlaufprioritäten, Toleranzen und Ersatzverfahren verbinden. Aktivierung, Stellvertretung und Entscheidungsrechte festhalten. Kontaktdaten und Kommunikationswege unabhängig von den betroffenen Systemen erreichbar halten. Übungen mit messbaren Zielen nachbereiten.",
                "Aktuelle Auswirkungsanalyse, freigegebener Ablaufplan und Messprotokoll einer Übung mit offenen Aufgaben und Fristen.", 12,
                "Organisationswechsel, wesentliche Prozessänderung oder Übungsabweichung.",
                "Bei einem größeren Ausfall fehlen Ausweichprozesse und Entscheidungsrechte. Parallelteams machen widersprüchliche Zusagen.",
                Measure("TABLETOP", "Notbetrieb und Krisenentscheidung als Planspiel prüfen", "Einen Ausfall mit nicht erreichbarer Schlüsselperson simulieren. Aktivierung, Stellvertretung, Ersatzkanal und Freigabe für Regelbetrieb durchspielen.", "Entscheidungen und Zeitpunkte sind dokumentiert. Feststellungen führen zu Maßnahmen und einer geplanten Wiederholungsprüfung."))),

        Category("d", "Sicherheit der unmittelbaren Lieferkette",
            "Sicherheitsrelevante Beziehungen zu direkten Anbietern und Dienstleistern risikobasiert steuern. Anbieterspezifische Schwächen, Qualität von Produkten und Verfahren der sicheren Entwicklung sowie einschlägige koordinierte Lieferkettenbewertungen berücksichtigen.",
            Control("VENDOR", "Sicherheitsrisiken direkter Dienstleister bewerten", "Beschaffung und Informationssicherheit",
                "Pro kritischer Leistung Anbieter, verantwortliche Rolle, Datenzugriff und Abhängigkeiten erfassen. Sicherheitspraktiken, bekannte Einschränkungen, unterstützte Produktstände und Nachweise zur Entwicklung bewerten. Risiken oder fehlende Auskünfte mit Freigabe, Ersatzschutz und Folgetermin behandeln.",
                "Bewertetes Lieferantenregister und Stichprobe vor Beauftragung oder Verlängerung mit Datenbasis, Ergebnis, Restrisiko und Entscheidung.", 12,
                "Neuer Anbieter, relevante Leistungsänderung, Vorfall beim Anbieter oder neue koordinierte Risikobewertung.",
                "Ein direkter Dienstleister verweigert Informationen zur Absicherung kritischer Schnittstellen. Die Einrichtung erkennt das Konzentrationsrisiko zu spät.",
                Measure("INVENTORY", "Kritische Anbieter und Leistungsabhängigkeiten prüfen", "Vertrags- und Einkaufslisten mit den als kritisch eingestuften Diensten verbinden. Abhängigkeiten und abgefragte Nachweise je Anbieter dokumentieren.", "Jede Stichprobe enthält eindeutige Leistung, Betreiberrolle, Risiken und Entscheidung zum verbleibenden Handlungsbedarf.")),
            Control("TERMS", "Sicherheitsanforderungen in Verträgen und Übergängen nachhalten", "Vertrags- und Dienstleistermanagement",
                "Anforderungen an Vorfallinformation, Aktualisierungen, Nachweiszugang und Wiederherstellung aus der Risikobewertung ableiten. Vertragsänderungen, Laufzeiten und Ausstiegspfad überwachen. Verantwortliche bei Abweichungen rechtzeitig vor einer Verlängerungsentscheidung informieren.",
                "Anforderungsliste mit Vertragsfundstellen, aktuelle Notfallkontakte sowie Stichprobe zu Frist, Nachweis und alternativer Leistungserbringung.", 12,
                "Verlängerung, erhebliche Abweichung, Substitutionsbedarf oder Anbieterwechsel.",
                "Bei einem Ausfall gibt es keine vereinbarte Übergabe oder rechtzeitige Information. Die Abhängigkeit verlängert die Betriebsunterbrechung.",
                Measure("EXIT", "Sicherheitsklauseln und Ausstiegsfähigkeit abgleichen", "Für priorisierte Verträge die Sicherheitsanforderungen, Meldeschnittstelle, Datenübergabe und Exit-Fristen nachweisen. Abweichungen samt Verantwortlichen dokumentieren.", "Die größte verbleibende Vertragslücke hat eine begründete Behandlung. Der Ersatzpfad ist zeitlich nachvollziehbar."))),

        Category("e", "Sichere Beschaffung, Entwicklung und Wartung",
            "Sicherheitsanforderungen beim Kauf, bei der Entwicklung und im laufenden Betrieb berücksichtigen. Einen nachvollziehbaren Umgang mit Schwachstellen einschließlich verantworteter Offenlegung etablieren.",
            Control("LIFECYCLE", "Sicherheitsanforderungen vor Auswahl und Freigabe prüfen", "Produkt- und Serviceverantwortung",
                "Sicherheitskriterien vor Anschaffung oder wesentlicher Änderung definieren. Sicherheitskritische Funktionen und Datenflüsse im Design bewerten. Anforderungen und Prüfergebnisse bis zur Freigabe verfolgen. Ausnahmen mit Risiko, Frist und Ersatzmaßnahmen freigeben lassen.",
                "Stichprobe aus Beschaffung und Änderung mit Sicherheitskriterien, Testbefund, Zuständigkeit und Freigabeentscheidung.", 12,
                "Wesentliche Änderung, neue Plattform oder Änderung des Beschaffungsprozesses.",
                "Ein System wird freigegeben, bevor Zugriffs- oder Protokollanforderungen geprüft wurden. Fehler bleiben bis zum Vorfall unerkannt.",
                Measure("GATE", "Sicherheitsprüfung als Freigabeschritt verankern", "Für aktuelle Projekte Pflichtkriterien und Prüfergebnisfelder in die Freigabe aufnehmen. Fälle mit genehmigter Abweichung nachvollziehbar markieren.", "In einer Stichprobe ist jede relevante Anforderung mit einem Prüfergebnis oder einer befristeten Entscheidung versehen.")),
            Control("VULN", "Schwachstellen entgegennehmen und nach Priorität beheben", "Schwachstellenmanagement",
                "Quellen für Herstellerhinweise und Meldungen festlegen. Betroffenheit, Kritikalität und Abhilfefrist bewerten. Abhilfe in freigegebener Umgebung prüfen, einspielen und die Beseitigung bestätigen. Externe Meldewege, verantwortete Kommunikation und vertrauliche Behandlung definieren.",
                "Aktuelle Meldeinformation und anonymisierte Stichprobe vom Eingang bis zum bestätigten Abschluss, einschließlich Ersatzmaßnahmen für Rückstände.", 3,
                "Schwerwiegender Befund, überfällige Behandlung, Plattformänderung oder geänderter Meldeweg.",
                "Hinweise auf eine betroffene Komponente bleiben ohne Eigentümer. Die Behandlung verzögert sich trotz verfügbarer Abhilfe.",
                Measure("WORKFLOW", "Schwachstellenprozess mit Testmeldung überprüfen", "Synthetische Meldung erfassen und Betroffenheit, Frist, verantwortliche Rolle, Änderung und erneute Prüfung nachvollziehen.", "Die Testmeldung ist bis zur Rückmeldung dokumentiert. Überfällige Fälle gelangen in einen geprüften Eskalationspfad."))),

        Category("f", "Wirksamkeit der Sicherheitsmaßnahmen",
            "Mit wiederholbaren Verfahren prüfen, ob die Sicherheitsmaßnahmen ihr Ziel im vereinbarten Geltungsbereich erreichen. Testergebnisse, Einschränkungen und Korrekturen dokumentieren.",
            Control("TEST", "Kontrolltests anhand von Kriterien planen", "Prüfverantwortung",
                "Je priorisiertem Control Ziel, Prüfmethode, Nachweisart, Stichprobe und Frequenz definieren. Durchführung und Beurteilung angemessen trennen. Vor dem Test Akzeptanzkriterien und den verwendeten Versionsstand festhalten.",
                "Kontrolltestplan und abgeschlossene Stichprobe mit Kriterium, tatsächlicher Beobachtung, Ergebnis, Zeitstempel und verfügbaren Nachweisen.", 12,
                "Geändertes Kontrollziel, Vorfall oder Prüfungsabweichung.",
                "Nur die Existenz einer Richtlinie wird als Erfolg gewertet. Defizite im laufenden Betrieb bleiben unentdeckt.",
                Measure("COVERAGE", "Prüfplan für priorisierte Controls aufstellen", "Kontrollen aus kritischen Diensten auswählen. Kriterien, zulässige Nachweise und Auswertungsverfahren in die Planung übernehmen.", "Zu jedem Pilot-Control ist eine Prüfung mit eindeutiger Zuständigkeit und nachvollziehbarer Soll-Ist-Auswertung vorgesehen.")),
            Control("FOLLOWUP", "Abweichungen nachverfolgen und erneut prüfen", "Maßnahmenkoordination",
                "Abweichungen nach Auswirkung priorisieren. Maßnahmen mit Verantwortlichen, Zieltermin und beobachtbarem Abschlusskriterium nachhalten. Abschluss nach erneuter Prüfung oder dokumentierter Entscheidung zur Risikoübernahme erfassen.",
                "Auszug offener und geschlossener Feststellungen mit Maßnahmenbezug. Für abgeschlossene Korrekturen das Wiederholungsprüfergebnis oder die genehmigte Entscheidung prüfen.", 6,
                "Überfällige Korrektur, erfolglose Wiederholungsprüfung oder wesentliche Änderung.",
                "Ein Befund wird mit Tätigkeitsmeldung geschlossen. Der erneute Test zeigt, dass die Schwäche weiterhin besteht.",
                Measure("RETEST", "Wiederholungsprüfung im Übergabeprozess testen", "Eine synthetische Abweichung mit Korrektur und Wiederholungsprüfung durchspielen. Terminüberschreitungen und Entscheidung zur Eskalation dokumentieren.", "Der Abschluss ist eindeutig an beobachtete Wirksamkeit oder an eine begründete Risikoentscheidung gebunden."))),

        Category("g", "Cyberhygiene und rollenbezogene Schulung",
            "Verbindliche Mindestpraktiken für sichere Arbeit und wiederkehrende, zielgruppengerechte Schulung etablieren. Durchführung und Lerneffekt anhand angemessener Nachweise verfolgen.",
            Control("BASELINE", "Grundschutz für alltägliche Arbeit umsetzen", "IT-Betrieb und Informationssicherheit",
                "Zielwerte für unterstützte Softwarestände, Aktualisierungen, sichere Gerätebasis, Bildschirm- und Kontoschutz sowie Meldung verdächtiger Ereignisse definieren. Einhaltung aus verlässlichen Systemdaten prüfen und Abweichungen priorisiert behandeln.",
                "Freigegebene Basisvorgaben und Stichprobe aus überwachten Geräten oder Diensten mit Sollwert, Istwert sowie verantworteter Ausnahme.", 6,
                "Änderung am Gerätestandard, neue Bedrohung oder wiederkehrende Abweichung.",
                "Veraltete Geräte und unsichere Einstellungen bleiben im Betrieb. Ein einzelner Fehler erreicht dadurch viele Konten und Systeme.",
                Measure("CHECK", "Mindestschutz mit realer Bestandsstichprobe abgleichen", "Freigegebene Anforderungen auf repräsentative Geräte oder Dienste anwenden. Abweichung, Ursache und Korrekturfrist zusammen dokumentieren.", "Stichprobe, Datenquelle und Verantwortliche sind eindeutig. Abweichungen sind einem Korrekturauftrag oder einer befristeten Ausnahme zugeordnet.")),
            Control("TRAIN", "Sicherheitskompetenz zielgruppengerecht entwickeln", "Personalentwicklung und Informationssicherheit",
                "Einführung für neue Mitarbeitende und regelmäßige Auffrischung planen. Aufgabenbezogene Inhalte für Administration, Entwicklung, Vorfallbearbeitung und Leitung vorsehen. Lernerfolg oder Transfer durch kurze Übungen erfassen und bei Lücken nachsteuern.",
                "Rollenbezogener Schulungsplan, anonymisierte Teilnahme- und Auswertungsdaten sowie dokumentierte Nachhol- oder Verbesserungsmaßnahmen.", 12,
                "Neue Rollen, geänderte Risiken, wiederkehrende Bedienfehler oder relevante Vorfälle.",
                "Personal erkennt Warnzeichen oder Meldestellen nicht. Routinetätigkeiten führen zu unbeabsichtigten Sicherheitsereignissen.",
                Measure("PLAN", "Schulungsplan mit Lernzielen und Nachholweg einrichten", "Zielgruppen, Inhalte, Rhythmus und ein messbares Lernziel pro Rolle festlegen. Vertrauliche Auswertungsregeln vereinbaren.", "Eine Piloterhebung weist Teilnahme und Kenntnis des Meldewegs nach. Fehlende Teilnahme löst einen definierten Nachholschritt aus."))),

        Category("h", "Kryptografie und Verschlüsselung",
            "Den Einsatz kryptografischer Verfahren für relevante Informationen, Systeme und Übertragungen risikobasiert steuern. Auswahl, Schlüsselbetrieb, Erneuerung und Ausnahmen kontrollieren.",
            Control("CRYPTO", "Kryptografische Verfahren und Einsatzpunkte festlegen", "Sicherheitsarchitektur",
                "Datenklassen und Übertragungswege auf erforderlichen Schutz prüfen. Akzeptierte Verfahren, Einsatzzweck, Konfigurationsanforderungen und Ausnahmen festhalten. Bei technischen Änderungen prüfen, ob Altverfahren oder ungeschützte Pfade bestehen bleiben.",
                "Freigegebene Kryptografievorgabe und Konfigurationsstichprobe für Speicherung und Übertragung mit Datenklasse, Verfahren und Ausnahmedokumentation.", 12,
                "Änderung der Kryptovorgaben, Migration oder neue personenbezogene beziehungsweise vertrauliche Datenflüsse.",
                "Ein schutzbedürftiger Datenfluss bleibt nach einem Plattformwechsel unverschlüsselt. Unbefugte Einsicht ist leichter möglich.",
                Measure("FLOWS", "Schutzbedarf und Konfiguration exemplarisch vergleichen", "Vertrauliche Datenflüsse und Ablageorte auswählen. Freigegebene Anforderungen mit tatsächlicher Konfiguration und wirksamer Ausnahmeentscheidung abgleichen.", "Jede Stichprobe weist Schutzklasse, System, Prüfergebnis und gegebenenfalls verbindliche Korrektur nach.")),
            Control("KEYS", "Schlüssel und Zertifikate kontrolliert verwalten", "Schlüsselverantwortung",
                "Schlüssel und Zertifikate mit Zweck, Systembezug, Ablauf und Zuständigkeit inventarisieren. Berechtigungen für Bereitstellung und Widerruf begrenzen. Erneuerung, sicherer Ersatz, Löschung und Wiederherstellung mit prüfbaren Abläufen planen.",
                "Bereinigter Inventarauszug ohne geheime Schlüssel, Stichprobe zu Erneuerung und Entzug sowie dokumentierter Ausfall- oder Ersatztest.", 6,
                "Schlüsselwechsel, Verdacht auf Offenlegung oder Ablösung eines Hauptsystems.",
                "Ein Zertifikat läuft ohne rechtzeitige Vertretung ab. Ein kritischer Dienst verliert die Erreichbarkeit.",
                Measure("ROTATE", "Erneuerung und Verantwortlichkeit für kritische Zertifikate prüfen", "Ablaufüberwachung und Vertretung im Inventar hinterlegen. Kontrollierte Erneuerung im Test durchführen und Rückfallentscheidung dokumentieren.", "Ein bevorstehender Ablauf erzeugt eine nachverfolgbare Aufgabe. Geprüfte Erneuerung verursacht keinen ungeplanten Verbindungsabbruch."))),

        Category("i", "Personalsicherheit, Zugriffssteuerung und Inventar",
            "Sicherheitsverantwortung im Beschäftigungsverhältnis festlegen. Zugriffe passend zur Aufgabe vergeben, ändern und entziehen. Informationswerte und Betriebsmittel mit eindeutigen Verantwortlichkeiten führen.",
            Control("ACCESS", "Eintritt, Wechsel und Austritt mit Zugriffskontrolle verbinden", "Personal und Berechtigungsmanagement",
                "Kritische Rollen, Least-Privilege-Grundsätze und Freigaben regeln. Eintritt, Rollenwechsel und Austritt mit Personaldatenabgleich und dokumentierten Zeitpunkten bearbeiten. Zutritts-, Dienst- und Administrationszugänge einschließlich befristeter Ausnahmen regelmäßig überprüfen.",
                "Berechtigungsrichtlinie und anonymisierte Stichproben aus Eintritt, Wechsel und Austritt mit genehmigter Vergabe oder bestätigtem Entzug.", 6,
                "Rollenwechsel, Austritt, erhöhte Berechtigungen oder geänderte Verantwortlichkeit.",
                "Ein ehemaliges Teammitglied behält Zugriff auf Anwendungen oder Standorte. Schützenswerte Informationen bleiben unbefugt erreichbar.",
                Measure("JML", "Zugriffsänderungen und Entzug mit Stichprobe bestätigen", "Abgleichen, welche Systeme und Zutritte bei Organisationswechsel oder Austritt betroffen sind. Zeitpunkte und Berechtigungszustand anhand verlässlicher Quellen prüfen.", "Jede Änderung ist genehmigt. Entzug und Zuordnung zu betroffenen Systemen sind belegt. Ausnahmen haben Termin und Verantwortliche.")),
            Control("ASSETS", "Informationswerte und Betriebsmittel im Bestand führen", "Asset-Verantwortung",
                "Betriebsmittel, Systeme, Informationsbestände und zugehörige Dienste mit Zuständigkeit, Schutzklasse und Lebenszykluszustand erfassen. Inventar mit geeigneten Bestandsquellen abgleichen. Aufnahme, Außerbetriebnahme und sichere Rückgabe oder Löschung dokumentieren.",
                "Datierter Inventarauszug und abgeglichene Stichprobe mit Besitz, Schutzklasse, Dienstbezug und Status. Ausgesonderte Ausstattung mit Rückgabe- oder Löschbeleg.", 6,
                "Neue Plattform, Reorganisation, Standortwechsel oder erhebliche Inventardifferenzen.",
                "Ein veraltetes System ohne verantwortliche Rolle bleibt aktiv. Schutzmaßnahmen und Wartung erreichen es nicht.",
                Measure("RECONCILE", "Inventar mit Betriebsdaten abstimmen", "Eine vom Register unabhängige Bestandsquelle auswählen. Unbekannte, doppelte und ausgemusterte Objekte kennzeichnen und zuweisen.", "Differenzen sind erläutert und verfolgt. Kritische Objekte haben eine benannte Zuständigkeit und einen aktuellen Lebenszyklusstatus."))),

        Category("j", "Starke Authentifizierung und geschützte Kommunikation",
            "Nach festgestelltem Schutzbedarf geeignete starke Authentifizierung und gesicherte Sprach-, Video- und Textkommunikation einsetzen. Einen geschützten internen Notfallkanal vorsehen, soweit für die Einrichtung angemessen.",
            Control("AUTH", "Starke Authentifizierung für risikoreiche Zugriffe durchsetzen", "Identitäts- und Zugangsbetrieb",
                "Administrative, externe und besonders sensible Zugriffe priorisieren. Geeignete Mehrfaktor- oder kontinuierliche Authentifizierung auswählen und in wiederholbaren Tests prüfen. Wiederherstellung von Konten und technische Ausnahmen so begrenzen, dass der Schutz erhalten bleibt.",
                "Datierter Konfigurations- und Abdeckungsnachweis mit Stichprobe zulässiger und abgewiesener Anmeldungen. Ausnahmen mit Frist und Ersatzschutz.", 6,
                "Wechsel des Identitätsdienstes, neue extern erreichbare Anwendung oder geänderter Anmeldeprozess.",
                "Ein gestohlenes Passwort genügt zur Anmeldung an einem administrativen Zugang. Betroffene Dienste sind dadurch gefährdet.",
                Measure("ENFORCE", "Abdeckung starker Anmeldung prüfen", "Priorisierte Systeme und Anmeldepfade zusammentragen. Durchsetzung pro Pfad und den vorgesehenen Wiederherstellungsprozess im Test kontrollieren.", "Alle priorisierten Pfade sind wirksam geschützt oder haben eine genehmigte, befristete Ausnahme mit Ersatzschutz.")),
            Control("COMMS", "Geschützte Regel- und Notfallkommunikation bereitstellen", "IT-Betrieb und Krisenkoordination",
                "Schutzanforderungen für Sprache, Video und Text in sensiblen Situationen bestimmen und geeignete Kommunikationsmittel freigeben. Für Krisen einen getrennt erreichbaren internen Kanal mit Zuständigkeit, Kontaktpflege und Schutz vor unberechtigtem Mitlesen testen.",
                "Kommunikationsvorgabe, Übersicht freigegebener Kanäle mit Schutzmerkmalen und Übungsprotokoll zur Erreichbarkeit des Notfallkanals.", 12,
                "Wechsel eines Kanals, Änderung der Krisenorganisation oder fehlgeschlagene Erreichbarkeitsprüfung.",
                "Der Ausfall betrifft gleichzeitig die übliche Kommunikation. Teams greifen auf ungeprüfte Kanäle für vertrauliche Inhalte zurück.",
                Measure("CHANNEL", "Ersatzkommunikation bei Ausfall des Hauptkanals üben", "Einen kontrollierten Ausfall annehmen und die Erreichbarkeit der Krisenrollen über den Ersatzkanal prüfen. Daten, die dort zulässig sind, vorher festlegen.", "Identität, Erreichbarkeit und zulässiger Informationsaustausch sind im Übungsbericht nachweisbar."))),

        new("23", "Art. 23 Abs. 1–4", "Meldung erheblicher Sicherheitsvorfälle",
            "Erhebliche Vorfälle anhand der dienstbezogenen Auswirkungen einstufen und der zuständigen Stelle fristgerecht melden. Die gestufte Kommunikation, Meldungsinhalte und Nachweise im jeweiligen Rechtsraum vorbereiten.",
            [
                Control("ASSESS", "Erheblichkeit und zuständigen Meldeweg bestimmen", "Incident-Response- und Meldeverantwortung",
                    "Schwere Beeinträchtigungen der Dienste, finanzielle Verluste der Einrichtung und beträchtliche materielle oder immaterielle Schäden bei anderen als Prüfkriterien erfassen. Tatsächliche und mögliche Folgen beurteilen. Zuständige Stelle, Kenntniszeitpunkt, Entscheidung und Vertretung dokumentieren. Prüfen, ob betroffene Dienstempfänger unverzüglich über den Vorfall oder über Schutzmaßnahmen gegen eine erhebliche Bedrohung informiert werden müssen. Einschlägige nationale und sektorspezifische Regeln in der eigenen Entscheidung hinterlegen.",
                    "Aktuelle Entscheidungsgrundlage mit Fundstellen und anonymisierte Beispieldokumentation mit Kenntniszeitpunkt, erheblicher Auswirkung, dokumentiertem Meldeweg und betroffenen Empfängern.", 6,
                    "Neue Meldepflicht, Änderung des Dienstes, neue nationale Zuständigkeit oder erheblicher Vorfall.",
                    "Ein potenziell erheblicher Vorfall wird wegen unklarer Rollen und unvollständiger Auswirkungen verspätet erkannt.",
                    Measure("DECISION", "Meldeentscheidung für zwei synthetische Fälle üben", "Je einen Fall mit und ohne erhebliche Folgen anhand derselben Kriterien beurteilen. Zuständige Stelle und Meldeschnittstelle recherchieren und dokumentieren.", "Entscheidungsgrund, Zeitbezug und zuständige Rolle sind für beide Fälle nachvollziehbar. Der Zugang zum Meldeweg ist für die Vertretung geprüft.")),
                Control("REPORT", "Gestufte Vorfallmeldungen steuern", "Meldeverantwortung",
                    "Nach Kenntnis eines erheblichen Vorfalls Frühwarnung unverzüglich und spätestens nach 24 Stunden sowie Vorfallmeldung unverzüglich und spätestens nach 72 Stunden organisieren. Für erhebliche Vorfälle mit Auswirkung auf die Erbringung von Vertrauensdiensten gilt für deren Anbieter die abweichende 24-Stunden-Frist der Vorfallmeldung. Zwischenbericht auf Anforderung vorbereiten. Abschlussbericht spätestens einen Monat nach der Vorfallmeldung einreichen. Dauert der Vorfall dann noch an, zunächst den Fortschritt und spätestens einen Monat nach Abschluss der Bearbeitung den Abschluss berichten. Eingangsbestätigung, Zeitlinie, Schwere, Auswirkungen, Ursache und Gegenmaßnahmen ablegen.",
                    "Ausgefüllte Übungsunterlagen, dokumentierte Fristberechnung, nachgewiesene Rollenerreichbarkeit und Status von Formulierung, Freigabe und bestätigter Übermittlung.", 6,
                    "Vorfall oder Übung mit Fristabweichung, neue Anforderung oder Änderung der Meldewege.",
                    "Der Meldezeitpunkt wird mit Aufklärung statt Kenntnis verknüpft. Eine notwendige Meldestufe bleibt dadurch aus oder kommt zu spät.",
                    Measure("TIMELINE", "Frist-, Freigabe- und Übermittlungsablauf simulieren", "Synthetischen Kenntniszeitpunkt setzen und Frühwarnung, Vorfallmeldung, Zwischenstand und Abschlussprobe in einem Zeitstrahl mit Rollen und Nachweisen dokumentieren.", "Fristen sind aus dem richtigen Bezugszeitpunkt abgeleitet. Die Übung zeigt Ersatzperson, Zuständigkeit und Aufnahme der Bestätigung."))
            ])
    ];

    public static Nis2Package Get(string key) => Packages.FirstOrDefault(package => package.Key == key)
        ?? throw new ArgumentException("Diese NIS2-Anforderungsgruppe ist nicht verfügbar.", nameof(key));

    private static Nis2Package Category(string letter, string title, string purpose, params Nis2ControlTemplate[] controls) =>
        new("21" + letter, "Art. 21 Abs. 2 Buchst. " + letter, title, purpose, controls);

    private static Nis2ControlTemplate Control(string key, string title, string owner, string implementation,
        string evidence, int reviewMonths, string triggers, string risk, params Nis2MeasureTemplate[] measures) =>
        new(key, title, owner, implementation, evidence, reviewMonths, triggers, risk, measures);

    private static Nis2MeasureTemplate Measure(string key, string title, string instructions, string acceptance) =>
        new(key, title, instructions, acceptance);
}
