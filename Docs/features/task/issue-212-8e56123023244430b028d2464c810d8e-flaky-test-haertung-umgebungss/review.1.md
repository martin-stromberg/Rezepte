# Plan-Review

> Modell-Hinweis: Devin-Umgebung ohne Codex-Modellauswahl; Sitzungsmodell geerbt
> (dokumentierte Abweichung, Rolle: Review, Effort: xhigh).

## Ergebnis

**Status:** Offene Aufgaben vorhanden

Alle Implementierungs-Planelemente sind vollständig und korrekt umgesetzt (Tasks
1–35). Offen bleiben ausschließlich die beiden Verifikations-Tasks 36 und 37:
Task 36 ist teilweise nachgewiesen (deterministische Lane lokal grün 758/758,
`--list-tests`-Filter verifiziert; der grüne Komplettlauf der Browser-Suite
steht noch aus), Task 37 erfordert committeten Stand und echte CI-Läufe.

## Umgesetzte Planelemente

### Neue Objekte

- [x] `TestCategories` (`Rezepte.Tests/TestHelpers/TestCategories.cs`, statische Klasse) — angelegt; Konstanten `Category`/`OsInterface`/`Flaky`
- [x] `TempDirectoryCleanup` (`Rezepte.Tests/TestHelpers/TempDirectoryCleanup.cs`, `internal static`) — angelegt; Kill vor Delete (10-s-`WaitForExit`-Budget), max. 5 Versuche mit `100 * attempt`-Backoff, nur `IOException`/`UnauthorizedAccessException` retried, wirft nie
- [x] `BrowserTimeouts` (`Rezepte.Tests.Browser/Infrastructure/BrowserTimeouts.cs`, `internal static`) — angelegt; `NavigationTimeoutMilliseconds = 60000`
- [x] `FlakyConventionTests` (`Rezepte.Tests/TestHelpers/FlakyConventionTests.cs`, Testklasse) — angelegt; Trait-Auswertung via `CustomAttributeData` (xUnit-2-tauglich), scannt die jeweils eigene Assembly
- [x] `TempDirectoryCleanupTests` (`Rezepte.Tests/TestHelpers/TempDirectoryCleanupTests.cs`, Testklasse) — angelegt; 7 Facts (Retry/Nie-Werfen/Prozess-Kill/nichtexistenter Pfad/`normalizeAttributes`/fehlende Datei)
- [x] `PlaywrightTraces` (`Rezepte.Tests.Browser/Infrastructure/PlaywrightTraces.cs`, statischer Helfer) — angelegt; `<RepoRoot>/playwright-traces/` via `RepositoryPaths.FindRepositoryRoot()`, Namenskonvention `{Prefix}-{yyyyMMdd-HHmmss}-{Guid:N}.zip`
- [x] `scripts/Retry-FlakyTests.ps1` — angelegt; Parameter `-TestProject`/`-PrimaryTrx`/`-Configuration`/`-RetryTrxFileName`; fehlendes TRX → Exit 0; genau ein gefilterter Re-Run; persistent → Exit 1
- [x] `scripts/Analyze-FlakyTestResults.ps1` — angelegt; Report-only (immer Exit 0), Blaupausen-Schwellwerte (`MaxRuns=20`, `QuarantineMinFlakeEvents=2`, `QuarantineMinFailureRate=0.2`, `QuarantineMinExecutions=5`, `WatchMinFailureRate=0.1`), `::warning::`-Annotation
- [x] `scripts/lib/Trx.ps1` — angelegt; `Get-TrxTestResults`, `Get-TrxRunStartTime`
- [x] `scripts/lib/TestDiscovery.ps1` — angelegt; `Get-FlakyTestNames` via `dotnet test --list-tests --filter "Category=Flaky"`

### Geänderte bestehende Objekte

