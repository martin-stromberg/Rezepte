# Umsetzungsplan: Reparatur der Rezeptsuche (Issue #171)

## Übersicht

Die Rezeptsuche (`/recipes/search`, `RecipeSearch.razor` → serverseitiger Self-Call via `ApiClient` → `RecipesController.SearchAsync` → `RecipeService.SearchAsync`) zeigt produktiv reproduzierbar „Keine Treffer." (`UiStrings:NoSearchResults`), obwohl die Suchkette lokal nachweislich funktioniert (Service-, API- und E2E-Verifikation in der Übersetzungsphase alle grün). Da der exakte Produktiv-Fehlpfad nicht lokal reproduzierbar ist, werden die drei identifizierten Defekt-Kandidaten behoben: die `OperationCanceledException`-Maskierung in `RunSearchAsync` (Timeout/Abbruch des internen API-Aufrufs erscheint als „Keine Treffer."), das fehlende `@rendermode InteractiveServer` auf `RecipeSearch.razor` (inerte Paginierung, `0`-Bindung der int-Query-Parameter) und die uneinheitlichen absoluten `/api/…`-URIs der Self-Calls (PathBase-/Reverse-Proxy-Risiko). Ergänzend werden die bisher ungetesteten Provider- und UI-Pfade durch SQLite-basierte Service-Tests, bUnit-Komponententests, Controller-Tests und einen dedizierten E2E-Regressionstest abgesichert.

## Designentscheidungen

| Komponente / Bereich | Gewählter Ansatz | Begründung |
|----------------------|-----------------|------------|
| Render-Modus `RecipeSearch.razor` | `@rendermode InteractiveServer` ergänzen (Muster: `Home.razor` Z. 3 und alle übrigen inhaltlichen Seiten) | Die Seite ist die einzige inhaltliche Seite ohne Render-Modus; dadurch sind `@onsubmit`-Suche und `@onclick`-Paginierung im ausgelieferten HTML inert. Alternative „statisch bleiben + `href`-Paginierung + `action`-Formular" würde ein zweites, abweichendes UI-Muster einführen und den dokumentierten Bedienfluss (Enhanced Navigation) verlieren. |
| Abbruch-/Timeout-Behandlung `RunSearchAsync` | `OperationCanceledException` in zwei Fälle unterscheiden: (a) Suche wurde durch eine neuere Suche ersetzt (andere `CancellationTokenSource` aktiv) → folgenlos; (b) Timeout/Abbruch des Requests bei noch aktiver Quelle → `searchFailed = true` + `SearchFailed`-Alert, Log-Level `LogWarning` | Timeout und echter Fehler sind heute von „keine Treffer" nicht unterscheidbar — genau das gemeldete Symptom. Die Unterscheidung ist nötig, weil `cts?.Cancel()` bei jeder neuen Suche die alte Quelle abbricht (und bei Prerender+Circuit-Start zwei Läufe existieren); eine ersetzte Suche darf keinen Fehlerzustand erzeugen. |
| Self-Call-URI-Form | Einheitlich **relative** URIs (`api/recipes/search`) in `RecipeSearch.razor` und `CalendarEventDialog.razor` | `ApiClient.Http.BaseAddress` ist `nav.BaseUri` und enthält einen evtl. gesetzten PathBase bereits; absolute `/api/…`-URIs verlieren den Subpfad und liefern hinter Reverse Proxy/PathBase 404 oder Timeout (ein plausibler Produktiv-Fehlpfad). `RecipeEdit.razor` (Z. 435) und `RecipeSelectDialog.razor` (Z. 99) nutzen bereits relative URIs — das ist die Repo-Konvention. |
| Fehlerbehandlung `CalendarEventDialog.TriggerRecipeSearchAsync` | Inneren Fallback-Request auf Deserialisierungsfehler (`JsonException`, `NotSupportedException`) beschränken; Abbruch/Timeout und andere Fehler führen zu neuem `recipeSearchFailed`-Flag → `alert-danger` mit vorhandenem `SearchFailed`-Schlüssel | Aktuell löst jede Exception (inkl. `OperationCanceledException`) einen zweiten, zwangsläufig ebenfalls scheiternden Request aus und maskiert den Fehler als leere Liste/`NoSearchResults`. Muster übernommen von `RecipeSelectDialog.razor` (`loadError` + `RecipesLoadFailed`-Alert, Z. 22–24). |
| Normalisierung `page`/`pageSize` in `RecipeSearch.razor` | Clientseitiges Clamping über Hilfsproperty `CurrentPage` (`pageNumber < 1 ? 1 : pageNumber`) und Erweiterung von `CurrentPageSize` auf `pageSize < 1`; beide in URL-Aufbau und Paginierungsanzeige verwenden | `[SupplyParameterFromQuery]` bindet fehlende int-Query-Werte mit `0` statt dem Property-Default — erzeugt „Seite 0 von 1" und sendet `page=0` an die API (wird serverseitig zwar geclamped, ist aber inkonsistent zur Anzeige). Muster existiert bereits: `CurrentPageSize` (Z. 95). |
| Case-Sensitivität der Suche | `RecipeService.SearchAsync` bleibt unverändert (`EF.Functions.Like`) | Die Methode findet exakt geschriebene Begriffe auf dem Produktiv-SQLite nachweislich korrekt (Scratch-Verifikation). Unicode-case-insensitive Umlaut-Suche ist kein Teil des gemeldeten Defekts → Offener Punkt. |

