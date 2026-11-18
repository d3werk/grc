# Prüfbericht D3 GRC 0.6.1

Ausgangsbasis war der Commit dc282ce auf main. Die Prüfung umfasst Projektdateien und Recovery, Migration, Anmeldung, Datenintegrität der Fachworkflows, Exporte und Bedienung der Desktop-Oberfläche mit synthetischen Testdaten.

## Struktur und fachliche Logik

Core, Desktop und Regressionstests bilden drei getrennte Projekte. Der Core legt die Regeln für Mappings, Nachweise und Auditabschlüsse fest. SQLite-Transaktionen bündeln fachliche Änderungen mit Journaleinträgen. Der Desktop gliedert die Oberfläche nach Arbeitsbereichen und verwendet gemeinsame UI-Helfer. Der Starterkatalog hält Entwurfsdatensätze, tatsächliche Nachweise und Auditresultate getrennt.

Der größte strukturelle Engpass liegt im Zusammenspiel zwischen SQLite-Arbeitskopie, Recovery, Projektdatei und Formular. Aus Anwendersicht ist „Änderungen speichern“ ein Vorgang. Technisch kann der Datenbank-Commit bereits abgeschlossen sein, während Recovery oder Hauptdatei noch fehlschlagen. Für einen erneuten Speicherversuch muss der Zustand dieser Schritte getrennt erfasst werden. Im Desktop steuert MainWindow zugleich Navigation, Auswahl, Formulare, Dateidialoge und Speichern. Ein gemeinsamer Bearbeitungszustand pro Editor würde Auswahl, Formularzustand und Speicherversuche einzeln prüfbar machen.

## Befunde mit hoher Priorität

| Befund | Auswirkung | Fundstelle |
| --- | --- | --- |
| Öffnen der Hauptdatei verdrängt eine vorhandene Recovery. | Beim Öffnen wird die Recovery in eine Sicherungsdatei rotiert. Ein weiterer Autosave überschreibt diese Sicherung. Ein zuvor wiederherstellbarer neuerer Stand kann verloren gehen. | [Workspace.OpenAsync und AutosaveAsync](../src/D3.Grc.Core/Workspace.cs) · [Files.AtomicAsync](../src/D3.Grc.Core/Security.cs) |
| Nach einem Dateischreibfehler kann „Speichern“ dieselbe Formularänderung erneut ausführen. | Der Datenbank-Commit liegt vor dem Dateischreiben. Ein neuer Eintrag kann beim Wiederholen eine zweite ID erhalten. Ein vorhandener Eintrag kann aufgrund seiner inzwischen erhöhten Version abgelehnt werden. | [Workspace.Change und SaveEntryAsync](../src/D3.Grc.Core/Workspace.cs) · [MainWindow.Save](../src/D3.Grc.Desktop/MainWindow.cs) · [EntryPage](../src/D3.Grc.Desktop/EntryPages.cs) |
| Die Verwerfen-Abfrage setzt den Änderungsstatus zurück, bevor Folgeaktionen abgeschlossen sind. | Nach Abbruch eines Exportdialogs kann bearbeiteter Text sichtbar bleiben. Globales Speichern kann ihn dann überspringen. | [MainWindow.Discard, Save und Export](../src/D3.Grc.Desktop/MainWindow.cs) |
| Statusfilter und Sortierung können Auswahl und Editor auseinanderbringen. | Das Register ersetzt zuerst seine Liste. Nach Abbruch der Warnung ist die vorherige Zeile unter Umständen aus der Liste gefiltert. „Neuer Eintrag“ kann außerdem die alte Zeilenmarkierung neben einem leeren Editor stehen lassen. | [EntryPage](../src/D3.Grc.Desktop/EntryPages.cs) · [MainWindow.SafeSelection](../src/D3.Grc.Desktop/MainWindow.cs) |

Die ersten beiden Befunde wurden auf Linux mit isolierten Testprojekten reproduziert. Nach Öffnen der Hauptdatei und einem Autosave fehlte der neuere Testeintrag sowohl in der Recovery als auch in ihrer Sicherung. Ein blockierter Sicherungspfad sorgte für einen Schreibfehler nach dem Datenbank-Commit. Beim erneuten Anlegen mit einer neuen Formular-ID waren anschließend zwei Einträge in der Projektdatei vorhanden. Beim Bearbeiten eines bestehenden Eintrags wurde der Wiederholungsversuch wegen eines veralteten Versionsstands abgelehnt.

