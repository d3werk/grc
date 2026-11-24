# Anwenderhandbuch D3 GRC Desktop

In Version 0.6.1 bieten zentrale Felder und Aktionen kurze Bedienhinweise. Den Mauszeiger auf dem Element stehen lassen oder das Element mit der Tastatur fokussieren und F1 drücken. Escape schließt einen geöffneten Hinweis.

## 1. Registrierung und Anmeldung
Profilname und Profilpasswort eingeben. „Profil erstellen“ legt ein lokales Profil an. Nach der ersten Anmeldung öffnet sich der Unternehmensassistent als Fenster über dem Arbeitsbereich. Beim Abbrechen bleibt das Profil angemeldet. Nach fünf fehlgeschlagenen Anmeldeversuchen wird das Profil vorübergehend gesperrt.

Die Anmeldung entsperrt den verschlüsselten Mandantenspeicher dieses Profils. Die Anwendung verwendet für jeden Mandanten einen eigenen Schlüssel. Die Anmeldung und die lokale Arbeitskopie folgen dem Einzelplatzmodell.

## 2. Mandanten einrichten, wechseln und sichern

- **Neuer Mandant.** Unternehmensart, Größe, Standorte, Organisationseinheiten und Risikofokus eingeben. Die Unternehmensart empfiehlt Merkmale und Fachthemen. Selbst gesetzte oder entfernte Häkchen bleiben beim Wechsel der Unternehmensart erhalten. Der Assistent zeigt daraus einen Umsetzungsplan und öffnet den intern gespeicherten Mandanten.
- **Mandant wechseln.** Oben rechts im Dropdown einen gespeicherten Mandanten wählen. Die Anwendung nutzt die laufende Profilanmeldung. Bei ausstehenden Formularänderungen lässt sich speichern, verwerfen oder zum Formular zurückkehren.
- **Bestehende Projektdatei übernehmen.** Unter **Mandanten verwalten → Bestehende Projektdatei importieren** die vorhandene verschlüsselte `.d3grc`-Datei auswählen und einmal ihr bisheriges Projektpasswort eingeben. Die Anwendung legt eine intern verwaltete Kopie an. Danach lässt sich der Mandant über das Dropdown öffnen.
- **Mandant exportieren.** Den gewünschten Mandanten im Dropdown auswählen und **Mandant exportieren** wählen. Das verschlüsselte `.d3grcbackup`-Archiv enthält den vollständigen Projektstand, einschließlich Unternehmenseinstellungen, Dokumenten, Historie und Nachweisdateien.
- **Alle Mandanten sichern.** Unter **Mandanten verwalten → Alle Mandanten sichern** ein Archiv für die Mandanten des angemeldeten Profils erstellen. Den angezeigten Sicherungsschlüssel getrennt ablegen. Zum Übertragen oder Wiederherstellen werden Archiv und Schlüssel benötigt.
- **Mandantenarchiv wiederherstellen.** Das Archiv auswählen, den Sicherungsschlüssel eingeben und die wiederhergestellten Mandanten im Dropdown öffnen. Die Wiederherstellung fügt separate Kopien hinzu.
- **Demo-Unternehmen öffnen.** Im Assistenten oder unter **Mandanten verwalten** auswählen. Nordlicht Service GmbH ist ein fiktiver, eigenständiger Mandant mit synthetischen Beispieldaten.

## 3. Thema, Geltungsbereich und Suche
Das Thema oben gilt für die aktuelle Ansicht. Alle Themen ist eine Gesamtansicht. In den Fachmodulen wird bei der Gesamtansicht deren vorgesehenes Thema für neue Einträge verwendet. Für die allgemeinen Register ein konkretes Thema auswählen. Die Fachgebiete sind Datenschutz, Informationssicherheit, NIS2, KI-Governance, Cloud Security, BCM, Open Source, DORA und Prozessmanagement.

Zuordnung prüfen enthält migrierte Objekte ohne eindeutig ableitbaren Themenbezug. Nicht automatisch alle internen Controls allen Themen zuordnen. Einen passenden Fachkontext auswählen und das Objekt dort über einen begründeten Themenbezug ergänzen.

Das Dropdown **Geltungsbereich** zeigt die in der geöffneten Projektdatei gespeicherten Bereiche. Bereich wählen und **Filter anwenden** drücken. **Alle Geltungsbereiche** zeigt die Einträge aller Bereiche. Die Suche findet Titel, Kürzel, Beschreibung und Verantwortung. Das Glossar bietet eine eigene Begriffssuche. Quellen und Vorlagen folgen dem Thema.

