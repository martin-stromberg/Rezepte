# Umsetzungsplan: Installationsdokumentation mit/ohne Reverse-Proxy (Issue #214)

## Ziel

`Docs/install.md` wird zu einer eigenständig lauffähigen Schritt-für-Schritt-Anleitung
umgebaut, die einen Administrator ohne Kenntnis externer Infrastruktur-Projekte von
„leerer Server" bis „Anwendung läuft" führt — über zwei alternative, jeweils
vollständige Pfade:

- **Pfad A — Eigenständig (ohne Reverse-Proxy):** HTTP-Betrieb auf eigenem Port,
  direkt erreichbar.
- **Pfad B — Hinter Reverse-Proxy (Caddy):** externe TLS-Terminierung,
  `PathBase`-Routing, `X-Forwarded-*`-Header.

Gemeinsame Schritte (Runtime-Prüfung, Publish, Transfer, Service-Account) werden in
einen gemeinsamen Basis-Abschnitt ausgelagert. Verlinkung in `Docs/index.md`,
`README.md` und `Docs/deployment-guide.md` wird synchron gehalten.

## Getroffene Entscheidungen

| Nr. | Entscheidung | Festgelegt auf |
|---|---|---|
| 1 | Zielplattform | **Ubuntu Server 24.04 LTS auf x86_64 mit systemd** als Referenz. Knapper Hinweis in der Einleitung: Debian 12+ funktioniert mit angepassten Paketbefehlen (abweichende Paketquellen für .NET/Caddy, ggf. `apt`- statt `ufw`-Beispiel). |
| 2 | Referenz-Reverse-Proxy | **Caddy** (automatisches TLS via Let's Encrypt, `reverse_proxy` setzt `X-Forwarded-Proto`/`X-Forwarded-For` automatisch und lässt WebSocket-Upgrades für Blazor/SignalR ohne Zusatzkonfiguration zu). Knapper Hinweis-Block: andere Proxys (nginx, Apache, Traefik) müssen dieselben Regeln abbilden — Pfad-Präfix ohne Stripping weiterleiten, `Host`-Header durchreichen, `X-Forwarded-Proto` + `X-Forwarded-For` setzen, WebSocket-Upgrades erlauben. |
| 3 | Service-Account und Härtung | **Dedizierter System-Account `rezepte` ist der dokumentierte Standard** (verbindlich in der Anleitung, keine optionale Empfehlung). Moderate systemd-Härtung als Standard-Unit: `NoNewPrivileges=true`, `PrivateTmp=true`, `ProtectSystem=strict`, `ProtectHome=true`, `ReadWritePaths=/var/www/rezepte`. Keine weiteren Direktiven — `ReadWritePaths` deckt bereits alle schreibenden Pfade ab, weil Publish-Verzeichnis, `WorkingDirectory` (ContentRoot) und `AppContext.BaseDirectory` (hängt `logs/` an) identisch sind. |
| 4 | Proxy-Modus | Primär **`PathBase=/rezepte`** auf der Domain `rezepte.example.com` mit Caddy **ohne Pfad-Stripping** (die App erwartet den vollen Pfad und strippt selbst via `UsePathBase`). Der Matcher lautet `@rezepte path /rezepte /rezepte/*` + `handle @rezepte` — `/rezepte/*` allein matcht den exakten Pfad `/rezepte` nicht (würde im Fallback 404 liefern), `/rezepte*` würde zusätzlich `/rezeptefoo` matchen und wird nicht verwendet. Kurze Alternative: Root-Pfad auf eigener Subdomain, `PathBase` dann ungesetzt — als knapper Unterabschnitt dokumentiert. |
| 5 | Standalone-Exposition | `ASPNETCORE_URLS=http://0.0.0.0:5000` plus `sudo ufw allow 5000/tcp`. Ausdrücklicher Hinweis: **HTTP-only**, die App bringt kein eigenes TLS mit; Empfehlung: nur in vertrauenswürdigen Netzen betreiben oder Pfad B (Reverse-Proxy mit TLS) nutzen. |
| 6 | Konkrete Vorgabewerte | Port **5000**, Deployment-Pfad **`/var/www/rezepte`**, Benutzer/Gruppe **`rezepte`**, **`PathBase=/rezepte`**, Domain-Platzhalter **`rezepte.example.com`**, Unit-Name `rezepte.service`. |
| 7 | Umfang | Erstinstallation **plus** kompakter Abschnitt „Update und Backup": `rezepte.db` bei gestopptem Dienst sichern, neues Publish kopieren, Eigentümer korrigieren, Dienst starten. |
| 8 | `Jwt:Key` | **Pflicht-Schritt:** `Environment=Jwt__Key=<zufälliger Schlüssel>` in beiden Unit-Varianten; Erzeugung per `openssl rand -base64 48` dokumentiert; Anforderung „mindestens 32 Zeichen" explizit benannt (sonst `InvalidOperationException` beim Start unter Production). Zusätzlich kurzer Hinweis auf `EnvironmentFile=` als Alternative für Secrets. |
| 9 | .NET-10-Paketquelle | Auf Ubuntu 24.04 liegt .NET 10 im **eingebauten Ubuntu-Feed** (`noble-updates`/`noble-security`, Quellpaket `dotnet10`); der Microsoft-Feed `packages.microsoft.com` enthält für Ubuntu keine Pakete mehr. Dokumentiert wird `sudo apt update && sudo apt install aspnetcore-runtime-10.0` (serverseitig genügt die ASP.NET-Core-Runtime; Alternativnennung: Metapaket `dotnet10` inkl. SDK). Für Debian 12+: Hinweis, dass dort der Microsoft-Feed (`packages.microsoft.com/config/debian/12`) einzurichten ist — als eine Zeile im Debian-Hinweis, kein eigener Befehlsblock. |
| 10 | Datei-Transfer | Publish wird zuerst per `scp`/`rsync` **ins Home-Verzeichnis** des SSH-Benutzers kopiert (`~/publish`), dann mit `sudo mkdir -p /var/www/rezepte` und `sudo cp -r ~/publish/. /var/www/rezepte/` als root übertragen — ein direktes `scp`/`rsync` in das root-eigene Zielverzeichnis scheitert an Schreibrechten. Als Alternative ein Satz zu `rsync --rsync-path="sudo rsync"` bzw. vorübergehendem `chown` des leeren Ziels auf den SSH-Benutzer. |

### Verifiziertes Laufzeitverhalten, das die Doku abbilden muss

- `UseForwardedHeaders` vertraut ausschließlich `KnownProxies` = IPv4/IPv6-Loopback
  (`Program.cs` ~Z. 87–93) → **Same-Host-Pflicht des Proxys** wird explizit
  dokumentiert; App im Proxy-Pfad an `http://127.0.0.1:5000` binden.
- `X-Forwarded-Host` wird **nicht** ausgewertet → die Proxy-Konfiguration muss den
  originalen `Host`-Header durchreichen (Caddy `reverse_proxy` macht das per Default;
  im Hinweis-Block für andere Proxys benannt).
- `X-Forwarded-For` muss wirken, sonst rate-limitet die App (Login/Register) alle
  Clients über die Proxy-Loopback-IP gemeinsam.
- Schreibende Pfade: `rezepte.db`, `update-backups/`, `exports/`, `plugins/` am
  `WorkingDirectory`; `logs/` am Assembly-Verzeichnis — alle unter
  `/var/www/rezepte`, durch `ReadWritePaths=/var/www/rezepte` und Eigentümer
  `rezepte:rezepte` abgedeckt. Rezeptbilder liegen in SQLite (kein
  Upload-Verzeichnis).
- Console-Logging ist auf `Error` beschränkt → Verifikation läuft über
  `systemctl status`, `journalctl -u rezepte` (nur Fehler) und `logs/app-*.log`.

## Harte Constraints (test-assertierte Literale — nicht entfernen/umformulieren)

Aus `Rezepte.Tests/Deployment/DeploymentDocumentationTests.cs`:

**`Docs/install.md`** (`InstallDocumentation_ShouldDescribeRuntimeRequirementsAndCorrectEntrypoint`, Z. 21–35):

| Art | Literal |
|---|---|
| `NotContain` | `Rezepte.dll` — der Dateiname darf **nirgends** im Dokument vorkommen (immer `Rezepte.Web.dll` schreiben) |
| `Contain` | `Rezepte.Web.dll` |
| `Contain` | `Rezepte.Web` |
| `Contain` | `dotnet --info` |
| `Contain` | `Microsoft.NETCore.App` |
| `Contain` | `Microsoft.AspNetCore.App` |
| `Contain` | `System.Runtime.Serialization.Primitives.dll` (inkl. der inhaltlichen Begründung zum `FileNotFoundException`-Risiko) |
| `Contain` | `--self-contained false` |
| `Contain` | `--self-contained true` |
| `Contain` | `chmod +x /var/www/rezepte/Rezepte.Web` |

**`README.md`** (`ReadmeDeploymentSection_ShouldPointToRuntimeCheckedDeploymentOptions`, Z. 41–50):

`Docs/install.md`, `.NET-10-Shared-Frameworks`, `Microsoft.NETCore.App`,
`Microsoft.AspNetCore.App`, `self-contained` — alle erhalten bleiben.

**Publish-Befehl synchron zum OsInterface-Test** (Z. 56–105): der dokumentierte
framework-abhängige Befehl `dotnet publish Rezepte.Web -c Release -f net10.0 -r linux-x64 --self-contained false`
bleibt exakt so stehen (der Test führt ihn real aus).

## Geplante Struktur der neuen `Docs/install.md`

Deutsch, „du"-Form, echte Umlaute (Konvention der Datei, Git-History zeigt
Umstellung auf echte Umlaute), Codeblock-Tags `bash` / `powershell` / `ini` —
für die Caddyfile zusätzlich ein ```text- oder `caddy`-Block.

```text
# Installationsanweisungen
  Einleitung: zwei alternative Betriebsarten (eigenständig / hinter Reverse-Proxy),
  Referenzplattform Ubuntu Server 24.04 LTS x86_64 mit systemd, net10.0,
  Hinweis Debian 12+ mit angepassten Paketbefehlen,
  Platzhalter-Tabelle (/var/www/rezepte, rezepte, Port 5000, /rezepte,
  rezepte.example.com).

