# Plan-Review: Automatischer Download neuer Plugins

## Status

Offene Aufgaben vorhanden

Die aktuelle Implementierung enthält die wesentlichen Bausteine des Plans: globale Quellen und Releasepersistenz, EF-Core-Migration, GitHub-Client, temporäre ZIP-Prüfung, Installer mit Backup, einmaligen Hosted-Service, PAT-Secret-Storage und eine administrative Quellenverwaltung. Der Plan ist jedoch noch nicht vollständig umgesetzt.

## Offene Aufgaben

### 1. Serverseitige Administrator-Autorisierung vervollständigen

`PluginSettingsService.EnsureAdmin` lässt die Ausführung zu, wenn kein `HttpContext` vorhanden ist (`Rezepte.Web/Services/Import/Plugins/PluginSettingsService.cs:181-193`). Damit können die Methoden zur Quellenverwaltung außerhalb eines HTTP-Kontexts ohne Administratornachweis aufgerufen werden. Der Plan verlangt eine serverseitig maßgebliche Admin-Prüfung; die UI-Ausblendung darf nicht die Sicherheitsgrenze sein. Ein fehlender Kontext muss daher abgewiesen oder durch einen verbindlichen, serverseitigen Autorisierungsmechanismus ersetzt werden.

### 2. Plugin-Lebenszyklus beim Reload koordinieren

`PluginPackageInstaller` ruft nach dem Dateiaustausch direkt `PluginManager.InitializeAsync` auf (`Rezepte.Web/Services/Import/Plugins/PluginPackageInstaller.cs:38`). Es gibt keine Koordination mit laufenden Import-Handlern und keine Behandlung der bereits geladenen, nicht sammelnden `AssemblyLoadContext`s (`Rezepte.Web/Services/Import/Plugins/PluginManager.cs:310-327`). Damit ist die im Plan geforderte kontrollierte Ablösung bzw. Wiederverwendung des bestehenden Plugin-Lebenszyklus nicht umgesetzt. Die temporäre Discovery lädt zudem bereits Assemblies aus dem Arbeitsverzeichnis, bevor diese installiert werden.

### 3. Reload- und Rollbackzustände nachvollziehbar persistieren

`PluginSourceRelease` kennt nur Download-, Validierungs- und Installationszeitpunkte (`Rezepte.Web/Entities/PluginSourceRelease.cs:15-26`). Ein eigener Reloadstatus bzw. Reloadfehler fehlt. Der Installer kann beim Reload bereits PluginSettings verändern, bevor ein Fehler den Dateibestand zurückspielt; die anschließende erneute Initialisierung wird nicht als eigener Zustand persistiert. Dadurch ist der im Plan geforderte nachvollziehbare Zwischen- und Fehlerzustand für Austausch und Reload nicht vollständig vorhanden.

### 4. GitHub-Fehlerbehandlung um Rate-Limit und kontrollierte Wiederholung ergänzen

`GitHubReleaseClient` behandelt außer `404` alle HTTP-Fehler nur über `EnsureSuccessStatusCode` (`Rezepte.Web/Services/Import/Plugins/GitHubReleaseClient.cs:13-43`). Die geplante kontrollierte Behandlung von Rate-Limits, API-Fehlern und Wiederholungen ist damit nicht umgesetzt. Es gibt weder eine erkennbare Rate-Limit-Auswertung noch Retry-/Backoff-Logik oder einen dedizierten persistierten Status dafür. Timeouts und Cancellation werden durch `HttpClient` grundsätzlich unterstützt.

### 5. Abnahmetests aus Umsetzungsschritt 7 ergänzen

Die vorhandenen neuen Tests decken nur Quellenkanonisierung/Secretablage, Admin-Verhalten, variable ZIP-Namen, einen ZIP-Pfadüberlauf, Mehrfachverzeichnisse sowie deaktivierte und bereits fehlgeschlagene Releases ab. Es fehlen die im Plan ausdrücklich genannten Tests für:

- GitHub-Authentifizierung öffentlich/privat, Releaseauswahl, Rate-Limit, Timeout, Cancellation und PAT-Ausschluss aus Requests/Logs;
- absolute Pfade, unerlaubte Inhalte und beschädigte ZIPs;
- Discovery ohne Seiteneffekte auf aktiven Bestand und PluginSettings;
- Installation, Überschreiben, Rollback, Reloadfehler und Nicht-Markierung eines fehlgeschlagenen Releases als erfolgreich;
- einmalige Startup-Ausführung, Cancellation, parallele Läufe und Statusübergänge;
- Administratorgrenzen und Secret-Ausschluss in UI-State und Logs als vollständiger Integrationspfad.

## Planpunkte mit vorhandener Umsetzung

- `PluginSource`/`PluginSourceRelease`, Indizes, `RezepteDbContext` und Migration sind vorhanden.
- GitHub-Repository-URLs werden auf HTTPS und `github.com` begrenzt und kanonisiert; der Release-Tag und variable ZIP-Assetnamen werden verwendet.
- Temporäre Arbeitsverzeichnisse, ZIP-Pfadprüfungen und Assembly-Discovery sind vorhanden; der aktive Bestand wird vor der Validierung nicht ersetzt.
- Backup, Dateiaustausch, Installationssperre und Wiederherstellungsversuch sind implementiert.
- Der Updateprozess ist als scoped Dienst registriert und wird durch einen einmaligen Hosted-Service nach dem bestehenden Startup-Service gestartet. Ein Timer und ein manueller Update-Trigger sind nicht vorhanden.
- PAT-Werte werden an einen Data-Protection-basierten Secret-Store übergeben und nicht in `PluginSourceSettingsItem` abgebildet. Die lokale Pluginaktivierung und Reihenfolge bleiben bestehen.

## Verifikation

Ausgeführt:

`dotnet test Rezepte.Tests/Rezepte.Tests.csproj --no-restore --filter 'FullyQualifiedName~PluginSettingsServiceTests|FullyQualifiedName~PluginPackageValidatorTests|FullyQualifiedName~PluginSourceReleaseTests|FullyQualifiedName~PluginUpdateServiceTests'`

Ergebnis: 11 Tests bestanden, 0 fehlgeschlagen. Es wurde eine bestehende `NU1903`-Warnung zur bekannten Sicherheitslücke in `SQLitePCLRaw.lib.e_sqlite3` ausgegeben.