Beim Thema **Datenschutz** steht im Bereichsfeld **Projektweit**. Die Datenschutzansichten und -exporte berücksichtigen Einträge und Dokumente aus allen Bereichen des Projekts. Im Detail lässt sich die gespeicherte Zuordnung jedes Eintrags ablesen. Bei einem Wechsel in ein anderes Thema steht der zuvor gewählte Bereich wieder zur Verfügung.

## 4. Datensatz bearbeiten
Eintrag links auswählen. Rechts stehen Tabs bereit:
- Details: Titel, Status, Verantwortung, Termin, Scope, Beschreibung und Änderungsbegründung.
- Quellen: primäre Quelle, konkrete Fundstelle, passende Vorlage und zusätzliche Quellen-/Themenbezüge.
- Bezüge: Mappings und fachliche Arbeitsabläufe.
- Evidence: tatsächliche Nachweise eines Controls.
- Historie: protokollierte Änderungen des Objekts.

Vorlage übernehmen kopiert den Text ins Formular. Eigene Ergänzungen sind erwünscht. Erst Änderungen speichern legt den konkreten Datensatz an beziehungsweise aktualisiert ihn und schreibt die verschlüsselte Hauptdatei. Ein Vorlagentext ist kein Nachweis einer Umsetzung.

In **Beschreibung / Umsetzung** lassen sich Textstellen markieren und über die Werkzeugleiste formatieren. Die Bearbeitung und Vorschau erfolgen im Textfeld. Bestehende Beschreibungen bleiben als Ausgangspunkt erhalten.

## 5. Speichern
Die globale Schaltfläche Speichern und Strg/Cmd+S speichern ein bearbeitetes Formular über dessen Speicherfunktion. Ohne Formularänderungen wird die Hauptdatei gespeichert.

Mandant exportieren übernimmt ein offenes Formular und erzeugt anschließend ein vollständiges verschlüsseltes Mandantenarchiv mit eigenem Sicherungsschlüssel.

Bei einem Wechsel fragt die Anwendung nur nach tatsächlichen Formularänderungen. „Speichern“ übernimmt die Änderung und setzt den Wechsel fort. „Verwerfen“ setzt den Wechsel ohne Übernahme fort. „Abbrechen“ behält den vorherigen Eintrag und die Bearbeitung bei.

Meldet die Anwendung, dass eine Änderung bereits übernommen wurde, aber die Hauptdatei noch nicht gespeichert ist, zuerst den Dateifehler beheben und erneut „Speichern“ wählen. Öffnen der Hauptdatei bei vorhandener Recovery erhält den bisherigen Wiederherstellungsstand als separat öffnbare verschlüsselte Projektdatei.

## 6. Quellen passend auswählen
Primäre Quellen und Vorlagen sind auf das Thema begrenzt. Beispiel Datenschutz: DSGVO, BDSG und interner Arbeitsstandard. BCM: BSI-Standard 200-4 und interner Arbeitsstandard. Ein konkretes Control kann mehrere Themen unterstützen.

Weitere Quelle hinzufügen ist eine bewusste Cross-Zuordnung mit Fundstelle und Begründung. Ein bestehendes Objekt mit ausdrücklich ergänztem Themenbezug bleibt in diesem Thema bearbeitbar. Zusätzliche Quellen sind am Objekt sichtbar; die globale Quellenliste bleibt der standardmäßigen Themenpolitik zugeordnet.

Unter „Quellen“ lassen sich eigene Quellen mit Titel, Fundstelle, Beschreibung und einem oder mehreren Themen anlegen. Eine HTTPS-Adresse ist optional. Eigene Quellen stehen in den zugeordneten Themen zur Auswahl und werden manuell gepflegt.

Anforderungsquelle und Nachweisherkunft sind getrennt. Ein Konfigurationsexport ist ein Nachweis, kein Gesetz.

## 7. Mappings
Beziehungstyp auswählen. Ausgangsobjekt stammt aus dem Kontext. Zielobjekte stammen standardmäßig ebenfalls aus dem Kontext; zusätzliche Themen/Scopes können bewusst eingeblendet werden. Quelle muss am Ausgangsobjekt vorhanden sein. Abdeckung und Begründung ergänzen und Zuordnung speichern.

