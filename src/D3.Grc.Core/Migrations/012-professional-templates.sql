-- Original editable work documents. Explicit topic assignments apply to these new templates only.
CREATE TABLE IF NOT EXISTS DesktopTemplateTopics (
    TemplateId TEXT NOT NULL REFERENCES Templates(Id) ON DELETE CASCADE,
    TopicId TEXT NOT NULL REFERENCES Topics(Id) ON DELETE CASCADE,
    PRIMARY KEY (TemplateId, TopicId)
);

INSERT INTO Templates (Id, Kind, Title, SourceId, Locator, Body, Revision) VALUES
('D3-PRO-PRIVACY-INC', 'Procedure', 'Datenschutzverletzung beurteilen und bearbeiten', 'GDPR',
 'Art. 33 und 34 DSGVO; ergänzende örtliche und vertragliche Anforderungen fallbezogen prüfen',
 '## Zweck, Geltung und Dokumentstand

Dieses Verfahren gilt für vermutete Verletzungen personenbezogener Daten in [Organisation und Geltungsbereich]. Es führt von der ersten Meldung über Schutzmaßnahmen und die getrennten Entscheidungen zu Aufsicht und Betroffenen bis zur kontrollierten Nachbereitung. Verantwortlich ist [Datenschutzkoordination], freigegeben durch [zuständige Rolle] ab [Datum].

## Einsatzbereitschaft

| Rolle | Aufgabe | Stellvertretung oder Kontakt |
| --- | --- | --- |
| Eingangsstelle | Meldungen annehmen und Eingang bestätigen | [Rufbereitschaft und Ersatzkanal] |
| Vorfallleitung | Sachverhalt, Eindämmung und Ereigniszeitlinie koordinieren | [IT- oder Fachbereitschaft] |
| Datenschutzkoordination | Risikoanalyse und Entscheidungen vorbereiten | [interner oder externer Kontakt] |
| Entscheidungsrolle | Kommunikation und weitere Maßnahmen freigeben | [Leitung und Vertretung] |

## Vorgehen im Fall

1. Fall-ID vergeben. Zeitpunkt und Zeitzone der internen Meldung, Zeitpunkt der Kenntnis für die datenschutzrechtliche Bewertung, Quelle und aktuell bekannte Fakten getrennt dokumentieren.
2. Datenflüsse eingrenzen. Betroffene Kategorien, Personengruppen, mögliche Empfänger, Umfang und Wiederherstellbarkeit aus geeigneten Quellen ermitteln. Vorläufige Schätzungen als solche markieren.
3. Schutzmaßnahmen parallel veranlassen. Fehlgeleitete Freigaben beenden, Zugriffe begrenzen und Nachweise zugriffsgeschützt sichern. Verwenden Sie Fallnummern und erforderliche Ausschnitte.
4. Folgen für Personen anhand Datenart, Identifizierbarkeit, Wahrscheinlichkeit eines Missbrauchs und wirksamer Schutzmaßnahmen bewerten. Die zuständige Stelle bestimmt die einschlägigen Fristen und entscheidet getrennt über Aufsichtsmeldung und Information Betroffener.
5. Für jede externe Information Zeitpunkt, Empfänger, verantwortliche Rolle und Begründung festhalten. Bei laufender Sachverhaltsklärung Versionsstand und nächste Neubewertung dokumentieren.

## Entscheidungsblatt

| Entscheidung | Begründung und Nachweis | Verantwortlich und fällig |
| --- | --- | --- |
| Risiko für Rechte und Freiheiten | [Szenarien, Folgen, Schutzwirkung und Restunsicherheit] | [Rolle und Zeitpunkt] |
| Meldung an die zuständige Aufsicht | [Entscheidung, Rechtsbezug und Übermittlungsbeleg oder Gründe] | [Rolle und Frist] |
| Information betroffener Personen | [Entscheidung, geprüfte Ausnahmen und Nachrichtenversion] | [Rolle und Frist] |

## Beispiel und Abschluss

Ein versehentlich freigegebener Personalordner wird gesperrt. Die Vorfallleitung prüft Freigabeumfang und verfügbare Zugriffsereignisse. Datenschutz bewertet unter anderem Empfängerkreis und Inhalt. Fehlende Protokolle werden in der Risikoentscheidung als Unsicherheit berücksichtigt.

Den Fall erst abschließen, wenn Entscheidungen und Schutzmaßnahmen belegt, Folgemaßnahmen mit Frist zugeordnet und die Aufbewahrungsregel vermerkt sind. Den Meldeweg und die Erreichbarkeit nach einer wesentlichen Änderung und im vereinbarten Übungsintervall prüfen.', 1),

('D3-PRO-PRIVACY-DSFA', 'Guidance', 'Datenschutz-Folgenabschätzung mit Entscheidungsvorlage', 'GDPR',
 'Art. 35 und 36 DSGVO; einschlägige Listen und Leitlinien der zuständigen Aufsichtsbehörde prüfen',
 '## Auftrag und Geltungsbereich

Die Folgenabschätzung behandelt [Verarbeitung, Gesellschaft und Systeme] in der Fassung [Versionsstand]. Der Fachbereich beschreibt Zweck, Personengruppen, Datenwege, Speicherdauer, Dienstleister und tatsächliche Entscheidungen. Er nennt den Anlass der Prüfung, etwa die Einführung eines neuen Verfahrens oder eine wesentliche Änderung der Datenverarbeitung.

## Rollen und Freigabe

Die Prozessverantwortung liefert die Sachverhaltsbeschreibung. Datenschutz prüft das Verfahren und dokumentiert seine Stellungnahme. Informationssicherheit validiert die Wirkung der technischen Maßnahmen. Die zuständige Stelle entscheidet über Änderungen und mögliche Inbetriebnahme. Stellungnahme und Entscheidung werden getrennt festgehalten.

## Bewertungsverfahren

1. Anlass und Entscheidung über das Erfordernis protokollieren. Verwendete Kriterien, aktuelle Fundstelle und offene Fragen dokumentieren.
2. Zweck und Mittel je Verarbeitungsschritt beschreiben. Notwendigkeit, Verhältnismäßigkeit, datensparsame Alternativen und den Zugang zu Betroffenenrechten anhand konkreter Szenarien beurteilen.
3. Für jede Personengruppe mögliche Nachteile benennen. Beispielsweise kann ein unzutreffender Treffer eine Leistung verzögern oder Zugang zu Informationen unberechtigt erweitern.
4. Risikoreduktion mit konkreter Funktionsweise und Prüfmethode beschreiben. Aus der Beobachtung eines Beispiels keine Aussage über alle Fälle ableiten. Verbleibende Ungewissheit dokumentieren.
5. Restbewertung und Entscheidung mit Begründung festhalten. Falls weiterhin ein hohes Risiko besteht, prüft die zuständige Stelle das Verfahren der vorherigen Konsultation und dokumentiert die nächsten Schritte.

## Risikotabelle

| Szenario | Folgen für Personen | Maßnahme und Wirksamkeitsnachweis | Restrisiko und Beschluss |
| --- | --- | --- | --- |
| Falsch zugeordnete Akte | Einsicht und falsche Kontaktaufnahme | Identitätsabgleich und Stichprobe zu ähnlichen Datensätzen | [Bewertung und Entscheidung] |
| Zu lange gespeicherter Export | Weitergabe über einen überholten Dateibestand | Befristete Ablage und Test des Löschereignisses | [Bewertung und Entscheidung] |

## Freigabeakte

Verarbeitungsbeschreibung, Datenfluss, Methodenstand, Stellungnahmen, Entscheidung, Testnachweise und befristete Auflagen versioniert referenzieren. Die Folgenabschätzung bei einem neuen Zweck, Datenumfang, System, Empfängerkreis oder einer veränderten Risikolage erneut prüfen. [Verantwortung und Wiedervorlage] im Register hinterlegen.', 1),

('D3-PRO-ISMS-ACCESS', 'Procedure', 'Berechtigungen und privilegierte Zugriffe regelmäßig überprüfen', 'ISO27001',
 'ISO/IEC 27001:2022, eigener ISMS-Rahmen und ausgewählte Kontrollen für Zugriffsrechte',
 '## Zweck und Laufzeit

Dieses Verfahren regelt die wiederkehrende Prüfung von Konten und Berechtigungen für [Dienste, Standorte und Organisationseinheiten]. Der fachliche Eigentümer entscheidet über Geschäftsbedarf, die Systemverantwortung über technische Umsetzung und die unabhängige Kontrollrolle über die Nachweisqualität. Prüfintervall [Intervall], genehmigt durch [Rolle und Datum].

## Prüfumfang vorbereiten

1. Stichtag, freigegebenen Rollenkatalog und zugehörige Systeme bestimmen. Kontenbestand, Gruppen, direkt zugewiesene Rechte, externe Identitäten sowie Notfall- und technische Konten getrennt erheben.
2. Vollständigkeit der Grundgesamtheit anhand Quellsystem, Abfragefilter, Exportzeit und Anzahl der Einträge prüfen. Nicht erfasste Systeme und Schnittstellen dokumentieren.
3. Eintritt, Rollenwechsel, Austritt, technische Konten und besonders weitreichende Rechte als getrennte Stichproben betrachten. Begründung für Stichprobengröße und Auswahl festhalten.

## Entscheidungen treffen und umsetzen

| Beobachtung | Entscheidung | Nachweis |
| --- | --- | --- |
| Fachliche Rolle und Rechte stimmen überein | Zuständigkeit bestätigt Bedarf und verbleibende Laufzeit | Freigabe und exportierter Rollenstand |
| Eine temporäre Berechtigung ist abgelaufen | Betrieb entzieht sie und überprüft delegierte Zugriffe | Änderungsticket und technische Gegenprüfung |
| Verwaistes Konto oder unklare Berechtigung | Fachstelle bewertet Risiko und veranlasst Sperre oder befristete Ausnahme | Entscheidung, Frist und technische Beobachtung |

Die Prüferrolle bestätigt stichprobenartig den tatsächlichen Entzug oder prüft bei begründetem Bedarf die genehmigte Verlängerung. Für Ausnahmen werden Verantwortung, Eintrittsvoraussetzung, Ende, kompensierende Kontrolle und Freigabe dokumentiert.

## Beispiel und Eskalation

Ein Fachkonto hat nach einem internen Wechsel noch eine administrierende Gruppe. Die Fachstelle prüft den verbleibenden Bedarf, der Betrieb entzieht die nicht mehr benötigte Rolle und die Prüferrolle vergleicht danach effektive Rechte und Standortzugang. Eine Lücke im Rollenkonzept wird als gesonderte Maßnahme nachgehalten.

## Abnahme und Wiedervorlage

Die Kontrollakte enthält Grundgesamtheit, geprüfte Rechte, Entscheidungen, Änderungen, offene Ausnahmen und Wiederholungsstichproben. [Kontrollverantwortung] prüft die Bearbeitungsquote, überfällige Entzüge und offene Ausnahmen und eskaliert vereinbarte Abweichungen an [Entscheidungsrolle].', 1),

('D3-PRO-ISMS-SOA', 'Standard', 'Anwendbarkeit und Abdeckung von Sicherheitskontrollen dokumentieren', 'ISO27001',
 'ISO/IEC 27001:2022, Abschnitt 6.1.3 und eigene genehmigte Kontrollauswahl',
 '## Zweck und Geltungsbereich

Diese Arbeitsfassung führt die Behandlungsentscheidungen des ISMS auf eine nachvollziehbare Kontrollauswahl zurück. Die ISMS-Verantwortung pflegt [Scope, Methode, Register und Bezugsfassungen]. Die Genehmigung erfolgt durch [Rolle], die nächste Abnahme am [Datum].

## Arbeitsregeln

1. Je Risiko und weiterer verbindlicher Vorgabe dokumentieren, welche Behandlung erforderlich ist. Begründete Entscheidungen aus Geltungsbereich, Verträgen und anderen Anforderungen mitführen.
2. Schutzmaßnahmen auswählen, ihre Herkunft und Fassung festhalten und die Kontrollauswahl auf Vollständigkeit prüfen. Für eine verwendete Referenzliste die Einträge und Ausschlussgründe anhand der genehmigten Bezugsfassung überprüfen.
3. Für jede ausgewählte Maßnahme Sollumsetzung, tatsächlichen Betriebsstand, zuständige Rolle und Beleg erfassen. Teilabdeckung und Übergangsmaßnahmen nachvollziehbar beschreiben.
4. Ausschlüsse begründen. Konkrete vorhandene Bedingungen, Abhängigkeiten und formale Freigabe der Entscheidung festhalten. Bei späterer Änderung eine Neubewertung auslösen.

## Arbeitstabelle

| Kontrollbezug und Fassung | Ursprung und betroffene Risiken | Entscheidung und Begründung | Umsetzung und Nachweis |
| --- | --- | --- | --- |
| [K-ID und Version] | Zugriff auf externe Administration | Anwendbar, da externe Betriebsrolle vorhanden | MFA-Konfiguration und Test vom [Datum] |
| [K-ID und Version] | Behandlung des physischen Standorts | [Anwendbar oder Ausschluss mit Begründung] | [Betrieb, Vertrag und Beleg] |

## Freigabe und Änderung

Die Vorfassung aufbewahren, Änderungen je Entscheidung dokumentieren und eine Stichprobe von der Risikoentscheidung bis zum Kontrollnachweis verfolgen. Ein Wechsel des Betriebsmodells, der Lieferbeziehung oder des Scope löst eine erneute Prüfung aus. Offene Lücken erhalten [Verantwortung, Termin und akzeptiertes Restrisiko].', 1)
ON CONFLICT(Id) DO NOTHING;

INSERT INTO Templates (Id, Kind, Title, SourceId, Locator, Body, Revision) VALUES
('D3-PRO-NIS2-SCOPE', 'Guidance', 'NIS-2-Betroffenheitsprüfung und Registrierungsakte für Deutschland', 'BSIG',
 '§ 28, §§ 33 und 34 sowie Anlagen 1 und 2 BSIG; Sonderregeln, maßgebliche Quellenfassung und zuständige Stelle prüfen',
 '## Prüfauftrag und Ausgangsdaten

[Rechtsträger, Länder, Leistungsportfolio, Eigentumsstruktur, Verantwortliche und Stichtag] bestimmen. Für jede betrachtete Einheit die tatsächlich angebotenen Leistungen, Zielgruppen, Beschäftigtenzahlen und Finanzdaten mit Datenquelle und Bezugszeitraum festhalten. Gruppendaten und Rollen mehrerer Einheiten gesondert auswerten.

## Entscheidung je Einrichtung

1. Tätigkeiten anhand der Sektoren und Einrichtungsarten in Anlagen 1 und 2 sowie der Begriffe des BSIG einordnen. Vertragsbeschreibung, tatsächlichen Betrieb und organisatorische Zuordnung vergleichen.
2. Maßgebliche Größen- und Verflechtungskriterien sowie mögliche größenunabhängige Fälle, Sonderfälle und Ausnahmen prüfen. Annahme, Nachweis und offene Tatsachen für jedes Kriterium getrennt dokumentieren.
3. Einstufung als besonders wichtige Einrichtung, wichtige Einrichtung, anderer Fall oder offen begründet festhalten. Bei Auslandsbezug die einschlägige nationale Rechtsordnung, Zuständigkeit und gegebenenfalls mehrere Pflichten getrennt ermitteln.
4. Für die betroffene Einrichtung die anwendbare Registrierungsvorschrift, Frist und Kontaktrolle bestätigen. Erforderliche Angaben mit der aktuellen Vorgabe abgleichen und den Registrierungsbeleg zugriffsgeschützt ablegen.
5. Meldewege, Risiko- und Leitungsverantwortung im zuständigen Projekt verankern. Änderungen an Leistungen, Organisation und Registerdaten als Ereignisse für eine neue Prüfung festlegen.

## Entscheidungstabelle

| Prüffrage | Beleg | Ergebnis |
| --- | --- | --- |
| Welche Leistungen umfasst der Rechtsträger tatsächlich? | [Leistungsnachweis, Standort und Empfänger] | [Einrichtungsart oder Lücke] |
| Welche Größen- oder Sonderregel ist anzuwenden? | [Bezugsdaten, Verbund und Rechtsgrundlage] | [Ergebnis mit Begründung] |
| Welche Stelle und welches Meldeverfahren greifen? | [Rechtsordnung und aktueller Behördenhinweis] | [Anwendbarer Kanal und Kontaktrolle] |
| Welche Einrichtung ist registriert? | [Registrierungsbeleg und Bestätigungsdatum] | [Status und offene Schritte] |

## Freigabe und Wiedervorlage

Compliance bereitet die Entscheidung vor, der Leistungsbereich bestätigt die tatsächlichen Tätigkeiten und die zuständige Leitung genehmigt Aufnahme und Maßnahmenplan. Beispiel zur Anpassung. Der neue Betrieb einer andersartigen digitalen Dienstleistung löst eine erneute Prüfung des Sektors und der Zuständigkeit aus. Offene Kriterien erhalten [Termin und verantwortliche Rolle].', 1),

('D3-PRO-NIS2-RESPONSE', 'WorkInstruction', 'Erheblichen Sicherheitsvorfall nach BSIG bearbeiten und dokumentieren', 'BSIG',
 '§ 32 BSIG und aktueller Meldekanal der zuständigen Stelle, bei Sonderfällen weitere Vorgaben prüfen',
 '## Auftrag und Geltung

Dieses Ablaufblatt unterstützt die koordinierte Behandlung eines möglichen erheblichen Sicherheitsvorfalls bei [betroffener Einrichtung] im Geltungsbereich [Dienste]. Es ergänzt den allgemeinen Vorfallplan und verwendet dessen Rufbereitschaft. [Vorfallleitung] hält technische Entscheidungen und [Meldekoordination] die rechtliche Einstufung fest. Für beide gilt ein benannter Ersatzkontakt.

## Fallaufnahme und erste Entscheidung

1. Fall-ID und Erkenntniszeitpunkte mit Zeitzone erfassen. Interne Entdeckung, Alarmübernahme, Kenntnis und spätere Neubewertungen mit ihren Quellen unterscheiden.
2. Betroffene Leistung, Ausfall, Qualitätseinbuße, Datenintegrität, betroffene Dritte und vorläufige Reichweite dokumentieren. Fortschreitende Auswirkungen und Abhängigkeiten in eigenen Feldern führen.
3. Erheblichkeit anhand der einschlägigen Regeln und bestätigten Fakten beurteilen. Die zuständige Stelle, anwendbare Frist und Rechtsträgerentscheidung unabhängig vom Betriebsstatus erfassen. Bei wechselseitigen Meldepflichten die Zuständigkeit je Regelwerk getrennt festhalten.
4. Frühmeldung, Folgemeldung, gegebenenfalls angeforderte Zwischenmeldung und Abschluss- oder Fortschrittsmeldung als eigene Aufgaben führen. Für jede Aufgabe rechtlichen Auslöser, Fälligkeit, vorliegenden Informationsstand und Übermittlungsnachweis festhalten.
5. Eindämmung, Notbetrieb, Wiederherstellung und Meldeentscheidung parallel verfolgen. Rechtzeitig vor einer offenen Frist Vertretung oder Eskalationsrolle aktivieren.

## Fallprotokoll

| Eintrag | Auszufüllen |
| --- | --- |
| Erkenntniszeitpunkt und Quelle | [Zeit mit Zeitzone, Quelle, bestätigter Fakt] |
| Bewertung und nächster Prüftermin | [Kriterium, Entscheidung, offenen Punkt und Verantwortung] |
| Meldung und Status | [Art, Rechtsgrundlage, Fälligkeit, Übermittlung und Bestätigung] |
| Folgen für Dienst und Dritte | [Umfang, Dauer, Wiederanlauf und Restfolgen] |
| Freigabe und Kommunikation | [Rolle, Entscheidung und Verteiler] |

## Synthetisches Übungsbeispiel

Die zentrale Fachanwendung fällt aus und ein Ersatzkanal ist nur eingeschränkt verfügbar. Die Vorfallleitung erfasst die tatsächlich betroffenen Leistungen. Die Meldekoordination dokumentiert eine vorläufige Einstufung, ordnet Zuständigkeit und Frist zu und hinterlegt, welche Erkenntnisse für die nächste Entscheidung fehlen. Das Übungsteam verwendet den dafür freigegebenen Testkanal.

## Abschluss und Nachweis

Quellprotokolle, Datumsangaben und übermittelte Fassung sicher referenzieren. Wiederanlauf und Endstand separat belegen. Aufarbeitung beschließt Ursache, Verbesserung, Verantwortung und Wiedervorlage. Bei geänderter Rufbereitschaft oder Meldestelle die Erreichbarkeit erneut testen.', 1),

('D3-PRO-NIS2-PROGRAM', 'Policy', 'NIS-2-Risikomanagement und Nachweisführung in der Leitung verankern', 'BSIG',
 '§§ 30, 31 und 38 BSIG; je Einrichtung Umfang, zusätzliche Anforderungen und Ausnahmen prüfen',
 '## Ziel und Freigabe

[Einrichtung] beschreibt geeignete und nachvollziehbare Maßnahmen für die Sicherheit ihrer Dienste. Die Festlegung gilt für [Dienste, Standorte, informationstechnische Systeme und Drittleistungen] und beruht auf [freigegebener Betroffenheitsentscheidung]. Dokumentverantwortung [Informationssicherheit], Genehmigung [Geschäftsleitung], Wirksamkeitsprüfung [Intervall].

## Verantwortung

Informationssicherheit koordiniert Zielbild und Nachweise. Dienstverantwortliche pflegen Risiken, Abhängigkeiten und Umsetzung. Einkauf steuert wesentliche Lieferbeziehungen. Der Betriebsbereich prüft Nutzung und Wiederherstellung. Die Geschäftsleitung verfolgt Umsetzung, Abweichungen, Entscheidungen und Schulungen im festgelegten Rhythmus.

## Arbeitsprogramm

1. Risiken nach betroffenen Diensten und möglichen Folgen erfassen. Die künftige Umsetzung an realistischen Gefährdungen, gemeinsamen Ausfallursachen und Abhängigkeiten ausrichten.
2. Vorgaben und Kontrollen für Vorfallbehandlung, Kontinuität, Lieferkette, sichere Beschaffung und Entwicklung, Zugriff, Authentisierung, Kryptografie und Schulung je anwendbarer Quelle einordnen. Jeden Punkt mit Owner, Entscheidung und Test verbinden.
3. Für übernommene und offene Controls je Dienst einen Beleg auswählen. Beispiele sind eine beobachtete Kontenstichprobe, ein dokumentierter Wiederanlauf, die bestätigte Lieferantenprüfung oder ein protokollierter Funktionstest.
4. Ziel, erreichbaren Stand, Lücke und Behandlung festhalten. Für ein Übergangsrisiko Befristung, Kompensation und zuständige Freigabe bestimmen.
5. Ergebnisse und Änderungsbedarf der Leitung vorlegen. Entscheidungen einschließlich Ressourcenzuweisungen und Wiederprüfung im Beschlussstand festhalten.

## Mindestangaben im Statusbericht

| Thema | Nachweis oder Entscheidung | Nächste Behandlung |
| --- | --- | --- |
| Sicherheitsrisiken der Dienste | [aktuelle wesentliche Risiken und Bewertungsstand] | [Owner, Termin und Eskalation] |
| Kontrollen und Prüfungen | [belegte Wirkung, Lücken, überfällige Erkenntnisse] | [Test- und Maßnahmenplan] |
| Drittanbieter und Kontinuität | [Ausfallabhängigkeit und getesteter Ersatz] | [Vertrag, Test oder Ersatzentscheidung] |
| Verantwortungen und Schulungen | [Beschlüsse, Schulungstermine und Vertretungen] | [Teilnehmende und Wiedervorlage] |

## Abnahme und Fortschreibung

Mindestens einen vollständigen Pfad von der anwendbaren Vorgabe über Risiko und Control bis zum Testbeleg nachvollziehen. Dazu einen unzureichenden Nachweis annehmen und prüfen, wie er korrigiert und erneut bewertet wird. Bei neuer Einrichtungsart, kritischer Abhängigkeit, erheblichem Vorfall oder maßgeblicher Änderung der Rechtslage aktualisieren.', 1),

('D3-PRO-AI-RELEASE', 'Guidance', 'KI-Einsatzfreigabe mit Prüfprotokoll und Auflagen', 'AIACT',
 'KI-Verordnung, insbesondere relevante Begriffe, Verbote, Einstufung und Betreiberpflichten; Rolle, Zeitpunkt und Anwendung prüfen',
 '## System und zulässiger Einsatz

[Systemversion, Modell oder Dienst, Anbieter, Prozess, Zwecke und Nutzergruppen] dokumentieren. Erlaubte Eingaben, ausgeschlossene Daten, erwartete Ausgaben und die menschlichen Entscheidungen nach der Ausgabe festlegen. Zwischen produktivem Einsatz, Pilot und technischer Erprobung unterscheiden.

## Rollen und Prüfung

Die Fachverantwortung legt den beabsichtigten Einsatz fest und führt geeignete Testfälle durch. KI-Governance dokumentiert Rolle, rechtliche Einordnung und offene Voraussetzungen. Datenschutz, Informationssicherheit und gegebenenfalls Arbeitnehmervertretung prüfen ihren Gegenstand. [Freigaberolle] entscheidet über Einsatzgrenzen und Auflagen. Betreiber und Vertretung erhalten erreichbare Eskalationswege.

## Bewertungs- und Prüfplan

1. Eingabequellen, Empfängerkreis, Abhängigkeiten, Modellversion und dokumentierte Verwendung des Anbieters festhalten. Auswirkungen auf Menschen und Fachentscheidungen anhand konkreter Fälle beschreiben.
2. Prüfdaten, Stichprobengröße und Erfolgskriterien vor dem Test festlegen. Grenzfälle, ähnliche Fälle mit unterschiedlichen Eingaben, Ausgabefehler und erwarteten Abbruch als getrennte Testgruppen betrachten.
3. Ergebnisse mit Quellen oder qualitätsgesicherten Vergleichsfällen abgleichen. Falsche Aussagen, fehlende Unsicherheitshinweise und unzulässige Datenweitergaben als nachprüfbare Befunde erfassen.
4. Zuständigkeit für menschliche Nachprüfung, Übergabe an manuelle Bearbeitung, Meldung von Fehlverhalten und vorübergehendes Aussetzen festlegen. Die Rückfallstrecke praktisch erproben.
5. Freigabeumfang, Bedingungen, Befristung, offene Befunde und erneute Prüfung mit dem Stand der eingesetzten Version verknüpfen.

## Prüfblatt und Beispiel

| Fall | Erwartung | Beobachtung und Entscheidung |
| --- | --- | --- |
| Antwortentwurf verweist auf eine unbekannte Quelle | Bearbeitung prüft Aussage vor Rückmeldung und verwirft unbelegte Inhalte | [Ergebnis, Fachfreigabe und Beleg] |
| Eingabe enthält eine für den Zweck unzulässige Information | Eingabe wird zurückgewiesen oder vor Übermittlung bereinigt | [Ergebnis und Schutzmaßnahme] |
| Modellversion oder Tool wechselt | Neue Version wird vor allgemeiner Nutzung gegen die geprüften Fälle verglichen | [Prüfung, Freigabe und Termin] |

## Nachweise und Überwachung

Zweckprofil, Rolle, verwendete Quelle, reproduzierbare Testfälle, Freigabe und Auflagen in der Einsatzakte halten. Die Fachverantwortung prüft Nutzungsgrenzen und offene Befunde [Intervall] sowie nach Änderungen an Modell, Datenquellen, Nutzerkreis, Prozess oder beobachteter Wirkung.', 1),

('D3-PRO-CLOUD-CHECK', 'Standard', 'Cloud-Dienst vor Produktivbetrieb und bei Änderungen abnehmen', 'MSSEC',
 'Herstellerdokumentation zum gewählten Dienst, interner ISMS-Kontrollrahmen und vertragliche Anforderungen',
 '## Dienstprofil und Geltung

Dokumentieren Sie [Dienstname, Vertragspartei, Mandant oder Konto, Datenklassen, Betriebsregion und unterstützte Prozesse]. Die Serviceverantwortung erstellt das Profil, Betrieb liefert Konfigurations- und Testnachweise, Informationssicherheit prüft die Vorgaben und [Freigaberolle] dokumentiert Entscheidung und Auflagen.

## Verantwortungsgrenzen

| Aufgabe | Zuständige Rolle und Dienstgrenze | Erwarteter Beleg |
| --- | --- | --- |
| Administration und Identitäten | [Organisation und Provider je Rollenebene] | Rollenexport, Anmeldeanforderung und Notfallzugangstest |
| Sicherung und Wiederherstellung | [verantwortet Datenebene, Plattform oder beides] | Wiederherstellungszeit, geprüfter Datenstand und Funktionsprobe |
| Vertraulichkeit und Aufbewahrung | [Konfiguration, Schlüsselkontrolle und Löschprozess] | Freigegebene Regel und überprüfte Stichprobe |
| Ereignisse und Eskalation | [Bereitschaft, Anbieterkanal und Ersatzkontakt] | Verfügbare Ereignisquelle und Kontaktübung |

Mehrmandantenfähigkeit, Zugriff durch Dienstpersonal, Protokollverfügbarkeit und relevante Unterauftragnehmer konkret abklären. Dokumentierte Zusagen und beobachtete Konfigurationen getrennt behandeln.

## Abnahme und erneute Prüfung

1. Schutzbedarf, Datenflüsse, Drittdienste und Zugriffsmodelle erfassen. Abweichungen vom Unternehmensstandard vor Produktivbetrieb mit Owner, Auswirkung, Kompensation und Ablaufdatum bewerten.
2. Je kritischer Voraussetzung einen überprüfbaren Test planen. Produktionsnahe Daten nur im genehmigten Rahmen einsetzen. Schnittstellen, Backups, Exportierbarkeit und alternative Unterstützung prüfen.
3. Den Ausfall einer zulässigen Komponente simulieren und Wiederherstellung oder manuellen Ersatz nachvollziehen. Stoppbedingungen und Rückkehrentscheidung im Testfall festhalten.
4. Betriebsfreigabe, Auflagen und betreuende Rolle im Dienstregister hinterlegen. Änderungen an Region, Identitätsanbieter, Schlüsseln, Exporten oder Wiederherstellungszusage lösen eine Neubewertung aus.

## Beispiel

Ein Abrechnungssystem übernimmt Identitäten aus dem Organisationsverzeichnis. Die Abnahme belegt Anmeldung und kontrollierten Notzugang, Wiederherstellung einer Testrechnung und den Export eines abgegrenzten Datensatzes. Kann ein Beleg noch nicht erbracht werden, bleibt die Freigabeentscheidung mit dieser konkreten Auflage und einem Folgetest dokumentiert.', 1)
ON CONFLICT(Id) DO NOTHING;

INSERT INTO Templates (Id, Kind, Title, SourceId, Locator, Body, Revision) VALUES
('D3-PRO-BCM-BIA', 'Procedure', 'Business Impact Analyse und Wiederanlaufprioritäten ausarbeiten', 'BSI2004',
 'BSI-Standard 200-4 als Orientierung; Bewertungsfenster und Wiederanlaufziele intern genehmigen',
 '## Auftrag, Geltungsbereich und Methodenstand

Diese Analyse betrachtet [Geschäftsprozess, Standorte, abhängige Dienste und Zeitraum]. Der Prozesseigner beschreibt wirkungsrelevante Abläufe und akzeptable Mindestleistung. Die BCM-Koordination führt die Methode, die technische Verantwortung plausibilisiert Ressourcen und Wiederherstellungszeiten. [Entscheidungsrolle] genehmigt Priorität, Ziele und offene Risiken.

## Auswirkungen über Zeit bestimmen

1. Ausfallbeginn und Annahmen festlegen. Hohe Nachfrage, geschäftliche Fristen, gemeinsame Abhängigkeiten und Datenerhalt getrennt betrachten.
2. Je Zeitfenster Konsequenzen für Kunden, Betrieb, Verträge, Sicherheit und Finanzen beschreiben. Für messbare Mengen Quellen und Unsicherheit festhalten.
3. Maximal tolerierbare Ausfallzeit (MTPD) aus den Folgen begründen. Das Wiederanlaufziel (RTO) so festlegen, dass Wiederaufnahme und erforderliche Puffer innerhalb der tolerierbaren Ausfallzeit liegen. Den maximal tolerierbaren Datenverlust (RPO) fachlich begründen.
4. Mindestbetrieb mit benötigter Kapazität, Personal, Daten, Anwendungen, Standorten und Lieferanten erfassen. Abhängigkeiten und ihre Wiederanlaufziele gegen die Prozessziele prüfen.
5. Ungedeckte Anforderungen mit Übergangslösung, Owner, Termin und genehmigtem Risiko dokumentieren. Kontrollierte Wiederaufnahme und Nachbearbeitung eines möglichen Rückstaus einplanen.

## Ausgefülltes Beispiel für die Herleitung

| Zeitfenster | Auswirkung und Annahme | Folgerung |
| --- | --- | --- |
| 0 bis 1 Stunde | Dringende Anfragen werden über einen abgesicherten Ersatzkanal aufgenommen | Mindestdienst und erreichbare Fallannahme prüfen |
| 1 bis 4 Stunden | Rückstand wächst, einzelne Rückmeldungen verzögern sich | Rückstau, Personalkapazität und Übernahmerhythmus berechnen |
| Nach 8 Stunden | Vereinbarte Tagesleistung ist ohne Zusatzkapazität nicht mehr erreichbar | MTPD und Zielpuffer im konkreten Fall begründen |

Im Beispiel wird ein RTO von vier Stunden und ein RPO von 30 Minuten für den Ticketbestand angenommen. Die endgültigen Werte ergeben sich aus [eigener Ausfallanalyse und Freigabe] und werden im Test mit geeigneten Zeit- und Datenbelegen verglichen.

## Prüfergebnis und Fortschreibung

Analyse, Annahmen, abhängige Ressourcen, Ziele und Beschluss im Register verknüpfen. [BCM-Koordination] prüft die Zielwerte nach Übungen, nach Änderungen im Servicebezug und zum nächsten planmäßigen Termin.', 1),

('D3-PRO-BCM-EXERCISE', 'WorkInstruction', 'Wiederanlauf üben und Zielerreichung unabhängig abnehmen', 'BSI2004',
 'BSI-Standard 200-4 als Arbeitsrahmen für Übungen und Tests; lokale Ziele und Szenarien verwenden',
 '## Übungsauftrag

[Übung, Prozess, Planfassung, Zeitraum und Umgebung] festlegen. Der Übungsleiter kontrolliert Durchführung und Stoppkriterien, Betriebs- und Fachrollen führen den Wiederanlauf aus. Eine unabhängige Beobachtungsrolle sichert Zeitmarken, Kontrollumfang und tatsächliches Ergebnis. Kritische Fachhandlungen vorher mit dem Betrieb abstimmen.

## Vorbereitung und Zielwerte

| Messgröße | Definition und Ausgangswert | Prüfmethode |
| --- | --- | --- |
| Wiederanlaufzeit (RTO) | [Startpunkt und genehmigtes Ziel] | Uhrzeit vom Ausfallbeginn bis zur fachlichen Nutzbarkeit |
| Datenverlust (RPO) | [akzeptabler Datenstand] | Bestätigter Vorgangsstand anhand vorbereiteter Prüfdaten |
| Mindestleistung | [Umsatz, Fallzahl oder verfügbare Prozessfunktionen] | Beobachtete Leistung über vereinbartes Zeitfenster |
| Rückkehr zum Regelbetrieb | [Entscheidung und Prüfpunkte] | Datenabgleich, offene Fälle und Kontrolle temporärer Rechte |

## Durchführung

1. Ausgangslage, Kommunikation, Sicherheitsgrenzen, Vertretungen und Abbruchkriterien bestätigen. Plan und Zielwerte für den Prüfstand festhalten.
2. Ausfall auslösen oder im Planspiel bekannt geben. Einstieg in den Notbetrieb, Verantwortliche und Entscheidungspunkte protokollieren.
3. Wiederanlauf anhand verfügbarer Ressourcen durchführen. Fachanwender prüfen maßgebliche Funktionen, Datenstände und Übergaben.
4. Unterbrechungen, Inanspruchnahme manueller Hilfe und nicht geprüfte Teile mit Zeit und Auswirkung dokumentieren. Rückkehr nur nach fachlicher Abnahme freigeben.
5. Ist-Werte gegen die Ziele vergleichen. Ausnahmen, verbleibende Risiken und Maßnahmen mit Zuständigkeit, Termin und Wiederholungstest beschließen.

## Synthetischer Fall und Abschluss

Im Beispiel steht das Kundenportal nach 95 Minuten technisch zur Verfügung. Erst nach bestätigtem Datenabgleich und einem vollständigen Testvorgang nach 130 Minuten ist der Dienst fachlich nutzbar. Gegen das genehmigte Ziel wird der fachliche Zeitpunkt ausgewertet. Der technische und der fachliche Zeitstempel bleiben im Testbericht erhalten.

[Übungsleitung] hält Szenario, Planfassung, Start, Stop, Beobachtungen, relevante Nachweise und Maßnahmen fest. Wesentliche Abweichungen lösen Überarbeitung des Wiederanlaufplans und erneute gezielte Prüfung aus.', 1),

('D3-PRO-OSS-RELEASE', 'WorkInstruction', 'Open-Source-Komponenten vor einer Softwarefreigabe prüfen', 'SPDX',
 'SPDX-Spezifikationen für Komponenten- und Lizenzinformationen; Pflichten nach konkreter Nutzung und Lizenztext prüfen',
 '## Ziel und Einsatzgrenze

Diese Arbeitsanweisung gilt für [Produkt, Lieferweg und Freigabekanäle]. Die Freigabe verwendet eine Software-Stückliste (SBOM) aus dem tatsächlich gebauten Stand. Produktverantwortung hält Build, Distribution und Freigabeentscheidung fest. Entwicklung liefert Komponentenbelege und Korrekturen, zuständige Fachrollen entscheiden über Lizenz- und Sicherheitsfragen.

## Prüfumfang

1. Produktversion, Build-ID, Lieferumfang und Erstellungszeit der Stückliste erfassen. Direkte und transitive Abhängigkeiten sowie enthaltene Binärdateien oder eingebettete Komponenten betrachten.
2. Komponentenidentität und Version gegen aufgelöste Build-Abhängigkeiten und Artefakt abgleichen. Manuell ergänzte Einträge kennzeichnen. Unbekannte Lizenz oder unplausible Version als Prüfungslücke behandeln.
3. Tatsächliche Verwendung und Vertriebsweg beschreiben. Pflichten wie Hinweise, Lizenztext, Quellbereitstellung oder besondere Erlaubnisse abhängig vom konkreten Fall fachlich prüfen und beschlossene Umsetzung dokumentieren.
4. Sicherheitsbefunde der Stückliste der eingesetzten Fassung zuordnen. Betroffenheit, erreichbare Schutzwirkung, Aktualisierung oder befristete Ausnahme anhand des Betriebsfalls bewerten. Auswirkungen eines Updates auf Build und Funktion prüfen.
5. Geeignete Hinweise, Belege und korrekte Stückliste mit dem freigegebenen Paket verknüpfen. Freigabe und Wiedervorlage der offenen Maßnahmen absichern.

## Stichprobenblatt

| Eintrag | Prüfmaßstab | Ergebnis |
| --- | --- | --- |
| Anwendungspaket | Identischer Produkt- und Buildstand in Paket und Stückliste | [Artefakt und Beleg] |
| Bibliothek mit unklarem Lizenzwert | Herkunft und einschlägige Lizenzbezeichnung fachlich geklärt | [Entscheidung und Nachweis] |
| Transitive Abhängigkeit mit Befund | Betroffenheit, Behandlung und Test dokumentiert | [Änderung oder befristete Ausnahme] |

## Beispiel und Nachweis

Bei einer weitergegebenen Desktop-Anwendung wird eine indirekte Bibliothek aus einer Build-Stufe ausgeliefert. Die Entwicklung gleicht die Version mit dem Paket ab, Fachrollen prüfen die korrekten Lizenzhinweise, anschließend wird das bereinigte Paket neu gebaut und freigegeben. Die Referenzen auf Freigabeakte, Stückliste und offene Ausnahmen werden in [Ablage] gepflegt.', 1),

('D3-PRO-DORA-TPRM', 'Procedure', 'IKT-Drittbezug und Vertragsakte für kritische Funktionen prüfen', 'DORA',
 'Art. 28 bis 30 DORA und einschlägige technische Standards; Rolle, Registerumfang und Vertragsfassung fallbezogen prüfen',
 '## Auftrag und Entscheidungsgrenze

[Finanzunternehmen, Vertragspartner, IKT-Dienst, betroffene Funktion und Vertragsfassung] bestimmen. Der Dienstverantwortliche beschreibt die Leistung und Abhängigkeit, Einkauf pflegt Vertragsdaten, Rechtsfunktion und Risikomanagement prüfen anwendbare Anforderungen und entscheiden über offene Punkte. [Freigaberolle] bewertet das verbleibende Risiko.

## Prüfung vor Beauftragung oder Änderung

1. Unterstützte Funktionen, Standorte und Datenverarbeitung erfassen. Die Einstufung der Funktion und wesentliche Folgen einer Leistungsstörung begründen.
2. Konzentrationen auf Anbieter, Standorte, Unterauftragnehmer und gemeinsam genutzte Plattformen beurteilen. Vertragspartner und tatsächliche Leistungserbringer im Register unterscheiden.
3. Rechte, Pflichten und geeignete Nachweise aus der Vertragsfassung herausarbeiten. Beispiele sind Leistungskennzahlen, Meldungen, Prüfrechte, Schutzanforderungen, Zugang zu Daten und Unterstützung bei Beendigung. Anwendbare Vorgaben anhand der tatsächlichen Rolle prüfen.
4. Mit dem Anbieter den Ausstieg, erreichbare Übergangszeit, Datenrückgabe, Löschbestätigung und Abhängigkeiten zu Ersatzdiensten beurteilen. Reale Machbarkeit und Testlücken getrennt festhalten.
5. Entscheidungsbefugte Rolle bestätigt vertragliche Verbesserungen, befristete Risikobehandlung oder Abbruch. Änderungen an Leistung, Unterbeauftragung oder Vertragsstruktur lösen eine erneute Prüfung aus.

## Prüftabelle

| Thema | Vertrag und betrieblicher Beleg | Entscheidung |
| --- | --- | --- |
| Wiederherstellung | [Zusage und zuletzt gemessene Verfügbarkeit, Wiederherstellungsprobe] | [akzeptiert, Auflage oder offen] |
| Vorfallunterstützung | [Ansprechpartner, Kanal und vereinbarte Reaktionsweise] | [Verantwortung und Übung] |
| Wechsel oder Ausstieg | [Datenformat, Übergangsleistungen, Frist, Test] | [Restabhängigkeit und Maßnahme] |

## Beispiel und laufende Überwachung

Ein externer Dienst ist für eine fristgebundene Finanzfunktion unverzichtbar. Die Funktionsverantwortung prüft, ob eine Ersatzleistung in der zulässigen Zeit starten kann. Eine fehlende Testmöglichkeit wird mit Begründung, wesentlicher Abhängigkeit, Maßnahmenfrist und Leitungsvorlage geführt. Vertrag, Registerstand, Review und Eskalation sind versioniert nachzuweisen.', 1),

('D3-PRO-PROCESS-PROFILE', 'Procedure', 'Geschäftsprozess beschreiben, freigeben und auf Änderungen prüfen', 'INTERNAL',
 'Interne Prozessmanagement- und Freigaberegeln; Prozesslandkarte, Maßnahmen und dokumentierte Revision verwenden',
 '## Prozessprofil

[Prozess-ID und Name], [Organisation und Geltungsbereich], [Prozessart und Kritikalität], [Prozesseigner] und [stellvertretende Rolle] angeben. Zweck, überprüfbares Ergebnis, auslösendes Ereignis, erste Tätigkeit und fachliches Ende beschreiben. In der Landkarte genau einen übergeordneten Prozess zuordnen, falls vorhanden. Liefert ein anderer Prozess ein benötigtes Ergebnis, die Abhängigkeit gesondert erfassen.

## Ablauf sowie Übergaben

| Schritt | Rolle und Eingabe | überprüfbares Ergebnis und Übergabe |
| --- | --- | --- |
| Anfrage erfassen | Service Desk übernimmt ein vollständiges Formular | Datensatz mit Kategorie und Termin an Fallbearbeitung |
| Voraussetzungen prüfen | Fallbearbeitung prüft Identität und Berechtigung gemäß Fachregel | Dokumentierte Entscheidung oder Rückfrage |
| Ausführen und kontrollieren | Fachteam folgt der genehmigten Reihenfolge | Ergebnis, Beleg und Informationsbedarf an die Fallverantwortung |
| Abschließen | Fallverantwortung vergleicht Ergebnis mit dem Auftrag | Bestätigter Abschluss und Status für Auswertung |

An jeder Übergabe Mindestangaben, Empfänger, Freigabe und Bearbeitungszeit festhalten. Sonderfälle wie fehlende Angaben, Vertretung, Wiederholung oder Ablehnung mit Auslöser und Weiterleitung beschreiben.

## Steuerung und Rollen

Prozessverantwortung pflegt Ablauf, Risiken und Verbesserungsvorschläge. Die ausführenden Rollen bestätigen praktische Umsetzbarkeit. [Kontrollrolle] gleicht ausgewählte Fälle mit den Sollschritten ab, [Freigaberolle] entscheidet über Einführung und Änderungen. Je wesentlichem Risiko eine maßgebliche Kontrolle und ihren Beleg referenzieren.

## Freigabe und Nachverfolgung

1. Normalfall, Rückfrage, Ausnahme und Übergabe anhand synthetischer Fälle durchspielen. Abweichungen als konkrete Prozessänderung nachhalten.
2. Gültige Fassung, Verantwortliche, Prüftermin und genehmigte Kennzahlen veröffentlichen. Ein Beispiel ist der Anteil vollständig übergebener Fälle im gewählten Monat.
3. Prozessverantwortung prüft den Ablauf bei einem neuen Auslöser, einer Schnittstellenänderung, erhöhtem Rückstand oder wiederholter Abweichung. Anlass, Änderung, Wirkung und Freigabe dokumentieren.

## Abnahme

[Prozessdiagramm, Prüffälle, Nachweis der Veröffentlichung und verknüpfte Verbesserungen] referenzieren. Offene Ausnahmen und die nächste Überprüfung mit Termin und Zuständigkeit im Prozessprofil festhalten.', 1)
ON CONFLICT(Id) DO NOTHING;

INSERT INTO Templates (Id, Kind, Title, SourceId, Locator, Body, Revision) VALUES
('D3-PRO-AUDIT-WORKPAPER', 'WorkInstruction', 'Interne Revision von der Stichprobe zur nachgeprüften Feststellung', 'INTERNAL',
 'Internes Revisionsmandat, freigegebener Auditplan und ausgewählte Kriterien mit Versionsstand',
 '## Prüfauftrag und Unabhängigkeit

[Audit-ID und Ziel] gilt für [Geltungsbereich, Zeitraum, Prozess und Standorte]. Die Auftragsleitung genehmigt Kriterien, Zeitplan und Berichtsempfänger. Die beauftragte Prüfrolle legt Befangenheit offen und dokumentiert Prüfgrenzen. Der Fachbereich bestätigt den Sachverhalt und übernimmt Maßnahmen. Eine unabhängige Rolle bestätigt wirksame Erledigung.

## Stichprobe dokumentieren

1. Prüfmaßstab und Fassung vor Beginn festhalten. Quelle und Grundgesamtheit bestimmen, ihre Vollständigkeit plausibilisieren und Stichprobenmethode mit Menge und Auswahl begründen.
2. Je Fall Soll, tatsächliche Beobachtung, Prüfer, Zeitpunkt und sichere Nachweisreferenz erfassen. Interviews durch Artefakte oder beobachtete Abläufe gegenprüfen.
3. Sachverhaltsgrenzen dokumentieren. Wird eine Stichprobe durch fehlende Unterlagen verhindert, den Umfang und die Auswirkung auf das Prüfungsurteil getrennt ausweisen.
4. Abweichungen beschreiben und mit Ursache, Wirkung sowie relevanten Kriterien verbinden. Geprüften Erwartungswert und die beobachtete Ausprägung unterscheidbar formulieren.

## Prüfung und Feststellung

| Merkmal | Musterinhalt |
| --- | --- |
| Kriterium | Für privilegierte Freigaben ist ein dokumentierter fachlicher Entscheid vor Aktivierung erforderlich. |
| Beobachtung | Bei [Anzahl] von [Stichprobengröße] Freigaben lag die Entscheidung erst nach der Aktivierung vor. |
| Auswirkung | Rechte waren bis zur Freigabe ohne bestätigten Fachbedarf verfügbar. |
| Nachweis | [minimierte Ticketreferenzen, Rollenauszug und Auswertungszeit] |
| Maßnahme und Nachprüfung | [Verantwortung, Frist, Nachweis und neu ausgewählte geeignete Fälle] |

## Bericht und Abschluss

Mit dem Fachbereich Fakten und Maßnahmen besprechen. Im Bericht Prüfziel, Umfang, Einschränkungen, wesentliche Feststellungen und den Grund für das Prüfungsurteil festhalten. Für jede Maßnahme Erfolgskriterium und Wirksamkeitsprüfung terminieren. Eine bestätigte Umsetzung wird im Feststellungsregister gesondert von der unabhängigen Wirksamkeit geführt. Abweichungen und noch offene Punkte an die dokumentierte Aufsichtsrolle eskalieren.', 1),

('D3-PRO-IKS-CONTROL', 'WorkInstruction', 'Interne Kontrolle ausführen und ihre Wirksamkeit prüfen', 'INTERNAL',
 'Interner IKS-Kontrollrahmen; zum Beispiel Genehmigung, Funktionstrennung und Nachweis der Ausführung',
 '## Kontrollprofil

[Control-ID und Titel] reduziert [konkretes Risiko] in [Prozess und Geltungsbereich]. [Kontrollverantwortung] hält Zweck, Auslöseereignis, Rhythmus, Benutzerrollen und zulässige Ersatzhandlung fest. Verwendete Datenquelle, erwarteter Kontrollumfang und Ausführungstermin sind dokumentiert.

## Sollablauf

1. Grundgesamtheit aus [führendem System] für den benötigten Zeitraum ermitteln. Filter, Zeitabdeckung und Vollständigkeit anhand Summen oder Vergleichsquelle überprüfen.
2. Kontrollhandlung mit genauem Entscheidungskriterium ausführen. Im Beispiel wird eine Vertragszahlung erst bestätigt, wenn Bestellung, Leistungsnachweis und genehmigter Betrag übereinstimmen.
3. Ausnahme mit Grund, Auswirkung und zu prüfender Kompensation dokumentieren. Bei erforderlicher Funktionstrennung Vertretung mit gleichwertiger Berechtigung verwenden.
4. Ergebnis, Datum, Rolle und Nachweis zur ausgewählten Grundgesamtheit festhalten. Schutzbedürftige Unterlagen über geeignete Referenzen belegen.

## Prüfschritte zur Wirksamkeit

| Prüffrage | Erwartung | Beobachtung |
| --- | --- | --- |
| Ist das Kontrolldesign zur Risikobehandlung geeignet? | Zweck, Auslöser und erwartete Entscheidung passen zum Risiko | [Test und Begründung] |
| Wird die Kontrolle wiederholbar durchgeführt? | Stichprobe zeigt Durchführung im erwarteten Zeitraum | [Prüfgegenstand, Zeitraum und Beleg] |
| Werden Abweichungen wirksam nachverfolgt? | Ausnahme hat Entscheidung, Verantwortliche und erneute Prüfung | [Fall, Maßnahme und Ergebnis] |

## Abweichung und Nachverfolgung

Wenn eine Ausführung fehlt oder der Beleg unvollständig ist, die Lücke mit Zeitraum und Wirkung erfassen und an [Zuständigkeit] geben. Ersatzhandlung für den aktuellen Zeitraum und dauerhafte Korrektur getrennt entscheiden. Im Folgetest neue Fälle prüfen, genehmigte Kriterien verwenden und den Abschluss samt möglichen Restrisiken dokumentieren.', 1),

('D3-PRO-RESILIENCE-SCENARIO', 'WorkInstruction', 'Kritischen Geschäftsdienst anhand seiner Auswirkungstoleranz testen', 'INTERNAL',
 'Interner Rahmen zur operationellen Resilienz; einschlägige sektorale Vorgaben und den genehmigten Dienstumfang ergänzen',
 '## Dienst, Auswirkung und Toleranz

[Kritischer Geschäftsdienst, Zielgruppen, Verantwortliche und Stichtag] bestimmen. Leistung und relevante Kennzahlen aus Sicht der Empfänger beschreiben. Genehmigte Toleranz mit Zeithorizont, erforderlicher Mindestleistung und erlaubtem Rückstau dokumentieren. Die Bewertung bezieht sich auf beobachtbare Folgen und benennt Annahmen über Zeitpunkt und Nachfrage.

## Abhängigkeiten und Szenario

1. Abhängigkeiten auf Ebene von Personal, Standorten, IT, Daten, Lieferanten und organisatorischen Übergaben erfassen. Gemeinsame Ursachen und feste Reihenfolgen markieren.
2. Plausible Mehrfachstörung wählen. Im Beispiel fällt das zentrale Ticketportal aus und der externe Support ist verzögert erreichbar. Annahmen zur verfügbaren Personalkapazität und fachlichen Ersatzstrecke dokumentieren.
3. Testverantwortung, Entscheidungsberechtigung, Umgebung, Abbruchbedingungen und unabhängige Beobachtung vorab festlegen. Genehmigte Toleranz und Erfolgsmaßstäbe zum Testzeitpunkt sichern.
4. Zeit, erreichbaren Mindestbetrieb und unbearbeitete Anfragen messen. Ersatzweg und Rückstauabbau bis zur Wiederherstellung beobachten.

## Bewertung und Nachverfolgung

| Messgröße | Vereinbart | Beobachtet | Bewertung |
| --- | --- | --- | --- |
| Unterbrechungsdauer | [genehmigte Zeittoleranz] | [Start, Ende und Dauer] | [Vergleich] |
| Kundenbezogene Mindestleistung | [Menge und Prüfintervall] | [erreichte Menge und Beleg] | [Vergleich] |
| Unbearbeitete Fälle | [zulässiger Rückstau] | [Menge und erwarteter Abbau] | [Vergleich] |

## Nachweise und Freigabe

Das Ergebnis enthält Aussagegrenzen, dokumentierte Engpässe und notwendige Verbesserungen. [Serviceverantwortung] priorisiert Maßnahmen und setzt Folgetest und Termin. Übergangsrisiken und die Entscheidung über Ressourcen oder alternative Lieferwege getrennt festhalten. Abhängigkeiten und Toleranz nach Test, wesentlichem Vorfall oder Geschäftsänderung erneut prüfen.', 1),

('D3-PRO-CLM-RENEWAL', 'Procedure', 'Verträge vor Verlängerung, Kündigung oder Ausstieg prüfen', 'INTERNAL',
 'Vertragsakte, vereinbarte Kündigungs- und Verlängerungsregel, interne Delegation und anwendbare Rechtslage prüfen',
 '## Zweck und Freigabe

Dieses Verfahren gilt für [Beschaffungs- und Leistungsarten] bei [Organisation und Scope]. Vertragsverantwortung pflegt die aktuelle Fassung, Fachbereich beurteilt Nutzen und Leistung, Einkauf führt die Abstimmung, Rechtsfunktion prüft unklare Fristen oder Formerfordernisse. [Entscheidungsrolle] genehmigt Fortsetzung, Neuverhandlung oder Beendigung.

## Vertrags- und Termindaten sichern

1. Parteien, Vertrags-ID, Leistung, Vertragsbeginn, aktuelle Laufzeit und unterschriebene Fassung ermitteln. Nachträge und die Reihenfolge anwendbarer Vertragsdokumente prüfen.
2. Stichtag, Erfüllungsort, Adressat und einzuhaltende Form aus der wirksamen Vertragsfassung festhalten. Internen Entscheidungs- und Versandtermin mit Puffer planen.
3. Entgelte, Nutzungsrechte, Änderungen, Leistungswerte, offenstehende Pflichten und Wiederbeschaffungs- oder Ausstiegsaufwand beurteilen. Interne Freigabebefugnisse und Abhängigkeiten einbeziehen.
4. Entscheidung mit Begründung und Zuständigkeit dokumentieren. Für Neuverhandlung oder Ausstieg die Schritte bis zur gesicherten Folgeleistung und zum Ende alter Zugriffe festlegen.
5. Empfang und Wirkung der Erklärung und neuen Vertragsfassung überprüfen. Register und Folgetermine an die tatsächlich wirksame Vereinbarung anpassen.

## Entscheidungsmatrix

| Option | Fachliche Kriterien | Nächste Handlung |
| --- | --- | --- |
| Verlängern | Leistung, Kosten und Risiken sind entschieden | Genehmigung, Vertrag und Folgetermin sichern |
| Neu verhandeln | Leistungsgrenzen, Prüfrechte oder Preise brauchen Anpassung | Vergleich, Verhandlungsleitung und Entscheidungsfrist setzen |
| Beenden oder auslaufen lassen | Nachfolge, Datenrückgabe und Betrieb sind geplant | Erklärung, Empfang, Übergabe und Schließung nachweisen |

## Beispiel und Ablage

Eine fiktive Supportleistung verlängert sich mangels Kündigung nach dem vereinbarten Stichtag. Die Vertragsverantwortung prüft Vertragsfassung, Fristen und Zustellung. Der Fachbereich benötigt die Übergabe eines Wissensbestands, bevor die Nachfolgelösung übernimmt. Die Entscheidung erfasst den reservierten Übergabezeitraum und einen verantwortlichen Ansprechpartner. Vertrag, Folgeschritte, Freigaben und Empfangsbelege im Register referenzieren.', 1),

('D3-PRO-SUPPLIER-REVIEW', 'Procedure', 'Lieferanten auswählen, überprüfen und bei Abweichungen nachsteuern', 'INTERNAL',
 'Interne Lieferantenrichtlinie, Vertragszusagen und vom konkreten Leistungstyp abhängige Fachvorgaben',
 '## Lieferprofil und Prüfumfang

[Lieferant und rechtliche Einheit], [Dienst und unterstützte Prozesse], [Standorte, Systeme, Zugriffs- und Datenmodell] und [Vertragsfassung] festhalten. Die Dienstverantwortung erklärt Geschäftsbedeutung und Ersatzmöglichkeiten. Einkauf führt die Akte. Fachstellen bewerten betroffene Schutzanforderungen, und [Risikoeigner] genehmigt Bedingungen und verbleibende Risiken.

## Vorprüfung und laufende Bewertung

1. Kritikalität anhand Ausfallwirkung, Zugriffsart und Wechselaufwand begründen. Unterauftragnehmer und gleichartige Abhängigkeiten über mehrere Leistungen betrachten.
2. Dokumente und zugesagte Leistungen gegen tatsächlichen Umfang und jüngste Vorfälle prüfen. Bescheinigungen anhand ausstellender Stelle, Zeitraum und konkretem Prüfumfang einordnen.
3. Vertragliche Leistungswerte, Wiederherstellung, Meldewege, verantwortliche Personen und Nachweise zur Einhaltung prüfen. Für eine wesentliche Abweichung vorläufigen Schutz und Frist zur Korrektur festlegen.
4. Bedarf zur Neubewertung bei Leistungsänderung, relevantem Vorfall, ausgelaufenem Nachweis oder neuem Unterauftragnehmer bestimmen.
5. Freigeben, mit Auflagen fortführen, zu weiteren Maßnahmen eskalieren oder einen geordneten Ausstieg beschließen. Umsetzung und finale Entscheidung dokumentieren.

## Prüftabelle

| Prüffeld | Beleg | Ergebnis und Behandlung |
| --- | --- | --- |
| Servicequalität und Störungen | Zeitreihe zu vereinbarten Werten, Störungsprotokoll | [Ergebnis, Ursachen und Termin] |
| Schutzmaßnahmen und Nachweise | Bericht, Stichprobe oder beobachtete Durchführung | [Abdeckung, Lücke und Auflage] |
| Wechsel und Notbetrieb | Aktualisierter Übergangsplan und Rückgabeformat | [Machbarkeit und Testtermin] |

## Beispiel und Wiedervorlage

Ein beauftragter Betriebsdienst meldet zwei Qualitätsabweichungen. Die Fachverantwortung gleicht zugesagtes Leistungsniveau mit Zeitreihe und Ursache ab. Die Entscheidung führt eine befristete Auflage mit Zwischenmessung und Abbruchkriterium. Bei fehlender Umsetzung entscheidet der Risikoeigner über Eskalation oder vorbereiteten Wechsel. [Nächster Reviewtermin, Verantwortung und Kontrollbeleg] im Lieferantenregister pflegen.', 1)
ON CONFLICT(Id) DO NOTHING;

INSERT INTO DesktopTemplateTopics (TemplateId, TopicId) VALUES
    ('D3-PRO-PRIVACY-INC', 'PRIVACY'),
    ('D3-PRO-PRIVACY-DSFA', 'PRIVACY'),
    ('D3-PRO-ISMS-ACCESS', 'ISMS'),
    ('D3-PRO-ISMS-SOA', 'ISMS'),
    ('D3-PRO-NIS2-SCOPE', 'NIS2'),
    ('D3-PRO-NIS2-RESPONSE', 'NIS2'),
    ('D3-PRO-NIS2-PROGRAM', 'NIS2'),
    ('D3-PRO-AI-RELEASE', 'AI'),
    ('D3-PRO-CLOUD-CHECK', 'CLOUD'),
    ('D3-PRO-BCM-BIA', 'BCM'),
    ('D3-PRO-BCM-EXERCISE', 'BCM'),
    ('D3-PRO-OSS-RELEASE', 'OSS'),
    ('D3-PRO-DORA-TPRM', 'DORA'),
    ('D3-PRO-PROCESS-PROFILE', 'PROCESS'),
    ('D3-PRO-AUDIT-WORKPAPER', 'ISMS'),
    ('D3-PRO-AUDIT-WORKPAPER', 'PROCESS'),
    ('D3-PRO-IKS-CONTROL', 'ISMS'),
    ('D3-PRO-IKS-CONTROL', 'PROCESS'),
    ('D3-PRO-RESILIENCE-SCENARIO', 'BCM'),
    ('D3-PRO-RESILIENCE-SCENARIO', 'DORA'),
    ('D3-PRO-CLM-RENEWAL', 'ISMS'),
    ('D3-PRO-CLM-RENEWAL', 'PROCESS'),
    ('D3-PRO-SUPPLIER-REVIEW', 'ISMS'),
    ('D3-PRO-SUPPLIER-REVIEW', 'BCM')
ON CONFLICT DO NOTHING;
