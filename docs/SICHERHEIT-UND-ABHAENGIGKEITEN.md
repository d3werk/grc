# Sicherheit und Abhängigkeiten

## Abhängigkeiten dieser Lieferung
- .NET-Zielframework net10.0; global.json verhindert Preview-SDK-Auswahl.
- Avalonia-Paketfamilie 11.3.23, kein ungeprüfter Sprung auf Hauptversion 12.
- Tmds.DBus.Protocol 0.21.3 als expliziter Backport-Pin.
- Microsoft.EntityFrameworkCore.Sqlite.Core 10.0.14.
- SQLitePCLRaw.bundle_e_sqlite3 3.0.5 für die explizite native SQLite-Kette.
- Konscious.Security.Cryptography.Argon2 1.3.1.

Die frühere Aussage, es gebe überhaupt kein neueres natives SQLite-NuGet-Paket, war zu pauschal. Die Advisory nennt für den damaligen Paketstand keinen Patch, während neuere Paketketten inzwischen verfügbar sind. Deshalb wird hier die Core-Variante des EF-Providers mit expliziter nativer Kette verwendet. Vor Nutzung muss der tatsächlich aufgelöste transitive Paketgraph erneut geprüft werden.

Offizielle Referenzen:
- [EF SQLite Core 10.0.14](https://www.nuget.org/packages/Microsoft.EntityFrameworkCore.Sqlite.Core/10.0.14)
- [SQLite-Bundle 3.0.5](https://www.nuget.org/packages/SQLitePCLRaw.bundle_e_sqlite3/3.0.5)
- [Avalonia-Releases](https://github.com/AvaloniaUI/Avalonia/releases)
- [D-Bus-Advisory](https://github.com/advisories/GHSA-xrw6-gwf8-vvr9)
- [SQLite-Advisory](https://github.com/advisories/GHSA-2m69-gcr7-jv3q)

NuGet-Audit ist eingeschaltet. NU1903 und NU1904 werden als Fehler behandelt, nicht ausgeblendet. Das ist ein Freigabeschutz. Ein zukünftiger neuer Befund kann den Build daher bewusst stoppen. Kein sauberer Restore oder Vulnerability-Audit wurde hier behauptet.

Beim Öffnen/Migrieren prüft der Code die tatsächliche SQLite-Engineversion auf mindestens 3.50.2. Diese Mindestprüfung adressiert den besprochenen Befund, ist aber kein Beweis für Freiheit von allen späteren Schwachstellen.

## Lokale Identität
Profilpasswörter werden mit Argon2id, zufälligem Salt und konstantzeitlichem Hashvergleich behandelt. Parameter: 64 MiB, 3 Iterationen, 2 Lanes, 32 Byte Ergebnis. Profilverifier sind getrennt von Projektdateien gespeichert. Kein Klartextpasswort wird absichtlich persistiert.

Die Anwendung ist ein Einzelplatzprototyp. Profile sind keine manipulationssichere Berechtigungsgrenze gegen einen lokalen Benutzer mit Dateizugriff. Kein SSO, keine verifizierte E-Mail, keine zentrale Kontosperre, kein Rollenversprechen.

## Projektverschlüsselung
D3GRC005 bleibt erhalten: 8 Byte Magic, 16 Byte Salt, 12 Byte Nonce, verschlüsselte SQLite-Datei und 16 Byte GCM-Tag. AES-256-GCM authentifiziert den Header. Jede Speicherung verwendet eine neue Nonce. Schlüsselableitung entspricht dem bekannten 0.5-Format.

Aktive SQLite-Arbeitskopie ist nicht SQLCipher-verschlüsselt. Privates Benutzerverzeichnis, Unix-Berechtigungen und normale Windows-Benutzerprofilrechte sind nur ergänzende Maßnahmen. Datenträgerverschlüsselung erforderlich. Keine Garantie gegen Administratoren, Malware, RAM-/Crash-Dumps oder vollständige lokale Manipulation.

## Quellenabruf
Nur freigegebene HTTPS-Hosts, maximal drei Weiterleitungen, 20 Sekunden und 4 MiB entpackter Inhalt. Kein Projektinhalt im Request. Keine deaktivierte TLS-Prüfung. Technischer Hashvergleich kann durch Layout-/Consent-Änderungen auslösen und ist keine juristische Inhaltsanalyse.

## Export
XLSX/HTML/Evidence sind Klartext. Der XLSX-Writer schreibt benutzereingegebene Texte als Inline-Strings, nicht als Formeln. Keine Makros und keine externen Workbookverknüpfungen werden erzeugt. Das ist kein verschlüsselter Excel-Container. Der Writer ist bewusst minimal; Excel-/LibreOffice-Interoperabilität muss nativ geprüft werden.

## Journal
Transaktionale Hashkette mit lokalem Profilnamen. Keine externe Verankerung oder Signatur. Nicht als revisionssicher oder als Nachweis einer verifizierten Person ausgeben.
