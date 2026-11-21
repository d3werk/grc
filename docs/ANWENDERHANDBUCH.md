# Anwenderhandbuch 0.10.0

In Version 0.6.1 bieten zentrale Felder und Aktionen kurze Bedienhinweise. Den Mauszeiger auf dem Element stehen lassen oder das Element mit der Tastatur fokussieren und F1 drücken. Escape schließt einen geöffneten Hinweis.

## 1. Registrierung und Anmeldung
Profilname und Profilpasswort eingeben. „Profil erstellen“ legt ein Profil im Benutzerverzeichnis dieses Rechners an und startet nach der Anmeldung die Projekteinrichtung. Beim Abbrechen des Assistenten bleibt das Profil angemeldet. Für bestehende Profile steht die normale Anmeldung zur Verfügung. Nach fünf fehlgeschlagenen Versuchen wird das Profil vorübergehend gesperrt.

Das lokale Profil kennzeichnet Aktivitäten, ist aber keine verifizierte Organisationsidentität. Mehrere lokale Profile ersetzen keine Teamrechte. Bei Profilverlust kann ein neues Profil eingerichtet werden; der Zugriff auf Projekte erfordert weiterhin das Projektpasswort.

## 2. Projekt öffnen oder anlegen
**Neues Projekt.** Im Assistenten Unternehmensgröße, Branche, Standorte und Organisationseinheiten angeben. Merkmale und Themen auswählen und den daraus abgeleiteten Plan prüfen. Ein separates Projektpasswort vergeben und einen neuen Speicherort wählen. Die Projektdatei öffnet sich direkt in der Übersicht.

**Unternehmensprofil.** Größe, Branche, Merkmale und Risikoschwerpunkt bestimmen, welche Aufgaben als wesentlich empfohlen werden. Für optionale Aufgaben steht der vollständige Arbeitsbereich weiterhin zur Verfügung. Über „Unternehmen konfigurieren“ lässt sich das Profil für bestehende Projekte ergänzen oder ändern. Fachliche Anwendbarkeit wird je Anforderung bewertet.
**Öffnen.** Verschlüsselte `.d3grc`-Datei auswählen und Projektpasswort eingeben. Ist ein Wiederherstellungsstand vorhanden, im Auswahlfenster den gewünschten Stand wählen oder den Vorgang abbrechen.

Die mitgelieferten `.db`-Dateien sind öffentliche Starter-/Referenzdatenbanken ohne Kundendaten. Sie sind keine verschlüsselten Projektdateien und werden nicht direkt über Öffnen geladen.

## 3. Thema, Scope und Suche
Das Thema oben gilt für die aktuelle Ansicht. Alle Themen ist eine Gesamtansicht. Zum Anlegen/Speichern eines Datensatzes ein konkretes Thema auswählen. Die Fachgebiete sind Datenschutz, Informationssicherheit, NIS2, KI-Governance, Cloud Security, BCM, Open Source und DORA.

Zuordnung prüfen enthält migrierte Objekte ohne eindeutig ableitbaren Themenbezug. Nicht automatisch alle internen Controls allen Themen zuordnen. Einen passenden Fachkontext auswählen und das Objekt dort über einen begründeten Themenbezug ergänzen.

Scope wird derzeit als exakter Textfilter behandelt, nicht als Organisationsbaum. Suche durchsucht Titel, Code, Beschreibung und Owner der Arbeitsobjekte. Filter anwenden aktualisiert den Kontext. Nicht jede Wissensansicht verwendet diese Objektsuche; das Glossar besitzt eine passende Begriffssuche. Quellen und Vorlagen werden primär nach Thema begrenzt.

