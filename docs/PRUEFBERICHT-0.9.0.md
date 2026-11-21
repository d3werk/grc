# D3 GRC 0.9.0 · Dokumente und Textvorlagen

Die Startseite und die Plattformübersicht von Swiss GRC waren durch den Cloud-Proxy gesperrt. Die folgende Bestandsaufnahme dokumentiert die Funktionen von D3 GRC und ihre Grenzen. Eine Zuordnung sämtlicher Angebote der verlinkten Plattform erfordert deren konkrete Modulübersicht. [Swiss GRC Startseite](https://swissgrc.com/) · [Swiss GRC Plattform](https://swissgrc.com/plattform/)

## Funktionsabdeckung

| Arbeitsbereich | Umgesetzt in D3 GRC | Stand für weitergehenden Einsatz |
| --- | --- | --- |
| Unternehmensprofil | Projektanlage nach Registrierung, Größe, Branche, Standorte, Organisationseinheiten und begründeter Umsetzungsplan. | Eigenständige Gesellschafts- und Standortstammdaten sowie projektübergreifende Rollen fehlen. |
| Governance und Dokumentenlenkung | Richtlinien, Verfahren und Arbeitsanweisungen im verschlüsselten Projekt. Entwurf, Prüfung, Freigabe, Versionsvorschau und Außerkraftsetzen mit lokalem Journal. | Rollenbasierte Freigaben über mehrere Arbeitsplätze und Kenntnisnahme-Kampagnen sind offen. |
| Anforderungen, IKS und Nachweise | Anwendbarkeit, belegte Quellen, Zuordnung zu Controls, begründete Abdeckung und zeitlicher Nachweisstatus. Dokumente können Anforderungen und Controls referenzieren. | Kontrollkalender, automatisierte Tests und zentrales Vier-Augen-Prinzip sind offen. |
| Risikomanagement und Maßnahmen | Verantwortliche, Geltungsbereiche, Fristen und typisierte Beziehungen zu Controls, Lieferanten und Findings. | Quantitative Matrix, Szenariovergleich und Portfolioauswertung stehen aus. |
| Informationssicherheit und IT | Thematischer Quellenkatalog, ISMS-Arbeitstexte, Maßnahmen, Kontrollbezüge und Nachweise. | Inventar- und Monitoring-Integrationen sowie ein eigener Incident-Workflow stehen aus. |
| Datenschutz und KI | Quellen, Anforderungen, KI-Register und Arbeitstexte für Datenerhebung, Betroffenenanfragen, Folgen- und Rollenprüfungen. | Für normierte Fachregister, Aufbewahrungsautomatiken und Fristenverwaltung je Fall fehlen eigene Workflows. |
| Lieferanten und Resilienz | Leistungen, Kritikalität, Verträge, Folgetermine und historisierte Bewertungen. BCM-Bezüge für Controls und Prüfszenarien. | Lieferantenportal, vollständig strukturiertes DORA-Informationsregister und dedizierte BIA- und Wiederanlaufplanung stehen aus. |
| Audits und Findings | Prüf-Scope, Nachweise, Kriterienstand, Testresultate und nachvollziehbare Findings. | Ressourcenplanung über Projekte und mehrstufiger zentraler Review sind offen. |
| Bericht und Zusammenarbeit | Filterbezogene Übersicht, XLSX und HTML mit Dokumentenmetadaten, lokale Dateiablage mit Verschlüsselung und Projektsperre. | Zentraler Mehrbenutzerbetrieb, SSO, Mandanten und Freigaberollen stehen aus. |

## Neuer Dokumentenablauf

1. Unter **Dokumente** einen Entwurf anlegen oder unter **Textvorlagen** eine Vorlage mit **Als Dokument verwenden** übernehmen.
2. Fachthema, Geltungsbereich, Kategorie und verantwortliche Rolle festhalten. Markdown-Überschriften, Listen, Tabellen und Codeblöcke werden nativ angezeigt. Im Entwurf können referenzierte Anforderungen und Controls zugeordnet werden.
3. Den Entwurf einreichen, die Prüfung begründen und die Fassung veröffentlichen. Zur weiteren Bearbeitung aus der veröffentlichten Fassung einen neuen Entwurf erstellen. Die vorherige Veröffentlichung bleibt mit Prüf- und Freigabevermerk lesbar. Im Reiter „Bezüge“ steht der aktuell gepflegte Verknüpfungsstand.
4. Die Bestandsübersicht berücksichtigt Thema, Geltungsbereich und Suche. Der allgemeine Export enthält die Dokumentmetadaten und Versionsstände. Volltexte bleiben in der Dokumenten- und Versionsvorschau.
5. Die Nachweisvorschau stellt PNG und UTF-8-Dateien einschließlich Markdown, CSV, Protokollen, JSON und XML dar. PDF und Office-Dateien sind über den Dateiexport zugänglich.

Die Vorschau rendert passive native Steuerelemente. Sie schreibt dazu keine externe HTML-Seite und führt keine Dokumentinhalte aus. Die bestehende Nachweisfunktion prüft den gespeicherten Hash vor der Nutzdatenvorschau.

## Vorlagen und Migration

Zu den 126 Katalogvorlagen kommen 27 ausführliche, eigene Arbeitstexte für acht Fachthemen und die übergreifende Governance sowie vier umfassende Richtlinienentwürfe hinzu. Die Texte beschreiben Rollen, konkrete Abläufe, Prüfkriterien, Beispiele und Nachweise. Platzhalter enthalten Werte, die vor der betrieblichen Freigabe festzulegen sind. Quellen und Fundstellen dienen als Ausgangspunkte für die fachliche Prüfung. Änderungen an der eigenen Fassung vorhandener Vorlagen bleiben erhalten.

Das Schema wird additiv erweitert. Beim Öffnen einer 0.8.0-Projektdatei wird der ursprüngliche verschlüsselte Stand vor Migration 008 als `.pre09-*.bak` gesichert. Die neue Version arbeitet weiterhin mit dem bestehenden Containerformat.

## Abnahme

- Der Release-Build aller drei Projekte lief unter Linux x64 mit .NET SDK 10.0.100 ohne Warnungen und Fehler.
- 51 Regressionstests bestanden. Geprüft wurden unter anderem der Wechsel zwischen Entwurf, Prüfung und Freigabe, veraltete Schreibstände, historisch unveränderte Veröffentlichungen, typisierte Bezüge sowie ein erneuter Dateischreibvorgang ohne doppelte Freigabe.
- 395 unabhängige SQL-, XML- und Strukturprüfungen bestanden. Sie prüfen den Erhalt der bisherigen Tabellen und Inhalte, die Unveränderlichkeit veröffentlichter Fassungen und die Quellenzuordnung aller 31 neuen Vorlagen. NuGet meldete für die abgefragte Quelle keine bekannten Schwachstellen der verwendeten direkten und transitiven Pakete.
- Der gefilterte XLSX- und HTML-Export zeigte Dokumentmetadaten einschließlich Arbeits- und Veröffentlichungsstand. Text und Prüfnotizen des Entwurfs erschienen nicht im allgemeinen Export.
- Im Linux-Desktop war die verschlüsselte Sicherung einer tatsächlich mit 0.8.0 erzeugten Testdatei bytegleich mit dem Original. Die eigene Fassung einer vorhandenen Vorlage blieb erhalten. Aus einem bestehenden Katalogtext entstand ein Entwurf mit formatiertem Text und Tabelle, der geprüft, veröffentlicht und revidiert wurde. Die ursprüngliche Freigabe blieb dabei in der Versionsvorschau sichtbar.
- Nach der Migration einer weiteren Kopie standen alle 157 Vorlagen in der Oberfläche zur Verfügung. Die Informationssicherheitsrichtlinie ließ sich über ihr Kürzel suchen. Überschriften und Tabellen waren im großen Fenster und bei 1120 × 760 sichtbar. Die kontrastreichen Icons und der Dokumenteneinstieg in der Übersicht wurden geprüft.
- Ein UTF-8-Testprotokoll und ein PNG-Testbild wurden über die Oberfläche importiert und nativ angezeigt. Beide Vorschauen zeigten Dateiname und Nachweiskontext. Der Text enthielt die erwarteten Sonderzeichen und das Bild wurde im richtigen Seitenverhältnis dargestellt.

Die Abnahme erfolgte mit synthetischen Projekten unter Linux. Native Abnahme und Screenreader-Tests unter Windows und macOS stehen aus.
