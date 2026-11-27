# Datenmodell

Datenmodellklassen und Records im Plugin-Update-Pfad (Issue #239: Arbeitsverzeichnisse unter `Path.GetTempPath()`).

## `PluginSource`
Datei: `Rezepte.Web/Entities/PluginSource.cs`

EF-Core-Entität einer konfigurierten Plugin-Quelle. `LastError`/`LastErrorAt` nehmen die im Issue sichtbare Fehlermeldung auf (aktuell die rohe Exception-Message).

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `Id` | `string` | GUID (Format `"N"`), Primärschlüssel; wird als `SourceKey` an den Installer weitergereicht |
| `RepositoryUrl` | `string` | Vom Admin eingegebene Repository-URL |
| `Owner` | `string` | GitHub-Owner (aus der URL abgeleitet) |
| `Repository` | `string` | Repository-Name |
| `IsPrivate` | `bool` | Private Quelle → PAT aus `ISystemSecretStore` nötig |
| `Enabled` | `bool` | Quelle aktiv (Vorbedingung der Update-Prüfung) |
| `TrustConfirmed` | `bool` | Quelle vom Admin als vertrauenswürdig bestätigt (Vorbedingung) |
| `SecretName` | `string?` | Name des hinterlegten PAT-Secrets |
| `LastSuccessfulReleaseTag` | `string?` | Zuletzt erfolgreich installiertes Release |
| `LastError` | `string?` | Letzter Fehlertext — wird in der Statusspalte und über `MapCheckError` angezeigt |
| `LastCheckedAt` | `DateTime?` | Zeitpunkt der letzten Prüfung |
| `LastErrorAt` | `DateTime?` | Zeitpunkt des letzten Fehlers |
| `CreatedAt` | `DateTime` | Anlagezeitpunkt |
| `UpdatedAt` | `DateTime` | Änderungszeitpunkt |
| `Releases` | `List<PluginSourceRelease>` | Navigationsliste der bekannten Releases dieser Quelle |

## `PluginSourceRelease`
Datei: `Rezepte.Web/Entities/PluginSourceRelease.cs`

EF-Core-Entität eines bekannten Releases einer Quelle; führt den Phasenstatus der Update-Verarbeitung.

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `Id` | `string` | GUID (Format `"N"`), Primärschlüssel |
| `PluginSourceId` | `string` | Fremdschlüssel auf `PluginSource` |
| `PluginSource` | `PluginSource?` | Navigation |
| `ReleaseTag` | `string` | GitHub-Release-Tag |
| `GitHubReleaseId` | `long` | GitHub-Release-ID |
| `AssetId` | `long` | GitHub-Asset-ID (zusammen mit `ReleaseTag` deduplizierend) |
| `AssetName` | `string` | Asset-Dateiname (wird als ZIP-Dateiname im Work-Root verwendet) |
| `Status` | `string` | Phasenstatus, Werte aus `PluginSourceReleaseStatus` (Default `Pending`) |
| `Error` | `string?` | Fehlertext der Phase (aktuell rohe `ex.Message`) |
| `CreatedAt` | `DateTime` | Anlagezeitpunkt |
| `DownloadedAt` | `DateTime?` | Download abgeschlossen |
| `ValidatedAt` | `DateTime?` | Validierung abgeschlossen |
| `InstalledAt` | `DateTime?` | Installation abgeschlossen |
| `ReloadStatus` | `string?` | Separater Reload-Status |
| `ReloadedAt` | `DateTime?` | Reload abgeschlossen |
| `ReloadError` | `string?` | Separater Reload-Fehlertext |

## `PluginUpdateCheckResult`
Datei: `Rezepte.Web/Services/Import/Plugins/PluginUpdateCheckResult.cs`

Record-Ergebnis der manuellen Update-Prüfung; wird von `PluginSettings.razor` ausgewertet.

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `Outcome` | `string` | Wert aus `PluginUpdateCheckOutcome` (`NoUpdate`/`Installed`/`Failed`) |
| `ReleaseTag` | `string?` | Betroffenes Release |
| `Error` | `string?` | Fehlertext bei `Failed` — bekannte Konstante aus `PluginUpdateCheckErrors` oder freie (Exception-)Message |
| `AlreadyInstalled` | `bool` | Release war bereits installiert |
| `IsSuccess` | `bool` | `Outcome != Failed` |
| `NoUpdate(releaseTag, alreadyInstalled)` | statisch | Factory für Erfolg ohne Installation |
| `Installed(releaseTag)` | statisch | Factory für erfolgreiche Installation |
| `Failed(error, releaseTag)` | statisch | Factory für Fehlschlag — nimmt aktuell auch `ex.Message` unverändert auf |

## `PluginPackageValidationResult`
Datei: `Rezepte.Web/Services/Import/Plugins/PluginPackageValidationResult.cs`

Record-Ergebnis der Paketvalidierung.

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `Success` | `bool` | Validierung erfolgreich |
| `Error` | `string?` | Validierungsfehler (fachlich formulierte Strings) |
| `ExtractedRoot` | `string` | Extraktionsverzeichnis (`{Temp}/rezepte-plugin-package/<guid>`); wird von `PluginUpdateService` im `finally` aufgeräumt |
| `PluginDirectories` | `IReadOnlyList<string>` | Plugin-Verzeichnisse mit DLLs — Eingabe für den Installer |
| `DiscoveredPlugins` | `IReadOnlyList<ImportPluginDescriptor>` | Bei Discovery geladene Plugin-Deskriptoren |
| `Failed(error, extractedRoot)` | statisch | Factory für Fehlschlag |

## `PluginUpdateOptions`
Datei: `Rezepte.Web/Configuration/PluginUpdateOptions.cs`

Konfiguration der Section `PluginUpdates` (gebunden in `ServiceCollectionExtensions`, Zeile 44; Defaults in `appsettings.json`, Zeilen 20–24). Enthält **keinen** Schlüssel für ein Arbeits-/Basisverzeichnis.

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `GitHubApiBaseUrl` | `string` | GitHub-API-Basis (Default `https://api.github.com`; in Browser-Tests auf Stub umgebogen) |
| `TimeoutSeconds` | `int` | HttpClient-Timeout (Default 30) |
| `UserAgent` | `string` | User-Agent für GitHub-Requests |

## `PluginPackageInstallOptions`
Datei: `Rezepte.Web/Services/Import/Plugins/PluginPackageInstallOptions.cs`

Record-Optionen für `IPluginPackageInstaller`.

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `ReleaseTag` | `string` | Release-Tag, aus dem `CreateVersionedDirectoryName` die Zielverzeichnisnamen ableitet |
| `SourceKey` | `string?` | `PluginSource.Id` der zugehörigen Quelle (steuert `MarkDroppedSourceEntries`) |

## `PluginPackageInstallException`
Datei: `Rezepte.Web/Services/Import/Plugins/PluginPackageInstallException.cs`

Exception mit fachlichem Status; `PluginUpdateService` mappt sie auf `PluginSourceReleaseStatus`.

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `Status` | `string` | `InstallFailed` oder `ReloadFailed` (aus `PluginSourceReleaseStatus`) |

## `PluginInstallState` / `PluginInstallEntry`
Datei: `Rezepte.Web/Services/Import/Plugins/PluginInstallState.cs`

Persistenter Install-State in `{ContentRootPath}/plugins/.install-state.json` (verwaltet von `PluginDirectoryRegistry`).

`PluginInstallState`:

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `Version` | `int` | Schema-Version (Default 1) |
| `Installs` | `List<PluginInstallEntry>` | Registrierte effektive Installationen je Basisname |
| `PendingDeletes` | `List<string>` | Zum Löschen markierte Verzeichnisnamen |

`PluginInstallEntry`:

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `BaseName` | `string` | Plugin-Basisname (ohne Versionssuffix) |
| `DirectoryName` | `string` | Effektives versioniertes Verzeichnis `<BaseName>@<ReleaseTag>` |
| `ReleaseTag` | `string` | Release-Tag der installierten Version |
| `SourceKey` | `string?` | Zugehörige Plugin-Quelle |

## `PluginSourceSettingsItem`
Datei: `Rezepte.Web/Services/Import/Plugins/PluginSourceSettingsItem.cs`

Record-DTO der Quellenliste für `PluginSettings.razor` (enthält u. a. `LastError`, `LastCheckedAt`, `LastErrorAt` für die Statusspalte).

## `GitHubReleaseAsset` / `GitHubReleaseInfo`
Datei: `Rezepte.Web/Services/Import/Plugins/GitHubReleaseModels.cs`

Records des GitHub-Release-Modells. `GitHubReleaseInfo.FindZipAsset()` wählt das ZIP-Asset (bevorzugt `release.zip`); `GitHubReleaseAsset.Name` wird als Dateiname unter dem Download-Work-Root verwendet.
