# Bestandsaufnahme: Installationsdokumentation mit/ohne Reverse-Proxy (Issue #214)

Analysiert wurden `Docs/install.md`, die Verlinkungsstruktur (`Docs/index.md`,
`Docs/deployment-guide.md`, `README.md`), das verifizierte Laufzeitverhalten in
`Rezepte.Web` (Program.cs, appsettings, persistente Pfade) sowie die
Build-/Test-/CI-Checks — bezogen auf die Anforderung „zwei alternative
Installationspfade (eigenständig / hinter Reverse-Proxy)" aus `requirement.md`.

## Zusammenfassung

- **`install.md` deckt nur „ohne Proxy" ab und hat eine verifizierte
  Funktionslücke:** Die dokumentierte systemd-Unit setzt
  `ASPNETCORE_ENVIRONMENT=Production`, erwähnt aber `Jwt:Key` nicht. Ohne einen
  konfigurierten Schlüssel (≥ 32 Zeichen) wirft `JwtSigningKeyProvider` beim
  Start eine `InvalidOperationException` — die Anleitung führt heute **nicht**
  zu einer laufenden Anwendung (`Security/JwtSigningKeyProvider.cs:51–58`,
  `appsettings.json` liefert `"Key": ""`).
- **Weitere verifizierte Lücken:** keine Bind-Adresse/Port (`Urls` fehlt in
  allen `appsettings*.json`; einzige Fundstelle ist `launchSettings.json`
  `http://localhost:5220`), keine Firewall-Hinweise, keine echten
  Verifikationsschritte, `User=www-data` ohne Hardening, kein Reverse-Proxy-Pfad.
- **Proxy-Verhalten im Code verifiziert:** `PathBase` via `UsePathBase()`
  (Program.cs:76–80, No-op wenn ungesetzt); `UseForwardedHeaders` wertet nur
  `X-Forwarded-Proto` + `X-Forwarded-For` aus und vertraut ausschließlich
  `KnownProxies` = IPv4/IPv6-Loopback (Program.cs:87–93) → **Same-Host-Pflicht
  des Proxys**; `X-Forwarded-Host` wird nicht ausgewertet, `ForwardLimit` ist
  nicht gesetzt (Default = 1).
- **Persistenz-Schreibrechte differenziert sich gegenüber der Anforderung:**
  `logs/` liegt am `AppContext.BaseDirectory` (Assembly-Verzeichnis,
  LoggingExtensions.cs:25), `rezepte.db`, `update-backups/`, `exports/` und
  `plugins/` am ContentRoot (= `WorkingDirectory` der Unit). **Rezeptbilder
  liegen als `byte[]` in SQLite** (RecipeImage.cs:33) — es gibt kein
  Upload-Verzeichnis im Dateisystem; der Anforderungspunkt
  „Upload-Verzeichnisse" trifft so nicht zu.
- **Test-Kopplung an `install.md` existiert:**
  `Rezepte.Tests/Deployment/DeploymentDocumentationTests.cs` assertiert
  Literal-Strings (`dotnet --info`, `--self-contained false/true`,
  `chmod +x /var/www/rezepte/Rezepte.Web`, u. a.; `Rezepte.dll` als
  NotContain). Diese Strings müssen bei jeder Umstrukturierung erhalten
  bleiben; gleiches gilt für vier Literale in `README.md`.
- **Keine Beispiel-Deployment-Dateien und keine Doku-Checks im CI:** kein
  `.service`/`*.conf`/`Caddyfile`/`.env` im Repo; kein Markdown-Lint-Gate;
  alle PR-Jobs laufen ohne Pfadfilter (Docs-only-Änderung triggert die
  volle .NET-Pipeline).
- **Verlinkung insgesamt konsistent, eine Stelle muss mitgepflegt werden:**
  `deployment-guide.md:64` nennt explizit `User=www-data` „aus
  `Docs/install.md`" — veraltet, sobald ein dedizierter Account dokumentiert
  wird.

## Betroffene Artefakte

