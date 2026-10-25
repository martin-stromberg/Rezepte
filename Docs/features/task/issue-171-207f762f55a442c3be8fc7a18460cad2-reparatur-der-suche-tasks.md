# Tasks: Reparatur der Rezeptsuche (Issue #171)

| # | Bereich | Aufgabe | Status | Testnachweis |
|---|---------|---------|--------|--------------|
| 1 | UI | `@rendermode InteractiveServer` in `RecipeSearch.razor` ergänzen | Offen | — |
| 2 | UI | `RunSearchAsync` in `RecipeSearch.razor`: Request-URI von absolut (`/api/recipes/search`) auf relativ (`api/recipes/search`) umstellen | Offen | — |
| 3 | UI | `RunSearchAsync` in `RecipeSearch.razor`: `OperationCanceledException` in Supersede- vs. Timeout-Fall aufspalten (Timeout → `searchFailed = true`, `LogWarning`) | Offen | — |
| 4 | UI | `CurrentPage`-Property (`pageNumber < 1 ? 1 : pageNumber`) in `RecipeSearch.razor` einführen und in URI-Aufbau, `ChangePage` und Paginierungsanzeige verwenden | Offen | — |
| 5 | UI | `CurrentPageSize` in `RecipeSearch.razor` auf `pageSize < 1` erweitern (bisher nur `== 0`) | Offen | — |
| 6 | UI | `MainLayout.razor`: Suchformular `action="recipes/search"` → `action="/recipes/search"` und `method="get"` ergänzen | Offen | — |
| 7 | UI | `CalendarEventDialog.razor`: Request-URI von absolut auf relativ (`api/recipes/search`) umstellen | Offen | — |
| 8 | UI | `CalendarEventDialog.razor`: `recipeSearchFailed`-Flag und `alert-danger`-Markup mit `Localizer["SearchFailed"]` im Picker-Ergebnisbereich ergänzen | Offen | — |
| 9 | UI | `CalendarEventDialog.razor`: Fallback-Request in `TriggerRecipeSearchAsync` auf `JsonException`/`NotSupportedException` beschränken; andere Fehler inkl. Abbruch → `recipeSearchFailed = true` | Offen | — |
| 10 | Tests | `RecipeServiceSearchSqliteTests` in `Rezepte.Tests/Services/` anlegen inkl. `CreateSqliteDb`-Helfer (`SqliteConnection("DataSource=:memory:")` + `UseSqlite` + `EnsureCreated`) | Offen | — |
| 11 | Tests | `SearchAsync_ShouldFindByTitle_OnSqlite` schreiben (exakter Teilstring-Treffer über `Title`) | Offen | — |
| 12 | Tests | `SearchAsync_ShouldBeCaseInsensitiveAscii_OnSqlite` schreiben (`honig` findet `Honig`) | Offen | — |
| 13 | Tests | `SearchAsync_ShouldFindByStepDescription_OnSqlite` schreiben (Treffer über `RecipeStep.Description`) | Offen | — |
| 14 | Tests | `SearchAsync_ShouldFindByIngredientName_OnSqlite` schreiben (Treffer über `RecipeIngredient.Name`) | Offen | — |
| 15 | Tests | `SearchAsync_ShouldNotReturnForeignUserRecipes_OnSqlite` schreiben (`UserId`-Filter) | Offen | — |
| 16 | Tests | `SearchAsync_ShouldPageAndCount_OnSqlite` schreiben (`TotalCount` + `Skip/Take`) | Offen | — |
| 17 | Tests | `StubHttpMessageHandler`-Hilfsklasse anlegen (konfigurierbare JSON-Antwort oder Exception für `ApiClient`) | Offen | — |
| 18 | Tests | `RecipeSearchComponentTests` (bUnit) in `Rezepte.Tests/Components/` anlegen (`AddLocalization()`, `FakeNavigationManager`, `ApiClient` mit `StubHttpMessageHandler`) | Offen | — |
| 19 | Tests | `RunSearchAsync_WhenCancelled_ShowsSearchFailed` schreiben (Abbruch → `alert-danger`, nicht `alert-secondary`) | Offen | — |
| 20 | Tests | `RunSearchAsync_WhenEmpty_ShowsNoSearchResults` schreiben | Offen | — |
| 21 | Tests | `RunSearchAsync_WhenResults_RendersListItems` schreiben (inkl. relative Request-URI) | Offen | — |
| 22 | Tests | `RecipesControllerSearchTests` in `Rezepte.Tests/Controllers/` anlegen (Moq `IRecipeService`, `IOptions<ImageOptions>`, `ClaimsPrincipal` mit `NameIdentifier`) | Offen | — |
| 23 | Tests | `SearchAsync_ShouldClampPageAndPageSize` schreiben (`page`/`pageSize` → geclamped an Service) | Offen | — |
| 24 | Tests | `SearchAsync_WithoutUserClaim_ReturnsUnauthorized` schreiben | Offen | — |
| 25 | Tests | `SearchAsync_MapsToResponseDto` schreiben (`Total`, `PublishedLabel`-Format, Snippet-Kappung) | Offen | — |
| 26 | E2E-Tests | `RecipeSeedHelper` in `Rezepte.Tests.Browser/Infrastructure/` anlegen (`page.Context.APIRequest` → `GET api/session/token` → `POST api/recipes` mit Bearer-JWT) | Offen | — |
| 27 | E2E-Tests | `RecipeSearchBrowserTests` (Collection `BrowserTestCollection`) anlegen: Navbar-Suche → Trefferliste zeigt Rezept | Offen | — |
| 28 | E2E-Tests | Direktaufruf `GET /recipes/search?q=…` zeigt Treffer (SSR-/Prerender-Pfad) | Offen | — |
| 29 | E2E-Tests | Suche ohne Treffer → `alert-secondary` „Keine Treffer." (kein `SearchFailed`) | Offen | — |
| 30 | E2E-Tests | Paginierung: > `pageSize` Rezepte seeden → „»"-Button → „Seite 2 von …" mit Resttreffern | Offen | — |
| 31 | E2E-Tests | In-Page-Suche über `#search-q`-Formular aktualisiert Trefferliste | Offen | — |
| 32 | Tests | `LoadingBarFormNavigationBrowserTests.SearchSubmit_ShowsLoadingBar` unter neuem Navigationsmechanismus (Enhanced Navigation) verifizieren und Inline-Kommentare aktualisieren | Offen | — |
| 33 | Abschluss | Vollständiger Testlauf `dotnet test Rezepte.sln --configuration Release` (vorher `dotnet publish Rezepte.Web -c Release` für Browser-Tests) — alle Suiten grün | Offen | — |
