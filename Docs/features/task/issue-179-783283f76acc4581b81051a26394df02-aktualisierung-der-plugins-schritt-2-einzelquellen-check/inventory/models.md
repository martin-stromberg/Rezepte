# Datenmodell / DTOs — Bestandsaufnahme (Schritt 2: Einzelquellen-Check)

Vollständige Modell-Liste des Plugin-Bereichs: [Projekt-inventory/models.md](../../../../projects/task/issue-179-783283f76acc4581b81051a26394df02-aktualisierung-der-plugins/inventory/models.md). Kein Schema-Umbau erforderlich (Scope-Ausschluss der Anforderung) — hier nur die für den Einzelquellen-Check relevanten Typen.

## `PluginSource`

Datei: `Rezepte.Web/Entities/PluginSource.cs`

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `Id` | `string` | PK, `Guid.NewGuid().ToString("N")`; Adressierung der Einzelquelle per `FindAsync`/`FirstOrDefault`. |
| `RepositoryUrl` | `string` | Kanonische GitHub-URL; wird für `new GitHubRepository(Owner, Repository, RepositoryUrl)` verwendet. |
| `Owner` / `Repository` | `string` | GitHub-Koordinaten. |
| `IsPrivate` | `bool` | Steuert PAT-Auflösung in `ProcessSourceAsync`. |
| `Enabled` | `bool` (default `true`) | Vorbedingung — im Start-Lauf `Where`-Filter; für Einzelcheck Ablehnungskriterium. |
| `TrustConfirmed` | `bool` | Vorbedingung — dto. |
| `SecretName` | `string?` | Schlüssel für `ISystemSecretStore.GetAsync`. |
| `LastSuccessfulReleaseTag` | `string?` | Wird bei erfolgreicher Installation in `DownloadValidateInstallAsync` gesetzt. |
| `LastError` / `LastErrorAt` | `string?` / `DateTime?` | Fehlerfortschreibung via `MarkSourceFailureAsync`; zu Check-Beginn zurückgesetzt. |
| `LastCheckedAt` | `DateTime?` | Wird zu Beginn von `ProcessSourceAsync` auf `DateTime.UtcNow` gesetzt. |
| `CreatedAt` / `UpdatedAt` | `DateTime` | Metadaten. |
| `Releases` | `List<PluginSourceRelease>` | Navigation. |

## `PluginSourceRelease`

Datei: `Rezepte.Web/Entities/PluginSourceRelease.cs`

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `Id` | `string` | PK (`Guid` als `N`-String). |
| `PluginSourceId` / `PluginSource` | `string` / `PluginSource?` | FK/Navigation; Lookup in `ProcessSourceAsync` über `(PluginSourceId, ReleaseTag, AssetId)`. |
| `ReleaseTag` | `string` | Release-Tag des GitHub-Releases. |
| `GitHubReleaseId` | `long` | GitHub-Release-Id. |
| `AssetId` / `AssetName` | `long` / `string` | ZIP-Asset-Identifikation. |
| `Status` | `string` (default `Pending`) | Werte aus `PluginSourceReleaseStatus` (siehe [enums.md](enums.md)). |
| `Error` | `string?` | Letzte Fehlermeldung; wird bei Retry-Reset gelöscht. |
| `CreatedAt` | `DateTime` | Anlagezeitpunkt. |
| `DownloadedAt` / `ValidatedAt` / `InstalledAt` | `DateTime?` | Phasen-Zeitstempel. |
| `ReloadStatus` / `ReloadedAt` / `ReloadError` | `string?` / `DateTime?` / `string?` | Getrennte Reload-Nachverfolgung (seit Schritt 1). |

## DTOs / Records im Check-Pfad

| Record | Datei | Member | Rolle für Schritt 2 |
|--------|-------|--------|---------------------|
| `GitHubRepository` | `Rezepte.Web/Services/Import/Plugins/GitHubRepository.cs` | `(string Owner, string Repository, string CanonicalUrl)`; `Parse`/`TryParse` | Wird in `ProcessSourceAsync` aus den Quellfeldern gebaut (kein `Parse`-Aufruf dort). |
| `GitHubReleaseInfo` | `GitHubReleaseModels.cs` | `(long Id, string TagName, IReadOnlyList<GitHubReleaseAsset> Assets)` + `FindZipAsset()` | `FindZipAsset` bevorzugt `release.zip`, sonst erstes `.zip` nach Name (Z. 25–32). |
| `GitHubReleaseAsset` | `GitHubReleaseModels.cs` | `(long Id, string Name, string DownloadUrl)` | — |
| `PluginSourceSettingsItem` | `PluginSourceSettingsItem.cs` | `(Id, RepositoryUrl, Owner, Repository, IsPrivate, Enabled, TrustConfirmed, HasSecret, LastSuccessfulReleaseTag, LastError, LastCheckedAt, LastErrorAt)` | Sichtbarkeit der Check-Ergebnisse über `GetSourcesAsync` bereits abgedeckt. |
| `PluginPackageInstallOptions` | `PluginPackageInstallOptions.cs` | `(string ReleaseTag, string? SourceKey)` | `SourceKey` = `PluginSource.Id`; steuert `MarkDroppedSourceEntries`. |
| `PluginPackageValidationResult` | `PluginPackageValidationResult.cs` | `Success`, `Error`, `ExtractedRoot`, `PluginDirectories`, … | Ergebnis der Paketvalidierung (unverändert). |

## Ergebnis-DTO (fehlt — Referenzmuster)

Es existiert **kein** Ergebnistyp für den Plugin-Check. Referenzmuster im Bestand:

`ApplicationUpdateCommandResult` — `Rezepte.Web/Services/Updates/ApplicationUpdateSettingsService.cs:316–339`:
`sealed record(string Outcome, string State, string? Message, string? Error)` mit abgeleitetem `IsSuccess` (`Error is null && Outcome is not Failed`) und statischer Factory `FromResult(AutoUpdateResult)` inkl. Lokalisierungshelfern (`ApplicationUpdateText`).

Für `PluginUpdateCheckResult` (Vorschlag in der Anforderung: Outcome-Angabe + optionales `ReleaseTag` + optionales `Error`) gibt es noch keinen Bestand; die Outcome-Ausprägungen könnten wie `PluginSourceReleaseStatus` als String-Konstanten oder als Enum abgebildet werden (Umsetzungsentscheidung).
