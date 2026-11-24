# Tests: Ausgangszustand und bestehende Abdeckung

## Test-Ausgangszustand vor der Umsetzung

- Zeitpunkt: 2026-11-24, ~22:15–22:30 Uhr (Europe/Berlin, UTC+1)
- Branch: `task/issue-237-5303623c153440109f5cf1bdd4dea813-major-version` (Basis `staging`)
- Commit-ID: `e9aed2d444f71adaebaf9f8a1cc7aba88fa9161c` (= Stand von `staging`)
- Uncommittete Änderungen im getesteten Stand: nur untracked Dateien unter `Docs/features/task-issue-237-5303623c153440109f5cf1bdd4dea813-major-version/` (requirement.md und diese Bestandsaufnahme) — kein Produktivcode geändert.
- Testumgebung: Windows, lokal installierte SDKs `10.0.404` und `11.0.100` (`dotnet --list-sdks`); `global.json` verlangt `11.0.100` (`rollForward: latestFeature`, `allowPrerelease: false`) → erfüllt. Node `v24.x` (engines `24.x`), npm ci nicht ausgeführt (für .NET-Tests nicht nötig).
- Ermittelte Testsuiten und Quellen der Testbefehle: `Docs/testing.md` (Lanes-Modell), `pr-staging-ci.yml` Gate 4 und `release.yml` Release-Gate führen `dotnet test Rezepte.Tests --no-build --filter "Category!=OsInterface"` als blockierende Lane; `Rezepte.Tests.Browser` + `OsInterface`-Subset sind die umgebungssensitive Best-Effort-Lane (CI `continue-on-error`). Test-Framework: `xunit.v3` (Ausgabe „xUnit.net VSTest Adapter v4.0.1", `net11.0`).

### Testläufe

| Lauf | Befehl inkl. Filter | Arbeitsverzeichnis | Exit-Code | Erfolgreich | Fehlgeschlagen | Übersprungen | Nachweis |
|------|--------------------|--------------------|-----------|-------------|----------------|--------------|----------|
| 1 | `dotnet restore Rezepte.sln` | Repo-Wurzel | 0 | – | – | – | (Restore-Log, 10 Projekte wiederhergestellt) |
| 1 | `dotnet build Rezepte.Web/Rezepte.Web.csproj -c Release --no-restore` | Repo-Wurzel | 0 | – | – | – | [Log](test-results/rezepte-tests-deterministic-2026-11-25.log) |
| 1 | `dotnet build Rezepte.Tests/Rezepte.Tests.csproj -c Release --no-restore` | Repo-Wurzel | 0 | – | – | – | ebenda |
| 1 | `dotnet test Rezepte.Tests/Rezepte.Tests.csproj -c Release --no-build --filter "Category!=OsInterface"` | Repo-Wurzel | 0 | 763 | 0 | 0 | [Log](test-results/rezepte-tests-deterministic-2026-11-25.log) |

Builds jeweils 0 Warnungen/0 Fehler. Hinweis im Log: wiederholte `TempDirectoryCleanup`-Warnungen („Access to the path … is denied" für Plugin-DLLs in Temp-Verzeichnissen) — rein kosmetisch, alle betroffenen Tests bestanden; kein Testfehler.

### Nachgewiesene bestehende Testfehler

Keine — der dokumentierte Lauf ist vollständig grün (763/763).

### Testlücken und Ausführungsprobleme

- `Rezepte.Tests` mit `Category=OsInterface` (umgebungssensitive Teilmenge): **nicht ausgeführt** — in CI bewusst Best-Effort-Lane; lokal nicht Teil des dokumentierten Laufs. Anzahl dieser Tests: unbekannt (Filter-Teilmenge der 763-Test-Suite nicht separat gezählt).
- `Rezepte.Tests.Browser` (Playwright/Chromium): **nicht ausgeführt** — erfordert Playwright-Chromium-Installation und App-Publish (`Docs/testing.md:25-32`); umgebungssensitive Lane, in CI nicht-blockierend.
- `Rezepte.Updater.TestHost`: Testhost-Projekt, keine eigenständige Test-Suite im CI-Gate.
- Node-Seite: keine Tests vorhanden (kein `test`-Skript in `package.json`, keine `*.test.*`-Dateien) — `resolve-release-version.mjs` ist lokal ungetestet; dessen Pfade werden nur in CI ausgeübt.
- Da die Anforderung ausschließlich Release-/CI-/Branching-Konfiguration betrifft (kein Anwendungscode), ist die relevante „Testabdeckung" die Workflow-Validierung selbst; siehe Hinweise in `version-resolution.md` (`--dry-run`-Verifikationspfad).

## Für die Anforderung relevante Test-/Validierungsartefakte

- `Rezepte.Tests/ContractExport/ContractExportScriptTests.cs` — würde nur bei einem `ImportContractVersion`-Bump angefasst (offene Frage 2): Konstante `ContractVersion` (Z. 62), Baseline-Pfade (Z. 147-148), `new Version(…)` in `ExportedBaselineAssembliesUseContractVersion`, Fehlertext in `ExportFailsFastWhenParameterVersionDiffersFromDirectoryBuildProps` (Z. 295). Prozedere: `Docs/help/import-plugins.md:181-188`.
- `Rezepte.Tests/Services/Import/PluginManagerTests*` — decken den Plugin-Ladepfad ab (u. a. `InitializeAsync_ShouldMarkBrokenDllAsLoadFailed`, `…MarkPluginWithInvalidHandlerTypeAsIncompatible`), alle bestanden.
- Akzeptanzvalidierung erfolgt per Anforderung über den realen Release-Lauf bzw. semantic-release-Dry-Runs — dafür existieren keine automatisierten Tests.
