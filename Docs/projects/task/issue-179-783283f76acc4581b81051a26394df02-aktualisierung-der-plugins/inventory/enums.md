# Enums — Bestandsaufnahme

Hinweis: Beide Statusbereiche sind keine `enum`-Typen, sondern statische Klassen mit `const string`-Werten (persistiert als `string`).

## `PluginStatus`

Datei: `Rezepte.Web/Services/Import/Plugins/PluginStatus.cs`

| Wert | Bedeutung |
|------|-----------|
| `Loaded` | Plugin wurde erfolgreich entdeckt und geladen |
| `Missing` | Plugin war konfiguriert, wurde aber bei der Discovery nicht gefunden (`Error = "Plugin was not found during startup discovery."`, gesetzt in `PluginManager.SynchronizeSettingsAsync`) |
| `Incompatible` | Assembly enthält kein gültiges `IImportPlugin` (kein Plugin, keine stabile Id oder ungültiger Handler-Typ) |
| `LoadFailed` | Assembly/Plugintyp konnte nicht geladen bzw. instanziiert werden |
| `RuntimeFailed` | Handler-Instanziierung zur Laufzeit fehlgeschlagen (`PluginManager.MarkRuntimeFailureAsync`) |

## `PluginSourceReleaseStatus`

Datei: `Rezepte.Web/Services/Import/Plugins/PluginSourceReleaseStatus.cs`

| Wert | Bedeutung |
|------|-----------|
| `Pending` | Release-Datensatz angelegt, Verarbeitung steht aus (Default) |
| `Downloading` | Asset-Download läuft |
| `DownloadFailed` | Download fehlgeschlagen |
| `RateLimited` | GitHub-Rate-Limit (`GitHubRateLimitException`) |
| `Validating` | Paketvalidierung läuft |
| `ValidationFailed` | Paketvalidierung fehlgeschlagen (u. a. kein ZIP-Asset) |
| `Installing` | Installation läuft |
| `InstallFailed` | Installation fehlgeschlagen (vor dem Reload-Abschnitt) |
| `Reloading` | Dateiaustausch abgeschlossen, Plugin-Reload läuft |
| `ReloadFailed` | Koordinierter Reload fehlgeschlagen |
| `Installed` | Release erfolgreich installiert und aktiv |
| `Skipped` | Definiert, wird im aktuellen Code nicht gesetzt (bereits `Installed`-Releases werden geloggt übersprungen, ohne den Status auf `Skipped` zu ändern) |
