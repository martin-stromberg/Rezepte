# Tasks: Flaky-Test-Härtung — umgebungssensitive Tests, Deadlines/Cleanup und Retry-Konvention (Issue #212)

| # | Bereich | Aufgabe | Status | Testnachweis |
|---|---------|---------|--------|--------------|
| 1 | Traits | `TestCategories` in `Rezepte.Tests/TestHelpers/` anlegen (Konstanten `Category`/`OsInterface`/`Flaky`) | Offen | — |
| 2 | Traits | `FlakyConventionTests`-Meta-Test anlegen (Trait-Auswertung via `CustomAttributeData`, xUnit-2-tauglich) | Offen | — |
| 3 | Traits | `<Compile Include>`-Links für `TestCategories`/`FlakyConventionTests` in `Rezepte.Tests.Browser.csproj` ergänzen | Offen | — |
| 4 | Traits | `[Trait("Category","OsInterface")]` auf allen 10 Browser-Testklassen vergeben | Offen | — |
| 5 | Traits | `[Trait("Category","OsInterface")]` auf `ContractExportScriptTests` (Klassenebene) vergeben | Offen | — |
| 6 | Traits | `[Trait("Category","OsInterface")]` auf `DeploymentDocumentationTests.FrameworkDependentLinuxPublish_…` und die 2 externen `PluginManagerTests`-Methoden vergeben (Methodenebene) | Offen | — |
| 7 | Traits | `[Trait("Category","Flaky")]` auf `RecipeSearchBrowserTests.DirectUrl_WithQuery_ShowsResults` vergeben (Methodenebene) | Offen | — |
| 8 | Cleanup-Helfer | `TempDirectoryCleanup` in `Rezepte.Tests/TestHelpers/` anlegen (`DeleteBestEffort` für Verzeichnisse + `DeleteFileBestEffort` für Einzeldateien, Kill vor Delete, Retry/Backoff, nur Diagnose-Restfehler, nie werfend) | Offen | — |
| 9 | Cleanup-Helfer | `<Compile Include>`-Link für `TempDirectoryCleanup` in `Rezepte.Tests.Browser.csproj` ergänzen | Offen | — |
| 10 | Cleanup-Helfer | `TempDirectoryCleanupTests` in `Rezepte.Tests/TestHelpers/` schreiben (beide Varianten: Verzeichnis + Einzeldatei) | Offen | — |
| 11 | Cleanup-Migration | `Directory.Delete`-Teardowns in `Rezepte.Tests` auf `TempDirectoryCleanup.DeleteBestEffort` umstellen (ContractExportScriptTests Z. 464/546, DeploymentDocumentationTests Z. 104, ExportCleanupServiceTests Z. 51, PluginDirectoryRegistryTestsBase Z. 66, PluginManagerTests Z. 91/682/699, PluginPackageValidatorTests Z. 78, UpdateBackupServiceTests Z. 176) | Offen | — |
| 11a | Cleanup-Migration | `File.Delete`-Teardowns auf `TempDirectoryCleanup.DeleteFileBestEffort` umstellen (`SqliteFileWebApplicationFactory` Z. 53 + 7 `finally`-Stellen: `GoogleCredentialsProviderTests` Z. 148/245, `GeminiClientTests` Z. 44/90, `SettingsCredentialAvailabilityTests` Z. 47, `AIImportHandlerActivationTests` Z. 78, `GitHubReleaseClientTests` Z. 70); `PluginManagerTests` Z. 767 begründet werfend belassen | Offen | — |
| 12 | Cleanup-Migration | `RezepteAppFixture.DeleteTemporaryDatabase` auf `TempDirectoryCleanup.DeleteBestEffort` (mit `processesToKill`) umstellen | Offen | — |
| 13 | Fixture-Härtung | `RezepteAppFixture`: Kindprozess-Ausgabe in Ringpuffer mitschneiden statt verwerfen; Tail in Timeout-/Exit-Exceptions; optionale Log-Datei unter `browser-app-logs/` | Offen | — |
| 14 | Fixture-Härtung | `RezepteAppFixture`: Start-Retry (max. 3 Versuche, neuer Port je Versuch) um `StartApplicationProcess`/`WaitUntilReadyAsync` legen; Timeout-Exception um Phase/Zeit/Exit-Code anreichern | Offen | — |
| 15 | Fixture-Härtung | `RezepteAppFixture`: `StartupTimeoutSeconds` 60 → 120 (gegen gemessene CI-Startdauer prüfen) | Offen | — |
| 16 | Fixture-Härtung | `GitHubStubServer.Dispose`: `HandleClientAsync`-Tasks mitverfolgen und gemeinsam mit Accept-Loop im 5-s-Budget abwarten | Offen | — |
| 17 | Fixture-Härtung | `PlaywrightTraces`-Helfer anlegen (Repo-Root-Pfad via `RepositoryPaths`, Dateinamen-Konvention) und die vier Trace-Schreibstellen (`BrowserPageObjectBase`, `LoadingBarPageObject`, `PluginSettingsPageObject`, `RegisterPage`) umstellen | Offen | — |
| 18 | Fixture-Härtung | `/playwright-traces/` in `.gitignore` ergänzen | Offen | — |
| 19 | Timing-Fixes | `LoadingBarPageObject.WaitUntilLoadingBarActiveAsync`-Default 1000 → 5000 ms; `WaitUntilLoadingBarVisibleAsync`/`WaitUntilLoadingBarColorChangedAsync`-Defaults 2000 → 5000 ms | Offen | — |
| 19a | Timing-Fixes | `BrowserTimeouts`-Helfer (`NavigationTimeoutMilliseconds = 60000`) in `Rezepte.Tests.Browser/Infrastructure/` anlegen; `context.SetDefaultNavigationTimeout` an allen sechs `NewContextAsync`-Stellen setzen (`BrowserPageObjectBase` Z. 54, `LoadingBarPageObject` Z. 70, `PluginSettingsPageObject` Z. 46, `RegisterPage` Z. 42, `ShareLinkPageObject` Z. 212/228) | Offen | — |
| 20 | Timing-Fixes | `ExportCleanupHostedServiceTests.StartAsync_ShouldCheckImmediatelyOnStartup`: `Task.Delay(100)` durch `TaskCompletionSource`-Signal im Moq-Callback + `WaitAsync(5s)` ersetzen | Offen | — |
| 21 | Timing-Fixes | Begründete Belassungen im Klassifikations-Dokument dokumentieren (Timeout-Inventar-Tabelle): `Task.Delay`-Stellen (`ImportOrchestratorTests` Z. 243, `PluginUpdateServiceTests_Serialization` Z. 36/77, `RecipeSearchComponentTests` Z. 274ff., `ApplicationUpdatePreInstallHandlerTests` Z. 108), `WaitForTimeoutAsync` Z. 70, belassene Playwright-Budgets (`RegisterPage` 30 s, `ShareLinkPageObject` 10–20 s, 10-s-URL-/Selector-Waits, 15-s/20-s-`RecipeSearchBrowserTests`, 20-s/120-s-`RegisterTests`, Retry-Muster `PluginSettingsPageObject` Z. 90 + `LoadingBarPageObject` Z. 180/191) und `PluginManagerTests` Z. 767 (werfender Arrange-Delete) | Offen | — |
| 22 | Skripte | `scripts/lib/Trx.ps1` aus Blaupause adaptieren (`Get-TrxTestResults`, `Get-TrxRunStartTime`) | Offen | — |
| 23 | Skripte | `scripts/lib/TestDiscovery.ps1` aus Blaupause adaptieren (`Get-FlakyTestNames` via `--list-tests --filter "Category=Flaky"`) | Offen | — |
| 24 | Skripte | `scripts/Retry-FlakyTests.ps1` aus Blaupause adaptieren (Pfade, Rezepte-Doku-Verweise, Release-Konfiguration beim Aufruf) | Offen | — |
| 25 | Skripte | `scripts/Analyze-FlakyTestResults.ps1` aus Blaupause adaptieren (Report-only, Schwellwert-Parameter mit Blaupausen-Defaults) | Offen | — |
| 26 | Skripte | Skripte mit synthetischen TRX-Sätzen verifizieren (recovered, persistent, kein Fehlschlag, fehlendes TRX, Skip-Outcome) | Offen | — |
| 27 | Workflows | `pr-staging-ci.yml`: `build-and-test` verschlanken (Browser-Build/Playwright-Install/Publish entfernen, Test-Step auf `Rezepte.Tests` + `--filter "Category!=OsInterface"` + attempt-TRX-Name, Upload-Pfade/Kommentar anpassen) | Offen | — |
| 28 | Workflows | `pr-staging-ci.yml`: Job `browser-tests` einführen (`needs: detect-backmerge`, `if: needs.detect-backmerge.outputs.is_backmerge != 'true'`, `continue-on-error: true`, `timeout-minutes: 45`, Build/Playwright/Publish mit `id: publish`, zwei Test-Steps — OsInterface-Subset mit `if: ${{ !cancelled() && steps.publish.outcome == 'success' }}`, `if: failure()`-Retry, `if: always()`-Analyze, Artefakt-Uploads inkl. Traces) | Offen | — |
| 29 | Workflows | `staging-ci.yml`: analoge `build-and-test`-Verschlankung + `browser-tests`-Job (inkl. Backmerge-Guard und präzisiertem Subset-`if:`); `version`-`needs` unverändert ohne `browser-tests` | Offen | — |
| 30 | Workflows | Artefakt-/TRX-Namen mit `${{ github.run_attempt }}`-Suffix versehen; TRX-Upload-Globs auf `test-results-browser*.trx`/`test-results-os-interface*.trx` (inkl. `*-flaky-retry.trx`) setzen | Offen | — |
| 31 | Dokumentation | `Docs/help/os-interface-tests.md` anlegen (Klassifizierungskriterien, Lane-Struktur, Trait-/Retry-/Quarantäne-Konvention, `gh run rerun --job <id>`-Regel nur für `browser-tests`, explizite Verknüpfung der AC-4-Formulierung mit der Ausbaustufe-1-Festlegung „Empfehlung statt Auto-Issue") | Offen | — |
| 32 | Dokumentation | Klassifikations-Ergebnis aus `inventory/testliste.md` in persistente Doku überführen (Einstufung + Trait-Vergaben + Timeout-Inventar-Tabelle mit begründeten Belassungen) | Offen | — |
| 33 | Dokumentation | `Docs/testing.md` um Lane-Beschreibung und lokale Filter-Aufrufe ergänzen | Offen | — |
| 34 | Dokumentation | `Docs/help/github-actions.md` an Job-Struktur/TRX-/Trace-Artefakte/`continue-on-error`-Semantik anpassen | Offen | — |
| 35 | Dokumentation | `Docs/features.md` (Abschnitt CI/CD) und `Docs/help/index.md` verlinken/aktualisieren | Offen | — |
| 36 | Verifikation | Lokale Verifikation: `dotnet test Rezepte.Tests --filter "Category!=OsInterface"` und `Rezepte.Tests.Browser` grün; `--list-tests`-Filter zeigen erwartete Mengen | Offen | — |
| 37 | Verifikation | ≥3 grüne Komplettläufe in `pr-staging-ci.yml` und `staging-ci.yml`; Retry-/Analyze-Steps und Trace-Artefakt-Inhalt in realen Läufen sichtbar prüfen | Offen | — |
