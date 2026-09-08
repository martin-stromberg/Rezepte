# Plan-Review

## Ergebnis

**Status:** Vollständig umgesetzt

## Umgesetzte Planelemente

- [x] `DemoDataSeedingJobHandler` (Klasse, `IBackgroundJobHandler`) — angelegt in `Rezepte.Web/Services/BackgroundJobs/Handlers/DemoDataSeedingJobHandler.cs`; `JobType = "seed-demo-data"` (Z. 15), parst Payload (Z. 29), validiert Benutzer (Z. 35–40), legt 5 Kochbücher (Z. 50–60), 43 Rezepte (Z. 62–91), 5 Kalendereinträge für `Tag+1` bis `Tag+5` um 18:00 (Z. 93–110) und Einkaufslisteneinträge des ersten Kalenderrezepts in der Standardgruppe an (Z. 112–137); wirft Exceptions bei `ok == false`.
- [x] `DemoDataSeedPayload` (Record) — angelegt in `Rezepte.Web/Services/BackgroundJobs/DemoDataSeedPayload.cs` mit `UserId`, `ToJson()`/`FromJson()`.
- [x] `DemoDataSource` (statische Klasse) — angelegt in `Rezepte.Web/Services/DemoDataSource.cs`; `Cookbooks`/`Recipes` als `IReadOnlyList`, 43 Rezepte auf 5 Kochbücher verteilt (Frühstück 8, Abendessen 10, Snacks 9, Weihnachtszeit 8, Grillsaison 8), basiert auf `RecipeCreateStep`/`RecipeCreateIngredient`.
- [x] `DemoCookbook` (Record) — angelegt in `Rezepte.Web/Services/DemoCookbook.cs` mit `Name` und `Recipes`.
- [x] `DemoRecipe` (Record) — angelegt in `Rezepte.Web/Services/DemoRecipe.cs` mit `Title`, `Description`, `Portions`, `CookbookName`, `Steps` (`IReadOnlyList<RecipeCreateStep>`).
- [x] Eigenschaft `CreateDemoData` in `RegisterRequest` — vorhanden in `Rezepte.Web/Contracts/AuthDtos.cs` Z. 17 (`bool`, Default `false`).
- [x] Feld `CreateDemoData` in `RegisterRequestForm` — vorhanden in `Rezepte.Web/Controllers/AuthController.cs` Z. 104 (Default `false`).
- [x] Methode `Register` in `AuthController` — liest `createDemoData` aus `Request.Form` via `bool.TryParse` (Z. 46) bzw. aus `RegisterRequest.CreateDemoData` (Z. 56) und übergibt es als drittes Argument an `RegisterAsync` (Z. 79).
- [x] Eigenschaft `CreateDemoData` im `RegisterModel` in `Register.razor` — vorhanden (Z. 57).
- [x] Checkbox in `Register.razor` — `InputCheckbox` mit `name="createDemoData"` und Localizer-Text (Z. 29–32); Localizer-Eintrag `CreateDemoData` in `UiStrings.resx` vorhanden.
- [x] `IUserService.RegisterAsync`-Signatur — erweitert auf `(string username, string password, bool createDemoData, CancellationToken ct)` (`Rezepte.Web/Services/UserService.cs` Z. 37).
- [x] `UserService`-Dependency `IBackgroundJobQueue` — über Konstruktor injiziert (Z. 126/130).
- [x] `UserService.RegisterAsync` — enqueuet bei `createDemoData == true` Job `"seed-demo-data"` mit serialisierter `DemoDataSeedPayload` und `user.Id` (Z. 155–159).
- [x] DI-Registrierung `services.AddScoped<IBackgroundJobHandler, DemoDataSeedingJobHandler>()` — vorhanden in `Rezepte.Web/Extensions/ServiceCollectionExtensions.cs` Z. 199.
- [x] `UserServiceTests` angepasst — alle `RegisterAsync`-Aufrufe nutzen vier Argumente; `CreateSut` erzeugt `Mock<IBackgroundJobQueue>` (Z. 28–30).
- [x] `UserServiceTests.RegisterAsync_ShouldEnqueueDemoDataJob_WhenCreateDemoDataIsTrue` — vorhanden (Z. 309 ff., verifiziert `EnqueueAsync("seed-demo-data", ...)`).
- [x] `UserServiceTests.RegisterAsync_ShouldNotEnqueueDemoDataJob_WhenCreateDemoDataIsFalse` — vorhanden (Z. 332 ff.).
- [x] `AuthControllerTests` Moq-Setups angepasst — `RegisterAsync`-Setups/Verifies mit `bool`-Parameter (`Rezepte.Tests/Controllers/AuthControllerTests.cs`).
- [x] `AuthControllerTests.Register_ShouldCallUserServiceWithCreateDemoDataTrue_WhenFormCheckboxChecked` — vorhanden (Z. 46).
- [x] `AuthControllerTests.Register_ShouldCallUserServiceWithCreateDemoDataFalse_WhenFormCheckboxUnchecked` — vorhanden (Z. 69).
- [x] `AuthControllerTests.Register_ShouldCallUserServiceWithCreateDemoDataFromJson_WhenProvided` — vorhanden (Z. 92).
- [x] `DemoDataSeedingJobHandlerTests` — angelegt in `Rezepte.Tests/Services/BackgroundJobs/DemoDataSeedingJobHandlerTests.cs` mit `HandleAsync_ShouldCreateFiveCookbooks`, `HandleAsync_ShouldCreateFortyThreeRecipes`, `HandleAsync_ShouldCreateFiveCalendarEvents`, `HandleAsync_ShouldAddIngredientsToDefaultShoppingList`, `HandleAsync_ShouldThrow_WhenUserDoesNotExist`, `HandleAsync_ShouldThrow_WhenAnyServiceOperationFails`.
- [x] `RegisterPage` Page-Object — angelegt in `Rezepte.Tests.Browser/Auth/RegisterPage.cs` mit `GotoAsync`, `FillUsernameAsync`, `FillEmailAsync`, `FillPasswordAsync`, `CheckCreateDemoDataAsync`, `SubmitAsync`, `GetErrorAsync` (plus `LoginAsync`).
- [x] `UniqueUsernameGenerator` — lokale Hilfsmethode in `Rezepte.Tests.Browser/Auth/RegisterTests.cs` Z. 99–102 (`demo_e2e_{guid}_{timestamp}`).
- [x] `DemoDataWaitHelper` — lokale Hilfsklasse in `RegisterTests.cs` Z. 137–170; poll die UI-Listen `/cookbooks`, `/recipes/search`, `/calendar`, `/shopping-list` mit Timeout.
- [x] E2E-Test `Register_WithDemoData` — vorhanden als `RegisterTests.Register_WithDemoData_CreatesDemoData` (Z. 38–64; prüft 5 Kochbücher, 43 Rezepte, 5 Kalendereinträge, >0 Einkaufslisteneinträge).
- [x] E2E-Test `Register_WithoutDemoData` — vorhanden als `RegisterTests.Register_WithoutDemoData_LeavesListsEmpty` (Z. 71–97; prüft leere Listen).
- [x] Folgeanpassung: `AdminUsersController` ruft `RegisterAsync(..., false, ct)` auf (`Rezepte.Web/Controllers/AdminUsersController.cs` Z. 53).

