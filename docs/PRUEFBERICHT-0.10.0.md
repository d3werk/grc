# D3 GRC 0.10.0 · Risikobewertung

Diese Version ergänzt den geprüften Stand aus 0.9.0 um quantitative Risikobewertung, Verlauf und eine anklickbare Matrix. Der Cloud-Proxy verweigert weiterhin den Abruf der Swiss-GRC-Plattformseite. Der Vergleich mit der vollständigen Modulübersicht bleibt offen. [Prüfbericht 0.9.0](PRUEFBERICHT-0.9.0.md) · [Swiss GRC Plattform](https://swissgrc.com/plattform/)

## Bewertung und Behandlung

1. Im Risikoregister ein konkretes Szenario mit Fachthema, Geltungsbereich und Verantwortung erfassen. Ursachen, betroffene Leistungen und die mögliche Auswirkung beschreiben.
2. Unter **Bewertungen** Bewertungsdatum, Eintrittswahrscheinlichkeit und Auswirkung des Ausgangsrisikos festhalten. Annahmen und Beobachtungszeitraum in der Begründung erklären.
3. Controls und geplante Maßnahmen über Mappings zuordnen. Beide Restrisikowerte erfassen, sobald eine Beurteilung mit den berücksichtigten Controls vorliegt. Die Behandlungsentscheidung und ihre Gründe dokumentieren.
4. Bei Änderungen eine weitere Bewertung hinzufügen. Die Risikomatrix und die Kennzahlen verwenden die zeitlich jüngste Bewertung. Eine später eingetragene Bewertung mit älterem Bewertungsdatum ergänzt den Verlauf.
5. In der Matrix zwischen Ausgangs- und Restrisiko wechseln und eine Zelle für die Detailansicht öffnen. Thema, Geltungsbereich und Suche bleiben wirksam.

## Bewertungsmethode

Eintrittswahrscheinlichkeit und Auswirkung erhalten je einen ganzen Wert von 1 bis 5. Für das Produkt beider Werte gilt folgende Einteilung. Fachliche Maßstäbe und Grenzfälle gehören in die Begründung. Die Werte beschreiben die Einschätzung im gewählten Projektkontext.

| Wert | Stufe |
| --- | --- |
| 1–4 | Niedrig |
| 5–9 | Mittel |
| 10–15 | Hoch |
| 16–25 | Kritisch |

Die Matrix weist Risiken ohne Bewertung aus. Ist nur das Ausgangsrisiko bewertet, bleibt das Restrisiko offen. Die Anzahl hoher und kritischer Restrisiken bezieht sich auf die tatsächlich eingetragenen Werte. Die App berechnet keine Schätzung aus dem Vorhandensein eines Controls.

Ein Beispiel ist der Ausfall eines zentralen Dienstes mit den Ausgangswerten Eintritt 4 und Auswirkung 5. Der Risikowert 20 ist kritisch. Werden Reaktionszeit und Ersatzbetrieb fachlich bewertet, kann sich daraus beispielsweise Eintritt 2 und Auswirkung 4 ergeben. Der Restrisikowert 8 liegt in der mittleren Stufe. Die Begründung hält Szenario, Annahmen und bei der Restrisikobewertung die berücksichtigten Controls fest.

## Datenhaltung und Bericht

Bewertungen werden im verschlüsselten Projekt fortlaufend gespeichert. Datum, verantwortliches lokales Profil, Entscheidung, Begründung und Bezug zur gespeicherten Fassung des Risikos bleiben nachvollziehbar. In den Berichten erscheinen Bewertungsverlauf und aktueller Eintrag innerhalb des gewählten Filters.

Migration 009 enthält eine separate Bewertungstabelle. Sie übernimmt vorhandene Risiken ohne zusätzliche Bewertungen. Beim Öffnen eines 0.9.0-Projekts wird der ursprüngliche verschlüsselte Stand vor der Migration als `.pre010-*.bak` gesichert.

## Abnahme

- Der Release-Build der drei Projekte lief unter Linux x64 mit .NET SDK 10.0.100 ohne Warnungen und Fehler. Alle 65 Regressionstests bestanden. Sie prüfen Bewertungsgrenzen, die getrennte Erfassung von Ausgangs- und Restrisiko, Historie, Dateischreibretry, Migration und gefilterte Berichte.
- 450 unabhängige SQL-, XML- und Strukturprüfungen bestätigten unter anderem die Unveränderlichkeit früherer Risikobewertungen und den Erhalt der bisherigen Daten. NuGet meldete für die abgefragte Quelle keine bekannten Schwachstellen der direkten und transitiven Pakete.
- Im Linux-Desktop wurde die Kopie einer mit 0.9.0 erstellten Datei migriert. Risikoeintrag, Maßnahme und alle 157 Vorlagen waren erhalten. Die verschlüsselte Sicherung war bytegleich mit dem Original.
- Der native Risikotest zeigte 20 Punkte im kritischen Ausgangsrisiko und sechs Punkte im mittleren Restrisiko. Eine später erfasste ältere Bewertung blieb im Verlauf. Beide Matrixansichten, Zellfilter, F1-Hinweise und der Einstieg in den ausgewählten Datensatz wurden geprüft. Bei 1120 × 760 blieb das Raster per Scrollen erreichbar.
- Nach erneutem Öffnen mit dem finalen Build waren beide Bewertungen und die in der Oberfläche gespeicherte Maßnahme-Risiko-Zuordnung vorhanden. Beim Filtern auf ein neu angelegtes, unbewertetes Risiko blieb die Restrisikoansicht aktiv und die Matrix wies null eingezeichnete Risiken aus.
- Ein erzeugter XLSX-Bericht wurde mit OpenPyXL gelesen und mit LibreOffice Calc als XLSX erneut gespeichert. Filter, Bewertungsdatum, Zahlenwerte und leere Restrisikofelder wurden in der ursprünglichen Datei und nach dem erneuten Speichern geprüft.

Die native Prüfung nutzte synthetische Daten unter Linux. Die Abnahme unter Windows und macOS sowie mit Screenreadern steht aus.
