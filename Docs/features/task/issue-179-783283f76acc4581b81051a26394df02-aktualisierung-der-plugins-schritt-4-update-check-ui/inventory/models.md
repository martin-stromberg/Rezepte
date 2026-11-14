# Datenmodelle und Ergebnistypen

## `PluginSourceSettingsItem` (Record)

Datei: `Rezepte.Web/Services/Import/Plugins/PluginSourceSettingsItem.cs`

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `Id` | `string` | Quellen-ID (Guid „N"-Format) — Parameter für `CheckSourceForUpdatesAsync` |
| `RepositoryUrl` | `string` | Kanonische GitHub-URL |
| `Owner` / `Repository` | `string` | Anzeigename `Owner/Repository` |
| `IsPrivate` | `bool` | Private Quelle (PAT nötig) |
| `Enabled` | `bool` | Vorbedingung für Update-Check |
| `TrustConfirmed` | `bool` | Vorbedingung für Update-Check |
| `HasSecret` | `bool` | PAT hinterlegt (nur Anzeige) |
| `LastSuccessfulReleaseTag` | `string?` | Status-Badge „letztes erfolgreiches Release" |
| `LastError` | `string?` | Statusanzeige letzter Fehler |
| `LastCheckedAt` | `DateTime?` | Statusanzeige „Geprüft" |
| `LastErrorAt` | `DateTime?` | Zeitpunkt des letzten Fehlers (derzeit nicht gerendert) |

## `PluginSource` (EF-Entität)

Datei: `Rezepte.Web/Entities/PluginSource.cs` — Felder wie oben plus `SecretName`, `CreatedAt`, `UpdatedAt`, `Releases` (`List<PluginSourceRelease>`).

## `PluginUpdateCheckResult` (Record)

Datei: `Rezepte.Web/Services/Import/Plugins/PluginUpdateCheckResult.cs`

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `Outcome` | `string` | `PluginUpdateCheckOutcome.NoUpdate` / `Installed` / `Failed` |
| `ReleaseTag` | `string?` | Tag des geprüften/installierten Releases |
| `Error` | `string?` | Fehlermeldung bei `Failed` (englisch, serverseitig) |
| `AlreadyInstalled` | `bool` | Bei `NoUpdate`: Release war bereits installiert |
| `IsSuccess` | `bool` | `Outcome != Failed` |

Factory-Methoden: `NoUpdate(releaseTag?, alreadyInstalled)`, `Installed(releaseTag)`, `Failed(error, releaseTag?)`.

## `PluginUpdateCheckOutcome` (statische Klasse, String-Konstanten)

Datei: `Rezepte.Web/Services/Import/Plugins/PluginUpdateCheckOutcome.cs`

| Wert | Bedeutung |
|------|-----------|
| `NoUpdate` | Prüfung ok, nichts installiert (ggf. `AlreadyInstalled`) |
| `Installed` | Neues Release installiert |
| `Failed` | Prüfung/Installation fehlgeschlagen (inkl. Vorbedingungs-Verletzung) |

## `PluginSourceSaveRequest` (Record)

Datei: `Rezepte.Web/Services/Import/Plugins/PluginSourceSaveRequest.cs` — Formular-DTO (`Id`, `RepositoryUrl`, `IsPrivate`, `Enabled`, `TrustConfirmed`, `PersonalAccessToken`); unverändert.
