# D3 GRC · Plattform-Backlog

Der erste Freigabepunkt ist ein gemeinsamer Datenpfad für Organisation, Risiko, Control, Maßnahme und Evidence. Der Datenschutzpfad verwendet anschließend dasselbe Asset und denselben Control. Produktionsbetrieb setzt außerdem die Abnahme von Identität, Dateiablage, Nachweisaufbewahrung und Wiederherstellung voraus. [Plattformarchitektur](PLATTFORM-ARCHITEKTUR.md)

## Priorisierte Umsetzung

### Phase 1a · Gemeinsamer Datenpfad

| ID | Arbeitsauftrag | Abhängigkeit und Abnahme | Lieferstand |
| --- | --- | --- | --- |
| 1A-01 | Versionierte Migrationen, dedizierter Datenbank-Login, lokaler Start und Healthchecks | Leere PostgreSQL-Datenbank migrieren. Migration wiederholen. Der API-Login erreicht nur die Runtime-Rolle. Bereits gebuchte Migrationen behalten ihre Prüfsummen. | Plattformkern |
| 1A-02 | Mandant, Anmeldung, Mitgliedschaft, Rollen und befristete Beratungsmandate | Zwei Mandanten mit verschiedenen Anmeldungen. Zulässigen Bereich prüfen. Nach Entzug oder Ablauf reagiert dieselbe Sitzung beim nächsten Zugriff. | Plattformkern mit lokalen Sitzungen und ersten Runtime-Rollen |
| 1A-03 | Organisationsbaum und gemeinsame Register für Asset, IT-Service und Geschäftsprozess | Zwei Organisationseinheiten anlegen. Ein Asset eindeutig zuordnen und unter erlaubtem Scope abrufen. Fremde Eltern-ID und unzulässige Änderungen werden abgewiesen. | Plattformkern |
| 1A-04 | Risikoerfassung mit Asset-Bezug, fester 5×5-Methode und Verlauf | Risiko dem Asset zuordnen. Ausgangsrisiko und optionales Restrisiko bewerten. Eine neue Bewertung erhält eigene ID, und ältere Daten bleiben lesbar. Die Matrix zeigt fehlende oder veraltete Bewertungen. | Plattformkern mit Basismethode |
| 1A-05 | Dauerhaften Control und befristete Maßnahme getrennt führen | Control mit dem Risiko verknüpfen und Maßnahme getrennt erstellen. Zulässige Beziehungstypen werden durchgesetzt. Veralteter Schreibstand ergibt einen Konflikt. | Plattformkern für Grundobjekte und Beziehungen |
| 1A-06 | Evidence mit Prüfsumme, Herkunft, Gültigkeit und wiederverwendbaren Bezügen | Dateinachweis oder externe Referenz erfassen. Control und Maßnahme referenzieren denselben Nachweis. Server behandelt Download und Hash getrennt von Metadaten. Fremder Scope bleibt geschützt. | Plattformkern mit begrenzten Dateiformaten und Uploadgröße |
| 1A-07 | Management-Dashboard, filterbezogener Bericht und Navigation | Objektzahlen, Risikostand und Evidence-Status gegen gespeicherte Daten abgleichen. Für JSON/CSV den Scope anwenden. Einen Eintrag über Listen und Fachdatenansicht verfolgen. | Plattformkern |
| 1A-08 | Technische Abnahme mit zwei Mandanten, Scopes und realer Datenbank | API-, SQL- und Browserprüfungen mit Testdaten. Sicherheitsgrenzen, persistierte Ergebnisse und API-Vertragsdokument prüfen. | Kriterien für den Entwicklungsstand |
| 1A-09 | Begrenzter CSV-Import für den gemeinsamen Katalog | Spalten und Zeilen prüfen, transaktional übernehmen und bei erneuter Anfrage dieselben Objekt-IDs liefern. Ändert sich der Inhalt unter demselben Importschlüssel, Konflikt melden. | Plattformkern, XLSX und Desktop-Import folgen |
| 1A-10 | Personen und fachliche Verantwortlichkeiten als eigene Objekte | Dieselbe Person als IT-Leitung, Asset Owner und Risk Owner zuordnen. Falsche Zielarten und fremde Objekte abweisen. Widerruf hält die ursprüngliche Zuordnung nachvollziehbar. | Plattformkern, Zugriff über die Runtime-Rolle und gegebenenfalls das Mandat |

