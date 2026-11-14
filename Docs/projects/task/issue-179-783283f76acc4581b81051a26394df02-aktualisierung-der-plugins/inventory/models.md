# Datenmodell — Bestandsaufnahme

## `PluginSource`

Datei: `Rezepte.Web/Entities/PluginSource.cs`

Entity für eine GitHub-Pluginquelle. Alle für die Transparenz-Anzeige benötigten Statusfelder existieren bereits.

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `Id` | `string` | GUID (`"N"`-Format), Primärschlüssel |
| `RepositoryUrl` | `string` | Kanonische GitHub-URL (`https://github.com/{owner}/{repo}`) |
| `Owner` | `string` | GitHub-Owner, für GitHub-API-Aufrufe |
| `Repository` | `string` | Repository-Name |
| `IsPrivate` | `bool` | Privates Repository → PAT aus `ISystemSecretStore` nötig |
| `Enabled` | `bool` | Vorbedingung für den Update-Check (`true` default) |
| `TrustConfirmed` | `bool` | Vorbedingung für den Update-Check |
| `SecretName` | `string?` | Schlüsselname des PAT im `ISystemSecretStore` |
| `LastSuccessfulReleaseTag` | `string?` | Letzter erfolgreich installierter Release-Tag; wird in `PluginUpdateService` nach erfolgreicher Installation gesetzt |
| `LastError` | `string?` | Letzte Fehlermeldung der Quelle |
| `LastCheckedAt` | `DateTime?` | Zeitpunkt der letzten Prüfung; wird zu Beginn von `ProcessSourceAsync` gesetzt |
| `LastErrorAt` | `DateTime?` | Zeitpunkt des letzten Fehlers |
| `CreatedAt` | `DateTime` | Anlagezeitpunkt (UTC) |
| `UpdatedAt` | `DateTime` | Letzte Änderung über `PluginSettingsService` (UTC) |
| `Releases` | `List<PluginSourceRelease>` | Navigation zur Release-Historie |

## `PluginSourceRelease`

Datei: `Rezepte.Web/Entities/PluginSourceRelease.cs`

Release-Historie je Quelle; dokumentiert Download-, Validierungs-, Installations- und Reload-Ergebnis.

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `Id` | `string` | GUID (`"N"`-Format), Primärschlüssel |
| `PluginSourceId` | `string` | Fremdschlüssel auf `PluginSource` |
| `PluginSource` | `PluginSource?` | Navigation |
| `ReleaseTag` | `string` | GitHub-Release-Tag (z. B. `v0.3.3`) |
| `GitHubReleaseId` | `long` | GitHub-Release-ID |
| `AssetId` | `long` | GitHub-Asset-ID des verwendeten ZIP |
| `AssetName` | `string` | Dateiname des Assets |
| `Status` | `string` | Wert aus `PluginSourceReleaseStatus` (default `Pending`) |
| `Error` | `string?` | Fehlermeldung des Release-Datensatzes |
| `CreatedAt` | `DateTime` | Anlagezeitpunkt (UTC) |
| `DownloadedAt` | `DateTime?` | Zeitpunkt des erfolgreichen Downloads |
| `ValidatedAt` | `DateTime?` | Zeitpunkt der erfolgreichen Paketvalidierung |
| `InstalledAt` | `DateTime?` | Zeitpunkt der erfolgreichen Installation |
| `ReloadStatus` | `string?` | Separater Reload-Status (Wert aus `PluginSourceReleaseStatus`) |
| `ReloadedAt` | `DateTime?` | Zeitpunkt des erfolgreichen Reloads |
| `ReloadError` | `string?` | Reload-Fehlermeldung |

## `PluginSetting`

Datei: `Rezepte.Web/Entities/PluginSetting.cs`

