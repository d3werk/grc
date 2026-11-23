# Prüfbericht D3 GRC Plattform 0.11.0

**Stand 23. November 2026.** Geprüft wurden die neue Web-Plattform mit synthetischen Mandanten und die bestehende Desktop-Anwendung unter Linux. Der Web-Stand liefert einen lokal ausführbaren ersten Produktabschnitt der integrierten GRC-Architektur. [Plattformstart und Arbeitsstrecke](../platform/README.md) · [Zielarchitektur](PLATTFORM-ARCHITEKTUR.md)

## Bestand und Struktur

Desktop 0.10.0 bringt einen Unternehmensassistenten, einen Starterkatalog, Quellen- und Vorlagenpflege, Dokumentenlenkung sowie ein lokales Risikoregister mit. Seine verschlüsselten Projektdateien bleiben lokal. Die Web-Plattform baut den gemeinsamen Mandantenbestand in PostgreSQL auf. Der geführte Transfer vorhandener Desktop-Projekte ist als eigenes Arbeitspaket beschrieben. [Desktop-Prüfbericht](PRUEFBERICHT-0.10.0.md) · [Ausbau und Abnahme](PLATTFORM-BACKLOG.md)

Die Web-Plattform kapselt Identität, Katalog, Organisation, Importe und die operativen Fachabläufe in einer FastAPI-Anwendung. Typisierte Verknüpfungen sichern die erlaubten Objektarten und den gemeinsamen Mandanten über zusammengesetzte Fremdschlüssel. Organisation, IT-Dienst, Risiko, Control, Maßnahme und Verarbeitungstätigkeit behalten jeweils ihre eigene UUID. TOM verwendet das gemeinsame Control-Register. Fachliche Zuständigkeiten und Anwendungsrechte haben eigene Zuordnungen. [PostgreSQL-Migrationen](../platform/backend/migrations) · [Berechtigungs- und Beziehungsmodell](PLATTFORM-ARCHITEKTUR.md#13-implementierungsartefakte)

## Durchgängige Abnahme

| Bereich | Beobachtung |
| --- | --- |
| Ersteinrichtung | Ein berechtigter Einrichtungsschlüssel erzeugt Mandant und Administrationskonto. Der Browser öffnet anschließend den Arbeitsbereich mit den geführten nächsten Schritten. |
| Gemeinsamer IT-Dienst | Dieselbe Dienst-UUID erscheint in Risiko, Control und Verarbeitung. Der Datenschutzbericht enthält den Dienst und den vorhandenen Control als TOM. |
| Verantwortlichkeit | Eine Person erhält getrennte Zuordnungen zur IT-Leitung, zum Dienst und zum Risiko. Beendete Zuordnungen erscheinen in der Historie und im Audit-Protokoll. |
| Risikoverlauf | Brutto- und Restwert werden aus den jeweiligen Skalenwerten berechnet. Eine weitere Bewertung erhält die vorherige. Nach Änderung des Risikokontexts wird der frühere Bewertungsstand als veraltet ausgewiesen. |
| Control und Maßnahme | Der Control und die konkrete Umsetzung bestehen als getrennte Objekte mit begründeter Beziehung. Eine Nachweisdatei kann beiden zugeordnet werden. |
| Nachweis | UTF-8-Text wurde im Browser angezeigt und bytegleich heruntergeladen. Die API speichert die Datei mit SHA-256. Dateivorschau und Quellenreferenz verwenden die Rechte des Arbeitsbereichs. |
| Mandant und Mandat | Der gezielte Zugriff eines fremden Mandanten auf Datensatz und Datei wird verweigert. Befristung, Scope und Widerruf eines Beratungsmandats wirken serverseitig. |
| Berichte | Management- und Datenschutzberichte verwenden den autorisierten Datenstand. Der CSV-Datenschutzexport enthält den freigegebenen IT-Dienst und TOM-Bezug. Zellinhalte mit Formelanfängen werden als Text behandelt. |
| CSV-Import | Die Formatprüfung erzeugt keine Einträge. Ein Fehler in der zweiten Zeile verwirft den gesamten Schreibvorgang. Eine erfolgreiche Wiederholung mit demselben Schlüssel liefert dieselben IDs. |
| Audit-Protokoll | Die Oberfläche zeigt berechtigten Mitgliedern die gespeicherten Fachereignisse, UTC-Zeit und handelndes Mitglied. Die Datenbankrolle der Anwendung kann die Protokollzeilen lediglich gemäß Policy lesen und ergänzen. |

Die Kennzahlen des Cockpits zeigen den Stand der erfassten Risiken, Controls, Maßnahmen und Nachweise. Dazu gehören fehlende und veraltete Bewertungen sowie Controls ohne gültigen Nachweis. Fachliche Wirksamkeits- und Konformitätsentscheidungen folgen im jeweiligen Prüfprozess. [API-Integrationstest](../platform/backend/tests/test_platform_flow.py) · [OpenAPI](../platform/openapi.json)

## Technische Prüfungen

| Prüfung | Ergebnis |
| --- | --- |
| Backend mit PostgreSQL 17.12 | 12 Tests bestanden. Die einzige Warnung betrifft die TestClient-Abhängigkeit in der Entwicklungsumgebung. |
| Datenbankgrenzen | In 14 privaten IAM- und Fachtabellen ist Row-Level Security erzwungen. Die API verwendet ein separates, unprivilegiertes Datenbankkonto. Ein Datenbankzugriff unter der Anwendungsrolle ohne gesetzten Mandantenkontext sieht keine Fachdaten. |
| API-Dokumentation | Das ausgelieferte OpenAPI-Schema stimmt mit der JSON-Datei im Repository überein. Der OpenAPI-Validator hat die Datei bestätigt. 32 Pfade und 42 Operationen einschließlich Upload, Download, Import und Berichten. |
| Browser bei 1440 und 390 Pixeln | Anmeldung, Registeranlage, Verknüpfungen, Risikomatrix, Nachweisvorschau und -download, Berichte und erneute Anmeldung wurden mit Chromium gegen die echte API ausgeführt. Der Mobiltest hatte keinen horizontalen Seitenüberlauf. |
| Personen und Audit im Browser | Anlage und Umbenennung einer Person, drei fachliche Zuordnungen, Beendigung mit Historie und lesender Zugriff wurden bei 390 Pixeln geprüft. Bearbeitung und Auditansicht entsprachen den Rollen. |
| Registerbedienung | Kennzahlen-Hilfe per Tastatur erreicht und mit Escape geschlossen. Unveränderte Formulare lassen sich direkt verlassen. Änderungen können vor einem Wechsel gespeichert oder verworfen werden. |
| PostgreSQL und Compose | Mehrstufiges Image gebaut, UI und API am selben lokalen Port gestartet. Das Upgrade von sechs auf acht Migrationen erhielt vorhandene Nachweiszuordnungen. Die Anwendungsinstanz läuft als unprivilegierter Benutzer. Der Cloud-Build verwendete eine temporär eingebundene Proxy-CA bei aktiver Zertifikatsprüfung. |
| Backup und Wiederherstellung | Synthetischer Mandantenbestand per `pg_dump -Fc` und `pg_restore --exit-on-error` in eine unabhängige PostgreSQL-Instanz übernommen. Acht Migrationen, 28 validierte Fremdschlüssel, verknüpfte Objekte, Nachweisdatei und Berichtsausgabe wurden nach der Wiederherstellung geprüft. |
| Abhängigkeiten | npm, `pip-audit` für die gesperrten Python-Laufzeitabhängigkeiten und die NuGet-Abfrage meldeten in den jeweiligen Feeds keine bekannten Schwachstellen für den geprüften Stand. |
| Desktop 0.10.0 unter Linux x64 | Release-Build mit .NET SDK 10.0.100, 65 xUnit-Tests und 450 Strukturprüfungen bestanden. Das native Anmeldefenster wurde mit isoliertem Profil gerendert. |

## Abnahme vor einem Produktivbetrieb

- **Identität und Betrieb.** OIDC/Entra ID, MFA, feinere Fach- und Objektberechtigungen, Transport- und Speicherverschlüsselung, Schlüsselverwaltung, unabhängige Sicherheitsprüfung und Betriebsüberwachung sind eigene Abnahmepunkte. Die geprüfte Compose-Instanz ist für den lokalen Zugriff vorgesehen.
- **Nachweise und Aufbewahrung.** Dieser Stand speichert Nachweisdateien in PostgreSQL. S3-kompatibler Speicher, Malwareprüfung, Versionen, Löschfristen und Legal Holds folgen gemeinsam. Der Wiederherstellungstest verwendet synthetische Daten. Wiederanlauf- und Datenverlustziele bleiben für eine Betriebsumgebung zu prüfen.
- **Fachverfahren und Integrationen.** Die feste 5×5-Bewertung und die Register für Regelwerksfassungen und DSFA-Fachfelder sind die Basis für konfigurierbare Risikomethoden, einen Framework-Versionsvergleich und verbindliche Freigaben. Vollständige Auditprogramme, Datenschutzvorfälle, Betroffenenrechte, BCM, Beraterportal für mehrere Mandanten, Hintergrundaufgaben, Microsoft-Integrationen und Shop-Lizenzierung sind im Backlog priorisiert.
- **Prüfumfang.** Die Abnahme umfasst Linux und Chromium mit Maus und Tastatur. Lasttests für große Mandantenbestände, Browser- und Desktop-Abnahmen unter weiteren Betriebssystemen, Screenreader-Tests sowie reproduzierbare Secret- und Container-Scans in CI stehen aus. [Backlog mit Akzeptanzkriterien](PLATTFORM-BACKLOG.md)
