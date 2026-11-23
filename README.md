# D3 GRC

## Web-Plattform 0.11.0

Der neue Web-Arbeitsbereich führt Organisation, Assets und IT-Dienste, Risiken, Controls, Maßnahmen und Nachweise in einem gemeinsamen Bestand. Das Datenschutzregister kann denselben Dienst und Control verwenden. Anmeldung, Rollen und befristete Beratungsmandate grenzen den Zugriff ein. Management- und Datenschutzberichte lassen sich gefiltert abrufen und als CSV exportieren.

Docker Compose startet die Entwicklungsumgebung lokal mit Browserportal, FastAPI und PostgreSQL. Für den Produktbetrieb gelten die dokumentierten Betriebs- und Fachabnahmen. [Start und erste Arbeitsstrecke](platform/README.md) · [Architektur und Berechtigungen](docs/PLATTFORM-ARCHITEKTUR.md) · [Backlog und Abnahme](docs/PLATTFORM-BACKLOG.md) · [Prüfbericht 0.11.0](docs/PRUEFBERICHT-0.11.0.md)

## Desktop 0.10.0

Der Desktop-Lieferumfang umfasst die .NET-Quellcode-Solution. Der Prüfbericht dokumentiert die Linux-Abnahme für Dokumente und Risikobewertung. [Prüfbericht 0.10.0](docs/PRUEFBERICHT-0.10.0.md)

Nach der lokalen Registrierung führt der Unternehmensassistent von der verschlüsselten Projektanlage zu einem begründeten Umsetzungsplan. Der Arbeitsbereich verbindet Anforderungen, Risiken, Controls, Lieferanten, Quellen, Nachweise und Audits mit einem Dokumentenregister. Textvorlagen lassen sich ansehen, bearbeiten und als Ausgangspunkt für Dokumententwürfe verwenden. Die Risikomatrix macht Behandlungsbedarf im gewählten Geltungsbereich sichtbar.

### Ausgangsbasis der Desktop-Migration
Die ursprüngliche 0.5-ZIP und dein lokal korrigierter Quellcode waren in dieser Sitzung nicht als Dateien verfügbar. Deshalb ist dies eine konsolidierte neue Solution, kein gegen dein Repository getesteter In-place-Patch. Die bekannte 0.5-Datenbankstruktur und das D3GRC005-Containerformat werden weiter unterstützt; eine additive Migration ist enthalten. Die Migration wurde mit einer kompatiblen SQL-Testdatenbank geprüft, NICHT mit deiner persönlichen Projektdatei oder über die native C#-Laufzeit.

**Nicht über den alten Quellcode entpacken. Alte Projektdateien zunächst nur als Kopie öffnen.**

### Schnellstart in JetBrains Rider
1. ZIP in ein neues Verzeichnis entpacken.
2. `D3.Grc.sln` öffnen.
3. Ein stabiles .NET-10-SDK installieren/erkennen lassen. `global.json` schließt Preview-SDKs aus.
4. NuGet Restore ausführen, nicht alle Pakete auf neue Hauptversionen aktualisieren.
5. Solution bauen und die Tests unter `tests/D3.Grc.Tests` ausführen.
6. `D3.Grc.Desktop` als Startprojekt starten.
7. Mit einem bestehenden lokalen Profil anmelden oder ein neues Profil registrieren. Nach der Registrierung startet direkt die Projekteinrichtung.
8. Unternehmen konfigurieren, den Umsetzungsplan prüfen und die Projektdatei mit einem separaten Passwort anlegen. Die Datei wird sofort geöffnet. Bestehende Projekte lassen sich öffnen und anschließend konfigurieren.
9. In der Übersicht eine Aufgabe öffnen, Anforderungen und Controls prüfen und Änderungen speichern.

Keine Standard-Zugangsdaten. Profil- und Projektpasswort sind getrennt. Mindestens 12 Zeichen bei der Einrichtung. Kein Passwort-Reset.