### Phase 1b · Datenschutz auf gemeinsamen Objekten

| ID | Arbeitsauftrag | Abhängigkeit und Abnahme | Lieferstand |
| --- | --- | --- | --- |
| 1B-01 | VVT mit Organisation, Verantwortlichkeit und Asset- oder Dienstbezug | Eine Verarbeitung mit einem vorhandenen Asset oder IT-Dienst verknüpfen. Beide Ansichten zeigen dieselbe Objekt-UUID. Zweck, Rechtsgrundlage, Kategorien und Löschregeln bleiben als eigene Felder abrufbar. | Grundobjekt, Fachfelder und Beziehungen im Plattformkern, vollständige Praxisworkflows im Ausbau |
| 1B-02 | TOM aus gemeinsamem Control | VVT-TOM-Bezug auf die UUID des im ISMS angelegten Controls speichern. Aktualisierte Metadaten bleiben in beiden Ansichten nachvollziehbar. Freigabefassung später festschreiben. | Plattformkern für gemeinsame IDs |
| 1B-03 | Filterbezogener Bericht für den Datenschutzpfad | VVT-, TOM- und Asset-Bezug unter freigegebenem Scope nachvollziehen. JSON nennt den ausgewählten Bereich, CSV enthält die Bereiche der enthaltenen Einträge. Beide Ausgaben führen den Stand als UTC-Datum. Ein fachlich freigegebener Art.-30-Bericht und XLSX-Profile folgen. | Datenschutzbericht als JSON und CSV im Plattformkern |
| 1B-04 | DSFA und Prüfung nach Art. 35 / Art. 36 | Vom VVT in eine DSFA verzweigen. Betroffene Gruppen, Zweck, Notwendigkeit, Verhältnismäßigkeit, Risiken und Schutzmaßnahmen bewerten. Passt eine Konsultation, Entscheidung, zuständige Stelle und Verlauf getrennt erfassen. | Separater DSFA-Datensatz als Grundlage |

### Phase 2 · Fachworkflows, Integration und Betrieb

