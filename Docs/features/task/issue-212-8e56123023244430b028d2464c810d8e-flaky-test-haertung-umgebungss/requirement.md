# Anforderungsübersetzung

## Metadaten

- Aufgaben-ID: `8e561230-2324-4430-b028-d2464c810d8e`
- Branch: `task/issue-212-8e56123023244430b028d2464c810d8e-flaky-test-haertung-umgebungss`
- Verknüpftes Issue: `martin-stromberg/Rezepte` #212 — „Flaky-Test-Härtung: umgebungssensitive Tests, Deadlines/Cleanup und Retry-Konvention" (Vorlage: `martin-stromberg/Softwareschmiede` #428, Umsetzung PR #432)
- Kundenanforderung: Umgebungssensitive (flaky) Tests aus der blockierenden CI-Lane herauslösen, Timeouts/Cleanup härten, eine projektkonsistente Retry-/Quarantäne-Konvention etablieren und die Stabilität über mehrere CI-Durchläufe nachweisen.

## Fachliche Zusammenfassung

Die CI-Testsuite wird gegen umgebungsbedingte Fehlschläge gehärtet. Dazu wird die bestehende projektweise Trennung (`Rezepte.Tests` deterministisch, `Rezepte.Tests.Browser` umgebungssensitiv mit Playwright, echtem Kestrel-Kindprozess, `TcpListener`-Stub und SQLite-Temp-Datenbanken) auf Workflow-Ebene nachgezogen: Die Browser-Tests wandern aus dem einzigen blockierenden `dotnet test Rezepte.sln`-Schritt in eine best-effort-Lane mit genau einem automatisierten Re-Run markierter Fehlschläge. Ergänzend werden bekannte Flake-Quellen beseitigt (feste Sleeps, zu kurze Timeouts, TOCTOU-Portvergabe, best-effort-`Directory.Delete` ohne Retry), ein `TempDirectoryCleanup`-Helfer sowie die Skripte `scripts/Retry-FlakyTests.ps1` und `scripts/Analyze-FlakyTestResults.ps1` analog zur Softwareschmiede-Blaupause eingeführt und eine `OsInterface`-/`Flaky`-Trait-Konvention per Meta-Test abgesichert.

## Fachlicher Scope

- **Verarbeitungseinheit:** Ein CI-Testdurchlauf der Solution in den Workflows `pr-staging-ci.yml` (PR gegen `staging`) und `staging-ci.yml` (Push auf `staging`) — bestehend aus der deterministischen Testmenge (`Rezepte.Tests`) und der umgebungssensitiven Testmenge (`Rezepte.Tests.Browser` sowie Einzelfälle, die das Audit ausweist). Einzeltests innerhalb der umgebungssensitiven Menge sind die beobachtete Einheit für Flake-Statistik und Quarantäne.
- **Auslösende Ereignisse:** Pull Request gegen `staging` (`pr-staging-ci.yml`, Job `build-and-test`), Push auf `staging` (`staging-ci.yml`, Job `build-and-test`); lokal `dotnet test` auf Entwicklermaschinen (Windows und Linux).
- **Erwartete Wirkung:**
  - Die blockierende Lane enthält ausschließlich deterministische Tests; ein umgebungsbedingter Browser-Test-Fehlschlag blockiert den PR-Merge und das Pre-Release-Gate nicht mehr ungeprüft.
  - In der best-effort-Lane läuft genau ein automatisierter Re-Run nur der zuvor fehlgeschlagenen, `Flaky`-markierten Tests; es gibt kein Blanket-Retry der Gesamtsuite.
  - Cleanup-Fehler (Temp-Verzeichnisse, Kindprozess, Accept-Loop) werden mit Retry/Backoff behandelt und verbleibende Restfehler nur protokolliert — ein Cleanup-Fehler überdeckt nie den eigentlichen Assert.
  - TRX-Artefakte sind pro Job und Run-Attempt eindeutig benannt, sodass Erst- und Retry-Ergebnis korrelierbar sind; ein Report-Schritt wertet die Fehlschlag-then-Rerun-grün-Rate pro Test aus und erzeugt bei Überschreitung des Schwellwerts ein Quarantäne-Issue.
  - Timeout-Fehlschläge der Browser-Infrastruktur sind selbstdiagnostizierbar (Phase/State/Error; die bisher verworfene Kindprozess-Ausgabe wird optional in eine Log-Datei gesichert).