## Programmabläufe

### Suche über Navbar und Suchseite (Happy Path)

1. Anwender gibt einen Begriff in `#nav-search` (`MainLayout.razor`) ein und submittet.
2. Auf interaktiven Seiten: `MainLayout.OnSubmitSearch` → `Nav.NavigateTo("/recipes/search?q=…")`. Da das Ziel nach dem Fix ebenfalls interaktiv ist, läuft die Navigation als Blazor Enhanced Navigation (bisher: Full-Page-Reload wegen des statischen Ziels). Auf statischen Seiten (z. B. `Login.razor`): nativer Form-GET-Fallback über das korrigierte `action="/recipes/search"`.
3. `RecipeSearch.OnParametersSetAsync` synchronisiert `searchQuery = q` und ruft `RunSearchAsync`.
4. `RunSearchAsync` ersetzt die aktive `CancellationTokenSource` (10 s Timeout), setzt `isLoading`/`searchFailed`/Ergebniszustand zurück und ruft `Client.Http.GetFromJsonAsync<SearchResponseDto>("api/recipes/search?q=…&page=…&pageSize=…")` mit **relativer** URI und **geclampten** `CurrentPage`/`CurrentPageSize`-Werten auf.
5. `CircuitAuthHandler.SendAsync` hängt — im SSR-/Prerender-Kontext über `IHttpContextAccessor.HttpContext.User`, im Circuit über `AuthenticationStateProvider` — ein Bearer-JWT (`TokenService.GetToken`/`CreateToken`) an.
6. `RecipesController.SearchAsync` löst `userId` via `GetUserId()` auf (sonst `401`), clamped `page`/`pageSize` und ruft `RecipeService.SearchAsync`.
7. `RecipeService.SearchAsync` filtert `r.UserId == userId`, wendet bei nicht-leerem `q` das `EF.Functions.Like`-Prädikat auf `Title`, `Description`, `Steps.Description` und `Steps.Ingredients.Name` an, zählt, sortiert, paginiert und projiziert auf `SearchResultItem` (unverändert).
8. Die Seite rendert bei Treffern `.list-group-item`-Einträge (`Title`, `Snippet`, `PublishedLabel`), bei leerer Menge `NoSearchResults`, bei Fehler `SearchFailed`.
9. Paginierung: `ChangePage(n)` navigiert zu `/recipes/search?q=…&page=n&pageSize=…` → Schritt 3 erneut (jetzt funktionsfähig, da Circuit vorhanden).

### Fehler- und Abbruchpfad der Suche

