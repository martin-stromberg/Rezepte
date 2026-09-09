# Konfiguration

Die wichtigsten Einstellungen liegen in `Rezepte.Web/appsettings.json` und können wie üblich per User Secrets, Umgebungsvariablen oder Deployment-Konfiguration überschrieben werden.

## Übersicht der Einstellungen

| Einstellung | Bedeutung |
|-------------|-----------|
| `ConnectionStrings:Default` | SQLite-Connection-String. Fallback: `Data Source=rezepte.db`. |
| `Jwt:Key` | Signaturschlüssel für API-Tokens. Außerhalb der Entwicklungsumgebung zwingend erforderlich (mindestens 32 Zeichen); ohne gültigen Wert startet die Anwendung nicht. In der Entwicklungsumgebung wird ohne Konfiguration ein Zufallsschlüssel pro Prozess erzeugt. |
| `Jwt:Issuer`, `Jwt:Audience`, `Jwt:LifetimeMinutes` | JWT-Basiskonfiguration. Issuer und Audience werden zum Ausstellen und Validieren der Tokens verwendet (Standard: `rezepte` bzw. `rezepte.api`). |
| `RateLimiting:Authentication:PermitLimit`, `RateLimiting:Authentication:WindowSeconds` | Grenzwerte der Ratenbegrenzung für Login und Registrierung pro Client-IP (Standard: 10 Anfragen pro 60 Sekunden). |
| `Images:MaxSizeBytes` | Maximale Upload-Grösse für Bilder. |
| `Images:AllowedContentTypes` | Erlaubte Bildformate. |
| `AI:Simulate`, `AI:EnableCache`, `AI:CacheDurationHours` | Optionale Einstellungen für KI-Importe und Caching. |
| `GoogleCredentials:ServiceAccountFilePath`, `GoogleCredentials:GeminiApiKey` | Fallback für Google-Credentials, wenn die Umgebungsvariablen `GOOGLE_APPLICATION_CREDENTIALS` bzw. `GOOGLE_GEMINI_API_KEY` nicht gesetzt sind. |
| `PluginUpdates:GitHubApiBaseUrl`, `PluginUpdates:TimeoutSeconds`, `PluginUpdates:UserAgent` | Serverseitige GitHub-Kommunikation für die Startprüfung konfigurierter Pluginquellen. |
| `UpdateBackups:Directory`, `UpdateBackups:RetentionCount` | Zielverzeichnis und Aufbewahrungsanzahl für automatische Backups vor Programmupdates. |
| `UpdateBackups:IncludeImages`, `UpdateBackups:IncludePdf`, `UpdateBackups:SystemInitiatorUserId` | Umfang und technischer Initiator der Update-Backups. |
| `ApplicationUpdates:*` | Steuerung der eingebundenen `msTools.Updater`-Programmupdates, inklusive Quelle, Downloadpfad, automatischem Download und automatischer Installation. |
| `ApplicationUpdates:ServiceName` | Windows: Name des Dienstes, der während der Installation gestoppt und neu gestartet wird. |
| `ApplicationUpdates:ExecutablePath` | Windows: Pfad zur Executable, falls kein Dienst verwendet wird. |
| `ApplicationUpdates:AppPoolName` | Windows (ab `msTools.Updater` `0.10.0`): Name des IIS-Application-Pools, der gestoppt und neu gestartet wird. |
| `ApplicationUpdates:SiteName` | Windows (ab `msTools.Updater` `0.10.0`): Optionale IIS-Site für das Logging bei `AppPoolName`. |
| `ApplicationUpdates:UpdateUnitName` | Linux: Name der systemd-Unit, über die das Installationsskript ausgeführt wird. |
| `ApplicationUpdates:StopHostAfterScriptStart` | Linux: Muss `true` sein, damit der Hostprozess stoppt und das Skript Dateien ersetzen kann. |
| `LoadingBar:Enabled` | Aktiviert oder deaktiviert den Ladebalken bei Navigation global (Standard: `true`). |
| `LoadingBar:Height` | Höhe des Ladebalkens als CSS-Länge, z. B. `"3px"` oder `"0.25rem"` (Standard: `"3px"`). |
| `LoadingBar:AnimationDuration` | Dauer eines vollständigen Sweeps von rechts nach links als CSS-Zeit, z. B. `"2s"` (Standard: `"2s"`). |
| `LoadingBar:HideDelay` | Wartezeit nach Navigationsabschluss bis zum Ausblenden des Balkens, z. B. `"300ms"` (Standard: `"300ms"`). |
| `LoadingBar:MaxVisibleDuration` | Sicherheitsgrenze, nach der der Balken auch ohne Abschlusssignal ausgeblendet wird, z. B. `"15s"` (Standard: `"15s"`). |
| `LoadingBar:Colors` | Liste von Hexfarben in der Form `["#RGB", "#RRGGBB", ...]`, aus denen pro Navigationsinteraktion eine zufällige Farbe gewählt wird. |
| `SecurityTxt.Enabled` | Schaltet die security.txt-Auslieferung ein (`true`) oder aus (`false`). Standard: `false`. |
| `SecurityTxt.Contact` | RFC-9116-Direktive `Contact` — URI oder E-Mail, ein Wert pro Zeile. Pflichtfeld bei `Enabled = true`. |
| `SecurityTxt.Expires` | RFC-9116-Direktive `Expires` — Ablaufzeitpunkt als ISO-8601-Datum. Pflichtfeld bei `Enabled = true`; muss in der Zukunft liegen. |
| `SecurityTxt.Encryption` | RFC-9116-Direktive `Encryption` — URL zum öffentlichen Schlüssel. Optional. |
| `SecurityTxt.Acknowledgments` | RFC-9116-Direktive `Acknowledgments` — URL zur Danksagungsseite. Optional. |
| `SecurityTxt.PreferredLanguages` | RFC-9116-Direktive `Preferred-Languages` — kommagetrennte Sprachcodes. Optional. |
| `SecurityTxt.Policy` | RFC-9116-Direktive `Policy` — URL zur Sicherheitsrichtlinie. Optional. |
| `SecurityTxt.Hiring` | RFC-9116-Direktive `Hiring` — URL zu Sicherheitsstellen-Ausschreibungen. Optional. |