## Weitere fachliche und strukturelle Befunde

| Befund | Auswirkung | Fundstelle |
| --- | --- | --- |
| Der Finding-Abschluss speichert Grund und Status. In den Abschlussdaten fehlt die Evidence-ID. | Bei mehreren geeigneten Nachweisen fehlt die Zuordnung zum tatsächlich geprüften Nachweis. | [Workspace.CloseFindingAsync](../src/D3.Grc.Core/Workflows.cs) |
| Beim Speichern einer Anforderung wird die Anwendbarkeitsentscheidung erneut geschrieben, auch wenn ihr Status gleich bleibt. | Eine Textkorrektur kann Begründung, Prüfer und Datum der aktuellen Fachentscheidung ersetzen. | [Workspace.SaveEntryAsync](../src/D3.Grc.Core/Workspace.cs) |
| Dashboard und XLSX zählen Nachweise anhand des Ablaufdatums. Der fachliche Workflow prüft zusätzlich Erhebungszeitpunkt und Hash. | Die Anzeige kann eine Abdeckung melden, obwohl der Nachweis im Workflow nicht verwendbar ist. | [Dashboard](../src/D3.Grc.Desktop/Dashboard.cs) · [ExportSnapshotAsync](../src/D3.Grc.Core/Exports.cs) · [RequireProof](../src/D3.Grc.Core/Workflows.cs) |
| Navigation lädt Listen und Referenzdaten bei jedem Ansichtswechsel. Für die Nachweisabdeckung folgen Einzelabfragen pro Control. | Die Datenbankarbeit steigt mit der Zahl der Controls. Ein Lasttest mit großen Projekten steht aus. | [MainWindow.Render](../src/D3.Grc.Desktop/MainWindow.cs) · [Dashboard](../src/D3.Grc.Desktop/Dashboard.cs) |
| Die Anmeldung synchronisiert Fehlversuche innerhalb einer Instanz. | Parallel laufende Programme können denselben Zählerstand lesen und einzelne Fehlversuche gegenseitig überschreiben. | [LocalIdentity.LoginAsync](../src/D3.Grc.Core/Security.cs) |
| Die Bereinigung kann ein neu angelegtes Arbeitsverzeichnis vor Einrichtung seiner Sperrdatei erfassen. | Bei paralleler Projekterstellung und Bereinigung kann der Start des neuen Projekts scheitern. | [Workspace.Prepare und CleanupOrphans](../src/D3.Grc.Core/Workspace.cs) |

## UI und UX

Die Trennung zwischen Profilanmeldung und Projektpasswort ist sichtbar. Themenkontext, Datensatz-Tabs und Statusbalken helfen bei der Orientierung. Die ergänzten deutschen Tooltips erläutern Filter, Recovery, Klartextexporte, primäre und zusätzliche Quellen, Änderungsbegründungen, Mapping-Abdeckung und die Auswahl von Nachweisen.

