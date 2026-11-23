# Anforderungsübersetzung

## Metadaten

- Aufgaben-ID: `9180393a-849f-438e-9e7e-a5d44d9e2a21`
- Branch: `task/issue-214-9180393a849f438e9e7ea5d44d9e2a21-installationsdokumentation-pru`
- Verknüpftes Issue: `martin-stromberg/Rezepte` #214 — „Installationsdokumentation prüfen/aktualisieren: Server-Setup mit und ohne Reverse-Proxy"
- Kundenanforderung: Die Installationsdokumentation (`Docs/install.md`) so überarbeiten, dass ein Leser ohne Vorwissen über ein separates Server-Infrastruktur-Projekt Schritt für Schritt einen Server konfigurieren kann, auf dem die Anwendung funktionsfähig läuft — für zwei alternative Deployment-Modi: eigenständig (ohne Reverse-Proxy) und hinter einem Reverse-Proxy (TLS-Terminierung extern, `PathBase`, `X-Forwarded-*`-Header).

## Fachliche Zusammenfassung

Im Rahmen eines separaten Server-Infrastruktur-Projekts wurde die Produktionsumgebung für mehrere Anwendungen überarbeitet (dedizierte, gehärtete Service-Accounts pro App, gehärtete systemd-Units, optionaler Reverse-Proxy mit pfadbasiertem Routing und automatischem TLS). Die Installationsdokumentation dieses Repos soll diesem Stand nachgezogen werden, ohne das Infrastruktur-Projekt vorauszusetzen: `Docs/install.md` ist heute eine funktionierende Schritt-für-Schritt-Anleitung, deckt aber nur den Fall „ohne Reverse-Proxy" ab, nutzt `User=www-data` ohne Hardening-Direktiven und behandelt Bind-Adresse/Port, Firewall und Verifikationsschritte nicht. Die Dokumentation soll zwei klar getrennte, jeweils vollständige Installationspfade beschreiben — „eigenständig" (eigener Port, direkt erreichbar) und „hinter Reverse-Proxy" (externe TLS-Terminierung, `PathBase`-Routing, Weiterleitung der `X-Forwarded-*`-Header) — als alternative Betriebsarten, nicht als gleichzeitigen Betrieb auf einem Server. Dokumentiert werden darf nur, was der Code tatsächlich unterstützt.

## Fachlicher Scope

- **Verarbeitungseinheit:** Die Installationsdokumentation des Repos, konkret `Docs/install.md` als zentrales Artefakt sowie die Verlinkungsstruktur in `Docs/index.md` und `Docs/deployment-guide.md`. Die Anleitung ist in zwei alternative, jeweils in sich vollständige Pfade gegliedert (mit/ohne Reverse-Proxy); gemeinsame Schritte (Runtime, Publish, Transfer) dürfen in einen gemeinsamen Basis-Abschnitt ausgelagert werden, solange jeder Pfad für sich durchgängig nachvollziehbar bleibt.
- **Auslösende Ereignisse:** Ein Administrator mit allgemeinen Linux-Grundkenntnissen (aber ohne Kenntnis des Infrastruktur-Projekts) richtet die Anwendung auf einem leeren Server ein — entweder eigenständig oder hinter einem Reverse-Proxy.
- **Erwartete Wirkung:**
  - Pfad „ohne Reverse-Proxy" führt von „leerer Server" bis „Anwendung läuft": Runtime-Prüfung, `dotnet publish` (framework-abhängig vs. self-contained), Transfer nach `/var/www/rezepte`, dedizierter Service-Account mit Schreibrechten auf das Deployment-/Datenverzeichnis, systemd-Unit, explizite Bind-Adresse/Port (`ASPNETCORE_URLS`), Firewall-/Freigabe-Hinweis sowie Verifikationsschritte (`systemctl status`, `journalctl`, `curl`).
  - Pfad „mit Reverse-Proxy" ergänzt: Proxy-Beispielkonfiguration (TLS-Terminierung, Pfad-Präfix, `X-Forwarded-Proto`/`X-Forwarded-For`), `PathBase`-Konfiguration der App (z. B. `Environment=PathBase=/rezepte` in der systemd-Unit), Loopback-Bind der App sowie den Hinweis, dass der Proxy wegen `KnownProxies` = Loopback auf demselben Host laufen muss.
  - Die Dokumentation ist eigenständig lauffähig: keine Verweise auf andere Repositories, insbesondere keine auf private Repositories.
  - Die Dokumentation beschreibt ausschließlich vom Code tatsächlich unterstütztes Verhalten.
