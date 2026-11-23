# Plan-Check: Installationsdokumentation mit/ohne Reverse-Proxy (Issue #214)

**Status: Plan lückenhaft** — eine konkrete fachliche Lücke in der geplanten
Caddy-Konfiguration (Pfad-Matcher deckt die geplante Verifikations-URL nicht ab)
sowie zwei kleinere Umsetzungslücken sind vor der Umsetzung zu korrigieren.
Der Plan ist ansonsten vollständig, codekonform und gut abgesichert.

Geprüft gegen `requirement.md`, `inventory.md` + Detaildokumente, den Code
(`Rezepte.Web/Program.cs`, `JwtSigningKeyProvider.cs`, `LoggingExtensions.cs`,
`appsettings.json`), die test-assertierten Literale
(`Rezepte.Tests/Deployment/DeploymentDocumentationTests.cs`) und die
Verlinkungsziele (`Docs/index.md`, `Docs/deployment-guide.md`, `README.md`).

## Befund 1 (muss korrigiert werden): `handle /rezepte/*` matcht `/rezepte` nicht

Der geplante Caddyfile-Abschnitt (plan.md, Struktur B.4) lautet:

```text
rezepte.example.com {
    handle /rezepte/* {
        reverse_proxy 127.0.0.1:5000
    }
    handle {
        respond 404
    }
}
```

Das Konzept „kein Pfad-Stripping" ist korrekt (`handle`, nicht `handle_path` —
die App strippt selbst via `UsePathBase`, Program.cs:76–80). Der **Matcher ist
jedoch zu eng**: Caddy matcht Pfade exakt; `/rezepte/*` ist ein Präfix-Match auf
`/rezepte/` und deckt den exakten Pfad `/rezepte` (ohne Trailing-Slash) **nicht**
ab — laut offizieller Caddy-Matcher-Dokumentation matcht erst `/rezepte*` auch
`/rezepte` (dann aber ebenso `/rezeptefoo`). Ein Request auf
`https://rezepte.example.com/rezepte` fiele in den Fallback und bekäme **404**.

Damit widerspricht die Konfiguration dem eigenen Verifikationsschritt in
Szenario 2 (`curl -I https://rezepte.example.com/rezepte` → erwartet 200/302)
und der im Browser dokumentierten Einstiegs-URL.

**Korrektur (im Plan bzw. in der Struktur B.4 zu übernehmen), z. B.:**

```text
rezepte.example.com {
    @rezepte path /rezepte /rezepte/*
    handle @rezepte {
        reverse_proxy 127.0.0.1:5000
    }
    handle {
        respond 404
    }
}
```

Alternativ `redir /rezepte /rezepte/` vor den `handle`-Blöcken. Nicht empfohlen:
`handle /rezepte*` (matcht auch `/rezeptefoo`). Der App-seitige Loopback-Check
`curl -I http://127.0.0.1:5000/rezepte` ist davon nicht betroffen — dort greift
`UsePathBase` auch auf den exakten Pfad.

## Befund 2 (klein, zu ergänzen): Transfer-Schritt berücksichtigt Zielverzeichnis-Rechte nicht

Die geplante Reihenfolge (Gemeinsame Schritte 3→4) sieht vor:
`sudo mkdir -p /var/www/rezepte` → `scp`/`rsync` des Publish-Inhalts → erst danach
`useradd`/`chown`. Ein `scp`/`rsync` als normaler SSH-Benutzer in ein root-eigenes
`/var/www/rezepte` schlägt fehl — der Dry-Run-Checkpoint würde das zwar auffinden,
aber der Plan sollte den Mechanismus konkretisieren, z. B.:

- Kopieren ins Home-Verzeichnis und anschließend `sudo cp -r`/`sudo mv`, oder
- `rsync --rsync-path="sudo rsync"`, oder
- `sudo chown` des (noch leeren) Zielverzeichnisses auf den SSH-Benutzer vor dem
  Transfer, danach `chown -R rezepte:rezepte`.

## Befund 3 (klein, bei Umsetzung zu verifizieren): .NET-10-apt-Quelle auf Ubuntu 24.04

