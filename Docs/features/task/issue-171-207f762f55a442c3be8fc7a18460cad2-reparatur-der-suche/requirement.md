# Übersetzte Anforderung — GitHub Issue #171: Reparatur der Suche

## Original-Anforderung

> Die Rezeptsuche funktioniert nicht. Auch wenn die Suchbegriffe exakt in den Namen der Rezepte vorkommen, wird immer nur angezeigt, dass es keine Ergebnisse für die Suche gibt.

## Fachliche Zusammenfassung

Die Rezeptsuche (Seite `/recipes/search`, Blazor-Komponente `RecipeSearch`, dahinter der API-Endpunkt `GET /api/recipes/search` und `RecipeService.SearchAsync`) liefert dem Anwender reproduzierbar die Meldung „Keine Treffer." (`UiStrings:NoSearchResults`), selbst wenn der Suchbegriff exakt im `Recipe.Title` vorhanden ist. Erwartetes Verhalten: Bei nicht-leerem `q` werden die Rezepte des angemeldeten Benutzers (`Recipe.UserId == userId`) per Teilstring-Vergleich über `Recipe.Title`, `Recipe.Description`, `RecipeStep.Description` und `RecipeIngredient.Name` durchsucht und die Trefferliste mit Paging angezeigt; dieser dokumentierte Funktionsumfang (FR-023, `Docs/help/recipe-search.md`) ist wiederherzustellen.

## Betroffene Klassen und Komponenten