| ID | Arbeitsauftrag | Abhängigkeit und Abnahme |
| --- | --- | --- |
| 2-01 | OIDC, Entra ID und fein granulare RBAC-/ABAC-Policies | Konto- und Mandantenzuordnung prüfen. Organisationsbaum-Scopes, Vertraulichkeit, Ownership, Mandatsablauf, getrennte Prüfrollen und technisch privilegierte Konten getrennt testen. |
| 2-02 | Konfigurierbare Versionen der fünf Risikomethoden | Für Informationssicherheit, Datenschutz, Unternehmensrisiko, Lieferantenrisiko und BCM je Profil erstellen. Methodenänderung hält alte Eingaben und Ergebnisse abrufbar. |
| 2-03 | Framework-Releases, lizenzierte Kataloge und Änderungen | Zwei Ausgaben vergleichen. Hinzugefügte, geänderte und entfallene Anforderungen prüfen. Frühere Anwendbarkeit und Audit-Snapshots bleiben der alten Fassung zugeordnet. |
| 2-04 | Dokumentenlenkung, SoA, Kontrollkalender und Vier-Augen-Policies | Entwurf, Review, Freigabe, Revision und Außerkraftsetzen durch passende Rollen. Dokumente und Regelwerkversionen klassifizieren sowie Prüfaufträge erzeugen und verfolgen. |
| 2-05 | Versionierte Evidence im Objektspeicher | Upload mit Quarantäne, Scan, Hash und Freigabe. Ersatzversion erfassen und Altversion nach Aufbewahrung für historische Prüfungen wieder abrufen. Legal Hold, Löschung und Berechtigungsentzug prüfen. |
| 2-06 | Datenschutzvorfälle und gestaffelte Fristen | Vorfall erfassen, Schwere und Betroffenheit ermitteln, Meldung begründen und Folgeaufgaben erzeugen. Zuständigkeiten, Empfangszeit, Frist und Abschluss prüfen. |
| 2-07 | AVV, Betroffenenrechte, Aufbewahrung und Drittlandtransfer | Einzelne fachliche Workflows mit Verantwortlichen, Fallstatus, Fristenregeln, Entscheidungen und je Kontext zulässigem Export umsetzen. |
| 2-08 | Auditprogramm, Findings und Wirksamkeitsprüfung | Audit-Scope und Kriterien fixieren. Nachweise wiederverwenden und Testresultat speichern. Finding und Maßnahme anlegen. Abschluss mit Wirksamkeitsnachweis und begründetem Status. |
| 2-09 | Aufträge und Benachrichtigungen | Outbox in Änderungstransaktion. Wiederholung nach Fehler erzeugt keine doppelte Fachänderung. SMTP-Zustellstatus, verzögerte Ausführung und Fehlerwarteschlange im Test auslösen. |
| 2-10 | Gefilterte Exporte, Excel und P1-Integration | CSV/JSON um definierte XLSX-Formate ergänzen. Mandant, Scope, Sichtbarkeit und Exportpolicy beim Start und Abruf prüfen. Excel-Dateien mit Referenzlesern validieren. |
| 2-11 | Graph, Intune, LDAP und Webhooks | Tenant-Konfiguration, Connector-Genehmigung, Cursor, Idempotenz und abgestimmtes Feldmapping. Fehler bleiben mit Korrelations-ID nachvollziehbar. |
| 2-12 | Produktionsbetrieb, Sicherung und Wiederherstellung | TLS, Schlüsselrotation, CI-Prüfungen und Protokollkonzept abnehmen. Eine Datenbank plus Objektspeicher getrennt wiederherstellen. RPO und RTO messen und Zugriffe nach Restore prüfen. |
| 2-13 | Import aus verschlüsselten Desktop-Projektkopien | Referenzen, Quellversionen und Alt-IDs nachvollziehbar auf UUIDs abbilden. Wiederholung erzeugt keine Duplikate. Nachweis-Hash, Fehlerprotokoll und temporäre Dateibereinigung prüfen. |
| 2-14 | Unternehmensprofil und priorisierter Einrichtungsassistent | Branche, Größe, betroffene Regelwerke und Unternehmensstruktur erfassen. Für KMU und mehrstufige Organisationen einen begründeten Vorschlag mit erforderlichen und optionalen Arbeitsschritten erzeugen. Änderungen durch die Administration lassen sich nachvollziehen. |

### Phase 3 · Produktbetrieb und BCM

| ID | Arbeitsauftrag | Abhängigkeit und Abnahme |
| --- | --- | --- |
| 3-01 | Beraterportal | Wechsel zwischen gültigen Mandaten und getrennte Kennzahlen. Entzug wirkt auf alle betroffenen laufenden Aufträge und Abrufe. |
| 3-02 | Entitlements, Pakete und Shop | Privacy, Security, Compliance, Complete und Partner konfigurieren. Aktivierung und Ablauf, Paketwechsel, Export und Aufbewahrung prüfen. Abrechnung bleibt als eigener Ereignispfad nachvollziehbar. |
| 3-03 | Governance-Ziele, Beschlüsse und Schulungen | Ziele mit Verantwortlichen und Kennzahlen, Gremienentscheidung mit Freigabe und Kenntnisnahme mit Erinnerung nachweisen. |
| 3-04 | Defender, Purview, Jira und Wazuh | Befunde und technische Ressourcen im jeweiligen Mandanten verbinden. Kontrollierter Feldzugriff, sichere Abmeldung und nachvollziehbare Fehlersynchronisation. |
| 3-05 | BCM mit BIA und Übungen | Kritische Prozesse und Abhängigkeiten erfassen, RTO/RPO festlegen, Übung durchführen und Verbesserungen nachhalten. Bewertungsmethode aus 2-02 nutzen. |

