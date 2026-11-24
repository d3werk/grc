# D3 GRC

## Web-Plattform 0.12.0

Der neue Web-Arbeitsbereich führt Organisation, Assets und IT-Dienste, Risiken, Controls, Maßnahmen und Nachweise in einem gemeinsamen Bestand. Das Datenschutzregister kann denselben Dienst und Control verwenden. Anmeldung, Rollen und befristete Beratungsmandate grenzen den Zugriff ein. Management- und Datenschutzberichte lassen sich gefiltert abrufen und als CSV exportieren.

Längere Fachtexte lassen sich mit dem Rich-Text-Editor bearbeiten. Über die Mandantenauswahl kann ein weiteres Konto verbunden und anschließend innerhalb der laufenden Browsersitzung gewechselt werden. Docker Compose startet Browserportal, FastAPI und PostgreSQL für die lokale Erprobung. [Start und erste Arbeitsstrecke](platform/README.md) · [Architektur und Berechtigungen](docs/PLATTFORM-ARCHITEKTUR.md) · [Prüfbericht 0.12.0](docs/PRUEFBERICHT-0.12.0.md)

## Desktop

Der Unternehmensassistent öffnet sich nach der ersten lokalen Anmeldung. Er schlägt je nach Unternehmensart passende Merkmale und Themen vor. Im Desktop-Menü stehen ein Mandanten-Dropdown, verschlüsselter Einzelexport, Gesamtsicherung und Wiederherstellung bereit. Die Demo **Nordlicht Service GmbH** enthält synthetische Prozesse, Risiken, Prüfungen, Controls, Resilienztests, BCM-Übungen, KI-Systeme und Verträge. [Anwenderhandbuch](docs/ANWENDERHANDBUCH.md)

Unter **NIS2-Maßnahmen** stehen Pakete für Artikel 21 Absatz 2 und Artikel 23 bereit. Vorhandene ISO- oder BSI-Controls lassen sich mit den Anforderungen verknüpfen und zusammen mit ihren Nachweisen weiterverwenden. **Kontrollprüfungen** zeigt Verantwortung, Prüfintervall, anstehende Termine und zugeordnete IT-Risiken. Audits können einen Prüfverlauf mit überprüften Originaldateien exportieren. In neu angelegten Demo-Mandanten sind ein NIS2-Mapping für die Zugriffssteuerung, ein verknüpftes IT-Risiko und ein separater Meldeprozess vorbereitet. [Anwenderhandbuch](docs/ANWENDERHANDBUCH.md)

### Ausgangsbasis der Desktop-Migration
Die ursprüngliche 0.5-ZIP und dein lokal korrigierter Quellcode waren in dieser Sitzung nicht als Dateien verfügbar. Deshalb ist dies eine konsolidierte neue Solution, kein gegen dein Repository getesteter In-place-Patch. Die bekannte 0.5-Datenbankstruktur und das D3GRC005-Containerformat werden weiter unterstützt; eine additive Migration ist enthalten. Die Migration wurde mit einer kompatiblen SQL-Testdatenbank geprüft, NICHT mit deiner persönlichen Projektdatei oder über die native C#-Laufzeit.

**Nicht über den alten Quellcode entpacken. Alte Projektdateien zunächst nur als Kopie öffnen.**

### Schnellstart in JetBrains Rider
1. ZIP in ein neues Verzeichnis entpacken.
2. In Rider über **File → Open** die Datei `D3.Grc.sln` öffnen. Im linken Reiter **Solution** erscheint das Projekt `D3.Grc.Desktop`.
3. Ein stabiles .NET-10-SDK installieren/erkennen lassen. `global.json` schließt Preview-SDKs aus.
4. In Rider **Tools → NuGet → NuGet Restore** wählen.
5. Solution bauen und die Tests unter `tests/D3.Grc.Tests` ausführen.
6. Oben das Startprofil **D3 GRC Desktop** auswählen und **Run** drücken.
7. Mit einem bestehenden lokalen Profil anmelden oder ein neues Profil registrieren. Nach der Registrierung startet direkt die Projekteinrichtung.
8. Unternehmen konfigurieren und den Umsetzungsplan prüfen. Den neuen Mandanten anlegen oder die Demo öffnen. Vorhandene verschlüsselte Projektdateien einmalig über die Mandantenverwaltung importieren.
9. In der Übersicht eine Aufgabe öffnen, Anforderungen und Controls prüfen und Änderungen speichern.

Die lokale Anmeldung benötigt ein eigenes Profilpasswort mit mindestens zwölf Zeichen. Intern verwaltete Mandanten werden in dieser Anmeldung entsperrt.

#### Startprofil fehlt in Rider

Im Reiter **Solution** unter `D3.Grc.Desktop → Properties` die Datei `launchSettings.json` mit der rechten Maustaste anklicken und **Generate Configurations** wählen. Danach **D3 GRC Desktop** starten. Alternativ unter **Run → Edit Configurations → + → .NET Project** das Projekt `D3.Grc.Desktop` als Startprojekt wählen. [Rider-Dokumentation](https://www.jetbrains.com/help/rider/Running_LaunchSettings.html)

Der gleiche Start ist aus dem Verzeichnis mit `D3.Grc.sln` in PowerShell möglich.

```powershell
dotnet run --project .\src\D3.Grc.Desktop\D3.Grc.Desktop.csproj --launch-profile "D3 GRC Desktop"
```

#### NuGet-Update meldet eine inkompatible EF-Core-Version

D3 GRC verwendet `net10.0`. Das Projekt `D3.Grc.Core` legt den SQLite-Anbieter `Microsoft.EntityFrameworkCore.Sqlite.Core` auf **10.0.14** fest. Rider kann unter **Packages to Update** die Version **11.0.0** anzeigen. Diese Version benötigt `net11.0`.

Wenn Rider **Rolling back package changes** meldet, sollte unter **Installed** wieder 10.0.14 stehen. Die Zeile für 11.0.0 unter **Packages to Update** abwählen und **Tools → NuGet → NuGet Restore** ausführen. Falls die lokale Projektdatei weiterhin 11.0.0 enthält, den Eintrag in `src/D3.Grc.Core/D3.Grc.Core.csproj` auf `Version="[10.0.14]"` zurücksetzen. [Rider-Dokumentation](https://www.jetbrains.com/help/rider/Using_NuGet.html#restore-packages)

Zur Prüfung im Terminal neben `D3.Grc.sln` ausführen.

```powershell
dotnet --version
dotnet restore D3.Grc.sln
dotnet build D3.Grc.sln --no-restore
```

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
- 176 eigene editierbare Vorlagen, darunter 19 erweiterte Fachvorlagen für 14 Themenbereiche.
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
