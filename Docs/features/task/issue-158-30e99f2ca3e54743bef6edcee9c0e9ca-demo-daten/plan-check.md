# Plan-Gegenprüfung

## Ergebnis

**Status:** Plan vollständig

## Abgleich Akzeptanzkriterien

| Akzeptanzkriterium | Umsetzung im Plan | Testnachweis im Plan | Status |
|--------------------|-------------------|----------------------|--------|
| Checkbox in `Register.razor` zur Wahl, Demo-Daten anzulegen | Schritt 7: `CreateDemoData`-Checkbox in `RegisterModel` und `Register.razor` (Zeile 137-139) | E2E-Szenarien `Register_WithDemoData` / `Register_WithoutDemoData` in `RegisterTests.cs` | Abgedeckt |
| Nach erfolgreicher Registrierung Start eines Hintergrundjobs über `IBackgroundJobQueue` | Schritt 5: `UserService.RegisterAsync` enqueued `seed-demo-data`-Job bei `createDemoData == true` (Zeile 78-80) | `RegisterAsync_ShouldEnqueueDemoDataJob_WhenCreateDemoDataIsTrue` (Zeile 162) | Abgedeckt |
| Hintergrundjob legt 5 Kochbücher an | Handler ruft `ICookbookService.CreateAsync` für 5 Namen (Zeile 40-41) | `HandleAsync_ShouldCreateFiveCookbooks` (Zeile 167) | Abgedeckt |
| Hintergrundjob legt 43 Rezepte an und ordnet sie den Kochbüchern zu | Handler iteriert `DemoDataSource` und ruft `IRecipeService.CreateAsync` (Zeile 41-42) | `HandleAsync_ShouldCreateFortyThreeRecipes` (Zeile 168) | Abgedeckt |
| Hintergrundjob legt 5 Kalendereinträge aus „Abendessen" für die nächsten 5 Tage an | Handler erzeugt 5 Events für `DateTime.Today.AddDays(1..5)` (Zeile 42) | `HandleAsync_ShouldCreateFiveCalendarEvents` (Zeile 169) | Abgedeckt |
| Hintergrundjob fügt Zutaten des Rezepts für den nächsten Tag der Einkaufsliste hinzu | Handler holt Standardgruppe und ruft `AddItemAsync` für Zutaten des ersten Kalendereintrags (Zeile 43) | `HandleAsync_ShouldAddIngredientsToDefaultShoppingList` (Zeile 170) | Abgedeckt |
| Demo-Daten werden über bestehende Service-Methoden erzeugt | Handler verwendet nur `ICookbookService`, `IRecipeService`, `ICalendarService`, `IShoppingListService` (Zeile 41-43) | Indirekt über Handler-Unit-Tests (Anzahl/Richtigkeit) | Abgedeckt |
| `CreateDemoData` in `RegisterRequest` und `RegisterRequestForm` (JSON und Form) | Schritt 1: DTOs erweitert; Schritt 6: `AuthController` liest aus Form bzw. JSON (Zeile 68-69, 112-115) | `Register_ShouldCallUserServiceWithCreateDemoDataTrue_WhenFormCheckboxChecked`, `Register_ShouldCallUserServiceWithCreateDemoDataFromJson_WhenProvided` (Zeile 164, 166) | Abgedeckt |
| Demo-Daten-Seeding optional, Default `false`, keine zentrale Konfiguration | Default `false` in DTOs/Modell; optionaler Konfigurationsschalter explizit nicht eingeführt (Zeile 15, 73-74) | `RegisterAsync_ShouldNotEnqueueDemoDataJob_WhenCreateDemoDataIsFalse` (Zeile 163); `Register_ShouldCallUserServiceWithCreateDemoDataFalse_WhenFormCheckboxUnchecked` (Zeile 165) | Abgedeckt |
| Fehlerbehandlung im Hintergrundjob: Kein Retry, Job-Status `Failed` | Validierungsregel: fehlender Benutzer / `ok == false` wirft Exception, Hosted Service setzt `Failed` (Zeile 93-97) | `HandleAsync_ShouldThrow_WhenUserDoesNotExist`, `HandleAsync_ShouldThrow_WhenAnyServiceOperationFails` (Zeile 171-172) | Abgedeckt |
| Demo-Daten enthalten keine Bilder, sind reguläre Datensätze, kein Gruppenlöschen | Designentscheidungen und Risikokapitel (Zeile 14-16, 104-108) | Nicht funktional testbar / nicht erforderlich | Abgedeckt |

## Fehlende oder unvollständige Testanforderungen

Nur ausfüllen, wenn Status `Plan lückenhaft`:

## E2E-Abdeckung

| Benutzerfluss / Akzeptanzkriterium | Geplanter E2E-Test | Status |
|------------------------------------|--------------------|--------|
| Registrierung mit angehaktem Demo-Daten-Checkbox → anschließend 5 Kochbücher, 43 Rezepte, 5 Kalendereinträge, Einkaufslisteneinträge | `Register_WithDemoData` in `Rezepte.Tests.Browser/Auth/RegisterTests.cs` mit konkretem Setup, Aktion und sichtbarem Ergebnis (Zeile 192) | Abgedeckt |
| Registrierung ohne Demo-Daten-Checkbox → keine Demo-Daten sichtbar | `Register_WithoutDemoData` in `Rezepte.Tests.Browser/Auth/RegisterTests.cs` mit konkretem Setup, Aktion und sichtbarem Negativ-Ergebnis (Zeile 193) | Abgedeckt |

## Fehlende oder unvollständige Planbestandteile

Nur ausfüllen, wenn Status `Plan lückenhaft`:

## Hinweise

- Die Befunde aus `plan-check.1.md` (fehlende konkrete E2E-Tests, fehlender Fehlerfall-Unit-Test, fehlende E2E-Hilfsmittel) wurden in der aktualisierten `plan.md` eingearbeitet.
- Die 7 offenen Punkte aus `requirement.md` wurden vom Auftraggeber bestätigt und im Plan in den Designentscheidungen beantwortet.
