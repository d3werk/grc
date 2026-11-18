# Anforderungsabdeckung und ehrlicher Lieferstatus

Status „im Quellcode“ bedeutet nicht „nativ erfolgreich getestet“.

| Anforderung | Stand |
|---|---|
| Anmeldung | Lokale Profilanmeldung im Quellcode |
| Registrierung | Lokale Einrichtung, kein Onlinekonto |
| Logout | Projekt-/Schlüssel-/Sitzungsabschluss im Quellcode |
| Speichern | Globale und Formularaktionen, Hauptdatei plus Recovery |
| Speichern unter | Neue verschlüsselte Projektkopie, gleicher Schlüsselkontext/Passwort |
| Themenzuordnung | 8 Fachthemen plus Zuordnung prüfen |
| Tabs | Dashboard, Datensatzdetails und Quellen |
| Passende Quellen | Themenpolitik plus objektbezogene begründete Zusatzbezüge |
| Mappings | Typ, Abdeckung, Scope, Begründung, bestätigte Zuordnung |
| Evidence | Echte Dateien, Hash, Gültigkeit, Mehrfachverwendung |
| Audits/Findings | Scope, Tests, Kriterien-Snapshot, Abschlussregeln |
| Vorlagen | 126 eigene Startertexte; Original/eigene Fassung getrennt |
| Datenbank | Befüllte Starter-/Referenzdatenbanken und additive EF-Migration |
| Glossar | 48 Starterbegriffe, editierbar, druckbar, im XLSX |
| Quellenbutton | Technischer Abruf/Hashvergleich, keine Rechtsautomatik |
| Grafisches Dashboard | Native Statusbalken, Kennzahlen und Drill-down |
| Excel | Minimaler echter OOXML-XLSX-Writer, mehrere Blätter |
| Drucken | HTML im Browser, PDF über Browserdruck |
| Stabiler SDK-Pfad | global.json: .NET 10, keine Preview |
| Bekannte Paketbefunde | Abhängigkeiten geändert, Audit muss beim Restore neu laufen |
| Build/Test | In dieser Umgebung blockiert, kein dotnet SDK |

## Noch nicht vollständig umgesetzt
- Zentraler Identitätsdienst, SSO, MFA-Anbindung und serverseitige Rollen-/Mandantenrechte.
- Organisationsbaum und fein granulare Scope-Berechtigungen.
- Eigener Framework-/Versionsauswahldialog für jede historische Rechts-/Normfassung.
- Vollständige lizenzierte ISO-/BSI-Anforderungskataloge.
- Automatische fachliche Auswirkungsanalyse neuer Rechtsfassungen.
- Historische Dashboard-Zeitreihen, 5×5-Risikomatrix und eingebettete Excel-Diagramme.
- Mehrfachauswahl einer beliebigen Themenmenge im Export; aktuell ein Thema oder alle Themen.
- Eigener PDF-Renderer, direkter nativer Druckdialog.
- Automatischer SBOM-Parser und KI-Risikoklassifikator.
- Vollständiger strukturierter CAPA-/Auditprogramm-Workflow; Maßnahmen nutzen derzeit die generischen Felder und Textbausteine.
- Signierte self-contained Installer und native Plattformabnahme.

## Architekturentscheidung
Die lokale Desktopausrichtung bleibt erhalten. Keine zusätzliche Cloudregistrierung wird vorgetäuscht. EF Core verwaltet die versionierten Schemamigrationen; der Datenzugriff erfolgt parameterisiert über Microsoft.Data.Sqlite. Der EF-Modell-Snapshot ist deshalb bewusst leer. Das Datenmodell bleibt teilweise generisch und ist noch kein vollständig ausmodelliertes Enterprise-GRC-System.
