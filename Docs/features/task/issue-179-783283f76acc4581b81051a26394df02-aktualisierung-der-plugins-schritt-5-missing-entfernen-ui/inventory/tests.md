# Tests: Bestehende Abdeckung und Ausgangszustand

## Bestehende Tests und Hilfsmethoden für den Bereich

| Test / Hilfsmethode | Datei | Zweck |
|--------------------|-------|-------|
| `PluginSettingsComponentTests` (12 Facts) | `Rezepte.Tests/Components/PluginSettingsComponentTests.cs` | bUnit-Tests der Komponente (Quellen-Check-Button, Busy-State, lokalisierte Alerts, Info-Text, Icon-Button-Attribute, `LastErrorAt`) |
| `WithSources(...)` | ebenda, Z. 276–280 | Mock-Setup `GetSourcesAsync` |
| `Render()` | ebenda, Z. 282–287 | Rendert Komponente, wartet auf erste Tabelle |
| `AssertIconButton(cut, iconClass, text)` | ebenda, Z. 294–301 | Prüft `title`/`aria-label`/Wrapper-`title` eines Icon-Buttons (statischer Text — für parametrisiertes Label nicht direkt nutzbar) |
| `Source(...)` | ebenda, Z. 303–327 | Fabrik für `PluginSourceSettingsItem` |
| `PluginSettingsServiceTests_RemovePlugin` | `Rezepte.Tests/Services/Import/PluginSettingsServiceTests_RemovePlugin.cs` | Service-Tests: Admin-Gate, Missing-Guard, Löschung |
| `SeedInstalledReleaseAsync` | `Rezepte.Tests.Browser/Infrastructure/PluginSettingsAppFixture.cs` | SQLite-Seed-Muster auf `DatabasePath` — Vorlage für `PluginSettings`-Seed |
| `SourceRow`, `CheckButton`, `GetAlertTextAsync` | `Rezepte.Tests.Browser/Infrastructure/PluginSettingsPageObject.cs` | Locators für Quellen-Tabelle/Alerts — Plugin-Tabelle nicht abgedeckt |

## Test-Ausgangszustand vor der Umsetzung

- Zeitpunkt: 2026-11-14 ~21:02 (lokale Zeit, Zeitzone nicht ermittelt)
- Branch: `task/issue-179-783283f76acc4581b81051a26394df02-aktualisierung-der-plugins-schritt-5-missing-entfernen-ui`, Commit `bbb7d2e`
- Uncommittete Änderungen im getesteten Stand: nur untracked Feature-Verzeichnis
  `Docs/features/task/issue-179-…-missing-entfernen-ui/` (keine Codeänderungen)
- Testumgebung: .NET SDK 10.0.403, `net10.0`, Windows

### Testläufe

| Lauf | Befehl inkl. Filter | Arbeitsverzeichnis | Exit-Code | Erfolgreich | Fehlgeschlagen | Übersprungen | Nachweis |
|------|--------------------|--------------------|-----------|-------------|----------------|--------------|----------|
| 1 | `dotnet test Rezepte.Tests/Rezepte.Tests.csproj --filter "FullyQualifiedName~PluginSettingsComponentTests" --nologo -v q` | Repo-Root | 0 | 12 | 0 | 0 | [dotnet-test-pluginsettingscomponents-2026.log](test-results/dotnet-test-pluginsettingscomponents-2026.log) |

### Nachgewiesene bestehende Testfehler

Keine — alle 12 Tests der betroffenen Suite bestanden.

### Nicht ausgeführte Suiten

| Suite | Grund |
|-------|-------|
| `Rezepte.Tests.Browser` (Playwright-E2E) | Erfordert Playwright-Chromium + App-Fixture-Start; für die Baseline dieses kleinen Schritts nicht gestartet. Vor Umsetzungsende als Teil der Testphase zu prüfen. |
| Übrige `Rezepte.Tests`-Suiten | Außerhalb des Anforderungsbereichs; `PluginSettingsServiceTests_RemovePlugin` deckt den Service bereits ab (unverändert). |