- **Umgang mit fehlenden Objekten:**
  - `OsInterface`- und `Flaky`-Traits: erzeugen — existieren derzeit nicht im Repo (verifiziert); `Flaky` ist nur in Kombination mit `OsInterface` zulässig und wird per Meta-Test abgesichert. Der Trait dient nur ergänzend für Einzelfälle innerhalb von `Rezepte.Tests`; die Lane-Trennung erfolgt primär per Projektsplit.
  - `TempDirectoryCleanup`-Helfer: erzeugen — gemeinsame Implementierung mit Retry/Backoff analog zur Vorlage; plattformübergreifend (Windows-Datei-Locks relevanter als Linux).
  - `scripts/Retry-FlakyTests.ps1`: erzeugen — Pflicht, da Microsoft.Playwright + xUnit keinen eingebauten Retry-Mechanismus besitzen; Vorlage aus `martin-stromberg/Softwareschmiede` PR #432 adaptieren.
  - `scripts/Analyze-FlakyTestResults.ps1`: erzeugen — TRX-Auswertung für die Flake-Statistik.
  - Quarantäne-Issue-Mechanismus: Automatismus und Deduplizierung offen (siehe Offene Fragen); Übergangsmechanismus (Test skippen mit Issue-Verweis vs. Trait entziehen) zu definieren.
- **Scope-Ausschlüsse:**
  1. Keine vollständige Determinisierung echter UI-/Prozess-Tests — Zielbild ist „selten, lokalisiert, selbstdiagnostizierend"; Fake-Umbau nur dort, wo das Audit ihn als sinnvollen Kandidaten ausweist.
  2. Kein Blanket-Retry der Gesamtsuite — Retry nur einmalig, nur in der best-effort-Lane, nur für markierte Fehlschläge.
  3. Keine Windows-zentrische Audit-Heuristik — die Vorlagen-Signale (`ConPTY`, FlaUI/UI-Automation, Clipboard, NTFS-Junctions/Reparse-Points) treffen auf `ubuntu-latest`-CI weitgehend nicht zu; relevante Signale sind `Process.Start`/Sockets/`TcpListener`, Playwright mit kurzen expliziten Timeouts, feste Sleeps statt Zustands-Polling, `Task.Delay`-Warten, Temp-Dateisystem-Nutzung und freie-Port-Vergabe.
  4. Keine fachliche Änderung am Produktcode (`Rezepte.Web`, Plugins) — die Aufgabe betrifft Testinfrastruktur, Testcode und CI-Workflows.
  5. Keine Änderung der übrigen Gates (Format-Check, Security-Scan, Static Analysis, Backmerge-Detection, Contract-Export, 70-%-Coverage-Schwellwert) — nur die Test-Lane-Struktur und die TRX-/Coverage-Artefaktbehandlung werden angepasst.
  6. Kein `OsInterface`-Split auf Solution-Ebene als Standardmechanismus — anders als in der Vorlage existiert hier eine projektweise Trennung, die als Basis dient.

## Betroffene Klassen und Komponenten

### CI-Workflows

- `.github/workflows/pr-staging-ci.yml` — Job `build-and-test`, Schritt „Test" (ca. Zeilen 142–151: `dotnet test Rezepte.sln` mit `--collect:"XPlat Code Coverage"` und TRX-Logger), Schritte „Upload test results" (Zeilen 211–219: Artefakt `test-results-pr` mit identischem Dateinamen `test-results.trx` aus beiden Testprojekten), „Upload Playwright traces", „Generate coverage report"/„Enforce coverage threshold".
- `.github/workflows/staging-ci.yml` — analoge Schritte (Test ca. Zeilen 131–136, Upload ca. Zeilen 192–200, Artefakt `test-results-staging`); betrifft das Pre-Release-Gate vor `version`/`prerelease`.
- Neue Skripte: `scripts/Retry-FlakyTests.ps1`, `scripts/Analyze-FlakyTestResults.ps1` (Pendants zur Softwareschmiede-Vorlage).