Hinweis: `Canonical` ist nicht admin-konfigurierbar. Die Direktive wird vom Server je Ausgabeformat (Plain-Text/Markdown/HTML) automatisch aus Request-Schema, Host, PathBase und Zielpfad erzeugt.

## Besondere Konfigurationsbereiche

### Ladebalken

Bei der Navigation (Klicks auf Navigationslinks oder Absenden von Formularen) wird ein schmaler, farbiger Ladebalken unterhalb der Navigationsleiste angezeigt. Der Balken erscheint sofort bei Benutzerinteraktionen, nutzt eine zufällig gewählte Farbe aus der konfigurierten Farbpalette und animiert sich mit einer linearen Bewegung von rechts nach links (Sweep-Effekt). Er wird ausgeblendet, sobald die Navigation abgeschlossen ist oder ein Sicherheits-Timeout ausläuft.

Das Feature ist standardmässig aktiviert (`LoadingBar:Enabled: true`), kann aber global deaktiviert werden. Bei aktiviertem `prefers-reduced-motion` (Systemeinstellung) wird die Bewegung durch einen statischen, farbigen Balken ersetzt. Details zu allen `LoadingBar:*`-Parametern, Beispielen und der Problembehandlung stehen in [Docs/help/loading-bar-configuration.md](help/loading-bar-configuration.md).

### Programmupdates und Update-Backups

Die Anwendung bindet `msTools.Updater` als externe Update-Komponente ein. Administratoren sehen den Update-Status in den Einstellungen und können dort Prüfung, Download und Installation auslösen. Vor einer Installation erstellt das `BeforeInstall`-Event ein Update-Backup im konfigurierten Zielverzeichnis und wendet die konfigurierte Aufbewahrungsanzahl an. Schlägt das Backup fehl, wird die Installation abgebrochen. Details stehen in [Docs/help/application-updates.md](help/application-updates.md).

### Google-Credentials

Die Google-Credentials werden nicht als Datei im Projekt abgelegt und nicht in Build-Ausgaben kopiert. Sie werden zur Laufzeit bevorzugt über die Umgebungsvariablen `GOOGLE_GEMINI_API_KEY` und `GOOGLE_APPLICATION_CREDENTIALS` oder alternativ über die Konfigurationssektion `GoogleCredentials` bereitgestellt. URL-basierte KI-Importe können allein mit Gemini-API-Key arbeiten; Fotoimporte benötigen für Google Vision eine lesbare Service-Account-Datei und zusätzlich Gemini-Authentifizierung. Details zur lokalen Einrichtung stehen in [Docs/development-guide.md](development-guide.md), Details zum Produktionsbetrieb in [Docs/deployment-guide.md](deployment-guide.md).