| Artefakt | Art der Berührung |
|---|---|
| `Docs/install.md` | Hauptarbeit: Zwei-Pfad-Umbau, Lücken schließen (siehe Detaildokumente) |
| `Docs/index.md` | Einzeiler-Linktext ggf. an neue Zwei-Pfad-Struktur anpassen |
| `Docs/deployment-guide.md` | Nur Zeile 64 (`User=www-data`-Verweis) synchron halten; inhaltlich nicht vermischen |
| `README.md` | Deployment-Abschnitt (Z. 72–76) beschreibt nur den alten Umfang; Verlinkung prüfen |
| `Rezepte.Tests/Deployment/DeploymentDocumentationTests.cs` | Nur als Constraint — Assertions müssen grün bleiben, keine Teständerung geplant |
| `Rezepte.Web/Program.cs`, `appsettings*.json` | Reine Referenz; **kein Code im Scope** |

## Risiken

1. **Testbruch bei Umformulierung:** Entfernen/Umbenennen der assertierten
   Literale in `install.md`/`README.md` lässt zwei deterministische Tests
   fehlschlagen (Details: [pruefungen-und-ci.md](inventory/pruefungen-und-ci.md)).
2. **Unvollständige Anleitung bleibt unvollständig:** Ohne `Jwt:Key`-Schritt
   startet die App unter `ASPNETCORE_ENVIRONMENT=Production` nicht — das
   Akzeptanzkriterium „führt zu einer funktionierenden Installation" wäre
   ohne diesen Schritt nicht erfüllt.
3. **`logs/` vs. `WorkingDirectory`:** Wer Schreibrechte nur auf das
   WorkingDirectory beschreibt, dokumentiert am Code vorbei — `logs/` hängt
   am Assembly-Verzeichnis (Details: [laufzeitverhalten.md](inventory/laufzeitverhalten.md)).
4. **`X-Forwarded-Host`-Falle:** Proxys, die den Host-Header nicht
   durchreichen, erzeugen falsche absolute URLs — die App wertet den
   Forwarded-Host-Header nicht aus; die Proxy-Beispielkonfiguration muss den
   originalen Host weiterleiten.
5. **Standalone = HTTP-only:** Die App bringt kein eigenes TLS mit
   (`UseHsts()` greift nur auf HTTPS-Responses); im Standalone-Pfad ist das
   als dokumentierte Einschränkung zu benennen (offene Frage 5).

## Offene Fragen (aus `requirement.md` übernommen, durch Befunde geschärft)

1. **Zielplattform:** Distribution(en) konkret benennen (implizit
   „Linux mit systemd"; Beispiel-Publish ist `linux-x64`).
2. **Reverse-Proxy-Produkt:** Caddy/nginx/Apache/produktneutral — kein
   Beispielmaterial im Repo vorhanden, frei wählbar.
3. **Härtungsumfang:** dedizierter Account verbindlich vs. Empfehlung;
   Hardening-Direktiven Umfang (`ProtectSystem`, `NoNewPrivileges`,
   `PrivateTmp`, `ProtectHome`, `ReadWritePaths` für `logs/`+Datenpfade).
4. **Proxy-Modus-Umfang:** nur `PathBase`-Präfix oder auch Root-Pfad auf
   Subdomain (Code unterstützt beides; `PathBase` ungesetzt = Root).
5. **Exposition Standalone:** `0.0.0.0` + Firewall vs. LAN — TLS-Einschränkung
   dokumentieren.
6. **Vorgabewerte:** Beispiel-Port und `PathBase`-Beispielwert
   (copy-paste-fähig).
7. **Umfang:** nur Erstinstallation oder auch Update-Pfad (Publish über
   Bestandsinstallation, `rezepte.db`-Backup).

## Details

- [Ist-Zustand `Docs/install.md` und Doku-Konventionen/Verlinkung](inventory/install-md-ist-zustand.md)
- [Verifiziertes Laufzeitverhalten: Program.cs, Konfiguration, persistente Pfade](inventory/laufzeitverhalten.md)
- [Prüfungen: DeploymentDocumentationTests, CI-Gates, sinnvolle lokale Checks](inventory/pruefungen-und-ci.md)
