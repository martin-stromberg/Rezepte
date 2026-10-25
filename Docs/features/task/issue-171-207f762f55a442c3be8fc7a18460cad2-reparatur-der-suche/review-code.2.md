# Code-Review

## Ergebnis

**Status:** Befunde vorhanden

## Befunde

### RecipeSearch.razor (RecipeSearch)

- **Namenskonventionen und Einheitlichkeit** — Z. 72 (geänderte Zeile): `Seite @CurrentPage von @TotalPages` ist hartkodierter deutscher UI-Text ohne Localizer, obwohl die Projektregel User-Messages nur über den Localizer erlaubt und sämtliche benachbarten Strings derselben Komponente lokalisiert sind (`PageNavigation`, `PreviousPage`, `NextPage`, `SearchInProgress`, `NoSearchResults`, `SearchFailed`). Ein passender `UiStrings`-Schlüssel existiert nicht. Dasselbe Muster im überarbeiteten Formularblock: Z. 20 `<button ...>Suchen</button>` ist ebenfalls hartkodiert.

  Empfehlung: `UiStrings`-Schlüssel ergänzen (z. B. `SearchPageOf` = „Seite {0} von {1}" und `SearchSubmit` = „Suchen") und die Texte über `@Localizer[...]` ausgeben.

- **Fehlerbehandlung** — `RunSearchAsync` (Z. 114–117): Die ersetzte `CancellationTokenSource` wird mit `cts?.Cancel()` abgebrochen, aber nie disposed; auch die aktive Quelle wird beim Abbau der Komponente weder abgebrochen noch disposed (die Komponente implementiert kein `IDisposable`/`OnDisposing`). Jeder Suchlauf allokiert eine neue `CancellationTokenSource` mit 10-s-Timer (`CancelAfter`), deren Ressourcen bis zum Timer-Ablauf/Finalisierung offen bleiben.

  Empfehlung: Beim Ersetzen `cts?.Cancel(); cts?.Dispose();` ausführen (eine gecancelte Quelle darf disposed werden; der bereits ausgegebene Token bleibt für den laufenden Request gültig) und die Komponente `IDisposable` implementieren lassen, die `cts` beim Abbau cancelt und disposed.

### MainLayout.razor (MainLayout)

- **Kopplung und Erweiterbarkeit / Einheitlichkeit** — Z. 111–121 in Verbindung mit Z. 39: Die beiden Submit-Pfade desselben Formulars verhalten sich bei leerer Eingabe unterschiedlich. Der interaktive Pfad (`@onsubmit="OnSubmitSearch"`, Circuit aktiv) navigiert bei leerem `searchQuery` zu `/recipes`; der native GET-Fallback (`action="/recipes/search" method="get"`, Circuit noch nicht verbunden) sendet `q=` an `/recipes/search` und landet damit auf der Suchergebnisseite statt auf der Rezeptliste. Dasselbe UI-Element erzeugt je nach Verbindungszustand ein anderes Navigationsziel.

  Empfehlung: Das Verhalten angleichen — z. B. `OnSubmitSearch` ebenfalls auf `/recipes/search?q=…` navigieren lassen und die leere Eingabe server-/seitenseitig konsistent behandeln, oder die Zielseite für den leeren Query-Fall auf `/recipes` umleiten.

### RecipeSearchComponentTests.cs (RecipeSearchComponentTests)

- **Testqualität** — Der generische Fehlerpfad `catch (Exception)` in `RecipeSearch.RunSearchAsync` (Z. 150–156, `searchFailed = true` + `LogError`) hat keinen Komponententest. Der Plan (Schritt 5) verlangt die Prüfung „bei `OperationCanceledException` und bei generischer Exception"; umgesetzt ist nur der Abbruchfall (`RunSearchAsync_WhenCancelled_ShowsSearchFailed`). `StubHttpMessageHandler.Throwing` akzeptiert bereits beliebige Exceptions.

  Empfehlung: Test ergänzen, z. B. `RunSearchAsync_WhenRequestFails_ShowsSearchFailed` mit `StubHttpMessageHandler.Throwing(new HttpRequestException("…"))`, der `alert-danger` (`SearchFailed`) und das Fehlen von `alert-secondary` assertiert.

### RecipeSearchBrowserTests.cs (RecipeSearchBrowserTests)

- **Testqualität** — `Pagination_NextPage_ShowsPageTwo`, Retry-Schleife (Z. 122–134): `nextButton.ClickAsync()` wird blind wiederholt, ohne zu prüfen, ob der vorherige Klick bereits gewirkt hat. Ein verspätet wirksamer Klick (Seite 2 wird aktiv, während `pageTwoLabel.WaitForAsync` gerade ausläuft) gefolgt vom Retry-Klick navigiert auf Seite 3 — bei 12 Treffern und `pageSize=5` zeigt Seite 3 nur 2 Einträge, sodass das abschließende `ToHaveCountAsync(5)` sporadisch fehlschlägt. Der `catch (TimeoutException)`-Block ist zudem leer (Retry-Absicht nur aus dem Kommentar über der Schleife ersichtlich).

  Empfehlung: Vor jedem Retry-Klick den Zielzustand prüfen und ggf. abbrechen — z. B. `if (page.Url.Contains("page=2")) break;` oder Sichtbarkeitsprüfung des Seite-2-Labels vor `ClickAsync()` — und den leeren catch-Block um einen Kommentar oder ein Delay ergänzen, das die Retry-Absicht dokumentiert.

## Befundlage zu Iteration 1

Alle vier Befunde aus `review-code.1.md` sind im aktuellen Stand behoben:

- `RecipeSearch.razor` `finally`-Block: Zustandsrücksetzung an `ReferenceEquals(cts, localCts)` gekoppelt (Z. 157–165) — behoben.
- `CalendarEventDialog.razor`: Alle sechs `Client.Http`-Aufrufe nutzen relative URIs (`api/recipes/…` Z. 326, `api/calendar` Z. 382/390, `api/calendar/{EditingId}` Z. 398, `api/recipes/search` Z. 504, `api/recipes/{recipeId}/side-dishes` Z. 582) — behoben.
- `RecipesControllerSearchTests.cs`: Clamping-Test als `[Theory]` mit zwei `[InlineData]`-Grenzfällen umgebaut (Z. 48–61) — behoben.
- `RecipeServiceSearchSqliteTests.cs`: `SqliteConnection` als Feld gehalten und via `IDisposable.Dispose` geschlossen (Z. 17–46) — behoben.

## Geprüfte Dateien

Liste aller geprüften Dateien:
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
