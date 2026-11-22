# Test-Ergebnisse

> Modell-Hinweis: Devin-Umgebung ohne Codex-Modellauswahl; Sitzungsmodell geerbt
> (dokumentierte Abweichung, Rolle: Tests).

Datum: 2026-11-22 (lokaler Lauf, Windows, .NET SDK 11.0.100 / .NET 10.0.15, pwsh 7.6.10)

## Ergebnis

**Status:** Keine Fehler

Alle lokal ausführbaren Lanes und Nachweise sind grün. Die CI-Workflow-Verifikation
(Akzeptanzkriterium 10: ≥3 grüne Komplettläufe beider Workflows) ist im Working Tree
nicht durchführbar und als **ausstehende externe Verifikation** dokumentiert — nicht
als fehlgeschlagener Test (siehe Abschnitt „Reale End-to-End-Abnahme").

## Ausgeführte Lanes (Reihenfolge gemäß HintPath-Build-Falle eingehalten)

| # | Kommando | Ergebnis |
|---|----------|----------|
| 1 | `dotnet build Rezepte.sln -c Release` | Erfolgreich, 0 Warnungen, 0 Fehler (alle 10 Projekte) |
| 2 | `dotnet test Rezepte.Tests/Rezepte.Tests.csproj -c Release --no-build --filter "Category!=OsInterface"` | 758 bestanden, 0 fehlgeschlagen (~20 s) |
| 3 | `dotnet test Rezepte.Tests/Rezepte.Tests.csproj -c Release --no-build --filter "Category=OsInterface"` | 15 bestanden, 0 fehlgeschlagen (~43 s) |
| 4 | `dotnet test Rezepte.Tests.Browser/Rezepte.Tests.Browser.csproj -c Release` | 35 bestanden, 0 fehlgeschlagen (~1,9 min) — Publish vorhanden/aktualisiert via `dotnet publish Rezepte.Web -c Release -o Rezepte.Web/bin/Release/net10.0/publish` (Standard-Auflösungspfad des `RezepteAppFixture`; `REZEPTE_PUBLISH_DIR`-Override nicht nötig) |
| 5 | Meta-Tests gezielt: `--filter "FullyQualifiedName~FlakyConventionTests|FullyQualifiedName~TempDirectoryCleanupTests"` (Rezepte.Tests) + `FullyQualifiedName~FlakyConventionTests` (Rezepte.Tests.Browser) | 9/9 bzw. 1/1 bestanden |
| 6 | `pwsh scripts/Analyze-FlakyTestResults.ps1` gegen synthetischen TRX-Satz | siehe Abschnitt „Skript-Smoke-Verifikation" |

## Fehlgeschlagene Tests

Keine.

## E2E-Abdeckung

Plan (`plan.md` Z. 298–309): kein neuer E2E-Test erforderlich — reine
CI-/Testinfrastruktur-Änderung; die bestehende Browser-Suite (34 Tests) ist der
Regressionsschirm.

| Szenario | Test / Testklasse | Ergebnis |
|----------|-------------------|----------|
| Regressionsschirm Browser-Suite (alle 10 Testklassen, Trait-Vergaben, Fixture-/Timeout-/Trace-Pfad-Änderungen) | `Rezepte.Tests.Browser` komplett (35 Tests inkl. Meta-Test) | Bestanden (35/35) |
| Dokumentierter Baseline-Flake | `RecipeSearchBrowserTests.DirectUrl_WithQuery_ShowsResults` (`Category=Flaky`) | Bestanden im Primärlauf — kein Flake in diesem Lauf, daher kein isolierter Wiederholungslauf nötig (zusätzlich im Retry-E2E-Smoke einmal gefiltert ausgeführt und grün, s. u.) |

## Reale End-to-End-Abnahme

Die Anforderung ändert keine UI und keine Benutzerflüsse; der Funktionsnachweis sind
die Lane-Läufe und das Infrastruktur-Verhalten selbst (Akzeptanzszenarien 1–7 betreffen
CI-Verhalten). Nachweise:

| Szenario | Durchgeführt über | Ergebnis |
|----------|-------------------|----------|
| Filter-Verhalten deterministische Lane (`Category!=OsInterface`) | `dotnet test Rezepte.Tests --list-tests --filter "Category!=OsInterface"` → 758 Tests; Lauf grün | Bestanden |
| Filter-Verhalten OsInterface-Subset (`Category=OsInterface`) | `--list-tests` → 15 Tests in `Rezepte.Tests` (11× `ContractExportScriptTests`, 2× externe `PluginManagerTests`-Methoden, 1× `DeploymentDocumentationTests.FrameworkDependentLinuxPublish_…`, 1× `TempDirectoryCleanupTests.DeleteBestEffort_ProcessesToKill_…` — Prozess-Spawn-Test), 34/35 in `Rezepte.Tests.Browser` (einzige Ausnahme: `FlakyConventionTests` selbst — Meta-Test ohne Trait, läuft projektübergreifend) | Bestanden |
| `Flaky`-Menge | `--list-tests --filter "Category=Flaky"` → `Rezepte.Tests`: 0 Tests; `Rezepte.Tests.Browser`: genau 1 Test (`DirectUrl_WithQuery_ShowsResults`) — entspricht Plan Schritt 6 | Bestanden |
| Nie-werfendes Cleanup (Akzeptanzszenario 4) | `TempDirectoryCleanupTests` (8 Tests: Retry bei IOException/Lock, Nie-Werfen nach Max-Versuchen, Prozess-Kill vor Delete, fehlende Pfade, Read-only-Normalisierung) + reale Beobachtung im Lauf: `TempDirectoryCleanup:`-Diagnosezeilen auf stderr (gesperrte Plugin-DLLs/eine SQLite-DB nach 5 Versuchen) ohne Test-Fehlschlag — Restfehler nur protokolliert, Testausgang unmaskiert | Bestanden |
| Trace-Ablage am Repo-Root | `playwright-traces/` enthält nach dem Browser-Lauf frische ZIPs im Konventionsformat `{Prefix}-{yyyyMMdd-HHmmss}-{Guid}.zip` (z. B. `ShareLink-20261122-181417-….zip`, `SecurityTxt-20261122-181433-….zip`); `.gitignore` Z. 420 enthält `/playwright-traces/` | Bestanden |
| Kindprozess-Log-Ablage | `browser-app-logs/` enthält frische `rezepte-app-(startup|restart)-attempt1-*.log`-Dateien des Laufs (Kindprozess-Ausgabe; überwiegend 0 Byte, da die App-Konsole auf Error-Level läuft — erwartetes Verhalten, ein Log mit Inhalt vorhanden); `.gitignore` Z. 423 enthält `/browser-app-logs/` | Bestanden |
| Retry-Konvention Ende-zu-Ende (Akzeptanzszenarien 1–3) | `Retry-FlakyTests.ps1` gegen synthetische Primär-TRX mit 2 Fehlschlägen (1× `Flaky`-markiert, 1× nicht markiert): nur `DirectUrl_WithQuery_ShowsResults` wurde einmalig mit `(Category=Flaky)&(FullyQualifiedName~…)` wiederholt → grün → `test-results-browser-a1-flaky-retry.trx` neben der Primär-TRX abgelegt → „recovered", Exit 0; nicht markierter Fehlschlag korrekt vom Retry ausgeschlossen | Bestanden |
| CI-Workflow-Verifikation (Akzeptanzkriterium 10: ≥3 grüne Komplettläufe `pr-staging-ci.yml` + `staging-ci.yml`, Report-Step-Sichtprüfung, Trace-Artefakt-Inhalt) | Im Working Tree nicht ausführbar — Workflow-Läufe entstehen erst bei PR/Push auf `staging`; die geänderten Workflow-Dateien wurden nicht lokal ausgeführt | Nicht durchführbar (lokal) — **ausstehende externe Verifikation** im CI, kein Testfehlschlag |

## Skript-Smoke-Verifikation

### `scripts/Analyze-FlakyTestResults.ps1`

Synthetischer TRX-Satz in `%TEMP%\flaky-trx` (5 Primär-Läufe + 1 `-flaky-retry`-TRX):
`TestA` Primär-Fehlschlag + Retry-grün (recovered), `TestB` Fehlschlag in allen
5 Primär-Läufen (persistent), `TestC` immer grün.

| Test | Executions | Failures | Flake events | Failure rate | Klassifikation |
|------|-----------|----------|--------------|--------------|----------------|
| Suite.TestA_Recovered | 5 | 1 | 1 | 20 % | quarantine candidate |
| Suite.TestB_Persistent | 5 | 5 | 0 | 100 % | quarantine candidate |
| Suite.TestC_Stable | 5 | 0 | 0 | 0 % | stable |

- Exit-Code: **0** (reiner Report, wie spezifiziert — `QuarantineMinFailureRate=0.2`
  bei `QuarantineMinExecutions=5` trifft sowohl recovered-Flake als auch persistenten
  Fehlschlag; Quarantäne-Empfehlung als `[warning]`-Zeile, in GitHub Actions
  `::warning::`-Annotation).
- Zusatzfall nicht vorhandenes Verzeichnis: „No TRX files found — empty report.", Exit **0**.

### `scripts/Retry-FlakyTests.ps1`

| Fall | Ergebnis | Exit-Code |
|------|----------|-----------|
| Fehlende/unesbare Primär-TRX | „No flaky failures: primary TRX … is missing or unreadable." | **0** |
| Primär-TRX mit 1 Flaky- + 1 Nicht-Flaky-Fehlschlag | Schnittmenge korrekt (nur der Flaky-Test), genau ein Re-Run, Retry-TRX `<primär>-flaky-retry.trx` im selben Verzeichnis, „recovered" | **0** |

Der persistente Retry-Fall (Exit 1) wurde nicht real ausgelöst (kein persistenter
Fehlschlag verfügbar); die Klassifikationslogik dafür ist im Analyze-Smoke
(`TestB_Persistent` → 100 % Fehlrate → Quarantäne-Kandidat) und im Skriptcode
abgedeckt.

## Sichtprüfung der Abnahme-Screenshots

**Status:** Nicht anwendbar — die Anforderung enthält keine UI-Szenarien und erzeugt
keine Abnahme-Screenshots (kein neuer/geänderter Benutzerfluss, kein Dialog/Overlay;
plan-check.md „E2E-Abdeckung": „Nicht erforderlich mit Begründung").

## Zusammenfassung

- Gesamt (Testausführungen): 808 (758 deterministische Lane + 15 OsInterface-Subset + 35 Browser-Suite; die gezielten Meta-Test-Läufe von Schritt 5 sind Teilmengen davon)
- Bestanden: 808
- Fehlgeschlagen: 0
- Übersprungen: 0
- Skript-Smoke: 3 Skript-Pfade verifiziert (Analyze: 2 Fälle, Retry: 2 Fälle), alle mit erwartetem Exit-Code und erwarteter Klassifikation

## Beobachtungen (keine Fehler)

- **`TempDirectoryCleanup`-Restfehler im echten Einsatz:** Mehrere stderr-Zeilen
  `TempDirectoryCleanup: '…' could not be deleted after 5 attempts` — betroffen sind
  mitgeladene Plugin-Assemblies (`Rezepte.Import.Abstractions.dll`,
  `Rezepte.Import.Plugins.AIFoto.dll` — Windows-Assembly-Lock, fachlich nicht lösbar)
  sowie einmal eine gesperrte `rezepte-browser-test.db`. Alle betroffenen Tests
  bestanden; die Diagnose maskiert keinen Assert — exakt das spezifizierte
  Nie-Werfen-Verhalten.
- **Flake-Beobachtung:** `RecipeSearchBrowserTests.DirectUrl_WithQuery_ShowsResults`
  (einziger `Flaky`-markierter Test) bestand im Primärlauf — kein Flake aufgetreten.

## Testabdeckung

**Abdeckung:** Nicht erhoben — die vorgegebene Kommandofolge enthält kein
`--collect`; das 70-%-Coverage-Gate läuft unverändert in der CI
(`build-and-test`-Job auf `Rezepte.Tests`, siehe plan.md „Coverage in der
best-effort-Lane").

## Fehlende Tests

Quelle: `Dateinamen-Konvention` (keine Coverage-Daten erhoben)

Keine für diese Änderung relevanten Lücken: Alle neuen C#-Artefakte sind selbst
Testinfrastruktur und durch eigene Meta-/Unit-Tests abgedeckt
(`TempDirectoryCleanup` → `TempDirectoryCleanupTests`, Trait-Konvention →
`FlakyConventionTests` in beiden Assemblies). Die neuen PowerShell-Skripte haben
keine Unit-Test-Konvention im Repo und wurden stattdessen — wie im Plan vorgesehen —
gegen synthetische TRX-Sätze und real verifiziert (s. „Skript-Smoke-Verifikation").
`BrowserTimeouts`, `PlaywrightTraces` und `TestCategories` sind
Konstanten-/Pfad-Helfer ohne isolierte Testanforderung (plan.md „Neue Tests":
`PlaywrightTraces` als Hilfsmethode ohne eigene Testklasse eingeordnet).