Vorbereiteten Vorschlag laden füllt nur das Formular. Erst Speichern erzeugt eine aktive Beziehung. Teilweise, Vollständig und Unterstützend sind fachliche Bewertungen, keine automatische Normgleichwertigkeit.

## 8. Evidence
Control auswählen. Die Quellenliste wird aus den Quellenbezügen dieses Controls gebildet. Optionale Nachweisvorlage übernehmen, Titel und tatsächliche Herkunft/Scope ergänzen. Gültigkeit festlegen und echte Datei importieren.

Maximal 8 MiB je Datei, 96 MiB Gesamtinhalt. Dateien werden nicht automatisch ausgeführt. Wiederverwendung für weitere Controls ist möglich. Hash und Gültigkeit werden bei Verwendung geprüft. Evidence-Export erzeugt eine unverschlüsselte Datei nach Warnung.

## 9. Audits und Findings
Audit anlegen, auf In Prüfung setzen und unter Mappings über Audit → Control den Scope festlegen. Im Audit unter Bezüge Prüfungen dokumentieren öffnen.

Nur Controls im Audit-Scope werden angeboten. Prüfquelle stammt vom Control. Passende gültige Evidence auswählen. Prüftext übernehmen und tatsächliche Beobachtung/Stichprobe ergänzen. Bestanden benötigt Evidence. Abweichung benötigt vor Abschluss ein Finding mit Audit- und Control-Bezug. Nicht anwendbar benötigt eine begründete Notiz.

Ein Audit kann abgeschlossen sein, während Findings offen bleiben. Historische Evidence-Gültigkeit wird zum Prüfzeitpunkt beurteilt, nicht rückwirkend nach dem heutigen Datum. Bei neuen Tests wird die Controlversion mit Quellenbezügen als Kriterien-Snapshot gespeichert. Alte importierte Audits erhalten keine erfundenen historischen Snapshots.

Finding auf Wirksamkeitsprüfung setzen, passenden Nachweis und Prüfbegründung auswählen und schließen. Bei mehreren betroffenen Controls muss der gewählte Nachweis allen zugeordnet sein; gegebenenfalls Evidence zuvor wiederverwenden. Maßnahmen werden separat über Maßnahme → Finding verknüpft.

## 10. Quellen aktualisieren
Quelle auswählen und Quelle aktualisieren / prüfen anklicken. Externen Abruf bestätigen. Die Anwendung verwendet eine feste HTTPS-Hostliste, Zeit-/Größenlimits und prüft Weiterleitungen. Projektinhalte werden nicht übertragen.

Erstabruf oder Inhaltsänderung erzeugt Prüfbedarf. Fehler verändern den letzten erfolgreichen Abrufzeitpunkt nicht. Eigene Texte bleiben unverändert. Referenzstand bestätigen dokumentiert eine begründete Bestätigung des technischen Stands; das ist keine automatische juristische Freigabe. Layoutänderungen können ebenfalls einen neuen Hash erzeugen.

## 11. Dashboard
Management: Bestandszahlen, Anwendbarkeit und Datenqualität.
Umsetzung: Statusbalken der Controls.
Nachweise & Audits: aktuelle Nachweisabdeckung und Findingstatus.
Quellen & Qualität: themenbezogene Quellen und Prüfhinweise.

**Ansehen** öffnet die passende Detailansicht. Balken zeigen die Anzahlen im aktuellen Bestand. **Risikomatrix öffnen** führt zur 5×5-Ansicht mit demselben Thema, Geltungsbereich und Suchfilter.

## 12. Excel und Druck
Excel exportieren erzeugt eine echte `.xlsx`-Datei ohne installierte Excel-Anwendung. Exportiert werden Thema, Scope und Objektsuche für alle Objekttypen im Kontext. Lokale Statusfilter einer einzelnen Liste werden nicht übernommen; der Exportdialog weist darauf hin.

Arbeitsblätter enthalten Kontext, Kennzahlen, Anforderungen, Controls, Risiken, Audits, Findings, Maßnahmen, KI-/OSS-Register, Mappings, Evidence-Metadaten, Prüfungen, Quellen, Statusverteilungen, Glossar und Definitionen. Mappings können auf Objekte außerhalb des Filters verweisen; diese sind über IDs erkennbar. Evidence-Dateiinhalte werden nicht eingebettet.

Texte werden als Textzellen geschrieben, nicht als Formeln. Zahlen und geeignete Datumsfelder erhalten passende Zelltypen. Überschriften sind fixiert, Filter vorhanden. Diagramme sind im Dashboard; eingebettete Excel-Diagramme sind noch nicht enthalten.

