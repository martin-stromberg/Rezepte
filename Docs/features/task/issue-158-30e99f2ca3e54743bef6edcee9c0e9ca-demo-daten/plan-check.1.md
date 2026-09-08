# Plan-Gegenprüfung

## Ergebnis

**Status:** Plan lückenhaft

## Abgleich Akzeptanzkriterien

| Akzeptanzkriterium | Umsetzung im Plan | Testnachweis im Plan | Status |
|--------------------|-------------------|----------------------|--------|
| Checkbox in `Register.razor` zur Wahl, Demo-Daten anzulegen | UI-Schritt: `CreateDemoData`-Checkbox in `RegisterModel` und `Register.razor` | E2E-Szenarien `Register_WithDemoData` / `Register_WithoutDemoData`; AuthController-Form-Tests | Abgedeckt |
| Nach erfolgreicher Registrierung Start eines Hintergrundjobs über `IBackgroundJobQueue` | `UserService.RegisterAsync` enqueued `seed-demo-data`-Job bei `createDemoData == true` | `RegisterAsync_ShouldEnqueueDemoDataJob_WhenCreateDemoDataIsTrue` | Abgedeckt |
| Hintergrundjob legt 5 Kochbücher an | `DemoDataSeedingJobHandler` ruft `ICookbookService.CreateAsync` für 5 Namen | `HandleAsync_ShouldCreateFiveCookbooks` | Abgedeckt |
| Hintergrundjob legt 43 Rezepte an und ordnet sie den Kochbüchern zu | Handler iteriert `DemoDataSource` und ruft `IRecipeService.CreateAsync` | `HandleAsync_ShouldCreateFortyThreeRecipes` | Abgedeckt |
| Hintergrundjob legt 5 Kalendereinträge aus „Abendessen" für die nächsten 5 Tage an | Handler erzeugt 5 Events für `DateTime.Today.AddDays(1..5)` | `HandleAsync_ShouldCreateFiveCalendarEvents` | Abgedeckt |
| Hintergrundjob fügt Zutaten des Rezepts für den nächsten Tag der Einkaufsliste hinzu | Handler holt Standardgruppe und ruft `AddItemAsync` für Zutaten des ersten Kalendereintrags | `HandleAsync_ShouldAddIngredientsToDefaultShoppingList` | Abgedeckt |
| Demo-Daten werden über bestehende Service-Methoden erzeugt | Handler verwendet nur `ICookbookService`, `IRecipeService`, `ICalendarService`, `IShoppingListService` | Indirekt über Handler-Unit-Tests (Anzahl/Richtigkeit) | Abgedeckt |
| `CreateDemoData` in `RegisterRequest` und `RegisterRequestForm` (JSON und Form) | DTOs erweitert; `AuthController` liest aus Form bzw. JSON | AuthController-Tests für Form- und JSON-Weitergabe | Abgedeckt |
| Demo-Daten-Seeding optional, Default `false`, keine zentrale Konfiguration | Default `false` in DTOs/Modell; optionaler Konfigurationsschalter explizit nicht eingeführt | `RegisterAsync_ShouldNotEnqueueDemoDataJob_WhenCreateDemoDataIsFalse`; AuthController unchecked-Test | Abgedeckt |
| Fehlerbehandlung im Hintergrundjob: Kein Retry, Job-Status `Failed` | Validierungsregel: fehlender Benutzer / `ok == false` wirft Exception, Hosted Service setzt `Failed` | Kein expliziter Test geplant | Lücke |
| Demo-Daten enthalten keine Bilder, sind reguläre Datensätze, kein Gruppenlöschen | Designentscheidungen und Risikokapitel | Nicht funktional testbar / nicht erforderlich | Abgedeckt |

## Fehlende oder unvollständige Testanforderungen

Nur ausfüllen, wenn Status `Plan lückenhaft`:

- [ ] Fehlender Unit-Test für den Fehlerfall des `DemoDataSeedingJobHandler`: Nicht-existierender Benutzer oder `ok == false` einer Service-Operation führt zu einer Exception, sodass der Hosted Service den Job auf `Failed` setzt (vgl. Akzeptanzkriterium „Kein Retry, Job-Status Failed").
- [ ] E2E-Test `Register_WithDemoData` ist noch nicht ausreichend konkret: Setup (eindeutiger Testbenutzer, DB-Isolation/Warte-Logik für Hintergrundjob), exakte UI-Aktionen (Öffnen von `/register`, Eingabe, Anhaken der Checkbox, Absenden) und sichtbares Ergebnis (Login, Navigieren zu Rezept-/Kochbuch-/Kalender-/Einkaufslisten-Seiten, Zählen der erwarteten Einträge) fehlen im Plan.
- [ ] E2E-Test `Register_WithoutDemoData` ist noch nicht ausreichend konkret: Fehlende Angabe von Setup, exakter Benutzeraktion und sichtbarem Negativ-Ergebnis (z. B. „Meine Rezepte" zeigt 0 Einträge).
- [ ] Notwendige E2E-Hilfsmittel/Fixtures fehlen: Unique-Username-Strategie, Hintergrundjob-Poll-/Warte-Helper, Playwright-Page-Object für `Register.razor`.

## E2E-Abdeckung

| Benutzerfluss / Akzeptanzkriterium | Geplanter E2E-Test | Status |
|------------------------------------|--------------------|--------|
| Registrierung mit angehaktem Demo-Daten-Checkbox → anschließend 5 Kochbücher, 43 Rezepte, 5 Kalendereinträge, Einkaufslisteneinträge | `Register_WithDemoData` in `Rezepte.Tests.Browser/Auth/RegisterTests.cs` | Lücke (Szenario skizziert, aber exakte Schrittfolge, Setup und sichtbares Ergebnis noch nicht konkretisiert) |
| Registrierung ohne Demo-Daten-Checkbox → keine Demo-Daten sichtbar | `Register_WithoutDemoData` in `Rezepte.Tests.Browser/Auth/RegisterTests.cs` | Lücke (keine konkreten UI-Schritte und Überprüfung beschrieben) |

## Fehlende oder unvollständige Planbestandteile

Nur ausfüllen, wenn Status `Plan lückenhaft`:

- [ ] Konkrete E2E-Testbeschreibung mit Setup, auslösender Nutzeraktion und sichtbarem Ergebnis fehlt im Abschnitt `E2E-Tests`.
- [ ] Fehlender Testfall/Validator für den `DemoDataSeedingJobHandler`-Fehlerpfad (kein Benutzer, fehlgeschlagene Service-Aufrufe).
- [ ] Notwendige E2E-Testdaten, Fixtures und Hilfsmethoden sind nicht vollständig genannt (Unique-User, Job-Wait-Helper, Page-Object).

## Hinweise

- Die 7 offenen Punkte aus dem Plan wurden vom Anwender bestätigt; die darin getroffenen Designentscheidungen gelten als geklärt.
- Der Umsetzungsplan ist ansonsten nachvollziehbar; die wesentlichen Lücken liegen in der noch nicht hinreichend konkreten E2E-Testplanung und im fehlenden Fehlerfall-Test für den Hintergrundjob.