- [x] `RezepteAppFixture` — `StartupTimeoutSeconds` = 120 (Z. 30); `StartApplicationProcessWithRetryAsync` (max. 3 Versuche, neuer Port je Versuch); Ringpuffer `_stdoutTail`/`_stderrTail` (100 Zeilen) statt verwerfender Handler; Timeout-/Exit-Exceptions mit Phase, vergangener Zeit, Exit-Code und Ausgabe-Tail; Vollausgabe in `browser-app-logs/` (laufzeitverifiziert); `DeleteTemporaryDatabase` → `TempDirectoryCleanup.DeleteBestEffort` mit `processesToKill` (Z. 167–178)
- [x] `GitHubStubServer.Dispose` — `_handlerTasks` (`ConcurrentBag<Task>`) mitverfolgt; `Task.WaitAll` über Accept-Loop + Handler im 5-s-Budget; `AggregateException` weiter geschluckt
- [x] `LoadingBarPageObject` — Defaults `WaitUntilLoadingBarActiveAsync` 1000→5000, `…VisibleAsync`/`…ColorChangedAsync` 2000→5000 (Z. 246/259/273); `SetDefaultNavigationTimeout` nach `NewContextAsync` (Z. 71); Trace-Pfad via `PlaywrightTraces` (Z. 302)
- [x] `BrowserPageObjectBase`, `PluginSettingsPageObject`, `RegisterPage`, `ShareLinkPageObject` — `SetDefaultNavigationTimeout` an allen sechs `NewContextAsync`-Stellen (Z. 55 / 47 / 44 / 213+230); Trace-Schreibstellen `BrowserPageObjectBase` Z. 139, `PluginSettingsPageObject` Z. 272, `RegisterPage` Z. 143 auf `PlaywrightTraces` umgestellt (exakt 4 Schreibstellen)
- [x] `LoadingBarFormNavigationBrowserTests` — `WaitForTimeoutAsync(500)` belassen, Kommentar um Konventions-Verweis ergänzt (Z. 70–74)
- [x] 10 Browser-Testklassen — `[Trait(TestCategories.Category, TestCategories.OsInterface)]` auf Klassenebene (`RegisterTests`, `LoadingBarColorBrowserTests`, `LoadingBarDisabledBrowserTests`, `LoadingBarFormNavigationBrowserTests`, `LoadingBarSafetyTimeoutBrowserTests`, `LoadingBarVisibilityBrowserTests`, `PluginSettingsBrowserTests`, `RecipeSearchBrowserTests`, `SecurityTxtBrowserTests`, `ShareLinkBrowserTests`)
- [x] `RecipeSearchBrowserTests.DirectUrl_WithQuery_ShowsResults` — `[Trait(…, TestCategories.Flaky)]` auf Methodenebene (Z. 60); `--list-tests --filter "Category=Flaky"` zeigt genau diesen einen Test
- [x] `ContractExportScriptTests` — Klassen-Trait `OsInterface` (Z. 15); `Directory.Delete` ×2 → `DeleteBestEffort` (Z. 465, 546)
- [x] `DeploymentDocumentationTests` — Methoden-Trait `OsInterface` auf `FrameworkDependentLinuxPublish_…` (Z. 56); `Directory.Delete` → `DeleteBestEffort` (Z. 103)
- [x] `PluginManagerTests` — Methoden-Traits `OsInterface` auf den beiden externen Methoden (Z. 160, 195); `Directory.Delete` ×3 → `DeleteBestEffort` (Z. 88, 673, 686); `File.Delete(adjacentContract)` (Z. 746) begründet werfend belassen (Arrange-Logik)
- [x] `ExportCleanupHostedServiceTests.StartAsync_ShouldCheckImmediatelyOnStartup` — `Task.Delay(100)` durch `TaskCompletionSource` im Moq-`Callback` + `WaitAsync(5 s)` ersetzt (Z. 75–82)
- [x] Cleanup-Migration `Directory.Delete` — `ExportCleanupServiceTests` Z. 50, `PluginDirectoryRegistryTestsBase` Z. 63, `PluginPackageValidatorTests` Z. 77, `UpdateBackupServiceTests` Z. 175 → `DeleteBestEffort`
- [x] Cleanup-Migration `finally`-`File.Delete` — `SqliteFileWebApplicationFactory` Z. 52, `GoogleCredentialsProviderTests` Z. 148/245, `GeminiClientTests` Z. 44/90, `SettingsCredentialAvailabilityTests` Z. 47, `AIImportHandlerActivationTests` Z. 78, `GitHubReleaseClientTests` Z. 69 (inkl. Wegfall der `File.Exists`-Vorprüfung) → `DeleteFileBestEffort`
- [x] `Rezepte.Tests.Browser.csproj` — `<Compile Include>`-Links für `TestCategories.cs`, `TempDirectoryCleanup.cs`, `FlakyConventionTests.cs` (Z. 30–32, Muster wie `RepositoryPaths.cs` Z. 29)
- [x] `.gitignore` — `/playwright-traces/` und `/browser-app-logs/` ergänzt
- [x] `pr-staging-ci.yml` — `build-and-test` verschlankt (Browser-Steps entfernt, Filter `Category!=OsInterface`, `test-results-a${{ github.run_attempt }}.trx`, Upload nur `Rezepte.Tests/TestResults`); neuer Job `browser-tests` (Z. 207–326): `needs: detect-backmerge` + `is_backmerge != 'true'`-Guard, `continue-on-error: true`, `timeout-minutes: 45`, `id: publish`, OsInterface-Subset mit `if: ${{ !cancelled() && steps.publish.outcome == 'success' }}`, `if: failure()`-Retry-Step (pwsh, beide Primär-TRX, aggregierter Exit-Code), `if: always()`-Analyze-Step, Uploads `test-results-browser-pr-aN`/`playwright-traces-pr-aN`/`browser-app-logs-pr-aN`
- [x] `staging-ci.yml` — analoge Verschlankung + identischer `browser-tests`-Job (Staging-Artefaktnamen); `version` behält `needs: [detect-backmerge, static-checks, build-and-test]` ohne `browser-tests` (Z. 308)
- [x] `Docs/help/os-interface-tests.md` — angelegt: Klassifizierungskriterien, Lane-Struktur, Trait-/Retry-/Quarantäne-Konvention, `gh run rerun --job <id>`-Regel nur für `browser-tests`, AC-4-/Ausbaustufe-1-Verknüpfung, Timeout-Inventar-Tabelle mit begründeten Belassungen
- [x] `Docs/testing.md`, `Docs/help/github-actions.md`, `Docs/features.md`, `Docs/help/index.md` — Lane-Beschreibung, lokale Filter-Aufrufe, Job-Struktur/`continue-on-error`-Semantik/Artefakte, CI/CD-Verweis, Index-Verlinkung

