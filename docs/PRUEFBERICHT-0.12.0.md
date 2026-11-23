# Prüfbericht D3 GRC 0.12.0

**Stand 23. November 2026.** Die Abnahme verwendet synthetische Projekte und Mandanten. Geprüft wurden die zuletzt bereitgestellte Desktop- und Web-Anwendung, der Rich-Text-Editor, der Wechsel des Arbeitsbereichs und die visuelle Überarbeitung. [Änderungsverlauf](../CHANGELOG.md)

## Desktop

- In **Beschreibung / Umsetzung** und den Texten für Vorlagen und Dokumente stehen Fett, Kursiv, Unterstreichen, Überschriften, Aufzählungen und Rückgängig/Wiederholen zur Verfügung. Die Vorschau zeigt die Formatierung. Suche und Exporte verwenden den lesbaren Text. Das Wiederöffnen formatierter Inhalte in einer verschlüsselten Projektdatei wurde getestet. [Textbearbeitung](ANWENDERHANDBUCH.md#4-datensatz-bearbeiten) · [Persistenztests](../tests/D3.Grc.Tests/RichTextPersistenceTests.cs)
- **Mandant wechseln** ist in der Kopfzeile erreichbar. Das Fenster bietet eine verschlüsselte Projektdatei, kürzlich verwendete Projekte der laufenden Anmeldung und die Projektanlage über den Unternehmensassistenten. Jeder Dateiwechsel erfordert das zugehörige Projektpasswort. Bei einem fehlgeschlagenen Wechsel bleibt das bisherige Projekt nutzbar. Die Tests decken ein falsches Passwort, eine gesperrte Zieldatei, einen fehlgeschlagenen Speichervorgang und die Freigabe der bisherigen Dateisperre ab. [Wechseltests](../tests/D3.Grc.Tests/WorkspaceSwitchTests.cs)
- Vorhandene Projektdateien und unformatierte Texte lassen sich weiterverwenden. Neu gespeicherte Formatierungen benötigen Desktop 0.12.0 zur Anzeige. Die Liste kürzlich verwendeter Projekte gilt für die laufende lokale Anmeldung. [Anwenderhandbuch](ANWENDERHANDBUCH.md)

## Web

- Der Editor speichert zugelassene Textstrukturen und Formatierungen in getrennten JSON-Feldern. Migration 009 ergänzt die Felder für Registertexte, Verknüpfungen und Risikobewertungen. Die API liefert weiterhin Klartext für Suche, bestehende Clients und Berichte. Änderungen an einzelnen Klartextfeldern erhalten die Formatierung unveränderter Felder. Die API weist unzulässige Knotentypen einschließlich HTML ab. [Migration 009](../platform/backend/migrations/009_rich_text.sql) · [API-Vertrag](../platform/openapi.json)
- Die Mandantenauswahl enthält die in diesem Tab verbundenen Arbeitsbereiche. Die Erstanmeldung je Mandant benötigt dessen gültige Zugangsdaten. Beim Wechsel wird die Sitzung geprüft. Die Oberfläche lädt danach Rollen, Register und Filter des gewählten Mandanten. Die Tests decken auch abgewiesene Anmeldungen, fremde Objekt-IDs, den Sitzungswiderruf und das Abmelden verbundener Sitzungen ab. [Browser-Mandantentest](../platform/frontend/tests/tenant-switch.cjs)
- Der Browser-Test prüft, dass bereits eine reine Formatänderung als Bearbeitung zählt. Eine unveränderte Ansicht lässt sich direkt verlassen. Entwürfe können gespeichert oder verworfen werden. Gespeicherte Formatierungen erscheinen nach erneutem Öffnen. Der Editor-Code lädt beim Öffnen einer Bearbeitung. [Browser-Ablauftest](../platform/frontend/tests/platform-flow.cjs)

## Gestaltung

Das mitgelieferte D3-Werk-Logo ist die Referenz für Grün, Cyan und Taupe auf hellen Flächen. In der Navigation erscheint das Zeichen dezent. Der Originalgrafik entspricht auch die im Webpaket ausgelieferte Datei. Für `d3-werk.de` und `www.d3-werk.de` lieferte der direkte Abruf HTTP 403. Die Gestaltungsabnahme bezieht sich deshalb auf das bereitgestellte Original-Logo und die Anwendungsscreenshots.

## Technische Prüfung

| Prüfung | Ergebnis |
| --- | --- |
| Backend | 13 Tests mit PostgreSQL 17 bestanden. Der bestehende TestClient weist auf eine veraltete Entwicklungsabhängigkeit hin. |
| Browser | Durchgängiger Arbeitsablauf und Mandantenwechsel mit Chromium und synthetischen Daten. Beide Playwright-Suiten bestanden auch gegen das fertige Compose-Image. |
| OpenAPI | Version 0.12.0 und Rich-Text-Schema validiert. |
| Abhängigkeiten | npm meldete keine bekannten Schwachstellen im geprüften Paketstand. |
| Desktop | Linux-Release-Build ohne Warnungen oder Fehler, 69 xUnit-Tests und 453 unabhängige SQL-, XML- und Strukturprüfungen bestanden. |
| Desktop-Oberfläche | Projektdatei aus 0.10.0 geöffnet, Control-Beschreibung mit Überschrift und Fettdruck formatiert und gespeichert. Bei falschem Passwort blieb das erste Projekt geöffnet. Nach dem erfolgreichen Wechsel und der Rückkehr über „Zuletzt verwendet“ waren beide Formatierungen vorhanden. |
| Compose | Gepacktes Image mit neuem PostgreSQL-Volume gestartet. Migrationen bis 009, Bereitschaft, OpenAPI-Vertrag und Auslieferung des Original-Logos bestätigt. |

Die Web-Anwendung wurde lokal gegen PostgreSQL geprüft. Die verschlüsselten Desktop-Projekte und die Web-Mandanten haben jeweils ihren eigenen Speicher und ihre eigene Anmeldung. [Plattformstart](../platform/README.md)