Der Plan sieht einen „kurzen apt-Hinweis zur .NET-10-Installation auf Ubuntu
24.04" vor. Bei der Umsetzung sicherstellen, dass die benannte Paketquelle
`.NET 10`/`aspnetcore-runtime-10` für noble tatsächlich ausliefert (Canonical-
Feed vs. Microsoft-Feed `packages.microsoft.com`) — nicht stillschweigend
`apt install dotnet-sdk-10` ohne Quellenangabe zeigen.

## Verifiziert als korrekt/vollständig (keine Befunde)

### Vollständigkeit beider Installationspfade (Anforderung, AK 2+3)

- **Pfad A** deckt alle geforderten Elemente ab: Runtime-Prüfung (`dotnet --info`,
  beide Shared Frameworks inkl. `System.Runtime.Serialization.Primitives.dll`-
  Begründung), beide Publish-Varianten, Transfer, dedizierter Service-Account
  `rezepte` mit `chown`, systemd-Unit, `ASPNETCORE_URLS=http://0.0.0.0:5000`,
  `ufw allow 5000/tcp`, Verifikation (`systemctl status`, `journalctl -u rezepte`,
  `logs/app-*.log`, `curl -I`), HTTP-only-Einschränkung (offene Frage 5 sauber
  entschieden).
- **Pfad B** deckt ab: Caddy-Beispiel mit automatischem TLS, Pfad-Präfix ohne
  Stripping, `X-Forwarded-Proto`/`-For` (von Caddy `reverse_proxy` automatisch
  gesetzt — korrekt), `Environment=PathBase=/rezepte`, Loopback-Bind
  `http://127.0.0.1:5000`, Same-Host-Bedingung, Subdomain-Alternative (offene
  Frage 4 entschieden), Hinweisblock für andere Proxys inkl. Host-Durchreichen
  und WebSocket/SignalR.
- Gemeinsamer Basis-Abschnitt plus zwei eigenständig nachvollziehbare Pfade
  entspricht der geforderten Struktur; „alternativ, nicht gleichzeitig" ist im
  Ziel benannt.

### Codekonforme Aussagen

- `PathBase` via `UsePathBase()`, No-op wenn ungesetzt, vor der restlichen
  Pipeline — verifiziert (`Program.cs:76–80`); `Environment=PathBase=/rezepte`
  ist die korrekte Env-Form (einfacher Schlüsselname, kein `__` nötig).
- `UseForwardedHeaders` = `XForwardedProto | XForwardedFor`, `KnownProxies` =
  IPv4- + IPv6-Loopback — verifiziert (`Program.cs:87–93`). Same-Host-Pflicht,
  fehlende `X-Forwarded-Host`-Auswertung und die Rate-Limiting-Folge ohne
  wirksames `X-Forwarded-For` sind korrekt abgeleitet.
- `Jwt:Key`: Minimum 32 Zeichen, `InvalidOperationException` außerhalb
  Development — verifiziert (`JwtSigningKeyProvider.cs:31,51–58`,
  `appsettings.json` liefert `""`). `Environment=Jwt__Key=` (doppelter
  Unterstrich) ist die korrekte Mapping-Form; `openssl rand -base64 48`
  erzeugt ≥ 32 Zeichen; Base64-Zeichensatz (`+`, `/`, `=`) ist in systemd-
  `Environment=` unproblematisch. `EnvironmentFile=`-Alternative sinnvoll.
- Persistente Pfade korrekt differenziert: `logs/` am `AppContext.BaseDirectory`
  (`LoggingExtensions.cs:25`), `rezepte.db`/`update-backups/`/`exports/`/
  `plugins/` am ContentRoot/`WorkingDirectory`; alle unter `/var/www/rezepte` —
  `ReadWritePaths=/var/www/rezepte` + `chown` deckt sie korrekt ab. Rezeptbilder
  in SQLite → kein Upload-Verzeichnis nötig (Anforderungsformulierung korrekt
  relativiert).
