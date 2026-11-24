# Umgebungssensitive Tests (OsInterface/Flaky)

Reguläre Tests sind deterministische Unit-, Service- und Komponententests ohne echte
Betriebssystem-Berührung. Umgebungssensitive Tests prüfen genau diese
Umgebungskontakte — echte Prozessstarts, Sockets, Browser, externe Toolchains. Sie
bleiben erhalten, werden aber getrennt markiert, in einer best-effort-Lane
ausgeführt und ausgewertet.

Diese Konvention folgt der Softwareschmiede-Blaupause (PR #432), übertragen auf die
hier bestehende projektweise Trennung (`Rezepte.Tests` deterministisch,
`Rezepte.Tests.Browser` umgebungssensitiv) und `xunit.v3`.

## Klassifizierungskriterien

Maßgeblich ist, ob die **Assertion** eines Tests am Verhalten der Umgebung hängt —
nicht, ob irgendwo ein `Process.Start` oder `Task.Delay` vorkommt.

Audit-Signale für `OsInterface` (CI läuft auf `ubuntu-latest`):

- Kindprozess-Starts (`Process.Start`, `dotnet build`/`publish`/`test`-Aufrufe, pwsh)
- Sockets, `TcpListener`, freie-Port-Vergabe
- Playwright (echter Chromium gegen einen echten Kestrel-Prozess)
- externe Repositories/Checkouts oder Toolchains (z. B. `REZEPTE_EXTERNAL_PLUGINS_PATH`)
- feste Sleeps/`Task.Delay`-Warten ohne gebundene Deadline
- Temp-Dateisystem mit sperranfälligem Cleanup

Als triviales Fixture-Plumbing gelten dagegen (bleiben regulär): in-proc
`WebApplicationFactory`, EF InMemory, SQLite `:memory:`, Moq, bUnit sowie
Env-Mutationsklassen, die über `GoogleCredentialsEnvironmentCollection`
serialisiert in-process laufen und keine externe Fehlerquelle haben
(`GoogleCredentialsProviderTests`, `SettingsCredentialAvailabilityTests`,
`AIImportHandlerActivationTests`, `GeminiClientTests`).

## Lane-Struktur

In `pr-staging-ci.yml` und `staging-ci.yml` laufen zwei getrennte Test-Lanes:

| Lane | Job | Inhalt | Wirkung |
|------|-----|--------|---------|
| blockierend | `build-and-test` | `dotnet test Rezepte.Tests --filter "Category!=OsInterface"` + Coverage-Gate (70 %) | Fehlschlag blockiert PR-Merge und Pre-Release |
| best-effort | `browser-tests` | `dotnet test Rezepte.Tests.Browser` + `dotnet test Rezepte.Tests --filter "Category=OsInterface"` | `continue-on-error: true`; kein `needs:`-Eintrag in `version` — ein Fehlschlag blockiert weder Merge noch Gate, bleibt aber als rotes Job-Signal sichtbar |

Der `browser-tests`-Job trägt denselben Backmerge-Guard
(`needs.detect-backmerge.outputs.is_backmerge != 'true'`) wie die übrigen Jobs,
`timeout-minutes: 45` und führt nach einem Fehlschlag genau einen automatisierten
Retry der `Flaky`-markierten Fehlschläge sowie immer den
Flake-Statistik-Report-Schritt aus.

## Kategorien

Die Lane-Markierung erfolgt über xUnit-`[Trait]`-Attribute; die Konstanten liegen
in `Rezepte.Tests/TestHelpers/TestCategories.cs`:

```csharp
[Trait(TestCategories.Category, TestCategories.OsInterface)]  // "Category", "OsInterface"
[Trait(TestCategories.Category, TestCategories.Flaky)]        // "Category", "Flaky"
```

Aktuelle Vergaben:

- **Klassenebene `OsInterface`:** alle 10 Browser-Testklassen
  (`LoadingBarColorBrowserTests`, `LoadingBarDisabledBrowserTests`,
  `LoadingBarFormNavigationBrowserTests`, `LoadingBarSafetyTimeoutBrowserTests`,
  `LoadingBarVisibilityBrowserTests`, `PluginSettingsBrowserTests`,
  `RecipeSearchBrowserTests`, `SecurityTxtBrowserTests`, `ShareLinkBrowserTests`,
  `RegisterTests`) sowie `ContractExportScriptTests` in `Rezepte.Tests`
  (pwsh-/`dotnet build`-Subprozesse inkl. NuGet-Restore).
- **Methodenebene `OsInterface`:**
  `DeploymentDocumentationTests.FrameworkDependentLinuxPublish_ShouldProduceDocumentedEntrypointAndRuntimeFrameworks`
  (`dotnet publish -r linux-x64`-Subprozess),
  `PluginManagerTests.InitializeAsync_ShouldLoadPublishedExternalChefkochPluginWithoutAdjacentContractAssembly`
  und `…_ShouldLoadAllPublishedExternalOnlinePluginsWithoutAdjacentContractAssembly`
  (`dotnet publish`-Subprozess + `REZEPTE_EXTERNAL_PLUGINS_PATH`-Abhängigkeit) sowie
  `TempDirectoryCleanupTests.DeleteBestEffort_ProcessesToKill_KillsProcessAndDeletesDirectory`
  (echter Placeholder-Prozess).
- **`Flaky`:** `RecipeSearchBrowserTests.DirectUrl_WithQuery_ShowsResults`
  (dokumentierter Baseline-Flake: 30-s-`GotoAsync`-Timeout auf `/login`).

## `Flaky`-Kategorie

`Flaky` ist eine reine Markierungs-Kategorie für nachweislich umgebungsflaky
Tests. Sie berechtigt zum einmaligen automatischen Retry in der best-effort-Lane.

Vergaberegeln:

- `Flaky` wird **nur in Kombination mit `OsInterface`** vergeben — nie in der
  regulären Lane (abgesichert durch `FlakyConventionTests`, der per
  `<Compile Include>`-Link in beiden Testprojekten läuft und jeweils die eigene
  Assembly scannt; die Trait-Werte liest er versionsneutral via
  `CustomAttributeData`, weil `Xunit.TraitAttribute` keinen öffentlichen
  Trait-Reader bietet).
- `Flaky` wird nur bei einem **dokumentierten Flake-Ereignis** (konkreter
  CI-/Lokal-Fehlschlag mit Fehlerbild) oder einem Analyzer-Befund aus
  `Analyze-FlakyTestResults.ps1` vergeben — ein gewollter Feedback-Loop statt
  Blanket-Retry.
- Die Markierung wird entfernt, sobald die Ursache behoben ist.

## Lokale Befehle

```powershell
# Nur deterministische Tests
dotnet test Rezepte.Tests/Rezepte.Tests.csproj --filter "Category!=OsInterface"

# Nur umgebungssensitive Tests in Rezepte.Tests
dotnet test Rezepte.Tests/Rezepte.Tests.csproj --filter "Category=OsInterface"

# Nur Flaky-markierte Tests
dotnet test Rezepte.Tests.Browser/Rezepte.Tests.Browser.csproj --filter "Category=Flaky"
```

Lokale `dotnet test`-Aufrufe ohne Filter führen unverändert alles aus — die
Filter wirken nur in den Workflows.

## Automatischer Retry (Flaky)

Der automatische Einmal-Retry greift ausschließlich für `Flaky`-markierte Tests
und existiert nur in der best-effort-Lane — die blockierende Lane bleibt
retry-frei. Es gibt **kein** Blanket-Retry der Gesamtsuite.

**In der CI** (`browser-tests`-Job in `pr-staging-ci.yml` und `staging-ci.yml`):
Schlägt ein Testschritt fehl, läuft der Schritt `Retry flaky tests once`
(`if: failure()`, `shell: pwsh`) `scripts/Retry-FlakyTests.ps1` je Testprojekt:

1. Primär-TRX parsen, fehlgeschlagene Testnamen einsammeln (Theory-Datenzeilen
   werden auf den Methoden-FQN normalisiert).
2. Flaky-Menge via `dotnet test --no-build --list-tests --filter "Category=Flaky"`
   ermitteln.
3. Schnittmenge leer → „keine Flaky-Fehlschläge", Exit 0 (kein Retry; bei
   Build-Fehlschlag/fehlendem TRX ebenso, damit der `if: failure()`-Schritt
   nichts verschleiert).