- `Rezepte.Web/Components/Pages/RecipeSearch.razor` — Suchseite; einzige routbare Seite **ohne** `@rendermode InteractiveServer` (alle übrigen Seiten wie `Home.razor`, `RecipePage.razor`, `RecipeEdit.razor`, `Cookbooks.razor`, `Calendar.razor` haben es). Rendert daher statisch (SSR): Die `@onclick`-Paginierungsbuttons sind ohne Circuit funktionslos, und die int-Parameter `pageNumber`/`pageSize` werden bei fehlenden Query-Werten mit `0` gebunden („Seite 0 von 1"). In `RunSearchAsync` wird `OperationCanceledException` (eigene `CancellationTokenSource` mit 10 s Timeout) nur geloggt und führt zu einer leeren Treffermenge — ein Timeout/Abbruch des internen API-Aufrufs ist damit in der UI von „keine Treffer" nicht unterscheidbar und erzeugt exakt das gemeldete Symptom.
- `Rezepte.Web/Components/Layout/MainLayout.razor` — Navbar-Suchformular (`OnSubmitSearch` → `Nav.NavigateTo("/recipes/search?q=…")`, `InputText` `#nav-search`).
- `Rezepte.Web/Controllers/RecipesController.cs` — `SearchAsync` (`[HttpGet("search")]`, `[Authorize]` mit `JwtBearerDefaults.AuthenticationScheme`), Parameter `q`, `tags`, `cookbookId`, `page`, `pageSize`, `sort`; DTOs `RecipeSearchItemDto`, `RecipeSearchResponseDto`; Userauflösung über `ApiControllerBase.GetUserId()` (`ClaimTypes.NameIdentifier`).
- `Rezepte.Web/Services/RecipeService.cs` — `SearchAsync(string userId, string? q, string? tags, string? cookbookId, int page, int pageSize, string sort, CancellationToken ct)`: `EF.Functions.Like`-Prädikat (Zeilen 892–897), `Include`-Kette auf `Images`, `Steps.Ingredients`, `RecipeCookbooks`, `Snippet`-Projektion mit `string.Join`, Ergebnistypen `SearchResult`/`SearchResultItem`.
- `Rezepte.Web/Services/ApiClient.cs`, `Rezepte.Web/Services/CircuitAuthHandler.cs`, `Rezepte.Web/Services/TokenService.cs`, `Rezepte.Web/Security/JwtSigningKeyProvider.cs` — serverseitiger HTTP-Self-Call an `nav.BaseUri` mit Bearer-JWT; relevant für Umgebungen hinter Reverse Proxy / IIS / PathBase.
- `Rezepte.Web/Data/RezepteDbContext.cs` mit den Entitäten `Recipe`, `RecipeStep`, `RecipeIngredient`, `RecipeCookbook` — SQLite-Provider (`UseSqlite`, `ServiceCollectionExtensions.cs:67`).
- Weitere Aufrufer desselben Endpunkts (Nebenbefund, gleicher Fehlerpfad): `Components/Shared/CalendarEventDialog.razor` (Rezept-Picker, `TriggerRecipeSearchAsync`) und `Components/Pages/RecipeEdit.razor` (`SearchSideDishesAsync`); zusätzlich `Components/Shared/RecipeSelectDialog.razor` über `GET api/recipes/available-for/{cookbookId}`.
- Tests: `Rezepte.Tests/Services/RecipeServiceTests.cs` (bestehende `SearchAsync_*`-Tests laufen auf dem **InMemory**-Provider und decken Provider-spezifisches Verhalten nicht ab); `Rezepte.Tests.Browser` für einen E2E-Regressionstest der Suchseite.

## Implementierungsansatz

Bugfix — zunächst den Fehlpfad in der Bestandsaufnahme eindeutig identifizieren, dann beheben. Verifikationsergebnisse aus der Übersetzungsphase (gegen den aktuellen Branch `task/issue-171-…`, identisch mit `origin/main`):

- `RecipeService.SearchAsync` findet auf einer **echten SQLite**-Datenbank ein per `CreateAsync` angelegtes Rezept über den Titel korrekt (Scratch-Test mit `UseSqlite`/`:memory:`: grün).
- `GET /api/recipes/search?q=Honig` liefert mit gültigem JWT `200` und die Trefferliste.
- E2E (Playwright, Chromium): Login → Navbar-Suche → `/recipes/search?q=Honig` zeigt das Rezept; Direktaufruf und erneute Suche über `#search-q` ebenfalls erfolgreich. **Lokal ist der gemeldete Fehler nicht reproduzierbar.**

Zu prüfende/behebende Defekt-Kandidaten (Annahmen, nicht verifiziert als Ursache):

1. Der `catch (OperationCanceledException)`-Pfad in `RunSearchAsync` (`RecipeSearch.razor:136-139`) zeigt nach Timeout/Abbruch des internen Requests „Keine Treffer." statt `SearchFailed` — maskiert jeden Infrastrukturfehler als leere Treffermenge. Fehlerbehandlung so umbauen, dass Abbruch/Timeout und echter Fehler nicht mehr als „keine Ergebnisse" erscheinen.
2. Fehlendes `@rendermode InteractiveServer` auf `RecipeSearch.razor`: statische SSR-Seite mit inerter Paginierung (`@onclick`-Buttons ohne Circuit) und `0`-Bindung der int-Query-Parameter. Vergleich mit den übrigen Seiten nahegelegt.
3. `ApiClient`-Self-Call über `nav.BaseUri` mit absoluter URI `/api/recipes/search` (andere Komponenten nutzen relative URIs `api/…`): bei Betrieb hinter Reverse Proxy/PathBase (Deployment: Linux + systemd + Kestrel, siehe `Docs/install.md`) kann der Aufruf 404/Timeout liefern; Fehlerbehandlung gemäß Punkt 1 prüfen.
4. `EF.Functions.Like` auf SQLite ist nur für ASCII case-insensitiv — für exakt geschriebene Begriffe unerheblich, bei abweichender Groß-/Kleinschreibung mit Umlauten jedoch trefferlos; Robustheit der Vergleichslogik (z. B. Collation/`COLLATE NOCASE` bzw. clientseitige Auswertung) zu prüfen.
5. `string.Join` in der `Snippet`-Projektion und die `Include`-Karteprodukt-Menge: auf dem SQLite-Provider aktuell übersetzbar (verifiziert), aber auf InMemory getestete Pfade decken Provider-Unterschiede nicht ab — bestehende `SearchAsync`-Tests zusätzlich/ersetzend auf SQLite ausführen.

Kein neuer Erweiterungspunkt/kein neues Interface erforderlich; Abhängigkeiten bleiben `IRecipeService`, `ApiClient`, `NavigationManager`, `IStringLocalizer<UiStrings>`.

## Konfiguration

Keine Konfigurationserweiterung erforderlich. Zu beachten: `Jwt:Key` muss in Nicht-Development-Umgebungen gesetzt sein (`JwtSigningKeyProvider` wirft sonst beim Start); die App läuft produktiv hinter Kestrel/Reverse Proxy — der serverseitige Self-Call der Suchseite über `nav.BaseUri` ist bei der Fehleranalyse mitzuberücksichtigen.

## Offene Fragen

- Welche Meldung wird exakt angezeigt — „Keine Treffer." (`NoSearchResults`) oder „Die Suche ist fehlgeschlagen." (`SearchFailed`)? Das unterscheidet „API liefert leere Menge / Abbruchpfad" von „Exception-Pfad".
- Was steht im Server-Log beim Suchvorgang (`LogError` „Recipe search for query {Query} failed" vs. `LogInformation` „… was cancelled or timed out")?
- Produktiv-Topologie: läuft die App hinter einem Reverse Proxy, mit TLS-Terminierung oder unter einem Subpfad/PathBase? (Betrifft den `ApiClient`-Self-Call.)
- Tritt das Problem für alle Benutzer und alle Begriffe auf — auch bei direktem Aufruf von `GET /api/recipes/search?q=…` mit gültigem JWT (z. B. via `GET /api/session/token`)?
- Soll die Suche case-insensitiv auch für deutsche Umlaute funktionieren (z. B. „äpfel" findet „Äpfel"), oder genügt der dokumentierte Ist-Umfang?