### Testinfrastruktur `Rezepte.Tests.Browser` (umgebungssensitive Lane)

- `Infrastructure/RezepteAppFixture.cs` — `StartupTimeoutSeconds` = 60 s (Zeile 29) gegen tatsächliche Publish-/Migrationsdauer auf dem Runner prüfen; `GetFreeTcpPort()` TOCTOU-Race (Zeilen 263–275: Port wird freigegeben, bevor der Kindprozess ihn bindet); `DeleteTemporaryDatabase()` best-effort ohne Retry (Zeilen 156–172) → `TempDirectoryCleanup`-Helfer; Kindprozess-Ausgabe wird gedraint und verworfen (Zeilen 215–218) → optional in Log-Datei sichern; `Kill(entireProcessTree: true)` + `WaitForExit` in `finally`/Dispose belassen.
- `Infrastructure/GitHubStubServer.cs` — Accept-Loop-Beendigung mit 5-s-Warte im `Dispose` (Zeilen 96–110); `TcpListener`-Lebenszyklus.
- `Infrastructure/LoadingBarPageObject.cs` — `WaitUntilLoadingBarActiveAsync` mit 1-s-Default-Timeout (Zeile 245); Aufrufer mit sehr kurzen Budgets.
- `LoadingBarFormNavigationBrowserTests.cs` — fester Sleep `Page.WaitForTimeoutAsync(500)` (Zeile 70) statt Zustands-Polling.
- Weitere Audit-Kandidaten im Projekt: `PlaywrightBrowserFixture`, `BrowserPageObjectBase`, `LoadingBarBrowserSession`, Page-Objects (`LoginPage`, `RegisterPage`, `SecurityTxtPageObject`, `ShareLinkPageObject`, `PluginSettingsPageObject`), Test-Collections (`BrowserTestCollection`, `RegisterTestCollection`, `PluginSettingsTestCollection`) sowie die abgeleiteten Fixtures `ConfiguredRezepteAppFixture`, `PluginSettingsAppFixture`, `RegisterTestsAppFixture` (erben `RezepteAppFixture`-Verhalten).

### Deterministische Lane `Rezepte.Tests`

- Audit-Kandidaten mit `Task.Delay`-Polling (zu verifizieren, ggf. `OsInterface`-Trait oder Fake-Umbau): `Components/RecipeSearchComponentTests.cs` (Zeilen 263, 278), `Services/Import/ImportOrchestratorTests.cs` (Zeilen 243, 491), `Services/Import/PluginUpdateServiceTests_Serialization.cs` (Zeilen 36, 77), `Services/ApplicationUpdatePreInstallHandlerTests.cs` (Zeile 108), `Services/ExportCleanupHostedServiceTests.cs` (Zeile 79).
- Gesamtes Projekt auf Umgebungssensitivität auditieren (InMemory-EF, `WebApplicationFactory`, Moq, bUnit sind weitgehend deterministisch — Einstufung pro Testliste dokumentieren).
- `Rezepte.Tests.csproj` — referenziert derzeit kein `Xunit.SkippableFact` (nur `Rezepte.Tests.Browser.csproj` tut das); Paketbezug ggf. nötig, falls der Übergangsmechanismus Skips in `Rezepte.Tests` erfordert.

### Neue/gemeinsame Artefakte und Tests

- `TempDirectoryCleanup`-Helfer mit Retry/Backoff (Annahme zum Ort: gemeinsam nutzbar, z. B. `Rezepte.Tests/TestHelpers/` oder `Rezepte.Tests.Browser/Infrastructure/` — `RepositoryPaths.cs` wird bereits projektübergreifend per `<Compile Include>` geteilt; finaler Ort im Plan festzulegen).
- Meta-Test für die Trait-Konvention: `Flaky` nur in Kombination mit `OsInterface` zulässig.
- Dokumentation: Pendant zu `docs/help/stabilitaet/os-interface-tests.md` der Vorlage unter `Docs/help/` (Retry-/Quarantäne-Regel inkl. dokumentierter `gh run rerun --job <id>`-Regel nur für den OS-Job); `Docs/testing.md` und `Docs/features.md` (Abschnitt CI/CD) an die Lane-Trennung anpassen.

