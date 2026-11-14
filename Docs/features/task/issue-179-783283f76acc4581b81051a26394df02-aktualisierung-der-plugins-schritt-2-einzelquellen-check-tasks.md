# Tasks: Manuelle Update-Prüfung für eine einzelne Pluginquelle (Schritt 2, Issue #179)

| # | Bereich | Aufgabe | Status | Testnachweis |
|---|---------|---------|--------|--------------|
| 1 | Datenmodell | `PluginUpdateCheckOutcome` (statische Konstantenklasse `NoUpdate`/`Installed`/`Failed`) in `Rezepte.Web/Services/Import/Plugins/` anlegen | Offen | — |
| 2 | Datenmodell | `PluginUpdateCheckResult` (sealed record: `Outcome`, `ReleaseTag`, `Error`, `AlreadyInstalled`, `IsSuccess`, Factories `NoUpdate`/`Installed`/`Failed`) anlegen | Offen | — |
| 3 | Logik | `ProcessSourceAsync` auf `Task<PluginUpdateCheckResult>` umstellen (alle Early-Returns und `catch` mappen) | Offen | — |
| 4 | Logik | `DownloadValidateInstallAsync` auf `Task<PluginUpdateCheckResult>` umstellen (`Installed`/`Failed`) | Offen | — |
| 5 | Logik | Statischen `SemaphoreSlim CheckGate` in `PluginUpdateService` einführen und `CheckForUpdatesAsync` komplett darunter stellen | Offen | — |
| 6 | Logik | `IPluginUpdateService.CheckSourceForUpdatesAsync(sourceId, ct)` deklarieren | Offen | — |
| 7 | Logik | `PluginUpdateService.CheckSourceForUpdatesAsync` implementieren (Guard, Quelle laden, Vorbedingungen als `Failed`, Delegation an `ProcessSourceAsync`) | Offen | — |
| 8 | Logik | `IPluginSettingsService.CheckSourceForUpdatesAsync(sourceId, ct)` deklarieren | Offen | — |
| 9 | Logik | `PluginSettingsService`: `IPluginUpdateService` als erforderliche Konstruktor-Abhängigkeit ergänzen | Offen | — |
| 10 | Logik | `PluginSettingsService.CheckSourceForUpdatesAsync` implementieren (`EnsureAdmin` + Delegation) | Offen | — |
| 11 | Validierung | Vorbedingungs-Ablehnungen als `Failed`-Ergebnisse abbilden (unbekannte Id, `Enabled=false`, `TrustConfirmed=false`) | Offen | — |
| 12 | Tests | `PluginUpdateServiceTestsBase` anlegen und `CreateDb`/`CreateSut`/Fakes aus `PluginUpdateServiceTests` dorthin verschieben | Offen | — |
| 13 | Tests | `FakeSecretStore` konfigurierbar machen (Token-Rückgabe steuerbar, Standard `"token"`) | Offen | — |
| 14 | Tests | `BlockingGitHubReleaseClient` (TCS-Blockade + Entered-Signal) für Serialisierungstests anlegen | Offen | — |
| 15 | Tests | `FakePluginUpdateService` (fängt `sourceId`, konfigurierbares Ergebnis) für Settings-Tests anlegen | Offen | — |
| 16 | Tests | `PluginUpdateServiceTests` auf Basisklasse umstellen (4 bestehende Tests unverändert) | Offen | — |
| 17 | Tests | `PluginSettingsServiceTests`: 9 Konstruktoraufrufe um `FakePluginUpdateService` ergänzen | Offen | — |
| 18 | Tests | `PluginUpdateServiceTests_CheckSource`: Install-Ergebnis + Statusfortschreibung | Offen | — |
| 19 | Tests | `PluginUpdateServiceTests_CheckSource`: AlreadyInstalled-Skip (`NoUpdate` + Flag + Tag, kein Download) | Offen | — |
| 20 | Tests | `PluginUpdateServiceTests_CheckSource`: Retry fehlgeschlagener Release → `Installed` | Offen | — |
| 21 | Tests | `PluginUpdateServiceTests_CheckSource`: Fehlerfälle kein Release / kein ZIP-Asset / fehlendes PAT / unerwartete Exception → `Failed` + `LastError` | Offen | — |
| 22 | Tests | `PluginUpdateServiceTests_Preconditions`: unbekannte Id / `Enabled=false` / `TrustConfirmed=false` → `Failed`, kein GitHub-Aufruf | Offen | — |
| 23 | Tests | `PluginUpdateServiceTests_Serialization`: manueller Check wartet hinter `CheckForUpdatesAsync` (instanzübergreifend) | Offen | — |
| 24 | Tests | `PluginUpdateServiceTests_Serialization`: `CheckForUpdatesAsync` wartet hinter manuellem Check (Spiegelrichtung) | Offen | — |
| 25 | Tests | `PluginSettingsServiceTests_CheckSource`: Ablehnung Nicht-Admin + fehlender HttpContext | Offen | — |
| 26 | Tests | `PluginSettingsServiceTests_CheckSource`: Delegation an `IPluginUpdateService` und Ergebnisdurchreiche | Offen | — |
| 27 | Tests | Plugin-gefilterten Testlauf (`dotnet test Rezepte.Tests --filter "FullyQualifiedName~Plugin"`) und Gesamtsuite ausführen | Offen | — |