1. `RunSearchAsync` fängt `OperationCanceledException`: Ist die fangende `CancellationTokenSource` noch die aktive (kein neuere Suche gestartet), handelt es sich um Timeout/Abbruch des Requests → `LogWarning`, `searchFailed = true` → `SearchFailed`-Alert. Wurde eine neuere Suche gestartet, bleibt der Abbruch folgenlos (Ergebnis der neuen Suche ist maßgeblich, kein Fehler-Flash).
2. `catch (Exception)` bleibt bestehen: `LogError`, `searchFailed = true`, leere Ergebnismenge → `SearchFailed`-Alert.
3. `CalendarEventDialog.TriggerRecipeSearchAsync`: `GetFromJsonAsync<RecipeSearchResponseDto>` wirft bei Antwort-Shape-Mismatch `JsonException`/`NotSupportedException` → nur dann erfolgt der bestehende Fallback auf `List<RecipeSearchResultDto>`. Alle anderen Fehler (inkl. Abbruch) setzen `recipeSearchFailed = true` → `alert-danger` mit `SearchFailed` statt `NoSearchResults`.

Beteiligte Klassen/Komponenten: `RecipeSearch`, `MainLayout`, `ApiClient`, `CircuitAuthHandler`, `TokenService`, `RecipesController`, `RecipeService`, `CalendarEventDialog`

## Neue Klassen

Keine neuen Produktivklassen erforderlich (Bugfix ohne neuen Erweiterungspunkt; `IRecipeService`, `ApiClient`, `NavigationManager`, `IStringLocalizer<UiStrings>` bleiben die Abhängigkeiten). Neu anzulegende Testklassen und -helfer:

| Klasse | Typ | Zweck |
|--------|-----|-------|
| `RecipeServiceSearchSqliteTests` | Testklasse (`Rezepte.Tests/Services/`) | `SearchAsync`-Tests auf dem Produktiv-SQLite-Provider (Provider-Verhalten von `EF.Functions.Like`, Projektion, Paging) |
| `CreateSqliteDb` (Hilfsmethode in `RecipeServiceSearchSqliteTests`) | Test-Hilfsmethode | `SqliteConnection("DataSource=:memory:")` offen halten + `UseSqlite` + `EnsureCreated` (Muster: `ExportServiceExportTests.cs` Z. 24–28) |
| `RecipeSearchComponentTests` | bUnit-Testklasse (`Rezepte.Tests/Components/`) | `RecipeSearch.razor`: Treffer-/Leer-/Fehlerzustände, inkl. maskiertem Abbruchpfad |
| `StubHttpMessageHandler` | Test-Hilfsklasse (`Rezepte.Tests/Components/` oder `TestHelpers/`) | `HttpMessageHandler`, der konfigurierbar JSON-Antworten oder Exceptions (u. a. `OperationCanceledException`) für den `ApiClient`-`HttpClient` liefert |
| `RecipesControllerSearchTests` | Testklasse (`Rezepte.Tests/Controllers/`) | `RecipesController.SearchAsync`: Clamping, `Unauthorized`, DTO-Mapping (Moq auf `IRecipeService`) |
| `RecipeSearchBrowserTests` | E2E-Testklasse (`Rezepte.Tests.Browser/`, Collection `BrowserTestCollection`) | Dedizierter Playwright-Regressionstest der Suche |
| `RecipeSeedHelper` | E2E-Hilfsklasse (`Rezepte.Tests.Browser/Infrastructure/`) | Legt Rezepte für den `browsertest`-Benutzer über die echte API an: `page.Context.APIRequest` → `GET api/session/token` (Cookie-Session) → `POST api/recipes` mit Bearer-JWT (`CreateRecipeRequest`) |

## Änderungen an bestehenden Klassen

### `RecipeSearch` (Seite, `Rezepte.Web/Components/Pages/RecipeSearch.razor`)