## Gemeinsame Schritte
### 1. Runtime auf dem Server prüfen
  Bestehender Abschnitt weitgehend unverändert (dotnet --info, beide
  Shared Frameworks, System.Runtime.Serialization.Primitives.dll-Begründung).
  Ergänzt: .NET-10-Installation auf Ubuntu 24.04 aus dem eingebauten
  Ubuntu-Feed: sudo apt update && sudo apt install aspnetcore-runtime-10.0
  (Alternativnennung: Metapaket dotnet10 inkl. SDK; kein Microsoft-Feed
  für Ubuntu nötig/verfügbar).
### 2. Publish erzeugen
  Beide bestehenden Publish-Befehle unverändert (--self-contained false/true).
### 3. Dateien auf den Server übertragen
  scp -r <publish-ordner> user@server:~/publish (bzw. rsync -a ... ~/publish),
  dann auf dem Server:
    sudo mkdir -p /var/www/rezepte
    sudo cp -r ~/publish/. /var/www/rezepte/
  Ein Satz als Alternative: rsync --rsync-path="sudo rsync" direkt ins Ziel
  oder vorübergehendes chown des leeren Ziels auf den SSH-Benutzer.
### 4. Dedizierten Service-Account anlegen und Rechte setzen
  sudo useradd --system --home /var/www/rezepte --shell /usr/sbin/nologin rezepte
  sudo chown -R rezepte:rezepte /var/www/rezepte
  Begründung: DB, logs/, update-backups/, exports/, plugins/ liegen unter
  diesem Verzeichnis und brauchen Schreibrechte.
