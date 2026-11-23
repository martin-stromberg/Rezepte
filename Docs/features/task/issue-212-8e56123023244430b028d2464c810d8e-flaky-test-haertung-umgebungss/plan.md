# Umsetzungsplan: Flaky-Test-Härtung — umgebungssensitive Tests, Deadlines/Cleanup und Retry-Konvention (Issue #212)

> Modell-Hinweis: Devin-Umgebung ohne Codex-Modellauswahl; Sitzungsmodell geerbt
> (dokumentierte Abweichung, Rolle: Planung, Effort: high).

## Übersicht

Die CI-Testsuite wird gegen umgebungsbedingte Fehlschläge gehärtet: Die bestehende
projektweise Trennung (`Rezepte.Tests` deterministisch, `Rezepte.Tests.Browser`
umgebungssensitiv) wird auf Workflow-Ebene nachgezogen — in `pr-staging-ci.yml` und
`staging-ci.yml` entsteht ein eigener best-effort-Job `browser-tests`
(`continue-on-error: true`) neben dem blockierenden `build-and-test`-Job. Ergänzend
werden die auditierten Flake-Quellen behoben (TOCTOU-Portvergabe, Startup-Deadline,
feste Sleeps/Timeouts, best-effort-`Directory.Delete`/`File.Delete`, verworfene Kindprozess-Ausgabe,
Accept-Loop-Teardown), ein geteilter `TempDirectoryCleanup`-Helfer, die Skripte
`Retry-FlakyTests.ps1`/`Analyze-FlakyTestResults.ps1` sowie die
`OsInterface`/`Flaky`-Trait-Konvention mit Meta-Test analog zur
Softwareschmiede-Blaupause (PR #432) eingeführt. Betroffen sind ausschließlich
Testcode, Testinfrastruktur, CI-Workflows, Skripte und Doku — kein Produktcode.

## Scope-Klärung

| Feld | Festlegung |
|------|------------|
| Verarbeitungseinheit | Ein CI-Testdurchlauf in `pr-staging-ci.yml` (PR → `staging`) und `staging-ci.yml` (Push → `staging`), bestehend aus der deterministischen Menge (`Rezepte.Tests` ohne `Category=OsInterface`) und der umgebungssensitiven Menge (`Rezepte.Tests.Browser` komplett + `OsInterface`-markierte Einzelfälle in `Rezepte.Tests`). Einzeltests der best-effort-Menge sind die Beobachtungseinheit für Flake-Statistik und Quarantäne. |
| Auslösende Ereignisse | Pull Request gegen `staging` (Job `build-and-test` + neuer Job `browser-tests`), Push auf `staging` (analog; `version`/`prerelease` ohne `needs:` auf `browser-tests`); lokal `dotnet test` auf Entwicklermaschinen (Windows und Linux). |
| Erwartete Wirkung | Die blockierende Lane enthält nur deterministische Tests; ein Browser-Test-Fehlschlag blockiert weder PR-Merge noch Pre-Release-Gate. In der best-effort-Lane läuft genau ein automatisierter Re-Run der zuvor fehlgeschlagenen, `Flaky`-markierten Tests. Cleanup-Fehler werden mit Retry/Backoff behandelt und Restfehler nur protokolliert. TRX-Artefakte sind pro Job und Run-Attempt eindeutig benannt; ein Report-Schritt wertet die Flake-Rate aus und meldet Quarantäne-Kandidaten. Timeout-Fehlschläge der Browser-Infrastruktur tragen Phase/State/Error und gesicherte Kindprozess-Ausgabe. |
| Umgang mit fehlenden Objekten | `OsInterface`/`Flaky`-Traits + Meta-Test: **erzeugen** (existieren nicht im Repo; `Flaky` nur in Kombination mit `OsInterface`). `TempDirectoryCleanup`-Helfer: **erzeugen** in `Rezepte.Tests/TestHelpers/`, per `<Compile Include>`-Link ins Browser-Projekt geteilt (Muster wie `RepositoryPaths.cs`). `scripts/Retry-FlakyTests.ps1`, `scripts/Analyze-FlakyTestResults.ps1` + `scripts/lib/`-Helfer: **erzeugen** (Blaupausen-Adaptation). Quarantäne-Issues: Übergangsmechanismus offen (siehe Offene Punkte). Repo-Root-`playwright-traces/`: **erzeugen** zur Laufzeit durch Code (deterministischer Pfad via `RepositoryPaths`). |

### Scope-Ausschlüsse

| # | Einschränkung / Ausschluss | Begründung | Status |
|---|----------------------------|------------|--------|
| 1 | Keine vollständige Determinisierung echter UI-/Prozess-Tests | Zielbild ist „selten, lokalisiert, selbstdiagnostizierend"; Fake-Umbau nur auditgesteuert in Einzelfällen | bestätigt (Issue-Vorgabe/Nicht-Ziel, 2026-11-22) |
| 2 | Kein Blanket-Retry der Gesamtsuite | Retry nur einmalig, nur best-effort-Lane, nur `Flaky`-markierte Fehlschläge | bestätigt (Issue-Vorgabe/Nicht-Ziel, 2026-11-22) |
| 3 | Keine Windows-zentrische Audit-Heuristik | CI läuft auf `ubuntu-latest`; relevante Signale: Subprozess/Sockets, Playwright-Timeouts, feste Sleeps, `Task.Delay`, Temp-FS, freie Ports | bestätigt (Issue-Vorgabe/Nicht-Ziel, 2026-11-22) |
| 4 | Keine fachliche Änderung am Produktcode (`Rezepte.Web`, Plugins) | Aufgabe betrifft Testinfrastruktur, Testcode und CI-Workflows | bestätigt (Issue-Vorgabe/Nicht-Ziel, 2026-11-22) |
| 5 | Keine Änderung der übrigen Gates (Format, Security-Scan, Static Analysis, Backmerge, Contract-Export, 70-%-Coverage) | Nur Test-Lane-Struktur und TRX-/Coverage-Artefaktbehandlung werden angepasst | bestätigt (Issue-Vorgabe/Nicht-Ziel, 2026-11-22) |
| 6 | Kein `OsInterface`-Split auf Solution-Ebene und kein Umbau von `release.yml` | Projektweise Trennung existiert als Basis; `release.yml` ruft Tests bereits projektweise getrennt (beide blockierend) auf — außerhalb des benannten Scopes | bestätigt (Issue-Vorgabe/Nicht-Ziel, 2026-11-22) |

## Designentscheidungen

| Komponente / Bereich | Gewählter Ansatz | Begründung |
|----------------------|-----------------|------------|
| Lane-Trennung (Job vs. Step) | **Separater Job `browser-tests`** mit `continue-on-error: true` (job-level) in beiden Workflows | Entspricht der Blaupause (`os-interface-tests`-Job): sauberes Job-Signal mit sichtbarer Failure-Annotation statt verstecktem Step-Fehlschlag in grünem Job; die dokumentierte `gh run rerun --job <id>`-Regel braucht eine Job-Ebene; die blockierende Lane entfällt Browser-Build, Playwright-Install und Publish → schnelleres Gate. Gegenvariante `continue-on-error`-Step im selben Job würde Build/Publish/Playwright-Install sparen, verwässert aber Signal und Rerun-Regel. |
| Trait-Umsetzung (xUnit 2.9.3) | **`[Trait("Category", …)]`-Attribute + `TestCategories`-Konstanten** statt Custom-Attribute der Blaupause | Die Blaupause nutzt xUnit v3 (`ITraitAttribute.GetTraits()`); in xUnit 2 ist `TraitAttribute` `sealed` und `Xunit.Sdk.ITraitAttribute` ein reines Marker-Interface — ein Custom-Attribute bräuchte einen eigenen `ITraitDiscoverer` ohne Mehrwert. Der Meta-Test liest Trait-Werte per `CustomAttributeData.ConstructorArguments` (kein Instanziieren nötig). |
| Markierungsumfang | Alle 10 Browser-Testklassen erhalten `[Trait("Category","OsInterface")]` (Klassenebene); `Flaky` zunächst nur `RecipeSearchBrowserTests.DirectUrl_WithQuery_ShowsResults` (Methodenebene, dokumentierter Baseline-Flake). In `Rezepte.Tests`: `ContractExportScriptTests` (Klassenebene), `DeploymentDocumentationTests.FrameworkDependentLinuxPublish_…` und die zwei externen `PluginManagerTests`-Methoden (Methodenebene) erhalten `OsInterface` | OsInterface = „umgebungssensitiv" als einheitliche Semantik; `Flaky` folgt der Blaupausen-Regel „nur bei dokumentiertem Flake-Ereignis" — neue Flakes werden im Fehlerfall untersucht und dann markiert (gewollter Feedback-Loop statt Blanket-Retry). Env-Mutationsklassen (`GoogleCredentialsProviderTests`, `SettingsCredentialAvailabilityTests`, `AIImportHandlerActivationTests`, `GeminiClientTests`) bleiben **blockierend**: in-proc, über `GoogleCredentialsEnvironmentCollection` serialisiert, keine externe Fehlerquelle. |
| TOCTOU bei `GetFreeTcpPort()` | **Begrenzter Start-Retry**: Port frei wählen (bestehender Mechanismus) → Kindprozess starten → bei frühem Exit (inkl. Bind-Fehler) oder Nicht-Readiness bis zu 3 Versuche mit neuem Port; Kindprozess-Ausgabe wird mitgeschnitten | Port-0 + „Now listening on"-Parsing ist **nicht** verfügbar: Serilog-Konsole ist auf `Error` begrenzt und `Microsoft`-Kategorien auf `Warning` (`LoggingExtensions.cs` Z. 33/37) — die Bind-Logzeile erscheint nicht auf stdout, und Produktcode-Änderungen sind Scope-Ausschluss. Die Race bleibt formal, ist aber durch Retry + mitgeschnittener Ausgabe erkennbar und selbstheilend. |
| Kindprozess-Diagnostik | **In-Memory-Ringpuffer** (letzte N Zeilen je Stream) statt wegwerfender Handler; bei Timeout/Early-Exit landet der Tail in der Exception, optional Schreiben der vollständigen Ausgabe in eine Log-Datei unter `browser-app-logs/` (Artefakt) | Erfüllt AC 8 (Phase/State/Error) ohne Produktcode-Eingriff; Pipes müssen weiterhin kontinuativ gedraint werden (Kommentar Z. 212–214 bleibt fachlich gültig). |
| Playwright-Trace-Pfad | **Deterministischer Pfad via `RepositoryPaths.FindRepositoryRoot()`** — zentrale Hilfsfunktion liefert `<RepoRoot>/playwright-traces/`; alle vier Schreibstellen nutzen sie | Verifizierter Befund: Traces landen im `bin/`-Ausgabeverzeichnis, der Workflow lädt aber `playwright-traces/` vom Repo-Root hoch. Code-Fix ist robuster als ein Workflow-Glob, weil der Testhost-Working-Directory implizit bleiben kann. `/playwright-traces/` wird in `.gitignore` ergänzt. |
| Coverage in der best-effort-Lane | **Kein `--collect`** im `browser-tests`-Job; 70-%-Gate bleibt unverändert auf `Rezepte.Tests` im `build-and-test`-Job | Verifiziert per Code-Inspektion: Der `Rezepte.Web`-Kindprozess läuft ohne Collector-Instrumentierung → 0 %-Beitrag zur Produktcode-Abdeckung; die Browser-Cobertura deckte nur die Testassembly selbst ab. Das Gate misst damit weiter die deterministische Lane; der dokumentierte Beleg steht in `inventory/workflows.md`. |
| Quarantäne-Mechanismus (Erstausbaustufe) | **Report-Empfehlung statt Auto-Issue**: `Analyze-FlakyTestResults.ps1` bleibt reiner Report (Exit 0), gibt Quarantäne-Kandidaten als `::warning::`-Annotation + Job-Summary aus | Entspricht der Blaupause („Quarantäne-Issues werden empfohlen statt automatisch angelegt"); Automatismus/Deduplizierung bleibt Offener Punkt. |
| `File.Delete`-Teardowns in `finally`-Blöcken | **`TempDirectoryCleanup.DeleteFileBestEffort(path)`** — derselbe Retry/Backoff-/Nie-Werfen-Vertrag wie `DeleteBestEffort`, auf Einzeldateien angewandt | Ein werfender `File.Delete` aus `finally` propagiert und maskiert den Testausgang — identisches Fehlerbild wie `Directory.Delete` (AC 6). Betroffen (verifiziert): `GoogleCredentialsProviderTests` Z. 148/245, `GeminiClientTests` Z. 44/90, `SettingsCredentialAvailabilityTests` Z. 47, `AIImportHandlerActivationTests` Z. 78, `GitHubReleaseClientTests` Z. 70 (dessen `File.Exists`-Vorprüfung entfällt — der Helfer toleriert fehlende Dateien) sowie `SqliteFileWebApplicationFactory` Z. 53. Ausnahme: `PluginManagerTests` Z. 767 `File.Delete(adjacentContract)` bleibt **werfend** — es ist Arrange-Logik (Entfernen der Vertrags-DLL simuliert „Plugin ohne Vertragsassembly"); ein stilles Ignorieren würde das Testszenario verfälschen, ein Fehlschlag soll den Test laut scheitern lassen. |
| Explizite Playwright-Timeouts | **Geteiltes 60-s-Navigations-Default** via `context.SetDefaultNavigationTimeout` (Konstante in neuem `BrowserTimeouts`-Helfer) + Anhebung der 2-s-`WaitForFunction`-Defaults auf 5 s; übrige explizite Budgets (10–30 s) **begründet belassen** | Der dokumentierte Baseline-Flake (Lauf 4/5) war ein 30-s-`GotoAsync`-Default-Timeout auf `/login` — unter Parallel-Last (bis zu 3 App-Prozesse + Chromium) zu knapp. `SetDefaultNavigationTimeout` an den sechs `NewContextAsync`-Stellen deckt alle `GotoAsync` ohne explizite Optionen ab (`LoadingBarPageObject` Z. 105, `LoginPage` Z. 40, `BrowserPageObjectBase` Z. 73/88, `PluginSettingsPageObject` Z. 65/82, `ShareLinkPageObject` Z. 158/217, `RegisterTests` Z. 218/229/240/298) — 60 s bleibt unter der 120-s-Host-Deadline. `WaitUntilLoadingBarVisibleAsync`/`…ColorChangedAsync` (2 s) folgen der `ActiveAsync`-Begründung (positive Warte, längeres Budget ändert nur den Worst Case). Belassen und im Klassifikations-Dokument begründet: `RegisterPage` Z. 61/62 (explizite 30 s, bewusstes per-Call-Override), `ShareLinkPageObject` 10–20 s, `RecipeSearchBrowserTests` 15-s-Interaction-/20-s-Content- und `RegisterTests` 20-s/120-s-Budgets, alle 10-s-`WaitForURLAsync`/`WaitForSelectorAsync` (`LoginPage`, `SecurityTxtPageObject`, `BrowserPageObjectBase`), die 5-s-Warte im 30-s-Deadline-Click-Retry (`PluginSettingsPageObject` Z. 90) und die 2-s-Warte im 5-Versuche-Retry (`LoadingBarPageObject` Z. 191) — großzügige positive Budgets bzw. gebundene Retry-Muster, Restrisiko über die `Flaky`-Retry-Konvention abgedeckt. |
| Browser-Collections-Parallelität | **Beibehalten** (3 Collections potenziell parallel: bis zu 3 App-Kindprozesse + Chromium) | Serialisierung (`DisableTestParallelization`) wäre die stärkste Last-Reduktion, verdoppelt aber die Laufzeit der ohnehin best-effort-Lane; zunächst Härtung über Deadlines/Retry/Diagnostik — bei anhaltenden Flakes ist Serialisierung der dokumentierte Folgeschritt. |

## Programmabläufe

### PR-CI-Ablauf (`pr-staging-ci.yml`)

1. `detect-backmerge` läuft unverändert zuerst.
2. `static-checks` (unverändert), `build-and-test` und der neue `browser-tests`-Job laufen parallel (`needs: detect-backmerge` nur).
3. `build-and-test`: Checkout → Setup .NET → `dotnet restore Rezepte.sln` → `dotnet build Rezepte.Web` → `dotnet build Rezepte.Tests` (browser-Build/Playwright-Install/Publish entfallen hier) → `dotnet test Rezepte.Tests/Rezepte.Tests.csproj -c Release --no-build --filter "Category!=OsInterface" --collect:"XPlat Code Coverage" --logger "trx;LogFileName=test-results-a${{ github.run_attempt }}.trx"` → ReportGenerator → 70-%-Gate → Contract-Export → Uploads (`coverage-report-pr`, `test-results-pr-a${{ github.run_attempt }}` nur `Rezepte.Tests/TestResults`).
4. `browser-tests` (`needs: detect-backmerge`, `if: needs.detect-backmerge.outputs.is_backmerge != 'true'` — derselbe Backmerge-Guard wie `static-checks`/`build-and-test`, damit die 45-Minuten-Lane bei reinen Backmerge-Pushes nicht sinnlos anläuft; `continue-on-error: true`, `timeout-minutes: 45`): Checkout → Setup .NET → Restore → `dotnet build Rezepte.Web` → `dotnet build Rezepte.Tests` → `dotnet build Rezepte.Tests.Browser` → Playwright-Install → `dotnet publish Rezepte.Web` (Step mit `id: publish`) → Step „Test (browser)": `dotnet test Rezepte.Tests.Browser -c Release --no-build --logger "trx;LogFileName=test-results-browser-a${{ github.run_attempt }}.trx"` → Step „Test (OsInterface subset)": `dotnet test Rezepte.Tests -c Release --no-build --filter "Category=OsInterface" --logger "trx;LogFileName=test-results-os-interface-a${{ github.run_attempt }}.trx"` mit `if: ${{ !cancelled() && steps.publish.outcome == 'success' }}` — läuft nach einem Browser-Test-Fehlschlag weiter, aber nicht nach einem Build-/Publish-Fehlschlag (`--no-build` hätte dann ohnehin nichts zu testen) → Step „Retry flaky tests once" (`if: failure()`, `shell: pwsh`): `Retry-FlakyTests.ps1` für beide Primär-TRX, aggregierter Exit-Code → Step „Analyze flaky test results" (`if: always()`, `shell: pwsh`): `Analyze-FlakyTestResults.ps1 -Path Rezepte.Tests/TestResults, Rezepte.Tests.Browser/TestResults` → Uploads (`test-results-browser-pr-a${{ github.run_attempt }}` mit Glob `test-results-browser*.trx` + `test-results-os-interface*.trx` inkl. `*-flaky-retry.trx`; `playwright-traces-pr-a${{ github.run_attempt }}` mit Pfad `playwright-traces/`; optional `browser-app-logs`).
5. `no-pending-backmerge` unverändert; `browser-tests` ist **nicht** in `needs:` irgendeines Gate-Jobs → ein Fehlschlag kann weder Merge noch Release-Kette blockieren.

### Staging-CI-Ablauf (`staging-ci.yml`)

Analog `pr-staging-ci.yml`; `version` behält `needs: [detect-backmerge, static-checks, build-and-test]` — `browser-tests` fehlt bewusst in `needs:` (Blaupausen-Muster, Kommentar im Workflow). `prerelease` bleibt unverändert.

### Retry-Ablauf (`scripts/Retry-FlakyTests.ps1`)

1. Primär-TRX parsen (`lib/Trx.ps1`): `Failed`-Ergebnisse einsammeln, Theory-Datenzeilen auf Methoden-FQN normalisieren.
2. `Flaky`-Menge des Projekts via `dotnet test --no-build --list-tests --filter "Category=Flaky"` ermitteln (`lib/TestDiscovery.ps1`).
3. Schnittmenge leer → „keine Flaky-Fehlschläge", Exit 0 (kein Retry; fehlendes TRX gilt ebenfalls als keine Flaky-Fehlschläge, damit ein `if: failure()`-Schritt bei Build-Fehlschlag nichts verschleiert).
4. Sonst genau ein Re-Run: `dotnet test <projekt> -c Release --no-build --filter "(Category=Flaky)&(FullyQualifiedName~…|…)" --logger "trx;LogFileName=<primär>-flaky-retry.trx"`.
5. Retry-TRX auswerten: `Passed` = recovered; persistente Fehlschläge → Exit 1 (ehrliches Signal — der Job bleibt trotzdem non-blocking über job-level `continue-on-error`).

### Analyse-Ablauf (`scripts/Analyze-FlakyTestResults.ps1`)

1. TRX-Verzeichnisse rekursiv scannen; `<name>.trx` = Primärlauf, `<name>-flaky-retry.trx` = zugehöriger Retry (Lauf-Gruppierung über Dateinamen).
2. Flake-Ereignis = Fehlschlag im Primär-TRX + grün im Retry-TRX desselben Laufs, oder fail→pass zwischen aufeinanderfolgenden Primär-Läufen (sortiert nach TRX-`Times start`, Fallback `LastWriteTimeUtc`); `NotExecuted`/Skip-Outcomes zählen als fehlende Beobachtung.
3. Klassifikation je Test im Fenster `-MaxRuns` (Standard 20): Quarantäne-Kandidat (≥ `QuarantineMinFlakeEvents` = 2 Flake-Ereignisse ODER Fehlrate ≥ `QuarantineMinFailureRate` = 0,2 bei ≥ `QuarantineMinExecutions` = 5 Läufen), beobachten (≥ 1 Ereignis oder Fehlrate ≥ `WatchMinFailureRate` = 0,1), stabil sonst.
4. Reiner Report: Exit immer 0; Quarantäne-Empfehlung als `::warning::`-Annotation in GitHub Actions.

### Cleanup-Ablauf (`TempDirectoryCleanup`)

**`DeleteBestEffort(root, processesToKill, normalizeAttributes)`** — Verzeichnis-Variante:

1. Optionale registrierte `Process`-Objekte zuerst `Kill(entireProcessTree: true)` + `WaitForExit` (10 s Budget); bereits disposed/fehlende Prozesse toleriert.
2. Optional `normalizeAttributes`: Dateiattribute rekursiv auf `Normal` setzen (Read-only-Fälle); Fehler dabei sind nur Vorbereitungs-Verlust, kein Abbruch.
3. `Directory.Delete(root, recursive: true)` mit Retry: max. 5 Versuche, `Thread.Sleep(100 * attempt)` Backoff, nur `IOException`/`UnauthorizedAccessException` werden retried.
4. Restfehler nach letztem Versuch nur `Console.Error` — der Helfer wirft **nie** (kein `AggregateException`-Overlay aus `Dispose`).

**`DeleteFileBestEffort(path)`** — Datei-Variante für die `finally`-`File.Delete`-Teardowns auf `Path.GetTempFileName()`/`GetTempPath()`-Dateien:

1. Nichtexistenter Pfad → sofortiger Erfolg (No-op; die `File.Exists`-Vorprüfung an Aufruferstellen entfällt).
2. `File.Delete(path)` mit demselben Retry/Backoff (max. 5 Versuche, `100 * attempt` ms, nur `IOException`/`UnauthorizedAccessException`); optional Attribut-Normalisierung auf der Einzeldatei (Read-only).
3. Restfehler nur `Console.Error` — wirft **nie**, sodass ein `finally`-Delete den Testausgang nicht mehr maskieren kann.

Beteiligte Klassen/Komponenten: `RezepteAppFixture`, `GitHubStubServer`, `LoadingBarPageObject`, `BrowserPageObjectBase`, `PluginSettingsPageObject`, `RegisterPage`, `ShareLinkPageObject`, `LoginPage`, `BrowserTimeouts`, `TempDirectoryCleanup`, `TestCategories`, `FlakyConventionTests`, die 10 Browser-Testklassen, `ContractExportScriptTests`, `DeploymentDocumentationTests`, `PluginManagerTests`, `ExportCleanupHostedServiceTests`, `SqliteFileWebApplicationFactory`, `GoogleCredentialsProviderTests`, `GeminiClientTests`, `SettingsCredentialAvailabilityTests`, `AIImportHandlerActivationTests`, `GitHubReleaseClientTests`, die übrigen D/T-Testklassen in `Rezepte.Tests`, beide Workflow-Dateien, vier neue Skripte.

## Neue Klassen

| Klasse | Typ | Zweck |
|--------|-----|-------|
| `TestCategories` (`Rezepte.Tests/TestHelpers/`, per `<Compile Include>` ins Browser-Projekt) | statische Klasse | Konstanten `Category`/`OsInterface`/`Flaky` für `[Trait]`-Werte und Filterausdrücke |
| `TempDirectoryCleanup` (`Rezepte.Tests/TestHelpers/`, per `<Compile Include>` ins Browser-Projekt) | `internal static` Klasse | `DeleteBestEffort(root, processesToKill, normalizeAttributes)` für Verzeichnisse + `DeleteFileBestEffort(path)` für Einzeldateien — Kill vor Delete, Retry/Backoff, Restfehler nur Diagnose (Blaupause: `Softwareschmiede.TestSupport.TempDirectoryCleanup`) |
| `BrowserTimeouts` (`Rezepte.Tests.Browser/Infrastructure/`) | `internal static` Klasse | Zentrale Konstante `NavigationTimeoutMilliseconds = 60000` für das geteilte Navigations-Default; Aufruf `context.SetDefaultNavigationTimeout(...)` an allen `NewContextAsync`-Stellen (kein Link nötig — nur Browser-Projekt) |
| `FlakyConventionTests` (`Rezepte.Tests/TestHelpers/` oder `Testing/`, per `<Compile Include>` ins Browser-Projekt) | Testklasse | Meta-Test: jede Methode/Klasse mit `Category=Flaky`-Trait muss `Category=OsInterface` tragen; Auswertung via `CustomAttributeData` (xUnit-2-tauglich) |
| `TempDirectoryCleanupTests` (`Rezepte.Tests/TestHelpers/`) | Testklasse | Unit-Tests für den Helfer (Retry-Verhalten, Nie-Werfen-Garantie, Prozess-Kill-Reihenfolge, nichtexistenter Pfad) — nur in `Rezepte.Tests`, kein Link |
| `PlaywrightTraces` (oder Methode in `BrowserPageObjectBase`) (`Rezepte.Tests.Browser/Infrastructure/`) | statischer Helfer | Liefert `<RepoRoot>/playwright-traces/` via `RepositoryPaths.FindRepositoryRoot()` + Dateinamen-Konvention `{Prefix}-{yyyyMMdd-HHmmss}-{Guid:N}.zip` |
| `scripts/Retry-FlakyTests.ps1` | pwsh-Skript | Einmal-Re-Run `Flaky`-markierter Fehlschläge (Parameter `-TestProject`, `-PrimaryTrx`, `-Configuration Release`, `-RetryTrxFileName`) |
| `scripts/Analyze-FlakyTestResults.ps1` | pwsh-Skript | TRX-Flake-Statistik + Quarantäne-Empfehlung, immer Exit 0 |
| `scripts/lib/Trx.ps1` | pwsh-Modul (Dot-Sourcing) | TRX-Parsing: `Get-TrxTestResults`, `Get-TrxRunStartTime` |
| `scripts/lib/TestDiscovery.ps1` | pwsh-Modul (Dot-Sourcing) | `Get-FlakyTestNames` via `dotnet test --list-tests --filter "Category=Flaky"` |

## Änderungen an bestehenden Klassen

### `RezepteAppFixture` (Test-Fixture, `Rezepte.Tests.Browser/Infrastructure/`)

- **Geänderte Konstanten:** `StartupTimeoutSeconds` 60 → **120** (Host-Deadline oberhalb realistischer Publish-/Migrations-/Kaltstart-Dauer auf dem Last-Runner; gemessene Dauer in der Implementierung gegenprüfen).
- **Geänderte Methoden:** `StartApplicationProcess` — Start in einen begrenzten Retry-Loop (max. 3 Versuche, neuer Port je Versuch) gehüllt; `GetFreeTcpPort` bleibt, ein verlorenes TOCTOU-Rennen äußert sich als früher Kind-Exit und wird retried. Stdout/Stderr-Handler füllen einen Ringpuffer (statt `(_, _) => { }`), `BeginOutputReadLine` bleibt.
- **Geänderte Methoden:** `WaitUntilReadyAsync` — `TimeoutException`/`InvalidOperationException` werden um Phase, vergangene Zeit, Exit-Code (falls vorhanden) und den Ringpuffer-Tail erweitert; optionale Vollausgabe in eine Log-Datei unter `browser-app-logs/`.
- **Geänderte Methoden:** `DeleteTemporaryDatabase` — `Directory.Delete` wird durch `TempDirectoryCleanup.DeleteBestEffort(_tempDirectory, processesToKill: [_process])` ersetzt (Kill vor Delete, Retry/Backoff).
- `StopApplicationProcess` (`Kill` + `WaitForExit` in `try/finally`) bleibt unverändert.
- Abgeleitete Fixtures (`ConfiguredRezepteAppFixture`, `PluginSettingsAppFixture`, `RegisterTestsAppFixture`, `DisabledFixture`, `ShortMaxVisibleDurationFixture`) erben das Verhalten — keine eigenen Änderungen.

### `GitHubStubServer` (Infrastruktur)

- **Geänderte Methoden:** `Dispose` — zusätzlich Mitverfolgung der fire-and-forget-`HandleClientAsync`-Tasks (z. B. `ConcurrentBag<Task>`), Abwarten gemeinsam mit `_acceptLoop` innerhalb des bestehenden 5-s-Budgets; Restfehler weiter nur geschluckt. Das 5-s-Budget bleibt (begrenzte Warte ist gewollt, `AggregateException`-Schlucken dokumentiert).

### `LoadingBarPageObject` (Page Object)

- **Geänderte Methoden:** `WaitUntilLoadingBarActiveAsync` — Default `timeoutMilliseconds` 1000 → **5000** (Aufrufer mit expliziten Budgets bleiben; nur der implizit kurze Default war flake-anfällig).
- **Geänderte Methoden:** `WaitUntilLoadingBarVisibleAsync`, `WaitUntilLoadingBarColorChangedAsync` — Default `timeoutMilliseconds` 2000 → **5000** (gleiche Begründung: positive Zustands-Warte unter CI-Last; `WaitForFunction` kehrt bei erfülltem Prädikat sofort zurück, ein längeres Budget verschiebt nur den Worst Case).
- **Geänderte Methoden:** `CreateAsync` — nach `NewContextAsync` ein `context.SetDefaultNavigationTimeout(BrowserTimeouts.NavigationTimeoutMilliseconds)` (60 s); deckt u. a. `GotoAsync` (Z. 105) ab — die Stelle des dokumentierten Baseline-Flakes.
- **Geänderte Methoden:** `DisposeAsync` — Trace-Pfad über den neuen `PlaywrightTraces`-Helfer (Repo-Root statt Testhost-CWD).
- **Begründet belassen:** 10-s-Edit-Button-Warte (Z. 180) und 2-s-Warte im 5-Versuche-Retry (Z. 191) in `SubmitInteractiveShoppingListItemAsync` — gebundenes Retry-Muster mit ~10 s effektivem Budget; Dokumentation im Klassifikations-Dokument.

### `BrowserPageObjectBase`, `PluginSettingsPageObject`, `RegisterPage`, `ShareLinkPageObject`, `LoginPage` (Page Objects)

- **Geänderte Methoden:** `DisposeAsync` (wo vorhanden) — gleiche Trace-Pfad-Umstellung auf `PlaywrightTraces`-Helfer.
- **Geänderte Methoden:** Context-Erzeugung — `context.SetDefaultNavigationTimeout(BrowserTimeouts.NavigationTimeoutMilliseconds)` an allen `NewContextAsync`-Stellen ergänzen: `BrowserPageObjectBase` Z. 54, `PluginSettingsPageObject` Z. 46, `RegisterPage` Z. 42, `ShareLinkPageObject` Z. 212/228 (zusätzlich zu `LoadingBarPageObject` Z. 70). Wirkung: alle `Page.GotoAsync`-Aufrufe ohne explizite Optionen (`LoadingBarPageObject` Z. 105, `LoginPage` Z. 40, `BrowserPageObjectBase` Z. 73/88, `PluginSettingsPageObject` Z. 65/82, `ShareLinkPageObject` Z. 158/217, `RegisterTests` Z. 218/229/240/298) laufen mit 60 s statt dem 30-s-Playwright-Default; explizite per-Call-Optionen (z. B. `RegisterPage` Z. 61 mit 30 s) haben Vorrang und bleiben unverändert.
- **Begründet belassen:** explizite Budgets — `RegisterPage` Z. 61/62 (30 s `GotoAsync`/`WaitForSelector`, bewusstes per-Call-Override), `ShareLinkPageObject` Z. 159/170/174/179/260/263/274 (10–20 s), `SecurityTxtPageObject` 10 s, `LoginPage`/`BrowserPageObjectBase`/`PluginSettingsPageObject` 10-s-`WaitForURLAsync`, `PluginSettingsPageObject` Z. 90 (5-s-Warte im 30-s-Deadline-Click-Retry) sowie `RecipeSearchBrowserTests` (15-s-Interaction-/20-s-Content) und `RegisterTests` (20-s-Content/120-s-DemoData) — großzügige positive Budgets bzw. gebundene Retry-Muster; Restrisiko über die `Flaky`-Retry-Konvention abgedeckt, Festlegung im Klassifikations-Dokument dokumentiert.

### `LoadingBarFormNavigationBrowserTests` (Testklasse)

- `WaitForTimeoutAsync(500)` (Z. 70) **begründet belassen** — Negativ-Assert ohne pollenbares positives Signal; Kommentar wird um den Verweis auf die Konvention ergänzt. Keine Codeänderung außer `[Trait("Category","OsInterface")]` auf Klassenebene.

### Browser-Testklassen (10 Klassen)

- Klassenebene: `[Trait(TestCategories.Category, TestCategories.OsInterface)]` ergänzen.
- `RecipeSearchBrowserTests.DirectUrl_WithQuery_ShowsResults`: zusätzlich `[Trait(TestCategories.Category, TestCategories.Flaky)]` (Methodenebene, dokumentierter Baseline-Flake Lauf 4/5).

### `ContractExportScriptTests` (Testklasse, `Rezepte.Tests/`)

- `[Trait("Category","OsInterface")]` auf Klassenebene (pwsh-/`dotnet build`-Subprozesse inkl. NuGet-Restore — stärkster OsInterface-Kandidat).
- `Directory.Delete`-Aufrufe (Z. 464, 546) → `TempDirectoryCleanup.DeleteBestEffort`.

### `DeploymentDocumentationTests` (Testklasse)

- `[Trait("Category","OsInterface")]` auf `FrameworkDependentLinuxPublish_…` (Methodenebene; `dotnet publish -r linux-x64`-Subprozess).
- `Directory.Delete` (Z. 104) → `TempDirectoryCleanup.DeleteBestEffort`.

### `PluginManagerTests` (Testklasse)

- `[Trait("Category","OsInterface")]` auf den beiden externen Methoden `…_ShouldLoadPublishedExternalChefkochPluginWithoutAdjacentContractAssembly` und `…_ShouldLoadAllPublishedExternalOnlinePluginsWithoutAdjacentContractAssembly` (Methodenebene — `dotnet publish`-Subprozess + `REZEPTE_EXTERNAL_PLUGINS_PATH`-Abhängigkeit).
- `Directory.Delete` (Z. 91, 682, 699) → `TempDirectoryCleanup.DeleteBestEffort`.

### `ExportCleanupHostedServiceTests` (Testklasse)

- **Geänderte Methode:** `StartAsync_ShouldCheckImmediatelyOnStartup` — feste `Task.Delay(100)`-Warte (Z. 79, echte Flake-Quelle) wird durch deterministisches Signal ersetzt: `TaskCompletionSource` im Moq-`Callback` von `IsCleanupDueAsync`, dann `await tcs.Task.WaitAsync(TimeSpan.FromSeconds(5))` vor `StopAsync`/Verify.

### D/T-Testklassen in `Rezepte.Tests` (Cleanup-Migration)

- `ExportCleanupServiceTests` (Z. 51), `PluginDirectoryRegistryTestsBase` (Z. 66), `PluginPackageValidatorTests` (Z. 78), `UpdateBackupServiceTests` (Z. 176) — Umstellung der `Directory.Delete`-Teardowns auf `TempDirectoryCleanup.DeleteBestEffort`.
- `SqliteFileWebApplicationFactory` (`Dispose`, Z. 53 `File.Delete`) — Umstellung auf `TempDirectoryCleanup.DeleteFileBestEffort`.
- Begründet belassen (Dokumentation im Klassifikations-Dokument): `Task.Delay(75)` in `ImportOrchestratorTests` (Z. 243, kurze Settle-Warte nach Zustands-Assert), `Task.Delay(250)`-Negativwarten in `PluginUpdateServiceTests_Serialization` (Z. 36, 77 — Gate wird von `BlockingGitHubReleaseClient` gehalten, frühes Completen ist unmöglich), `Task.Delay(50)`-Poll mit 5-s-Deadline in `RecipeSearchComponentTests` (bereits Deadline-Polling), `Task.Delay(10)` im Fake in `ApplicationUpdatePreInstallHandlerTests` (simuliert asynchrone Arbeit, deterministisch).

### `finally`-`File.Delete`-Teardowns in `Rezepte.Tests` (Cleanup-Migration)

Sieben Fundstellen, alle `File.Delete` in `finally`-Blöcken auf `Path.GetTempFileName()`/`GetTempPath()`-Dateien — ein dort werfender Delete propagiert und maskiert den Assert (gleiches Fehlerbild wie AC 6). Alle werden auf `TempDirectoryCleanup.DeleteFileBestEffort` umgestellt (nie werfend, Retry/Backoff):

- `GoogleCredentialsProviderTests` (Z. 148, 245)
- `GeminiClientTests` (Z. 44, 90)
- `SettingsCredentialAvailabilityTests` (Z. 47)
- `AIImportHandlerActivationTests` (Z. 78)
- `GitHubReleaseClientTests` (Z. 70 — die umschließende `File.Exists`-Vorprüfung entfällt; der Helfer toleriert fehlende Dateien)

**Begründet belassen (im Klassifikations-Dokument dokumentiert):** `PluginManagerTests` Z. 767 `File.Delete(adjacentContract)` — Arrange-Logik (simuliert „Plugin ohne Vertragsassembly"), kein Teardown; ein Fehlschlag soll den Test laut scheitern lassen statt das Szenario still zu verfälschen.

### `Rezepte.Tests.Browser.csproj`

- Neue `<Compile Include>`-Links auf `TestCategories.cs`, `TempDirectoryCleanup.cs`, `FlakyConventionTests.cs` (Muster wie `RepositoryPaths.cs`, Z. 29).

### `.gitignore`

- Eintrag `/playwright-traces/` ergänzen (Traces landen künftig deterministisch am Repo-Root).

### `.github/workflows/pr-staging-ci.yml` und `staging-ci.yml`

- `build-and-test`: Steps „Build browser tests", „Install Playwright browsers", „Publish application for browser tests" entfernen; Test-Step auf `Rezepte.Tests` + `--filter "Category!=OsInterface"` + attempt-eindeutigem TRX-Namen (`test-results-a${{ github.run_attempt }}.trx`) umstellen; Kommentar „both test projects are blocking" aktualisieren; Upload `test-results-*` nur `Rezepte.Tests/TestResults` mit Attempt-Suffix; Playwright-Trace-Upload in den neuen Job verlagern.
- Neuer Job `browser-tests` wie unter „Programmabläufe" beschrieben — inklusive `if: needs.detect-backmerge.outputs.is_backmerge != 'true'` (Backmerge-Guard wie alle anderen Jobs) und dem präzisierten `if:` des OsInterface-Subset-Steps (`steps.publish.outcome == 'success'`); in `staging-ci.yml` bewusst **nicht** in `needs:` von `version`.

### Dokumentation

- Neu: `Docs/help/os-interface-tests.md` (Pendant zur Blaupausen-`docs/help/stabilitaet/os-interface-tests.md`): Klassifizierungskriterien (Audit-Signale), Lane-Struktur, Trait-/Retry-/Quarantäne-Konvention, dokumentierte `gh run rerun --job <id>`-Regel **nur** für den `browser-tests`-Job, Verlinkung aus `Docs/help/index.md`. Die Datei verknüpft explizit die AC-4-Formulierung („bei Schwellwertüberschreitung entsteht ein Quarantäne-Issue") mit der Ausbaustufe-1-Festlegung (Empfehlung statt Auto-Issue, s. Offene Punkte #2), damit die bewusste Abweichung nachverfolgbar bleibt.
- `Docs/testing.md`: Lane-Beschreibung + lokale Filter-Aufrufe (`--filter "Category!=OsInterface"`, `Category=OsInterface`).
- `Docs/help/github-actions.md`: Job-Struktur (neuer Job inkl. Backmerge-Guard, TRX-/Trace-Artefakte, `continue-on-error`-Semantik).
- `Docs/features.md` Abschnitt CI/CD: kurzer Verweis auf die Lane-Trennung.
- Klassifikations-Ergebnis: `inventory/testliste.md` wird in die finale Einstufung mit den konkreten Trait-Vergaben überführt (persistente Ablage in `Docs/help/os-interface-tests.md` oder als eigenes Audit-Dokument unter `Docs/`). Das Dokument enthält zusätzlich eine **Timeout-Inventar-Tabelle**: je Fundstelle (Playwright-Timeouts, `Task.Delay`, `WaitForTimeoutAsync`, `File.Delete`/`Directory.Delete`) die Festlegung „angepasst" (mit neuem Wert) oder „begründet belassen" (mit Begründung) — inklusive der belassenen 10–30-s-Budgets, der Retry-Muster und `PluginManagerTests` Z. 767.

## Datenbankmigrationen

Keine.

## Validierungsregeln

Keine — es werden keine Anwendungseingaben geändert. Die Skriptparameter
(`-TestProject`, `-PrimaryTrx`, `-Configuration`, Schwellwerte) folgen dem
Blaupausen-Verhalten: fehlende/unauswertbare Primär-TRX gelten als „keine
Flaky-Fehlschläge" (Exit 0), damit ein `if: failure()`-Schritt nie verschleiert.

## Konfigurationsänderungen

| Eintrag | Typ | Standardwert | Zweck |
|---------|-----|--------------|-------|
| `browser-tests`-Job in beiden Workflows | CI-Job | `continue-on-error: true`, `timeout-minutes: 45`, `if: needs.detect-backmerge.outputs.is_backmerge != 'true'` | best-effort-Lane inkl. Backmerge-Guard |
| TRX-LogFileName pro Lane | CI-Logger-Parameter | `test-results-a${{ github.run_attempt }}.trx`, `test-results-browser-a${{ github.run_attempt }}.trx`, `test-results-os-interface-a${{ github.run_attempt }}.trx` | eindeutige Benennung pro Job und Run-Attempt; `${{ github.run_attempt }}` ist der **GitHub-Ausdruck** (wird vom Runner vor dem `dotnet test`-Aufruf aufgelöst), kein Logger-Platzhalter |
| Artefaktnamen | CI | `…-a${{ github.run_attempt }}`-Suffix | Kollision bei Re-Runs vermeiden, Erst-/Retry-Ergebnis korrelierbar |
| `BrowserTimeouts.NavigationTimeoutMilliseconds` | Testinfrastruktur-Konstante | `60000` | Geteiltes Navigations-Default via `context.SetDefaultNavigationTimeout` an allen `NewContextAsync`-Stellen |
| `--filter "Category!=OsInterface"` / `"Category=OsInterface"` | CI-Testfilter | — | Lane-Zuordnung innerhalb `Rezepte.Tests` |
| `REZEPTE_PUBLISH_DIR` | Env-Variable | unverändert | bleibt der Publish-Override der Browser-Tests |
| Schwellwerte `Analyze-FlakyTestResults.ps1` | Skriptparameter | `QuarantineMinFlakeEvents=2`, `QuarantineMinFailureRate=0.2`, `QuarantineMinExecutions=5`, `WatchMinFailureRate=0.1`, `MaxRuns=20` | Flake-Klassifikation (Blaupausen-Defaults, kalibrierbar) |
| `Xunit.SkippableFact` in `Rezepte.Tests.csproj` | NuGet-Bezug | derzeit nicht referenziert | nur nötig, falls der Quarantäne-Übergangsmechanismus dynamische Skips wählt — abhängig vom offenen Punkt |

## Seiteneffekte und Risiken

- **Coverage-Aggregat:** Entfällt die Browser-Cobertura, verschiebt sich die Gesamtheit der gemessenen Zeilen minimal (Testassembly-Anteil entfällt aus dem Aggregat). Das 70-%-Gate misst dann ausschließlich `Rezepte.Tests` — belegt, dass Browser-Tests 0 % Produktcode-Abdeckung beitragen; Restrisiko einer leicht veränderten Quote ist bekannt und vertretbar.
- **Workflow-Laufzeit/Runner-Minuten:** Der eigene Job dupliziert Restore/Build/Publish/Playwright-Install → mehr Runner-Minuten, aber kürzere Zeit bis zum blockierenden Gate-Ergebnis (parallel).
- **`dotnet test Rezepte.sln` entfällt:** Nicht-Testprojekte (`Rezepte.Tests.PluginFixture`, `Rezepte.Updater.TestHost`) werden weiterhin über die Solution-Restore/Static-Analysis-Schritte gebaut; die Testschritte laufen projektweise — die dokumentierte Build-Reihenfolge-Falle (AI-Plugins per `HintPath` auf `Rezepte.Web.dll`) erfordert weiterhin den vorgeschalteten `dotnet build Rezepte.Web`-Step in **beiden** Jobs.
- **`OsInterface`-Markierung lokaler Läufe:** Lokale `dotnet test`-Aufrufe ohne Filter führen weiterhin alles aus (unverändert); die Filter wirken nur in den Workflows.
- **`REZEPTE_EXTERNAL_PLUGINS_PATH`-Tests in der OS-Lane:** Ohne externen Checkout bleiben die beiden `PluginManagerTests` stille No-ops — jetzt in der best-effort-Lane; dokumentiert, kein Verhaltensverlust in der CI.
- **Blaupausen-Adaptation:** Die Skripte stammen aus Softwareschmiede PR #432; Doc-Verweise und Pfade müssen angepasst werden (Rezepte-Doku-Pfade, `Release`-Konfiguration).
- **Trace-Artefakt:** Nach dem Pfad-Fix enthält `playwright-traces-*` erstmals zuverlässig Inhalt — Artefaktgröße steigt (Retention 14 d bleibt).

## Umsetzungsreihenfolge

1. **Trait-Grundgerüst:** `TestCategories` anlegen, `FlakyConventionTests` (Meta-Test via `CustomAttributeData`) schreiben, beides in `Rezepte.Tests.csproj` aufnehmen; `<Compile Include>`-Links in `Rezepte.Tests.Browser.csproj` ergänzen.
   - Voraussetzungen: Keine (xUnit 2.9.3 vorhanden, FluentAssertions in beiden Projekten).
   - Beschreibung: Konstanten + Meta-Test; noch keine Trait-Vergaben → Meta-Test läuft ins Leere und ist grün.
2. **`TempDirectoryCleanup`-Helfer + Unit-Tests:** Helfer in `Rezepte.Tests/TestHelpers/` anlegen, `TempDirectoryCleanupTests` schreiben, `<Compile Include>`-Link ins Browser-Projekt.
   - Voraussetzungen: Schritt 1 abgeschlossen (gleiches Sharing-Muster).
3. **Cleanup-Migration:** Alle `Directory.Delete`-Teardown-Stellen in `Rezepte.Tests` (ContractExportScriptTests Z. 464/546, DeploymentDocumentationTests Z. 104, ExportCleanupServiceTests Z. 51, PluginDirectoryRegistryTestsBase Z. 66, PluginManagerTests Z. 91/682/699, PluginPackageValidatorTests Z. 78, UpdateBackupServiceTests Z. 176) und `RezepteAppFixture.DeleteTemporaryDatabase` auf `DeleteBestEffort` umstellen; alle `File.Delete`-Teardowns (`SqliteFileWebApplicationFactory` Z. 53 sowie die 7 `finally`-Stellen in `GoogleCredentialsProviderTests` Z. 148/245, `GeminiClientTests` Z. 44/90, `SettingsCredentialAvailabilityTests` Z. 47, `AIImportHandlerActivationTests` Z. 78, `GitHubReleaseClientTests` Z. 70) auf `DeleteFileBestEffort` umstellen. `PluginManagerTests` Z. 767 bleibt begründet werfend (Arrange-Logik, s. Designentscheidung).
   - Voraussetzungen: Schritt 2.
4. **Fixture-/Infrastruktur-Härtung (Browser-Projekt):** `RezepteAppFixture` (Start-Retry ≤3, Ringpuffer-Ausgabe + Log-Datei, angereicherte Timeout-Exception, `StartupTimeoutSeconds = 120`), `GitHubStubServer.Dispose` (Handler-Tracking), `PlaywrightTraces`-Helfer + Umstellung der vier Trace-Schreibstellen, `WaitUntilLoadingBarActiveAsync`-Default 5 s, `WaitUntilLoadingBarVisibleAsync`/`…ColorChangedAsync`-Defaults 2 s → 5 s, `BrowserTimeouts`-Helfer + `SetDefaultNavigationTimeout` an den sechs `NewContextAsync`-Stellen, `.gitignore`-Eintrag.
   - Voraussetzungen: Schritt 2 (TempDirectoryCleanup), Schritt 3.
   - Beschreibung: reine Testinfrastruktur, kein Produktcode.
5. **Timing-Fixes und begründete Belassungen:** `ExportCleanupHostedServiceTests`-Warte deterministisch machen; übrige `Task.Delay`-Stellen, `WaitForTimeoutAsync` (Z. 70) und die begründet belassenen Playwright-Timeouts (10–30-s-Budgets, Retry-Muster, `RegisterPage`-30-s) auditiert in der Timeout-Inventar-Tabelle des Klassifikations-Dokuments begründen.
   - Voraussetzungen: Keine (unabhängig).
6. **Trait-Vergaben:** `OsInterface` auf allen 10 Browser-Testklassen + `ContractExportScriptTests` (Klasse) + `DeploymentDocumentationTests.FrameworkDependentLinuxPublish_…` + 2 `PluginManagerTests`-Methoden; `Flaky` auf `DirectUrl_WithQuery_ShowsResults`. Meta-Test prüft die Konvention ab jetzt real.
   - Voraussetzungen: Schritt 1.
   - Beschreibung: Nach diesem Schritt lokal verifizieren: `dotnet test Rezepte.Tests --filter "Category!=OsInterface"` läuft ohne die markierten Tests; `--list-tests --filter "Category=Flaky"` zeigt genau einen Test.
7. **Skripte:** `scripts/lib/Trx.ps1`, `scripts/lib/TestDiscovery.ps1`, `scripts/Retry-FlakyTests.ps1`, `scripts/Analyze-FlakyTestResults.ps1` aus der Blaupause adaptieren (Pfade, `Release`-Default bei Aufruf, Rezepte-Doku-Verweise); mit synthetischen TRX-Sätzen verifizieren (Flake-recovered, persistent, kein Fehlschlag, fehlendes TRX).
   - Voraussetzungen: Schritt 6 (`Category=Flaky`-Discovery braucht mindestens einen markierten Test).
8. **Workflow-Umbau:** `pr-staging-ci.yml` und `staging-ci.yml` — `build-and-test` verschlanken (Browser-Steps raus, Filter, TRX-/Artefakt-Namen mit `${{ github.run_attempt }}`), neuen Job `browser-tests` einführen (inkl. `is_backmerge`-Guard, `id: publish` am Publish-Step und präzisiertem `if:` am OsInterface-Subset-Step), `version`-`needs` unverändert lassen.
   - Voraussetzungen: Schritte 4, 6, 7.
9. **Dokumentation:** `Docs/help/os-interface-tests.md` (inkl. `gh run rerun --job`-Regel und Klassifikations-/Trait-Tabelle), `Docs/testing.md`, `Docs/help/github-actions.md`, `Docs/features.md`, `Docs/help/index.md`.
   - Voraussetzungen: Schritte 6–8 (Doku beschreibt den finalen Zustand).
10. **CI-Verifikation:** Mindestens drei grüne Komplettläufe in `pr-staging-ci.yml` **und** `staging-ci.yml`; Flake-Statistik/Report-Schritt im Lauf sichtbar prüfen; Trace-Artefakt-Inhalt verifizieren.
    - Voraussetzungen: Schritte 1–9 gemergt auf den Branch bzw. im PR-Lauf.

## Tests

### Neue Tests

| Test / Hilfsmethode | Testklasse | Was wird geprüft / bereitgestellt? |
|--------------------|------------|-------------------------------------|
| `FlakyMarkedTests_AlwaysCarryOsInterface` | `FlakyConventionTests` | Jede Methode/Klasse mit `Category=Flaky` trägt `Category=OsInterface` (via `CustomAttributeData`, Klassen-Traits vererben auf Methoden) |
| `DeleteBestEffort_*` / `DeleteFileBestEffort_*` (Retry bei `IOException`, Nie-Werfen nach Max-Versuchen, Prozess-Kill vor Delete, nichtexistenter Pfad, `normalizeAttributes`, fehlende Datei = No-op) | `TempDirectoryCleanupTests` | Verhalten des Cleanup-Helfers beider Varianten (Blaupausen-Pendant `TempDirectoryCleanupTests.cs`) |
| `PlaywrightTraces` | — (Hilfsmethode) | Liefert Repo-Root-Trace-Verzeichnis, legt es an, erzeugt konventionskonforme Dateinamen |
| Ringpuffer/Log-Hilfsmethode in `RezepteAppFixture` | — | Sammelt Kindprozess-Ausgabe zeilenweise begrenzt; Tail landet in Timeout-/Exit-Exceptions |

### Betroffene bestehende Tests

| Test / Testklasse | Grund der Anpassung |
|-------------------|---------------------|
| `ExportCleanupHostedServiceTests.StartAsync_ShouldCheckImmediatelyOnStartup` | Feste 100-ms-Warte → deterministisches `TaskCompletionSource`-Signal (Flake-Fix) |
| `ContractExportScriptTests` | Klassen-Trait `OsInterface`; `Directory.Delete` → `TempDirectoryCleanup` |
| `DeploymentDocumentationTests.FrameworkDependentLinuxPublish_…` | Methoden-Trait `OsInterface`; `Directory.Delete` → `TempDirectoryCleanup` |
| `PluginManagerTests` (2 externe Methoden + Dispose-Stellen) | Methoden-Traits `OsInterface`; `Directory.Delete` → `TempDirectoryCleanup` |
| `ExportCleanupServiceTests`, `PluginDirectoryRegistryTests*` (via Base), `PluginPackageValidatorTests`, `UpdateBackupServiceTests`, `SqliteFileWebApplicationFactory`-Konsumenten | Teardown auf `TempDirectoryCleanup` umgestellt |
| `GoogleCredentialsProviderTests`, `GeminiClientTests`, `SettingsCredentialAvailabilityTests`, `AIImportHandlerActivationTests`, `GitHubReleaseClientTests` | `finally`-`File.Delete` → `TempDirectoryCleanup.DeleteFileBestEffort` (nie werfend, maskiert keinen Assert mehr) |
| Alle 10 Browser-Testklassen | Klassen-Trait `OsInterface`; Fixture-/Page-Object-Änderungen (Trace-Pfad, Timeouts) wirken transparent |
| `RecipeSearchBrowserTests.DirectUrl_WithQuery_ShowsResults` | Methoden-Trait `Flaky` |

### E2E-Tests (primärer Funktionsnachweis)

Keine neuen E2E-Tests erforderlich — die Anforderung ändert ausschließlich
CI-Struktur, Testinfrastruktur und Testcode; es entsteht kein neuer oder
geänderter, über die UI erreichbarer Benutzerfluss. Den Funktionsnachweis liefern
die Workflow-Läufe selbst (Akzeptanzkriterium 10: drei grüne Komplettläufe beider
Workflows) zuzüglich des Report-Schritts. Geometrische Assertions sind nicht
betroffen (kein Dialog/Overlay geändert).

| Test / Testklasse | Grund der Anpassung |
|-------------------|---------------------|
| Gesamte `Rezepte.Tests.Browser`-Suite (34 Tests) | Trait-Vergaben, Fixture-Timeout-/Retry-Änderungen und Trace-Pfad-Fix werden gegen die laufende Suite verifiziert (lokaler Vollauf + CI-Lane) |

## Akzeptanzszenarien

| # | Szenario | Auslöser | Ausgangszustand | Schritte | Sichtbare Wirkung | Nachweis |
|---|----------|----------|------------------|----------|-------------------|----------|
| 1 | Happy Path: PR-CI mit transiente Flake | PR gegen `staging` geöffnet | Traits/Skripte/Job gemergt; ein `Flaky`-markierter Browser-Test schlägt im Primärlauf fehl | `detect-backmerge` → `static-checks` + `build-and-test` + `browser-tests` parallel; `browser-tests` führt Browser-Suite + OsInterface-Subset aus → `if: failure()`-Retry-Step wiederholt genau den Fehlschlag → grün | Workflow-Run insgesamt grün; `browser-tests`-Job zeigt Fehlschlag + Retry-Recovery im Log; Artefakte `test-results-browser-pr-aN` enthalten Primär- + `-flaky-retry`-TRX; Analyze-Step listet das Flake-Ereignis | CI-Lauf + Artefakt-Inspektion |
| 2 | Persistenter Fehlschlag in best-effort-Lane | PR gegen `staging` | Wie 1, aber Retry schlägt ebenfalls fehl | Retry-Skript endet mit Exit 1 | `browser-tests`-Job zeigt Failure-Status, blockiert aber weder Workflow-Ergebnis (job-level `continue-on-error`) noch Merge-Gate; `version`/`prerelease` in `staging-ci.yml` laufen unverändert (kein `needs:`) | CI-Lauf |
| 3 | Kein Fehlschlag in best-effort-Lane | Push auf `staging` | Alles grün | Test-Steps laufen durch, `if: failure()`-Retry wird **übersprungen**, Analyze-Step läuft trotzdem (`if: always()`) | Kein `-flaky-retry.trx` im Artefakt; Report zeigt „stabil" | CI-Lauf |
| 4 | Cleanup-Restfehler maskiert keinen Assert | Test-Teardown (lokal oder CI) | Temp-Verzeichnis ist gesperrt (Datei-Lock) | `TempDirectoryCleanup.DeleteBestEffort` retried ≤5× mit Backoff, Prozess wurde vorher gekillt | Bei persistenter Sperre nur `Console.Error`-Diagnose; der eigentliche Testausgang bleibt unverändert | `TempDirectoryCleanupTests` |
| 5 | Start-Timeout des Kindprozesses | Browser-Test-Init | `Rezepte.Web` wird nicht rechtzeitig bereit (z. B. Bind-Race verloren oder Migrationsstau) | Start-Retry bis 3× mit neuem Port; bei endgültigem Timeout `TimeoutException` mit Phase, vergangener Zeit, Exit-Code und Ausgabe-Tail; optionale Log-Datei | Fehlschlag ist selbstdiagnostizierbar (Phase/State/Error + Kindprozess-Output statt nacktem Timeout) | Unit-/Integrationsnachweis beim Implementieren + erster CI-Flake |
| 6 | Trait-Konvention verletzt | Entwickler markiert `[Trait("Category","Flaky")]` ohne `OsInterface` | Neuer Flaky-Test ohne OsInterface | `FlakyConventionTests` läuft in beiden Assemblies | Meta-Test schlägt fehl mit Namen der Verletzung → Verstoß kann nicht in die blockierende Lane rutschen | `FlakyConventionTests` |
| 7 | Quarantäne-Kandidat | Wiederholte Flakes desselben Tests über Läufe | TRX-Artefakte mehrerer Runs | `Analyze-FlakyTestResults.ps1` klassifiziert über Schwellwerte | `::warning::`-Annotation + Empfehlung „Quarantäne-Issue anlegen"; kein automatisches Issue (Ausbaustufe 1) | Skript-Test mit synthetischem TRX-Satz |

## Offene Punkte

Alle Punkte wurden im autonomen SCM-Lauf anhand der Issue-Empfehlungen und der
Softwareschmiede-Blaupause (PR #432) entschieden und werden zusätzlich als
Issue-Kommentar dokumentiert (autonome Bearbeitung: Interaktion läuft über das
Issue; bei abweichender Entscheidung kann der Kunde dort widersprechen).

| # | Offener Punkt | Entscheidung (2026-11-22) |
|---|---------------|---------------------------|
| 1 | Soll die best-effort-Lane auch im `staging-ci.yml`-Pre-Release-Gate non-blocking sein? (RC mit grünem Gate bei roten Browser-Tests wäre bewusster Trade-off) | **Ja — non-blocking** wie in der Blaupause (job-level `continue-on-error`, kein `needs:` in `version`). Begründung: identische Lane-Semantik in beiden Workflows; die dokumentierte `gh run rerun --job <id>`-Regel deckt echte Browser-Regressionen vor dem Release manuell ab — ein persistenter Fehlschlag bleibt als rotes Job-Signal sichtbar und ist im Analyze-Report nachvollziehbar. |
| 2 | Quarantäne-Automatismus und Übergangsmechanismus (Auto-Issue + Deduplizierung; Test skippen mit Issue-Verweis vs. Trait entziehen) | **Ausbaustufe 1 wie Blaupause**: Report empfiehlt Quarantäne-Issues (`::warning::`-Annotation + Job-Summary), kein Automatismus/Deduplizierung. Übergang manuell: betroffenen Test per `Skip.IfNot` mit Issue-Verweis quarantänieren — `Xunit.SkippableFact` in `Rezepte.Tests.csproj` wird beim ersten tatsächlichen Fall nachgerüstet (Trait-Entzug würde den Test in die blockierende Lane zurückholen und ist der schlechtere Mechanismus). |
| 3 | Konkrete Schwellwerte und Platzierung des Report-Schritts | **Blaupausen-Defaults** (`QuarantineMinFlakeEvents=2`, `QuarantineMinFailureRate=0.2`, `QuarantineMinExecutions=5`, `WatchMinFailureRate=0.1`, `MaxRuns=20`); Report als `if: always()`-Step ausschließlich im `browser-tests`-Job (einzige Lane mit Retry-TRX). Kalibrierung nach den ersten realen Läufen. |
