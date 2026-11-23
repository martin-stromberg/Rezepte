# Ist-Zustand `Docs/install.md` und Doku-Konventionen

## `Docs/install.md` — bestehender Umfang (77 Zeilen)

Gliederung (Stand Branch-Head, verifiziert):

| Abschnitt | Zeilen | Inhalt |
|---|---|---|
| Einleitung | 1–3 | „Linux-Server mit systemd", `net10.0`, typischerweise `linux-x64` |
| Runtime auf dem Server prüfen | 5–18 | `dotnet --info`; beide Shared Frameworks `Microsoft.NETCore.App` + `Microsoft.AspNetCore.App` in .NET-10-Version; Begründung `System.Runtime.Serialization.Primitives.dll` / `FileNotFoundException` bei Blazor |
| Publish erzeugen | 20–34 | `dotnet publish Rezepte.Web -c Release -f net10.0 -r linux-x64 --self-contained false` bzw. `true`; Kopieren nach `/var/www/rezepte` |
| systemd-Service einrichten | 36–68 | Unit-Beispiel `rezepte.service`: `WorkingDirectory=/var/www/rezepte`, `ExecStart=/usr/bin/dotnet /var/www/rezepte/Rezepte.Web.dll`, `Restart=always`, `RestartSec=10`, `SyslogIdentifier=rezepte-api`, `User=www-data`, `Environment=ASPNETCORE_ENVIRONMENT=Production`; self-contained-Variante mit `chmod +x` + nativem Host; Hinweis, dass `WorkingDirectory` für relative Pfade (DB, Logs, statische Dateien) erhalten bleiben muss |
| Dienst installieren und starten | 70–77 | `daemon-reload`, `enable`, `start`, `status` |

## Verifizierte Lücken (gegen Akzeptanzkriterien)

1. **`Jwt:Key` fehlt komplett** — die Unit setzt `ASPNETCORE_ENVIRONMENT=Production`,
   ohne Schlüssel (≥ 32 Zeichen, `Jwt__Key`-Env) bricht der Start ab
   (siehe `laufzeitverhalten.md`). Blocker für „führt zu funktionierender Installation".
2. **Kein `ASPNETCORE_URLS`/Bind-Adresse/Port** — die App bindet per Default auf
   `http://localhost:5000` (ASP.NET-Core-Default); nicht dokumentiert, nicht in
   `appsettings*.json` gesetzt.
3. **Keine Firewall-/Freigabe-Hinweise** (ufw/firewalld o. ä. nicht erwähnt).
4. **Keine Verifikationsschritte** über `systemctl status` hinaus — `journalctl`
   und `curl`-Smoke-Test fehlen. Relevant: Console-Logging ist auf
   `Error` beschränkt (LoggingExtensions.cs:37) → `journalctl` zeigt nur Fehler;
   Detaildiagnose läuft über `logs/app-*.log`.
5. **Kein Schreibrechte-Abschnitt** — `User=www-data` muss auf `rezepte.db`,
   `logs/`, `update-backups/`, `exports/`, `plugins/` schreiben können;
   nicht dokumentiert.
6. **Kein Reverse-Proxy-Pfad** — `PathBase`, `X-Forwarded-*`, Loopback-Bind,
   Same-Host-Bedingung fehlen vollständig.
7. **Kein Service-Account-Konzept** — `User=www-data` (geteilter Account),
   keine Hardening-Direktiven, kein `Group=`.
8. **Keine Update-/Re-Deploy-Schritte** (nur Erstinstallation; offene Frage 7).

## Stil und Konventionen der Datei

- Sprache: Deutsch, „du"-Form („Prüfe", „Kopiere", „Erstelle").
- Codeblöcke mit Sprach-Tag: `bash` (Server-Kommandos), `powershell`
  (Publish-Kommandos — Windows-Entwicklungsmaschine als Publish-Quelle
  implizit), `ini` (systemd-Unit).
- Befehle mit `sudo`-Präfix für Systemaktionen.
- Gemischte Umlaut-Schreibung: überwiegend echte Umlaute (`für`,
  `müssen`), aber vereinzelt Transkription (`verlaesslich` Z. 22,
  `Framework-Assemblies`). Git-History zeigt zwei Commits, die
  ae/oe/ue-Transkriptionen rückwirkend in Umlaute überführt haben
  (`245eb89`, `befd34f`) — Zielstil ist **echte Umlaute**.

## Verlinkungsstruktur

| Datei | Zeile(n) | Verweis auf `install.md` |
|---|---|---|
| `Docs/index.md` | 12 | „Installationsanleitung — manuelles Deployment auf Linux mit systemd" |
| `Docs/index.md` | 13 | deployment-guide.md (Google-Credentials/Production) |
| `README.md` | 74–76 | Deployment-Abschnitt: „verbindliche Schritt-für-Schritt-Anleitung für Publish, Runtime-Prüfung und systemd-Betrieb"; framework-abhängig vs. self-contained |
| `README.md` | 87 | Linkliste „Installationsanleitung" |
| `Docs/deployment-guide.md` | 14, 40 | verweist ergänzend auf die Unit in `install.md` |
| `Docs/deployment-guide.md` | 64 | nennt `User=www-data` „aus `Docs/install.md`" — **veraltet bei dediziertem Account, synchron zu halten** |

## `Docs/RELEASE_NOTES.md`

Template mit Platzhaltern („no special notices", „no user-facing changes",
deutsch+englisch). Kein Bezug zu `install.md`; bei der Doku-Änderung
kein Eingriff nötig (Release-Notes werden separat gepflegt).

## Beispiel-Deployment-Dateien / CI-Doku-Prüfungen

- **Existieren nicht:** `find` über das Repo liefert keine `*.service`,
  `*.conf`, `Caddyfile`, `nginx*`- oder `.env`-Dateien. Die systemd-Unit
  existiert ausschließlich als eingebetteter Codeblock in `install.md`.
- **Kein Markdown-Lint/Link-Check** in `.github/workflows/*` — einzige
  Doku-bezogene Prüfung ist der Unit-Test
  `DeploymentDocumentationTests` (siehe `pruefungen-und-ci.md`).
- `Docs/github-pages.md`/`site/` betreffen nur die Präsentationsseite
  (eigenständige `site/index.html`), ohne Bezug zur Installationsdoku.