- **Neue Direktive:** `@rendermode InteractiveServer` — macht die Seite interaktiv (Muster aller übrigen inhaltlichen Seiten); behebt inerte Paginierung (`ChangePage`) und inertes `@onsubmit`.
- **Neue Eigenschaft:** `CurrentPage` (`int`) — liefert `pageNumber < 1 ? 1 : pageNumber`; wird in der Request-URI (Z. 127), in `ChangePage` und in der Anzeige „Seite @CurrentPage von @TotalPages" verwendet.
- **Geänderte Eigenschaft:** `CurrentPageSize` — clamping auf `pageSize < 1` (bisher nur `== 0`), Default 20 bleibt.
- **Geänderte Methode:** `RunSearchAsync` (Z. 113–152) —
  - Request-URI von absolut `/api/recipes/search` auf relativ `api/recipes/search` umstellen (PathBase-Robustheit, Repo-Konvention).
  - `catch (OperationCanceledException)`: Unterscheidung „durch neuere Suche ersetzt" (folgenlos, kein UI-Fehler) vs. „Timeout/Abbruch bei aktiver Quelle" (`searchFailed = true`, `LogWarning` statt `LogInformation`). Ergebnis: Timeout/Fehlschlag des Self-Calls wird als `SearchFailed` angezeigt statt als `NoSearchResults`.