## Implementierungsansatz

- **Audit zuerst:** Alle Tests in beiden Projekten anhand der angepassten Signale einstufen (deterministisch / umgebungssensitiv / Kandidat für Fake-Umbau); Ergebnis als dokumentierte Testliste — sie steuert, ob `OsInterface`-Traits innerhalb von `Rezepte.Tests` nötig sind.
- **Lane-Trennung per Projektsplit:** `dotnet test Rezepte.Tests` blockierend + `dotnet test Rezepte.Tests.Browser` best-effort (`continue-on-error` oder eigener Job mit großzügigem Timeout — offene Entscheidung, siehe Offene Fragen); `OsInterface`-Trait nur ergänzend für Einzelfälle per `--filter`.
- **Deadline-Angleichung:** Host-Poll-Deadlines größer als Kind-/Phasen-Budget (Referenzfehler der Vorlage: Host 30 s vs. Kind 90 s); `StartupTimeoutSeconds` (60 s) gegen gemessene Publish-/Migrationsdauer prüfen; Playwright-Timeouts (1–20 s) gegen CI-Last; kurze Zustands-Timeouts nur dort belassen, wo schnelles Scheitern fachlich gewollt ist.
- **Cleanup härten:** `Directory.Delete` über `TempDirectoryCleanup` mit Retry/Backoff; `Kill(entireProcessTree: true)` und Accept-Loop-Beendigung in `finally`/`Dispose` belassen; Restfehler nur protokollieren, nie als `AggregateException` werfen.
- **Retry-/Quarantäne-Konvention:** `Flaky` nur mit `OsInterface` (Meta-Test); genau ein automatisierter Re-Run markierter Fehlschläge in der best-effort-Lane über `scripts/Retry-FlakyTests.ps1`; dokumentierte `gh run rerun --job <id>`-Regel nur für den OS-Job.
- **Flake-Statistik:** TRX-Auswertung über `scripts/Analyze-FlakyTestResults.ps1` als Report-Schritt; TRX-Dateinamen/Artefakte eindeutig pro Job und Run-Attempt; Schwellwert für die Fehlschlag-then-Rerun-grün-Rate → Quarantäne-Issue.
- **Coverage-Wechselwirkung:** Verifizieren (nicht annehmen), ob die out-of-process laufenden Browser-Tests zur Zeilenabdeckung beitragen; falls ja, Coverage-Artefakte beider Lanes vor dem 70-%-Gate aggregieren.
- **Verifikation:** Mindestens drei grüne Komplettläufe in `pr-staging-ci.yml` **und** `staging-ci.yml`; ein einzelner grüner Lauf gilt nicht als Nachweis.

## Konfiguration

Keine Laufzeit-Konfiguration der Anwendung erforderlich. Steuerung erfolgt über CI-Mechanismen: `continue-on-error`/Job-Struktur in den Workflows, `timeout-minutes` für die best-effort-Lane, `--filter`-Ausdrücke für Traits, TRX-Dateibenennung pro Job/Attempt sowie Skriptparameter bzw. Konstanten für Retry-Anzahl (genau 1) und Flake-Schwellwert. Die bestehende Umgebungsvariable `REZEPTE_PUBLISH_DIR` bleibt der Override für das Publish-Artefakt der Browser-Tests.

## Akzeptanzkriterien