### Verifikationsnachweise aus diesem Review

- `dotnet build` beider Testprojekte: grün, 0 Warnungen/Fehler
- `dotnet test Rezepte.Tests --list-tests --filter "Category=OsInterface"`: exakt die geplante Teilmenge (alle `ContractExportScriptTests`, `DeploymentDocumentationTests.FrameworkDependentLinuxPublish_…`, 2 `PluginManagerTests`-Methoden, `TempDirectoryCleanupTests.DeleteBestEffort_ProcessesToKill_…`)
- `dotnet test Rezepte.Tests.Browser --list-tests --filter "Category=Flaky"`: genau `DirectUrl_WithQuery_ShowsResults`
- `dotnet test Rezepte.Tests --filter "Category!=OsInterface"`: **758/758 grün**
- `Retry-FlakyTests.ps1` end-to-end gegen `rezepte-tests-browser-baseline.trx`: Schnittmenge korrekt (1 Flaky-Fehlschlag), genau ein gefilterter Re-Run, `…-flaky-retry.trx` geschrieben, persistent → `::warning::` + Exit 1; fehlendes TRX → Exit 0
- `Analyze-FlakyTestResults.ps1` gegen synthetischen 3-Lauf-TRX-Satz: recovered → watch, persistent → quarantine candidate (`::warning::`), fail→pass-Transition → watch, `NotExecuted` → fehlende Beobachtung, stabil → stable; Exit 0
- Laufzeit-Artefakte: `playwright-traces/*.zip` (konventionskonforme Namen) und `browser-app-logs/*.log` am Repo-Root befüllt; beide Pfade gitignored

