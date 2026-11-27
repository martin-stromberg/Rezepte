# Enums und Konstanten

Klassische C#-Enums gibt es im Update-Pfad nicht; Status- und Fehlerwerte sind als `const string`-Klassen modelliert.

## `PluginUpdateCheckErrors`
Datei: `Rezepte.Web/Services/Import/Plugins/PluginUpdateCheckErrors.cs`

Bekannte fachliche Fehlertexte der Update-Prüfung. `PluginSettings.razor` (`MapCheckError`) mappt sie auf lokalisierte `UiStrings`-Schlüssel; jeder andere String fällt in den generischen „Prüfung fehlgeschlagen. (…)"-Fallback.

| Wert | Bedeutung | Lokalisierungsschlüssel (`UiStrings.resx`) |
|------|-----------|----------------------------------------------|
| `SourceNotFound` = `"Plugin source was not found."` | Quell-ID unbekannt | `PluginCheckSourceNotFound` |
| `SourceDisabled` = `"Plugin source is disabled."` | Quelle deaktiviert | `PluginCheckSourceDisabled` |
| `SourceNotTrusted` = `"Plugin source is not confirmed as trusted."` | Vertrauen nicht bestätigt | `PluginCheckSourceNotTrusted` |
| `PrivateSourceNoPat` = `"Private source has no stored PAT."` | Private Quelle ohne hinterlegten PAT | `PluginCheckNoPat` |
| `NoPublishedRelease` = `"No published GitHub release was found."` | Kein Release gefunden | `PluginCheckNoRelease` |
| `NoZipAsset` = `"Latest release has no ZIP asset."` | Release ohne ZIP-Asset | `PluginCheckNoZipAsset` |
| `ValidationFailed` = `"Plugin package validation failed."` | Paketvalidierung fehlgeschlagen (generisch) | `PluginCheckValidationFailed` |
| `RateLimited` = `"GitHub rate limit reached."` | GitHub-Rate-Limit | `PluginCheckRateLimited` |

**Befund:** Es existiert **keine** Konstante für „Arbeitsverzeichnis nicht verfügbar/anlegbar". Ein `Path.GetTempPath()`-Fehler kommt als freie Exception-Message durch und wird nicht lokalisiert dargestellt.

## `PluginUpdateCheckOutcome`
Datei: `Rezepte.Web/Services/Import/Plugins/PluginUpdateCheckOutcome.cs`

Outcome-Werte von `PluginUpdateCheckResult`.

| Wert | Bedeutung |
|------|-----------|
| `NoUpdate` = `"NoUpdate"` | Kein (neues) Update installiert |
| `Installed` = `"Installed"` | Release wurde installiert |
| `Failed` = `"Failed"` | Prüfung fehlgeschlagen |

## `PluginSourceReleaseStatus`
Datei: `Rezepte.Web/Services/Import/Plugins/PluginSourceReleaseStatus.cs`

Phasenstatus eines `PluginSourceRelease`-Datensatzes (und `ReloadStatus`).

| Wert | Bedeutung |
|------|-----------|
| `Pending` = `"Pending"` | Datensatz angelegt, Verarbeitung steht aus |
| `Downloading` = `"Downloading"` | Asset-Download läuft |
| `DownloadFailed` = `"DownloadFailed"` | Download fehlgeschlagen |
| `RateLimited` = `"RateLimited"` | GitHub-Rate-Limit erreicht |
| `Validating` = `"Validating"` | Paketvalidierung läuft |
| `ValidationFailed` = `"ValidationFailed"` | Validierung fehlgeschlagen |
| `Installing` = `"Installing"` | Installation läuft |
| `InstallFailed` = `"InstallFailed"` | Installation fehlgeschlagen |
| `Reloading` = `"Reloading"` | Plugin-Reload läuft |
| `ReloadFailed` = `"ReloadFailed"` | Reload fehlgeschlagen |
| `Installed` = `"Installed"` | Erfolgreich installiert |
| `Skipped` = `"Skipped"` | Übersprungen (derzeit im Update-Pfad nicht gesetzt) |
