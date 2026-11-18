# Freigabecheckliste

## Tatsächlich ausgeführt
- SQL-Migration auf einer kompatiblen Testdatenbank.
- Erhalt vorhandener Records, Vorlagenfassungen und Quellenhinweise geprüft.
- Fremdschlüssel und SQLite-Integrität geprüft.
- Themen-/Quellenfilter und Starterdaten geprüft.
- Statische SQL-Anweisungen gegen das migrierte Schema kompiliert (SQLite EXPLAIN).
- Projektdateien und Referenzen als XML geprüft.
- C#-Delimiter lexikalisch geprüft; das ist KEINE Kompilation.
- Build versucht: dotnet nicht installiert, Exit 127. SDK-Download nicht verfügbar.

Details: validation.json und build-attempt.txt.

## Noch zwingend auf einer SDK-Umgebung
1. Restore mit stabilem .NET 10; keine Preview-Auswahl.
2. Release-Build aller Projekte.
3. C#-Regressionstests ausführen.
4. Transitive NuGet-Abhängigkeiten und tatsächliche SQLite-Engine prüfen.
5. Registrierung, Fehlversuchssperre, Anmeldung, Logout und erneute Anmeldung testen.
6. Neues Projekt anlegen, speichern, schließen, erneut öffnen.
7. Eine Kopie eines echten 0.5-Projekts migrieren; eigene Texte und Evidence vergleichen.
8. Hauptdatei, pre06-Backup und Recovery im Fehlerfall prüfen.
9. Themenwechsel, Cross-Mappings, Quellenbezüge und Pflichtbegründungen testen.
10. Dashboardwerte gegen Detailregister und Excelzahlen abgleichen.
11. XLSX in Excel und LibreOffice öffnen; keine Reparaturmeldung, Filter und Datumszellen prüfen.
12. Formelähnliche Texte müssen Text bleiben.
13. Druckansicht und PDF-Ausgabe prüfen.
14. Audit ohne Scope/Test/Evidence muss Abschluss verweigern.
15. Audit mit offenen Findings darf abgeschlossen werden, Findings bleiben offen.
16. Historische Evidence zum Prüfzeitpunkt beurteilen.
17. Findingabschluss benötigt gültigen Nachweis für alle betroffenen Controls.
18. Quellenfehler darf LastSuccess nicht verändern und eigene Texte nicht überschreiben.
19. Zweite Instanz, Speicherfehler, abruptes Ende und Recovery testen.
20. Tastatur, DPI, Screenreader und lange deutsche Texte prüfen.
21. Windows, macOS und Linux nativ testen.
22. Signierte Installer separat erstellen und auf sauberen Systemen prüfen.

Bis dahin keine Produktionsfreigabe und keine echten Kundendaten.
