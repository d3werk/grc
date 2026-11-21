-- Original D3 GRC working templates. Existing templates and custom versions are retained.
-- Source locators are review starting points. Bracketed parameters are project-specific.
INSERT INTO Templates (Id, Kind, Title, SourceId, Locator, Body, Revision) VALUES
('D3-WP-PRIVACY-REQ', 'Requirement', 'Verarbeitungstätigkeit erfassen und freigeben', 'GDPR',
 'Art. 5, 6 und 30 DSGVO; je nach Verarbeitung weitere Vorschriften und Ausnahmen prüfen',
 '## Geltungsbereich

[Prozess und Gesellschaft], [betroffene Personengruppen], [Systeme und Dienstleister]. Beginn, Ende und Übergaben der Verarbeitung aus Sicht der Betroffenen beschreiben.

## Verantwortung

Die Prozessverantwortung erhebt Zweck, Datenflüsse und tatsächliche Empfänger. Datenschutz prüft Rollen, Rechtsgrundlage und den Bedarf einer Folgenabschätzung. Die Freigabe liegt bei [entscheidungsbefugter Rolle].

## Vorgehen

1. Erhebung, Nutzung, Übermittlung, Archivierung und Löschung als Datenfluss skizzieren. Datenarten, Speicherorte, Empfänger, Drittlandbezug und Zugriffsgruppen erfassen.
2. Je Zweck Rechtsgrundlage, Erforderlichkeit, Aufbewahrung und Löschereignis begründen. Bei gemeinsamen oder ausgelagerten Tätigkeiten Verantwortlichkeiten und Vertragsbezüge klären.
3. Informationen für Betroffene und tatsächliche Verarbeitung vergleichen. Risiken und offene Fragen vor Produktivsetzung mit Termin und Zuständigkeit dokumentieren.

## Prüfmaßstab

Beispiel zur Anpassung [Änderung des Zwecks, Exportziels oder Dienstleisters löst erneute Prüfung vor Freigabe aus]. Offene Hochrisikofragen an [Eskalationsrolle] geben.

## Nachweise und Wiedervorlage

Versionierter Verzeichniseintrag, Datenfluss, Entscheidung und Vertragshinweis. Die nächste Prüfung an [Frist oder Ereignis] binden und offene Folgeaufgaben referenzieren.', 1),

('D3-WP-PRIVACY-CTRL', 'Control', 'Anfragen betroffener Personen bearbeiten und nachhalten', 'GDPR',
 'Art. 12 bis 22 DSGVO; Anspruch, Ausnahmen und anwendbare Frist fallbezogen prüfen',
 '## Geltungsbereich

[Postfach, Supportkanäle, Niederlassungen]; umfasst Eingang, Zuordnung, Fachantwort und Abschluss einer Anfrage.

## Verantwortung

Die Eingangsstelle erfasst Anfragen. [Datenschutzkoordination] prüft Identität und Anspruch und koordiniert Fachbereiche. [Freigaberolle] entscheidet über Ablehnungen, Einschränkungen und Eskalationen.

## Ablauf

1. Empfangszeitpunkt, Anfrageart, betroffene Verfahren und verantwortliche Einheit protokollieren. Identität mit verhältnismäßigen Informationen absichern und Rückfragen dokumentieren.
2. Anwendbare Frist aus dem konkreten Fall bestimmen, Wiedervorlage und verantwortliche Person setzen. Drittempfänger und Suchumfang ermitteln.
3. Daten zusammentragen, Rechte Dritter und Ausnahmen prüfen und die Antwort für die zutreffende Person in einem geeigneten Kanal bereitstellen. Zeitpunkt und Umfang der Antwort dokumentieren.
4. Ursachen verspäteter oder unvollständiger Antworten überprüfen und Maßnahmen zuordnen.

## Eskalation

Beispiel zur Anpassung [interne Erinnerung bei Hälfte der Frist, Eskalation fünf Arbeitstage vor Ablauf]. Bei Unsicherheit über Identität oder Umfang die Fachfreigabe einholen.

## Nachweise und Prüfung

Minimiertes Eingangs- und Fristenprotokoll, Freigabe und Versandnachweis. [Quartalsweise] Fälle über verschiedene Kanäle auf Frist, Umfang und sicheren Versand prüfen.', 1),

('D3-WP-PRIVACY-AUD', 'Audit', 'Rollenberechtigungen und Löschfristen stichprobenartig prüfen', 'GDPR',
 'Art. 5 und 32 DSGVO; dokumentierte Zwecke, Aufbewahrungsregeln und Schutzmaßnahmen als Prüfkriterien konkretisieren',
 '## Prüfauftrag

[Verarbeitung, Anwendungen und Zeitraum]. Ziel ist die Gegenprüfung zwischen dokumentierter Regel und tatsächlicher Berechtigung sowie Aufbewahrung.

## Vorbereitung

[Unabhängige Prüferrolle] sichert die genehmigten Regeln, [Systemverantwortung] stellt exportierte Berechtigungs- und Löschereignisse bereit. Die Auswertung auf erforderliche Angaben begrenzen.

## Prüfschritte

1. Konten mit erweitertem Zugriff, Austrittsfälle und technische Konten als getrennte Gruppen betrachten. Je Gruppe [Stichprobengröße] und Auswahlweg dokumentieren.
2. Jede gezogene Berechtigung mit Genehmigung, Rolle, Zweck und zugehörigem Gültigkeitszeitraum abgleichen. Beim Austritt den tatsächlichen Entzug prüfen.
3. Bei [Datenkategorien] das Löschereignis sowie die nachfolgende Verfügbarkeit oder begründete Aufbewahrung anhand geeigneter Aufzeichnungen nachweisen.
4. Regelabweichungen mit Umfang, Risiko und objektiver Beobachtung festhalten. Wiederkehrende Abweichungen als systematische Ursache prüfen.

## Abschluss

Erwartung, Beobachtung und Nachweisreferenz pro Stichprobe erfassen. Für Abweichungen Rolle, Korrekturtermin und eine unabhängige Stichprobe zur Wirksamkeit festlegen. Nur minimierte Prüfauszüge hinterlegen.', 1),

('D3-WP-ISMS-REQ', 'Requirement', 'ISMS-Geltungsbereich und Risikobehandlung festlegen', 'ISO27001',
 'ISO/IEC 27001:2022, Abschnitte 4.3 und 6.1; genehmigte Normfassung und Scope prüfen',
 '## Ziel und Geltungsbereich

[Organisationseinheiten, Standorte, Geschäftsprozesse und Dienste]. Einbezogene Assets, Schnittstellen und ausgelagerte Leistungen benennen. Abgrenzungen anhand der tatsächlichen Abhängigkeiten begründen.

## Verantwortung

[ISMS-Verantwortung] pflegt Kriterien und Methode. Die Fachbereiche bewerten Risiken und schlagen Maßnahmen vor. [Leitung oder delegierte Rolle] genehmigt Geltungsbereich und wesentliche Restrisiken.

## Vorgehen

1. Informationswerte nach [Vertraulichkeit, Integrität und Verfügbarkeit] und Geschäftsbedeutung einordnen. Abhängigkeiten und betroffene Anforderungen referenzieren.
2. Szenario, Eintrittsbedingungen, Auswirkung und vorhandene Controls je Risiko dokumentieren. Skala und Akzeptanzschwelle genehmigen.
3. Behandlung wählen, Verantwortliche und Zieltermine setzen und die ausgewählten Controls mit Begründung abbilden. Ausnahmen mit Ablauf und Kompensation versehen.
4. Das verbleibende Risiko nach tatsächlicher Umsetzung erneut bewerten und die Entscheidung dokumentieren.

## Prüfmaßstab und Nachweis

Beispiel zur Anpassung [Risikostufe hoch erfordert Entscheidung der Leitung]. Versionierter Scope, Bewertungsmethode, Behandlungsplan und genehmigte Restrisiken. Bei wesentlichen Organisations- oder Systemänderungen neu prüfen.', 1),

('D3-WP-ISMS-CTRL', 'Control', 'Identitäten und privilegierte Zugriffe über den Lebenszyklus steuern', 'BSIGS',
 'IT-Grundschutz-Kompendium, Baustein zum Identitäts- und Berechtigungsmanagement; Ausgabe und Auswahl prüfen',
 '## Geltungsbereich

[Identitätsverzeichnis, Anwendungen und Infrastruktur] einschließlich personenbezogener, technischer und externer Konten.

## Verantwortung

[Führungskraft] beantragt fachlich benötigte Rollen. [Systemverantwortung] genehmigt erweiterte Rechte. [IAM-Betrieb] setzt Änderungen um. Eine unabhängige Person prüft Ausnahmefälle und Kontrollnachweise.

## Ablauf

1. Vor Bereitstellung Identität, Funktion, Enddatum und kleinste erforderliche Rolle ermitteln. Rollenwechsel lösen eine Prüfung alter und neuer Rechte aus.
2. Privilegien mit erhöhten Anforderungen an Authentisierung, Protokollierung und zeitliche Begrenzung gewähren. Für Notzugänge Verwahrung, Nutzung und anschließende Kontrolle festlegen.
3. Austritte aus dem führenden Verzeichnis erfassen und Zugang, Sitzungen sowie delegierte Schlüssel nach [Prozess] entziehen. Technische Konten mit benannter Verantwortung und Rotation führen.
4. [Prüfintervall] Berechtigungen mit Genehmigung und Nutzung vergleichen und Abweichungen nachhalten.

## Messung und Nachweis

Beispiel [unklare privilegierte Konten am selben Arbeitstag eskalieren]. Genehmigungs- und Entzugstickets, Kontenexport und Protokoll einer Stichprobe über Eintritt, Rollenwechsel und Austritt.', 1),

('D3-WP-ISMS-AUD', 'Audit', 'Internes Audit zur Wirksamkeit des ISMS planen', 'ISO27001',
 'ISO/IEC 27001:2022, Abschnitt 9.2; Auditprogramm und organisationsbezogene Kriterien heranziehen',
 '## Prüfziel und Grenzen

[Einheiten, Dienste und Zeitraum]. Genehmigte ISMS-Anforderungen, anwendbare Controls und vorgängige Feststellungen als Kriterien aufnehmen.

## Rollen

[Auditprogramm-Verantwortung] bestimmt Prioritäten und Umfang. Die Prüferrolle legt mögliche Interessenkonflikte offen. Fachverantwortliche liefern Unterlagen und erläutern den Betrieb.

## Prüfverfahren

1. Abdeckungsmatrix für Kriterien, Prozesse und Standorte erstellen. Für Risiken oder frühere Abweichungen gezielt vertiefte Stichproben planen.
2. Interviews durch dokumentierte Entscheidungen oder beobachtete Bedienhandlungen abgleichen. Für Controls Sollmaßstab und tatsächliches Ergebnis je Stichprobe festhalten.
3. Prüfgrenzen, fehlende Nachweise und zeitliche Lücken beschreiben. Auswirkungen auf die Schlussfolgerung nachvollziehbar begründen.
4. Feststellungen mit Kriterium, Beobachtung, Risiko und Nachweisreferenz besprechen. Zuständigkeit und Wirksamkeitsprüfung für Korrekturen vereinbaren.

## Abschluss und Ablage

Auditplan, Kriterienliste, Stichprobentabelle, Ergebnisbericht und Folgetermine versioniert hinterlegen. [Auditprogramm-Intervall] anhand von Änderungen, Risiken und Prüfergebnissen aktualisieren.', 1),

('D3-WP-NIS2-REQ', 'Requirement', 'NIS2-Betroffenheit je Einrichtung und Land dokumentieren', 'NIS2',
 'Art. 2 und Anhänge I und II NIS2; geltende nationale Umsetzung im jeweiligen Land prüfen',
 '## Geltungsbereich

[Rechtsträger, Geschäftstätigkeiten, Länder und Beteiligungen]. Leistungen je Einheit und deren Empfänger erfassen. Grenzüberschreitende Dienste gesondert betrachten.

## Verantwortung

[Compliance] führt die Entscheidungsvorlage. Fachbereiche und Finanzen liefern Leistungs- und Unternehmensdaten. Eine fachkundige Stelle prüft die zuständige Rechtsordnung und die Entscheidung.

## Vorgehen

1. Geschäftstätigkeiten und Einheiten mit den einschlägigen Sektoren und Kategorien abgleichen. Relevante Größenkriterien, Verbunddaten, Ausnahmen und Sonderfälle belegen.
2. Nationale Vorschriften, zuständige Stelle und gültige Fassung mit Fundstelle dokumentieren. Den Anwendungsstand je Einheit und Dienst begründen.
3. Für betroffene Einheiten Verantwortliche, abgeleitete Pflichten, Termine und Nachweise zuordnen. Offene Fragen mit fachlicher Wiedervorlage kennzeichnen.

## Wiedervorlage

Beispiel [Zukauf, Eintritt in einen neuen Sektor oder Änderung der Rechtsgrundlage löst erneute Prüfung aus]. Datum, Entscheidung und beteiligte Rollen erfassen.

## Nachweise

Einheitenliste, Quellen- und Versionsstand, dokumentierte Kriterien, Beschluss und Maßnahmenliste. Veränderungen der Ausgangsdaten in der nächsten Prüfung nachvollziehen.', 1),

('D3-WP-NIS2-CTRL', 'Control', 'Sicherheitsvorfälle einstufen und Meldeentscheidung nachhalten', 'NIS2',
 'Art. 23 NIS2; einschlägige nationale Meldeschwellen, Stellen und Fristen verifizieren',
 '## Geltungsbereich

[Dienste und beteiligte Einheiten], einschließlich durch Dienstleister gemeldeter Ereignisse. [24/7-Kanal oder geregelte Rufbereitschaft] und Ersatzkanal festlegen.

## Rollen

[Vorfallleitung] koordiniert technische Eindämmung und Zeitlinie. [Fach- und Rechtskoordination] bewertet Meldepflichten und Fristen. [Freigaberolle] entscheidet über externe Kommunikation.

## Ablauf

1. Eingang, Zeitpunkt der Kenntnis, Quelle und betroffene Dienste erfassen. Schwere anhand Auswirkung auf Betrieb, Kunden und Daten sowie Ausbreitungsrisiko bewerten.
2. Evidenz sichern, Kernfakten plausibilisieren und [Schwellwerte] anwenden. Meldestellen, zuständige Einheiten und einschlägige Frist entlang der tatsächlichen Betroffenheit bestimmen.
3. Meldeentscheidung mit Begründung und Wiedervorlage festhalten. Bei Meldung Erstinformation, Updates und Abschlussbericht mit Zuständigkeit und Übermittlungsbelegen nachhalten.
4. Ändern sich Auswirkung oder Kenntnisstand, Einstufung erneut prüfen. Übergabe an Wiederanlauf und Ursachenanalyse dokumentieren.

## Nachweise und Übung

Beispiel [kritische Dienstunterbrechung innerhalb von 30 Minuten intern bewerten]. Zeitlinie, Entscheidung, gesicherte Artefakte und Übungsprotokoll. [Halbjährlich] Erreichbarkeit, Vertretung und Fristsetzung üben.', 1),

('D3-WP-NIS2-RISK', 'Risk', 'Abhängigkeit von einem kritischen IKT-Dienstleister bewerten', 'BSIG',
 'Nationale Vorschriften für Cyberrisikomanagement und Lieferketten in der aktuellen Fassung zuordnen',
 '## Risikoszenario

[Lieferant und Dienst] fällt aus, meldet einen relevanten Vorfall verspätet oder verliert einen Unterauftragnehmer. Beschreiben, welche [Prozesse, Daten und Pflichten] dadurch betroffen wären.

## Analyse

1. Leistungsbezug, Vertrag, Daten- und Betriebszugriffe sowie Unterauftragnehmer dokumentieren. Einzeldienst, Plattform- oder Standortkonzentration und gemeinsame Ausfallursachen prüfen.
2. Auswirkungen nach Dauer und Umfang mit [freigegebener Skala] bewerten. Wiederanlauf, Portabilität und tatsächlich erprobte Ausweichwege mit dem Geschäftsziel vergleichen.
3. Behandlungsoptionen wie vertragliche Reaktionszeiten, Eskalation, alternative Leistung, Wiederherstellungstest oder Konzentrationsabbau begründen.

## Verantwortung und Entscheidung

[Einkauf], [Dienstverantwortung] und [Informationssicherheit] stimmen Bewertung und Maßnahmen ab. [Risikoeigner] genehmigt das Restrisiko bis [Datum] und benennt auslösende Ereignisse.

## Prüfmaßstab und Nachweise

Beispiel [kritischer Einzeldienst erfordert nachgewiesenen Wiederanlauf oder dokumentierte Übergangslösung]. Leistungsübersicht, Vertragsreferenz, Bewertung, Testprotokoll und Wiederprüfung bei wesentlicher Änderung.', 1),

('D3-WP-AI-SYSTEM', 'AiSystem', 'KI-System für den betrieblichen Einsatz erfassen und einordnen', 'AIACT',
 'Art. 3, 5 und 6 sowie Anhänge der KI-Verordnung als Prüfeinstieg; Rolle und Geltungszeitpunkt verifizieren',
 '## Systemprofil

[Betrieblicher Zweck, Modell oder Dienst, Version, Betreiber, Nutzergruppen, betroffene Personen]. Benennen, welche Entscheidungen das System vorbereitet, trifft oder beeinflusst.

## Verantwortung

[Fachverantwortung] dokumentiert Einsatzgrenzen und Nutzen. [KI-Governance] prüft Rolle, Einstufung und Pflichten. [Datenschutz und Informationssicherheit] bewerten Datenflüsse und Risiken im konkreten Kontext.

## Freigabeablauf

1. Eingänge, Ausgaben, Datenherkunft, Betriebsumgebung, Drittanbieter und menschliche Eingriffsmöglichkeiten erfassen.
2. Nutzungsart und Rechtsrolle begründen, Verbote und risikobegründende Einsatzfelder anhand der einschlägigen Fassung prüfen. Kriterien, Annahmen und offene Fragen dokumentieren.
3. Qualitätsrisiken, mögliche Benachteiligung, Fehlentscheidungen und Überwachung bewerten. Bedingungen für Inbetriebnahme, Stopp und Rückfallverfahren definieren.
4. Freigabeentscheidung mit Version, Geltungsbereich und Folgeschritten festhalten.

## Nachweise und Wiederprüfung

Systemsteckbrief, Datenfluss, Bewertungsbegründung und Testnachweise. Beispiel [eine Zweck- oder Modelländerung erfordert vor Einsatz eine erneute Bewertung durch die Fach- und Governance-Rollen].', 1),

('D3-WP-AI-CTRL', 'Control', 'Menschliche Aufsicht und sichere Nutzung eines KI-Systems organisieren', 'AIACT',
 'Art. 4 und 14 KI-Verordnung; Anwendbarkeit und konkrete Anforderungen je System und Rolle prüfen',
 '## Geltungsbereich

[KI-System, Modellstand, betrieblicher Zweck und Nutzergruppen]. Einsatzgrenzen, unerlaubte Nutzung und Fälle mit verbindlicher menschlicher Prüfung beschreiben.

## Verantwortung

[Betreibende Fachrolle] legt Freigabe- und Eskalationsregeln fest. [Aufsichtsperson] bewertet Vorschläge und kann überstimmen oder stoppen. [Betrieb] setzt Berechtigungen und Rückfallverfahren um.

## Kontrollablauf

1. Nutzende mit geeigneten Beispielen zu Leistungsgrenzen, Eingabedaten, Fehlinterpretationen und Eskalation einweisen.
2. Fälle mit [betroffenen Rechten, hoher Tragweite oder geringer Datenqualität] vor Weiterverarbeitung durch eine befugte Person prüfen lassen.
3. Abweichende Entscheidung, Grund und gegebenenfalls Stopp protokollieren. Wiederkehrende Fehlermuster an Modell- oder Prozessverantwortung melden.
4. Bei Stopp einen freigegebenen manuellen Ablauf aktivieren. Wiederaufnahme erst nach dokumentierter Prüfung.

## Messung und Nachweise

Beispiel [fünf zufällige und alle überstimmten Fälle monatlich prüfen]. Schulungsnachweis, dokumentierte Entscheidungspfade, Stichprobe und Test des Rückfallverfahrens.', 1),

('D3-WP-AI-AUD', 'Audit', 'KI-Ausgaben und Eskalation nach Modelländerung prüfen', 'AIACT',
 'KI-Verordnung, einschlägige Anforderungen an Qualität, Überwachung und Dokumentation fallbezogen bestimmen',
 '## Prüfziel

[System, bisherige und neue Modellversion, Einsatzgrenze]. Auswirkungen der Änderung vor Freigabe anhand eines dokumentierten Prüfsatzes bewerten.

## Rollen

[Fachprüfung] bestätigt realistische Erwartungen. [Unabhängige Testrolle] prüft Vergleichbarkeit, Grenzfälle und die Protokollierung. [Systemverantwortung] verantwortet Korrektur oder Rücknahme.

## Prüfverfahren

1. Repräsentative synthetische Fälle aus häufigen, seltenen und besonders folgenreichen Nutzungssituationen auswählen. Auswahl, Referenzentscheidungen und Datenrestriktionen dokumentieren.
2. Beide Versionen unter vergleichbaren Bedingungen testen. Fehlerarten, Abbrüche, Unterschiede zwischen relevanten Fallgruppen und Überstimmungen auswerten.
3. In mindestens [Anzahl] vorgesehenen Grenzfällen Warnung, menschliche Freigabe, Ablehnung und Rückfallverfahren beobachten.
4. Ergebnisse gegen die vorab genehmigten Grenzen vergleichen. Beispiel [jede unbeaufsichtigte Entscheidung in einem Testfall mit hohem Risiko stoppt die Freigabe].

## Nachweise und Abschluss

Versionierter Prüfsatz, Erwartungen, Beobachtungen, begründete Freigabeentscheidung und Wiederprüfung für offene Abweichungen. Personenbezug in Prüfdaten auf erforderliche Angaben begrenzen.', 1),

('D3-WP-CLOUD-REQ', 'Requirement', 'Verantwortung und Datenflüsse pro Cloud-Dienst festlegen', 'MSSEC',
 'Herstellerdokumentation zur geteilten Verantwortung und Leistungsbeschreibung des konkreten Dienstes prüfen',
 '## Geltungsbereich

[Cloud-Dienst, Abonnement, Mandant, Datenarten und nutzende Einheiten]. Bereitstellungsmodell, Regionen, Verwaltungszugänge und Schnittstellen erfassen.

## Zuständigkeit

[Dienstverantwortung] pflegt die Aufgabenmatrix. Beschaffung prüft Leistungs-, Support- und Ausstiegsbedingungen. Sicherheit und Datenschutz prüfen Schutzbedarf, Zugriffswege und vertragliche Anforderungen.

## Vorgehen

1. Aufgaben für Identitäten, Updates, Netzwerk, Schlüssel, Datensicherung, Protokolle und Vorfallunterstützung je Dienstebene zuordnen. Für gemeinsam verantwortete Aufgaben die Übergabe konkret beschreiben.
2. Datenflüsse, Administratorstandorte, Residenz, Sicherungsregionen und Unterauftragnehmer nachvollziehen.
3. Anbieterzusagen mit der eigenen Konfiguration vergleichen. Lücken mit Verantwortlichen und Terminen versehen.
4. Ausstieg vorbereiten. Exportformat, Zugang zu Schlüsseln, Wiederherstellbarkeit und Nachweis der Löschung vereinbaren.

## Nachweise und Prüfung

Versionierte Verantwortungsmatrix, Vertragshinweis und Architekturdiagramm. Beispiel [neue Region, neues Dienstmodell oder Änderung am Schlüsseldienst löst Neubewertung vor Produktivsetzung aus].', 1),

('D3-WP-CLOUD-CTRL', 'Control', 'Cloud-Basisregeln für Zugriffe, Protokolle und Schlüssel durchsetzen', 'MSSEC',
 'Herstellerdokumentation zu Identität, Logging und Schlüsselverwaltung der eingesetzten Cloud-Dienste heranziehen',
 '## Geltungsbereich

[Cloud-Organisation, Produktionsumgebungen und Dienste]. Verantwortete Regionen und erlaubte Schnittstellen aus der freigegebenen Architektur ableiten.

## Rollen

[Plattformteam] setzt die technische Grundkonfiguration um. [Dienstverantwortung] meldet notwendige Ausnahmen. [Sicherheit] prüft Protokolle, erhöhte Rechte und Ausnahmeablauf.

## Kontrollablauf

1. Privilegierte Zugriffe mit geeigneter Mehrfaktor-Authentisierung und [zeitlicher Begrenzung] konfigurieren. Dienstidentitäten, Notzugänge und deren Prüfroutinen erfassen.
2. Verwaltungs- und Sicherheitsereignisse in einer getrennt berechtigten Ablage sammeln. Aufbewahrung, Zeitquelle und Sichtbarkeit fehlender Logzufuhr festlegen.
3. Schlüsselverantwortung und Rotation dokumentieren. Abweichungen von freigegebenen Regionen oder öffentlichen Freigaben erkennen und binnen [Zeit] zuordnen.
4. Ausnahmen schriftlich begründen, befristen und mit einer angemessenen Kompensation verbinden.

## Prüfung und Nachweise

Konfigurationsauszug, Rollenfreigabe, Beispielereignis mit Eingang in der Protokollablage und Übersicht offener Ausnahmen. Beispiel [vierteljährlicher Nachweis der Notzugangskontrolle].', 1),

('D3-WP-CLOUD-RISK', 'Risk', 'Ausfall einer Cloud-Region oder eines zentralen Cloud-Dienstes bewerten', 'MSSEC',
 'Herstellerdokumentation zu Verfügbarkeitszonen, Regionen und Wiederherstellung je eingesetztem Dienst',
 '## Risikoszenario

[Region oder zentraler Cloud-Dienst] ist für [Dauer und Startzeitpunkt] nicht verfügbar. Betroffene Workloads, Abhängigkeiten und Auswirkungen auf Kunden oder interne Prozesse beschreiben.

## Analyse

1. Gemeinsame Abhängigkeiten für Identität, Namensauflösung, Schlüssel, Images, Konfigurationsspeicher und Administrationskanäle erfassen.
2. Anbieterzusagen, tatsächlich konfigurierte Redundanz und geübte Wiederherstellung mit [RTO, RPO und betrieblicher Zielkapazität] vergleichen.
3. Eintritt und Auswirkung mit freigegebener Risikomethode bewerten. Ausweichregion, getrennte Sicherung oder fachlichen Notbetrieb als Maßnahmen vergleichen.
4. Beim Ausweichweg Risiken für Datenresidenz, Zugriff, Integrität und Rückkehrbetrieb fachlich prüfen.

## Entscheidung und Nachweise

[Dienstverantwortung] plant Maßnahmen, [Risikoeigner] entscheidet über Restrisiko und Frist. Abhängigkeitsübersicht, Wiederherstellungstest, RTO-/RPO-Messung und genehmigten Betriebsweg hinterlegen. Beispiel [erneute Bewertung nach Architektur- oder Regionswechsel].', 1),

('D3-WP-BCM-REQ', 'Requirement', 'Geschäftsauswirkungsanalyse und Wiederanlaufziele festlegen', 'BSI2004',
 'BSI-Standard 200-4, Geschäftsauswirkungsanalyse und Kontinuitätsanforderungen; Ausgabe und internen Scope prüfen',
 '## Geltungsbereich

[Geschäftsprozess, Produkt, Standort und beteiligte Einheiten]. Bemessungsgrundlage und untersuchte Störungsdauer festhalten.

## Verantwortung

[Prozesseigner] liefert Zeit- und Auswirkungsdaten. [BCM-Koordination] gleicht Abhängigkeiten und Rangfolge ab. [Leitung] genehmigt Ziele und den Umgang mit Zielkonflikten.

## Vorgehen

1. Auswirkungen nach [Zeitfenstern], zum Beispiel 4 Stunden, einem und drei Tagen, für Kunden, Finanzen, Sicherheit und Pflichten bewerten. Betriebszeiten und saisonale Spitzen berücksichtigen.
2. Zeit bis zu einer nicht tragbaren Unterbrechung, benötigte Wiederanlaufzeit (RTO), tolerierbaren Datenverlust (RPO) und Mindestleistung begründen.
3. Personal, Systeme, Kommunikationswege, Standorte und Dienstleister mit ihren eigenen Wiederanlaufzeiten abgleichen. Konflikte und gemeinsame Engpässe sichtbar machen.
4. Anforderungen genehmigen lassen und Verantwortliche für fehlende Ausweichverfahren benennen.

## Prüfung und Nachweise

Versionierte Auswirkungsanalyse, Prozessabhängigkeiten und Beschluss. Beispiel [Zielüberschreitung eines unverzichtbaren Dienstes löst eine konkrete Maßnahme mit Termin aus]. Bei wesentlichem Prozesswechsel nachführen.', 1),

('D3-WP-BCM-CTRL', 'Control', 'Wiederanlaufplan mit Notbetrieb und Rückkehrverfahren ausarbeiten', 'BSI2004',
 'BSI-Standard 200-4, Geschäftsfortführung und Wiederanlauf; organisationseigene Ziele einbeziehen',
 '## Geltungsbereich

[Prozess, Anlagen, Dienste und Menschen], [Ausfallszenario] und [in der Auswirkungsanalyse genehmigte Ziele]. Den Plan an einem auch im Störungsfall erreichbaren Ort bereitstellen.

## Rollen

[Krisenleitung] aktiviert und beendet den Notbetrieb. [Prozesseigner] priorisiert Leistungen. [Wiederanlaufteam] setzt Maßnahmen um und protokolliert Entscheidungen und verwendete Sicherungsstände.

## Ablauf

1. Auslöser, Entscheidungsweg, Vertretung und Kontaktkette festlegen. Beispiel [bei erwarteter Überschreitung von zwei Stunden intern eskalieren].
2. Reihenfolge der Wiederherstellung aus Abhängigkeiten ableiten. Ersatzarbeitsplätze, Kommunikationswege, Sicherungszugriff, manuelle Erfassung und Mindestkapazität vorbereiten.
3. Zwischenstände, Datenkonsistenz und fachliche Mindestleistung kontrollieren. Nächste Schritte mit Zeitpunkt und verantwortlicher Rolle festhalten.
4. Geordnete Rückkehr mit Datenabgleich, Behandlung provisorischer Berechtigungen und Freigabe des Regelbetriebs dokumentieren.

## Nachweise und Prüfung

Aktueller Plan, Kontaktprüfung, Wiederherstellungsprotokoll und dokumentierte RTO-/RPO-Messung. Änderungen aus Übungen und realen Ereignissen mit Terminen nachhalten.', 1),

('D3-WP-BCM-AUD', 'Audit', 'Ausweichbetrieb und Wiederherstellung anhand einer Übung prüfen', 'BSI2004',
 'BSI-Standard 200-4, Übungen und Tests; Ressourcen, Szenarien und genehmigte Ziele festlegen',
 '## Prüfauftrag

[Prozess und Wiederanlaufplan], [Szenario, Dauer und Testumgebung]. Festlegen, welche Ziele die Übung tatsächlich misst und welche Annahmen simuliert werden.

## Verantwortung

[Übungsleitung] setzt Ereignisse und Zeitmarken. [Betrieb und Fachbereich] führen definierte Maßnahmen aus. [Beobachtung] hält Entscheidungen und Ergebnisse mit Zeitstempel fest.

## Prüfschritte

1. Abbruchkriterien, Schutz des Produktivbetriebs und Ersatzkommunikation vor der Übung abstimmen.
2. Szenario starten. Erreichbarkeit, Eskalation und Reihenfolge vorgegebenen Rollen gegenüberstellen.
3. Ausgewählte Sicherung in einer freigegebenen Umgebung wiederherstellen und fachliche Nutzbarkeit, Datenstand und technische Abhängigkeiten überprüfen. RTO und RPO mit klar festgelegtem Startpunkt messen.
4. Notbetrieb, Datenabgleich und Freigabe der Rückkehr durchspielen. Wiederkehrende Engpässe und unklare Zuständigkeiten als Befunde erfassen.

## Nachweise und Abschluss

Szenarioplan, Zeitlinie, Prüfergebnisse, Testdaten- und Systemreferenzen. Für offene Abweichungen Maßnahme, Verantwortliche, Termin und erneuten Test vereinbaren.', 1),

('D3-WP-OSS-REQ', 'Requirement', 'Open-Source-Komponenten und Lizenzpflichten erfassen', 'SPDX',
 'SPDX-Spezifikation als Austauschformat; Projektlizenzen und Pflichten für den jeweiligen Versandweg fachlich prüfen',
 '## Geltungsbereich

[Produkt, Repository, Artefakte und Auslieferungsweg]. Direkte, transitive und mitgelieferte Komponenten, Containerbasis und separat gebündelte Dateien erfassen.

## Verantwortung

[Entwicklung] liefert Herkunft, Version und Einsatzweise. [OSS-Verantwortung] prüft Vollständigkeit und Freigaberegeln. [Fachkundige Stelle] bewertet unklare Pflichten und Lizenzkonflikte.

## Vorgehen

1. Komponente mit Version, Herkunft, Identifier, Integritätsmerkmal und Abhängigkeit zum Produkt erfassen. Lizenzmeldung und festgestellte Lizenz auseinanderhalten.
2. Nutzung, Veränderung und Verbreitung anhand des tatsächlichen Lieferwegs bewerten. Pflichten, Begründung und verantwortliche Rolle in der Freigabe dokumentieren.
3. Benötigte Hinweise, Quellenbereitstellung und Texte aus den vorhandenen autorisierten Quellen vorbereiten. Ausnahmen befristen und bei Versionen oder Nutzungsarten erneut prüfen.

## Nachweise und Prüffrage

Beispiel [unbekannte Lizenz stoppt externe Auslieferung bis zur Freigabe]. Stückliste, Lizenzentscheidung, Hinweispaket und Archivkennung des ausgelieferten Artefakts auf denselben Release beziehen.', 1),

('D3-WP-OSS-CTRL', 'Control', 'Software-Stückliste als Freigabeschritt prüfen', 'CYCLONEDX',
 'CycloneDX-Spezifikation, verwendete Formatversion und Werkzeugabdeckung dokumentieren',
 '## Geltungsbereich

[Release, Build-System und ausgelieferte Artefakte]. Stückliste für genau die kontrollierte Version einschließlich relevanter eingebetteter Bibliotheken erzeugen.

## Rollen

[Build-Verantwortung] erzeugt und archiviert die Liste. [Sicherheitsverantwortung] beurteilt Befunde. [Release-Freigabe] entscheidet über befristete Ausnahmen.

## Kontrollablauf

1. Erfassung aus Build und Artefakt vergleichen. Direkte und transitive Abhängigkeiten, Paketkennungen und bekannte Grenzen des Werkzeugs festhalten.
2. Stückliste mit Produktversion, Build-Referenz und Integritätsmerkmal verbinden. Ausgelieferte Datei und Freigabestand gegen dieselbe Referenz prüfen.
3. Befunde zur tatsächlich verwendeten Version und zum Einsatzkontext bewerten. Fehlende Daten, nicht geprüfte Module und befristete Ausnahmen offen ausweisen.
4. Stückliste geschützt archivieren und Verantwortliche für nachträglich bekannt gewordene Befunde benennen.

## Freigabekriterium und Nachweise

Beispiel [kritisch bewerteter, erreichbarer Befund verlangt Freigabeentscheidung vor Auslieferung]. Build-Protokoll, Stückliste, Entscheidungen und Stichprobe einer Komponente bis zum Artefakt.', 1),

('D3-WP-OSS-MEASURE', 'Measure', 'Befund in einer Software-Abhängigkeit beheben und erneut bewerten', 'CYCLONEDX',
 'CycloneDX-Stückliste und Originalmeldung zur genauen Komponentenversion als Arbeitsreferenz',
 '## Anlass und Geltungsbereich

[Befund und betroffene Komponente], [konkrete Releases und Einsatzumgebungen]. Die Betroffenheit anhand Version, Konfiguration und Verwendung nachvollziehbar eingrenzen.

## Maßnahme

1. [Komponentenverantwortung] entscheidet zwischen Update, Ersetzen, Entfernen oder befristeter Kompensation. Abhängigkeiten und verbleibendes Risiko dokumentieren.
2. Änderung in [freigegebener Umgebung] testen und geordnete Auslieferung planen. Bei gestaffelter Einführung betroffene Restbestände ausweisen.
3. Neue Stückliste erzeugen und die tatsächlich verteilte Fassung kontrollieren. Die Behebung mit Regressionstest und aktualisierter Befundbewertung belegen.

## Verantwortung und Frist

[Korrekturrolle], [Freigaberolle], [Zieltermin]. Beispiel [Fristüberschreitung bei kritischem und erreichbarem Befund am gleichen Arbeitstag an den Risikoeigner eskalieren].

## Abschlussnachweise

Änderungsreferenz, Testergebnis, Artefakt- und Stücklistenreferenz, Rolloutnachweis sowie verbleibende Ausnahmen mit Enddatum. Eine unabhängige Rolle bestätigt die wirksame Korrektur.', 1),

('D3-WP-DORA-REQ', 'Requirement', 'DORA-Anwendungsbereich und IKT-Abhängigkeiten festhalten', 'DORA',
 'Art. 2, 5 und 6 DORA als Einstieg; Anwendbarkeit, Rollen und einschlägige Konkretisierungen prüfen',
 '## Geltungsbereich

[Finanzunternehmen, Rechtsträger, Funktionen und Standorte]. Gemeinsam oder ausgelagert bereitgestellte IKT-Dienste und Verantwortungsgrenzen dokumentieren.

## Verantwortung

[Compliance] prüft die Anwendbarkeit für jede Einheit. [IKT-Risikomanagement] erhebt Abhängigkeiten und Risiken. [Leitungsorgan oder delegierte Rolle] entscheidet entsprechend der festgelegten Verantwortung.

## Vorgehen

1. Einrichtungsart, Ausnahmen und anwendbare Regeln anhand der aktuellen Rechtsquellen begründen. Abweichende Zuständigkeiten oder besondere Anforderungen kennzeichnen.
2. Funktionen, deren Kritikalität, eingesetzte IKT, Datenflüsse und Dienstleister erfassen. Konzentrationen und gemeinsam genutzte Standorte und Dienste sichtbar machen.
3. Pflichten an Verantwortliche, Controls und Nachweise zuordnen. Lücken, Entscheidungen und Folgeprüfungen mit Termin und Eskalationsweg dokumentieren.

## Prüfung und Nachweise

Einheitenliste, Rechtsstand, Abhängigkeitsmatrix und genehmigter Umsetzungsplan. Beispiel [neue Finanzdienstleistung, Auslagerung einer kritischen Funktion oder wesentliche Änderung löst Neubewertung aus].', 1),

('D3-WP-DORA-SUPPLIER', 'Supplier', 'IKT-Dienstleister für eine kritische Funktion beurteilen', 'DORA',
 'Art. 28 bis 30 DORA; konkrete Pflichten, technische Standards und erforderliches Informationsregister prüfen',
 '## Geltungsbereich

[Dienstleister, Vertragsreferenz, IKT-Dienst, unterstützte Funktion und nutzende Einheiten]. Zweck und Kritikalität des Leistungsbezugs begründen.

## Verantwortung

[Einkauf] stellt Vertragsunterlagen bereit. [Dienstverantwortung] bewertet Leistung und Betriebsabhängigkeit. [IKT-Risiko und Compliance] beurteilen Schutzbedarf und anwendbare Anforderungen.

## Prüfung

1. Umfang, Standorte, Datenzugriff, Unterauftragnehmer und Konzentrationsrisiko erfassen. Abhängigkeiten mit dem Wiederanlaufziel der unterstützten Funktion abgleichen.
2. Vertragliche Regelungen zu Leistung, Vorfällen, Zugang, Prüfung und Beendigung anhand konkreter Fundstellen bewerten. Abweichungen mit Maßnahme und Frist festhalten.
3. Wechsel oder Rückführung einschließlich Datenexport, Übergangsleistung und getesteter Wiederherstellung beurteilen.
4. Entscheidung, Bedingungen und nächsten Prüftermin dokumentieren. Beispiel [wesentliche Leistungs- oder Unterauftragnehmeränderung löst vorab eine Neubewertung aus].

## Nachweise

Leistungs- und Vertragsbezug, Risikoprüfung, relevante Tests und genehmigte Maßnahmen. Die Ergebnisse als Arbeitsgrundlage für einschlägige Register- und Berichtspflichten verwenden.', 1),

('D3-WP-DORA-AUD', 'Audit', 'IKT-Wiederherstellung für eine kritische Funktion prüfen', 'DORA',
 'Art. 11, 24 und 25 DORA; Anforderungen an Testprogramm und Umfang für die konkrete Einheit prüfen',
 '## Prüfauftrag

[Finanzfunktion], [Dienste, Daten und externe Mitwirkung], [Szenario und Testdatum]. Umfang und Kriterien vor dem Test genehmigen.

## Rollen

[Testleitung] erstellt Ablauf und Schutzmaßnahmen. [Wiederanlaufteam und Dienstleister] führen die genehmigte Übung aus. [Unabhängige Prüfrolle] beurteilt die Ergebnisse.

## Prüfschritte

1. Ausgangszustand, Abhängigkeiten, Wiederanlauf- und Datenverlustziele und Mindestleistung dokumentieren. Stopp- und Eskalationskriterien vereinbaren.
2. Wiederherstellung beobachten. Zeitpunkt, Datenstand, Integrität, Rechte und verfügbare Schnittstellen gegen die genehmigten Ziele prüfen.
3. Betrieb mit Ersatzwegen, Vorfallinformation und Übergabe zwischen internen und externen Rollen anhand beobachteter Reaktionen bewerten.
4. Abweichungen nach ihrer Auswirkung auf die Funktion priorisieren. Korrektur, verantwortliche Rolle, Termin und Wiederholungstest festhalten.

## Nachweise und Abschluss

Genehmigtes Szenario, Zeitmessung, Datenintegritätsprüfung, Schnittstellentest und Korrekturliste. Beispiel [überschrittene Ziele für eine kritische Funktion an die planverantwortliche Leitung eskalieren].', 1),

('D3-WP-GOV-REQ', 'Requirement', 'GRC-Verantwortung, Delegation und Entscheidungswege festlegen', 'INTERNAL',
 'Interne Ordnung, Beschluss oder Richtlinie mit aktuellem Versionsstand zuordnen',
 '## Geltungsbereich

[Gesellschaften, Organisationseinheiten, Standorte und Themen]. Verbindliche Regelungen und lokale Konkretisierungen mit Geltungszeitraum und zuständiger Stelle erfassen.

## Verantwortung

[Leitung] setzt Prioritäten und genehmigt Risiken oberhalb delegierter Grenzen. [GRC-Koordination] betreut Quellen, Zuordnungen und Termine. Fachverantwortliche setzen Maßnahmen um und pflegen Nachweise.

## Vorgehen

1. Entscheidungsbefugnis, Stellvertretung, Ausführung und Kontrolle je Vorgang im Arbeitsablauf zuordnen.
2. Einheitenspezifische Abweichungen mit Grund, Umfang, Gültigkeit und Kompensation erfassen. Bei Rollenüberschneidungen eine geeignete zusätzliche Prüfung festlegen.
3. Anforderungen und Ausnahmen mit verantworteten Controls, Risiken und Maßnahmen verbinden. Eskalationen für fehlende Entscheidung und Fristüberschreitung einrichten.

## Messung und Nachweise

Beispiel [wesentliches nicht behandeltes Risiko binnen fünf Arbeitstagen an die Leitung geben]. Genehmigter Geltungsbereich, Rollenübersicht, Beschluss, Ausnahmeliste und Stichprobe eines tatsächlichen Entscheidungswegs.', 1),

('D3-WP-GOV-CTRL', 'Control', 'Interne Richtlinien erstellen, freigeben und fortschreiben', 'INTERNAL',
 'Interne Dokumentenlenkung mit zuständiger Freigaberolle und Ablageort zuordnen',
 '## Geltungsbereich

[Richtlinienfamilie, Abteilungen und Adressaten]. Inhaltliche Verantwortung, Freigabe, Veröffentlichung und Aufbewahrung je Fassung bestimmen.

## Verantwortung

[Dokumentenverantwortung] erstellt Entwurf und Änderungsbegründung. [Fachprüfung] beurteilt Inhalt und Auswirkungen auf betroffene Prozesse. [Freigaberolle] bestätigt genau die zu veröffentlichende Fassung.

## Ablauf

1. Zweck, Reichweite, erforderliche Rollen und konkrete Regelungen mit überprüfbaren Ergebnissen formulieren. Verweise auf betroffene Controls und Quellen festhalten.
2. Fachbereiche einbinden, Einwände oder Ausnahmen dokumentieren und den geprüften Entwurf zur Entscheidung vorlegen.
3. Freigabe mit Version, Inhalt, Datum, Gründen und Adressaten dokumentieren. Wirksamkeitsbeginn und Kommunikationsweg bekannt machen.
4. Änderungsbedarf gesammelt erfassen und vor Veröffentlichung einer Folgefassung erneut prüfen. Frühere Freigaben und Geltungszeiträume nachvollziehbar halten.

## Prüfung und Nachweise

Beispiel [jährliche Wiedervorlage und zusätzliche Prüfung nach wesentlicher Rechts- oder Prozessänderung]. Freigabeprotokoll, veröffentlichte Fassung, Verteiler und Prüfergebnis.', 1),

('D3-WP-GOV-MEASURE', 'Measure', 'Festgestellte Abweichung korrigieren und Wirksamkeit belegen', 'INTERNAL',
 'Interne Regel zur Behandlung von Abweichungen und Korrekturmaßnahmen zuordnen',
 '## Ausgangspunkt

[Feststellung, Kriterium, betroffener Prozess und beobachteter Umfang]. Sofortmaßnahme, Ursachenanalyse und dauerhafte Korrektur als einzelne Arbeitsschritte planen.

## Rollen

[Maßnahmenverantwortung] plant und setzt um. [Fachverantwortung] bestätigt die Abdeckung des betroffenen Bereichs. Eine geeignete Prüferrolle bewertet die Wirksamkeit.

## Ablauf

1. Auswirkungen begrenzen, Wiederholungsmöglichkeiten und gemeinsame Ursachen untersuchen. Entscheidung über vorläufige Kompensationen dokumentieren.
2. Tätigkeit, Verantwortliche, [Zieltermin] und überprüfbares Erfolgskriterium vereinbaren. Abhängige Controls und betroffene Einheiten einbeziehen.
3. Umsetzung durch tatsächliche Artefakte und Beobachtungen belegen. Zur Wirksamkeit nach angemessenem Zeitraum eine unabhängige Stichprobe ziehen.
4. Bei erneuter Abweichung Ursache und Maßnahme korrigieren und den Termin erneut begründen. Verbleibendes Risiko mit zuständiger Freigabe festhalten.

## Abschluss und Nachweise

Ursachenentscheidung, Arbeitsnachweis und Prüfprotokoll referenzieren. Beispiel [Maßnahme erst nach aussagefähiger Stichprobe mit belegtem Kriterium als wirksam bewerten].', 1),

('D3-DOC-ISMS', 'Policy', 'Informationssicherheitsrichtlinie', 'ISO27001',
 'ISO/IEC 27001:2022, Abschnitte 4 bis 10 als Bezugsrahmen; Anforderungen und eigene Kontrollauswahl im Scope prüfen',
 '## Freigabe und Geltungsbereich

Diese Richtlinie beschreibt den Schutz von Informationen und Diensten bei [Organisation]. Sie gilt für [Gesellschaften, Standorte, Prozesse und Informationssysteme] sowie für externe Personen und Dienste, soweit ihr Aufgaben- und Vertragsumfang betroffen ist.

| Dokumentverantwortung | Freigabe | Gültig ab | Nächste Überprüfung |
| --- | --- | --- | --- |
| [ISMS-Verantwortung] | [Leitungsrolle und Datum] | [Datum] | [Datum oder Ereignis] |

Die Geltungsgrenzen einschließlich ausgelagerter Leistungen, gemeinsamer Dienste und Übergaben werden in [Scope-Dokument] begründet. Für betroffene Verträge und widersprüchliche Vorgaben bestimmt [Fachrolle] die erforderliche Behandlung und hält die Entscheidung fest.

## Verantwortung

| Tätigkeit | Federführung | Kontrolle oder Entscheidung |
| --- | --- | --- |
| Schutzziele und Risikogrenzen festlegen | [ISMS-Verantwortung] | [Leitung] |
| Werte erfassen, einstufen und schützen | [Fachverantwortung] | [Dienstverantwortung] |
| Technische Controls betreiben | [IT-Betrieb] | [Informationssicherheit] |
| Wirksamkeit und Ausnahmen beurteilen | [Unabhängige Prüfrolle] | [Risikoeigner] |

Die Verantwortlichen benennen eine Stellvertretung, die bei Störungen auf Status, Kontaktwege und notwendige Entscheidungsunterlagen zugreifen kann.

## Verbindliche Anforderungen im gewählten Scope

1. Fachverantwortliche erfassen Informationen, Dienste und wesentliche Abhängigkeiten in [Verzeichnis]. Sie ordnen eine Schutzklasse und eine verantwortliche Rolle zu. Umgang, Übermittlung und Aufbewahrung richten sich nach der genehmigten Klasse.
2. Betriebsverantwortliche stellen Zugriffe entsprechend dem Arbeitsbedarf bereit. Neue und geänderte Rechte werden genehmigt, mit Enddatum versehen, soweit befristet, und bei Rollenwechsel oder Austritt überprüft. Privilegierte Zugänge unterliegen [MFA- und Protokollierungsregel].
3. Änderungen an wesentlichen Diensten werden mit Risikoabschätzung, Test und Rückfallablauf freigegeben. Betrieb und Sicherheit vereinbaren Protokollierung, Sicherungsumfang und Wiederherstellungstests je Dienst.
4. Sicherheitsereignisse werden über [Meldekanal] erfasst. [Incident-Koordination] bewertet Auswirkung und Dringlichkeit, benennt die nächste Entscheidung und trägt geschäftliche oder externe Meldeerfordernisse mit der Fachstelle zusammen.
5. Sicherheitsmaßnahmen, Zuständigkeiten und zulässige Nutzungswege werden bei Aufnahme der Tätigkeit und bei wesentlichen Änderungen vermittelt. Fachbereiche halten die Durchführung der Information nachvollziehbar fest.

## Risiken, Grenzen und Ausnahmen

Risiken werden nach [genehmigter Skala] anhand Szenario, betroffener Werte, vorhandener Controls und möglicher Auswirkung bewertet. Behandlung, Prüftermin und verbleibendes Risiko werden dokumentiert. Beispiel [ab Stufe hoch entscheidet die Leitung vor Inbetriebnahme, privilegierte Rechte werden quartalsweise überprüft]. Diese Werte sind vor Freigabe durch die zuständigen Rollen festzulegen.

Eine Ausnahme enthält Anforderung, Grund, Geltungsbereich, kompensierende Maßnahme, verantwortliche Rolle und Enddatum. [Risikoeigner] bewertet Folgen und genehmigt innerhalb der delegierten Grenze. Bei Ablauf bestätigt die zuständige Stelle die Rückkehr zum Regelbetrieb oder entscheidet erneut.

## Freigabeprüfung und laufende Kontrolle

| Prüfschritt | Beleg | Ergebnis |
| --- | --- | --- |
| Scope und Verantwortungen abgestimmt | [Scope und Rollenübersicht] | [offen/freigegeben] |
| Ziele, Eskalationswege und Grenzwerte vollständig | [Vorgaben und Liste der Controls] | [offen/freigegeben] |
| Ausnahmen und offene Risiken eingeplant | [Beschlüsse, Termine und Zuständigkeiten] | [offen/freigegeben] |
| Fassung veröffentlicht und bekannt gemacht | [Datum und Ablage] | [offen/freigegeben] |

[ISMS-Verantwortung] berichtet [Rhythmus] über offene Risiken, überfällige Ausnahmen, Vorfälle und Ergebnisse von Kontrollstichproben. Wesentliche Änderungen an Geschäft, Rechtsrahmen oder Technik führen zur erneuten Prüfung mit Änderungsgrund, Dokumentdatum und Freigabe.', 1),

('D3-DOC-PRIVACY', 'Policy', 'Datenschutzrichtlinie für den betrieblichen Umgang mit personenbezogenen Daten', 'GDPR',
 'Art. 5, 6, 12 bis 22 und 24 bis 39 DSGVO als Arbeitsbezug; weitere anwendbare Vorschriften prüfen',
 '## Freigabe, Ziel und Geltung

Diese Richtlinie legt fest, wie [Organisation] personenbezogene Daten in [Gesellschaften, Tätigkeiten und Standorten] erhebt, verwendet, weitergibt, aufbewahrt und löscht. Sie gilt für die genutzten Arbeitsmittel, Anwendungen und die durch Dienstleister ausgeführten Verarbeitungsschritte im festgelegten Bereich.

| Dokumentverantwortung | Freigabe | Gültig ab | Nächste Prüfung |
| --- | --- | --- | --- |
| [Datenschutzkoordination] | [Leitungsrolle und Datum] | [Datum] | [Datum] |

## Rollen

| Fachaufgabe | Federführung | Beteiligung oder Freigabe |
| --- | --- | --- |
| Zweck und Datenfluss dokumentieren | [Prozesseigner] | [Datenschutzkoordination] |
| Schutzmaßnahmen umsetzen | [Systemverantwortung] | [Informationssicherheit] |
| Auskunft, Löschung und weitere Anfragen bearbeiten | [Anlaufstelle] | [Datenschutz und Fachbereich] |
| Datenschutzvorfälle bewerten | [Vorfallkoordination] | [zuständige Entscheidungsrolle] |

Die Datenschutzbeauftragten werden einbezogen, soweit ihre Aufgaben betroffen sind. Die fachliche Verantwortung für die Verarbeitung und ihre Freigabe bleibt in der dafür zuständigen Linie. Für Fristen werden erreichbare Vertretungen benannt.

## Vorgaben für Verarbeitungen

1. Vor Beginn einer Tätigkeit Zweck, Datenarten, Personengruppen, Empfänger, Systeme, Rollen und begründete Rechtsgrundlage im [Verzeichnis] erfassen. Festlegen, welche Felder benötigt werden und wie betroffene Personen informiert werden.
2. Zugriff, Übertragung und Ablage nach Schutzbedarf gestalten. Der Fachbereich überprüft die tatsächlich verwendeten Datenfelder, Standardberechtigungen, Kopien und Exportwege. [Ablageort] enthält die freigegebene Aufbewahrungs- und Löschregel für jede relevante Kategorie.
3. Bei neuen Empfängern, Auftragsverarbeitung und Übermittlungen in Drittländer die tatsächlichen Rollen, Vertragspflichten und Voraussetzungen fachkundig bewerten. Freigabe, Vertragsbezug und Auflagen vor Beginn nachvollziehbar dokumentieren.
4. Bei Änderungen von Zweck, Datenumfang oder Technik das Risiko für betroffene Personen neu bewerten. Bedarf einer Datenschutz-Folgenabschätzung und notwendige Maßnahmen nach [Prüfprozess] dokumentieren.

## Rechte und Vorfälle

Anfragen gehen über [Eingang und Ersatzkontakt] ein. [Anlaufstelle] erfasst Eingangszeit und Art, prüft die Identität verhältnismäßig und ermittelt die zutreffende Frist. Fachbereiche recherchieren und sichern die benötigten Angaben. Datenschutz prüft Umfang, weitere Rechte und sichere Übermittlung. Die Antwort und der Zeitpunkt ihrer Zustellung werden dokumentiert.

Mögliche Verletzungen werden über [Meldeweg] unverzüglich intern gemeldet. Vorfallkoordination führt Zeitlinie und Fakten, veranlasst Schutzmaßnahmen und klärt mit den zuständigen Rollen die rechtlichen Melde- und Informationspflichten. Entscheidung, Begründung, Frist und mögliche Meldung werden fallbezogen dokumentiert.

## Ausnahmen und Prüfmaßstäbe

Abweichungen von internen Prozessvorgaben benötigen Umfang, Begründung, Schutzmaßnahme und genehmigte Dauer. Die fachkundige Stelle bewertet den rechtlichen Rahmen und legt bei Bedarf eine andere Vorgehensweise fest. Beispiel [interne Erinnerung bei halber Anfragelaufzeit und Eskalation fünf Arbeitstage vor Fristablauf]. Interne Zeitwerte an konkrete gesetzliche und vertragliche Fristen anpassen.

## Freigabeprüfung und Weiterentwicklung

| Freigabepunkt | Erwarteter Beleg | Ergebnis |
| --- | --- | --- |
| Verantwortliche und Vertretung je Vorgang benannt | [Rollen- und Kontaktliste] | [offen/freigegeben] |
| Zwecke, Datenwege und Löschregeln nachvollziehbar | [Verzeichnis und Nachweisstichprobe] | [offen/freigegeben] |
| Frist- und Vorfallprozess erprobt | [Testfall und Zeitlinie] | [offen/freigegeben] |
| Referenzen auf interne Richtlinien aktuell | [Versionsstand] | [offen/freigegeben] |

[Datenschutzkoordination] prüft die Fassung [Intervall] und bei Änderungen der Verarbeitung, des Rechtsrahmens oder nach relevanten Vorfällen. Ergebnisse, genehmigte Ergänzungen und Beschlüsse geschützt und auf erforderliche Angaben begrenzt ablegen.', 1),

('D3-DOC-BCM', 'Policy', 'Richtlinie für Geschäftskontinuität und Wiederanlauf', 'BSI2004',
 'BSI-Standard 200-4 als Rahmen für Business Continuity Management; interne Verfahren und Ziele konkretisieren',
 '## Geltungsbereich und Freigabe

[Organisation] hält geschäftskritische Leistungen in vereinbartem Mindestumfang aufrecht und stellt sie nach Unterbrechungen geordnet wieder her. Die Richtlinie gilt für [Einheiten, Produkte, Standorte und Dienste] mit ihren internen und externen Abhängigkeiten. Der gewählte Umfang und begründete Abgrenzungen sind in [Scope-Dokument] festgehalten.

| Dokumentverantwortung | Freigabe | Gültig ab | Nächste Prüfung |
| --- | --- | --- | --- |
| [BCM-Koordination] | [Leitungsrolle und Datum] | [Datum] | [Datum] |

## Zuständigkeiten

| Aufgabe | Federführung | Beteiligung oder Entscheidung |
| --- | --- | --- |
| Auswirkungen und Mindestleistung bewerten | [Prozesseigner] | [BCM-Koordination] |
| Ziele und Prioritäten entscheiden | [Leitung] | [Fachbereich und Betrieb] |
| Notbetrieb aktivieren und koordinieren | [Krisenleitung] | [Kommunikation und Fachverantwortung] |
| Sicherungen und Dienste wiederherstellen | [Wiederanlaufteam] | [Fachbereich und Dienstleister] |

Vertretungen und Erreichbarkeit werden regelmäßig überprüft. Notfallunterlagen und Kontaktwege müssen im geplanten Ausfallszenario zugänglich sein, einschließlich [Ersatzablage und Ersatzkommunikation].

## Planung und Durchführung

1. Für kritische Prozesse die Folgen einer Unterbrechung nach [Zeitfenstern] einordnen. Mindestleistung, gewünschte Wiederanlaufzeit (RTO) und tolerierten Datenverlust (RPO) anhand Kundenbedarfs, Verpflichtungen und verfügbarer Ressourcen festlegen.
2. Kritische Abhängigkeiten erfassen und ihre eigenen Wiederherstellungszeiten gegen die Prozessziele prüfen. Dafür Personal, Räume, Daten, Kommunikation, Identitäten, Systeme und Drittanbieter berücksichtigen.
3. Für priorisierte Szenarien die Aktivierung, Zuständigkeit, Ablaufreihenfolge und Ersatzwege dokumentieren. Beispiel [bei erwarteter Zielüberschreitung innerhalb von 30 Minuten an die Krisenleitung eskalieren]. Zahlenwerte in Abstimmung mit den Prozessen festlegen.
4. Bei Aktivierung Zeitlinie, getroffene Entscheidungen, verfügbare Kapazität und Wiederherstellungsstand laufend dokumentieren. Änderungen an Prioritäten anhand Auswirkungen und Ressourcen begründen.
5. Vor Rückkehr in den Regelbetrieb Datenstände abgleichen, provisorische Arbeitswege und Rechte auflösen sowie das Ergebnis fachlich freigeben. Offene Abweichungen werden mit Verantwortlichen und Terminen nachgehalten.

## Zielkonflikte, Ausnahmen und Tests

Bei einer Lücke zwischen Geschäftsziel und erreichbarer Wiederanlaufleistung bewertet [Risikoeigner] erwartete Auswirkungen, Übergangslösung und Termin zur Korrektur. Abweichungen von Testintervallen und Wiederherstellungszielen werden befristet, begründet und durch die zuständige Rolle genehmigt. Kritische Überschreitungen führen zur Entscheidung durch [Leitung].

[BCM-Koordination] plant Übungen nach [Risikoklasse und Intervall]. Tests definieren Ausgangszustand, erwarteten Datenstand, Mindestleistung, Abbruchkriterien und Freigabeverantwortung. Prüfer vergleichen beobachteten Zeitpunkt und Datenstand mit den Zielen und bestätigen die Nutzbarkeit im Fachprozess.

## Freigabeprüfung und Wiedervorlage

| Freigabeprüfung | Nachweis | Ergebnis |
| --- | --- | --- |
| Geschäftsziele und Abhängigkeiten abgestimmt | [Auswirkungsanalyse und Beschluss] | [offen/freigegeben] |
| Kontaktkette und Ersatzwege geprüft | [Kontaktprüfung und Übungsprotokoll] | [offen/freigegeben] |
| Wiederherstellung und Rückkehr bewertbar | [Messwerte, Nutzbarkeit und Freigabe] | [offen/freigegeben] |
| Abweichungen mit Fristen und Folgetest eingeplant | [Maßnahmenübersicht] | [offen/freigegeben] |

Nach Übungen, realen Unterbrechungen und wesentlichen Prozess- oder Lieferantenänderungen prüft die Dokumentverantwortung die Gültigkeit. Die nächste reguläre Wiedervorlage erfolgt [Intervall] mit Stand, Beschlüssen und Nachweisen.', 1),

('D3-DOC-SUPPLIER', 'Policy', 'Richtlinie zur Steuerung externer Lieferanten und Dienstleister', 'INTERNAL',
 'Interne Beschaffungs- und Risikoregeln zuordnen; DSGVO, NIS2 oder DORA bei festgestellter Relevanz gesondert prüfen',
 '## Zweck, Reichweite und Freigabe

Diese Richtlinie regelt Auswahl, Beauftragung, laufende Bewertung und geordnete Beendigung externer Leistungen bei [Organisation]. Der Scope umfasst [Einheiten und Vertragsarten] sowie eingebundene Unterauftragnehmer, soweit ihre Leistung für Betrieb, Informationsschutz oder Verpflichtungen der Organisation relevant ist.

| Dokumentverantwortung | Freigabe | Gültig ab | Nächste Prüfung |
| --- | --- | --- | --- |
| [Lieferantenmanagement] | [Leitungsrolle und Datum] | [Datum] | [Datum] |

## Rollen im Lebenszyklus

| Aufgabe | Federführung | Beteiligung oder Entscheidung |
| --- | --- | --- |
| Bedarf, Leistungsumfang und Alternativen bestimmen | [Dienstverantwortung] | [Fachbereich] |
| Vertrags- und Registerdaten pflegen | [Einkauf] | [Vertragsverantwortung] |
| Risiken, Kontrollen und Nachweise bewerten | [Informationssicherheit oder Risikofunktion] | [Datenschutz und Compliance] |
| Restrisiko und Ausnahmen genehmigen | [Risikoeigner] | [Leitung oberhalb delegierter Grenze] |

## Beauftragung und laufende Steuerung

1. Vor Beauftragung Leistungsumfang, unterstützte Prozesse, Leistungsorte, Zugriff, Datenarten, Unterauftragnehmer und verantwortliche Rolle dokumentieren. Alternativen, Austauschbarkeit und Konzentration auf einzelne Anbieter oder Plattformen beurteilen.
2. Kritikalität und Risikobegründung anhand [Bewertungskriterien] im Lieferantenregister festhalten. Beispiel [hohe oder kritische Einstufung setzt eine dokumentierte Fachprüfung und Freigabe vor Vertragsabschluss voraus]. Abgeleitete Controls, Risiken und Maßnahmen nachvollziehbar zuordnen.
3. Verträge mit der zuständigen Fachstelle auf Leistungsniveau, Meldewege, Schutzanforderungen, Prüf- und Zugangsrechte, Unterauftragnehmer und Beendigung prüfen. Rechtliche Register- oder Meldepflichten für den konkreten Anwendungsfall bestimmen. Entscheidungen an Vertragsreferenz und Fassung binden.
4. Leistung, Sicherheitsvorfälle, Änderungen und Nachweise im vereinbarten Rhythmus bewerten. Wesentliche Leistungs- oder Unterauftragnehmeränderungen lösen eine erneute Bewertung aus. Ergebnisse mit Freigabe, Bedingungen, Maßnahmen und Folgetermin erfassen.
5. Vor Ende der Leistung Übergabe, Datenexport, Zugriffsentzug und Löschung klären. Bei kritischen Diensten Wiederherstellung oder Anbieterwechsel nach [Testumfang] erproben. Für offene Abhängigkeiten einen verantworteten Übergangsplan hinterlegen.

## Eskalation und Ausnahmen

Bei Leistungsunterbrechung, Sicherheitsereignis oder wesentlicher Abweichung erfasst [Verantwortung] Zeitpunkt, Auswirkung und nächste Maßnahme. [Eskalationsrolle] entscheidet über fortgesetzten Betrieb, Auflagen oder Beendigung. Beispiel [kritische Dienstleister halbjährlich, sonstige nach genehmigtem Prüfplan bewerten und bei negativem Ergebnis innerhalb von fünf Arbeitstagen die Behandlungsentscheidung vorlegen]. Zeitwerte an Leistungsumfang und geltende Vorgaben anpassen.

Für Ausnahmen werden Umfang, Grund, betroffene Leistung, kompensierende Controls, Restrisiko und Enddatum dokumentiert. Genehmigungen erfolgen nach Kompetenzordnung. Vor Ablauf wird der Ausnahmezustand beendet, korrigiert oder erneut geprüft.

## Freigabeprüfung und Fortschreibung

| Freigabeprüfung | Nachweis | Ergebnis |
| --- | --- | --- |
| Leistung, Eigner und Kritikalität erfasst | [Registereintrag und Bewertung] | [offen/freigegeben] |
| Vertragliche Regelungen geprüft | [Vertragsreferenz und konkrete Fundstellen] | [offen/freigegeben] |
| Betriebs- und Ausstiegsvorsorge belegt | [Übung, Test oder begründeter Plan] | [offen/freigegeben] |
| Maßnahmen, Folgetermin und Eskalation festgelegt | [Beschluss und Prüfplan] | [offen/freigegeben] |

[Lieferantenmanagement] bewertet [Intervall] überfällige Prüfungen, Konzentrationen, offene Auflagen und wirksame Kontrollen anhand von Stichproben. Vorfälle, Änderungen der Lieferkette oder neue Anforderungen führen zur vorgezogenen Überprüfung dieser Richtlinie.', 1)
ON CONFLICT(Id) DO NOTHING;
