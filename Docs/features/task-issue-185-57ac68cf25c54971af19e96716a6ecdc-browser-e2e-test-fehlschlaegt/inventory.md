# Bestandsaufnahme: Flaky Browser-E2E-Test `InPageSearch_Submit_UpdatesResultList` (Issue #185)

Bestandsaufnahme des Browser-E2E-Tests `Rezepte.Tests.Browser.RecipeSearchBrowserTests.InPageSearch_Submit_UpdatesResultList`,
der Seite `/recipes/search` (`RecipeSearch.razor`) und der zugehörigen Testinfrastruktur,
bezogen auf die Anforderung in `requirement.md` (Stabilisierung des sporadisch im
Pre-Release-Workflow fehlschlagenden Tests).

## Zusammenfassung

- **Fehlerbild bestätigt (im Code nachvollziehbar):** `RecipeSearch.razor` läuft mit
  `@rendermode InteractiveServer` unter aktivem Prerendering; `OnParametersSetAsync` →
  `RunSearchAsync` wird beim Circuit-Start erneut ausgeführt und toggelt `isLoading`
  (zusätzliche Re-Renders). Das `value`-Attribut von `#search-q` wird einseitig aus `q`
  gerendert (`Rezepte.Web/Components/Pages/RecipeSearch.razor`, Z. 25–26) und kann den per
  `FillAsync` gesetzten Wert zeitabhängig zurückschreiben — genau das in Issue #185
  beobachtete Bild (`E2E-AndererBegriff-…` statt `E2E-Formular-…`).
- **Der betroffene Test hat keinerlei Schutz gegen den Prerender/Circuit-Doppellauf:**
  `InPageSearch_Submit_UpdatesResultList` (`RecipeSearchBrowserTests.cs`, Z. 154–180) wartet
  nur auf `.alert-secondary` aus dem SSR-Prerender, füllt dann ohne Stabilitäts-Check
  `#search-q` und assertiert sofort. Es existiert kein Helfer wie „Circuit aktiv/Seite stabil".
- **Etabliertes Retry-Muster bereits vorhanden:** `Pagination_NextPage_ShowsPageTwo`
  (Z. 118–142) löst denselben Flake-Mechanismus per Deadline-Schleife um den Klick;
  `SubmitInteractiveShoppingListItemAsync` (`LoadingBarPageObject.cs`, Z. 171–204) nutzt ein
  ähnliches Muster. `DirectUrl_WithQuery_ShowsResults`/`Pagination_NextPage_ShowsPageTwo`
  kommentieren das `isLoading`-Transientfenster explizit (Z. 66–68, 107–109).
- **Infrastruktur vollständig vorhanden:** `LoadingBarPageObject` (Page Object mit
  Goto/Login/Route-Kontrolle/Loading-Bar-Warten), `LoadingBarBrowserSession`,
  `RecipeSeedHelper.SeedRecipesAsync` (unverändert nutzbar), `RezepteAppFixture`
  (App-Prozess gegen SQLite-Temp-DB, Publish-Pfad via `REZEPTE_PUBLISH_DIR` oder
  `bin/<Cfg>/<tfm>/publish`), `PlaywrightBrowserFixture` (headless Chromium, Skip statt Fail).
- **Kein Datenmodell-/Interface-Umbau nötig:** `IRecipeService.SearchAsync` und die
  `SearchResult`-/`SearchResultItem`-Modelle sind unverändert einsetzbar (Scope-Ausschluss).
  Auffällig: `IRecipeService` ist in `RecipeService.cs` deklariert; `IRecipeService.cs`
  enthält nur ein `using`.
- **Test-Ausgangszustand:** `Rezepte.Tests` 714/714 grün; `Rezepte.Tests.Browser` 24/25 —
  ein nachgewiesener Fehlschlag in `RegisterTests.Register_WithDemoData_CreatesDemoData`
  (120-s-Timeout, Kalender-Demodaten blieben 0 bei `seed job status=Succeeded`; ohne Bezug
  zum Suchpfad). Der Zieltest war im Basislauf und in 5/5 gefilterten Wiederholungsläufen
  grün — der Flake ließ sich lokal nicht reproduzieren. Details und Nachweise:
  [Tests](inventory/tests.md).

## Details

- [Logik](inventory/logic.md) — `RecipeSearch`-Komponente, Navbar-Suche, `RecipeService.SearchAsync`,
  Browser-Test-Infrastruktur, CI-Ausführungskontext
- [Interfaces](inventory/interfaces.md) — `IRecipeService` (relevante Member)
- [Datenmodelle](inventory/models.md) — `RecipeSearch.RecipeResult`, `RecipeService.SearchResult`/`SearchResultItem`
- [Tests](inventory/tests.md) — Test-Ausgangszustand mit Lauf-Nachweisen, nachgewiesene Fehler,
  Testklassen und Hilfsmethoden
