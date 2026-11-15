# Tests, Build & Konventionen — Bestandsaufnahme (Schritt 1)

## Test-Ausgangszustand vor der Umsetzung

- Zeitpunkt: 2026-11-15, ~15:30 UTC
- Branch und Commit-ID: `task/issue-177-280a0a2130b7488bbf6a95f3291b7e6f-code-review-orchestrierung-meh-schritt-1-import-pfad` @ `961e58bdc9a548051b6c306bec5ee2c505835e93`
- Uncommittete Änderungen im getesteten Stand: nur neue ungetrackte Artefakte unter `docs/features/task/.../` (kein Produktiv-/Testcode)
- Testumgebung: Windows, .NET SDK 10.0.403, `net10.0`, Debug-Konfiguration
- Ermittelte Testsuiten: `Rezepte.Tests` (xUnit 2.9.3, FluentAssertions, Moq, bUnit, EFCore InMemory, Mvc.Testing); `Rezepte.Tests.Browser` (Playwright-E2E — in dieser Umgebung nicht ausführbar, siehe unten)
- Build vor Test: `dotnet build Rezepte.sln` → 0 Fehler, 0 Warnungen

### Testläufe

| Lauf | Befehl inkl. Filter | Arbeitsverzeichnis | Exit-Code | Erfolgreich | Fehlgeschlagen | Übersprungen | Nachweis |
|------|--------------------|--------------------|-----------|-------------|----------------|--------------|----------|
| 1 | `dotnet test Rezepte.Tests/Rezepte.Tests.csproj --no-build --logger "console;verbosity=normal"` | Repo-Root | 0 | 703 | 0 | 0 | [baseline-dotnet-test.log](test-results/baseline-dotnet-test.log) |

### Nachgewiesene bestehende Testfehler

Keine — alle 703 Tests bestanden im Ausgangslauf.

### Testlücken und Ausführungsprobleme

- `Rezepte.Tests.Browser` (Playwright): Das Projekt baut in der Solution, ist in dieser Umgebung jedoch nicht ausführbar (Playwright-Browser nicht installiert bzw. vom Auftraggeber als nicht lauffähig deklariert). Nicht im Basistest enthalten; betrifft keine Testabdeckung dieses Schritts (kein Browser-Test für die Import-Endpunkte bekannt).
- Keine Controller-Tests für die Import-Endpunkte vorhanden (`Rezepte.Tests/Controllers/` enthält keine `CookbooksController`-Import-Tests).

## Betroffene Testdateien

### `Rezepte.Tests/Services/Import/ImportServicePluginTests.cs` (105 Zeilen)

- Testet `ImportService` direkt (`CreateService` Zeilen 50-55: `FakePluginManager` + `PassthroughPersister` + `NullLogger`).
- 2 Tests: `ImportAsync_ShouldUseFirstMatchingPluginInConfiguredOrder` (20-34), `ImportAsync_ShouldReturnPluginError_WhenNoPluginCanHandleInput` (40-48).
- Hilfscode: `PassthroughPersister` (57-68), `FakePluginManager` (70-84), `RecordingHandler` (86-104).
- **Anpassung:** entfällt bei Entfernen von `ImportService`; inhaltlich bereits durch `ImportOrchestratorTests` abgedeckt (Reihenfolge- und Kein-Handler-Fall).

### `Rezepte.Tests/Services/Import/ImportOrchestratorTests.cs` (510 Zeilen)

- 12 Tests (Zeilen 23-322): Plugin-Reihenfolge, `InitiatorUserId`/Session-Isolation, interaktive Bestätigung (`Confirm`), Collection-Selection/Item-Status/Cancel/Duplikat-IDs, Concurrent-Submit, Cancellation vor Start.
- Hilfscode: `CreateOrchestrator` (324-338: `ServiceCollection` mit Persister-Singleton, `IServiceScopeFactory`), `CancellingStream` (347-354), `PassthroughPersister` mit `TargetCookbookIds` (356-370), `WaitUntilAsync`/`WaitForResultAsync` = Polling 25 ms/Timeout 5 s (372-395), `FakePluginManager` (397-411), `RecordingHandler`/`InteractiveRecordingHandler`/`ThrowingHandler`/`CollectionRecordingHandler` (413-509).
- **Anpassung:** bleibt Referenztest des konsolidierten Pfads; `uri`-Durchreichung heute nicht explizit assertiert (`RecordingHandler.HandleAsync` ignoriert `uri`) — Ergänzung geplant.

### Sonstige geprüfte Testdateien ohne Änderungsbedarf

`RemoteUrlGuardTests`, `PluginSettingsComponentTests`, `CollectionImportSelectionStateTests`, `CookbookServiceTests`, `RecipeServiceTests`, `ContractExportScriptTests` (Plugin-Vertrag unverändert).

## Build- und Test-Kommandos

- `dotnet build Rezepte.sln` (Debug, lokal ausreichend)
- `dotnet test Rezepte.Tests/Rezepte.Tests.csproj` — vollständige ausführbare Suite in dieser Umgebung
- CI baut Release mit `TreatWarningsAsErrors` und führt `dotnet test Rezepte.sln` aus (inkl. Browser-Projekt, dort lauffähig).

## Weitere Konventionen

- `CLAUDE.md` (Repo-Root): Programmcode auf Englisch; faktenbasiertes Arbeiten.
- `Directory.Build.props`: `GenerateDocumentationFile` + `WarningsAsErrors=CS1591` (fehlende XML-Doku auf öffentlichen Membern = Build-Fehler, auch in Testprojekten).
- Pre-Commit-Hook (`.githooks/`): u. a. `dotnet format --verify-no-changes` auf der Solution, `csproj-xmldoc-check.py`, `razor-l10n-check.py`.
- Feature-Doku-Konvention `Docs/help/`: Importablauf in `import-plugins.md` (Abschnitt „Importablauf", ca. Zeilen 87-96) — beschreibt Ist-Zustand, ggf. anzupassen.