4. Sonst genau ein gefilterter Re-Run:
   `--filter "(Category=Flaky)&(FullyQualifiedName~...)"` → Retry-TRX
   `<primärname>-flaky-retry.trx`.
5. Auswertung recovered/persistent: recovered wird als `::notice::`, persistent
   als `::warning::` annotiert; persistente Fehlschläge lassen den Schritt mit
   Exit 1 fehlschlagen (der Job bleibt durch `continue-on-error`
   nicht-blockierend).

Beide TRX-Dateien (Primär und Retry) wandern über die Artefakt-Globs
`test-results-browser*.trx`/`test-results-os-interface*.trx` ins Upload-Artefakt —
der Erstfehlschlag bleibt sichtbar.

**Lokal** manuell:

```powershell
dotnet test <projekt> --filter "Category=OsInterface" --logger "trx;LogFileName=lauf.trx"
./scripts/Retry-FlakyTests.ps1 -TestProject <projekt> -PrimaryTrx <verzeichnis>/lauf.trx
```

## Manuelle Wiederholung des Jobs

Die dokumentierte Rerun-Regel gilt **ausschließlich** für den `browser-tests`-Job
(best-effort-Lane) — niemals für `build-and-test` oder andere Gates:

```powershell
# Nur den browser-tests-Job eines Laufs wiederholen
gh run rerun <run-id> --job <job-id>
```

