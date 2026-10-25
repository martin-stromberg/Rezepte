# Plan-Review

## Ergebnis

**Status:** Vollständig umgesetzt

## Umgesetzte Planelemente

### Produktivcode — `RecipeSearch.razor` (`Rezepte.Web/Components/Pages/`)

- [x] Direktive `@rendermode InteractiveServer` — vorhanden (Z. 2)
- [x] Eigenschaft `CurrentPage` (`pageNumber < 1 ? 1 : pageNumber`) — vorhanden (Z. 101), verwendet in Request-URI (Z. 127), `ChangePage` (Z. 168–172) und Paginierungsanzeige „Seite @CurrentPage von @TotalPages" (Z. 68–75)
- [x] Eigenschaft `CurrentPageSize` — Clamping auf `pageSize < 1` erweitert (Z. 102)
- [x] Methode `RunSearchAsync` — Request-URI relativ (`api/recipes/search`, Z. 127); `catch (OperationCanceledException)` aufgespalten in Timeout-/Abbruch-Fall (`ReferenceEquals(cts, localCts)` → `LogWarning` + `searchFailed = true`, Z. 138–144) und Supersede-Fall (`LogInformation`, folgenlos, Z. 145–148); `catch (Exception)` → `LogError` + `searchFailed` (Z. 150–156); `finally`-Block ebenfalls an `ReferenceEquals` gekoppelt — Code-Review-Befund aus Iteration 1 behoben (Z. 157–165)
- [x] Markup Paginierungsanzeige — nutzt `CurrentPage` statt `pageNumber` (Z. 68–75)
- [x] Suchformular — nach Iteration 1 von `@onsubmit` auf natives GET-Formular umgestellt (`method="get"`, `name="q"`, verstecktes `pageSize`-Feld, Z. 16–20); erfüllt das Planziel „funktionierender Submit" robuster (funktioniert auch ohne Circuit, PathBase-erhaltend) — siehe Hinweise

### Produktivcode — `MainLayout.razor` (`Rezepte.Web/Components/Layout/`)

- [x] Suchformular `action="/recipes/search"` + `method="get"` — vorhanden (Z. 39)

### Produktivcode — `CalendarEventDialog.razor` (`Rezepte.Web/Components/Shared/`)

- [x] Feld `recipeSearchFailed` (`bool`) — vorhanden (Z. 272)
- [x] `alert-danger`-Markup mit `@Localizer["SearchFailed"]` im Picker-Ergebnisbereich — vorhanden (Z. 211–214), vor `NoSearchResults`-Zweig priorisiert
- [x] Methode `TriggerRecipeSearchAsync` — Request-URI relativ (Z. 504); Fallback-Request per Exception-Filter auf `JsonException`/`NotSupportedException` beschränkt (Z. 510); alle anderen Fehler inkl. Abbruch → `recipeSearchFailed = true`, `LogError` (Z. 530–535)
- [x] Übrige `Client.Http`-Aufrufe der Komponente auf relative URIs umgestellt (`api/recipes/{id}` Z. 326, `api/calendar` Z. 382/390, `api/calendar/{id}` Z. 398, `api/recipes/{id}/side-dishes` Z. 582) — Code-Review-Befund aus Iteration 1 behoben

### Testklassen und -helfer

