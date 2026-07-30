# Bestandsaufnahme - Automatisierte Programmupdates

## Kontext

Die Anforderung beschreibt die Einbindung von `msTools.Updater` in die Webanwendung. Vor jeder Installation einer neuen Web-App-Version muss ein vollstaendiger Datenexport erfolgreich erzeugt und in einem konfigurierbaren Backup-Verzeichnis abgelegt werden. Die Bestandsaufnahme konzentriert sich auf bestehende Start-/DI-Strukturen, Export- und Background-Job-Code, Konfigurationsmuster, Plugin-Updater als Vergleich und die Projektstruktur fuer eine moegliche Einbindung.

## Detaildokumente

- [Program.cs, DI und Hosted Services](inventory/program-di-hosted-services.md)
- [Export, Backup und BackgroundJobs](inventory/export-backup-backgroundjobs.md)
- [Konfiguration, appsettings und Settings-Klassen](inventory/configuration-settings.md)
- [Plugin-Update-Services als Vergleich](inventory/plugin-update-comparison.md)
- [Projekt- und Paketstruktur fuer msTools.Updater](inventory/project-package-structure.md)
- [Risiken und offene technische Punkte](inventory/risks-open-questions.md)

## Kernergebnisse

- `Rezepte.Web/Program.cs` ist bewusst schlank. Die Anwendung ruft `builder.Services.AddRezepteServices(...)` auf; die eigentliche DI- und Hosted-Service-Registrierung liegt in `Rezepte.Web/Extensions/ServiceCollectionExtensions.cs` und `Rezepte.Web/Extensions/JobQueueServiceCollectionExtensions.cs`.
- Es gibt bereits mehrere Hosted Services: `BackgroundJobHostedService`, `PluginStartupService` und `PluginUpdateHostedService`. Ein Web-App-Updater wuerde architektonisch in dieselbe Start-/DI-Schicht passen, muss aber fachlich von Import-Plugin-Updates getrennt bleiben.
- Der vorhandene vollstaendige Export ist `IExportService.ExportAllAsync(adminUserId, includeImages, includePdf, ct)`. Er erzeugt einen ZIP-Stream im Speicher und ist fuer einen synchron wartenden Pre-Install-Hook besser geeignet als der bestehende Background-Job-Weg.
- Der vorhandene Admin-Export als Background Job schreibt Dateien nach `<ContentRoot>/exports`. Dieses Ziel ist nicht konfigurierbar und die Job-Verarbeitung ist asynchron. Fuer die Update-Anforderung braucht es ein konfigurierbares Backup-Ziel und eine harte Erfolg-/Fehler-Rueckmeldung vor Installation.
- Konfigurationswerte werden fuer technische Optionen aktuell als Options-Klassen aus `appsettings.json` gebunden, z. B. `PluginUpdates`, `Images`, `GoogleCredentials`, `LoadingBar`. Fuer Update-Backups passt eine neue Options-Klasse, z. B. `UpdateBackupOptions`, gebunden aus einer neuen Section.
- Zusaetzlich existieren DB-basierte `AppSetting`-Eintraege ueber `SettingsService`. Diese werden fuer UI-/Laufzeit-Schalter genutzt, nicht fuer technische Start-/Pfadkonfiguration. Backup-Pfad und Retention sollten eher ueber `appsettings`/Options laufen, sofern die Anforderung mit `appSettings` die JSON-Konfiguration meint.
- Der Plugin-Updater bietet hilfreiche Vergleichsmuster: `IHostedService` beim Startup, scoped Service-Aufruf, Status-/Fehlerpersistenz, Download/Validate/Install-Phasen, Installationslock, temporare Arbeitsverzeichnisse und Rollback bei Installationsfehlern. Diese Muster sollten nicht direkt wiederverwendet werden, weil sie Import-Plugin-Verzeichnisse und Plugin-Releases betreffen.
- `Rezepte.Web.csproj` targetet `net10.0`, nutzt Paketreferenzen direkt im Projekt und hat bereits MSBuild-Targets fuer Import-Plugin-Build/Copy. Fuer `msTools.Updater` ist zu klaeren, ob ein NuGet-Paket existiert, ob eine Projekt-/Git-Referenz noetig ist oder ob die Komponente als separater Build-Artefakt/Tool eingebunden werden muss.

## Naheliegende Integrationspunkte

- Neue Options-Klasse unter `Rezepte.Web/Configuration`, Registrierung in `AddRezepteServices`.
- Neuer scoped Backup-Service, der `IExportService.ExportAllAsync(...)` aufruft, den Stream in ein konfiguriertes Verzeichnis schreibt, atomar/fehlerbewusst arbeitet und Retention anwendet.
- Updater-Registrierung in `AddRezepteServices`, abhaengig von der API von `msTools.Updater`.
- Pre-Install-Event/Callback des Updaters mit scoped Service-Aufloesung ueber `IServiceScopeFactory`, damit `IExportService` und `RezepteDbContext` korrekt scoped bleiben.
- Logging ueber die bestehende Serilog-/ILogger-Infrastruktur.

## Nicht geloeste Punkte fuer die Planung

- Die konkrete API von `msTools.Updater`, insbesondere Paketname, Ziel-Frameworks, Registrierungs-API und Pre-Install-Event-Signatur, ist noch zu pruefen. Der direkte GitHub-Link aus der Anforderung war in der Websuche nicht belastbar auffindbar; eine NuGet-Suche lieferte keinen eindeutigen Treffer fuer `msTools.Updater`.
- Die Anforderung verlangt einen "vollstaendigen Datenexport". Der vorhandene `ExportAllAsync` exportiert Benutzer, Kochbuecher, Rezepte, Schritte, Zutaten und Bilder, laesst aber einige neuere Felder/Tabellen moeglicherweise aus. Details stehen im Export-Dokument.
- `ExportAllAsync` verlangt aktuell eine `adminUserId`. Fuer ein automatisches System-Backup ohne angemeldeten Benutzer muss entschieden werden, ob ein technischer Initiator erlaubt ist oder ob der Service fachlich von einem Admin-Kontext entkoppelt wird.