## Abnahmematrix

| Fall | Prüfaufbau und erwartetes Ergebnis | Beleglage des ersten Plattformstands |
| --- | --- | --- |
| A01 · Mandantentrennung | Identische Titel in A und B. Fremde UUID in Einzelabruf, Änderungsaufruf, Beziehung, Nachweisabruf und Report abweisen. Such- und Zählwerte bleiben getrennt. | Grundlage aus Tenant-Kontext, RLS und Composite-Fremdschlüsseln |
| A02 · Mandat entzogen | Berater sieht zunächst einen freigegebenen Scope. Nach Ablauf und nach aktivem Entzug denselben Token erneut verwenden. Abgelaufene Mandate dürfen keinerlei neue, geschützte Daten abrufen. | Mandatsprüfung im Plattformkern, asynchrone Aufträge im Ausbau |
| A03 · Risikohistorie | Zwei unterschiedlich datierte Bewertungen speichern. Alte Version bleibt identisch, fehlendes Restrisiko bleibt leer und ein geänderter Risiko-Datensatz zeigt veralteten Bewertungsstand an. | Feste 5×5-Historie im Plattformkern, Methode als freigegebene Version im Ausbau |
| A04 · Frameworkhistorie | Ausgabe B mit einem neuen, einem geänderten und einem entfallenen Kapitel veröffentlichen. Report für Ausgabe A erneut erzeugen und Fachbezüge vergleichen. | Ausbau 2-03 |
| A05 · Evidence ersetzt | Datei A in Audit referenzieren, Datei B als Ersatz freigeben. Alte Version für das Audit nach Policy abrufen. Hash und Retention prüfen. | Hash und Wiederverwendung vorhanden. Austausch mit Historie und Aufbewahrung folgt in 2-05. |
| A06 · Gemeinsamer Control | In ISMS einen Control anlegen. VVT-TOM-Bezug auf denselben Control setzen und beide Referenzen vergleichen. | Geteiltes Beziehungsmodell im Plattformkern |
| A07 · Gefilterter Bericht | Scope X und Y mit gleicher Objektart vergleichen. Dashboard sowie Management- und Datenschutzbericht als berechtigter und eingeschränkter Nutzer ausführen. Datenstand, Mengen und sichtbare Beziehungen vergleichen. | Scopes, Management- und Datenschutzbericht im Plattformkern |
| A08 · Wiederholung nach Fehler | Dieselbe Erstellung wiederholen und gespeicherten Datensatz sowie Journal zählen. Zusätzlich einen Auftragsfehler und die Wiederholung eines Benachrichtigungsversuchs auslösen. | Deduplizierung unterstützter Erstellungspfade prüfen. Worker und Zustellung folgen in 2-09. |
| A09 · Wiederherstellung | Gesicherte Testumgebung unter neuer Adresse bereitstellen. Migrationen, erwartete Datensätze, Objekthashes und Mandantenansichten abgleichen. Zeit und Datenstand protokollieren. | Gesamtprüfung mit Objektspeicher in 2-12 |
| A10 · Incident und Folgearbeit | Vorfall mit betroffenen VVT/Assets, Folgen und Frist anlegen. Zuständigkeit ändern, Entscheidung dokumentieren und Folgeaufgaben auf Abruf und Zustellung prüfen. | Ausbau 2-06 und 2-09 |

Jeder Abnahmelauf protokolliert Version, Umgebung, Testdatenquelle, Zeitpunkt, Prüf-ID und Ergebnis. Ein API-Test prüft den gespeicherten Zustand und den sichtbaren Rückgabewert. Ein Browser-Test verfolgt dieselbe Fachstrecke und beobachtet Eingaben, Berechtigungsfehler und sichtbare Ergebnisse. Produktionszusagen setzen die Betriebsprüfungen mit freigegebener Konfiguration voraus.
