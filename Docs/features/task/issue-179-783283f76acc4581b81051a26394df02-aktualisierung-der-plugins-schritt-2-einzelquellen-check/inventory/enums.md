# Enums / Status-Konstanten — Bestandsaufnahme (Schritt 2: Einzelquellen-Check)

Keine echten `enum`-Typen im Check-Pfad; Status werden als String-Konstanten abgebildet. Vollständige Liste: [Projekt-inventory/enums.md](../../../../projects/task/issue-179-783283f76acc4581b81051a26394df02-aktualisierung-der-plugins/inventory/enums.md).

## `PluginSourceReleaseStatus`

Datei: `Rezepte.Web/Services/Import/Plugins/PluginSourceReleaseStatus.cs` (`public static class` mit `const string`-Werten)

| Wert | Bedeutung im Check-Ablauf |
|------|---------------------------|
| `Pending` | Neu angelegter oder zurückgesetzter Datensatz; Ausgangswert vor `DownloadValidateInstallAsync`. |
| `Downloading` | Download des ZIP-Assets läuft. |
| `DownloadFailed` | Fehler in der Download-Phase. |
| `RateLimited` | `GitHubRateLimitException` (z. B. `Retry-After`-Fall). |
| `Validating` | Paketvalidierung läuft. |
| `ValidationFailed` | Validierung fehlgeschlagen oder kein ZIP-Asset im Release. |
| `Installing` | Installation läuft (`InstallWithReloadTrackingAsync`). |
| `InstallFailed` | Fehler vor Abschluss des Verzeichnis-Tauschs (`PluginPackageInstallException.Status`). |
| `Reloading` | Koordinierter Plugin-Reload läuft (via `beforeReload`-Callback, auch in `ReloadStatus` gespiegelt). |
| `ReloadFailed` | Austausch lief, Reload/Reinitialisierung schlug fehl. |
| `Installed` | Release installiert und aktiv — Skip-Kriterium in `ProcessSourceAsync` (Z. 80). |
| `Skipped` | Definiert, wird von `PluginUpdateService` derzeit **nicht** gesetzt. |

**Schritt-2-Befund (Retry-Logik):** `ProcessSourceAsync` setzt Datensätze mit `ValidationFailed`, `DownloadFailed`, `InstallFailed`, `ReloadFailed` oder `RateLimited` auf `Pending` zurück (`PluginUpdateService.cs:100–107`) — dieselbe Wiederholungssemantik gilt dann für den Einzelcheck.

## Weitere relevante Status-Typen

| Typ | Ort | Werte / Rolle |
|-----|-----|---------------|
| `AutoUpdateOutcome` (enum, extern `msTools.Updater`) | verwendet in `ApplicationUpdateSettingsService.cs:326,405` | `Success`, `NoUpdate`, `Skipped`, `Failed` — referenziert für `IsSuccess`/Lokalisierung des Ergebnis-Musters. |
| `PluginStatus` | `Rezepte.Web/Services/Import/Plugins/PluginStatus.cs` | Plugin-Status (`Loaded`, `Missing`, …) — für Schritt 2 nicht direkt relevant (nur UI-Liste). |

Für das neue Ergebnis-Objekt (`PluginUpdateCheckResult`-Vorschlag) existiert noch kein Status-Typ; Ausprägungen kein Update / installiert / Fehler können wie oben als String-Konstanten oder als neuer Enum abgebildet werden (Umsetzungsentscheidung).