### 5. JWT-Signierschlüssel erzeugen
  openssl rand -base64 48 → in die Unit eintragen (Environment=Jwt__Key=...),
  Mindestlänge 32 Zeichen, Hinweis auf EnvironmentFile-Alternative.

## Pfad A: Eigenständiger Betrieb (ohne Reverse-Proxy)
### A.1 systemd-Service einrichten
  Unit: User=rezepte, Group=rezepte, WorkingDirectory=/var/www/rezepte,
  ExecStart=/usr/bin/dotnet /var/www/rezepte/Rezepte.Web.dll,
  Environment=ASPNETCORE_ENVIRONMENT=Production,
  Environment=ASPNETCORE_URLS=http://0.0.0.0:5000,
  Environment=Jwt__Key=<...>,
  Härtung: NoNewPrivileges/PrivateTmp/ProtectSystem=strict/ProtectHome/
  ReadWritePaths=/var/www/rezepte, Restart=always, RestartSec=10,
  SyslogIdentifier=rezepte-api.
  Hinweis: Einheitsdatei enthält Secret → sudo chmod 640 + root:rezepte.
  Hinweisblock self-contained: chmod +x /var/www/rezepte/Rezepte.Web +
  ExecStart=/var/www/rezepte/Rezepte.Web (Literale erhalten).