- **Rückmeldung beim Bearbeiten.** Mehrere Workflow-Formulare erkennen nur Änderungen ausgewählter Felder. Eine geänderte Quellen- oder Objektauswahl kann deshalb ohne Warnung verloren gehen. Der Editor bietet auch einzelne Status- und Löschaktionen an, die der Core für den Datensatztyp verweigert. [WorkflowPages](../src/D3.Grc.Desktop/WorkflowPages.cs) · [EntryPages](../src/D3.Grc.Desktop/EntryPages.cs)
- **Finding-Abschluss.** Der Dialog bietet Nachweise einzelner betroffener Controls an, darunter gegebenenfalls abgelaufene. Der Abschluss verlangt einen aktuellen Nachweis, der allen betroffenen Controls zugeordnet ist. Eine entsprechend gefilterte Liste würde Fehlversuche und Sucharbeit vermeiden. [EntryPages](../src/D3.Grc.Desktop/EntryPages.cs) · [Workflows](../src/D3.Grc.Core/Workflows.cs)
- **Platz und Orientierung.** Das Fenster erfordert mindestens 1200 × 800. Navigation, Aktionsleiste und Listen beanspruchen feste Breite. Bei kleiner nutzbarer Fläche oder hoher Skalierung bleibt weniger Platz für das Formular. Der aktuelle Navigationspunkt ist im Seitenmenü nicht hervorgehoben. „Sitzung sperren“ und „Logout / Abmelden“ führen dieselbe Aktion aus. [MainWindow](../src/D3.Grc.Desktop/MainWindow.cs) · [Ui](../src/D3.Grc.Desktop/Ui.cs)
- **Projektaktionen.** Auf dem Linux-Testdesktop war „Arbeitskopien bereinigen“ in der Seitenleiste am rechten Rand abgeschnitten. „Speichern“, „Projekt schließen“ und Export sind bereits vor dem Öffnen eines Projekts optisch aktiv. Die Anwendung könnte in diesem Zustand die nächste sinnvolle Aktion deutlicher hervorheben. [MainWindow](../src/D3.Grc.Desktop/MainWindow.cs)
- **Tastatur und Assistenztechnik.** Dialoge brauchen nachvollziehbaren Startfokus, Escape-Verhalten und eine erkennbare Standardaktion. Gleich beschriftete Dashboard-Buttons brauchen eindeutigen Kontext. Automatisierte Feldnamen und Hilfetexte ergänzen sichtbare Beschriftungen. Die Ausgabe mit Screenreadern muss je Plattform geprüft werden. [Ui](../src/D3.Grc.Desktop/Ui.cs) · [Dashboard](../src/D3.Grc.Desktop/Dashboard.cs)

Die Fehlerpfade rund um Recovery, erneutes Speichern und Editorwechsel haben Vorrang vor Layout- oder Performance-Arbeit. Version 0.6.1 ist ein Entwicklungsstand zur Prüfung. Für die Produktionsfreigabe müssen diese Fehlerpfade behoben und die ausstehenden Plattform- und Migrationsprüfungen durchgeführt werden.

## Build und Prüfungen

- Restore über NuGet und Release-Build aller drei Projekte mit .NET SDK 10.0.100 auf Linux x64.
- 22 von 22 xUnit-Tests bestanden. Geprüft wurden unter anderem Anmeldung, Sperre nach Fehlversuchen, verschlüsselte Projektdateien, Migration und Wiederöffnen, Themenfilter, Mappingregeln, Auditabschluss und XLSX-Inhalt.
- 197 zusätzliche SQL-, XML- und Strukturprüfungen bestanden. Das Prüfskript wurde in einer Arbeitskopie ausgeführt, da es den historischen Prüfstand der Version 0.6 als Bericht ausgibt.
- Auf dem Linux-Testdesktop wurden ein lokales Profil und ein verschlüsseltes Testprojekt angelegt. Die finale Version öffnete das Projekt erneut. Der Datenschutzfilter reduzierte sowohl Anforderungen als auch Controls von jeweils 18 auf jeweils 3. Das gefilterte Control-Register zeigte die erwarteten drei Datensätze.
- Der Tooltip des Profilnamens erschien in der finalen Version beim Überfahren mit der Maus und bei fokussiertem Feld über F1. Escape schloss den Hinweis.
- Die mitgelieferte native SQLite-Bibliothek meldete Version 3.53.4. NuGet meldete für die drei Projekte und ihre transitiven Abhängigkeiten keine bekannten Schwachstellen in der abgefragten Quelle.
- Ein XLSX-Bericht aus einem synthetischen Datenschutzprojekt wurde mit der Exportfunktion erzeugt, mit OpenPyXL gelesen und in LibreOffice Calc im Headless-Modus geöffnet und als XLSX erneut gespeichert. Die anschließende Prüfung bestätigte alle 19 Arbeitsblätter, Text mit führendem Gleichheitszeichen, Datum, Zahlenzelle, Autofilter und fixierte Kopfzeile.
- Im Ausgangsmanifest waren eine CI-Datei und eine Git-Ignore-Datei aufgeführt, die im GitHub-Checkout fehlen. Das aktualisierte Manifest beschreibt den gelieferten Dateisatz.

Offen bleiben unter anderem DPI- und Screenreader-Tests, die native Abnahme auf Windows und macOS, die Migration einer echten 0.5-Projektkopie sowie die Abnahme in Excel und signierte Installer. Die mitgelieferte 0.5-Testdatenbank deckt den bekannten Migrationspfad ab.