Drucken / PDF erzeugt eine lokale HTML-Datei und öffnet den Standardbrowser. Dort Drucken oder als PDF speichern. Kein stiller Druck und kein eingebetteter nativer Druckdialog.

## 13. Logout und Sperren
Beide Aktionen beenden in diesem Einzelplatzstand die Sitzung vollständig: Formularänderungen behandeln, Projektzustand sichern, Projekt schließen, Schlüsselkontext und lokale Dateisperre freigeben, zur Anmeldung zurückkehren. Zum Weiterarbeiten erneut im lokalen Profil anmelden.

## 14. Grenzen
Aktive SQLite-Arbeitskopien sind Klartext im privaten Benutzerverzeichnis. Nach Absturz können Reste bestehen. Erst verschlüsselte Recovery prüfen, danach verwaiste Arbeitskopien bereinigen. Datenträgerverschlüsselung verwenden. Exporte und Backups nach dem eigenen Schutz-/Löschkonzept behandeln.

## 15. Lieferanten und Dienstleister

Unter **Lieferanten** pro externem Partner den Namen, die Leistung und die verantwortliche Rolle oder Organisationseinheit pflegen. Ein konkretes Fachthema und ein Geltungsbereich machen die Zuordnung in gefilterten Arbeitsbereichen sichtbar. Kritikalität und Risikobegründung dokumentieren. Eine Vertragsreferenz kann auf die verwaltete Vertragsfassung verweisen.

Eine dokumentierte Prüfung hält Datum, Ergebnis und Begründung fest. Der zuletzt geprüfte Stand steuert die Anzeige und den nächsten Termin. Frühere Prüfungen bleiben in der Historie sichtbar. Lieferanten lassen sich über Mappings mit Risiken, Controls und Maßnahmen verbinden. Die zugeordneten Controls bilden den Einstieg in die vorhandene Nachweisverwaltung.

Ein relevanter externer Dienstleister, ein gewählter Drittparteien-Risikofokus oder andere passende Unternehmensmerkmale priorisieren das Register im Umsetzungsplan. Im Dashboard sind kritische Lieferanten und Prüfbedarf im aktuellen Filter sichtbar. Berichte enthalten die zugehörigen Lieferantendaten und Prüfungen.

Beim Öffnen eines älteren Projektstands legt die Anwendung vor der Migration eine verschlüsselte Sicherung an. Für ein Projekt aus 0.8.0 endet der Sicherungsname auf `.pre09-*.bak`. Für die Rückkehr zur alten Version eine Kopie der Sicherung mit der Dateiendung `.d3grc` in der ursprünglichen App öffnen.

## 16. Dokumente und Richtlinien

Im Bereich **Dokumente** einen Entwurf mit Thema, Geltungsbereich, Kategorie und Verantwortung anlegen. Die Vorschau stellt Überschriften, Listen, Tabellen und weitere Markdown-Elemente dar. Im Entwurf die Platzhalter einer Vorlage durch die tatsächlichen betrieblichen Angaben ersetzen. Anforderungen und Controls lassen sich zum Dokument zuordnen.

Nach Bearbeitung den Entwurf zur Prüfung einreichen. Das Prüfergebnis und die Freigabe werden mit Begründung und lokalem Profil dokumentiert. Die veröffentlichte Fassung bleibt in der Versionsansicht lesbar. Für eine Änderung auf Basis der Freigabe einen neuen Entwurf anlegen. Außer Kraft gesetzte Dokumente bleiben für die Nachverfolgung erhalten.

Unter **Textvorlagen** nach Titel, Kürzel und Inhalt suchen und nach Art filtern. Über **Vorschau** und **Katalogfassung** lassen sich die eigene und ursprüngliche Fassung ansehen. **Als Dokument verwenden** öffnet einen vorausgefüllten Entwurf. Der allgemeine XLSX- und HTML-Bericht enthält den Dokumentenbestand im gewählten Kontext.

Die Nachweisvorschau zeigt UTF-8-Text und PNG direkt im Arbeitsbereich. Für andere Dateiformate steht der separate Dateiexport zur Verfügung.

## 17. Risiken bewerten

