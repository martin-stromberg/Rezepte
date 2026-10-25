# Komponenten (UI) — Rezeptsuche

## Render-Modus der routbaren Seiten

`RecipeSearch.razor` ist die einzige inhaltliche Seite ohne `@rendermode InteractiveServer`. Übersicht (`@page`-Direktive ↔ Render-Modus):

| Datei | Route | `@rendermode` |
|-------|-------|---------------|
| `RecipeSearch.razor` | `/recipes/search` | **fehlt** (statisches SSR) |
| `Home.razor` | `/` | `InteractiveServer` (Z. 3) |
| `RecipePage.razor` | `/recipes/{Id}` | `InteractiveServer` (Z. 10) |
| `RecipeEdit.razor` | `/recipes/{Id}/edit`, `/recipes/new` | `InteractiveServer` (Z. 5) |
| `Cookbooks.razor` | `/cookbooks` | `InteractiveServer` (Z. 3) |
| `CookbookPage.razor` | `/cookbooks/{Id}` | `InteractiveServer` (Z. 4) |
| `CookbookDetails.razor` | `/cookbooks/{Id}/edit` | `InteractiveServer` (Z. 3) |
| `Calendar.razor` | `/calendar` | `InteractiveServer` (Z. 6) |
| `Settings.razor` | `/settings` | `InteractiveServer` (Z. 6) |
| `ShoppingList.razor` | `/shopping-list` | `InteractiveServer` (Z. 10) |
| `Login.razor` | `/login` | fehlt (bewusst statisch) |
| `Register.razor` | `/register` | fehlt (bewusst statisch) |
| `Error.razor` | `/Error` | fehlt (bewusst statisch) |

`ScheduledRecipes.razor` hat keine `@page`-Direktive (eingebettete Komponente). `App.razor` rendert `Routes` ohne globalen Render-Modus; `Program.cs:103-104` aktiviert `AddInteractiveServerRenderMode()`.

## `RecipeSearch` (Seite)

Datei: `Rezepte.Web/Components/Pages/RecipeSearch.razor` — `@page "/recipes/search"`, kein `@rendermode`, kein `@attribute [Authorize]` (Zugriffsschutz über `AuthorizeRouteView` → `RedirectToLogin`).

