# Code-Review

## Ergebnis

**Status:** Keine Befunde

## Verifikation der Befunde aus Runde 1 (`review-code.1.md`)

Alle 7 Befunde sind korrekt behoben; die Fixes wurden auf Nebenwirkungen geprüft:

1. **`RezepteAppFixture.StopApplicationProcess`** (RezepteAppFixture.cs Z. 145–182): `Kill`/`WaitForExit` sind jetzt mit `catch (Exception)` best-effort gekapselt und loggen nach `Console.Error`; der `WaitForExit`-Timeout wird explizit behandelt und geloggt. `finally` disposed `_process` und nullt das Feld unter `_outputLock` — korrekte Reihenfolge (`Dispose()` vor dem Lock, keine Deadlock-Falle, da die Async-Reader den Lock nur kurz halten).
2. **`DeleteTemporaryDatabase`** (Z. 184–195): Das tote `processesToKill`-Argument ist entfernt; ein Kommentar dokumentiert, warum kein Kill-Sicherheitsnetz übergeben wird.
3. **Ringpuffer/Log-Datei über Retry-Versuche** (Z. 318–372): `_stdoutTail`/`_stderrTail` werden je Versuch in `StartProcessLogFile` unter `_outputLock` geleert; `_processLogWriter`/`_processLogFilePath` werden ebenfalls unter dem Lock zugewiesen. Zusätzlich filtert ein `ReferenceEquals`-Guard in `CaptureOutputLine` verspätete Callbacks des gekillten Vorgängerprozesses — sauber gelöst, inklusive Registrierung von `_process` unter dem Lock vor dem Drain-Start.
4. **`TempDirectoryCleanup.DeleteBestEffort`** (TempDirectoryCleanup.cs Z. 32–43): `KillProcesses` steht jetzt in einem eigenen `try`/`catch` — ein werfender Enumerator bricht den Never-throws-Vertrag nicht mehr und die Delete-Phase läuft dennoch. Regressionstest `DeleteBestEffort_ProcessesToKillEnumerationThrows_StillDeletesDirectory` (TempDirectoryCleanupTests.cs Z. 111–122) prüft beides; korrekt ohne `OsInterface`-Trait (kein Prozess-Spawn).
5. **`GitHubStubServer.Dispose`** (GitHubStubServer.cs Z. 101–128): Zweiphasiges Warten — erst `_acceptLoop.Wait`, danach `Task.WaitAll(_handlerTasks.ToArray(), remaining)` mit Rest-Budget. Nach dem Loop-Exit ist die Bag final, das Restbudget ist gegen ≤ 0 abgesichert, `AggregateException` (faulted loop/handler) wird gefangen. Handler, die das Budget überschreiten, laufen auf das bereits gecancelte Token und werden über ihren Exception-Filter beendet.
6. **`Retry-FlakyTests.ps1`** (Z. 124–161): `Count -eq 0` landet jetzt im `$indeterminate`-Bucket („not executed in the retry"); ein weiterer Zweig ordnet nicht-Passed/nicht-Failed-Outcomes (z. B. NotExecuted) ebenfalls `indeterminate` zu. Annotationstexte sind jetzt faktisch korrekt; fehlende Retry-TRX ⇒ alle Methoden indeterminate ⇒ Exit 1 (ehrliches Signal).
7. **`Get-FlakyTestNames`** (TestDiscovery.ps1 Z. 13–41): Gibt ein `PSCustomObject` mit `Names`, `DiscoverySucceeded`, `DiscoveryExitCode` zurück; der Aufrufer prüft `DiscoverySucceeded` und meldet einen Discovery-Ausfall als `::warning::`-Annotation, während der Exit-0-Pfad erhalten bleibt (Retry-FlakyTests.ps1 Z. 84–95).

Nebenwirkungs-Prüfung ohne weitere Feststellungen: `_outputLock`-Disziplin ist für alle callback-erreichbaren Felder konsistent; die unsynchronisierten `_process`-Reads in `WaitUntilReadyAsync`/`StopApplicationProcess` liegen auf dem Ein-Thread-Pfad des jeweiligen Versuchs. Die `internal`-Sichtbarkeit von `TempDirectoryCleanup` gegenüber dem Browser-Projekt ist über `<Compile Include Link>` im `.csproj` abgedeckt.

## Geprüfte Dateien

Fokus der zweiten Runde (verifiziert per Zeitstempel: nur diese Dateien wurden nach `review-code.1.md` geändert) plus direkt betroffene Kontextdateien:

- `Rezepte.Tests.Browser/Infrastructure/RezepteAppFixture.cs`
- `Rezepte.Tests.Browser/Infrastructure/GitHubStubServer.cs`
- `Rezepte.Tests/TestHelpers/TempDirectoryCleanup.cs`
- `Rezepte.Tests/TestHelpers/TempDirectoryCleanupTests.cs`
- `scripts/Retry-FlakyTests.ps1`
- `scripts/lib/TestDiscovery.ps1`
- `scripts/lib/Trx.ps1` (Kontext: `Get-TrxTestResults`-Vertrag)
- `scripts/Analyze-FlakyTestResults.ps1` (Kontext: Retry-Outcome-Auswertung)
- `Rezepte.Tests/TestHelpers/FlakyConventionTests.cs` (Kontext: Compile-Link in beide Testassemblies)
- `Rezepte.Tests.Browser/Rezepte.Tests.Browser.csproj` (Kontext: Compile-Links, Publish-Target)