Ein gespeichertes Risiko im Register **Risiken** auswählen und im Reiter **Bewertungen** Eintrittswahrscheinlichkeit und Auswirkung jeweils auf einer Skala von 1 bis 5 festhalten. Die Bruttobewertung beschreibt das Risiko vor Berücksichtigung von Controls. Sobald ein begründeter Wert für das Risiko mit berücksichtigten Controls vorliegt, können beide Restrisikowerte ergänzt werden. Datum, Behandlung und Begründung werden der Bewertung zugeordnet.

Das Produkt aus Eintrittswahrscheinlichkeit und Auswirkung ergibt den Punktwert der Zelle. Die Farbstufen lauten 1–4 niedrig, 5–9 mittel, 10–15 hoch und 16–25 kritisch. Die Zahl in der Zelle gibt an, wie viele Risiken im aktuellen Thema, Geltungsbereich und Suchkontext genau diese Kombination haben. Maßstäbe und Annahmen in der Bewertung festhalten, damit Werte im Projekt vergleichbar bleiben.

Die Zellzahlen ändern sich, wenn ein Filter andere Risiken erfasst, die Bewertungssicht zwischen Ausgangs- und Restrisiko wechselt oder eine Bewertung die Zuordnung eines Risikos verändert. Je Risiko zählt der neueste Bewertungsstichtag. Bei gleichem Stichtag gilt der zuletzt gespeicherte Eintrag. Die Restrisikoansicht zeigt Risiken mit dokumentierten Restrisikowerten.

Rechts zählt **Alle** den gefilterten Risikobestand. **Ohne Bewertung** zählt Risiken ohne gespeicherte Bewertung. **Ohne Restrisiko** zählt Risiken mit einer Ausgangsbewertung und fehlenden Restrisikowerten in der maßgeblichen Fassung. Nach Auswahl einer Zelle oder einer dieser Listen zeigt der Trefferzähler die Anzahl der angezeigten Risiken im Verhältnis zum gesamten Filterbestand. **Bewertung öffnen** führt beim ausgewählten Risiko zum Reiter **Bewertungen**. Die Verknüpfungen zu Controls und Maßnahmen stehen unter **Bezüge**.

## Fachmodule und Demo-Arbeitsstrecken

| Arbeitsbereich | Arbeitsablauf in der Demo |
| --- | --- |
| Prozessmanagement | In der interaktiven Prozesslandkarte **Kundenanliegen steuern** auswählen. Unter **Fachdetails** Prozessart, Eingaben, Ergebnisse und geordnete Schritte mit Rollen ansehen. Unter **Bezüge** ergänzen sich Unterprozesse, Risiken, Controls und Lieferanten. [BPM](https://swissgrc.com/bpm-software/) |
| Interne Audits | Jahresplan, Fortschritt der Control-Prüfungen und überfällige Feststellungen öffnen. Im Prüfauftrag die Stichprobe mit einer Abweichung und der dazugehörigen Maßnahme ansehen. [Internal Audit](https://swissgrc.com/internal-audit-software/) |
| Internes Kontrollsystem | Die Schlüsselkontrolle zur Rezertifizierung ansehen. Historie, Prüfart, Ergebnis, Nachweis und Maßnahme öffnen. [IKS](https://swissgrc.com/iks-software/) |
| Operationelle Resilienz | Kritische Dienste, ihre Abhängigkeiten und Szenariotests ansehen. Beobachtete Ausfallzeit und dokumentierte Toleranz werden in den Testkarten gegenübergestellt. [Operational Resilience](https://swissgrc.com/operational-resilience-software/) |
| Business Continuity | Business Impact Analyse, Wiederanlaufplan und Übung zur Bearbeitung von Kundenanfragen öffnen. Die Übersicht zeigt MTPD, RTO, RPO und die gemessenen Ergebnisse. [BCM](https://swissgrc.com/bcm-software/) |
| AI-GRC | Den simulierten Einsatz zur Wissenssuche mit interner Risikoeinstufung, menschlicher Prüfung, Auflagen und Überprüfungsdatum öffnen. [AI GRC](https://swissgrc.com/ai-grc/) |
| Vertragsmanagement | Anstehende und überfällige Vertragsfristen prüfen. Den verknüpften Hostingvertrag mit Laufzeit, Pflichten und zuständiger Stelle öffnen. [CLM](https://contraqto.com/) |

Unter **Risikomatrix** enthält die Demo bewertete Beispiele mit und ohne Restrisiko sowie offene Bewertungen. Im Thema **Datenschutz** kannst du in die Dokumente wechseln und Einträge aus verschiedenen Projektbereichen sehen.