- systemd-Härtung plausibel: `ProtectSystem=strict` + `ReadWritePaths`,
  `ProtectHome`, `PrivateTmp`, `NoNewPrivileges` unproblematisch für die App;
  Port 5000 braucht keine `AmbientCapabilities`; `/usr/sbin/nologin` und
  `/usr/bin/dotnet` sind Ubuntu-24.04-korrekt; `useradd --system` erzeugt die
  gleichnamige Gruppe.
- Unit-Datei als Secret-Träger: `chmod 640 root:rezepte` funktioniert (systemd
  lädt als root; `systemctl status` via D-Bus auch für Nicht-root lesbar).
- ufw: `ufw allow 5000/tcp` und `ufw allow 80,443/tcp` (Multiport-Syntax
  gültig); Loopback-Bind im Pfad B ohne Port-Freigabe korrekt.
- Console-Logging auf `Error` beschränkt (`LoggingExtensions.cs:37`) →
  `journalctl`-Hinweis + `logs/app-*.log` für Detaildiagnose korrekt.

### Test-assertierte Literale

Alle Literale stimmen exakt mit `DeploymentDocumentationTests.cs` überein
(install.md: Z. 25–34 zehn Assertions inkl. `NotContain("Rezepte.dll")`;
README: Z. 45–49; Publish-Befehl Z. 68). Der Plan listet sie vollständig und
korrekt, inklusive der Falle, dass `Rezepte.dll` nie allein stehen darf.

### Akzeptanzszenarien, Prüfstrategie, Verlinkung, Nicht-Ziele

- Drei Szenarien (Standalone, Reverse-Proxy, Update/Backup) decken die AK;
  Ausgangslagen (sudo, DNS, Publish-Maschine) und Nachweise konkret.
- Prüfstrategie: gezielter Testfilter, Volllauf, manuelle Linkprüfung,
  `Rezepte.dll`/`www-data`-Grep, Suche nach privaten Repo-Verweisen,
  Fresh-Server-Dry-Run mit benannten Prüfpunkten — angemessen und ausreichend.
- Verlinkung: `Docs/index.md:12` (Linktext), `Docs/deployment-guide.md:64`
  (`User=www-data` → `User=rezepte` — Fundstelle verifiziert), `README.md`
  (minimaler Zusatz, Literale bleiben). `deployment-guide.md:14/40` bleiben
  gültig. Scope-Einhaltung (keine Vermischung) gewahrt.
- Private Repo-Verweise: Grep-Check geplant; die nötige Caddy-apt-Quelle
  (cloudsmith) ist eine Installationsquelle, kein Repo-Verweis im Sinne der
  Anforderung — vertretbar, aber bei der Umsetzung auf minimale externe
  Nennungen achten (kein GitHub-Link zum Infrastruktur-Projekt).
- Nicht-Ziele: implizit über „Keine Änderungen an", HTTP-only-Hinweis und
  Alternativ-Formulierung abgedeckt — ein kurzer expliziter
  Nicht-Ziele-Abschnitt in `install.md` wäre wünschenswert (kein Blocker).

## Ergebnis

**Plan lückenhaft.** Vor Umsetzung zu korrigieren:

1. **Befund 1 (Pflicht):** Caddy-Matcher für den exakten Pfad `/rezepte`
   ergänzen (`@rezepte path /rezepte /rezepte/*` + `handle @rezepte`, oder
   `redir /rezepte /rezepte/`); sonst schlägt der eigene
   Verifikations-Schritt fehl.
2. **Befund 2 (Pflicht):** Transfer-Schritt um einen funktionierenden
   Schreibmechanismus für `/var/www/rezepte` ergänzen (sudo-Kopie aus dem
   Home-Verzeichnis o. ä.).
3. **Befund 3 (bei Umsetzung verifizieren):** apt-Quelle für .NET 10 auf
   Ubuntu 24.04 konkret und korrekt benennen.
4. Optional: expliziten Nicht-Ziele-Abschnitt in `install.md` vorsehen.

Nach diesen Korrekturen ist der Plan vollständig und umsetzungsreif.
