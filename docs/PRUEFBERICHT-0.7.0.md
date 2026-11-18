# D3 GRC 0.7.0 · Funktionsabgleich und Abnahme

Ausgangsbasis für die Weiterentwicklung war der Prüfbranch 0.6.1, Commit c4e2d9e. Die ZenGRC-Vergleichsseite war über den konfigurierten Cloud-Proxy nicht abrufbar. Der folgende Abgleich ordnet die verfügbare Software anhand gängiger GRC-Funktionen ein. Eine Einzelzuordnung der Aussagen und Produkte der Vergleichsseite steht aus. [ZenGRC-Vergleich](https://www.zengrc.com/compare/best-grc-software/)

## Funktionsabgleich

| Fachbereich | Lieferumfang 0.7.0 | Für breiteren Unternehmenseinsatz offen |
| --- | --- | --- |
| Unternehmensprofil und Prioritäten | Der Assistent erfasst Größe, Branche, Standorte, Organisationseinheiten, Themen, Risikofokus und betriebliche Merkmale. Er begründet Schlüsselaufgaben und optionale Ergänzungen. | Eigenständige Stammdaten und Hierarchien pro Geschäftseinheit, projektübergreifende Übersicht. |
| Governance und Regelwerke | Acht Fachthemen mit Quellen und Vorlagen, eigene Quellen, fachlich begründete Anwendbarkeit und Zuordnung. | Zentrale Richtlinienfreigabe, Kenntnisnahme, vollständige Normkataloge und automatische Folgenabschätzung. |
| Risiken und Maßnahmen | Risiken und Maßnahmen mit Zuständigkeit, Geltungsbereich, Termin, Quellenbezug und typisierten Zuordnungen. | Bewertungsmatrizen je Organisation, Freigabestufen, Terminbenachrichtigungen und Kennzahlenhistorie. |
| Controls und Evidence | Anforderung und Fundstelle, bestätigte Control-Bezüge, Abdeckung und Geltungsbereich sowie zugeordnete Nachweise in einer Ansicht. Nachweise sind wiederverwendbar. | Automatische Evidence-Anbindungen, zentrale Freigaben und laufende Prüfungen. |
| Audit und Findings | Audit-Scope, Kriterienstand und Prüfresultate, begründete Findings und Wirksamkeitsnachweise. | Mehrstufige Review- und Freigabeprozesse, Auditplanung über mehrere Projekte. |
| Drittparteien und Resilienz | Unternehmensplan priorisiert Cloud-Dienste, IKT-Dienstleister, Notfallvorsorge und Softwarelieferkette mit passenden vorhandenen Arbeitsbereichen. | Lieferantenregister mit Bewertungen, Fragebögen, Verantwortlichkeiten, Fristen und Integrationen. |
| Berichte und Zusammenarbeit | Übersicht mit anklickbaren Kennzahlen, XLSX- und HTML-Export einschließlich Unternehmensplan, lokales Journal und verschlüsselte Projektdatei. | Gleichzeitige Bearbeitung, zentrale Benutzerverwaltung, SSO, Rollen, Mandanten und angebundene Systeme. |

Der Produktschwerpunkt ist der nachvollziehbare Einstieg vom Firmenprofil bis zum Nachweis. Kleine Teams erhalten eine kurze, begründete Liste für den Einstieg. Größere Organisationen können dieselbe Arbeitsweise für mehrere Geltungsbereiche anwenden und gemeinsame Controls sowie Nachweise wiederverwenden. Die Empfehlungen sind Arbeitshilfen. Die fachliche Entscheidung wird am Datensatz mit Quelle und Begründung festgehalten.

## Bedien- und Datenflüsse

- **Registrierung und Projektanlage.** Nach der Profilerstellung startet die angemeldete Sitzung mit dem Assistenten. Der Assistent speichert das Unternehmensprofil beim Anlegen zusammen mit dem verschlüsselten Projekt. Anschließend steht es direkt in der Übersicht offen. Abbruch lässt das lokale Profil zur normalen Nutzung verfügbar.
- **Unternehmensplan.** KMU- und Großunternehmensprofil unterscheiden sich nach Projektkontext, Aufgaben und Begründung. Relevante Bereiche können aus dem Plan direkt geöffnet werden. Das vollständige Fachangebot bleibt erreichbar. Bei Projekten ohne gespeichertes Profil kann dieses später ergänzt werden.
- **Übersicht.** Kennzahlen und verlinkte Arbeitsbereiche stehen im gefilterten Kontext. Die Anzeige der Nachweise beschreibt die zeitliche Gültigkeit und Zuordnung. Die Prüfung des Dateihashs gehört zu den Funktionen, die den Nachweis tatsächlich verwenden.
- **Textvorlagen und weitere Editoren.** Auswahl und Navigation vergleichen den tatsächlichen Formularinhalt mit dem zuletzt geladenen oder gespeicherten Stand. Bei einer Abweichung stehen Speichern, Verwerfen und Abbrechen zur Wahl.
- **Eigene Quellen.** Titel, Fundstelle, Beschreibung, Themen und eine optionale HTTPS-Adresse werden im Projekt gepflegt. Der Status macht die manuelle Pflege sichtbar. Die Funktion für automatische Abrufe bleibt auf die vorgesehenen offiziellen Referenzen beschränkt.
- **Wiederherstellung und Speicherung.** Beim Öffnen der Hauptdatei bleibt ein vorhandener Wiederherstellungsstand separat als verschlüsselte Projektdatei erhalten. Nach einem bereits erfolgreichen Datenbank-Commit schreibt ein Wiederholungsversuch den Projektstand, ohne die Formularänderung zu wiederholen. Anwendbarkeitsentscheidungen behalten bei reinen Textänderungen ihre frühere Begründung. Bei Finding-Abschlüssen wird der gewählte Nachweis im Journal festgehalten.

## Plattformgrenzen

Die Software arbeitet lokal mit projektbezogener Dateisperre. Bei einem großen Unternehmen kann sie von einzelnen Verantwortlichen oder für getrennte Projekte genutzt werden. Eine zentrale Mehrbenutzerplattform erfordert weitere Architekturarbeit. Eine Aktualisierung bestehender Projektdateien sollte zuerst mit einer Kopie verifiziert werden.

## Ergebnis der Abnahme

- Release-Build mit .NET SDK 10.0.100 unter Linux x64. Alle drei Projekte kompilierten mit null Warnungen und null Fehlern.
- 37 xUnit-Tests bestanden. Die Tests decken unter anderem Profilregeln und verschlüsseltes Wiederöffnen, Legacy-Projekte, 19 Exportblätter ohne Profil und die zusätzliche Planansicht bei konfigurierten Projekten, eigene Quellen und ihre Themen, bestätigte Zuordnungen und Nachweisstatus ab.
- Zwei Fälle blockierten den Datei-Schreibvorgang absichtlich. Der erneute Dateischreibvorgang erhielt genau einen vorhandenen Datensatz. Ein bereits vorhandener neuerer Wiederherstellungsstand blieb auch nach Autosave und erneutem Speichern als eigene Projektdatei öffnbar.
- 202 SQL-, XML- und Strukturprüfungen bestanden in einer Kopie des Dateisatzes. NuGet meldete für die direkten und transitiven Abhängigkeiten keine bekannten Schwachstellen bei der abgefragten Quelle.
- In der nativen Linux-Oberfläche wurden Registrierung, Eingabevalidierung im Assistenten, Anlage und direktes Öffnen einer verschlüsselten Projektdatei sowie die Unternehmensübersicht geprüft. Die Quellenprüfung umfasste den Eingabefehler für eine HTTP-Adresse, das Anlegen und Bearbeiten einer eigenen HTTPS-Referenz und die Anzeige im zugeordneten Thema.
- Die überarbeitete Navigation und das Dashboard wurden bei 1440 × 960 und 1120 × 760 geprüft. In der kleineren Ansicht brachen die Filteraktionen um und weitere Inhalte blieben durch Scrollen erreichbar. Ein F1-Tooltip wurde am fokussierten Anmeldefeld geprüft.
- Unveränderte Textvorlagen konnten ohne Rückfrage gewechselt werden. Bei Änderungen wurden Speichern, Verwerfen und Abbrechen mit Rückkontrolle in Textvorlagen und Quellenbewertungen geprüft. Nach Abbruch eines nachgelagerten Dialogs blieb die noch sichtbare Bewertung als ungespeichert markiert. In einem absichtlich ausgelösten Schreibfehler blieb der bereits lokal übernommene Vorlagenstand genau einmal im Journal dokumentiert. Erneutes Speichern schrieb ihn erfolgreich in die Projektdatei.

Windows und macOS, Screenreader, eine reale Altprojektdatei und Microsoft Excel waren nicht Teil dieser Abnahme. Der Export wurde über die erzeugten XLSX-Arbeitsblätter und die HTML-Ausgabe geprüft.
