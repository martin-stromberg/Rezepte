# Plan-Check (2. Runde): Installationsdokumentation mit/ohne Reverse-Proxy (Issue #214)

**Status: Plan vollständig** — alle drei Pflichtbefunde aus `plan-check.1.md`
sind im nachgeschärften Plan korrekt behoben, der optionale Verbesserungshinweis
(Nicht-Ziele-Abschnitt) wurde ebenfalls umgesetzt. Geprüft gegen
`requirement.md`, `inventory.md` + Detaildokumente und externe Quellen
(packages.ubuntu.com für die .NET-10-Paketquelle). Es verbleiben nur zwei
kosmetische Anmerkungen ohne Blocker-Charakter.

## Nachprüfung der Pflichtbefunde aus `plan-check.1.md`

### Befund 1 — Caddy-Matcher für `/rezepte` und `/rezepte/*` — **behoben**

- Entscheidung 4 (plan.md Z. 26) legt den Matcher
  `@rezepte path /rezepte /rezepte/*` + `handle @rezepte` verbindlich fest und
  begründet beide Fallen korrekt (`/rezepte/*` matcht den exakten Pfad nicht;
  `/rezepte*` würde `/rezeptefoo` matchen und wird nicht verwendet).
- Die konkrete Caddyfile-Struktur B.4 (Z. 151–167) setzt das exakt so um:
  `handle @rezepte { reverse_proxy 127.0.0.1:5000 }` mit `respond 404`-Fallback.
  `handle` (nicht `handle_path`) → **kein Pfad-Stripping**, die App strippt
  selbst via `UsePathBase` (codekonform, `Program.cs:76–80`).
- Szenario 2 (Z. 273–274, 283–286) verifiziert nun beide Varianten:
  `curl -I https://rezepte.example.com/rezepte` **und** `.../rezepte/` — der
  frühere Selbstwiderspruch (Verifikations-URL fällt in den 404-Fallback) ist
  aufgelöst.
- Der Fresh-Server-Dry-Run benennt den Matcher explizit als Prüfpunkt
  (Z. 329–330).

### Befund 2 — funktionierender Transfer-Mechanismus — **behoben**

- Entscheidung 10 (Z. 32) legt fest: `scp`/`rsync` **ins Home-Verzeichnis**
  (`~/publish`), dann `sudo mkdir -p /var/www/rezepte` +
  `sudo cp -r ~/publish/. /var/www/rezepte/` — umgeht das Schreibrechte-Problem
  am root-eigenen Ziel korrekt. Alternativen (`rsync --rsync-path="sudo rsync"`,
  vorübergehendes `chown` des leeren Ziels) sind als Satz benannt.
- Konsistent umgesetzt in Schritt 3 der Struktur (Z. 104–110), Szenario 1
  Schritt 3 (Z. 246–248) und als Dry-Run-Prüfpunkt (Z. 327–328).
- Detail stimmt: `cp -r ~/publish/.` übernimmt auch Dotfiles; `rsync`-Alternative
  ist nur Nebenbemerkung, nicht Pflichtbestandteil der Schritte.

### Befund 3 — .NET-10-Quelle für Ubuntu 24.04 — **behoben und extern verifiziert**

- Entscheidung 9 (Z. 31) benennt die Quelle konkret: `aspnetcore-runtime-10.0`
  aus dem eingebauten Ubuntu-Feed (`noble-updates`/`noble-security`,
  Quellpaket `dotnet10`), installiert per `sudo apt update && sudo apt install
  aspnetcore-runtime-10.0`.
- **Extern verifiziert:** `aspnetcore-runtime-10.0`
  (10.0.14-0ubuntu1~24.04.1) und das Metapaket `dotnet10` sind in
  `noble-updates` publiziert (packages.ubuntu.com/Launchpad). Die Aussage, dass
  `packages.microsoft.com` für Ubuntu 24.04 keine .NET-Pakete mehr liefert,
  entspricht der Microsoft-Dokumentation (Distro-Feed ist Referenz).
- Der Befehl ist damit auf einem frischen Ubuntu-24.04-Server ohne
  zusätzliche Repo-Einrichtung direkt ausführbar — exakt das, was Befund 3
  gefordert hat. Debian-12-Hinweis (Microsoft-Feed nötig) ist korrekt als
  Ein-Zeilen-Abgrenzung geplant.

### Optionaler Hinweis — **umgesetzt**

