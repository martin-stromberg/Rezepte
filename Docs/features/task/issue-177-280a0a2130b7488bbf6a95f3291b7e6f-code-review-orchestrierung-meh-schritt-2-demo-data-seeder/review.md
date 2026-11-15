# Plan-Review

## Ergebnis

**Status:** Vollständig umgesetzt

## Umgesetzte Planelemente

- [x] `IDemoDataSeeder` (Interface, `Rezepte.Web/Services/IDemoDataSeeder.cs`) mit `Task SeedForUserAsync(string userId, CancellationToken ct)` — vorhanden
- [x] `DemoDataSeeder` (scoped Klasse, `Rezepte.Web/Services/DemoDataSeeder.cs`) mit Konstruktor-Injektion von `ICookbookService`, `IRecipeService`, `ICalendarService`, `IShoppingListService`, `ILogger<DemoDataSeeder>` — vorhanden
- [x] Konstanten `CalendarEventCount` (5), `DefaultPortions` (2), `DinnerHour` (18), `DemoImagesPath` — in `DemoDataSeeder` übernommen (`DemoDataSeeder.cs:16-37`)
- [x] Private Methoden `SeedCookbooksAsync`, `SeedRecipesAsync`, `OpenDemoImages`, `AttachImageAsync`, `GetImageContentType`, `SeedCalendarAsync`, `SeedShoppingListAsync`, `SeededRecipe`-Record — 1:1 in `DemoDataSeeder` umgezogen, inhaltlich unverändert (Parameter `recipeService`/`calendarService`/`shoppingListService`/`logger` durch injizierte Felder ersetzt)
- [x] `DemoDataSeedingJobHandler.HandleAsync` reduziert auf: `DemoDataSeedPayload.FromJson` + UserId-Prüfung + `IUserService.GetByIdAsync` + `IDemoDataSeeder`-Auflösung/-Aufruf — vorhanden (`DemoDataSeedingJobHandler.cs:20-40`); `JobTypeName`/`JobType` unverändert
- [x] `services.AddScoped<IDemoDataSeeder, DemoDataSeeder>()` — vorhanden (`ServiceCollectionExtensions.cs:197`)
- [x] `DemoDataSeederTests_Sequence` — 4 Tests (5 Cookbooks, 43 Rezepte `uri: null`, 5 Kalenderevents `Today+1..5` 18:00 `RecurrenceType.None`/`WeekDays.None`, 6 ShoppingList-Items + `EnsureDefaultGroupAsync`)
- [x] `DemoDataSeederTests_Errors` — 5 Tests (Fehler je Sequenzschritt → `InvalidOperationException`; `AddImageAsync`-Misserfolg tolerant)
- [x] `DemoDataSeederMocks` — gemeinsame Mock-Fabrik (aus `HappyPathMocks` übernommen, ohne `ServiceProvider`/`IUserService`)
- [x] `DemoDataSeedingJobHandlerTests` umgebaut — 4 Tests: `HandleAsync_WithValidJob_CallsSeedForUserAsync`, `…WhenUserDoesNotExist_Throws…` (+ Seeder nie aufgerufen), `…WhenUserIdEmpty_Throws…` (+ Seeder nie aufgerufen), `…WhenSeederThrows_PropagatesException`
- [x] Build (`dotnet build Rezepte.sln`, 0 Fehler/0 Warnungen) und Testsuite (`dotnet test Rezepte.Tests`: 714/714 grün, Baseline 707 + 9 neue Seeder-Tests − 2 umgezogene Handler-Sequenz-Tests)
- [ ] Doku-Anpassung `Docs/help/user-accounts.md` — bewusst offen: erfolgt im Lifecycle-Schritt 12 (Dokumentation), nicht Teil der Implementierung
- [ ] E2E-Ausführung `RegisterTests.Register_WithDemoData_CreatesDemoData` — bewusst offen: erfolgt im Lifecycle-Schritt 10 (Tests)

## Hinweise

- Unverändert geblieben wie geplant: `UserServiceTests`-Enqueue-Tests, `Rezepte.Tests.Browser/Auth/RegisterTests.cs`, `JobTypeName`, `DemoDataSeedPayload`, `DemoDataSource`, Domänen-Service-Signaturen, Job-Infrastruktur.
- Die Tasks 9–10 der Tasks-Datei sind keine Implementierungsplanelemente im engeren Sinn (Doku = Schritt 12, E2E-Nachweis = Schritt 10); die Implementierungselemente 1–8 sind vollständig.
