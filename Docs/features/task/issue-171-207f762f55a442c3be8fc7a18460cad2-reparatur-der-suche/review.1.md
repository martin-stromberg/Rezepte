# Plan-Review

## Ergebnis

**Status:** Offene Aufgaben vorhanden

## Umgesetzte Planelemente

### Produktivcode — `RecipeSearch.razor`

- [x] Direktive `@rendermode InteractiveServer` — vorhanden (Z. 2)
- [x] Eigenschaft `CurrentPage` (`pageNumber < 1 ? 1 : pageNumber`) — vorhanden (Z. 96), verwendet in Request-URI (Z. 130), `ChangePage` (Z. 64/70) und Paginierungsanzeige „Seite @CurrentPage von @TotalPages" (Z. 67)
- [x] Eigenschaft `CurrentPageSize` — Clamping auf `pageSize < 1` erweitert (Z. 97, bisher `== 0`)
- [x] Methode `RunSearchAsync` — Request-URI relativ (`api/recipes/search`, Z. 130); `catch (OperationCanceledException)` aufgespalten: `ReferenceEquals(cts, localCts)` → `LogWarning` + `searchFailed = true`, sonst Supersede-Fall → `LogInformation`, folgenlos (Z. 139–152)
- [x] Markup Paginierungsanzeige — nutzt `CurrentPage` statt `pageNumber` (Z. 63–70)

### Produktivcode — `MainLayout.razor`

- [x] Suchformular `action="/recipes/search"` + `method="get"` — vorhanden (Z. 39)

### Produktivcode — `CalendarEventDialog.razor`

- [x] Feld `recipeSearchFailed` (`bool`) — vorhanden (Z. 272)
- [x] `alert-danger`-Markup mit `@Localizer["SearchFailed"]` im Picker-Ergebnisbereich — vorhanden (Z. 211–214), vor `NoSearchResults`-Zweig priorisiert
- [x] Methode `TriggerRecipeSearchAsync` — Request-URI relativ (Z. 504); Fallback-Request per Exception-Filter auf `JsonException`/`NotSupportedException` beschränkt (Z. 510); alle anderen Fehler inkl. Abbruch laufen in den äußeren `catch` → `recipeSearchFailed = true`, `LogError` (Z. 530–535)

### Testklassen und -helfer

