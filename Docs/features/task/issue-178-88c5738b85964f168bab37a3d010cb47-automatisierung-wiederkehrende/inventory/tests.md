# Test-Ausgangszustand und bestehende Tests

Die Anforderung betrifft ausschließlich GitHub-Actions-Workflows — es gibt keine
.NET-Testoberfläche für das zu ändernde Artefakt. Trotzdem wurde der projektweite
Test-Ausgangszustand vor jeder Änderung festgehalten (Nachweisregel: nur hier
dokumentierte Fehlschläge gelten später als „preexisting").

## Test-Ausgangszustand vor der Umsetzung

- **Zeitpunkt (mit Zeitzone):** 2026-11-23; Testfenster laut TRX-Zeitstempeln
  05:27:02–05:29:37 UTC (lokale Zeit 06:27–06:29, UTC+1).
- **Branch und Commit-ID:** `task/issue-178-88c5738b85964f168bab37a3d010cb47-automatisierung-wiederkehrende`,
  Commit `94957e91315c1665f8935fee93ddb0a10aa85ae1` („Docs: Server-Setup mit und
  ohne Reverse-Proxy (#223)").
- **Uncommittete Änderungen im getesteten Stand:** Keine geänderten Dateien;
  einziges Untracked-Verzeichnis `Docs/features/task/issue-178-…/`
  (Anforderungs-/Inventardokumente, kein Code). `git status --porcelain`
  bestätigt.
- **Testumgebung und Runtime-/SDK-Versionen:** Lokale Windows-Entwicklermaschine;
  `dotnet --version` = **11.0.100** (weiteres SDK 10.0.404 installiert, kein
  `global.json` vorhanden → SDK 11 aktiv, TFM `net10.0` aus
  `Directory.Build.props`); `pwsh` 7.6.10; Node v24.15.0; `gh` 2.92.0
  (authentifiziert als `softwarewerkstatt`); Playwright-Browser-Cache vorhanden
  (`%LOCALAPPDATA%\ms-playwright`, u. a. chromium-1255). Abweichung zur CI:
  Dort läuft `ubuntu-latest` mit `actions/setup-dotnet@v6` → `10.0.x`.
- **Ermittelte Testsuiten und Quellen der Testbefehle:**
  - `Rezepte.Tests` — xUnit, 597 Testmethoden (560 `[Fact]` + 37 `[Theory]`,
    expandiert 774 Testfälle). CI-Aufteilung (`pr-staging-ci.yml`/
    `staging-ci.yml`): blockierende Lane `--filter "Category!=OsInterface"`,
    best-effort-Lane `--filter "Category=OsInterface"`; lokal `dotnet test` ohne
    Filter (`Docs/testing.md` Z. 5–21). Beide Lanes wurden getrennt ausgeführt.
  - `Rezepte.Tests.Browser` — xUnit + `Xunit.SkippableFact`, 34
    `[SkippableFact]`-Methoden plus 1 geteilter `[Fact]` aus
    `Rezepte.Tests/TestHelpers/FlakyConventionTests.cs` (per `<Compile Include>`
    eingebunden, `Rezepte.Tests.Browser.csproj` Z. 33) = 35 Testfälle.
    Voraussetzung `Rezepte.Web`-Publish (automatisch via Target
    `PublishRezepteWebForBrowserTests`, csproj Z. 36–39) + Playwright-Chromium.
  - Projekte ohne Tests: `Rezepte.Tests.PluginFixture` (Fixture),
    `Rezepte.Updater.TestHost` (EXE-Testhost, kein Testprojekt).

### Testläufe

| Lauf | Befehl inkl. Filter | Arbeitsverzeichnis | Exit-Code | Erfolgreich | Fehlgeschlagen | Übersprungen | Nachweis |
|------|--------------------|--------------------|-----------|-------------|----------------|--------------|----------|
| 1 — Restore | `dotnet restore Rezepte.sln` | Repo-Root | 0 | – | – | – | (Konsolenausgabe, nicht archiviert) |
| 2 — Build App+Plugins | `dotnet build Rezepte.Web/Rezepte.Web.csproj --configuration Release --no-restore` | Repo-Root | 0 | – | – | – | (Konsolenausgabe, nicht archiviert) |
| 3 — Build Tests | `dotnet build Rezepte.Tests/Rezepte.Tests.csproj --configuration Release --no-restore` | Repo-Root | 0 | – | – | – | (Konsolenausgabe, nicht archiviert) |
| 4 — `Rezepte.Tests` deterministische Lane | `dotnet test Rezepte.Tests/Rezepte.Tests.csproj --configuration Release --no-build --filter "Category!=OsInterface" --logger "trx;LogFileName=rezepte-tests-deterministic-baseline.trx" --logger "console;verbosity=normal"` | Repo-Root | 0 | 759 | 0 | 0 | [TRX](test-results/rezepte-tests-deterministic-baseline.trx), [Console-Log](test-results/rezepte-tests-deterministic-console.log) |
| 5 — `Rezepte.Tests` OsInterface-Lane | `dotnet test Rezepte.Tests/Rezepte.Tests.csproj --configuration Release --no-build --filter "Category=OsInterface" --logger "trx;LogFileName=rezepte-tests-osinterface-baseline.trx" --logger "console;verbosity=normal"` | Repo-Root | 0 | 15 | 0 | 0 | [TRX](test-results/rezepte-tests-osinterface-baseline.trx), [Console-Log](test-results/rezepte-tests-osinterface-console.log) |
| 6 — Build Browser-Tests | `dotnet build Rezepte.Tests.Browser/Rezepte.Tests.Browser.csproj --configuration Release --no-restore` (inkl. `PublishRezepteWebForBrowserTests`) | Repo-Root | 0 | – | – | – | (Konsolenausgabe, nicht archiviert) |
| 7 — `Rezepte.Tests.Browser` | `dotnet test Rezepte.Tests.Browser/Rezepte.Tests.Browser.csproj --configuration Release --no-build --logger "trx;LogFileName=rezepte-tests-browser-baseline.trx" --logger "console;verbosity=normal"` | Repo-Root | 0 | 35 | 0 | 0 | [TRX](test-results/rezepte-tests-browser-baseline.trx), [Console-Log](test-results/rezepte-tests-browser-console.log) |

Gesamt: **809 Tests ausgeführt, 809 bestanden, 0 fehlgeschlagen, 0 übersprungen**
(TRX-Zähler `executed`/`passed`/`failed` in allen drei Dateien verifiziert; keine
`Skipped`-/`NotExecuted`-Einträge).

Hinweis zur Build-Reihenfolge: `Rezepte.Web` wurde vor den Testprojekten gebaut
(Läufe 2→3→6), weil `Rezepte.Import.Plugins.AIUrl`/`AIFoto` `Rezepte.Web.dll` per
`<Reference>`+`<HintPath>` statt `ProjectReference` referenzieren — die bekannte,
bereits in der Issue-#212-Bestandsaufnahme dokumentierte Reihenfolge-Falle, die
die CI über denselben vorgeschalteten Build-Schritt umgeht.

### Nachgewiesene bestehende Testfehler

Keine. Alle drei Lanes liefen im Ausgangslauf vollständig grün; es gibt keinen
nachgewiesenen Preexisting-Fehler für diesen Stand.

### Testlücken und Ausführungsprobleme

- **Keine Coverage-Erhebung:** `--collect:"XPlat Code Coverage"` und das
  70-%-Gate wurden lokal nicht gefahren (CI-only; ohne Aussage über die Quote).
- **Kein Solution-weiter Lauf:** `dotnet test Rezepte.sln` nicht ausgeführt;
  die drei projektweisen Läufe decken alle Testprojekte ab (gleiche Teilmenge
  wie die CI-Lanes).
- **Kein Browser-Retry-Stand:** `scripts/Retry-FlakyTests.ps1`/
  `Analyze-FlakyTestResults.ps1` sind CI-Hilfen und wurden nicht aufgerufen
  (kein Fehlschlag vorhanden).
- **Playwright-Install nicht wiederholt:** Cache `chromium-1255` vorhanden;
  `playwright.ps1 install --with-deps chromium` entfiel (Voraussetzung erfüllt).
- **Keine Workflow-Tests:** Für `.github/workflows/*.yml` existiert kein
  Test-/Lint-Mechanismus im Repo (kein actionlint/yamllint; `check-encoding.ps1`
  ohne `.yml`-Extension). Verifikation neuer Workflows kann hier nur über
  Review, YAML-Syntaxprüfung und `workflow_dispatch`-Probelauf erfolgen.

## Testklassen

Keine der Anforderung zugeordneten Testklassen — die Änderung liegt in
`.github/workflows/`, nicht in C#-Code. Relevant nur als Konventionsmuster:

### `FlakyConventionTests` (`Rezepte.Tests/TestHelpers/FlakyConventionTests.cs`)

- `FlakyMarkedTests_AlwaysCarryOsInterface` (Z. 22–55) — Meta-Test, der per
  Reflection die Trait-Konvention „`Flaky` nur zusammen mit `OsInterface`"
  durchsetzt; wird per `<Compile Include>` in beide Testassemblys kompiliert.
  Zeigt das Repo-Muster „Konventionen werden ggf. durch Meta-Tests abgesichert".

### `ContractExportScriptTests` (`Rezepte.Tests/ContractExport/ContractExportScriptTests.cs`)

- Führen `scripts/Export-ImportContract.ps1` in `pwsh`-Kindprozessen aus —
  Beispiel dafür, dass Repo-Skripte indirekte Testabdeckung haben können;
  `OsInterface`-markiert (15-Test-Lane, Lauf 5).

## Hilfsmethoden

Nicht anforderungsrelevant; für Kontext siehe die Issue-#212-Bestandsaufnahme
(`Docs/features/task/issue-212-8e56123023244430b028d2464c810d8e-flaky-test-haertung-umgebungss/inventory/tests.md`,
Abschnitte „Testklassen"/„Hilfsmethoden"), die die vollständige
Testinfrastruktur dokumentiert.
