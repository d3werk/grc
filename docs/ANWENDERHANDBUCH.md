# Anwenderhandbuch 0.6

In Version 0.6.1 bieten zentrale Felder und Aktionen kurze Bedienhinweise. Den Mauszeiger auf dem Element stehen lassen oder das Element mit der Tastatur fokussieren und F1 drücken. Escape schließt einen geöffneten Hinweis.

## 1. Registrierung und Anmeldung
Profilname und Profilpasswort eingeben. Registrieren / lokales Profil einrichten legt ausschließlich ein Profil im Benutzerverzeichnis dieses Rechners an. Es wird keine E-Mail verschickt und kein Onlinekonto erstellt. Danach mit demselben Profil anmelden. Nach fünf fehlgeschlagenen Versuchen wird das Profil vorübergehend gesperrt.

Das lokale Profil kennzeichnet Aktivitäten, ist aber keine verifizierte Organisationsidentität. Mehrere lokale Profile ersetzen keine Teamrechte. Bei Profilverlust kann ein neues Profil eingerichtet werden; der Zugriff auf Projekte erfordert weiterhin das Projektpasswort.

## 2. Projekt öffnen oder anlegen
Neues Projekt: Name, separates Projektpasswort und neuen Speicherort wählen. Vorhandene Dateien werden bei der Neuanlage nicht still überschrieben.
Öffnen: verschlüsselte `.d3grc`-Datei auswählen und Projektpasswort eingeben. Bei vorhandener Recovery wird Wiederherstellung angeboten. Der Hinweis im Dialog erklärt, dass Abbrechen dort die Hauptdatei auswählt; das folgende Passwortfenster kann weiterhin abgebrochen werden.

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

Bei Navigations-/Auswahlwechsel warnt die Anwendung vor ungespeicherten Formularänderungen. Abbrechen im Warnfenster erhält die Bearbeitung. Recovery ist ein Wiederherstellungsstand, kein Ersatz für eine erfolgreiche Hauptdateispeicherung. Bei Speicherfehler nicht schließen, sondern Ursache beheben und erneut speichern.

## 6. Quellen passend auswählen
Primäre Quellen und Vorlagen sind auf das Thema begrenzt. Beispiel Datenschutz: DSGVO, BDSG und interner Arbeitsstandard. BCM: BSI-Standard 200-4 und interner Arbeitsstandard. Ein konkretes Control kann mehrere Themen unterstützen.

Weitere Quelle hinzufügen ist eine bewusste Cross-Zuordnung mit Fundstelle und Begründung. Ein bestehendes Objekt mit ausdrücklich ergänztem Themenbezug bleibt in diesem Thema bearbeitbar. Zusätzliche Quellen sind am Objekt sichtbar; die globale Quellenliste bleibt der standardmäßigen Themenpolitik zugeordnet.

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
