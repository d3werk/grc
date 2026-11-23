# D3 GRC Plattform 0.12.0

Der Web-Arbeitsbereich verbindet Organisation, Informationswerte, IT-Dienste, Risiken, Controls, Maßnahmen und Nachweise. Das Verzeichnis der Verarbeitungstätigkeiten (VVT) verwendet dieselben Dienste und Controls für den Datenschutz. Der Stand ist für Entwicklung und lokale Erprobung vorgesehen. D3 GRC Desktop arbeitet mit verschlüsselten Projektdateien. Die Plattform verwendet einen eigenen PostgreSQL-Datenbestand. Die Übernahme verschlüsselter Desktop-Projekte ist im Backlog beschrieben. [Plattformarchitektur](../docs/PLATTFORM-ARCHITEKTUR.md) · [Ausbau und Abnahmekriterien](../docs/PLATTFORM-BACKLOG.md)

## Lokal mit Docker starten

Voraussetzungen sind Docker mit Compose V2 und OpenSSL. Im Verzeichnis `platform` einmal eine private `.env` erzeugen. Die drei Werte können auch aus einer lokalen Kennwortverwaltung in die Felder der mitgelieferten Vorlage übernommen werden. [Konfigurationsvorlage](.env.example)

```bash
cd platform
if [ ! -e .env ]; then
  (umask 077
   printf 'GRC_DB_ADMIN_PASSWORD=%s\n' "$(openssl rand -hex 32)"
   printf 'GRC_DB_RUNTIME_PASSWORD=%s\n' "$(openssl rand -hex 32)"
   printf 'GRC_BOOTSTRAP_TOKEN=%s\n' "$(openssl rand -hex 32)"
   printf 'GRC_HTTP_PORT=8080\n') > .env
fi
docker compose up --build --wait
```