### A.2 Firewall freigeben
  sudo ufw allow 5000/tcp
  HTTP-only-Einschränkung + Empfehlung vertrauenswürdige Netze / Pfad B.
### A.3 Dienst starten und verifizieren
  daemon-reload/enable/start/status; journalctl -u rezepte -f;
  logs/app-*.log; curl -I http://localhost:5000 (200/302 erwartet);
  Browser http://<server>:5000 zeigt Registrierungs-/Login-Seite.

## Pfad B: Betrieb hinter einem Reverse-Proxy (Caddy)
### B.1 systemd-Service einrichten
  Wie A.1, aber Environment=ASPNETCORE_URLS=http://127.0.0.1:5000 und
  zusätzlich Environment=PathBase=/rezepte; Port 5000 wird NICHT in der
  Firewall freigegeben (Loopback-Bind).
### B.2 Voraussetzungen
  DNS A/AAAA-Record für rezepte.example.com zeigt auf den Server;
  Proxy läuft auf demselben Host (KnownProxies = Loopback, verifiziert).
### B.3 Caddy installieren
  Installation über das offizielle Caddy-apt-Repository (kurzer Block).
### B.4 Caddy konfigurieren
  /etc/caddy/Caddyfile:
    rezepte.example.com {
        @rezepte path /rezepte /rezepte/*
        handle @rezepte {
            reverse_proxy 127.0.0.1:5000
        }
        handle {
            respond 404
        }
    }
  Erläuterung: named matcher deckt /rezepte UND /rezepte/... ab
  (/rezepte/* allein matcht den exakten Pfad nicht; /rezepte* würde
  auch /rezeptefoo matchen — beides kurz benannt),
  kein Pfad-Stripping (UsePathBase erledigt das),
  Caddy setzt X-Forwarded-Proto/-For automatisch, reicht Host durch,
  WebSocket (Blazor/SignalR) ohne Zusatzconfig; automatisches TLS.
  sudo systemctl reload caddy; ufw allow 80,443/tcp.
### B.5 Alternative: Root-Pfad auf eigener Subdomain
  Kurzer Block: rezepte.example.com { reverse_proxy 127.0.0.1:5000 },
  PathBase entfällt (Environment-Zeile weglassen).
### B.6 Hinweis für andere Reverse-Proxys
  Knappe Regelliste: Pfad-Präfix unverändert weiterleiten, Host-Header
  durchreichen, X-Forwarded-Proto/-For setzen, WebSocket-Upgrade
  erlauben, Proxy auf demselben Host.
### B.7 Dienst starten und verifizieren
  daemon-reload/enable/start; systemctl status rezepte + caddy;
  curl -I https://rezepte.example.com/rezepte (TLS + 200/302);
  Browser-Check Registrierungs-/Login-Seite.

## Update und Backup
  Kompakt: sudo systemctl stop rezepte;
  cp /var/www/rezepte/rezepte.db ~/rezepte.db-backup-$(date +%F);
  neues Publish wie in Schritt 3 einspielen (ins Home, dann sudo cp -r);
  chown -R rezepte:rezepte; systemctl start;
  kurzer Verweis, dass Migrationen beim Start automatisch laufen.

## Nicht abgedeckt (Nicht-Ziele)
  Kurzer Abschnitt am Ende: kein gleichzeitiger Betrieb beider Pfade auf
  einem Server (Alternativen); kein TLS direkt in der Anwendung (nur über
  Pfad B); kein automatisierter Rollout (Skripte, Ansible o. ä.);
  andere Reverse-Proxys nur als Hinweis-Regelliste, nicht als
  vollständige Anleitung.
```

## Konkrete Dateiänderungen

### 1. `Docs/install.md` — vollständiger Umbau (siehe Struktur oben)

- Bestehende, verifizierte Inhalte (Runtime-Prüfung, beide Publish-Befehle,
  `chmod +x`-Block, `WorkingDirectory`-Begründung) werden in den gemeinsamen
  Basis-Abschnitt bzw. die Pfad-Abschnitte übernommen.
- Neu: `Jwt__Key`-Schritt, Service-Account `rezepte`, Härtungs-Direktiven,
  `ASPNETCORE_URLS` (beide Bind-Varianten), `ufw`-Schritte, Verifikationsblock
  (`systemctl status`, `journalctl`, `logs/app-*.log`, `curl`, Browser),
  kompletter Pfad B (Caddy, `PathBase`, Same-Host-Bedingung, Header-Regeln,
  Subdomain-Alternative), Abschnitt „Update und Backup", abschließender
  kurzer Abschnitt „Nicht abgedeckt" (Nicht-Ziele).

### 2. `Docs/index.md` — Zeile 12 anpassen

Linktext erweitern, z. B.:
`[Installationsanleitung](install.md) — manuelles Deployment auf Linux mit systemd: eigenständig oder hinter einem Reverse-Proxy.`
Keine weiteren Änderungen.

### 3. `Docs/deployment-guide.md` — nur Zeile 64 korrigieren

`User=www-data` → `User=rezepte` (Verweis „aus `Docs/install.md`" bleibt
inhaltlich korrekt). Keine inhaltliche Vermischung — Google-Credentials-Thema
unverändert.

### 4. `README.md` — Deployment-Abschnitt (Z. 72–76) minimal ergänzen

Einen Satz ergänzen, dass die Anleitung beide Betriebsarten abdeckt
(eigenständig und hinter Reverse-Proxy mit TLS). Alle test-assertierten
Literale bleiben wörtlich erhalten; keine Umformulierung der bestehenden
Sätze.

**Keine Änderungen an:** `Rezepte.Web/Program.cs`, `appsettings*.json`,
Testdateien, Workflows, `Docs/RELEASE_NOTES.md`.

## Akzeptanzszenarien

### Szenario 1 — Happy Path Standalone (Pfad A)

Ausgang: frischer Ubuntu Server 24.04 LTS x86_64, SSH-Zugang mit sudo,
Publish-Maschine mit .NET-10-SDK.

1. Auf dem Server die .NET-10-Runtime aus dem Ubuntu-Feed installieren:
   `sudo apt update && sudo apt install aspnetcore-runtime-10.0`;
   `dotnet --info` zeigt
   `Microsoft.NETCore.App` und `Microsoft.AspNetCore.App` in Version 10.x.
2. Auf der Publish-Maschine:
   `dotnet publish Rezepte.Web -c Release -f net10.0 -r linux-x64 --self-contained false`
   (oder `--self-contained true`, dann ohne Runtime auf dem Server).
3. Publish-Inhalt per `scp`/`rsync` nach `~/publish` ins Home-Verzeichnis
   kopieren; auf dem Server `sudo mkdir -p /var/www/rezepte` und
   `sudo cp -r ~/publish/. /var/www/rezepte/` ausführen.
4. `sudo useradd --system --home /var/www/rezepte --shell /usr/sbin/nologin rezepte`;
   `sudo chown -R rezepte:rezepte /var/www/rezepte`.
5. `openssl rand -base64 48` → in `/etc/systemd/system/rezepte.service` als
   `Environment=Jwt__Key=<Wert>` eintragen; Unit aus Pfad A übernehmen
   (inkl. `ASPNETCORE_URLS=http://0.0.0.0:5000` und Härtungs-Direktiven).
6. `sudo ufw allow 5000/tcp`.
7. `sudo systemctl daemon-reload && sudo systemctl enable --now rezepte.service`.
8. **Nachweis:**
   - `systemctl status rezepte.service` → `active (running)`.
   - `journalctl -u rezepte.service` → keine Fehler; bei Problemen
     `sudo tail /var/www/rezepte/logs/app-*.log`.
   - `curl -I http://localhost:5000` → HTTP 200 oder 302.
   - Browser `http://<server-ip>:5000` → Registrierungs-/Login-Seite der
     Anwendung.

### Szenario 2 — Happy Path Reverse-Proxy (Pfad B)

Ausgang: wie Szenario 1, zusätzlich DNS A/AAAA-Record `rezepte.example.com`
→ Server-IP.

1.–4. Wie Szenario 1 (Runtime, Publish, Transfer, Account).
5. Unit aus Pfad B: `ASPNETCORE_URLS=http://127.0.0.1:5000`,
   `Environment=PathBase=/rezepte`, `Jwt__Key`, gleiche Härtung.
6. Caddy aus dem offiziellen apt-Repository installieren; Caddyfile wie im
   Plan (`@rezepte path /rezepte /rezepte/*` + `handle @rezepte` →
   `reverse_proxy 127.0.0.1:5000`, Fallback `respond 404`);
   `sudo systemctl reload caddy`;
   `sudo ufw allow 80/tcp && sudo ufw allow 443/tcp` (Port 5000 bleibt
   geschlossen).
7. `sudo systemctl daemon-reload && sudo systemctl enable --now rezepte.service`.
8. **Nachweis:**
   - `systemctl status rezepte.service` und `systemctl status caddy` →
     `active (running)`.
   - `curl -I http://127.0.0.1:5000/rezepte` auf dem Server → 200/302.
   - `curl -I https://rezepte.example.com/rezepte` und
     `curl -I https://rezepte.example.com/rezepte/` → gültiges TLS
     (Let's-Encrypt-Zertifikat, von Caddy automatisch ausgestellt),
     HTTP 200/302; Redirect-Ziele beginnen mit `https://rezepte.example.com/rezepte`.
   - Browser `https://rezepte.example.com/rezepte` → App lädt,
     Registrierung/Login funktioniert (interaktive Blazor-Verbindung /
     WebSocket steht).

### Szenario 3 — Update/Backup (kompakt)

`systemctl stop rezepte` → `rezepte.db` kopieren → neues Publish einspielen →
`chown` → `systemctl start` → `systemctl status` + `curl` wie in Szenario 1/2.

## Test- und Prüfstrategie

1. **Gezielte Doku-Tests (Pflicht):**
   `dotnet test Rezepte.Tests --filter "FullyQualifiedName~DeploymentDocumentationTests"`
   — beide deterministischen Tests müssen grün sein; der
   OsInterface-Publish-Test läuft lokal ggf. mit, dokumentierter
   Publish-Befehl bleibt ohnehin unverändert.
2. **Voller deterministischer Testlauf (sinnvoll, vertretbar):**
   `dotnet test Rezepte.Tests` — da die PR-Pipeline ohne Pfadfilter die volle
   Suite fährt, sichert ein lokaler Vollauf ab; Docs-only-Änderung erwartet
   keine neuen Failures.
3. **Markdown-/Linkprüfung (manuell, kein Lint-Gate im CI):**
   - Relative Links `index.md` → `install.md`, `deployment-guide.md` →
     `install.md`, `README.md` → `Docs/install.md` auflösen.
   - Alle Codeblöcke mit Sprach-Tag; systemd-Units syntaktisch konsistent
     (`[Unit]`/`[Service]`/`[Install]`); Caddyfile-Klammern geschlossen.
   - `grep` über die geänderten Dateien: kein Vorkommen von `Rezepte.dll`
     (außer als Teil von `Rezepte.Web.dll`), kein `www-data` mehr in
     `install.md`/`deployment-guide.md`.
4. **Suche nach privaten Repo-Verweisen:** in den geänderten Dateien nach
   `github.com/`, Repo-Namen des Infrastruktur-Projekts und generell externen
   Links suchen — die Anleitung darf keine Verweise auf andere/private
   Repositories enthalten (einzige legitime externe Verweise: keine;
   Caddy-Installationsbefehle sind generische Kommandos, kein Repo-Link nötig).
5. **Gedanklicher Fresh-Server-Dry-Run:** die beiden Akzeptanzszenarien
   Schritt für Schritt gegen die fertige `install.md` abgleichen — jeder
   Befehl muss wörtlich ausführbar sein, jede Voraussetzung (sudo, DNS,
   Paketquellen) muss im Text stehen, bevor sie gebraucht wird. Besondere
   Prüfpunkte: `Jwt__Key` gesetzt, bevor `systemctl start` kommt;
   `ReadWritePaths` deckt `logs/` ab; Loopback-Bind im Pfad B;
   keine Firewall-Freigabe für Port 5000 im Pfad B;
   Transfer-Mechanismus verwendet das Home-Verzeichnis + `sudo cp -r`
   (kein direktes `scp` in das root-eigene Ziel);
   Caddy-Matcher deckt `/rezepte` exakt UND `/rezepte/...` ab, ohne
   `/rezeptefoo` zu matchen.

## Risiken und Constraints

1. **Testbruch durch Umformulierung** (höchstes Risiko): die oben
   tabellierten Literale sind harte Constraints — beim Umbau zuerst
   sicherstellen, dass alle `Contain`-Strings weiterhin wörtlich vorkommen
   und `Rezepte.dll` nie allein steht.
2. **`logs/` am Assembly-Verzeichnis:** nur dokumentierbar korrekt, weil
   Publish-Verzeichnis = `WorkingDirectory` = `/var/www/rezepte` und
   `ReadWritePaths`/`chown` das gesamte Verzeichnis freigeben; die Anleitung
   nennt das ausdrücklich, damit niemand das Verzeichnislayout ändert, ohne
   die Konsequenz zu kennen.
3. **`X-Forwarded-Host`-Falle:** App wertet ihn nicht aus — Caddy reicht den
   `Host`-Header per Default durch; im Hinweisblock für andere Proxys ist das
   explizit benannt (sonst falsche absolute URLs/Redirects).
4. **Secret in der Unit-Datei:** `Jwt__Key` in `/etc/systemd/system/rezepte.service`
   ist per Default lesbar → `sudo chmod 640` + `sudo chown root:rezepte`
   dokumentieren; `EnvironmentFile=`-Alternative kurz erwähnen.
5. **Same-Host-Bedingung:** `KnownProxies` = Loopback ist verifiziert; die
   Doku muss verhindern, dass Leser den Proxy auf einem anderen Host
   betreiben (dann wären `X-Forwarded-*` wirkungslos und fälschbar).
6. **SignalR/WebSocket:** Caddy handhabt Upgrades automatisch — kein
   Konfigurationsrisiko, aber im Hinweisblock für andere Proxys als Regel
   benannt (sonst fällt Blazor auf Long-Polling zurück/bricht).
7. **Docs-only triggert volle CI:** `pr-staging-ci.yml` hat keine
   Pfadfilter; der PR-Lauf dauert entsprechend — kein Doku-spezifisches
   Gate, aber `build-and-test` enthält die `DeploymentDocumentationTests`.
8. **Kein automatisierter Link-Check/Markdown-Lint:** Fehler in Links und
   Formatierung fallen nur in Review/manueller Prüfung auf → manuelle
   Linkprüfung ist Teil der Umsetzung, nicht optional.