## Hinweise

- Die E2E-Testnamen weichen geringfügig vom Plan ab (`Register_WithDemoData_CreatesDemoData` statt `Register_WithDemoData`, `Register_WithoutDemoData_LeavesListsEmpty` statt `Register_WithoutDemoData`); inhaltlich identisch abgedeckt.
- `RegisterRequest.CreateDemoData` trägt `[param: Required]`; bei `bool` mit Default `false` ist das ohne Bindungseffekt — fehlende Felder im JSON-Body ergeben `false`, wie geplant.
- Der Handler verwendet bei Kalendereinträgen `recipe.Portions > 0 ? recipe.Portions : 2` als Portionszahl (Plan nennt `portions` ohne konkreten Wert); zudem wird die Einkaufsliste aus den `DemoRecipe.Steps` der `DemoDataSource` befüllt (Titelvergleich zum ersten Kalenderrezept) statt aus den persistierten Rezept-Entitäten — funktional gleichwertig, da die Mock-Rezepte keine Zutaten liefern.
- `RegisterRequestForm` ist deklariert, wird aber vom Controller nicht gebunden (Form-Daten werden via `Request.Form` gelesen) — entspricht dem Plan ("deklariert für Konsistenz").
- Die Tasks-Datei `issue-158-...-demo-daten-tasks.md` wurde aktualisiert: alle 25 Aufgaben auf `Erledigt` mit Testnachweis.