Öffne [http://127.0.0.1:8080](http://127.0.0.1:8080) und wähle **Neuen Mandanten einrichten**. Vergib einen Namen, ein eindeutiges Kürzel und ein eigenes Administrationspasswort mit mindestens 12 Zeichen. Als Einrichtungsschlüssel dient der Wert von `GRC_BOOTSTRAP_TOKEN` in `.env`. Nach der Einrichtung öffnet sich der Arbeitsbereich mit dem Konto des neuen Mandanten. Ein leeres `GRC_BOOTSTRAP_TOKEN` deaktiviert die Einrichtung nach Neuerstellung des Plattformdienstes mit `docker compose up -d platform`.

Der Anwendungscontainer liefert das Browserportal und die API am selben lokalen Port aus. Der Migrationsdienst beendet sich nach erfolgreicher Migration. PostgreSQL ist im Compose-Netz erreichbar und speichert die Daten im Volume `grc-db`. Beim Stoppen mit `docker compose stop` bleibt das Volume erhalten. [Compose-Dienste](compose.yaml)

| Prüfpunkt | Adresse oder Befehl |
| --- | --- |
| Dienstestatus | `docker compose ps -a` |
| Migrations- oder Anwendungsstart | `docker compose logs migrate platform` |
| API bereit | `curl -fsS http://127.0.0.1:8080/readyz` |
| API im Browser | `http://127.0.0.1:8080/docs` |
| API-Vertrag im Repository | [OpenAPI](openapi.json) |

## Die erste Arbeitsstrecke

1. Erfasse eine Organisationseinheit, beispielsweise **IT und Personal**, und verwende für die Testeinträge denselben Geltungsbereich. Organisationseinheit und Geltungsbereich sind eigene Felder. Ein Beratermandat prüft den genauen Text des Geltungsbereichs.
2. Erfasse **Microsoft 365** unter **IT-Dienste**. Verknüpfe ein Risiko **Unberechtigter Zugriff auf Personaldaten** mit diesem IT-Dienst. Bewerte Eintrittswahrscheinlichkeit und Auswirkung auf der 5×5-Skala. Bei einer Neubewertung bleibt die frühere Bewertung erhalten.
3. Lege einen Control **Mehrfaktor-Authentifizierung** an und verknüpfe ihn mit dem Risiko. Erfasse die konkrete Maßnahme **MFA-Richtlinie für alle Personal-Konten aktivieren** mit Zieltermin und Erfolgskriterium. Ordne die Maßnahme dem Control zu.
4. Hinterlege für den Control einen synthetischen Konfigurationsauszug oder eine HTTPS-Quellenreferenz. Verknüpfe denselben Nachweis mit der zugehörigen Maßnahme. Ein Nachweis lässt sich für mehrere Controls und Maßnahmen im selben Geltungsbereich verwenden. Dateien können als PNG oder UTF-8-Text (`.txt`, `.log`) bis 4 MiB hinterlegt werden. Die Weboberfläche bietet Vorschau und Download für die unterstützten Inhalte.
5. Prüfe in der **Übersicht** und der **Risikomatrix** den Stand. Die Kennzahlen ergeben sich aus dem sichtbaren Register. Das Dashboard weist fehlende und veraltete Risikobewertungen separat aus.
6. Lege im **Verarbeitungsverzeichnis** eine Tätigkeit **Personalverwaltung** an. Ergänze Zweck, Rechtsgrundlagen und Kategorien und verknüpfe den vorhandenen IT-Dienst sowie den bereits angelegten Control als TOM. Unter **Berichte** lassen sich die Verarbeitungstätigkeit und die für dich sichtbaren Bezüge anzeigen und als CSV exportieren.

Kennzahlen beschreiben den erfassten Datenstand. Eine fachliche Prüfung der Wirksamkeit, eine Rechtsbewertung und eine bestätigte Konformität benötigen die dafür zuständige Stelle. [Fachmodule und Regeln](../docs/PLATTFORM-ARCHITEKTUR.md#2-fachmodule-ag)

## Zugriffe verwalten

Die Administration kann Mitglieder und befristete Beratungsmandate im eigenen Mandanten anlegen. Lesende Mitglieder sehen ihre zulässigen Register, Prüfstände und Berichte. Bearbeitende Mitglieder pflegen auch Fachobjekte, Bewertungen und Nachweise. Berater erhalten pro Mandat ausgewählte Aktionen in genau einem Geltungsbereich. Ablauf und Widerruf werden bei jeder Anfrage geprüft. Mehrere Mandate können verschiedene Bereiche freigeben. [Berechtigungsmatrix](../docs/PLATTFORM-ARCHITEKTUR.md#rbac-und-abac-im-plattformkern)

Die Mandantenauswahl zeigt die Arbeitsbereiche, die in diesem Browser-Tab angemeldet wurden. Für einen weiteren Arbeitsbereich sind dessen Mandantenkürzel und gültige Zugangsdaten nötig. Bei einem Wechsel prüft der Server die ausgewählte Sitzung erneut. Der Browser baut den Arbeitsbereich mit seinen eigenen Berechtigungen und Filtern neu auf. Beim Abmelden leert der Browser die Auswahl und fordert den Server auf, die verbundenen Sitzungen zu beenden.

Die Sitzung läuft standardmäßig acht Stunden. Beim vollständigen Neuladen des Portals ist eine neue Anmeldung erforderlich.

Unter **Basisregister → Verantwortlichkeiten** lässt sich dieselbe Person als IT-Leitung einer Einheit, als Asset Owner eines Dienstes und als Risk Owner eines Risikos zuordnen. Beendete Zuordnungen lassen sich in der Historie einblenden. Zugriffsrechte folgen aus den Mitgliedschaften und Mandaten. Unter **Administration → Audit-Protokoll** sehen berechtigte Nutzer die protokollierten Änderungen und Exporte mit handelndem Mitglied und Zeitpunkt in UTC. [API-Vertrag](openapi.json)

## CSV-Import und Schnittstellen

Die Beschreibungs- und längeren Fachfelder im Register haben eine Formatierungsleiste. Die API speichert Absätze, Überschriften, Listen, Fett- und Kursivformatierung strukturiert. Suche, CSV und bestehende API-Clients verwenden weiterhin die Klartextfelder. `rich_text` ergänzt die Darstellung der Formatierungen. Wer über die API nur ein Klartextfeld ändert, erhält die Formatierung der übrigen unveränderten Felder. [OpenAPI](openapi.json)

`POST /api/v1/imports/catalog` erwartet eine Objektart `kind` und den CSV-Inhalt als `content`. `title` ist Pflicht. Optional sind `code`, `description`, `owner`, `scope`, `status`, `org_unit_id` und `attributes` als JSON innerhalb einer CSV-Zelle möglich. Der Standardtrenner ist `;`. Pro Aufruf sind bis zu 100 Datensätze erlaubt.

Der Standard `dry_run=true` prüft Format, Felder und erlaubte Bereiche und liefert die Zeilenzahl. Beim Schreiben mit `dry_run=false` prüft die API zusätzlich die fachlichen Referenzen. Der Header `Idempotency-Key` kennzeichnet den Import. Eine Wiederholung der gleichen Eingabe durch denselben berechtigten Benutzer liefert dieselben IDs. Ein fachlicher Fehler verhindert die Übernahme des gesamten Stapels.

`GET /api/v1/reports/management` und `GET /api/v1/reports/privacy` liefern JSON oder CSV mit `format=csv`. `scope` grenzt die jeweilige Sicht bei Bedarf zusätzlich ein. Die API ermittelt den Mandanten über den Sitzungstoken. [OpenAPI](openapi.json)

## Entwicklungs- und Testlauf mit PostgreSQL

Für lokale Quellcodearbeit werden Python 3.12, `uv`, Node.js 24 und Docker verwendet. Die Integrationstests erzeugen synthetische Testmandanten. Verwende dafür eine separate Datenbank. Das folgende Beispiel startet einen temporären PostgreSQL-Container am lokalen Port 55432. Seine Daten existieren bis zum Stoppen des Containers.

```bash
cd platform/backend
export GRC_DB_ADMIN_PASSWORD="$(openssl rand -hex 32)"
export GRC_DB_RUNTIME_PASSWORD="$(openssl rand -hex 32)"
export GRC_DB_HOST=127.0.0.1 GRC_DB_PORT=55432
POSTGRES_PASSWORD="$GRC_DB_ADMIN_PASSWORD" docker run --rm --detach \
  --name d3-grc-test-db --publish 127.0.0.1:55432:5432 \
  --tmpfs /var/lib/postgresql/data \
  --env POSTGRES_USER=grc_owner --env POSTGRES_DB=grc --env POSTGRES_PASSWORD \
  postgres:17-bookworm
```

Wenn `docker exec d3-grc-test-db pg_isready -h 127.0.0.1 -U grc_owner -d grc` eine Verbindung akzeptiert, folgen Migration und Tests im selben Terminal. Ohne `GRC_TEST_DATABASE_URL` werden die PostgreSQL-Integrationstests übersprungen.

```bash
uv sync --locked
uv run --locked python -m app.deployment migrate
export GRC_TEST_DATABASE_URL="postgresql://grc_api:${GRC_DB_RUNTIME_PASSWORD}@127.0.0.1:55432/grc"
export GRC_TEST_ADMIN_DATABASE_URL="postgresql://grc_owner:${GRC_DB_ADMIN_PASSWORD}@127.0.0.1:55432/grc"
uv run --locked pytest
```

Für die Entwicklung des Portals kann die lokale API auf Port 8000 weiterlaufen. Der Vite-Server leitet `/api` standardmäßig an diese Adresse weiter. `GRC_BOOTSTRAP_TOKEN` nur setzen, wenn ein zusätzlicher lokaler Testmandant eingerichtet werden soll.

```bash
export DATABASE_URL="$GRC_TEST_DATABASE_URL"
export GRC_BOOTSTRAP_TOKEN="$(openssl rand -hex 32)"
uv run --locked python -m app.deployment serve
```

In einem weiteren Terminal aus dem Repository-Stamm starten.

```bash
cd platform/frontend
npm ci
npm run build
npm run dev -- --host 127.0.0.1
```

Nach den Tests beendet `docker stop d3-grc-test-db` den dafür angelegten temporären Container.

## Browserprüfung

Der Playwright-Test arbeitet gegen eine laufende lokale Testinstanz mit aktiviertem Einrichtungsschlüssel. Er legt einen synthetischen Mandanten an und prüft Arbeitsabläufe am Desktop und bei schmaler Browserbreite. Starte ihn aus dem Repository-Stamm und passe die Adresse an den lokalen Testport an.

```bash
cd platform/frontend
npm ci
npx playwright install chromium
read -r -s -p 'Einrichtungsschlüssel der Testinstanz eingeben ' GRC_BOOTSTRAP_TOKEN
export GRC_BOOTSTRAP_TOKEN
GRC_BROWSER_URL=http://127.0.0.1:8080 npm run test:e2e
unset GRC_BOOTSTRAP_TOKEN
```

Mit `GRC_BROWSER_EXECUTABLE` kann der Test einen vorhandenen Chromium-Browser verwenden. `GRC_BROWSER_ARTIFACTS_DIR` benennt bei Bedarf den Ordner für Screenshots.

## Betriebsreife und Ausbau

Die lokale Compose-Umgebung bietet PostgreSQL-Zugriffsgrenzen, HTTP am Loopback-Port, Sitzungsanmeldung und nachvollziehbare Fachänderungen. Nachweise liegen in PostgreSQL. Produktionsfreigabe, OIDC/SSO mit MFA, Objektspeicher mit Malwareprüfung, feinere Zugriffsregeln, Aufbewahrung, Wiederherstellungsziele und vollständige Fachworkflows sind in Architektur und Abnahme einzeln beschrieben. [Prüfbericht 0.12.0](../docs/PRUEFBERICHT-0.12.0.md) · [Produktionsanforderungen und Abnahmekriterien](../docs/PLATTFORM-BACKLOG.md)
