# Datenmodell — Bestandsaufnahme

## `PluginSourceRelease`

Datei: `Rezepte.Web/Entities/PluginSourceRelease.cs`

Release-Historie je `PluginSource`; trägt den für die Versionsverzeichnis-Ableitung benötigten `ReleaseTag`.

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `Id` | `string` | GUID (`"N"`-Format), Primärschlüssel |
| `PluginSourceId` | `string` | Fremdschlüssel auf `PluginSource` |
| `PluginSource` | `PluginSource?` | Navigation |
| `ReleaseTag` | `string` | GitHub-Release-Tag (z. B. `v0.3.3`) — **Quelle für die Versionsverzeichnis-Ableitung**, wird in `PluginUpdateService.DownloadValidateInstallAsync` über `record` erreicht, aktuell aber nicht an den Installer weitergereicht |
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

## `PluginSource`

Datei: `Rezepte.Web/Entities/PluginSource.cs`

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `Id` | `string` | GUID (`"N"`-Format), Primärschlüssel |
| `RepositoryUrl` | `string` | Kanonische GitHub-URL |
| `Owner` | `string` | GitHub-Owner |
| `Repository` | `string` | Repository-Name |
| `IsPrivate` | `bool` | Privates Repository → PAT aus `ISystemSecretStore` |
| `Enabled` | `bool` | Vorbedingung für den Update-Check |
| `TrustConfirmed` | `bool` | Vorbedingung für den Update-Check |
| `SecretName` | `string?` | Schlüsselname des PAT im `ISystemSecretStore` |
| `LastSuccessfulReleaseTag` | `string?` | Letzter erfolgreich installierter Release-Tag |
| `LastError` / `LastErrorAt` | `string?` / `DateTime?` | Letzter Fehler der Quelle |
| `LastCheckedAt` | `DateTime?` | Zeitpunkt der letzten Prüfung |
| `CreatedAt` / `UpdatedAt` | `DateTime` | Zeitstempel (UTC) |
| `Releases` | `List<PluginSourceRelease>` | Navigation zur Release-Historie |

## `PluginSetting`

Datei: `Rezepte.Web/Entities/PluginSetting.cs`

Verwaltungseintrag je erkanntem Plugin (`PluginId` ist Primärschlüssel); erhält über `PluginManager.SynchronizeSettingsAsync` den Status `Missing`, wenn ein Plugin nicht mehr entdeckt wird.

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
| `DiscoveredAt` / `LastSeenAt` | `DateTime` | Erst-/Letzterkennung (UTC) |

## `ImportPluginDescriptor`

Datei: `Rezepte.Web/Services/Import/Plugins/ImportPluginDescriptor.cs`

Laufzeit-Deskriptor (sealed record) eines entdeckten Plugins: `Id`, `DisplayName`, `Description`, `Version`, `AssemblyName`, `TypeName`, `HandlerType` (`Type?`, `null` bei collectible Discovery), `DefaultPriority`, `Status`, `Error`, `PluginType` (`Type?`). Enthält **keine** Information über das Herkunftsverzeichnis — eine versionsbewusste Auswahl müsste den Pfad/die Version anderweitig berücksichtigen.

## `PluginPackageValidationResult`

Datei: `Rezepte.Web/Services/Import/Plugins/PluginPackageValidationResult.cs`

Ergebnis der Paketvalidierung (sealed record): `Success`, `Error`, `ExtractedRoot`, `PluginDirectories` (`IReadOnlyList<string>` — die vom `PluginPackageInstaller` zu installierenden Quellverzeichnisse), `DiscoveredPlugins`; Factory `Failed(error, extractedRoot)`.

## `GitHubReleaseInfo` / `GitHubReleaseAsset`

Datei: `Rezepte.Web/Services/Import/Plugins/GitHubReleaseModels.cs`

- `GitHubReleaseInfo(long Id, string TagName, IReadOnlyList<GitHubReleaseAsset> Assets)` mit `FindZipAsset()` — liefert `TagName`, aus dem `PluginSourceRelease.ReleaseTag` gespeist wird.
- `GitHubReleaseAsset(long Id, string Name, string DownloadUrl)`.

## `PluginImportHandler` / `PluginHandlerLease`

Dateien: `Rezepte.Web/Services/Import/Plugins/PluginImportHandler.cs`, `PluginHandlerLease.cs`

- `PluginImportHandler(ImportPluginDescriptor Plugin, IImportHandler Handler)` — record für aktive Handler.
- `PluginHandlerLease` — `IAsyncDisposable` mit `Handlers` und `releaser`; hält `_lifecycleLock` des `PluginManager` bis `DisposeAsync`.

## Dateisystem-„Modell" (kein Code-Artefakt)

Es existiert aktuell **kein** Verwaltungsmodell für Plugin-Verzeichnisse: weder ein Versionsverzeichnis-Namensschema noch eine Löschmarkierung (Marker-Datei, Umbenennung, Löschverzeichnis) noch eine Unterscheidung Publish- vs. quelleninstallierte Verzeichnisse. `plugins/` enthält flache Plugin-Verzeichnisse; DLLs können auch direkt unter `plugins/` liegen (`DiscoverExternalPlugins`, Top-Level-`*.dll`).
