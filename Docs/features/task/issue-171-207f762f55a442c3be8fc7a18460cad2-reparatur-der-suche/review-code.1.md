# Code-Review

## Ergebnis

**Status:** Befunde vorhanden

## Befunde

### RecipeSearch.razor (RecipeSearch)

- **Fehlerbehandlung** — `RunSearchAsync`, `finally`-Block (Z. 160–164): `isLoading = false` und `StateHasChanged()` werden unbedingt ausgeführt — auch dann, wenn die Suche bereits durch eine neuere ersetzt wurde (Supersede-Fall, `!ReferenceEquals(cts, localCts)`). Der `catch`-Block unterscheidet die beiden Fälle korrekt, der `finally`-Block jedoch nicht: Läuft die neuere Suche noch, setzt die abgebrochene Suche `isLoading` zurück und triggert ein Re-Render mit leerer Trefferliste — die UI blendet damit kurzzeitig exakt den Zustand „Keine Treffer." (`alert-secondary`) ein, den dieser Fix beseitigen soll. Das Szenario ist real: Der im Plan dokumentierte Prerender/Circuit-Doppellauf erzeugt regelmäßig zwei überlappende `RunSearchAsync`-Aufrufe.

  Empfehlung: Die Zustandsrücksetzung im `finally`-Block ebenfalls an `ReferenceEquals(cts, localCts)` koppeln (z. B. `if (ReferenceEquals(cts, localCts)) { isLoading = false; }`), damit eine abgelöste Suche den Ladezustand der aktiven Suche nicht verwischt.

### CalendarEventDialog.razor (CalendarEventDialog)

- **Namenskonventionen und Einheitlichkeit** — Der Fix stellt nur den Such-Aufruf auf die relative URI-Form um (`api/recipes/search`, Z. 504), während alle übrigen `Client.Http`-Aufrufe derselben Komponente weiterhin absolute `/api/…`-URIs verwenden: Z. 326 (`/api/recipes/{form.RecipeId}`), Z. 382 und Z. 390 (`/api/calendar`), Z. 398 (`/api/calendar/{EditingId}`) und Z. 582 (`/api/recipes/{recipeId}/side-dishes`). Damit bleibt genau der behobene PathBase-/Reverse-Proxy-Defekt an fünf Stellen in derselben Datei bestehen, und die Komponente mischt zwei URI-Konventionen.

  Empfehlung: Die verbleibenden `Client.Http`-Aufrufe in der Komponente auf relative URIs (`api/recipes/…`, `api/calendar…`) umstellen.

### RecipesControllerSearchTests.cs (RecipesControllerSearchTests)

- **Testqualität** — `SearchAsync_ShouldClampPageAndPageSize` (Z. 45–57) prüft zwei fachliche Fälle in einer Testmethode: einmal `page: 0, pageSize: 0` → `1, 1` und einmal `page: -3, pageSize: 150` → `1, 100` (zwei `SearchAsync`-Aufrufe, zwei getrennte `Verify`s).

  Empfehlung: In zwei getrennte Tests aufteilen oder als `[Theory]` mit `[InlineData]` je Grenzfall ausdrücken, damit ein Fehlschlag eindeutig einem Fall zugeordnet werden kann.

### RecipeServiceSearchSqliteTests.cs (RecipeServiceSearchSqliteTests)

- **Fehlerbehandlung** — `CreateSqliteDb` (Z. 22–34): Die `SqliteConnection` wird erzeugt und geöffnet, aber nie disposed — beim `await using var db` wird nur der `RezepteDbContext` disposed; EF Core gibt extern übergebene Verbindungen nicht frei. Das vom Plan referenzierte Muster (`ExportServiceExportTests` Z. 24) hält die Connection dagegen als `await using`-Variable im Test und schließt sie sauber.

  Empfehlung: Die Connection pro Test fachlich mitverwalten — z. B. `CreateSqliteDb` als Instanzmethode, die die Connection in einem Feld hält und in `IDisposable`/`IAsyncDisposable` der Testklasse schließt, oder Connection und Context gemeinsam (z. B. als Tupel) zurückgeben.

## Geprüfte Dateien

- `Rezepte.Web/Components/Pages/RecipeSearch.razor`
- `Rezepte.Web/Components/Layout/MainLayout.razor`
- `Rezepte.Web/Components/Shared/CalendarEventDialog.razor`
- `Rezepte.Tests.Browser/LoadingBarFormNavigationBrowserTests.cs`
- `Rezepte.Tests.Browser/Infrastructure/RecipeSeedHelper.cs`
- `Rezepte.Tests.Browser/RecipeSearchBrowserTests.cs`
- `Rezepte.Tests/Components/RecipeSearchComponentTests.cs`
- `Rezepte.Tests/Components/StubHttpMessageHandler.cs`
- `Rezepte.Tests/Controllers/RecipesControllerSearchTests.cs`
- `Rezepte.Tests/Services/RecipeServiceSearchSqliteTests.cs`