Der „Nicht abgedeckt"-Abschnitt (Z. 188–193) ist jetzt in der Zielstruktur
enthalten: Alternativen-Charakter beider Pfade, kein App-eigenes TLS, kein
automatisierter Rollout, andere Proxys nur als Regelliste.

## Wiedergeprüfte Restkriterien (unverändert korrekt)

- **Vollständigkeit Pfad A:** Runtime-Prüfung mit beiden Shared Frameworks +
  `System.Runtime.Serialization.Primitives.dll`-Begründung, beide
  Publish-Varianten, Transfer, Account `rezepte` + `chown`,
  `ASPNETCORE_URLS=http://0.0.0.0:5000`, `ufw allow 5000/tcp`, Verifikation
  (`systemctl status`, `journalctl -u`, `logs/app-*.log`, `curl -I`, Browser),
  HTTP-only-Einschränkung — alle AK-2-Elemente abgedeckt.
- **Vollständigkeit Pfad B:** Caddy mit automatischem TLS, kein Stripping,
  `X-Forwarded-Proto`/`-For` (Caddy-Default), `PathBase=/rezepte`,
  Loopback-Bind `127.0.0.1:5000`, Same-Host-Bedingung (`KnownProxies` =
  Loopback), Subdomain-Alternative, Fremd-Proxy-Regelliste inkl. Host-Durchreichen
  und WebSocket — alle AK-3-Elemente abgedeckt.
- **`Jwt:Key` als Pflichtschritt** (Entscheidung 8, Struktur-Schritt 5):
  `Environment=Jwt__Key=` (doppelter Unterstrich korrekt),
  `openssl rand -base64 48` (≥ 32 Zeichen), `EnvironmentFile=`-Alternative,
  `chmod 640 root:rezepte` für die Secret-tragende Unit — codekonform
  (`JwtSigningKeyProvider.cs:51–58`, `MinimumKeyLength=32`).
- **Test-assertierte Literale:** Tabelle Z. 52–78 vollständig und exakt
  (inkl. `NotContain("Rezepte.dll")`-Falle und synchronem Publish-Befehl zum
  OsInterface-Test). Struktur hält alle `Contain`-Strings
  (`chmod +x /var/www/rezepte/Rezepte.Web` im self-contained-Hinweisblock).
- **Persistenz-Präzision:** `logs/` am `AppContext.BaseDirectory` vs.
  DB/Backups/Exports/Plugins am `WorkingDirectory`; Abdeckung über identisches
  `/var/www/rezepte` + `ReadWritePaths` + `chown` korrekt beschrieben —
  Risiko 2 in plan.md Z. 338–342 nennt die Abhängigkeit ausdrücklich.
- **Verlinkung:** `index.md:12` (Linktext), `deployment-guide.md:64`
  (`www-data` → `rezepte`), `README.md` Z. 72–76 (nur Zusatzsatz, Literale
  bleiben) — deckt alle Fundstellen aus der Inventory ab.
- **Akzeptanzszenarien + Prüfstrategie:** drei Szenarien mit konkreten
  Nachweisen; gezielter Testfilter, Vollauf, Link-/Literal-Greps,
  Private-Repo-Verweis-Suche und Dry-Run-Prüfpunkte (jetzt inkl.
  Transfer-Mechanismus und Matcher) — ausreichend und auf die geänderten
  Risiken zugeschnitten.

## Verbleibende Anmerkungen (kosmetisch, kein Blocker)

1. Formulierung in Entscheidung 9: „der Microsoft-Feed … enthält für Ubuntu
   keine Pakete mehr" ist als Verkürzung lesbar — gemeint ist „für Ubuntu
   24.04 (bzw. .NET)". Bei der Umsetzung in `install.md` präzise auf 24.04
   beziehen, damit die Aussage auch für Leser mit Ubuntu 22.04 im Hinterkopf
   stimmt.
2. ufw-Syntax uneinheitlich: Struktur B.4 skizziert `ufw allow 80,443/tcp`,
   Szenario 2 verwendet zwei Einzelbefehle (`allow 80/tcp` + `allow 443/tcp`).
   Beides ist gültig — in der finalen Datei für eine Variante entscheiden.

## Ergebnis

**Plan vollständig.** Alle Pflichtkorrekturen sind sauber und konsistent
eingearbeitet; der Plan ist codekonform, deckt beide Installationspfade plus
Update/Backup ab, hält die test-assertierten Literale stabil und enthält eine
belastbare Prüfstrategie. Umsetzungsreif.