## 4. Datensatz bearbeiten
Eintrag links auswählen. Rechts stehen Tabs bereit:
- Details: Titel, Status, Verantwortung, Termin, Scope, Beschreibung und Änderungsbegründung.
- Quellen: primäre Quelle, konkrete Fundstelle, passende Vorlage und zusätzliche Quellen-/Themenbezüge.
- Bezüge: Mappings und fachliche Arbeitsabläufe.
- Evidence: tatsächliche Nachweise eines Controls.
- Historie: protokollierte Änderungen des Objekts.

Vorlage übernehmen kopiert den Text ins Formular. Eigene Ergänzungen sind erwünscht. Erst Änderungen speichern legt den konkreten Datensatz an beziehungsweise aktualisiert ihn und schreibt die verschlüsselte Hauptdatei. Ein Vorlagentext ist kein Nachweis einer Umsetzung.

## 5. Speichern
Die globale Schaltfläche Speichern und Strg/Cmd+S speichern ein bearbeitetes Formular über dessen Speicherfunktion. Ohne Formularänderungen wird die Hauptdatei gespeichert.

Speichern unter übernimmt ein offenes Formular zuerst und erzeugt anschließend eine neue verschlüsselte Projektkopie mit demselben Passwort. Die alte Datei bleibt erhalten. Vorhandene Zieldateien werden nicht überschrieben.

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

Ansehen öffnet eine passende Detailansicht. Balken zeigen Anzahlen, keine Prozentwerte. Leere Daten werden nicht als positive Konformität dargestellt. Es gibt keine historische Zeitreihe und keine 5×5-Risikomatrix in dieser Lieferung.

## 12. Excel und Druck
Excel exportieren erzeugt eine echte `.xlsx`-Datei ohne installierte Excel-Anwendung. Exportiert werden Thema, Scope und Objektsuche für alle Objekttypen im Kontext. Lokale Statusfilter einer einzelnen Liste werden nicht übernommen; der Exportdialog weist darauf hin.

Arbeitsblätter enthalten Kontext, Kennzahlen, Anforderungen, Controls, Risiken, Audits, Findings, Maßnahmen, KI-/OSS-Register, Mappings, Evidence-Metadaten, Prüfungen, Quellen, Statusverteilungen, Glossar und Definitionen. Mappings können auf Objekte außerhalb des Filters verweisen; diese sind über IDs erkennbar. Evidence-Dateiinhalte werden nicht eingebettet.

Texte werden als Textzellen geschrieben, nicht als Formeln. Zahlen und geeignete Datumsfelder erhalten passende Zelltypen. Überschriften sind fixiert, Filter vorhanden. Diagramme sind im Dashboard; eingebettete Excel-Diagramme sind noch nicht enthalten.

Drucken / PDF erzeugt eine lokale HTML-Datei und öffnet den Standardbrowser. Dort Drucken oder als PDF speichern. Kein stiller Druck und kein eingebetteter nativer Druckdialog.

## 13. Logout und Sperren
Beide Aktionen beenden in diesem Einzelplatzstand die Sitzung vollständig: Formularänderungen behandeln, Projektzustand sichern, Projekt schließen, Schlüsselkontext und lokale Dateisperre freigeben, zur Anmeldung zurückkehren. Zum Weiterarbeiten erneut anmelden und Projekt entsperren. Es gibt keinen versteckten entsperrten Hintergrund-Workspace.

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

Die Risikomatrix trennt Brutto- und Restrisiken. Eine ausgewählte Zelle zeigt die zugehörigen Risiken im aktuellen Thema, Geltungsbereich und Suchkontext. Risiken ohne Bewertung und Risiken ohne Restrisikowerte werden sichtbar ausgewiesen. Für ein Risiko lassen sich Controls und Maßnahmen über typisierte Mappings dokumentieren.

Das Produkt aus Eintrittswahrscheinlichkeit und Auswirkung ergibt die Bewertungszahl. Die Einteilung lautet 1–4 niedrig, 5–9 mittel, 10–15 hoch und 16–25 kritisch. Maßstäbe und Annahmen bei der Bewertung so festhalten, dass die Werte im Projekt vergleichbar bleiben.