- **Umgang mit fehlenden Objekten:**
  - Dedizierter Service-Account statt `www-data`: in der Dokumentation zu beschreiben — verbindlich oder als Empfehlung je nach Entscheidung zum Härtungsumfang (siehe Offene Fragen).
  - systemd-Hardening-Direktiven (z. B. `ProtectSystem`, `NoNewPrivileges`, `PrivateTmp`): Umfang offen — Entscheidung aus dem Issue-Punkt „Härtungsumfang" abhängig.
  - Bind-Adresse: bisher nirgends konfiguriert oder dokumentiert (`appsettings*.json` enthält kein `Urls`) — in der Anleitung explizit zu setzen; im Proxy-Szenario Loopback (z. B. `ASPNETCORE_URLS=http://127.0.0.1:<port>`).
  - Reverse-Proxy-Beispielkonfiguration: existiert im Repo nicht — neu zu erstellen; Referenzprodukt offen (siehe Offene Fragen).
  - Verifikationsschritte und Firewall-Hinweis: fehlen derzeit — zu ergänzen.
- **Scope-Ausschlüsse:**
  1. `Docs/deployment-guide.md` behandelt Google-Credentials für den Import (anderes Thema) und wird inhaltlich nicht vermischt — nur die bestehende Verlinkung wird geprüft/sauber gehalten.
  2. Kein gleichzeitiger Betrieb beider Modi auf einem Server — die beiden Pfade sind Alternativen.
  3. Keine Änderungen am Anwendungscode (`Rezepte.Web/Program.cs` u. a.) — dokumentiert wird nur das tatsächlich unterstützte Verhalten.
  4. Keine Verweise auf andere bzw. private Repositories in der geschriebenen Dokumentation (die private Pattern-Sammlung des Repo-Besitzers darf nur als internes Ausgangsmaterial dienen und wird in der Doku weder verlinkt noch benannt).
  5. Kein eigenes TLS-Setup der App dokumentieren — die Anwendung bringt keines mit; TLS ist ausschließlich Thema des externen Reverse-Proxys (bzw. als dokumentierte Einschränkung im Standalone-Modus).

## Betroffene Bereiche

- `Docs/install.md` — Hauptarbeit: bestehenden „ohne Proxy"-Pfad prüfen und ergänzen (Service-Account, Hardening je nach Entscheidung, `ASPNETCORE_URLS`, Firewall, Verifikation, Schreibrechte für `rezepte.db`/`logs/`), neuen „mit Reverse-Proxy"-Pfad ergänzen, ggf. gemeinsamen Basis-Abschnitt einführen.
- `Docs/index.md` — Verlinkung ggf. an die neue Struktur anpassen (verlinkt derzeit `install.md` und `deployment-guide.md`).
- `Docs/deployment-guide.md` — nicht inhaltlich vermischen; nur bestehende Verlinkung auf `install.md` prüfen/sauber halten.
- `Rezepte.Web/Program.cs` — nur Referenz für dokumentiertes Verhalten (verifiziert): `PathBase` wird über den Konfigurationsschlüssel `PathBase` gelesen und per `UsePathBase()` aktiviert (Zeilen ~70–79, Default: nicht gesetzt, No-op); `UseForwardedHeaders` wertet `X-Forwarded-Proto` und `X-Forwarded-For` aus — nur von `KnownProxies` = Loopback IPv4 + IPv6 (Zeilen ~87–93). Konsequenz: Same-Host-Bedingung für den Reverse-Proxy; `X-Forwarded-Host` wird nicht ausgewertet.
- Persistenz im `WorkingDirectory` — dokumentationsrelevant: SQLite-Datei `rezepte.db` (Default `Data Source=rezepte.db`), Serilog-Dateisink `./logs/` sowie Backup-/Upload-Verzeichnisse; der Service-Account benötigt Schreibrechte auf das Deployment-Verzeichnis (oder auf ein dediziertes Datenverzeichnis).

## Implementierungsansatz

- **Bestehenden Pfad bewahren und erweitern:** `Docs/install.md` als Basis für den Pfad „ohne Proxy" verwenden; die als funktionierend verifizierten Schritte (Runtime-Prüfung, `dotnet publish` framework-abhängig vs. self-contained, Kopieren nach `/var/www/rezepte`, einfache systemd-Unit, enable/start) übernehmen und um die genannten Lücken ergänzen.
- **Zwei-Pfad-Struktur:** Gemeinsame Schritte (Runtime, Publish, Transfer) in einen Basis-Abschnitt auslagern; danach zwei klar getrennte, jeweils vollständige Pfade, die für sich durchgängig nachvollziehbar sind.
- **Nur unterstütztes Verhalten dokumentieren:** Same-Host-Bedingung aus `KnownProxies` = Loopback explizit nennen; `X-Forwarded-Host` wird nicht ausgewertet; im Proxy-Szenario App an Loopback binden.
- **Eigenständigkeit sicherstellen:** Die Anleitung setzt keine Kenntnis des Infrastruktur-Projekts voraus; allgemeine Linux-Grundkenntnisse dürfen vorausgesetzt werden. Alle benötigten Informationen stehen in diesem Repo.
- **Vollständigkeitsprüfung:** Die Schritte gedanklich (oder real) mit einem frischen Server durchspielen und prüfen, ob sie tatsächlich zu einer funktionierenden Installation führen.

