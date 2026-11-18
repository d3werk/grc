# D3 GRC 0.5 – Brand Workspace

Eigenständiger Desktop-Entwicklungsstand für Datenschutz, Informationssicherheit, KI-Governance und Open-Source-Governance. C#/.NET 10, Avalonia, SQLite, EF-Core-Schemamigrationen.

**Status: Quellcode, keine erfolgreich kompilierte oder signierte Endanwenderdistribution.**

## Zuerst ansehen
`DESIGN-VORSCHAU.html` per Doppelklick im Browser öffnen. Die interaktive Offline-Vorschau zeigt Navigation, Quellenfilter, Vorlagenbearbeitung, Mapping-Bestätigung, Glossar und Drucklayout. Sie speichert Änderungen nur im Arbeitsspeicher und ist KEIN Screenshot oder Ersatz der nativen Anwendung. Keine produktiven Daten eingeben.

## Wichtig zum bisherigen Stand
Die zuvor erzeugte 0.4-ZIP war im aktuellen Arbeitsverzeichnis nicht mehr verfügbar. Diese Lieferung ist daher eine konsolidierte eigenständige Solution, kein getesteter In-place-Patch auf dein lokales Repository. Nicht ungeprüft über bestehende Quellen kopieren. Das neue Dateiformat D3GRC005 ist nicht mit alten D3GRC004-/ZIP-/JSON-Projekten kompatibel. Ein Datenimport muss separat implementiert und getestet werden.

## Enthalten
- Logo unverändert eingebunden; Markenfarben Grün #486E31, Türkis #2E90B2 und Graubraun #584E44.
- Native Sidebar, Dashboard, Listen-/Detailansichten, Suche, Quellen-/Statusfilter, Sortierung, Leerzustände und Bestätigungsdialoge.
- Datensätze: Anforderungen, Controls, Risiken, Audits, Findings, Maßnahmen, KI-Systeme, OSS-Komponenten.
- Quellenbezogene typisierte Mappings mit Begründung und vorbereiteten Vorschlägen.
- Evidence-Dateiimport mit Herkunft, Quellenbezug, Control, Hash und Gültigkeit; Wiederverwendung für mehrere Controls.
- Audit-Scope, passende Evidence-Auswahl, Prüftextvorlagen und Abschlussregeln.
- Quellenaktualisierung auf Klick als technischer Abruf/Hashvergleich mit Prüfhistorie und expliziter Bestätigung.
- Eigene Texte getrennt von Katalogoriginalen; Updates überschreiben keine eigenen Fassungen oder Datensätze.
- Speichern unter als verschlüsselte Projektkopie mit gleichem Passwort.
- Druckansichten im Standardbrowser mit Druckdialog / PDF-Ausgabe; CSV-Export.
- Tatsächlich befüllte SQLite-Referenzdatenbank plus JSON-Katalog.
- 48 editierbare Glossarbegriffe und druckbares Glossar.
- AES-256-GCM-/Argon2id-Projektcontainer, lokale kooperative Sperre, verschlüsselte Recovery nach Mutation und alle 30 Sekunden bei Dirty-Stand.
- C#-Tests, SQL-/Strukturtests und vorbereitete native CI-Matrix.

## Vorbefüllung
8 Quellen: 7 offizielle externe Referenzen plus 1 interner D3-Arbeitsstandard.
59 eigene Textvorlagen: 16 Anforderungen, 14 Controls, 6 Risiken, 4 Audits, 4 Findings, 3 Maßnahmen, 6 Nachweisanforderungen, 4 Prüfhandlungen, 1 KI-Steckbrief, 1 OSS-Steckbrief.
48 Glossarbegriffe, 18 Mapping-Vorschläge.
30 aktive Ausgangsdatensätze (16 Anforderungen + 14 Controls) sind ausdrücklich als Entwurf / vorbelegt markiert.
**Keine echten Evidence-Dateien, keine bestandenen Audits und keine erfundenen Organisationsdaten werden vorbefüllt.**

## In JetBrains Rider
1. ZIP in ein neues Verzeichnis entpacken.
2. `D3.Grc.sln` öffnen.
3. .NET 10 SDK erkennen lassen, NuGet Restore durchführen.
4. `D3.Grc.Desktop` als Startprojekt ausführen.
5. Neues Projekt mit Passwort (mindestens 12 Zeichen) anlegen.
6. Anforderungen und Controls prüfen, Verantwortliche und Scope ergänzen.
7. Unter Mappings Vorschläge prüfen und explizit bestätigen.
8. Tatsächliche Nachweise importieren, Audits anlegen und Scope zuordnen.

Für Endanwender sind weiterhin signierte self-contained Pakete vorgesehen. Keine Endanwenderskripte nötig; solche fertigen Installer sind hier NICHT enthalten.

## Druck und Export
Drucken erzeugt nach Warnung eine lokale Klartext-HTML-Datei und öffnet den Standardbrowser. Dort Drucken / als PDF speichern. Das ist kein eingebetteter plattformübergreifender nativer Druckdialog und kein eigener PDF-Renderer. CSV exportiert die aktuell gefilterte Ansicht mit Schutz gegen übliche Tabellenformel-Injektion. Exporte nach dem Löschkonzept behandeln.

## Sicherheitsgrenze
Projektdatei, Recovery und Backups sind verschlüsselt. Die aktive SQLite-Arbeitskopie im privaten Benutzerverzeichnis ist NICHT SQLCipher-verschlüsselt. Nach normalem Schließen wird sie entfernt; nach Absturz können Reste bleiben. Explizite Bereinigung vorhanden. Datenträgerverschlüsselung verwenden. Kein Schutz gegen Malware/Administrator/RAM-Dumps. Kein Passwort-Reset.

## Prüfstatus
- Native C#-Kompilation: blockiert, kein dotnet SDK vorhanden; Buildversuch protokolliert.
- C#-Tests: enthalten, hier nicht ausgeführt.
- HTML-Designvorschau: Browserinteraktionen und Druck-CSS separat geprüft; nicht mit Avalonia-Tests verwechseln.
- SQLite-/Seed-/Strukturprüfungen: siehe docs/validation.
- Keine Installer, Signaturen oder native Plattformabnahme erzeugt.

## Eigenständigkeit
Öffentlich beschriebene integrierte GRC-Funktionsprinzipien von SAVISCON dienen als fachliche Orientierung. Keine Kopie der Oberfläche, kein Zugriff auf interne Produktlogik, keine Übernahme proprietärer Texte und keine Verbindung/Partnerschaft behauptet. Die D3-Oberfläche und alle Vorlagentexte sind eigenständig.

## Dokumentation
- docs/UI-UX.md: Designsystem und Bedienregeln.
- docs/ANWENDERHANDBUCH.md: vollständiger Arbeitsablauf.
- docs/QUELLEN-UND-UPDATES.md: Herkunft, Updategrenzen und Lizenzhinweise.
- docs/DATENMODELL.md: Tabellen und Migrationen.
- docs/GLOSSAR.md und docs/GLOSSAR.html: Glossar.
- docs/ABNAHME.md: Freigabecheckliste und Grenzen.
