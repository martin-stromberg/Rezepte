# Anforderungsübersetzung

## Metadaten

- Aufgaben-ID: `57ac68cf-25c5-4971-af19-e96716a6ecdc`
- Branch: `task/issue-185-57ac68cf25c54971af19e96716a6ecdc-browser-e2e-test-fehlschlaegt`
- Thema: Flaky Browser-E2E-Test `InPageSearch_Submit_UpdatesResultList` im Pre-Release-Workflow (Issue #185)

## Fachliche Zusammenfassung

Der Browser-E2E-Test `Rezepte.Tests.Browser.RecipeSearchBrowserTests.InPageSearch_Submit_UpdatesResultList` schlägt im Pre-Release-Workflow (`.github/workflows/staging-ci.yml`, Job `build & test`) sporadisch fehl: Nach `FillAsync("#search-q", title)` enthält das Suchfeld weiterhin den ursprünglichen Query-Parameter-Wert (`E2E-AndererBegriff-…`) statt des gesetzten Begriffs (`E2E-Formular-…`), sodass die Playwright-Assertion `Expect(...).ToHaveValueAsync` in `RecipeSearchBrowserTests.cs:166` nach Ablauf des Timeouts fehlschlägt. Ursache ist eine Race Condition zwischen dem Befüllen des Eingabefelds und dem Start des interaktiven Blazor-Server-Circuits (`InteractiveServer`): Der SSR-Prerender zeigt bereits `.alert-secondary`, danach durchläuft die Komponente im Circuit `OnParametersSetAsync`/`RunSearchAsync` erneut und schreibt das `value`-Attribut von `#search-q` erneut mit `q`. Zu beheben ist diese Instabilität, sodass der Test deterministisch grün läuft.

## Fachlicher Scope

- **Verarbeitungseinheit:** Ein E2E-Testdurchlauf der In-Page-Rezeptsuche auf `/recipes/search` — konkret die Interaktion „Suchbegriff im Formularfeld `#search-q` ändern und per GET-Formular absenden" einschließlich des Übergangs vom SSR-Prerender zum interaktiven Circuit.
- **Auslösende Ereignisse:** Ausführung des Browser-Tests im GitHub-Actions-Lauf des Pre-Release-Workflows (Playwright/Chromium); testintern: `GotoAsync("/recipes/search?q=…")`, Sichtbarwerden von `.alert-secondary`, `FillAsync("#search-q", …)`, `ClickAsync` auf den Submit-Button.
- **Erwartete Wirkung:** `InPageSearch_Submit_UpdatesResultList` läuft im Pre-Release-Workflow stabil ohne Wiederholungsbedarf grün. Der per `FillAsync` gesetzte Suchbegriff bleibt bis zum Submit im Feld erhalten (bzw. wird zu einem Zeitpunkt gesetzt, zu dem kein Re-Render ihn mehr zurückschreiben kann); das Submit navigiert zu `/recipes/search?q=<neuer Begriff>` und die Trefferliste zeigt das geseedete Rezept.
- **Umgang mit fehlenden Objekten:** Das benötigte Testrezept wird weiterhin über `RecipeSeedHelper.SeedRecipesAsync` mit eindeutigem GUID-Titel erzeugt (Wiederverwendung bestehender Infrastruktur); keine neuen fachlichen Objekte erforderlich.
- **Scope-Ausschlüsse:**
  - Keine Änderung der fachlichen Suchlogik (`IRecipeService.SearchAsync`, Trefferliste, Pagination, Fehler-/Leerzustands-Alerts).
  - Kein Umbau des Suchformulars auf `@bind`-Zwei-Wege-Bindung — das „plain GET form"-Design ist bewusst gewählt und im Markup von `RecipeSearch.razor` dokumentiert.
  - Keine Änderung am Pre-Release-Workflow oder anderen CI-Jobs.
  - Keine generelle Überarbeitung der übrigen Browser-Tests; lediglich identische Flake-Mechanismen dürfen mit abgedeckt werden, wenn sie ohne Zusatzaufwand aus derselben Korrektur profitieren.

## Betroffene Klassen und Komponenten

- `Rezepte.Tests.Browser/RecipeSearchBrowserTests.cs` — Test `InPageSearch_Submit_UpdatesResultList` (Zeilen 154–180), Assertion an Zeile 166; ggf. Timeout-Konstanten (`PageContentTimeoutMilliseconds`, `InteractionRetryTimeoutMilliseconds`).
- `Rezepte.Web/Components/Pages/RecipeSearch.razor` — GET-Suchformular mit `#search-q` und `value="@(q ?? string.Empty)"` (Zeilen 19–30); `OnParametersSetAsync`/`RunSearchAsync` (Zeilen 112–180), die beim Circuit-Start ein zweites Mal durchlaufen werden und die `isLoading`-Re-Renders auslösen.
- `Rezepte.Tests.Browser/Infrastructure/LoadingBarPageObject.cs` / `LoadingBarBrowserSession.cs` — mögliche Erweiterung um ein Warten auf „interaktiver Circuit aktiv / Seite stabil" vor der Formularinteraktion.
- `Rezepte.Tests.Browser/Infrastructure/RecipeSeedHelper.cs` — unverändert (Testdaten-Seeding).
- `.github/workflows/staging-ci.yml` — nur Ausführungskontext (Workflow `Pre-Release`, Job `build & test`), keine Änderung.
- Keine Datenmodell-, Service- oder Interface-Änderungen zu erwarten.

## Implementierungsansatz

- **Fehlerbild-Analyse:** Der Prerender liefert `.alert-secondary` („Keine Treffer."), worauf der Test wartet. Anschließend verbindet sich `blazor.server.js`, die Komponente läuft im interaktiven Circuit und `OnParametersSetAsync` → `RunSearchAsync` wird erneut ausgeführt (`isLoading` toggelt → Re-Renders). Dabei wird das einseitig gerenderte `value`-Attribut von `#search-q` erneut mit `q` (`E2E-AndererBegriff-…`) ins DOM geschrieben und überschreibt — je nach Timing — den zwischenzeitlich per `FillAsync` gesetzten Wert. Derselbe „Prerender/Circuit-Doppellauf" wurde bereits bei `Pagination_NextPage_ShowsPageTwo` als Flake-Ursache dokumentiert (siehe `Docs/features/task/issue-171-207f762f55a442c3be8fc7a18460cad2-reparatur-der-suche-tasks.md`, Zeile 34) und dort per Deadline-Retry entschärft (`RecipeSearchBrowserTests.cs`, Zeilen 118–142).
- **Ansatzoptionen (Entscheidung in der Planungsphase):**
  1. *Testseitig:* Vor `FillAsync` auf einen stabilen Zustand warten (interaktiver Circuit aktiv, kein laufender `isLoading`-Zyklus) und/oder Fill → Submit als wiederholbare Interaktion mit Deadline nach dem Muster der Paginierungs-Klick-Schleife auslegen; der Submit-Nachweis bleibt über `WaitForURLAsync` abgesichert.
  2. *Anwendungsseitig:* Sicherstellen, dass ein Re-Render den Live-Input-Wert von `#search-q` nicht zurückschreibt (z. B. `value` nur im initialen/Prerender-Frame ausgeben oder die Vorbelegung auf einen Weg lösen, der kein späteres Überschreiben auslöst), ohne das dokumentierte „plain GET form"-Design zu brechen.
- **Abhängigkeiten:** Playwright (`Page.FillAsync`, `Assertions.Expect(...).ToHaveValueAsync`, `WaitForURLAsync`), `LoadingBarPageObject.GotoAsync`, `RecipeSeedHelper.SeedRecipesAsync`; Referenz-Retry-Muster in `RecipeSearchBrowserTests.cs` Zeilen 118–142.

## Konfiguration

Keine Konfigurierbarkeit erforderlich. Timeout- und Retry-Budgets bleiben Testkonstanten in `RecipeSearchBrowserTests`.

## Offene Fragen

- Soll die Behebung rein testseitig erfolgen (Stabilisierung des Tests) oder auch anwendungsseitig (ein getippter Suchbegriff darf vom Circuit-Start/Re-Render nicht zurückgesetzt werden)? Ein anwendungsseitiger Fix verändert reales Nutzerverhalten und geht über „Test korrigieren" hinaus.
- Falls anwendungsseitig: Wie wird die Vorbelegung des Felds mit `q` nach GET-Navigation weiterhin gewährleistet, ohne dass spätere Re-Renders den Nutzer-Input überschreiben?
- Woran wird „nicht mehr flaky" festgemacht — genügt ein erfolgreicher Pre-Release-Lauf, oder sind wiederholte CI-Läufe als Verifikation vorgesehen?
