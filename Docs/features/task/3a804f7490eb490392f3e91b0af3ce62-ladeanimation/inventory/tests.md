# Tests (fehlend)

Derzeit existieren **keine Tests** für die Ladeanimation:
- Keine Unit-Tests für `LoadingBarService`
- Keine Integrationstests für Navigations-Binding
- Keine Komponententests für `LoadingBar.razor`

## Zu erwartende Teststruktur

### Unit-Tests für LoadingBarService

Datei: `Rezepte.Tests/Services/LoadingBarServiceTests.cs` (zu erstellen)

Mögliche Testmethoden:

| Testmethode | Was wird getestet? |
|-------------|-------------------|
| `ShowAsync_SetsVisibilityToTrue` | `ShowAsync()` macht die Animation sichtbar |
| `ShowAsync_SelectsRandomColor` | `ShowAsync()` wählt eine Farbe aus der konfigurierten Liste |
| `HideAsync_SetsVisibilityToFalse` | `HideAsync()` verbirgt die Animation |
| `ShowAsync_MultipleCallsUseDifferentColors` | Mehrfache `ShowAsync()`-Aufrufe können verschiedene Farben wählen |
| `ShowAsync_RespectsFeaturesEnabledSetting` | Bei `Enabled: false` wird nichts angezeigt |
| `HideAsync_RespectesHideDelay` | `HideAsync()` wartet die konfigurierte `HideDelay` |

### Integrationstests

Datei: `Rezepte.Tests/Components/LoadingBarIntegrationTests.cs` (zu erstellen)

Mögliche Testszenarien:

| Testszenario | Was wird getestet? |
|-------------|-------------------|
| `NavigationTriggersLoadingBar` | Navigation via `NavigationManager` triggert die LoadingBar |
| `MultipleNavigationsResetColor` | Schnelle Navigation führt zu neuer Farbe |
| `LoadingBarHidesAfterNavigation` | LoadingBar verschwindet nach Navigationsvollendung |

## Bestehende Test-Infrastruktur

Das Projekt hat ein etabliertes Test-Setup unter `Rezepte.Tests`:

- Verwendung von Unit-Test-Framework (vermutlich xUnit oder NUnit)
- Service-Mocking und Dependency-Injection-Testing
- Integration mit Entity Framework Core für Datenbankzugriffe
- Mock-Patterns für `IOptions<T>` Konfiguration

Die LoadingBar-Tests sollten diesem bestehenden Pattern folgen.

## Abhängigkeiten zum Mocken

Beim Testen von `LoadingBarService` müssen folgende Abhängigkeiten gehandhabt werden:

- `IOptions<LoadingBarOptions>` — Konfiguration mocken
- `Random` oder Zufallsfunktion — Für reproduzierbare Tests
- `Task.Delay()` — Für Timeout/HideDelay-Tests