## Konfiguration

Keine Änderung an Konfigurationsmechanismen — nur Dokumentation bestehender Schlüssel:

- `PathBase` (Konfigurationsschlüssel, z. B. per `Environment=PathBase=/rezepte` in der systemd-Unit): aktiviert `UsePathBase()`; Default nicht gesetzt (No-op). Beispielwert offen (siehe Offene Fragen).
- `ASPNETCORE_URLS` / `Urls`: Bind-Adresse/Port; im Proxy-Szenario Loopback-Bind (z. B. `http://127.0.0.1:<port>`); Beispiel-Port offen (siehe Offene Fragen). Bisher in keinem `appsettings*.json` konfiguriert oder dokumentiert.
- `UseForwardedHeaders`: wertet `X-Forwarded-Proto` und `X-Forwarded-For` aus, ausschließlich von `KnownProxies` = Loopback (IPv4 + IPv6); `X-Forwarded-Host` wird nicht ausgewertet.
- Persistenz: `Data Source=rezepte.db` (Default), `./logs/` sowie Backup-/Upload-Verzeichnisse relativ zum `WorkingDirectory` — Schreibrechte des Service-Accounts erforderlich.

## Akzeptanzkriterien

1. `Docs/install.md` ist geprüft und — je nach Entscheidung zum Härtungsumfang — an den dedizierten Service-Account-Ansatz angepasst.
2. Der Pfad „ohne Reverse-Proxy" ist vollständig dokumentiert: Publish, Transfer, Service-Account mit Schreibrechten, systemd-Unit, Bind-Adresse/Port, Firewall-Hinweis, Verifikation der laufenden Anwendung.
3. Der Pfad „mit Reverse-Proxy" ist vollständig dokumentiert: Proxy-Beispielkonfiguration mit TLS-Terminierung und `X-Forwarded-*`-Headern, `PathBase`-Konfiguration, Loopback-Bind der App, Same-Host-Bedingung.
4. Die Dokumentation enthält keine Verweise auf andere bzw. private Repositories.
5. Die Verlinkung ist konsistent (`Docs/index.md`, `deployment-guide.md`).
6. Die Schritte sind gedanklich (oder real) mit einem frischen Server durchgespielt und führen nachweislich zu einer funktionierenden Installation.

## Explizite Nicht-Ziele

- Keine inhaltliche Vermischung mit `Docs/deployment-guide.md` (Google-Credentials/Import bleibt separates Thema).
- Kein gleichzeitiger Betrieb beider Deployment-Modi auf einem Server.
- Keine Codeänderungen an der Anwendung — auch keine Erweiterung des Forwarded-Headers-Verhaltens (z. B. `X-Forwarded-Host`).
- Kein TLS-Setup innerhalb der Anwendung dokumentieren oder einführen.
- Keine Nennung oder Verlinkung externer/privater Repositories (inkl. der privaten Pattern-Collection, die nur internes Ausgangsmaterial ist).
- Kein automatisiertes Setup (Skripte, Ansible o. ä.) — Scope ist die Dokumentation.

## Offene Fragen

1. **Zielplattform:** Für welche Distribution(en)/Version(en) gilt die Anleitung (aktuell implizit „Linux mit systemd", z. B. Debian/Ubuntu)?
2. **Reverse-Proxy-Produkt:** Welches Produkt soll das Referenzbeispiel zeigen (Caddy wegen „automatischem TLS", nginx + certbot, Apache, Traefik)? Alternativ produktneutral mit einem konkreten Beispiel.
3. **Härtungsumfang:** Ist der dedizierte, gehärtete Service-Account verbindlich (inkl. Direktiven wie `ProtectSystem`, `NoNewPrivileges`, `PrivateTmp`) oder eine dokumentierte Empfehlung? Der ursprüngliche Text stellt das zur Prüfung — die Entscheidung steht noch aus.
4. **Proxy-Modus nur mit Pfad-Präfix?** Soll ausschließlich `PathBase` (z. B. `/rezepte`) dokumentiert werden oder auch Root-Pfad (`/` auf Subdomain) hinter dem Proxy? Der Code unterstützt beides.
5. **Exposition im Standalone-Modus:** Öffentlich erreichbar (`0.0.0.0` + Firewall-Freigabe) oder LAN/Loopback? Die App bringt kein eigenes TLS-Setup mit — Standalone ist faktisch HTTP-only; das muss als Einschränkung dokumentiert oder anders entschieden werden.
6. **Konkrete Vorgabewerte:** Beispiel-Port und `PathBase`-Beispielwert festlegen, damit die Schritte copy-paste-fähig sind.
7. **Umfang:** Nur Erstinstallation oder auch Update-Pfad (neues Publish über bestehende Installation, Backup von `rezepte.db`)?