- [x] `RecipeServiceSearchSqliteTests` (`Rezepte.Tests/Services/`) — angelegt; `CreateSqliteDb`-Helfer mit `SqliteConnection("DataSource=:memory:")`, `UseSqlite`, `EnsureCreated` (Z. 22–34); alle 6 geplanten Testmethoden vorhanden: `SearchAsync_ShouldFindByTitle_OnSqlite`, `SearchAsync_ShouldBeCaseInsensitiveAscii_OnSqlite`, `SearchAsync_ShouldFindByStepDescription_OnSqlite`, `SearchAsync_ShouldFindByIngredientName_OnSqlite`, `SearchAsync_ShouldNotReturnForeignUserRecipes_OnSqlite`, `SearchAsync_ShouldPageAndCount_OnSqlite`
- [x] `StubHttpMessageHandler` (`Rezepte.Tests/Components/`) — angelegt; `WithJson<T>`, `Throwing(Exception)` (u. a. `OperationCanceledException`-fähig), `LastRequest` zur URI-Prüfung
- [x] `RecipeSearchComponentTests` (`Rezepte.Tests/Components/`, bUnit) — angelegt; `AddLocalization()`, eigener `PathBaseNavigationManager` (`http://localhost/sub/`), `ApiClient` mit `new HttpClient(StubHttpMessageHandler)`; alle 3 Testmethoden vorhanden: `RunSearchAsync_WhenCancelled_ShowsSearchFailed` (alert-danger statt alert-secondary), `RunSearchAsync_WhenEmpty_ShowsNoSearchResults`, `RunSearchAsync_WhenResults_RendersListItems` (assertiert relative URI `/sub/api/recipes/search`)
- [x] `RecipesControllerSearchTests` (`Rezepte.Tests/Controllers/`) — angelegt; `Mock<IRecipeService>`, `IOptions<ImageOptions>`, `ClaimsPrincipal` mit `NameIdentifier`; alle 3 Testmethoden vorhanden: `SearchAsync_ShouldClampPageAndPageSize`, `SearchAsync_WithoutUserClaim_ReturnsUnauthorized`, `SearchAsync_MapsToResponseDto` (`Total`, `PublishedLabel` `yyyy-MM-dd`, Snippet-Kappung 200 + „…")
- [x] `RecipeSeedHelper` (`Rezepte.Tests.Browser/Infrastructure/`) — angelegt; `page.Context.APIRequest` → `GET api/session/token` → `POST api/recipes` mit Bearer-JWT (`SeedRecipesAsync`, Z. 20–61)
- [x] `RecipeSearchBrowserTests` (`Rezepte.Tests.Browser/`, Collection `BrowserTestCollection`) — angelegt; alle 5 geplanten Szenarien vorhanden: `NavbarSearch_ShowsSeededRecipe`, `DirectUrl_WithQuery_ShowsResults`, `Search_WithoutMatches_ShowsNoSearchResults`, `Pagination_NextPage_ShowsPageTwo`, `InPageSearch_Submit_UpdatesResultList`
- [x] `LoadingBarFormNavigationBrowserTests.SearchSubmit_ShowsLoadingBar` — Inline-Kommentare auf den neuen Mechanismus aktualisiert (Z. 40–47 beschreiben Enhanced Navigation via `enhancednavigationstart` bzw. nativen Fallback via `beforeunload`)

### Verifikationsnachweise (im Rahmen dieses Reviews ausgeführt)

- `dotnet build Rezepte.Tests -c Release` — erfolgreich, 0 Warnungen, 0 Fehler (CS1591-Doku-Auflagen erfüllt)
- `dotnet build Rezepte.Tests.Browser -c Release` — erfolgreich
- `dotnet test Rezepte.Tests --filter "RecipeSearchComponentTests|RecipeServiceSearchSqliteTests|RecipesControllerSearchTests"` — 12/12 grün
- `dotnet publish Rezepte.Web -c Release` + `dotnet test Rezepte.Tests.Browser --filter "RecipeSearchBrowserTests|LoadingBarFormNavigationBrowserTests"` — 6/7 grün im ersten Lauf; `Pagination_NextPage_ShowsPageTwo` beim isolierten Re-Run grün (siehe Hinweise)

## Offene Aufgaben

- [ ] Vollständiger Testlauf `dotnet test Rezepte.sln --configuration Release` (Tasks-#33) — fehlt: Der dokumentierte Lauf in `inventory/test-results/` (24.10. 21:03–21:06, 590 + 15 grün) stammt aus der Bestandsaufnahme-Phase und liegt zeitlich **vor** allen Implementierungsänderungen (25.10. ~05:25); er umfasst die neuen Tests nicht. Im Review wurden nur Teilmengen ausgeführt (12 Unit-/Komponenten-/Controller-Tests, 7 Browser-Tests) — ein vollständiger Lauf über alle Suiten steht noch aus.
- [ ] Produktiv-Verifikation des Fehlpfads (Plan-Schritt 10, in der Tasks-Datei neu als #34 ergänzt) — fehlt: deploymentabhängig; Suche im Produktivsystem erneut ausführen und anhand der jetzt sichtbaren `SearchFailed`-Meldung sowie der Log-Zeilen (`LogWarning` „was cancelled or timed out" vs. `LogError`) den tatsächlichen Fehlpfad bestätigen.

## Hinweise

- **Flake-Risiko `Pagination_NextPage_ShowsPageTwo`:** Im ersten Lauf schlug die Assertion `items.Count == 5` mit 0 Treffern fehl (`RecipeSearchBrowserTests.cs:104`), im Re-Run bestand der Test. Vermutete Ursache ist der im Plan dokumentierte Prerender/Circuit-Doppellauf von `RunSearchAsync`: `items.First.WaitForAsync` wird durch die Prerender-Trefferliste erfüllt, danach leert der zweite (Circuit-)Lauf die Liste während `isLoading`. Für den finalen Suite-Lauf ggf. Beobachtung/Retry einplanen.
- **Zusätzliche Ausführungsnachweise:** Alle 12 neuen Unit-/Komponenten-/Controller-Tests und (nach Re-Run) alle 7 relevanten Browser-Tests laufen grün — einschließlich `LoadingBarFormNavigationBrowserTests.SearchSubmit_ShowsLoadingBar` unter dem neuen Enhanced-Navigation-Mechanismus.
- Der `CalendarEventDialog`-Fix (Tasks #7–#9) ist plangemäß ohne dedizierten Test umgesetzt; die Zustandslogik deckt sich mit dem Muster aus `RecipeSearch.razor`.
