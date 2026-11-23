# Installationsanweisungen

Diese Anleitung beschreibt das manuelle Deployment der Anwendung auf einem Linux-Server mit systemd. Sie führt von einem leeren Server bis zur laufenden Anwendung und deckt zwei alternative Betriebsarten ab:

- **Pfad A — Eigenständiger Betrieb (ohne Reverse-Proxy):** Die Anwendung lauscht per HTTP auf einem eigenen Port und ist direkt erreichbar.
- **Pfad B — Betrieb hinter einem Reverse-Proxy (Caddy):** Ein Reverse-Proxy terminiert TLS und leitet Anfragen unter dem Pfad-Präfix `/rezepte` an die Anwendung weiter.

Die beiden Pfade sind Alternativen — wähle genau einen. Alle Schritte, die beide Betriebsarten gemeinsam haben, stehen in den [Gemeinsamen Schritten](#gemeinsame-schritte); danach folgst du entweder [Pfad A](#pfad-a-eigenständiger-betrieb-ohne-reverse-proxy) oder [Pfad B](#pfad-b-betrieb-hinter-einem-reverse-proxy-caddy).

Die Anleitung verwendet **Ubuntu Server 24.04 LTS auf x86_64** als Referenzplattform; die Anwendung zielt auf `net10.0` und wird typischerweise für `linux-x64` veröffentlicht. Debian 12 und neuer funktionieren mit angepassten Paketbefehlen: Dort ist für .NET 10 der Microsoft-Feed (`packages.microsoft.com/config/debian/12`) einzurichten, die Paketquelle für Caddy wird analog der Caddy-Dokumentation hinterlegt, und die Firewall-Beispiele (`ufw`) sind ggf. durch das dort verwendete Werkzeug zu ersetzen.

In dieser Anleitung werden folgende Vorgabewerte verwendet; passe sie bei Bedarf einheitlich an:

| Vorgabe | Wert |
|---|---|
| Deployment-Verzeichnis | `/var/www/rezepte` |
| Service-Account (Benutzer/Gruppe) | `rezepte` |
| Port der Anwendung | `5000` |
| Pfad-Präfix (nur Pfad B) | `/rezepte` |
| Domain (nur Pfad B, Platzhalter) | `rezepte.example.com` |
| systemd-Unit | `rezepte.service` |

## Gemeinsame Schritte

### 1. Runtime auf dem Server prüfen

Bei einem framework-abhängigen Publish (`--self-contained false`) müssen die passenden .NET-10-Shared-Frameworks auf dem Zielserver installiert sein. Auf Ubuntu 24.04 liegt .NET 10 im eingebauten Ubuntu-Feed — ein separater Microsoft-Feed ist nicht nötig (und für Ubuntu auch nicht verfügbar). Installiere die ASP.NET-Core-Runtime auf dem Server:

```bash
sudo apt update
sudo apt install aspnetcore-runtime-10.0
```

Auf einem Server genügt die ASP.NET-Core-Runtime; wer das volle SDK benötigt, kann stattdessen das Metapaket `dotnet10` installieren.

Prüfe die installierte Runtime vor dem Ausrollen:

```bash
dotnet --info
```

In der Ausgabe müssen beide Shared Frameworks in einer passenden .NET-10-Version vorhanden sein:

- `Microsoft.NETCore.App`
- `Microsoft.AspNetCore.App`

Framework-Assemblies wie `System.Runtime.Serialization.Primitives.dll` müssen bei einem framework-abhängigen Publish nicht im Publish-Verzeichnis liegen. Sie werden aus dem installierten .NET-Shared-Framework des Servers geladen. Fehlt dort eine passende .NET-10-Runtime, kann die Anwendung beim Rendern von Blazor-Komponenten mit `System.IO.FileNotFoundException` abbrechen.

Bei einem self-contained Publish (`--self-contained true`) ist keine .NET-Runtime auf dem Server erforderlich — Schritt 1 kann dann übersprungen werden.

### 2. Publish erzeugen

Erzeuge das Publish auf einer Maschine mit .NET-10-SDK (z. B. deinem Entwicklungsrechner oder der Build-Umgebung).

Verwende framework-abhängiges Deployment, wenn der Server die passenden .NET-10-Shared-Frameworks verlässlich bereitstellt:

```powershell
dotnet publish Rezepte.Web -c Release -f net10.0 -r linux-x64 --self-contained false
```

Wenn die Server-Runtime nicht kontrollierbar, nicht aktualisierbar oder unklar ist, verwende stattdessen ein self-contained Publish:

```powershell
dotnet publish Rezepte.Web -c Release -f net10.0 -r linux-x64 --self-contained true
```

### 3. Dateien auf den Server übertragen

Der Inhalt des Publish-Verzeichnisses muss nach `/var/www/rezepte` auf den Server. Da `/var/www/rezepte` root gehört, kopierst du das Publish zuerst ins Home-Verzeichnis deines SSH-Benutzers und überträgst es dann mit `sudo` an den Zielort.

Auf der Publish-Maschine:

```bash
scp -r <publish-ordner>/. <user>@<server>:~/publish
```

Das `/.` am Ende des Quellpfads kopiert den **Inhalt** des Publish-Ordners nach `~/publish` — wichtig, falls `~/publish` bereits existiert (z. B. von einem früheren Deployment): Ohne `/.` würde `scp -r` ein verschachteltes Unterverzeichnis `~/publish/<publish-ordner>` anlegen. Räume das Verzeichnis bei Bedarf vorher auf dem Server auf (`rm -rf ~/publish`), damit keine Dateien eines älteren Stands mitkopiert werden.

Alternativ mit rsync: `rsync -a <publish-ordner>/ <user>@<server>:~/publish`.

Dann auf dem Server:

```bash
sudo mkdir -p /var/www/rezepte
sudo cp -r ~/publish/. /var/www/rezepte/
```

Ein direktes `scp`/`rsync` nach `/var/www/rezepte` scheitert an fehlenden Schreibrechten. Alternativ kannst du `rsync --rsync-path="sudo rsync"` direkt ins Zielverzeichnis verwenden oder das noch leere Zielverzeichnis vorübergehend deinem SSH-Benutzer übereignen (`sudo chown`).

### 4. Dedizierten Service-Account anlegen und Rechte setzen

Die Anwendung läuft unter einem dedizierten System-Account `rezepte` — nicht als root und nicht unter einem generischen Webserver-Account. Lege den Account an und übereigne ihm das Deployment-Verzeichnis:

```bash
sudo useradd --system --home /var/www/rezepte --shell /usr/sbin/nologin rezepte
sudo chown -R rezepte:rezepte /var/www/rezepte
```

Der Account braucht Schreibrechte auf das Deployment-Verzeichnis: Die SQLite-Datenbank `rezepte.db`, das Log-Verzeichnis `logs/` sowie die Verzeichnisse `update-backups/`, `exports/` und `plugins/` liegen unterhalb von `/var/www/rezepte` (Daten und Logs relativ zum `WorkingDirectory`, Logs am Assembly-Verzeichnis — beides ist bei dieser Struktur identisch). Rezeptbilder werden in der SQLite-Datenbank abgelegt; es gibt kein separates Upload-Verzeichnis.

### 5. JWT-Signierschlüssel erzeugen

Die Anwendung benötigt in Production einen JWT-Signierschlüssel; ohne ihn schlägt der Start mit einer `InvalidOperationException` fehl. Erzeuge einen zufälligen Schlüssel auf dem Server:

```bash
openssl rand -base64 48
```

Trage den erzeugten Wert als `Environment=Jwt__Key=<Wert>` in die systemd-Unit ein (siehe die Unit-Dateien in Pfad A bzw. Pfad B). Der Schlüssel muss mindestens 32 Zeichen lang sein — die Ausgabe von `openssl rand -base64 48` erfüllt das.

Alternativ können Secrets aus einer separaten, nicht eingecheckten Datei bezogen werden (`EnvironmentFile=` in der Unit, siehe auch `Docs/deployment-guide.md`).

## Pfad A: Eigenständiger Betrieb (ohne Reverse-Proxy)

### A.1 systemd-Service einrichten

Erstelle `/etc/systemd/system/rezepte.service` für ein framework-abhängiges Deployment:

```ini
[Unit]
Description=Rezepte Web Service
After=network.target

[Service]
WorkingDirectory=/var/www/rezepte
ExecStart=/usr/bin/dotnet /var/www/rezepte/Rezepte.Web.dll
Restart=always
RestartSec=10
SyslogIdentifier=rezepte
User=rezepte
Group=rezepte
Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=ASPNETCORE_URLS=http://0.0.0.0:5000
Environment=Jwt__Key=<zufälliger Schlüssel aus Schritt 5>
NoNewPrivileges=true
PrivateTmp=true
ProtectSystem=strict
ProtectHome=true
ReadWritePaths=/var/www/rezepte

[Install]
WantedBy=multi-user.target
```

`ASPNETCORE_URLS=http://0.0.0.0:5000` bindet die Anwendung an Port 5000 auf allen IPv4-Schnittstellen (IPv6-Clients erreichen sie damit nicht; bei Bedarf stattdessen `http://[::]:5000` für IPv6/Dual-Stack verwenden). `WorkingDirectory=/var/www/rezepte` muss erhalten bleiben, damit relative Pfade für Datenbank, Logs und statische Dateien zum Deployment-Verzeichnis passen; `ReadWritePaths=/var/www/rezepte` gewährt dem gehärteten Dienst Schreibzugriff genau dort.

Bei einem self-contained Publish kann alternativ der native Host gestartet werden:

```bash
sudo chmod +x /var/www/rezepte/Rezepte.Web
```

```ini
ExecStart=/var/www/rezepte/Rezepte.Web
```

Die Unit-Datei enthält mit `Jwt__Key` ein Secret und sollte nur für root und den Service-Account lesbar sein:

```bash
sudo chown root:rezepte /etc/systemd/system/rezepte.service
sudo chmod 640 /etc/systemd/system/rezepte.service
```

### A.2 Firewall freigeben

```bash
sudo ufw allow 5000/tcp
```

**Hinweis:** Auf einem frischen Ubuntu-Server ist ufw typischerweise inaktiv — `sudo ufw allow` legt dann nur Regeln an, ohne sie zu aktivieren. Prüfe den Zustand mit `sudo ufw status`. Soll die Firewall aktiviert werden, gib vorher SSH frei (`sudo ufw allow OpenSSH` bzw. `sudo ufw allow 22/tcp`), sonst sperrst du dich bei `sudo ufw enable` aus.

**Einschränkung:** In dieser Betriebsart läuft die Anwendung ausschließlich über HTTP — sie bringt kein eigenes TLS-Setup mit. Zugangsdaten und Inhalte werden unverschlüsselt übertragen. Betreibe diese Variante nur in vertrauenswürdigen Netzen (z. B. LAN/VPN) oder nutze Pfad B mit einem Reverse-Proxy für TLS.

### A.3 Dienst starten und verifizieren

```bash
sudo systemctl daemon-reload
sudo systemctl enable rezepte.service
sudo systemctl start rezepte.service
sudo systemctl status rezepte.service
```

Erwartetes Ergebnis: `active (running)`.

Das Console-Logging der Anwendung ist auf Fehler beschränkt. Zur Fehlersuche:

```bash
sudo journalctl -u rezepte.service
sudo tail /var/www/rezepte/logs/app-*.log
```

Funktionsprüfung:

```bash
curl -I http://localhost:5000
```

Erwartet wird eine Antwort mit HTTP 200 oder 302. Im Browser sollte `http://<server>:5000` die Registrierungs-/Login-Seite der Anwendung zeigen.

## Pfad B: Betrieb hinter einem Reverse-Proxy (Caddy)

In dieser Betriebsart terminiert ein Reverse-Proxy das TLS (automatisch per Let's Encrypt) und leitet Anfragen an die Anwendung weiter. Als Referenz dient Caddy: `reverse_proxy` setzt `X-Forwarded-Proto` und `X-Forwarded-For` automatisch, reicht den `Host`-Header durch und lässt WebSocket-Upgrades für die Blazor-Verbindung ohne Zusatzkonfiguration zu.

### B.1 systemd-Service einrichten

Erstelle `/etc/systemd/system/rezepte.service` — gegenüber Pfad A ändern sich nur die Bind-Adresse (Loopback statt aller Schnittstellen) und das Pfad-Präfix:

```ini
[Unit]
Description=Rezepte Web Service
After=network.target

[Service]
WorkingDirectory=/var/www/rezepte
ExecStart=/usr/bin/dotnet /var/www/rezepte/Rezepte.Web.dll
Restart=always
RestartSec=10
SyslogIdentifier=rezepte
User=rezepte
Group=rezepte
Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=ASPNETCORE_URLS=http://127.0.0.1:5000
Environment=PathBase=/rezepte
Environment=Jwt__Key=<zufälliger Schlüssel aus Schritt 5>
NoNewPrivileges=true
PrivateTmp=true
ProtectSystem=strict
ProtectHome=true
ReadWritePaths=/var/www/rezepte

[Install]
WantedBy=multi-user.target
```

- `ASPNETCORE_URLS=http://127.0.0.1:5000` bindet die Anwendung nur an Loopback — sie ist ausschließlich über den Reverse-Proxy erreichbar. Port 5000 wird daher **nicht** in der Firewall freigegeben.
- `PathBase=/rezepte` lässt die Anwendung unter dem Pfad-Präfix `/rezepte` laufen; die App erwartet dabei den vollen Pfad vom Proxy und entfernt das Präfix selbst — der Proxy darf also **kein** Pfad-Stripping machen.

Bei einem self-contained Publish gilt wie in Pfad A: `chmod +x /var/www/rezepte/Rezepte.Web` und `ExecStart=/var/www/rezepte/Rezepte.Web`. Die Hinweise zum Schutz der Unit-Datei (Secret `Jwt__Key`: `sudo chown root:rezepte` + `sudo chmod 640`) gelten ebenso.

### B.2 Voraussetzungen

- Ein DNS-Record (A bzw. AAAA) für `rezepte.example.com` zeigt auf die öffentliche IP des Servers — sonst kann Caddy kein TLS-Zertifikat ausstellen.
- Der Reverse-Proxy läuft **auf demselben Host** wie die Anwendung: Die App akzeptiert `X-Forwarded-*`-Header nur von Loopback-Adressen (`KnownProxies` = IPv4- und IPv6-Loopback). Läuft der Proxy auf einem anderen Host, werden die Header ignoriert.

### B.3 Caddy installieren

Installiere Caddy über das offizielle apt-Repository:

```bash
sudo apt install -y debian-keyring debian-archive-keyring apt-transport-https curl
curl -1sLf 'https://dl.cloudsmith.io/public/caddy/stable/gpg.key' | sudo gpg --dearmor -o /usr/share/keyrings/caddy-stable-archive-keyring.gpg
curl -1sLf 'https://dl.cloudsmith.io/public/caddy/stable/debian.deb.txt' | sudo tee /etc/apt/sources.list.d/caddy-stable.list
sudo apt update
sudo apt install caddy
```

### B.4 Caddy konfigurieren

Lege `/etc/caddy/Caddyfile` mit folgendem Inhalt an:

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

Erläuterung:

- Der benannte Matcher `@rezepte path /rezepte /rezepte/*` deckt den exakten Pfad `/rezepte` **und** alle Pfade darunter ab. `/rezepte/*` allein würde den exakten Pfad `/rezepte` nicht matchen (er fiele in den 404-Fallback), und `/rezepte*` würde zusätzlich fremde Pfade wie `/rezeptefoo` matchen — beides ist unerwünscht.
- Es wird **kein** Pfad-Stripping konfiguriert (kein `handle_path`): Die Anwendung erwartet den vollen Pfad und entfernt das Präfix selbst über `PathBase=/rezepte`.
- Caddy setzt bei `reverse_proxy` die Header `X-Forwarded-Proto` und `X-Forwarded-For` automatisch und reicht den ursprünglichen `Host`-Header durch — Letzteres ist nötig, weil die Anwendung `X-Forwarded-Host` nicht auswertet und für Redirects den `Host`-Header verwendet.
- `X-Forwarded-For` muss wirken: Ohne die echte Client-IP würde das integrierte Rate-Limiting (Login/Registrierung) alle Clients über die Loopback-Adresse des Proxys gemeinsam behandeln.
- WebSocket-Upgrades (für die interaktive Blazor-Verbindung) erlaubt Caddy ohne Zusatzkonfiguration.
- TLS-Zertifikate stellt Caddy automatisch via Let's Encrypt aus, sobald der DNS-Record stimmt.

Caddy neu laden und die Proxy-Ports in der Firewall freigeben:

```bash
sudo systemctl reload caddy
sudo ufw allow 80/tcp
sudo ufw allow 443/tcp
```

Der Hinweis zu inaktivem ufw auf frischen Servern aus [Abschnitt A.2](#a2-firewall-freigeben) gilt hier ebenso.

Port 5000 bleibt geschlossen — die Anwendung ist nur über Loopback erreichbar.

### B.5 Alternative: Root-Pfad auf eigener Subdomain

Wenn die Anwendung statt eines Pfad-Präfixes die komplette (Sub-)Domain belegen soll, genügt:

```text
rezepte.example.com {
    reverse_proxy 127.0.0.1:5000
}
```

In diesem Fall entfällt das Pfad-Präfix: Die Zeile `Environment=PathBase=/rezepte` wird in der systemd-Unit weggelassen.

### B.6 Hinweis für andere Reverse-Proxys

Mit nginx, Apache, Traefik o. ä. funktioniert dieselbe Betriebsart, sofern der Proxy dieselben Regeln abbildet:

- das Pfad-Präfix `/rezepte` **unverändert** an die Anwendung weiterleiten (kein Stripping — die App entfernt es selbst),
- den originalen `Host`-Header durchreichen (`X-Forwarded-Host` wird von der Anwendung nicht ausgewertet),
- `X-Forwarded-Proto` und `X-Forwarded-For` setzen,
- WebSocket-Upgrades für Blazor/SignalR erlauben,
- auf demselben Host laufen wie die Anwendung (`KnownProxies` = Loopback).

### B.7 Dienst starten und verifizieren

```bash
sudo systemctl daemon-reload
sudo systemctl enable rezepte.service
sudo systemctl start rezepte.service
sudo systemctl status rezepte.service
sudo systemctl status caddy
```

Erwartetes Ergebnis: beide Dienste `active (running)`.

Funktionsprüfung:

```bash
curl -I http://127.0.0.1:5000/rezepte
curl -I https://rezepte.example.com/rezepte
```

Erwartet wird auf Loopback HTTP 200 oder 302 sowie über die Domain eine Antwort mit gültigem TLS (von Caddy automatisch ausgestellt) und HTTP 200 oder 302; bei einem Redirect ist der `Location`-Header relativ (`/rezepte/...`) und löst sich auf `https://rezepte.example.com/rezepte/...` auf. Im Browser sollte `https://rezepte.example.com/rezepte` die Registrierungs-/Login-Seite laden und die Anmeldung funktionieren (die interaktive Blazor-Verbindung per WebSocket steht). Zur Fehlersuche gelten `sudo journalctl -u rezepte.service` und `sudo tail /var/www/rezepte/logs/app-*.log` wie in Pfad A.

## Update und Backup

Bei einem Update der Anwendung sicherst du zuerst die Datenbank bei gestopptem Dienst, spielst dann das neue Publish ein und startest neu:

```bash
sudo systemctl stop rezepte.service
cp /var/www/rezepte/rezepte.db ~/rezepte.db-backup-$(date +%F)
```

Danach das neue Publish wie in Schritt 3 übertragen (`~/publish` vorher auf dem Server bereinigen — dort existiert es in der Regel schon vom letzten Deployment —, dann `sudo cp -r ~/publish/. /var/www/rezepte/`), den Eigentümer korrigieren und den Dienst starten:

```bash
sudo chown -R rezepte:rezepte /var/www/rezepte
sudo systemctl start rezepte.service
```

Datenbank-Migrationen laufen beim Start automatisch. Verifiziere den Erfolg wie in Schritt A.3 bzw. B.7 (`systemctl status`, `curl`).

## Nicht abgedeckt

- **Gleichzeitiger Betrieb beider Pfade** auf einem Server — die Betriebsarten sind Alternativen.
- **TLS direkt in der Anwendung** — die Anwendung bringt kein eigenes TLS-Setup mit; TLS ist ausschließlich Thema des Reverse-Proxys (Pfad B).
- **Automatisiertes Setup** (Skripte, Ansible o. ä.) — diese Anleitung beschreibt das manuelle Deployment.
- **Vollständige Anleitungen für andere Reverse-Proxys** als Caddy — für diese gelten nur die Regeln in Abschnitt B.6.