1. Eine dokumentierte Testliste stuft alle Tests als deterministisch, umgebungssensitiv oder Fake-Umbau-Kandidat ein.
2. In `pr-staging-ci.yml` und `staging-ci.yml` läuft `Rezepte.Tests` blockierend und `Rezepte.Tests.Browser` best-effort; die reguläre Lane bleibt deterministisch und blockierend.
3. `scripts/Retry-FlakyTests.ps1` führt in der best-effort-Lane genau einen Re-Run nur der `Flaky`-markierten Fehlschläge aus; es existiert kein Blanket-Retry.
4. `scripts/Analyze-FlakyTestResults.ps1` wertet TRX-Ergebnisse als Report-Schritt aus; TRX-Artefakte sind pro Job/Run-Attempt eindeutig benannt; bei Schwellwertüberschreitung entsteht ein Quarantäne-Issue.
5. Ein Meta-Test sichert ab, dass `Flaky` nur in Kombination mit `OsInterface` vergeben ist; die `gh run rerun --job <id>`-Regel für den OS-Job ist dokumentiert.
6. `Directory.Delete` läuft über einen gemeinsamen `TempDirectoryCleanup`-Helfer mit Retry/Backoff; Cleanup-Restfehler werden nur protokolliert und überdecken nie den eigentlichen Assert.
7. Die identifizierten Flake-Quellen sind behoben oder begründet belassen: fester Sleep in `LoadingBarFormNavigationBrowserTests`, 1-s-Default in `WaitUntilLoadingBarActiveAsync`, `Task.Delay`-Polling in `Rezepte.Tests`, TOCTOU bei `GetFreeTcpPort()`, 5-s-Accept-Loop-Warte in `GitHubStubServer.Dispose`; Host-Deadlines sind gegen Kind-Budgets abgeglichen (`StartupTimeoutSeconds`, Playwright-Timeouts).
8. Die Kindprozess-Ausgabe der Browser-Test-App ist bei Timeout-Fehlschlägen diagnostizierbar (mindestens Phase/State/Error, optional Log-Datei statt verwerfen).
9. Der Coverage-Beitrag der Browser-Tests ist verifiziert; das 70-%-Gate fällt nicht unbeabsichtigt durch die Lane-Trennung.
10. Mindestens drei grüne Komplettläufe in `pr-staging-ci.yml` und `staging-ci.yml` liegen vor.
11. Audit-Ergebnisse und `TempDirectoryCleanup` funktionieren plattformübergreifend (Entwickler führen Browser-Tests lokal unter Windows aus, CI läuft auf `ubuntu-latest`).

## Explizite Nicht-Ziele

- Kein Umbau aller Browser-/Prozess-Tests auf Fakes — nur auditgesteuerte Einzelfälle.
- Kein Blanket-Retry und kein generelles Entschärfen von Assertions.
- Keine Einführung einer Windows-zentrischen `OsInterface`-Lane analog der Vorlage auf Solution-Ebene.
- Keine fachlichen Änderungen an `Rezepte.Web`, den Import-Plugins oder dem Plugin-Vertrag.
- Keine Änderung der übrigen CI-Gates (Format, Security-Scan, Static Analysis, Backmerge-Detection, Contract-Export, Coverage-Schwellwert).
- Keine automatische Issue-Deduplizierung/Quarantäne-Automatik über den definierten Rahmen hinaus, solange die offenen Punkte ungeklärt sind.

## Offene Fragen

1. **Umfang der best-effort-Lane in `staging-ci.yml`:** Soll sie auch im Pre-Release-Gate non-blocking sein? Ein RC-Release mit grünem Gate bei roten Browser-Tests wäre ein bewusster Trade-off — mit dem Kunden klären.
2. **Job- vs. Step-Trennung:** Separater Job (sauberes Signal, blockierende Lane ohne Playwright-Install schneller) vs. `continue-on-error`-Step im bestehenden Job (kein doppeltes Build/Publish/Playwright-Install) — Entscheidung vor Umsetzung treffen.
3. **Quarantäne-Automatismus:** Automatische Erstellung und Deduplizierung der Quarantäne-Issues sowie der Übergangsmechanismus (Test skippen mit Issue-Verweis vs. `OsInterface`-/`Flaky`-Trait entziehen) sind zu definieren.
4. **Schwellwert und Report-Details:** Konkreter Schwellwert für die Fehlschlag-then-Rerun-grün-Rate und Platzierung des Report-Schritts (nur best-effort-Job vs. aggregierter Schritt) sind beim Ausbau der Skripte festzulegen.
