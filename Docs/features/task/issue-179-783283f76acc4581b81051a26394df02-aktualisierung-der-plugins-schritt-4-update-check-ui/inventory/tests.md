# Tests: Ausgangszustand und bestehende Abdeckung

## Test-Ausgangszustand vor der Umsetzung

Kein eigener Testlauf in diesem Schritt (vorgegeben). Baseline wird aus der Projektdokumentation zitiert:

- **Zeitpunkt (mit Zeitzone):** Inventur-Stand 2026-11-14, HEAD `552ad5f` (2026-11-14 20:29 +0100)
- **Branch:** `task/issue-179-783283f76acc4581b81051a26394df02-aktualisierung-der-plugins-schritt-4-update-check-ui`
- **Uncommittete Änderungen:** nur untracked `Docs/features/task/…-schritt-4-update-check-ui/` (diese Doku)
- **Testumgebung:** .NET `net10.0`, xUnit 2.9.3, FluentAssertions, Microsoft.Playwright 1.61.0 (Chromium headless), Xunit.SkippableFact
- **Testbefehle (Docs/testing.md):** `dotnet test` (Repo-Root); Browser-Tests: Release-Build + `playwright.ps1 install --with-deps chromium` + `dotnet test Rezepte.Tests.Browser/Rezepte.Tests.Browser.csproj -c Release --no-build`

### Zitierte Baseline (Nachweise)

| Lauf | Befehl | Ergebnis | Nachweis |
|------|--------|----------|----------|
| `Rezepte.Tests` (Unit) | `dotnet test Rezepte.Tests` | **687/687 bestanden** | `Docs/projects/task/issue-179-…-aktualisierung-der-plugins/acceptance-schritt-3.md` (Z. 22) |
| `Rezepte.Tests.Browser` | `dotnet test Rezepte.Tests.Browser` | **20/20 im letzten Gesamtlauf**; ein bekannter Flake | `acceptance-schritt-2.md` (Z. 21) + Projekt-`inventory/tests.md` |

### Nachgewiesene bestehende Testfehler (bekannter Flake)

| Test-ID | Suite / Datei | Fehlerbild | Nachweis |
|---------|---------------|------------|----------|
| `Rezepte.Tests.Browser.RegisterTests.Register_WithDemoData_CreatesDemoData` | `Rezepte.Tests.Browser/Auth/RegisterTests.cs` | `TimeoutException`: Demo-Daten-Zähler erreichen Erwartungswert nicht in 120 s, obwohl Seed-Job `Succeeded` meldet (zuletzt `calendar=0`); Einzelwiederholung bestand | Projekt-`inventory/test-results/` + `acceptance-schritt-2.md` |

Kein Bezug zu Plugins/UI — zeitabhängige Unschärfe, keine Regression.

## Testklassen (bestehende Abdeckung für diesen Bereich)

### `Rezepte.Tests.Services.Import`

- `PluginSettingsServiceTests_CheckSource` (`PluginSettingsServiceTests_CheckSource.cs`) — Admin-Guard (`UnauthorizedAccessException` für Nicht-Admin/fehlenden HttpContext) und Delegation an `IPluginUpdateService` inkl. Source-Id-Durchreichung.
- `PluginUpdateServiceTests_CheckSource` — Ergebnis-Mapping von `CheckSourceForUpdatesAsync`: Install, AlreadyInstalled→`NoUpdate`, Retry nach Fehlschlag, diverse `Failed`-Pfade (kein Release, kein ZIP-Asset, fehlender PAT, Install-Fehler, Exception).
- `PluginUpdateServiceTests_Preconditions` — `Failed` bei unbekannter/deaktivierter/nicht vertrauensbestätigter Quelle.
- `PluginUpdateServiceTests_Serialization` — `CheckGate`-Serialisierung: manueller Check wartet auf laufenden Start-Check; Gate wird nach Fehlschlag freigegeben.
- `PluginSettingsServiceTests`, `PluginSettingsServiceTests_RemovePlugin` — Quellen-/Plugin-Verwaltung inkl. `RemovePluginAsync` (Schritt 3).

### Hilfsmethoden (`PluginSettingsServiceTestsBase`)

- `FakePluginUpdateService(result)` — Stub für `IPluginUpdateService`; zählt `CheckSourceCalls`, merkt `LastSourceId`, liefert konfigurierbares `PluginUpdateCheckResult`.
- `CreateHttpContextAccessor(isAdmin: bool)`, `CreateDb()`, `FakePluginManager` — Admin-Kontext und In-Memory-DB für `PluginSettingsService`.

## Browser-Test-Setup (`Rezepte.Tests.Browser`)

- `PlaywrightBrowserFixture` — startet headless Chromium; bei fehlenden Binaries wird übersprungen statt fehlgeschlagen (`SkippableFact`).
- `RezepteAppFixture` — publizierte `Rezepte.Web` als eigener Prozess gegen temporäre SQLite-DB, seeded Test-User `browsertest`/`BrowserTest!123`; `REZEPTE_PUBLISH_DIR` steuerbar.
- Collections: `BrowserTestCollection` (Standard-Fixtures), `RegisterTestCollection`.
- Admin-UI-Testmuster: `SecurityTxtBrowserTests` + `SecurityTxtPageObject` — Login via `/login`, Navigation zum Settings-Panel, Formular bedienen; direktes Vorbild für einen Plugin-Settings-Browser-Test.
- Kein bestehender Browser-Test für `PluginSettings.razor` / den `/settings`-Plugins-Tab.

## Testlücken

- Keine UI-/Komponenten-Tests für `PluginSettings` (weder bUnit noch Browser).
- Lokalisierung der neuen Texte und Symbolbutton-Darstellung sind aktuell nicht testabgedeckt.