- [x] `RecipeServiceSearchSqliteTests` (`Rezepte.Tests/Services/`) — vorhanden; `CreateSqliteDb`-Helfer mit `SqliteConnection("DataSource=:memory:")`, `UseSqlite`, `EnsureCreated` (Z. 32–46); Connection als Feld gehalten und via `IDisposable` geschlossen (Z. 22–30) — Code-Review-Befund aus Iteration 1 behoben; alle 6 geplanten Testmethoden vorhanden: `SearchAsync_ShouldFindByTitle_OnSqlite`, `SearchAsync_ShouldBeCaseInsensitiveAscii_OnSqlite`, `SearchAsync_ShouldFindByStepDescription_OnSqlite`, `SearchAsync_ShouldFindByIngredientName_OnSqlite`, `SearchAsync_ShouldNotReturnForeignUserRecipes_OnSqlite`, `SearchAsync_ShouldPageAndCount_OnSqlite`
- [x] `StubHttpMessageHandler` (`Rezepte.Tests/Components/`) — vorhanden; `WithJson<T>`, `Throwing(Exception)` (u. a. `OperationCanceledException`-fähig), `LastRequest` zur URI-Prüfung
- [x] `RecipeSearchComponentTests` (`Rezepte.Tests/Components/`, bUnit) — vorhanden; `AddLocalization()`, `PathBaseNavigationManager` (`http://localhost/sub/`), `ApiClient` mit `StubHttpMessageHandler`; alle 3 Testmethoden vorhanden: `RunSearchAsync_WhenCancelled_ShowsSearchFailed`, `RunSearchAsync_WhenEmpty_ShowsNoSearchResults`, `RunSearchAsync_WhenResults_RendersListItems` (assertiert relative URI `/sub/api/recipes/search`)
- [x] `RecipesControllerSearchTests` (`Rezepte.Tests/Controllers/`) — vorhanden; `Mock<IRecipeService>`, `IOptions<ImageOptions>`, `ClaimsPrincipal` mit `NameIdentifier`; Clamping-Test als `[Theory]` mit zwei Grenzfällen umgesetzt (Z. 48–61) — Code-Review-Befund aus Iteration 1 behoben; `SearchAsync_WithoutUserClaim_ReturnsUnauthorized`, `SearchAsync_MapsToResponseDto` vorhanden
- [x] `RecipeSeedHelper` (`Rezepte.Tests.Browser/Infrastructure/`) — vorhanden; `page.Context.APIRequest` → `GET api/session/token` → `POST api/recipes` mit Bearer-JWT (`SeedRecipesAsync`, Z. 20–61)
- [x] `RecipeSearchBrowserTests` (`Rezepte.Tests.Browser/`, Collection `BrowserTestCollection`) — vorhanden; alle 5 geplanten Szenarien: `NavbarSearch_ShowsSeededRecipe`, `DirectUrl_WithQuery_ShowsResults`, `Search_WithoutMatches_ShowsNoSearchResults`, `Pagination_NextPage_ShowsPageTwo`, `InPageSearch_Submit_UpdatesResultList`
- [x] `LoadingBarFormNavigationBrowserTests.SearchSubmit_ShowsLoadingBar` — Inline-Kommentare auf den neuen Mechanismus aktualisiert (Enhanced Navigation via `enhancednavigationstart` bzw. nativer Fallback via `beforeunload`)

### Verifikationsnachweise (im Rahmen dieses Reviews ausgeführt)

- `dotnet build Rezepte.sln --configuration Release` — erfolgreich, 0 Warnungen, 0 Fehler (inkl. Publish-Ausgabe für Browser-Tests)
- `dotnet test Rezepte.sln --configuration Release --no-build` — **623/623 grün**: `Rezepte.Tests` 603 bestanden, `Rezepte.Tests.Browser` 20 bestanden; darunter der in Iteration 1 fehlgeschlagene `InPageSearch_Submit_UpdatesResultList` (grün nach Umstellung des In-Page-Formulars auf natives GET) sowie der als flake-beobachtete `Pagination_NextPage_ShowsPageTwo` (grün)

## Hinweise

- **Einschränkung (keine offene Plan-Aufgabe):** Tasks-#34 (Produktiv-Verifikation des Fehlpfads, Plan-Schritt 10) ist deploymentabhängig und in diesem Zyklus nicht ausführbar. Sie bleibt in der Tasks-Datei als „Nicht ausführbar — deploymentabhängig" dokumentiert und muss nach dem Deployment des Fixes erfolgen.
- **Abweichende Umsetzung des In-Page-Suchformulars:** Der Plan ging von einem `@onsubmit`-Handler aus (`OnSubmitSearch`); implementiert wurde stattdessen ein natives GET-Formular ohne `action`-Attribut (postet auf die aktuelle URL, erhält PathBase). Das ist funktional gleichwertig und robuster — der zugehörige E2E-Test `InPageSearch_Submit_UpdatesResultList` ist grün. Das planseitig dokumentierte Bedienkonzept (Submit aktualisiert Trefferliste über `q`-Parameter → `OnParametersSetAsync`) bleibt erfüllt.
- **Der dokumentierte Teillauf in `test-results.md` (621/622)** stammt aus Iteration 1 und liegt vor den Nacharbeiten; der in diesem Review ausgeführte Release-Gesamtlauf (623/623) ist der maßgebliche aktuelle Nachweis.