Verwaltungseintrag je erkanntem Plugin (`PluginId` ist Primärschlüssel). Ziel der neuen „Entfernen"-Aktion für `Missing`-Plugins.

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `PluginId` | `string` | Stabile Plugin-ID aus `IImportPlugin.Id` bzw. synthetische Fehler-ID (`{status}:{assembly}`) |
| `DisplayName` | `string` | Anzeigename |
| `Description` | `string?` | Beschreibung |
| `AssemblyName` | `string` | Assembly-Name des Plugins |
| `TypeName` | `string` | Vollqualifizierter Handler-Typname |
| `Enabled` | `bool` | Aktivierungsschalter des Anwenders |
| `OrderIndex` | `int` | Reihenfolge in der Pluginliste |
| `Status` | `string` | Wert aus `PluginStatus` (default `Loaded`) |
| `Error` | `string?` | Fehlermeldung (u. a. `"Plugin was not found during startup discovery."` bei `Missing`) |
| `DiscoveredAt` | `DateTime` | Erst-Erkennung (UTC) |
| `LastSeenAt` | `DateTime` | Letzte erfolgreiche Discovery (UTC) |

Hinweis: `PluginSettings`, `PluginSources` und `PluginSourceReleases` sind Teil des Systemexports (`Rezepte.Web/Services/ExportService.cs`, Zeilen ~459–500); ein wiederhergestelltes Backup kann entfernte Einträge zurückbringen.

## `PluginSourceSettingsItem`

Datei: `Rezepte.Web/Services/Import/Plugins/PluginSourceSettingsItem.cs`

DTO (sealed record) für die Quellenliste in der Admin-UI; enthält bereits alle Statusfelder für die Transparenz-Anzeige:

`Id`, `RepositoryUrl`, `Owner`, `Repository`, `IsPrivate`, `Enabled`, `TrustConfirmed`, `HasSecret`, `LastSuccessfulReleaseTag`, `LastError`, `LastCheckedAt`, `LastErrorAt`.

## `PluginSettingsItem`

Datei: `Rezepte.Web/Services/Import/Plugins/PluginSettingsItem.cs`

DTO (sealed record) für die Pluginliste in der Admin-UI:

`PluginId`, `DisplayName`, `Description`, `AssemblyName`, `TypeName`, `Enabled`, `OrderIndex`, `Status`, `Error`, `DiscoveredAt`, `LastSeenAt`, `Usability` (`PluginUsabilityResult?`, optional).

## `PluginSourceSaveRequest`

Datei: `Rezepte.Web/Services/Import/Plugins/PluginSourceSaveRequest.cs`

DTO (sealed record) für das Quellen-Formular: `Id`, `RepositoryUrl`, `IsPrivate`, `Enabled`, `TrustConfirmed`, `PersonalAccessToken`.

## `ImportPluginDescriptor`

Datei: `Rezepte.Web/Services/Import/Plugins/ImportPluginDescriptor.cs`

Laufzeit-Deskriptor (sealed record) eines entdeckten Plugins: `Id`, `DisplayName`, `Description`, `Version`, `AssemblyName`, `TypeName`, `HandlerType` (`Type?`, `null` bei collectible Discovery), `DefaultPriority`, `Status`, `Error`, `PluginType` (`Type?`).

## `GitHubReleaseInfo` / `GitHubReleaseAsset`

Datei: `Rezepte.Web/Services/Import/Plugins/GitHubReleaseModels.cs`

- `GitHubReleaseInfo(long Id, string TagName, IReadOnlyList<GitHubReleaseAsset> Assets)` mit Hilfsmethode `FindZipAsset()` — bevorzugt `release.zip`, sonst alphabetisch erstes `*.zip`.
- `GitHubReleaseAsset(long Id, string Name, string DownloadUrl)`.

## `PluginPackageValidationResult`

Datei: `Rezepte.Web/Services/Import/Plugins/PluginPackageValidationResult.cs`

Ergebnis der Paketvalidierung: `Success`, `Error`, `ExtractedRoot`, `PluginDirectories` (die vom `PluginPackageInstaller` zu installierenden Verzeichnisse), `DiscoveredPlugins`; Factory `Failed(error, extractedRoot)`.

## `PluginUpdateOptions`

Datei: `Rezepte.Web/Configuration/PluginUpdateOptions.cs`

Konfigurationsklasse (Abschnitt `PluginUpdates`, gebunden in `ServiceCollectionExtensions.cs:44`): `GitHubApiBaseUrl` (default `https://api.github.com`), `TimeoutSeconds` (default `30`), `UserAgent` (default `Rezepte.PluginUpdater`). Kein Prüfintervall konfigurierbar.
