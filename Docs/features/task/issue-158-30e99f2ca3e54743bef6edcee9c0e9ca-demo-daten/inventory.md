# Bestandsaufnahme: Demo-Daten bei Registrierung

Analysiert wurde der Registrierungs-, Hintergrundjob- und Service-Bereich von `Rezepte.Web` bezogen auf die Anforderung, bei der Registrierung optional Demo-Daten (5 Kochbücher, 43 Rezepte, 5 Kalendereinträge, Einkaufsliste) über einen Hintergrundjob anzulegen.

## Zusammenfassung

- **Vorhanden:** Vollständige Hintergrundjob-Infrastruktur (`IBackgroundJobQueue`/`BackgroundJobQueue` als Singleton mit Bounded Channel, `BackgroundJob`-Entity mit Persistenz, `BackgroundJobHostedService` mit Scope-pro-Job und Handler-Auflösung per `JobType`, `IBackgroundJobHandler`-Contract sowie zwei Beispiel-Handler `ExportUserJobHandler`/`ExportAllJobHandler`).
- **Vorhanden:** Alle benötigten Service-Methoden zum Anlegen der Demo-Daten: `ICookbookService.CreateAsync`, `IRecipeService.CreateAsync` (mit `RecipeCreateStep`/`RecipeCreateIngredient`), `ICalendarService.CreateEventAsync`, `IShoppingListService.EnsureDefaultGroupAsync`/`AddItemAsync`/`AddRecipeIngredientsAsync`.
- **Fehlt:** `Register.razor` hat keine Demo-Daten-Checkbox; `AuthController.Register` liest kein `createDemoData`-Feld; `RegisterRequest` und `RegisterRequestForm` haben kein `CreateDemoData`-Flag (`RegisterRequestForm` ist zudem deklariert, aber ungenutzt); `IUserService.RegisterAsync` hat keinen `createDemoData`-Parameter und `UserService` kennt `IBackgroundJobQueue` nicht; es gibt keinen `DemoDataSeedingJobHandler` und keine Demo-Datenquelle (43 Rezepte).
- **Beachten:** `UserService.RegisterAsync` wird in `UserServiceTests` vielfach mit 3 Argumenten aufgerufen; eine Signaturerweiterung betrifft alle diese Aufrufe sowie das Moq-Setup in `AuthControllerTests`.
- **Bekannte Eigenheit:** Die Plugin-Projekte referenzieren `Rezepte.Web.dll` per `HintPath`; ein Kaltstart-Build kann fehlschlagen, wenn `Rezepte.Web` noch nicht gebaut ist (siehe tests.md, Lauf 1).

Test-Ausgangszustand: Alle Suiten erfolgreich — 579/579 Unit-Tests (`Rezepte.Tests`) und 13/13 Browser-Tests (`Rezepte.Tests.Browser`), keine Fehlschläge, keine übersprungenen Tests. Kein bestehender Test für den Registrierungs-UI-Fluss oder Demo-Daten-Seeding. Nachweis: [Tests](inventory/tests.md).

## Details

- [Datenmodell](inventory/models.md)
- [Logik](inventory/logic.md)
- [Enums](inventory/enums.md)
- [Interfaces](inventory/interfaces.md)
- [Tests](inventory/tests.md)
