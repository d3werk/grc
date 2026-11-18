# Migration 0.5 → 0.6

## Unterstützte Basis
Container-Magic D3GRC005 mit AES-256-GCM und Argon2id-Parametern des bekannten 0.5-Stands. SQLite-Datenbank mit Migration `20261118120000_BrandWorkspace`.

Die ursprüngliche ZIP war hier nicht als Datei vorhanden. Die Kompatibilität basiert auf dem im Gespräch erzeugten Schema und Containercode. Kein Test mit deinem tatsächlichen Projektbestand. Vor produktiver Verwendung zwingend eine Kopie prüfen.

## Ablauf
1. 0.5-Anwendung schließen.
2. Quellcode und Projektdateien extern sichern.
3. 0.6 separat entpacken und erfolgreich bauen/testen.
4. Kopie einer 0.5-Projektdatei öffnen.
5. Vor Migration wird eine verschlüsselte `.pre06-<Zeitpunkt>.bak`-Kopie der Hauptdatei angelegt.
6. Entschlüsselung erfolgt in einer privaten Arbeitskopie.
7. Bekannte EF-Migrationshistorie und Journal werden geprüft.
8. Neue Migration `20261118210000_TopicsAndSessions` wird angewendet.
9. Recovery wird geschrieben. Hauptdatei wird erst durch Speichern oder eine speichernde Änderung aktualisiert.
10. Inhalt, eigene Texte, Quellenbezüge, Nachweise und Audits prüfen. Erst dann weiterarbeiten.

## Erhaltene Daten
Die Migration ändert keine bestehenden Record-/Template-/Glossary-/Evidence-/AuditTest-/Journal-Inhalte. Neue Tabellen und Quellen werden ergänzt. Eigene Quellenbeschreibungen und Vorlagenfassungen bleiben bestehen. Neue V06-Textbausteine besitzen eigene IDs.

Aus bestehenden primären Quellen wird eine erste Themenzuordnung abgeleitet. Interne oder nicht eindeutig zuordenbare Objekte kommen nach Zuordnung prüfen. Das ist keine automatische fachliche Freigabe. Zusätzliche Cross-Themen können begründet ergänzt werden.

## Neue Tabellen
Topics, TopicSources, RecordTopics, SourceVersions, RecordSources, MappingAssessments, Applicability, AuditCriteria und ExportJobs.

Lokale Profile liegen getrennt vom Projekt im Benutzerverzeichnis. Keine Passwörter oder Konten werden in vorhandene Projekte hinein erfunden.

## Grenzen
- Kein Import von 0.4-/JSON-/ZIP-Projektformaten.
- Kein automatisches Zusammenführen zweier Projektdateien.
- Kein Downgrade einer migrierten Datei in die alte Anwendung. Für Rückkehr die unveränderte Sicherung verwenden.
- Historische Kriterien-Snapshots werden nicht aus heutigen Daten rückwirkend erfunden.
- Der SQL-Test prüft Erhalt vorhandener Werte, ersetzt aber weder EF-Runtime-Test noch native Recovery-/Stromausfalltests.
