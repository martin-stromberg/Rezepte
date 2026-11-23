# Plan-Review

> Modell-Hinweis: Devin-Umgebung ohne Codex-Modellauswahl; Sitzungsmodell geerbt
> (dokumentierte Abweichung, Rolle: Review, Effort: xhigh).

## Ergebnis

**Status:** Offene Aufgaben vorhanden

Alle **implementierbaren** Planelemente sind vollständig und plan-konform umgesetzt
(Tasks 1–36). Einzig Task 37 bleibt offen — dies ist per Plan (Schritt 10:
„Voraussetzungen: Schritte 1–9 gemergt auf den Branch bzw. im PR-Lauf") eine
**externe CI-Verifikation nach Commit/PR** und aus dem Working Tree prinzipiell
nicht prüfbar, also kein Implementierungsdefizit. Der zu prüfende Workflow-Umbau
selbst ist vollständig und plan-konform (siehe unten).

Gegenüber dem archivierten Vorgänger-Review (`review.1.md`, damals Tasks 36/37
offen) ist Task 36 inzwischen vollständig nachgewiesen: `test-results.md`
dokumentiert den grünen Komplettlauf der Browser-Suite (35/35) sowie beide
`Rezepte.Tests`-Lanes grün (758/758 deterministisch, 15/15 OsInterface-Subset)
inklusive `--list-tests`-Filter-Verifikation.

## Umgesetzte Planelemente

### Neue Objekte

- [x] `TestCategories` (`Rezepte.Tests/TestHelpers/TestCategories.cs`, statische Klasse) — angelegt; Konstanten `Category`/`OsInterface`/`Flaky` (Z. 12/19/26)
- [x] `TempDirectoryCleanup` (`Rezepte.Tests/TestHelpers/TempDirectoryCleanup.cs`, `internal static`) — angelegt; `DeleteBestEffort(root, processesToKill, normalizeAttributes)` + `DeleteFileBestEffort(path)`; Kill vor Delete (10-s-`WaitForExit`-Budget), max. 5 Versuche mit `100 * attempt`-Backoff, nur `IOException`/`UnauthorizedAccessException` retried, wirft nie, fehlende Pfade = No-op
- [x] `BrowserTimeouts` (`Rezepte.Tests.Browser/Infrastructure/BrowserTimeouts.cs`, `internal static`) — angelegt; `NavigationTimeoutMilliseconds = 60000`
- [x] `FlakyConventionTests` (`Rezepte.Tests/TestHelpers/FlakyConventionTests.cs`, Testklasse, in beide Assemblies gelinkt) — angelegt; `FlakyMarkedTests_AlwaysCarryOsInterface` via `CustomAttributeData` (xUnit-2-tauglich), Klassen-Traits vererben auf Methoden
- [x] `TempDirectoryCleanupTests` (`Rezepte.Tests/TestHelpers/TempDirectoryCleanupTests.cs`, Testklasse) — angelegt; 8 Facts (Retry/Nie-Werfen/Prozess-Kill/Enumerator-Wurf/nichtexistenter Pfad/`normalizeAttributes`/fehlende Datei/Read-only)
- [x] `PlaywrightTraces` (`Rezepte.Tests.Browser/Infrastructure/PlaywrightTraces.cs`, statischer Helfer) — angelegt; `<RepoRoot>/playwright-traces/` via `RepositoryPaths.FindRepositoryRoot()`, Namenskonvention `{Prefix}-{yyyyMMdd-HHmmss}-{Guid:N}.zip`
- [x] `scripts/Retry-FlakyTests.ps1` — angelegt; Parameter `-TestProject`/`-PrimaryTrx`/`-Configuration`/`-RetryTrxFileName`; fehlendes TRX → Exit 0; genau ein gefilterter Re-Run; persistent → Exit 1
- [x] `scripts/Analyze-FlakyTestResults.ps1` — angelegt; Report-only (immer Exit 0), Blaupausen-Schwellwerte (`MaxRuns=20`, `QuarantineMinFlakeEvents=2`, `QuarantineMinFailureRate=0.2`, `QuarantineMinExecutions=5`, `WatchMinFailureRate=0.1`), `::warning::`-Annotation
- [x] `scripts/lib/Trx.ps1` — angelegt; `Get-TrxTestResults`, `Get-TrxRunStartTime`
- [x] `scripts/lib/TestDiscovery.ps1` — angelegt; `Get-FlakyTestNames` via `dotnet test --list-tests --filter "Category=Flaky"`

### Geänderte bestehende Objekte

- [x] `RezepteAppFixture` — `StartupTimeoutSeconds` = 120 (Z. 30); `StartApplicationProcessWithRetryAsync` (max. 3 Versuche via `MaxStartAttempts`, neuer Port je Versuch); Ringpuffer `_stdoutTail`/`_stderrTail` (100 Zeilen) statt verwerfender Handler; Timeout-/Early-Exit-Exceptions mit Phase, vergangener Zeit, Exit-Code und Ausgabe-Tail; Vollausgabe in `browser-app-logs/` (`StartProcessLogFile`, Z. 345–372); `DeleteTemporaryDatabase` → `TempDirectoryCleanup.DeleteBestEffort` (Z. 190, bewusst ohne `processesToKill` — Kommentar dokumentiert, dass `StopApplicationProcess` stets vorher läuft)
- [x] `GitHubStubServer.Dispose` — `_handlerTasks` (`ConcurrentBag<Task>`) mitverfolgt; zweiphasiges Warten (Accept-Loop, dann Handler im Rest des 5-s-Budgets); `AggregateException` weiter geschluckt (Z. 101–128)
- [x] `LoadingBarPageObject` — Defaults `WaitUntilLoadingBarActiveAsync` 1000→5000, `…VisibleAsync`/`…ColorChangedAsync` 2000→5000 (Z. 246/259/273); `SetDefaultNavigationTimeout` nach `NewContextAsync` (Z. 71); Trace-Pfad via `PlaywrightTraces` (Z. 302)
- [x] `BrowserPageObjectBase`, `PluginSettingsPageObject`, `RegisterPage`, `ShareLinkPageObject` — `SetDefaultNavigationTimeout` an allen sechs `NewContextAsync`-Stellen (`BrowserPageObjectBase` Z. 55, `PluginSettingsPageObject` Z. 47, `RegisterPage` Z. 44, `ShareLinkPageObject` Z. 213/230); Trace-Schreibstellen `BrowserPageObjectBase` Z. 139, `PluginSettingsPageObject` Z. 272, `RegisterPage` Z. 143 auf `PlaywrightTraces` umgestellt (exakt 4 Schreibstellen, plan-konform)
- [x] `LoadingBarFormNavigationBrowserTests` — `WaitForTimeoutAsync(500)` (Z. 74) begründet belassen; Kommentar verweist auf die Konvention in `Docs/help/os-interface-tests.md` (Z. 70–73)
- [x] 10 Browser-Testklassen — `[Trait(TestCategories.Category, TestCategories.OsInterface)]` auf Klassenebene (`RegisterTests` Z. 17, `LoadingBarColorBrowserTests` Z. 13, `LoadingBarDisabledBrowserTests`/`LoadingBarFormNavigationBrowserTests`/`LoadingBarSafetyTimeoutBrowserTests`/`LoadingBarVisibilityBrowserTests`/`PluginSettingsBrowserTests`/`SecurityTxtBrowserTests` Z. 12, `RecipeSearchBrowserTests` Z. 13, `ShareLinkBrowserTests` Z. 15)
- [x] `RecipeSearchBrowserTests.DirectUrl_WithQuery_ShowsResults` — `[Trait(…, TestCategories.Flaky)]` auf Methodenebene (Z. 60)
- [x] `ContractExportScriptTests` — Klassen-Trait `OsInterface` (Z. 15); `Directory.Delete` ×2 → `DeleteBestEffort` (Z. 465, 546)
- [x] `DeploymentDocumentationTests` — Methoden-Trait `OsInterface` auf `FrameworkDependentLinuxPublish_…` (Z. 56); `Directory.Delete` → `DeleteBestEffort` (Z. 103)
- [x] `PluginManagerTests` — Methoden-Traits `OsInterface` auf den beiden externen Methoden (Z. 160, 195); `Directory.Delete` ×3 → `DeleteBestEffort` (Z. 88, 673, 686); `File.Delete(adjacentContract)` (Z. 746) begründet werfend belassen (Arrange-Logik, plan-konform)
- [x] `ExportCleanupHostedServiceTests.StartAsync_ShouldCheckImmediatelyOnStartup` — `Task.Delay(100)` durch `TaskCompletionSource` im Moq-`Callback` + `WaitAsync(5 s)` ersetzt (Z. 75–82)
- [x] Cleanup-Migration `Directory.Delete` — `ExportCleanupServiceTests` Z. 50, `PluginDirectoryRegistryTestsBase` Z. 63, `PluginPackageValidatorTests` Z. 77, `UpdateBackupServiceTests` Z. 175 → `DeleteBestEffort`
- [x] Cleanup-Migration `finally`-`File.Delete` — `SqliteFileWebApplicationFactory` Z. 52, `GoogleCredentialsProviderTests` Z. 148/245, `GeminiClientTests` Z. 44/90, `SettingsCredentialAvailabilityTests` Z. 47, `AIImportHandlerActivationTests` Z. 78, `GitHubReleaseClientTests` Z. 69 → `DeleteFileBestEffort` (kein rohes `File.Delete`/`Directory.Delete` in Teardowns mehr; Grep-Verifikation)
- [x] `Rezepte.Tests.Browser.csproj` — `<Compile Include>`-Links für `TestCategories.cs`, `TempDirectoryCleanup.cs`, `FlakyConventionTests.cs` (Z. 30–32, Muster wie `RepositoryPaths.cs` Z. 29)
- [x] `.gitignore` — `/playwright-traces/` (Z. 420) und `/browser-app-logs/` (Z. 423) ergänzt
- [x] `pr-staging-ci.yml` — `build-and-test` verschlankt (Browser-Build/Playwright-Install/Publish entfernt, Filter `Category!=OsInterface`, `test-results-a${{ github.run_attempt }}.trx`, Upload nur `Rezepte.Tests/TestResults`, Kommentar aktualisiert); neuer Job `browser-tests` (Z. 207–326): `needs: detect-backmerge` + `is_backmerge != 'true'`-Guard, `continue-on-error: true`, `timeout-minutes: 45`, `id: publish`, OsInterface-Subset mit `if: ${{ !cancelled() && steps.publish.outcome == 'success' }}`, `if: failure()`-Retry-Step (pwsh, beide Primär-TRX, aggregierter Exit-Code), `if: always()`-Analyze-Step, Uploads `test-results-browser-pr-aN`/`playwright-traces-pr-aN`/`browser-app-logs-pr-aN`; `browser-tests` in keinem `needs:` eines Gate-Jobs
- [x] `staging-ci.yml` — analoge Verschlankung + identischer `browser-tests`-Job (Z. 192–304, Staging-Artefaktnamen); `version` behält `needs: [detect-backmerge, static-checks, build-and-test]` ohne `browser-tests` (Z. 308); `prerelease` unverändert
- [x] `Docs/help/os-interface-tests.md` — angelegt: Audit-/Klassifizierungskriterien, Lane-Struktur, Trait-/Retry-/Quarantäne-Konvention, `gh run rerun --job <id>`-Regel nur für `browser-tests`, AC-4-/Ausbaustufe-1-Verknüpfung, Timeout-Inventar-Tabelle mit begründeten Belassungen
- [x] `Docs/testing.md`, `Docs/help/github-actions.md`, `Docs/features.md`, `Docs/help/index.md` — Lane-Beschreibung + lokale Filter-Aufrufe, Job-Struktur/`continue-on-error`-Semantik/Artefakte, CI/CD-Verweis, Index-Verlinkung

### Verifikationsnachweise

- Lokale Lanes (`test-results.md`, 2026-11-22): `dotnet test Rezepte.Tests --filter "Category!=OsInterface"` **758/758 grün**; `--filter "Category=OsInterface"` **15/15 grün**; `dotnet test Rezepte.Tests.Browser` **35/35 grün**; Meta-Tests 9/9 + 1/1
- `--list-tests`-Filter: `Category=Flaky` → genau `DirectUrl_WithQuery_ShowsResults`; `Category=OsInterface` → die geplante Teilmenge in `Rezepte.Tests` (inkl. `TempDirectoryCleanupTests.DeleteBestEffort_ProcessesToKill_…`, dokumentierte plan-konforme Ergänzung) und 34/35 der Browser-Assembly
- `Retry-FlakyTests.ps1`: End-to-End gegen synthetische und reale Primär-TRX verifiziert (genau ein gefilterter Re-Run, `…-flaky-retry.trx`, recovered → Exit 0, persistent → Exit 1, fehlendes TRX → Exit 0)
- `Analyze-FlakyTestResults.ps1`: synthetischer Lauf-Satz — recovered/persistent/fail→pass/`NotExecuted`/stabil korrekt klassifiziert, immer Exit 0
- Laufzeit-Artefakte: `playwright-traces/*.zip` (Konventionsnamen) und `browser-app-logs/*.log` am Repo-Root befüllt, beide gitignored
- `review-code.md` (Runde 2): „Keine Befunde" — alle 7 Befunde aus Runde 1 korrekt behoben

## Offene Aufgaben

- [ ] `{Task 37 — CI-Verifikation}` — **extern verankerte Verifikation, kein Implementierungsdefizit**: ≥3 grüne Komplettläufe in `pr-staging-ci.yml` und `staging-ci.yml` inkl. Sichtprüfung der Retry-/Analyze-Steps und des Trace-Artefakt-Inhalts. Der Plan verankert diesen Schritt explizit nach Merge/PR (Schritt 10: „Voraussetzungen: Schritte 1–9 gemergt auf den Branch bzw. im PR-Lauf"); er ist aus dem uncommitteten Working Tree prinzipiell nicht durchführbar. Der implementierbare Anteil — der Workflow-Umbau beider Dateien — ist vollständig und plan-konform (siehe Checkliste oben).

## Hinweise

- `Analyze-FlakyTestResults.ps1` wertet für die fail→pass-Sequenz das **effektive Lauf-Outcome** aus (Primär-Outcome wird durch ein `Passed`/`Failed`-Retry-Outcome ersetzt) statt wörtlich nur Primär-Outcomes — verhindert Doppelzählung desselben Vorfalls; bereits in `review.1.md` als fachlich plausible, Schwellwert-konforme Lesart dokumentiert.
- Der dokumentierte Baseline-Flake `DirectUrl_WithQuery_ShowsResults` bestand im lokalen Komplettlauf (35/35) — der `Flaky`-Trait und die Retry-Konvention bleiben für etwaige CI-Flakes aktiv.
- `Xunit.SkippableFact` ist in `Rezepte.Tests.csproj` weiterhin nicht referenziert — plan-konform (Nachrüstung erst beim ersten tatsächlichen Quarantäne-Fall; das Browser-Projekt referenziert es bereits).
