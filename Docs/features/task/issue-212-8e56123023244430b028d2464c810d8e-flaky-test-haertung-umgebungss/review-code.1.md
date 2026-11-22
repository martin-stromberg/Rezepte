# Code-Review

## Ergebnis

**Status:** Befunde vorhanden

## Befunde

### RezepteAppFixture.cs (RezepteAppFixture)

- **Fehlerbehandlung / Cleanup-Vertrag** — `StopApplicationProcess` (Z. 145–165): `_process.Kill(entireProcessTree: true)` und `_process.WaitForExit(ShutdownGraceMilliseconds)` laufen zwar in einem `try`, haben aber keinen `catch` — nur ein `finally`. Race: Beendet sich der Kindprozess zwischen dem `HasExited`-Check und `Kill`, wirft `Kill` eine `InvalidOperationException` („process has already exited"), `Win32Exception` ist ebenfalls möglich. Die Exception propagiert durch `DisposeAsync` und überdeckt damit das eigentliche Testergebnis — exakt das Maskierungsproblem, das `TempDirectoryCleanup` auf Seiten der Datei-Löschung bewusst vermeidet. Zwei Folgeschäden: `DeleteTemporaryDatabase` wird übersprungen (Temp-Verzeichnis leakt), und im `catch`-Pfad von `InitializeAsync` (Z. 93–97) überdeckt ein Dispose-Fehler die ursprüngliche Start-Exception. Zusätzlich wird der Rückgabewert von `WaitForExit(5000)` ignoriert — überlebt der Prozess die Karenzzeit, wird `_process` dennoch disposed und die DB-Löschung läuft gegen eine noch gehaltene Datei.

  Empfehlung: Kill/WaitForExit mit `catch (Exception)` best-effort kapseln (analog `TempDirectoryCleanup.KillProcesses`) oder den Kill an `TempDirectoryCleanup` delegieren; den `WaitForExit`-Timeout explizit behandeln (loggen statt ignorieren).

- **Toter Code** — `DeleteTemporaryDatabase` (Z. 171–173): `processesToKill: _process is null ? null : new[] { _process }` — `_process` ist an beiden Aufrufstellen immer `null`, weil `DisposeAsync` und `RestartWithEmptyDatabaseAsync` vorher `StopApplicationProcess()` aufrufen, das das Feld im `finally` nullt. Ein übergebenes, bereits disposed Process-Objekt würde ohnehin nur den Toleranz-Catch auslösen.

  Empfehlung: Das Argument streichen (oder einen Pfad schaffen, in dem Kill vor dem Nullen tatsächlich greift) — aktuell suggeriert die Zeile einen Sicherheitsnetz-Effekt, der nie eintritt.

- **Randfall / Diagnose-Treue** — Ringpuffer und Log-Datei über Retry-Versuche hinweg (Z. 36–38, 228–256, 312–330): `_stdoutTail`/`_stderrTail` werden zwischen den Start-Versuchen nicht geleert — das „Last child process output" in der Exception von Versuch 3 enthält noch Zeilen der Prozesse aus Versuch 1/2 und ist damit irreführend beschriftet. Außerdem weist `StartProcessLogFile` `_processLogWriter`/`_processLogFilePath` außerhalb von `_outputLock` zu: ein verspätet eintreffender `OutputDataReceived`-Callback des gerade gekillten Prozesses (async Drain läuft nach `WaitForExit(timeout)` weiter) kann Zeilen des alten Prozesses in die Log-Datei des neuen Versuchs schreiben.

  Empfehlung: Tails je Versuch (z. B. in `StartProcessLogFile` oder vor `StartApplicationProcess`) unter `_outputLock` leeren; die Writer-Zuweisung ebenfalls unter `_outputLock` vornehmen.

### TempDirectoryCleanup.cs (TempDirectoryCleanup)

- **Fehlerbehandlung / Vertragsleck** — `DeleteBestEffort` (Z. 32): `KillProcesses(processesToKill)` steht vor und außerhalb des `try`-Blocks. Die Per-Prozess-Fehler sind innen korrekt best-effort gefangen, aber die Enumeration des aufruferseitigen `IEnumerable<Process>` (`GetEnumerator`/`MoveNext` im `foreach`, Z. 94) ist ungeschützt — ein werfender Enumerator durchbricht den dokumentierten „Never throws"-Vertrag.

  Empfehlung: Den `KillProcesses`-Aufruf in den `try`-Block ziehen (oder die Enumeration selbst absichern).

### GitHubStubServer.cs (GitHubStubServer)

- **Fehlerbehandlung / Concurrency** — `Dispose` (Z. 102–107): `Task.WaitAll([_acceptLoop, .. _handlerTasks], TimeSpan.FromSeconds(5))` snapshotet `_handlerTasks` beim Aufbau des Argument-Arrays. Ein Client, der zwischen `_listener.Stop()` und dem Exit der Accept-Loop noch akzeptiert wird, landet per `_handlerTasks.Add(HandleClientAsync(client))` (Z. 138) nach dem Snapshot in der Bag und wird nicht abgewartet — der Handler läuft dann in das direkt folgende `_shutdown.Dispose()`. Das im Kommentar adressierte Problem (Handler trifft disposed State) bleibt residual bestehen.

  Empfehlung: Nach `WaitAll` auf `_acceptLoop` die Bag erneut einlesen und die dann aktuellen Handler abwarten (Loop-Exit garantiert, dass keine neuen Handler mehr entstehen).

### Retry-FlakyTests.ps1

- **Fehlerbehandlung / Kommentar-Code-Inkonsistenz** — Z. 113–135: Der Kommentar behauptet, im Retry gar nicht ausgeführte Tests („discovery failure in the filtered re-run") würden als eigene nicht-recovered-Kategorie gemeldet. Tatsächlich landet `$retryOutcomes.Count -eq 0` im `elseif` in `$persistent` — die Annotation lautet dann „failed in the primary run AND in the retry", was für einen Test, der im Retry nicht lief, faktisch falsch ist.

  Empfehlung: `Count -eq 0` dem `$indeterminate`-Bucket zuordnen oder den Annotationstext für den nicht-ausgeführten Fall differenzieren.

- **Fehlerbehandlung** — Zusammen mit `Get-FlakyTestNames` (`scripts/lib/TestDiscovery.ps1`, Z. 23–31): Schlägt `dotnet test --list-tests` fehl (z. B. veralteter Build), entsteht eine leere oder partielle Flaky-Menge; das Skript meldet dann „none of them Flaky-marked" und endet mit Exit 0. Ein Discovery-Ausfall ist damit von „keine Flaky-Tests" nicht unterscheidbar und der vorgesehene Retry wird still übersprungen.

  Empfehlung: `$LASTEXITCODE` nach dem `--list-tests`-Aufruf prüfen und bei ≠ 0 eine Warnung ausgeben (Exit 0 als Verhalten kann bleiben, der Grund sollte aber im Log sichtbar sein).

## Geprüfte Dateien

- `Rezepte.Tests/TestHelpers/TestCategories.cs` (neu)
- `Rezepte.Tests/TestHelpers/TempDirectoryCleanup.cs` (neu)
- `Rezepte.Tests/TestHelpers/TempDirectoryCleanupTests.cs` (neu)
- `Rezepte.Tests/TestHelpers/FlakyConventionTests.cs` (neu)
- `Rezepte.Tests.Browser/Infrastructure/BrowserTimeouts.cs` (neu)
- `Rezepte.Tests.Browser/Infrastructure/PlaywrightTraces.cs` (neu)
- `Rezepte.Tests.Browser/Infrastructure/RezepteAppFixture.cs`
- `Rezepte.Tests.Browser/Infrastructure/GitHubStubServer.cs`
- `Rezepte.Tests.Browser/Infrastructure/BrowserPageObjectBase.cs`
- `Rezepte.Tests.Browser/Infrastructure/LoadingBarPageObject.cs`
- `Rezepte.Tests.Browser/Infrastructure/PluginSettingsPageObject.cs`
- `Rezepte.Tests.Browser/Infrastructure/ShareLinkPageObject.cs`
- `Rezepte.Tests.Browser/Auth/RegisterPage.cs`
- `Rezepte.Tests.Browser/Auth/RegisterTests.cs`
- `Rezepte.Tests.Browser/{LoadingBarColor,LoadingBarDisabled,LoadingBarFormNavigation,LoadingBarSafetyTimeout,LoadingBarVisibility,PluginSettings,RecipeSearch,SecurityTxt,ShareLink}BrowserTests.cs`
- `Rezepte.Tests.Browser/Rezepte.Tests.Browser.csproj`
- `Rezepte.Tests/ContractExport/ContractExportScriptTests.cs`
- `Rezepte.Tests/Controllers/SettingsCredentialAvailabilityTests.cs`
- `Rezepte.Tests/Deployment/DeploymentDocumentationTests.cs`
- `Rezepte.Tests/Services/ExportCleanupHostedServiceTests.cs`
- `Rezepte.Tests/Services/ExportCleanupServiceTests.cs`
- `Rezepte.Tests/Services/GoogleCredentialsProviderTests.cs`
- `Rezepte.Tests/Services/UpdateBackupServiceTests.cs`
- `Rezepte.Tests/Services/Import/{AIImportHandlerActivationTests,GeminiClientTests,GitHubReleaseClientTests,PluginDirectoryRegistryTestsBase,PluginManagerTests,PluginPackageValidatorTests}.cs`
- `Rezepte.Tests/TestSupport/SqliteFileWebApplicationFactory.cs`
- `scripts/Retry-FlakyTests.ps1` (neu)
- `scripts/Analyze-FlakyTestResults.ps1` (neu)
- `scripts/lib/Trx.ps1` (neu)
- `scripts/lib/TestDiscovery.ps1` (neu)
- `.github/workflows/pr-staging-ci.yml`
- `.github/workflows/staging-ci.yml`
- `.gitignore`
- `Docs/testing.md`, `Docs/features.md`, `Docs/help/index.md`, `Docs/help/github-actions.md`, `Docs/help/os-interface-tests.md` (neu)