- **Geändertes Markup:** Paginierungsanzeige (Z. 66) nutzt `CurrentPage` statt `pageNumber` (kein „Seite 0").

### `MainLayout` (Layout, `Rezepte.Web/Components/Layout/MainLayout.razor`)

- **Geändertes Markup:** Suchformular (Z. 39) — `action="recipes/search"` → `action="/recipes/search"` (und `method="get"` ergänzen): Der native HTML-Fallback relativ zur aktuellen URL ist auf Unterseiten falsch (`/cookbooks` → `/cookbooks/recipes/search?q=…`).

### `CalendarEventDialog` (Shared-Komponente, `Rezepte.Web/Components/Shared/CalendarEventDialog.razor`)

- **Neue Eigenschaft:** `recipeSearchFailed` (`bool`) — Fehlerzustand des Rezept-Pickers.
- **Geänderte Methode:** `TriggerRecipeSearchAsync` (Z. 481–530) —
  - Request-URI von absolut `/api/recipes/search` auf relativ `api/recipes/search` umstellen.
  - Innerer `catch (Exception)`: Fallback auf `List<RecipeSearchResultDto>` nur bei `JsonException`/`NotSupportedException` (Shape-Mismatch); alle anderen Fehler inklusive Abbruch/Timeout → `recipeSearchFailed = true`, kein zweiter Request, `LogError`/`LogWarning` bleibt.
- **Geändertes Markup:** Picker-Ergebnisbereich (Z. 237–240) — bei `recipeSearchFailed` `alert-danger` mit `@Localizer["SearchFailed"]` statt `NoSearchResults` (Muster: `RecipeSelectDialog.razor` `loadError`/`RecipesLoadFailed`).

### `RecipeService`, `RecipesController`, `ApiClient`, `CircuitAuthHandler`, `TokenService`

Keine Änderungen — `RecipeService.SearchAsync` ist auf dem Produktiv-SQLite verifiziert funktional; der Controller clamped bereits korrekt; die Self-Call-Pipeline bleibt unverändert.

## Datenbankmigrationen

Keine.

## Validierungsregeln

| Feld / Objekt | Regel | Fehlerfall |
|---------------|-------|------------|
| `pageNumber` / `pageSize` (Query-Parameter `RecipeSearch.razor`) | Normalisierung auf `>= 1` clientseitig (`CurrentPage`, `CurrentPageSize`); Server-Clamping im Controller bleibt bestehen | Fehlende oder `0`/negative Query-Werte erzeugten bisher `page=0`-Requests und „Seite 0 von 1" |

## Konfigurationsänderungen

Keine.

## Seiteneffekte und Risiken

- **Loading-Bar / Navigationsmechanismus:** Mit `@rendermode InteractiveServer` führt die Navbar-Suche von interaktiven Seiten eine Blazor Enhanced Navigation aus statt eines Full-Page-Reloads (bisher erzwungen durch das statische Ziel). `loadingBar.js` (Z. 96–111) lauscht auf beide Trigger (`enhancednavigationstart` und `beforeunload`) — die Anzeige bleibt funktional, aber `LoadingBarFormNavigationBrowserTests.SearchSubmit_ShowsLoadingBar` durchläuft einen anderen Mechanismus (siehe Tests).
- **Doppelte Suche bei Prerender+Circuit:** `InteractiveServer` prerendert statisch und startet danach den Circuit; `OnParametersSetAsync`/`RunSearchAsync` laufen dadurch zweimal (identisch zu allen übrigen interaktiven Seiten, deren Datenabrufe ebenfalls doppelt laufen). Der zweite Lauf nutzt den `AuthenticationStateProvider`-Pfad von `CircuitAuthHandler`; die Supersede-Unterscheidung verhindert Fehlalarme.
- **`RegisterTests` (E2E):** `CountRecipesAsync` nutzt `/recipes/search?q=&page=1&pageSize=100` als Datenquelle; Settle-Selektor `.list-group-item, .alert` bleibt gültig, die Seite liefert Treffer jetzt bereits im Prerender.
- **`CalendarEventDialog`:** Fehler im Rezept-Picker zeigen künftig `SearchFailed` statt einer leeren Trefferliste — bewusste, sichtbare Verhaltensänderung (Nebenbefund-Fix).
- **`RecipeEdit.SearchSideDishesAsync`:** nutzt bereits relative URIs und propagiert Exceptions (kein Maskierungsproblem) — bleibt unverändert.
- **Rate-Limiting/Auth:** keine Auswirkung; Bearer-JWT-Pfad unverändert.

## Umsetzungsreihenfolge

1. **`RecipeSearch.razor`: Render-Modus, URI-Form, Abbruchbehandlung, Clamping**
   - Voraussetzungen: Keine — `@rendermode`-Infrastruktur ist vorhanden (`AddInteractiveServerRenderMode` in `Program.cs:103-104`), der `SearchFailed`-Schlüssel existiert in `UiStrings.resx` (Z. 81).
   - Beschreibung: `@rendermode InteractiveServer` ergänzen; URI relativ stellen; `CurrentPage`-Property und `CurrentPageSize`-Clamping einführen und in URI/Anzeige nutzen; `catch (OperationCanceledException)` in Supersede- vs. Timeout-Fall aufspalten (Timeout → `searchFailed = true`, `LogWarning`).

2. **`MainLayout.razor`: Form-`action` korrigieren**
   - Voraussetzungen: Keine.
   - Beschreibung: `action="recipes/search"` → `action="/recipes/search"`, `method="get"` ergänzen.

3. **`CalendarEventDialog.razor`: URI-Form und Fehlerbehandlung**
   - Voraussetzungen: Keine — `SearchFailed`-Schlüssel vorhanden; Fehler-Muster aus `RecipeSelectDialog.razor` referenzierbar.
   - Beschreibung: URI relativ stellen; `recipeSearchFailed`-Flag und `alert-danger`-Markup ergänzen; Fallback-Request auf `JsonException`/`NotSupportedException` beschränken.

4. **SQLite-basierte `SearchAsync`-Tests (`RecipeServiceSearchSqliteTests`)**
   - Voraussetzungen: `Microsoft.EntityFrameworkCore.Sqlite`/`Microsoft.Data.Sqlite` ist über die `Rezepte.Web`-Projektreferenz transitiv im Testprojekt verfügbar (Nachweis: `ExportServiceExportTests`, `BackgroundJobPersistenceTests` nutzen `UseSqlite` bereits); kein neues Paket nötig. `Moq`/`FluentAssertions` vorhanden.
   - Beschreibung: Testklasse mit `CreateSqliteDb`-Helfer (`SqliteConnection("DataSource=:memory:")`, `EnsureCreated`) anlegen; `SearchAsync`-Fälle aus `RecipeServiceTests` provider-realistisch nachbilden (Titel-, Schritt-, Zutaten-Treffer, User-Filter, ASCII-Case-Insensitivität, Paging).

5. **bUnit-Komponententests (`RecipeSearchComponentTests`)**
   - Voraussetzungen: `bunit`-Paket vorhanden; `AddLocalization()`-Muster aus `SecurityTxtSettingsTests_ErrorHandling.cs` (Z. 25); `FakeNavigationManager` durch bUnit registriert; `ApiClient` ist mit `HttpClient`+`NavigationManager` instanziierbar (Z. 27–30 `ApiClient.cs`); `StubHttpMessageHandler` wird in diesem Schritt erstellt.
   - Beschreibung: Testklasse anlegen; `ApiClient` mit `new HttpClient(new StubHttpMessageHandler(...))` injizieren; Zustände Trefferliste / `NoSearchResults` / `SearchFailed` bei `OperationCanceledException` und bei generischer Exception prüfen.

6. **Controller-Tests (`RecipesControllerSearchTests`)**
   - Voraussetzungen: `Moq` vorhanden; `IRecipeService` ist mockbar; `RecipesController`-Ctor benötigt `IRecipeService` + `IOptions<ImageOptions>`.
   - Beschreibung: `Mock<IRecipeService>`; `ControllerContext` mit `ClaimsPrincipal` (`NameIdentifier`); Clamping (`page<1`→1, `pageSize>MaxPageSize`→`MaxPageSize`), `Unauthorized` ohne Claim, `RecipeSearchResponseDto`-Mapping (`Total`, `PublishedLabel`-Format `yyyy-MM-dd`).

7. **E2E-Regressionstest (`RecipeSearchBrowserTests` + `RecipeSeedHelper`)**
   - Voraussetzungen: bestehende Browser-Infrastruktur (`BrowserTestCollection`, `RezepteAppFixture`, `PlaywrightBrowserFixture`, `LoadingBarBrowserSession`, `LoadingBarPageObject`); publizierte `Rezepte.Web.dll` (`dotnet publish Rezepte.Web -c Release`); `GET api/session/token` (`SessionTokenController`) und `POST api/recipes` (`RecipesController.Create`, `CreateRecipeRequest`) vorhanden.
   - Beschreibung: `RecipeSeedHelper` legt Rezepte per `page.Context.APIRequest` an (Session-Token → Bearer → `POST api/recipes`); Testfälle: Navbar-Suche → Treffer, Direktaufruf mit `q` → Treffer, leere Treffermenge → `NoSearchResults`, Paginierung über „»" → Seite 2 (Seeding > `pageSize` Rezepte), In-Page-`#search-q`-Submit → Treffer.

8. **Betroffenen Browser-Test `SearchSubmit_ShowsLoadingBar` verifizieren**
   - Voraussetzungen: Schritt 1 umgesetzt; Schritt 7 ausführbar.
   - Beschreibung: Test laufen lassen; dokumentierten Mechanismus (Full-Page-Reload) gegen den neuen (Enhanced Navigation) prüfen und Inline-Kommentar/Testannahmen ggf. aktualisieren — die Assertion (Bar aktiv) muss über `enhancednavigationstart` weiterhin greifen.

9. **Vollständiger Testlauf und Format-/Lokalisierungsprüfung**
   - Voraussetzungen: Schritte 1–8; Publish für Browser-Tests.
   - Beschreibung: `dotnet test Rezepte.sln --configuration Release`; neue UI-Texte bleiben auf vorhandene `UiStrings`-Schlüssel beschränkt (kein resx-Neuwert, `razor-l10n-check` unberührt); XML-Doku aller neuen öffentlichen Testmember (`CS1591` ist Fehler).

10. **Produktiv-Verifikation des Fehlpfads**
    - Voraussetzungen: Deployment des Fixes.
    - Beschreibung: Suche im Produktivsystem erneut ausführen; anhand der nun sichtbaren Unterscheidung (`SearchFailed` statt `NoSearchResults`) und der Log-Einträge („was cancelled or timed out" als `LogWarning` vs. `LogError`) den tatsächlichen Fehlpfad bestätigen (voraussichtlich Self-Call hinter Reverse Proxy).

## Tests

### Neue Tests

| Test / Hilfsmethode | Testklasse | Was wird geprüft / bereitgestellt? |
|--------------------|------------|-------------------------------------|
| `CreateSqliteDb` | `RecipeServiceSearchSqliteTests` | `SqliteConnection("DataSource=:memory:")` + `UseSqlite` + `EnsureCreated`; Connection bleibt für die Testdauer offen |
| `SearchAsync_ShouldFindByTitle_OnSqlite` | `RecipeServiceSearchSqliteTests` | Exakter Teilstring im `Title` findet das Rezept auf dem Produktivprovider |
| `SearchAsync_ShouldBeCaseInsensitiveAscii_OnSqlite` | `RecipeServiceSearchSqliteTests` | `honig` findet `Honig` (ASCII-Case-Insensitivität von SQLite-`LIKE`) |
| `SearchAsync_ShouldFindByStepDescription_OnSqlite` | `RecipeServiceSearchSqliteTests` | Treffer über `RecipeStep.Description` |
| `SearchAsync_ShouldFindByIngredientName_OnSqlite` | `RecipeServiceSearchSqliteTests` | Treffer über `RecipeIngredient.Name` |
| `SearchAsync_ShouldNotReturnForeignUserRecipes_OnSqlite` | `RecipeServiceSearchSqliteTests` | `UserId`-Filter greift auf SQLite |
| `SearchAsync_ShouldPageAndCount_OnSqlite` | `RecipeServiceSearchSqliteTests` | `TotalCount` vor Paging und `Skip/Take`-Ergebnismenge korrekt |
| `StubHttpMessageHandler` | `RecipeSearchComponentTests` (Hilfsklasse) | Liefert konfigurierbar JSON-Antwort oder Exception (`OperationCanceledException`, `HttpRequestException`) |
| `RunSearchAsync_WhenCancelled_ShowsSearchFailed` | `RecipeSearchComponentTests` | Handler wirft `OperationCanceledException` bei aktiver Quelle → `alert-danger` (`SearchFailed`), nicht `alert-secondary` (`NoSearchResults`) |
| `RunSearchAsync_WhenEmpty_ShowsNoSearchResults` | `RecipeSearchComponentTests` | Leere `Items` → `alert-secondary` mit `NoSearchResults` |
| `RunSearchAsync_WhenResults_RendersListItems` | `RecipeSearchComponentTests` | `.list-group-item` mit Titel wird gerendert; Request-URI ist relativ |
| `SearchAsync_ShouldClampPageAndPageSize` | `RecipesControllerSearchTests` | `page=0`/`pageSize=0`/`pageSize>100` → an `IRecipeService` weitergereichte Werte `1`/`1`/`100` |
| `SearchAsync_WithoutUserClaim_ReturnsUnauthorized` | `RecipesControllerSearchTests` | `401` ohne `NameIdentifier`-Claim; Service wird nicht aufgerufen |
| `SearchAsync_MapsToResponseDto` | `RecipesControllerSearchTests` | `Total`, `PublishedLabel` (`yyyy-MM-dd`), Snippet-Kappung ≤ 200 + „…" |

### Betroffene bestehende Tests

| Test / Testklasse | Grund der Anpassung |
|-------------------|---------------------|
| `LoadingBarFormNavigationBrowserTests.SearchSubmit_ShowsLoadingBar` | Navigationsmechanismus zur Suchseite ändert sich (Enhanced Navigation statt Full-Page-Reload); Assertion muss weiterhin grün sein, Inline-Kommentare/Testannahmen (Z. 40–48) an den neuen Mechanismus anzupassen |
| `RegisterTests` (`CountRecipesAsync`, Z. 189–197) | Indirekter Nutzer der Seite; kein Code-Zwang zur Änderung, aber im Voll-Lauf verifizieren (Prerender ändert Ladezeitpunkt der Ergebnisse) |
| `RecipeServiceTests.SearchAsync_*` (Z. 215, 235, 255) | Keine Anpassung nötig — Service unverändert; Tests bleiben auf InMemory grün und werden durch die neuen SQLite-Tests ergänzt |

### E2E-Tests (primärer Funktionsnachweis)

| Priorität | Szenario | Testdatei / Testklasse | Abgedecktes Akzeptanzkriterium | Warum E2E nötig ist |
|-----------|----------|------------------------|-------------------------------|-------------------|
| Pflicht | Login → Navbar-Suche nach Begriff im Rezepttitel → Trefferliste zeigt das Rezept | `Rezepte.Tests.Browser/RecipeSearchBrowserTests.cs` | FR-023 Happy Path: Suche findet eigenes Rezept | Exakt der gemeldete Benutzerfluss; Unit-/Integrationstests decken SSR, Self-Call mit JWT und Rendering nicht end-to-end ab |
| Pflicht | Direktaufruf `GET /recipes/search?q=…` (Reload-/Link-Pfad) zeigt Treffer | `RecipeSearchBrowserTests` | Suche funktioniert ohne Circuit-Interaktion beim ersten Render | Prerender-/SSR-Pfad ist ein separater Ausführungspfad von `RunSearchAsync` |
| Pflicht | Suche nach nicht vorhandenem Begriff → „Keine Treffer." (`alert-secondary`), kein `SearchFailed` | `RecipeSearchBrowserTests` | Leere Treffermenge bleibt von Fehlerzustand unterscheidbar | Sichtbare Zustandsunterscheidung ist Kern des Fixes |
| Pflicht | > `pageSize` Treffer → „»"-Button → Seite 2 zeigt Resttreffer und „Seite 2 von …" | `RecipeSearchBrowserTests` | Paginierung funktioniert (vorher ohne Circuit inert) | Geänderte Benutzerinteraktion (`@rendermode`-Fix); nur im Browser nachweisbar |
| Soll | Suche über In-Page-Formular `#search-q` auf `/recipes/search` aktualisiert die Trefferliste | `RecipeSearchBrowserTests` | `@onsubmit`-Pfad der Seite funktioniert im Circuit | Geänderte Interaktion (vorher kein funktionierender Submit-Handler) |

Fehlerfall-Abdeckung (`SearchFailed` bei Timeout/Abbruch des Self-Calls): **kein E2E-Szenario geplant** — der serverseitige HTTP-Self-Call (`ApiClient` → eigene API) ist im Browser nicht manipulierbar; Playwright-Routen greifen nur auf Browser-Requests. Der Pfad wird stattdessen durch `RecipeSearchComponentTests.RunSearchAsync_WhenCancelled_ShowsSearchFailed` (bUnit, injizierter Handler wirft `OperationCanceledException`) und manueller Produktiv-Verifikation (Schritt 10) nachgewiesen. Der `CalendarEventDialog`-Picker-Happy-Path ist implizit durch die unverändert funktionierende Kalenderseite abgedeckt; ein dedizierter Picker-E2E ist nicht erforderlich, da der Dialog an dieser Stelle nur Nebenbefund ist und die Fix-Pfade (relative URI, Fehler-Flag) denselben Mechanismus wie die Hauptseite nutzen.

Welche bestehenden E2E-Tests müssen angepasst werden?

| Test / Testklasse | Grund der Anpassung |
|-------------------|---------------------|
| `LoadingBarFormNavigationBrowserTests.SearchSubmit_ShowsLoadingBar` | Mechanismus-Wechsel (Enhanced Navigation statt Full-Page-Reload) — Kommentare/Erwartungsdokumentation aktualisieren, Assertion muss weiterhin grün sein |

## Offene Punkte

| # | Offener Punkt | Empfohlener Vorschlag |
|---|---------------|----------------------|
| 1 | Soll die Suche case-insensitiv auch für deutsche Umlaute funktionieren (z. B. „äpfel" findet „Äpfel")? SQLite-`LIKE` und `lower()` sind auf ASCII beschränkt; echte Unicode-Case-Insensitivität erfordert ICU-Collation oder clientseitige Auswertung. | Dokumentierten Ist-Umfang belassen (`EF.Functions.Like`, exakte Schreibweise trifft immer zu); der gemeldete Defekt betrifft exakt geschriebene Begriffe. Umlaut-Robustheit ggf. als separates Issue nachziehen — kein Bestandteil dieses Bugfixes. |
| 2 | Welche Meldung erscheint produktiv exakt (`NoSearchResults` vs. `SearchFailed`), was steht im Server-Log, und läuft die App hinter Reverse Proxy/TLS-Terminierung/PathBase? | Nicht implementierungsblockierend — die geplanten Fixes decken alle drei Kandidaten ab (maskierter Abbruch, Render-Modus, absolute URIs). Nach Deployment mit Schritt 10 anhand der nun sichtbaren `SearchFailed`-Meldung und der Log-Zeilen den tatsächlichen Pfad bestätigen; vermutete Hauptursache ist der Self-Call-Timeout hinter Reverse Proxy (bislang maskiert als „Keine Treffer."). |