| Member | Sichtbarkeit | Kurzbeschreibung |
|--------|-------------|------------------|
| `q`, `pageNumber`, `pageSize` | `public` `[Parameter]` `[SupplyParameterFromQuery]` | Query-Parameter `q` (string), `page` (int, Default 1), `pageSize` (int, Default 10). Bei statischem SSR bindet fehlendes `page`/`pageSize` zu `0`, nicht zum Property-Default → `CurrentPageSize` fängt `0` ab (Zeile 95), `pageNumber` bleibt `0` („Seite 0 von 1") |
| `OnParametersSetAsync()` | `protected override` | synchronisiert `searchQuery = q` und ruft `RunSearchAsync` (Zeilen 98–103) |
| `OnSubmitSearch()` | `private` | `@onsubmit`-Handler des Suchformulars; `Nav.NavigateTo("/recipes/search?q=…&page=1&pageSize=…", forceLoad: false)` — ohne Circuit wird der Handler nie ausgelöst; HTML-Fallback ist `action`-los, Submit lädt die Seite ohne `q` neu |
| `RunSearchAsync()` — Zeilen 113–152 | `private` | `CancellationTokenSource` mit 10 s Timeout (vorherige Suche wird gecancelt); GET `"/api/recipes/search?q=…&page=…&pageSize=…"` (**absolute URI**) via `Client.Http.GetFromJsonAsync<SearchResponseDto>`; `catch (OperationCanceledException)` (Zeilen 136–139) loggt nur `LogInformation` → `results` bleibt leer → UI zeigt `NoSearchResults`; `catch (Exception)` setzt `searchFailed = true` → `SearchFailed`-Alert |
| `ChangePage(int)` — Zeilen 154–159 | `private` | `@onclick`-Paginierung via `Nav.NavigateTo(...)` — ohne Circuit inert (kein `href` auf den Buttons) |
| `RecipeResult`, `SearchResponseDto` | `private sealed record` | lokale DTOs, siehe [models.md](models.md) |

UI-Zustände (Zeilen 18–73): `isLoading` → Spinner `SearchInProgress`; `searchFailed` → `SearchFailed`-Alert; `results` leer → `NoSearchResults`-Alert; sonst Trefferliste mit `<a href="/recipes/{Id}">`, Snippet, `PublishedLabel`, Tags und Paginierung („Seite @pageNumber von @TotalPages").

Injizierte Dienste: `ApiClient Client`, `NavigationManager Nav`, `ILogger<RecipeSearch>`, `IStringLocalizer<UiStrings>`.

## `MainLayout` (Navbar-Suchformular)

Datei: `Rezepte.Web/Components/Layout/MainLayout.razor` — wird von **allen** Seiten (auch statischen) verwendet.

| Member | Sichtbarkeit | Kurzbeschreibung |
|--------|-------------|------------------|
| `OnSubmitSearch()` — Zeilen 111–124 | `private` | `@onsubmit`-Handler: leeres Feld → `Nav.NavigateTo("/recipes")`, sonst `Nav.NavigateTo("/recipes/search?q={escaped}")`. Funktioniert nur mit Circuit |
| `searchQuery` | `private` | `@bind-Value` des `InputText` `#nav-search` (Zeile 40) |
| `<form … action="recipes/search">` — Zeile 39 | Markup | HTML-Fallback: Browser-Submit navigiert zu `recipes/search` relativ zur aktuellen Seite und hängt `?q=…` an — auf `/` ergibt das `/recipes/search?q=…`, auf Unterseiten einen falschen relativen Pfad |

## `CalendarEventDialog` (Rezept-Picker)

Datei: `Rezepte.Web/Components/Shared/CalendarEventDialog.razor` — Shared-Komponente, läuft auf der interaktiven Seite `Calendar.razor`.

| Member | Sichtbarkeit | Kurzbeschreibung |
|--------|-------------|------------------|
| `TriggerRecipeSearchAsync()` — Zeilen 481–530 | `private` | GET `"/api/recipes/search?q=…&page=1&pageSize=12"` (**absolute URI**); bei Deserialisierungsfehler Fallback auf `List<RecipeSearchResultDto>`; jeder `Exception`-Pfad (inkl. Abbruch) → `LogError`/`LogDebug` + leere Liste → `NoSearchResults` |
| `OnRecipeSearchInput(ChangeEventArgs)` — Zeilen 457–479 | `private` | Debouncing 350 ms via eigener `CancellationTokenSource`, `Task.Run` + `InvokeAsync(StateHasChanged)` |
| `SelectRecipe`, `CloseRecipePicker`, `LoadSideDishSuggestionsAsync` | `private` | Picker-Selektion; `LoadSideDishSuggestionsAsync` nutzt `GET /api/recipes/{id}/side-dishes` |

## `RecipeEdit` (Beilagensuche)

Datei: `Rezepte.Web/Components/Pages/RecipeEdit.razor` — `@rendermode InteractiveServer` vorhanden.

| Member | Sichtbarkeit | Kurzbeschreibung |
|--------|-------------|------------------|
| `SearchSideDishesAsync()` — Zeilen 414–445 | `private` | GET `"api/recipes/search?q=…&page=1&pageSize=10"` (**relative URI**); bei Rezept-Id/URL-Eingabe Direktaufruf `LoadRecipeSearchResultByIdAsync`; keine Fehlerbehandlung außer `finally` (Exception propagiert) |
| `LoadRecipeSearchResultByIdAsync` — Zeilen 447–460 | `private` | GET `"api/recipes/{id}"`; `HttpRequestException` → `null` |
| `ExtractRecipeId`, `CanAddSideDish`, `AddSideDish` | `private` | Beilagen-Hilfslogik |

## `RecipeSelectDialog` (Kochbuch-Rezeptauswahl)

Datei: `Rezepte.Web/Components/Shared/RecipeSelectDialog.razor` — nutzt nicht den Such-Endpunkt, sondern `GET api/recipes/available-for/{cookbookId}` (**relative URI**, Zeile 99); `search`-Parameter wird serverseitig mit `Title.Contains` gefiltert (siehe `RecipesController.GetAvailableFor`).

| Member | Sichtbarkeit | Kurzbeschreibung |
|--------|-------------|------------------|
| `LoadAsync(bool reset)` — Zeilen 86–118 | `private` | Paging-Load mit `loadError`-Flag und `RecipesLoadFailed`-Alert |
| `OnSearchKeyDown` — Zeilen 130–136 | `private` | Enter → `LoadAsync(reset: true)` |
| `LoadMore`, `Confirm`, `OnInitializedAsync` | `private`/`protected` | Standard-Paging/Dialog-Logik |

## Lokalisierung (relevante `UiStrings`-Schlüssel)

Datei: `Rezepte.Web/Resources/UiStrings.resx`

| Schlüssel | Wert (de) | Verwendet in |
|-----------|-----------|--------------|
| `NoSearchResults` | „Keine Treffer." | `RecipeSearch.razor:31`, `CalendarEventDialog.razor:239` |
| `SearchFailed` | „Die Suche ist fehlgeschlagen. Bitte versuche es erneut." | `RecipeSearch.razor:27` |
| `SearchInProgress` | „Suche läuft…" | `RecipeSearch.razor:22` |
| `RecipeSearchPageTitle` | „Rezepte – Suche" | `RecipeSearch.razor:8` |
| `SearchRecipes`, `SearchRecipesPlaceholder`, `StartSearch` | „Suche Rezepte" u. a. | `MainLayout.razor:40-41`, `RecipeSearch.razor:12` |