`<job-id>` steht im GitHub-Actions-Log des Laufs oder lässt sich über
`gh run view <run-id> --json jobs` ermitteln. Ein Fehlschlag in der
blockierenden Lane wird nie durch einen Rerun „grün wiederholt" — dort ist die
Ursache zu beheben.

## Flake-Statistik und Quarantäne

`scripts/Analyze-FlakyTestResults.ps1` wertet gesammelte TRX-Verzeichnisse aus:
Lauf-Gruppierung über die Namenskonvention `<name>.trx` (Primär) /
`<name>-flaky-retry.trx` (Retry desselben Laufs). Als Flake-Ereignis zählt
Fehlschlag-im-Primär + grün-im-Retry desselben Laufs oder Fehlschlag→grün in
aufeinanderfolgenden Primär-Läufen. Primär-Outcomes außerhalb Passed/Failed
(z. B. `NotExecuted`) gelten als fehlende Beobachtung: Sie zählen weder als
Ausführung noch als grüner Folgelauf und unterbrechen die Fehlschlag→grün-Sequenz
nicht.

Klassifikation je Test im Fenster `-MaxRuns` (Standard 20):

- **Quarantäne-Kandidat:** ≥ `QuarantineMinFlakeEvents` = 2 Flake-Ereignisse ODER
  Fehlrate ≥ `QuarantineMinFailureRate` = 0,2 bei ≥ `QuarantineMinExecutions` = 5
  Ausführungen.
- **beobachten:** ≥ 1 Flake-Ereignis ODER Fehlrate ≥ `WatchMinFailureRate` = 0,1.
- **stabil:** sonst.

```powershell
# Lokale Läufe
./scripts/Analyze-FlakyTestResults.ps1 -Path Rezepte.Tests/TestResults, Rezepte.Tests.Browser/TestResults

# CI-Artefakte
gh run download <run-id> -n test-results-browser-pr-a1 -D artifacts/browser-pr
./scripts/Analyze-FlakyTestResults.ps1 -Path artifacts/browser-pr
```

### Ausbaustufe 1: Empfehlung statt Auto-Issue

Das Akzeptanzkriterium des Auftrags formuliert „bei Schwellwertüberschreitung
entsteht ein Quarantäne-Issue". **Getroffene Festlegung (analog zur Blaupause):**
Der Analyzer ist ein reiner Report mit Exit 0 und gibt Quarantäne-Kandidaten als
`::warning::`-Annotation plus Job-Summary aus — ein Issue wird **empfohlen**,
nicht automatisch angelegt. Automatismus und Deduplizierung bleiben ein offener
Ausbau-Punkt.

Übergangsmechanismus für einen tatsächlichen Quarantäne-Fall: den betroffenen
Test manuell per `Assert.SkipUnless` mit Issue-Verweis quarantänieren — die
Laufzeit-Skip-API ist in `xunit.v3` eingebaut und steht in beiden Testprojekten
zur Verfügung (das früher verwendete `Xunit.SkippableFact`-Paket wurde mit der
Migration auf `xunit.v3` entfernt). Ein Trait-Entzug ist der schlechtere
Mechanismus: Er würde den Test in die blockierende Lane zurückholen.

## Härtungsmaßnahmen der Infrastruktur

- `RezepteAppFixture`: `StartupTimeoutSeconds` = 120; Kindprozess-Start mit
  begrenztem Retry (max. 3 Versuche, neuer Port je Versuch — heilt die
  TOCTOU-Portvergabe); Stdout/Stderr werden in einem begrenzten Ringpuffer
  mitgeschnitten und als Tail in Timeout-/Exit-Exceptions eingebettet; die
  vollständige Ausgabe liegt pro Versuch unter `browser-app-logs/` (CI-Artefakt).
  `DeleteTemporaryDatabase` läuft über `TempDirectoryCleanup` (Kill vor Delete,
  Retry/Backoff).
- `TempDirectoryCleanup` (`Rezepte.Tests/TestHelpers/`, per Compile-Link im
  Browser-Projekt): `DeleteBestEffort` für Verzeichnisse und
  `DeleteFileBestEffort` für Einzeldateien — nie werfend, Restfehler nur
  `Console.Error`, damit Cleanup nie einen Assert überdeckt.
- `BrowserTimeouts.NavigationTimeoutMilliseconds` = 60 000 als geteiltes
  Navigations-Default via `context.SetDefaultNavigationTimeout` an allen
  `NewContextAsync`-Stellen; `WaitUntilLoadingBarActiveAsync`/`…VisibleAsync`/
  `…ColorChangedAsync`-Defaults auf 5 s angehoben.