## Offene Aufgaben

- [ ] `{Task 36 — lokale Verifikation}` — teilweise umgesetzt: `dotnet test Rezepte.Tests --filter "Category!=OsInterface"` ist grün (758/758) und `--list-tests`-Filter zeigen die erwarteten Mengen; der geforderte **grüne Komplettlauf von `Rezepte.Tests.Browser` steht noch aus**. Beobachtung: Der einzige in diesem Review ausgeführte Browser-Test (`DirectUrl_WithQuery_ShowsResults`, im Rahmen der Retry-Verifikation) schlug lokal fehl — persistent, mit dem auf 60 s angehobenen Navigations-Timeout auf `/login` (gleiche Flake-Signatur wie der dokumentierte Baseline-Flake, jetzt mit 60-s-Budget).
- [ ] `{Task 37 — CI-Verifikation}` — fehlt vollständig: ≥3 grüne Komplettläufe in `pr-staging-ci.yml` und `staging-ci.yml` inkl. Sichtprüfung der Retry-/Analyze-Steps und des Trace-Artefakt-Inhalts. Erfordert committeten Stand und echte Workflow-Läufe; aus dem Working Tree nicht prüfbar.

## Hinweise

- `Analyze-FlakyTestResults.ps1` wertet für die fail→pass-Sequenz das **effektive Lauf-Outcome** aus (Primär-Outcome wird durch ein `Passed`/`Failed`-Retry-Outcome ersetzt), statt wörtlich nur Primär-Outcomes zu vergleichen. Konsequenz: Ein durch den Retry geretteter Lauf schließt einen vorherigen Fehlschlag als Transition ab (zählt also ein Ereignis mehr als die Plan-Formel „fail→pass zwischen aufeinanderfolgenden **Primär**-Läufen"), verhindert aber zugleich die Doppelzählung „Retry-Recovery + grüner Folge-Primärlauf" desselben Vorfalls — im Code begründet und fachlich plausibel; Klassifikation bleibt Schwellwert-konform. Bei Abweichungswunsch an die wörtliche Plan-Formel wäre die Übergabe des Primär-Outcome statt des effektiven Outcomes an `LastOutcome` anzupassen.
- Der lokale persistente Fehlschlag von `DirectUrl_WithQuery_ShowsResults` (60-s-`GotoAsync`-Timeout auf `/login` trotz erfolgreichem Readiness-Poll) ist die gleiche Signatur wie der dokumentierte Baseline-Flake — die Härtung (60-s-Budget, Retry, Diagnostik) ist aktiv, löst den lokalen Auslöser aber nicht ursächlich. Vor dem CI-Nachweis (Task 37) lohnt ein Blick in `browser-app-logs/`/`playwright-traces/`, ob `/login` lokal systematisch langsam ist.
- `TempDirectoryCleanupTests.DeleteBestEffort_ProcessesToKill_KillsProcessAndDeletesDirectory` trägt zusätzlich `OsInterface` (echter Placeholder-Prozess) — plan-konforme Ergänzung über die geforderte Liste hinaus, in `os-interface-tests.md` dokumentiert.
- `Xunit.SkippableFact` ist in `Rezepte.Tests.csproj` weiterhin nicht referenziert — plan-konform (nur beim ersten tatsächlichen Quarantäne-Fall nachzurüsten; das Browser-Projekt referenziert es bereits für die `SkippableFact`-Browser-Tests).
