# Code-Review

## Ergebnis

**Status:** Befunde vorhanden

## Befunde

### RecipeSearch.razor (RecipeSearch)

- **Fehlerbehandlung** — `Dispose()` (Z. 171–175) in Verbindung mit `RunSearchAsync` (catch `OperationCanceledException` Z. 138–151, `finally` Z. 159–167): `Dispose()` cancelt und disposed `cts`, lässt das Feld aber auf der verworfenen Quelle stehen (`cts` wird nicht auf `null` gesetzt und es gibt kein Disposed-Flag). Ein durch den Komponenten-Abbau abgebrochener, noch laufender Suchlauf (z. B. Wegnavigieren während des Requests) landet deshalb im `ReferenceEquals(cts, localCts) == true`-Zweig: Der Teardown-Abbruch wird fälschlich als Timeout/Fehlschlag klassifiziert — `LogWarning` „was cancelled or timed out" (verwässert genau das Diagnosesignal, auf das Plan-Schritt 10 für die Produktiv-Verifikation angewiesen ist), `searchFailed = true` samt Zustandsrücksetzung und ein `StateHasChanged()` auf einer bereits abgebauten Komponente im `finally`.

  Empfehlung: In `Dispose()` nach `cts?.Dispose()` das Feld auf `null` setzen (oder ein `_disposed`-Flag einführen und in catch/finally prüfen), damit die Fortsetzung den Supersede-Pfad nimmt — keine Fehler-Logmeldung, keine Zustandsänderung, kein Render-Request auf der abgebauten Komponente.

### MainLayout.razor (MainLayout)

- **Fehlerbehandlung / Namenskonventionen und Einheitlichkeit** — `OnSubmitSearch` (Z. 115): `searchQuery.Trim()` wird nun unbedingt aufgerufen; die vorherige Implementierung war über `string.IsNullOrWhiteSpace(searchQuery)` null-sicher. `InputText` bindet auf `string?`-Ebene — der Binder kann dem Feld `null` zuweisen (z. B. bei einem null-Change-Wert), was hier zu einer `NullReferenceException` im Submit-Handler führen würde (`Uri.EscapeDataString(null)` wirft ebenfalls). Die Codebasis behandelt identisch deklarierte gebundene Suchfelder bereits defensiv: `recipeSearchQuery?.Trim()` (`CalendarEventDialog.razor` Z. 490) und `sideDishSearchQuery?.Trim()` (`RecipeEdit.razor` Z. 416) — beide als `private string ... = string.Empty` deklariert.

  Empfehlung: `var q = Uri.EscapeDataString(searchQuery?.Trim() ?? string.Empty);` verwenden, um die Null-Sicherheit der Vorgängerversion und die Repo-Konvention wiederherzustellen.

## Befundlage zu Iteration 2

Alle vier Befunde aus `review-code.2.md` sind im aktuellen Stand behoben:

- `RecipeSearch.razor` hartkodierte UI-Texte: Neue `UiStrings`-Schlüssel `SearchPageOf` (`Seite {0} von {1}`, `UiStrings.resx` Z. 86) und `SearchSubmit` (`Suchen`, Z. 87) angelegt; Verwendung in Z. 21 (`@Localizer["SearchSubmit"]`) und Z. 73 (`@Localizer["SearchPageOf", CurrentPage, TotalPages]`) — behoben.
- `RecipeSearch.razor` `CancellationTokenSource`-Leck: `@implements IDisposable` (Z. 3) + `Dispose()` mit `cts?.Cancel(); cts?.Dispose();` (Z. 171–175); beim Ersetzen in `RunSearchAsync` wird die alte Quelle ebenfalls disposed (Z. 115–116) — behoben (Restrisiko des Teardown-Pfads als neuer Befund oben).
- `MainLayout.razor` Leer-Eingabe-Divergenz: `OnSubmitSearch` navigiert jetzt immer zu `/recipes/search?q=…` (Z. 111–118), identisch zum nativen GET-Fallback `action="/recipes/search" method="get"` (Z. 39) — behoben.
- `RecipeSearchBrowserTests.cs` Pagination-Retry: Vor jedem Retry-Klick wird der Zielzustand geprüft (`page.Url.Contains("page=2")` oder Label-Sichtbarkeit, Z. 127), sodass ein verspätet wirksamer Klick nicht von einem Overshoot-Klick nach Seite 3 gefolgt wird; der catch-Block dokumentiert die Retry-Absicht und das Gesamtbudget läuft über `deadline` (Z. 124–142) — behoben.
- Fehlender Test für den generischen Fehlerpfad: `RunSearchAsync_WhenRequestFails_ShowsSearchFailed` ergänzt (`RecipeSearchComponentTests.cs` Z. 55–67) mit `StubHttpMessageHandler.Throwing(new HttpRequestException(...))`, assertiert `alert-danger` und das Fehlen von `alert-secondary` — behoben.

## Geprüfte Dateien

Liste aller geprüften Dateien:
- `Rezepte.Web/Components/Pages/RecipeSearch.razor`
- `Rezepte.Web/Components/Layout/MainLayout.razor`
- `Rezepte.Web/Components/Shared/CalendarEventDialog.razor`
- `Rezepte.Web/Resources/UiStrings.resx`
- `Rezepte.Tests.Browser/LoadingBarFormNavigationBrowserTests.cs`
- `Rezepte.Tests.Browser/Infrastructure/RecipeSeedHelper.cs`
- `Rezepte.Tests.Browser/RecipeSearchBrowserTests.cs`
- `Rezepte.Tests/Components/RecipeSearchComponentTests.cs`
- `Rezepte.Tests/Components/StubHttpMessageHandler.cs`
- `Rezepte.Tests/Controllers/RecipesControllerSearchTests.cs`
- `Rezepte.Tests/Services/RecipeServiceSearchSqliteTests.cs`
