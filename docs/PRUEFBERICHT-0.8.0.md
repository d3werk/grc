# D3 GRC 0.8.0 · Lieferantenregister

0.8.0 baut auf dem geprüften Unternehmensassistenten und Arbeitsbereich aus 0.7.0 auf. Das Lieferantenregister schließt eine konkrete Lücke im bisherigen Funktionsabgleich. [Prüfbericht 0.7.0](PRUEFBERICHT-0.7.0.md)

## Ablauf im Projekt

| Schritt | Umsetzung |
| --- | --- |
| Relevanz erkennen | Das Unternehmensprofil berücksichtigt externe Lieferanten und Dienstleister. Betriebliche Merkmale sowie Cloud-, Drittparteien- und DORA-Bezug können das Register zu einer wesentlichen Aufgabe im Umsetzungsplan machen. |
| Dienstleistung erfassen | Ein Lieferanteneintrag verbindet Leistung, Kritikalität, Vertragsreferenz und Risikobegründung mit Fachthema, Geltungsbereich, Quelle, Prüftermin und Verantwortung. |
| Prüfung dokumentieren | Das Prüfdatum, das Ergebnis, die Begründung, die dokumentierende Person und der nächste Termin erscheinen im Verlauf. Eine nacherfasste ältere Prüfung bleibt im Verlauf und ändert den aktuellen Bewertungsstand nicht. |
| Zusammenhänge herstellen | Lieferanten lassen sich typisiert mit Risiken und Controls verbinden. Maßnahmen können dem Lieferanten zugeordnet werden. Für Nachweise steht die bestehende Control-Nachweisverwaltung zur Verfügung. |
| Handlungsbedarf finden | Die Übersicht zählt Lieferanten, kritische Einstufungen sowie fällige, überfällige oder fehlende Prüftermine im gewählten Kontext. XLSX und HTML enthalten Lieferanten und Prüfverlauf passend zum Exportfilter. |

Ein KMU kann mit den wichtigsten Dienstleistern beginnen. Größere Organisationen können Dienstleister bestimmten Geltungsbereichen und Verantwortungen zuordnen und wiederverwendbare Controls für gemeinsame Prüfungen einsetzen.

## Migration und Betriebsgrenzen

Die Migration 007 ergänzt Lieferantendaten und Prüfungen zum bestehenden SQLite-Schema. Mit der neuen Version lassen sich ältere Dateien desselben verschlüsselten Containerformats öffnen. Vor der Migration wird der ausgewählte Projektstand als verschlüsselte `.pre08-*.bak`-Datei gesichert. Eine Kopie der Sicherung mit der Endung `.d3grc` lässt sich wieder in der früheren Version öffnen. Die Abnahme verwendet synthetische Daten und Projektkopien.

Das Register dokumentiert organisationsinterne Bewertungen. Ein regulatorisches DORA-Informationsregister mit vorgeschriebenen Tabellenformaten, Lieferantenportale, SSO, zentrale Rollen und gleichzeitige Bearbeitung über mehrere Arbeitsplätze stehen aus. Der Cloud-Proxy verweigerte den Zugriff auf die ZenGRC-Vergleichsseite auch beim erneuten Versuch. Der Abgleich aller einzelnen Aussagen dieser Seite bleibt offen. [ZenGRC-Vergleich](https://www.zengrc.com/compare/best-grc-software/)

## Ergebnis der Abnahme

- Der Release-Build aller drei Projekte mit .NET SDK 10.0.100 unter Linux x64 lief ohne Warnungen und Fehler.
- 44 xUnit-Tests bestanden. Die neuen Fälle prüfen Profilpriorisierung, atomare Speicherung von Stammdaten, mehrere Prüfungen in zeitlicher Reihenfolge, Typprüfung der Zuordnungen, Datei-Schreibretry ohne doppelte Bewertung und verschlüsseltes Wiederöffnen.
- Eine ältere verschlüsselte Projektdatei wurde in einem Integrationstest migriert. Das gespeicherte Unternehmensprofil blieb erhalten und die verschlüsselte Sicherung war byte-identisch zum zuvor geöffneten Stand.
- XLSX und HTML wurden auf Thema, Geltungsbereich und Suche geprüft. XLSX enthält native Datumszellen. Die Abschnitte „Lieferanten“ und „Lieferantenprüfungen“ erscheinen bei Treffern, die bisherigen Berichtsblätter bleiben verfügbar.
- 217 SQL-, XML- und Strukturprüfungen bestanden. Das Skript prüft zusätzlich die vorhandenen Zeilen vor und nach Migration 007 sowie beide Schutzregeln der Bewertungshistorie. NuGet meldete für die abgefragte Quelle keine bekannten Schwachstellen in den direkten und transitiven Abhängigkeiten.
- Im Linux-Desktop öffnete 0.8.0 die Kopie einer tatsächlich mit 0.7.0 erzeugten Projektdatei. Unternehmensprofil und eigene Quelle waren weiterhin vorhanden. Die verschlüsselte Sicherung der Projektkopie war byte-identisch mit der 0.7.0-Testdatei. Das Register ließ einen Lieferanten anlegen und hielt eine Bearbeitung beim Abbrechen der Wechselabfrage im Formular.
- In einem Zwischenstand löste die Aktualisierung der Liste nach einer erfolgreich geschriebenen Bewertung eine Fehlermeldung aus. Die Zeilenvorlage behandelt jetzt die beim Ersetzen einer Zeile auftretenden leeren Inhalte. Im Wiederholungstest zeigten Liste und Verlauf unmittelbar den neuen Status, Folgetermin und alle drei Prüfungen.
- Am selben Projekt zeigte das Dashboard die erwarteten Lieferantenkennzahlen. Die Oberfläche speicherte eine bestätigte Lieferant-Control-Zuordnung mit der mitmigrierten eigenen Quelle. Nach dem Schließen und erneuten Öffnen im finalen Build waren die Bewertungen und die Zuordnung weiterhin vorhanden.

Native Abnahme und Bedienprüfung mit Screenreadern auf Windows und macOS sowie eine Prüfung mit echten Altdaten stehen aus.