- `PlaywrightTraces`: deterministischer Trace-Pfad `<RepoRoot>/playwright-traces/`
  (statt Testhost-`bin/`), damit der Artefakt-Upload zuverlässig Inhalt findet.
- `GitHubStubServer.Dispose`: wartet neben dem Accept-Loop auch die
  `HandleClientAsync`-Handler-Tasks innerhalb des 5-s-Budgets ab.
- `ExportCleanupHostedServiceTests.StartAsync_ShouldCheckImmediatelyOnStartup`:
  `TaskCompletionSource`-Signal statt fester 100-ms-Warte.

## Timeout-Inventar (Festlegung je Fundstelle)

| Fundstelle | Wert | Festlegung |
|------------|------|------------|
| `RezepteAppFixture.StartupTimeoutSeconds` | 60 s → **120 s** | angepasst (Publish-/Migrations-/Kaltstart auf Last-Runner) |
| `GetFreeTcpPort()`-TOCTOU | Start-Retry ≤ 3×, neuer Port | angepasst (verlorenes Rennen = früher Kind-Exit, wird retried; Port-0+„Now listening"-Parsing nicht verfügbar, da Serilog-Konsole auf `Error` begrenzt) |
| Kindprozess-Ausgabe | Ringpuffer (je 100 Zeilen/Stream) + `browser-app-logs/` | angepasst (war verworfen) |
| `context.SetDefaultNavigationTimeout` | 60 s an allen 6 `NewContextAsync`-Stellen | angepasst (30-s-Default war die Stelle des Baseline-Flakes) |
| `WaitUntilLoadingBarActiveAsync` | Default 1 s → **5 s** | angepasst |
| `WaitUntilLoadingBarVisibleAsync`/`…ColorChangedAsync` | Default 2 s → **5 s** | angepasst (positive Zustands-Warte; `WaitForFunction` kehrt bei erfülltem Prädikat sofort zurück) |
| `LoadingBarPageObject` Edit-Button-Warte 10 s + 2-s-Warte im 5-Versuche-Retry | — | begründet belassen (gebundenes Retry-Muster, ~10 s effektives Budget) |
| `RegisterPage` `GotoAsync`/`WaitForSelector` 30 s | — | begründet belassen (bewusstes per-Call-Override) |
| `ShareLinkPageObject` 10–20-s-Waits | — | begründet belassen (großzügige positive Budgets) |
| `PluginSettingsPageObject` 30-s-Deadline-Click-Retry mit 5-s-Selector-Waits; Zeilen-Waits 15 s | — | begründet belassen (gebundenes Retry-Muster) |
| `SecurityTxtPageObject`/`LoginPage`/`BrowserPageObjectBase` 10-s-`WaitForURLAsync`/`WaitForSelectorAsync` | — | begründet belassen |
| `RecipeSearchBrowserTests` 15-s-Interaction-/20-s-Content-Budgets; `RegisterTests` 20-s/120-s-Budgets | — | begründet belassen |
| `LoadingBarFormNavigationBrowserTests` `WaitForTimeoutAsync(500)` | — | begründet belassen (Negativ-Assert ohne pollenbares Signal; Kommentar verweist auf diese Konvention) |
| `ExportCleanupHostedServiceTests` `Task.Delay(100)` | → `TaskCompletionSource` + `WaitAsync(5 s)` | angepasst (deterministisches Signal) |
| `ImportOrchestratorTests` `Task.Delay(75)` (Settle nach Zustands-Assert), `Task.Delay(25, ct)` in `WaitUntilAsync` (5-s-Deadline-Poll) | — | begründet belassen |
| `PluginUpdateServiceTests_Serialization` `Task.Delay(250)`-Negativwarten | — | begründet belassen (Gate durch `BlockingGitHubReleaseClient` gehalten; frühes Completen unmöglich) |
| `RecipeSearchComponentTests` `Task.Delay(50)`-Poll (5-s-Deadline), `Task.Delay(Timeout.Infinite, ct)` im Mock | — | begründet belassen (Deadline-Polling bzw. deterministischer in-flight-Simulator) |
| `ApplicationUpdatePreInstallHandlerTests` `Task.Delay(10)` im Fake | — | begründet belassen (simuliert asynchrone Arbeit, deterministisch) |
| `GitHubStubServer.Dispose` 5-s-Budget | + Handler-Task-Tracking | angepasst (Budget bewusst belassen, `AggregateException`-Schlucken dokumentiert) |
| `Directory.Delete`-/`File.Delete`-Teardowns | → `TempDirectoryCleanup` | angepasst (alle Teardown-Stellen; nie werfend) |
| `PluginManagerTests` `File.Delete(adjacentContract)` | — | begründet belassen (Arrange-Logik „Plugin ohne Vertragsassembly"; ein Fehlschlag soll den Test laut scheitern lassen) |
