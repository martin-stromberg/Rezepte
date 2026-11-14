# Status-Konstanten — Bestandsaufnahme

Hinweis: Beide „Enums" sind als `static class` mit `public const string`-Werten implementiert (keine `enum`-Typen). Laut Anforderung voraussichtlich unverändert — `Missing`, `InstallFailed` und `ReloadFailed` decken die geforderten Zustände ab.

## `PluginStatus`

Datei: `Rezepte.Web/Services/Import/Plugins/PluginStatus.cs`

| Wert | Bedeutung |
|------|-----------|
| `Loaded` | Plugin erkannt und ladbar |
| `Missing` | Zuvor konfiguriertes Plugin wurde bei der Discovery nicht gefunden (`SynchronizeSettingsAsync`, `PluginManager.cs:442`) |
| `Incompatible` | Assembly ohne gültiges `IImportPlugin` / stabile Id / `IImportHandler`-Handler-Typ |
| `LoadFailed` | Assembly/Plugin-Typ konnte nicht geladen oder instanziiert werden |
| `RuntimeFailed` | Handler-Instanziierung zur Laufzeit fehlgeschlagen (`MarkRuntimeFailureAsync`, `PluginManager.cs:471`) |

## `PluginSourceReleaseStatus`

Datei: `Rezepte.Web/Services/Import/Plugins/PluginSourceReleaseStatus.cs`

| Wert | Bedeutung |
|------|-----------|
| `Pending` | Release-Datensatz angelegt, Verarbeitung steht aus |
| `Downloading` | Asset-Download läuft |
| `DownloadFailed` | Download fehlgeschlagen |
| `RateLimited` | GitHub-Rate-Limit (`GitHubRateLimitException`) |
| `Validating` | Paketvalidierung läuft |
| `ValidationFailed` | Paketvalidierung fehlgeschlagen |
| `Installing` | Dateiaustausch läuft |
| `InstallFailed` | Austausch vor Abschluss der Datei-Ersetzung fehlgeschlagen (`replacementCompleted == false` in `PluginPackageInstaller`) |
| `Reloading` | Reinitialisierung des `PluginManager` läuft |
| `ReloadFailed` | Dateiaustausch abgeschlossen, aber Reinitialisierung fehlgeschlagen |
| `Installed` | Installation inkl. Reload erfolgreich |
| `Skipped` | Übersprungen (definiert; aktuell kein Setzpfad in `PluginUpdateService` gefunden — Reservestatus) |