### Funktionsumfang im Desktop-Arbeitsbereich
- Lokale Profilregistrierung, Argon2id-Passwortprüfung, temporäre Sperre nach Fehlversuchen.
- Anmeldung als Voraussetzung für Projektzugriffe.
- Logout und manuelles Sperren schließen das Projekt, geben die lokale Projektsperre und den Schlüsselkontext frei und führen zur Anmeldung zurück.
- Sichtbare globale Aktionen Speichern und Speichern unter; Formularaktion Änderungen speichern.
- Formularspeicherung schreibt Datensatz, verschlüsselte Recovery und Hauptprojektdatei.
- Themenfilter für Datenschutz, Informationssicherheit, NIS2, KI, Cloud Security, BCM, Open Source und DORA; zusätzlich Zuordnung prüfen.
- Quellen- und Vorlagenauswahl passend zum Thema; bewusste zusätzliche Quellenbezüge und Mehrfachthemen möglich.
- Datensatz-Tabs: Details, Quellen, Bezüge, Evidence, Historie.
- Dashboard-Tabs: Management, Umsetzung, Nachweise & Audits, Quellen & Qualität.
- Grafische Statusbalken mit echten Bestandszahlen und Drill-down. Keine erfundenen Compliance-Werte.
- XLSX-Export mit mehreren Arbeitsblättern, Zahlen-/Datumszellen, Filtern und fixierten Überschriften.
- HTML-Druckansicht im Standardbrowser, PDF über den Browserdruckdialog.
- Typisierte Mappings mit Abdeckung, Scope und Begründung; Vorschläge müssen bestätigt werden.
- Evidence-Dateiimport, Hashprüfung, Gültigkeit und Wiederverwendung für mehrere Controls.
- Audit-Scope, Prüfvorlagen, passende Evidence, Kriterien-Snapshot und Abschlussregeln.
- Quellenbutton mit HTTPS-Allowlist, Größen-/Zeitlimit, Hashvergleich und Prüfhistorie.

### Vorbefüllung bei einem neuen Projekt
- 8 Fachthemen plus Zuordnung prüfen.
- 13 Quellen: 12 externe offizielle Referenzen plus interner D3-Arbeitsstandard.
- 157 eigene editierbare Vorlagen, darunter 27 ausführliche Arbeitshilfen und vier vollständige Richtlinienentwürfe aus 0.9.0.
- 36 vorbereitete Datensätze: 18 Anforderungen und 18 Controls, alle Entwurf.
- 12 nicht automatisch aktive Mapping-Vorschläge.
- 48 Glossarbegriffe.
- Keine echten Nachweisdateien, keine bestandenen Prüfungen, keine erfundenen Organisationsdaten.

Die Zahlen gelten für ein neues Projekt aus dem mitgelieferten Starterkatalog. Bei Migration vorhandener 0.5-Projekte werden vorhandene Inhalte erhalten und neue V06-Inhalte ergänzt; die Gesamtzahlen können deshalb abweichen.

### Prüfstatus
Die Abnahme der Risikobewertung und ihre Methodik sind im [Prüfbericht 0.10.0](docs/PRUEFBERICHT-0.10.0.md) dokumentiert. Der [Prüfbericht 0.9.0](docs/PRUEFBERICHT-0.9.0.md) enthält den Funktionsabgleich und die Abnahme von Dokumenten, Vorschauen und Vorlagen.

#### Historischer Prüfstand der Version 0.6

Die unabhängigen SQL-/XML-/Strukturprüfungen wurden ausgeführt; Ergebnis in `docs/validation.json`. Ein C#-Build wurde versucht, scheiterte aber an der Umgebung: `dotnet` ist nicht installiert, Exit 127. Auch der SDK-Download war hier nicht zugänglich. Deshalb sind C#-Tests, native GUI, tatsächlicher XLSX-Laufzeitexport und Installer NICHT als erfolgreich geprüft zu verstehen.

### Sicherheit und Grenzen
- Lokale Profile sind KEIN zentraler Identitätsdienst und KEINE manipulationssichere Teamrechteverwaltung.
- Jeder mit Dateizugriff und Projektpasswort kann grundsätzlich das Projekt öffnen; ein lokales Profil ist keine zusätzliche serverseitige Zugriffsgrenze.
- Aktive SQLite-Arbeitskopie liegt unverschlüsselt im privaten Benutzerverzeichnis. Datenträgerverschlüsselung verwenden.
- Projektdatei/Recovery/Backups sind im Quellcode AES-GCM-verschlüsselt vorgesehen; kein unabhängiges Kryptoaudit.
- XLSX-, HTML- und Evidence-Exporte sind Klartext.
- Kein automatischer juristischer Aktualisierungsdienst und keine lizenzierten ISO-Normtexte.
- Kein SSO, keine E-Mail-Registrierung, keine serverseitigen Rollen, keine signierten Installer.

### Desktop-Dokumentation
- `docs/ANWENDERHANDBUCH.md`
- `docs/MIGRATION-05-06.md`
- `docs/ANFORDERUNGEN-STATUS.md`
- `docs/SICHERHEIT-UND-ABHAENGIGKEITEN.md`
- `docs/ABNAHME.md`
- `docs/GLOSSAR.md`
