# Umsetzungsplan: Stabilisierung des Browser-E2E-Tests `InPageSearch_Submit_UpdatesResultList` (Issue #185)

## Übersicht

Der Browser-E2E-Test `Rezepte.Tests.Browser.RecipeSearchBrowserTests.InPageSearch_Submit_UpdatesResultList`
schlägt im Pre-Release-Workflow sporadisch fehl, weil `FillAsync("#search-q", …)` in das
Zeitfenster zwischen SSR-Prerender und interaktivem Blazor-Server-Circuit fallen kann und ein
Circuit-Re-Render (`OnParametersSetAsync` → `RunSearchAsync`, `isLoading`-Toggle) das
einseitig gerenderte `value`-Attribut von `#search-q` mit `q` zurückschreibt. Umgesetzt wird
eine zweigleisige Behebung: **anwendungsseitig** wird das `value`-Rendering in
`Rezepte.Web/Components/Pages/RecipeSearch.razor` so abgesichert, dass ein getippter Wert den
Prerender→Interactive-Übergang und alle späteren Re-Renders bei unverändertem `q` überlebt;
**testseitig** bleibt zusätzlich die bereits geplante Deadline-Retry-Stabilisierung als
Absicherung gegen Rest-Races erhalten. Betroffen sind `RecipeSearch.razor`,
`Rezepte.Tests.Browser/RecipeSearchBrowserTests.cs` und
`Rezepte.Tests/Components/RecipeSearchComponentTests.cs`.

## Scope-Klärung

Verbindliche fachliche Abgrenzung vor der Umsetzung — übernommen aus dem Abschnitt
`Fachlicher Scope` der `requirement.md` und konkretisiert um die vom Anwender geforderte
anwendungsseitige Absicherung:

| Feld | Festlegung |
|------|------------|
| Verarbeitungseinheit | Ein E2E-Testdurchlauf bzw. Nutzerdurchlauf der In-Page-Rezeptsuche auf `/recipes/search` — die Interaktion „Suchbegriff im Formularfeld `#search-q` ändern und per GET-Formular absenden" einschließlich des Übergangs vom SSR-Prerender zum interaktiven Circuit |
| Auslösende Ereignisse | Ausführung des Browser-Tests im GitHub-Actions-Lauf (Playwright/Chromium) bzw. reale Nutzereingabe; testintern: `GotoAsync("/recipes/search?q=…")`, Sichtbarwerden von `.alert-secondary`, `FillAsync("#search-q", …)`, `ClickAsync` auf den Submit-Button |
| Erwartete Wirkung | (1) Anwendungsseitig: Ein in `#search-q` getippter Wert überlebt den Prerender→Interactive-Circuit-Übergang und wird von Re-Renders bei unverändertem `q` nicht mehr zurückgeschrieben; bei tatsächlicher `q`-Änderung (Parameter-Navigation) wird das Feld weiterhin korrekt auf das neue `q` synchronisiert. (2) `InPageSearch_Submit_UpdatesResultList` läuft im Pre-Release-Workflow stabil grün: Der per `FillAsync` gesetzte Begriff bleibt bis zum Submit im Feld bzw. wird so lange erneut gesetzt, bis kein Re-Render ihn mehr zurückschreiben kann; das Submit navigiert zu `/recipes/search?q=<neuer Begriff>` und die Trefferliste zeigt das geseedete Rezept |
| Umgang mit fehlenden Objekten | Das Testrezept wird weiterhin über `RecipeSeedHelper.SeedRecipesAsync` mit GUID-eindeutigem Titel erzeugt (wiederverwenden); für den App-Fix wird eine neue Komponenten-Zustandsproperty (`[PersistentState]`) angelegt, die beim Prerender befüllt und in der interaktiven Instanz wiederhergestellt wird (erzeugen/wiederverwenden); keine weiteren neuen fachlichen Objekte |

### Scope-Ausschlüsse

| # | Einschränkung / Ausschluss | Begründung | Status |
|---|----------------------------|------------|--------|
| 1 | Keine Änderung der fachlichen Suchlogik (`IRecipeService.SearchAsync`, Trefferliste, Pagination, Fehler-/Leerzustands-Alerts) | Expliziter Scope-Ausschluss der Anforderung; der Flake liegt im Prerender/Circuit-Timing, nicht in der Suchlogik | bestätigt (Anwender, 2026-11-15) |
| 2 | Kein Umbau des Suchformulars auf `@bind`-Zwei-Wege-Bindung | Das „plain GET form"-Design ist bewusst gewählt und in `RecipeSearch.razor` (Z. 19–23) dokumentiert. Geprüft gegen den gewählten App-Fix: Der Ansatz verändert nur die **Renderzeit-Entscheidung**, ob das `value`-Attribut emittiert wird — kein `@bind`, kein `oninput`-Handler, das Formular bleibt ein natives GET-Formular, das den Live-Wert des Feldes überträgt. **Kein Konflikt.** | bestätigt (Anwender, 2026-11-15) |
| 3 | Keine Änderung am Pre-Release-Workflow (`.github/workflows/staging-ci.yml`) oder anderen CI-Jobs | Der Workflow ist nur Ausführungskontext | bestätigt (Anwender, 2026-11-15) |
| 4 | Keine generelle Überarbeitung der übrigen Browser-Tests | Nur identische Flake-Mechanismen dürfen ohne Zusatzaufwand profitieren; `LoginAsync`/`SubmitNavigationSearchAsync` (`FillAsync` auf prerendered Seiten) bleiben unverändert, da dort kein Fehlschlag nachgewiesen ist. Hinweis: Der App-Fix reduziert das Flake-Risiko dieser Helfer mit, ohne dass ihr Code angefasst wird | bestätigt (Anwender, 2026-11-15) |

Der frühere Ausschluss „keine anwendungsseitige Änderung am `value`-Rendering von `#search-q`"
wurde vom Anwender (2026-11-15) zurückgewiesen und entfällt — die anwendungsseitige
Absicherung ist jetzt fester Scope-Bestandteil.

## Designentscheidungen

| Komponente / Bereich | Gewählter Ansatz | Begründung |
|----------------------|-----------------|------------|
| Anwendungsseitige Absicherung von `#search-q` | **`[PersistentState]`-gestütztes „Emit-on-q-Change"**: Eine `[PersistentState]`-Property `PrerenderedSearchQuery` trägt das beim SSR-Prerender gerenderte `q` in die neue interaktive Komponenteninstanz. Ein privates Feld `_syncedSearchQuery` (= das `q`, dessen Wert das Eingabefeld bereits repräsentiert) wird beim ersten `OnParametersSet` auf `PrerenderedSearchQuery` primed — beim Mount ohne Prerender-Kontext (clientseitige Navigation, bUnit) bleibt es `null`. Das `value`-Attribut wird nur noch emittiert, wenn `q` von `_syncedSearchQuery` abweicht; danach gilt `q` als synchronisiert. Damit ist das Attribut nach dem Circuit-Start bei unverändertem `q` in **keinem** interaktiven Frame mehr enthalten — getippter Text kann prinzipiell nicht mehr zurückgeschrieben werden | Evaluierte Alternativen: (a1) `value` nur im Prerender emittieren (`RendererInfo.IsInteractive`) — verworfen, weil das Feld dann bei clientseitigen Parameter-Navigationen mit geändertem `q` (In-Circuit-`Nav.NavigateTo`) veraltet bliebe; (a2) `_initialCircuitQ` beim ersten interaktiven `OnParametersSet` merken — verworfen, weil es einen Mount per clientseitiger Navigation nicht vom Post-Prerender-Mount unterscheiden kann (frisch eingefügtes leeres `<input>` würde `q` nie anzeigen) und nach der ersten `q`-Änderung wieder jedes Re-Render schreibt; (b1) `@oninput`-Tracking des getippten Werts — verletzt das dokumentierte „plain GET form"-Design (Ausschluss 2) und hilft im kritischen Fenster nicht, weil vor Circuit-Bereitschaft keine Events beim Server ankommen; (b2) JS-Readback des DOM-Werts in `OnAfterRender` — funktioniert, braucht aber JS-Modul, Interop-Roundtrip und Heuristiken für „User hat getippt vs. frisch gemountet"; deutlich mehr bewegliche Teile als die deklarative `[PersistentState]`-Lösung (.NET 10 stellt das Attribut bereit) |
| Form der Attribut-Unterdrückung | **Null-Bindung statt Splatting**: `value="@(_emitSearchValue ? (q ?? string.Empty) : null)"` — ein zu `null` ausgewerteter Attributwert wird von Blazor nicht gerendert und in Diffs entfernt; das Entfernen des `value`-Attributs lässt die DOM-Property (`input.value`) unangetastet | Minimal-invasiv ohne `@attributes`-Dictionary; `q ?? string.Empty` stellt sicher, dass bei Emission immer ein nicht-null-String geschrieben wird (z. B. `q`→`null`-Übergang setzt das Feld korrekt auf leer statt das Attribut nur zu entfernen) |
| Persistenz-Mechanismus | **`[PersistentState]`-Property** statt `PersistentComponentState`-Service-Code | Deklarativ, keine `RegisterOnPersisting`-Callback-Verwaltung; der Wert wird beim Prerender serialisiert und beim Circuit-Start in die neue Instanz injiziert — genau der benötigte „war dieser Mount prerendered?"-Kontext |
| Barriere „interaktiver Re-Render abgeschlossen" im Nachweis-Test | **Erzwungener In-Circuit-Paginierungszyklus mit `page=2`-Content-Barriere statt DOM-Übergangs-Beobachtung**: `InPageSearch_TypedValue_SurvivesInteractiveCircuit` beweist den Circuit-Render über (a) einen erfolgreichen `@onclick`-Paginierungs-Klick (`ChangePage` → `Nav.NavigateTo`), der nur funktioniert, wenn der Circuit attached ist und Event-Handler verdrahtet sind, und (b) das Erscheinen des Paginierungs-Labels „Seite 2 von 2" im `.pagination`-Block. Die Paginierungs-`nav` steht ausschließlich im `else`-Zweig des `isLoading`-Zustandsblocks (`RecipeSearch.razor` Z. 74–86) und wird nur bei `!isLoading` mit Ergebnissen gerendert; „Seite 2 von 2" belegt damit atomar, dass die In-Circuit-Navigation auf derselben Instanz stattgefunden hat (`CurrentPage == 2`) **und** der post-Navigation-`isLoading`-Suchzyklus vollständig abgelaufen ist (letzter interaktiver Frame im DOM). Die Label-Angabe ist sortierungsunabhängig — kein Bezug darauf, welches der beiden geseedeten Rezepte auf Seite 2 liegt | Der ursprünglich geplante `.list-group-item`-Wartepunkt traf bereits den SSR-Prerender und konnte die Assertion vor dem ersten interaktiven Render freigeben (Regressionsblindheit). Eine `MutationObserver`-Barriere auf `.spinner-border`-Einfügung wurde geprüft und **verworfen**: Sie hängt am Einfüge-/Entferne-Übergang, der nicht garantiert auftritt — landet der `Nächste`-Klick, während die initiale Circuit-Suche noch läuft, bleibt `.spinner-border` wegen des Superseded-Guards (`ReferenceEquals(cts, localCts)`, `RecipeSearch.razor` Z. 150–179) durchgehend über `page=2` hinweg im DOM; die Einfüge-Mutation tritt nie ein und die Warte läuft falsch in den Timeout. In-Circuit-`Nav.NavigateTo` auf dieselbe Route remountet die Komponente zudem **nicht** — Blazor setzt nur die Parameter auf derselben Instanz neu (`OnParametersSet`/`OnParametersSetAsync` erneut, Z. 112–115). Die Content-Barriere ist dagegen transitionsfrei und deckt beide Lade-Abläufe ab (Spinner sichtbar getoggelt oder durchgehend sichtbar) |
| Nachweis der `q`-Synchronisation bei geändertem `q` | **bUnit `SetParametersAndRender` auf derselben Instanz + E2E-Nutzerszenario**: Der Gleich-Instanz-Pfad (`OnParametersSet`-Re-Run mit geändertem `q`) wird deterministisch per bUnit getestet (Emit-on-Change und Suppression). Zusätzlich deckt ein neuer E2E-Test `NavbarSearch_WhileOnSearchPage_UpdatesSearchField` den realen Nutzerfluss „Navbar-Suche, während man bereits auf `/recipes/search` ist" ab | Bewertung der Pfade (verifiziert): `Routes`/`MainLayout` tragen **keinen** `@rendermode` — nur die Seitenkomponenten sind interaktiv; das Navbar-Formular (`@onsubmit` + GET-Fallback, `MainLayout.razor` Z. 39–41) liegt außerhalb der interaktiven Boundary und löst immer eine Dokument-Navigation aus (nativer GET bzw. Enhanced Navigation) → stets **Neu-Mount**, nie `OnParametersSet` auf derselben Instanz. Der Gleich-Instanz-Pfad ist in der App nur über In-Circuit-`Nav.NavigateTo` erreichbar (`ChangePage`, ändert `q` nicht). Ein E2E-Navbar-Submit kann den Gleich-Instanz-Pfad daher gar nicht auslösen — nur bUnit kann `OnParametersSet` mit geändertem `q` auf derselben Instanz deterministisch aufrufen; zudem ist die Attribut-**Suppression** (emit `null` → Attribut-Entfernung) im Browser-DOM optisch nicht vom Schreiben desselben Werts unterscheidbar und nur in bUnit direkt assertierbar |
| Testseitige Stabilisierung | Deadline-Retry um die **gesamte** Interaktion `FillAsync` → `Expect(...).ToHaveValueAsync` → `ClickAsync` → `WaitForURLAsync` bleibt geplant (Budget `InteractionRetryTimeoutMilliseconds`, Vorab-Zielzustandsprüfung auf der URL) | Auch mit App-Fix bleiben Rest-Races denkbar (z. B. ein Navigation-/Re-Render-Ereignis zwischen Wert-Check und Submit oder ein verschluckter Submit); das bewährte Muster aus `Pagination_NextPage_ShowsPageTwo` (Z. 118–142) ist billige Versicherung und unterscheidet weiterhin zwischen „verschluckt" und „defekt" |
| Stabilitäts-Erkennung | **Kein** neuer „interaktiver Circuit aktiv"-Wartehelfer in `LoadingBarPageObject` | Es gibt kein zuverlässiges öffentliches Signal für Circuit-Bereitschaft (nur `blazor.server.js`-/`blazor.web.js`-Interna, die fragil wären); der App-Fix plus Retry gegen beobachtbare Zielzustände (Feldwert + Ziel-URL) deckt alle Timing-Varianten ab. Für den Nachweis-Test wird die Circuit-Aktivität stattdessen **beweisbar erzwungen** (Paginierungs-Klick + `page=2`-Content-Barriere, siehe oben) |

### Ablaufprüfung des App-Fix („Emit-on-q-Change")

Das Eingabefeld `#search-q` wird nur noch dann von einem Render-Frame beschrieben, wenn sich
`q` seit der letzten Synchronisation geändert hat:

| Szenario | Verhalten mit dem gewählten Ansatz |
|----------|-------------------------------------|
| Erster interaktiver Render nach Circuit-Start (neue Instanz, gleiches `q`) | `PrerenderedSearchQuery` wurde aus dem Prerender wiederhergestellt und primed `_syncedSearchQuery = q` → kein `value`-Frame → der inzwischen getippte Text überlebt (der in Issue #185 beobachtete Flake kann nicht mehr auftreten) |
| In-Circuit-`Nav.NavigateTo` mit anderem `q` auf derselben Seite | `q != _syncedSearchQuery` → `value` wird emittiert → Blazor schreibt das neue `q` als DOM-Property ins Feld; `_syncedSearchQuery` folgt → korrekte `q`-Synchronisation bleibt erhalten. **Pfad-Hinweis (verifiziert):** In der aktuellen App existiert kein UI-Auslöser, der `q` in-circuit ändert — `ChangePage` erhält `q`; die Navbar- und In-Page-Suchformulare sind statische GET-Formulare und mounten neu. Der Pfad ist Defensiv-Logik des Fixes und wird per bUnit-`SetParametersAndRender` nachgewiesen |
| Pagination (`ChangePage` → `Nav.NavigateTo` mit gleichem `q`) | `q == _syncedSearchQuery` → kein `value`-Frame → ein getippter, noch nicht abgesendeter Begriff bleibt im Feld (Verbesserung gegenüber heute, wo jeder Re-Render `q` zurückschrieb). Instanz-Hinweis (verifiziert): Die In-Circuit-Navigation remountet die Komponente **nicht** — dieselbe Instanz erhält nur neue Parameter (`page`), sodass das gesetzte `_syncedSearchQuery` direkt die Suppressions-Entscheidung trifft (kein Priming nötig) |
| GET-Formular-Submit (voller Reload bzw. Enhanced Navigation) | Neuer Prerender / neue Instanz: `_syncedSearchQuery` startet `null` bzw. aus dem wiederhergestellten Prerender-Wert → `value` wird beim ersten Render emittiert bzw. die SSR-Antwort enthält das `q`-Attribut — Feld zeigt das neue `q` |
| `isLoading`-Re-Renders mitten im Tippen (`q` unverändert) | Kein `value`-Frame → getippter Text wird zu **keinem** Zeitpunkt mehr überschrieben — der Fix deckt die Race nicht nur beim Circuit-Start, sondern generell ab |
| Clientseitige Erst-Navigation auf `/recipes/search?q=x` von einer anderen Seite | Persistierter Zustand ist bereits konsumiert bzw. nicht vorhanden → `PrerenderedSearchQuery = null`, `_syncedSearchQuery = null` → `q != null` ⇒ `value` wird emittiert → Feld zeigt `x` |
| bUnit-Mounts (`RecipeSearchComponentTests`) | Kein Persistent-State-Kontext → identisch zum clientseitigen Mount: `value` wird bei `q != null` emittiert, bei `q == null` entfällt das leere Attribut |

## Programmabläufe

### Anwendungsseitig: Konditionierte `value`-Emission in `RecipeSearch`

1. Beim SSR-Prerender läuft `OnParametersSet`/`OnParametersSetAsync` normal: `_syncedSearchQuery`
   ist uninitialisiert → `value` wird emittiert wie bisher; `PrerenderedSearchQuery` wird auf
   `q` gesetzt und vom Framework beim Prerender-Abschluss serialisiert.
2. Beim Circuit-Start instanziiert Blazor die Komponente erneut; der Persistent-State-Mechanismus
   stellt `PrerenderedSearchQuery` vor dem ersten Parameter-Setzen wieder her.
3. `OnParametersSet` (neu, synchron; läuft vor `OnParametersSetAsync`):
   - Beim ersten Aufruf dieser Instanz: `_syncedSearchQuery = PrerenderedSearchQuery`
     (Post-Prerender-Mount) bzw. `null` (jeder andere Mount).
   - `_emitSearchValue = q != _syncedSearchQuery` berechnen; `_syncedSearchQuery = q` setzen.
   - `PrerenderedSearchQuery = q` nachziehen (persistiert den Wert im Prerender; im Circuit
     wirkungslos).
4. Render: `<input id="search-q" … value="@(_emitSearchValue ? (q ?? string.Empty) : null)" />`
   — bei `null` entfällt das Attribut; Blazor schreibt die DOM-Property `value` nur bei
   tatsächlicher `q`-Änderung.
5. `RunSearchAsync`, `isLoading`-/`searchFailed`-Zustandsblöcke, Paginierung und das GET-Formular
   bleiben unverändert; der Kommentar am Formular wird um den Grund der konditionierten
   `value`-Emission ergänzt (getippter Live-Wert darf nie überschrieben werden).

Beteiligte Klassen/Komponenten: `RecipeSearch` (`Rezepte.Web/Components/Pages/RecipeSearch.razor`).

### Stabilisierter Testablauf `InPageSearch_Submit_UpdatesResultList`

Schritt-für-Schritt-Beschreibung des geänderten Ablaufs (unveränderte Anfangs- und
Endschritte kursiv markiert):

1. *Session starten über `LoadingBarBrowserSession.StartLoggedInSessionAsync`, Rezept mit
   GUID-eindeutigem Titel über `RecipeSeedHelper.SeedRecipesAsync` seeden.*
2. *`GotoAsync("/recipes/search?q=E2E-AndererBegriff-…")` und auf `.alert-secondary` warten
   (`PageContentTimeoutMilliseconds`) — unverändert; der Prerender-Inhalt bleibt der
   Einstiegspunkt.*
3. Deadline-Retry-Schleife mit Budget `InteractionRetryTimeoutMilliseconds` (Absicherung
   gegen Rest-Races — mit dem App-Fix sollte die erste Iteration stets durchlaufen):
   - Vorab prüfen, ob `Page.Url` bereits `q=<title>` enthält (ein verspätet gelandetes
     Submit einer vorherigen Iteration) — dann Schleife verlassen.
   - `FillAsync("#search-q", title)` ausführen.
   - `Expect(...).ToHaveValueAsync(title)` mit kurzem Timeout (~5 s, analog dem 5-s-Fenster
     der Paginierungs-Schleife): Bleibt der Wert stehen, weiter mit dem nächsten Punkt;
     schlägt die Assertion mit `PlaywrightException` fehl und die Deadline läuft noch,
     nächste Iteration (ein Re-Render/Reload hat den Wert zurückgeschrieben).
   - `ClickAsync` auf `form:has(#search-q) button[type='submit']` ausführen.
   - `WaitForURLAsync("**/recipes/search?q=<title>*")` mit kurzem Timeout (~5 s): Erfolg →
     Schleife verlassen; `TimeoutException` innerhalb der Deadline → nächste Iteration.
   - Nach Ablauf der Deadline ohne Erfolg schlägt der Test fehl; der zuletzt aufgetretene
     Fehler bleibt sichtbar (kein stilles Verschlucken nach Deadline-Ende).
4. *Erfolgsnachweis: `.list-group-item:has-text('{title}')` mit `PageContentTimeoutMilliseconds`.*
   Ergänzt wird eine Feldwert-Assertion `Expect(#search-q).ToHaveValueAsync(title)` nach dem
   URL-Wait — sie beweist zusätzlich, dass das `value`-Emit auf dem Neu-Mount-Pfad das neue
   `q` korrekt ins Feld schreibt (Nutzer-sichtbare `q`-Synchronisation nach GET-Submit).
5. Erklärender Inline-Kommentar analog zu den Kommentaren bei
   `DirectUrl_WithQuery_ShowsResults`/`Pagination_NextPage_ShowsPageTwo`: benennt den
   Prerender/Circuit-Doppellauf als Grund für den Retry um Fill und Submit sowie die
   anwendungsseitige Absicherung des `value`-Renderings.

### Neuer E2E-Nachweis `InPageSearch_TypedValue_SurvivesInteractiveCircuit`

Der Test erzwingt nach dem Befüllen einen vollständigen, nur im interaktiven Circuit
möglichen Render-Zyklus — die Wert-Assertion kann nicht vor dem ersten interaktiven Frame
grün werden:

1. Session + zwei Rezepte mit gemeinsamem GUID-Präfix seeden (z. B. `E2E-Tipp-{guid}-01/02`),
   damit bei `pageSize=1` zwei Ergebnisseiten existieren.
2. `GotoAsync("/recipes/search?q=<prefix>&page=1&pageSize=1")` und auf `.list-group-item`
   warten (`PageContentTimeoutMilliseconds`) — nur als Prerender-Einstiegspunkt, **nicht**
   als Circuit-Barriere.
3. `FillAsync("#search-q", typedValue)` mit einem String, der sich von `q` unterscheidet —
   möglichst früh; der Nachweis ist nicht vom Zeitpunkt des Fills abhängig.
4. Deadline-Retry-Schleife (Budget `InteractionRetryTimeoutMilliseconds`, Muster aus
   `Pagination_NextPage_ShowsPageTwo`): `button[aria-label='Nächste']` klicken, bis
   `Page.Url` `page=2` enthält bzw. das Label „Seite 2 von 2" sichtbar ist (~5-s-Innen-
   Timeout, Vorab-Abbruchcheck). Ein erfolgreicher Klick beweist: Der Circuit war attached,
   der erste interaktive Render war angewendet (Event-Handler verdrahtet) und alle
   Pre-Klick-Frames sind gelandet. `Nav.NavigateTo` führt dabei **keinen** Remount durch —
   dieselbe Instanz bekommt nur neue Parameter (`page`), alle zwischengeschalteten
   Re-Renders laufen auf derselben Komponente.
5. **Content-Barriere**: `Expect(pageObject.Page.Locator(".pagination")).ToContainTextAsync(
   "Seite 2 von 2")` bzw. `GetByText("Seite 2 von 2")` mit `PageContentTimeoutMilliseconds`.
   Der `.pagination`-Block wird nur im `else`-Zweig bei `!isLoading` mit Ergebnissen
   gerendert (`RecipeSearch.razor` Z. 74–86) — das Label existiert daher erst, wenn die
   Suche für `page=2` vollständig abgeschlossen und der letzte interaktive Frame im DOM
   gelandet ist; zugleich kann „Seite 2 von 2" nur durch den interaktiven Render nach der
   In-Circuit-Navigation entstehen (der SSR-Einstieg zeigt „Seite 1 von 2", es gibt keinen
   Dokument-Navigationspfad zu `page=2` in diesem Test). Die Barriere hängt an einem
   **Endzustand**, nicht an einem Einfüge-/Entferne-Übergang: Sie funktioniert gleichermaßen,
   wenn der `.spinner-border` sichtbar toggelt **und** wenn er — bei einem Klick während
   noch laufender Initialsuche — wegen des Superseded-Guards
   (`ReferenceEquals(cts, localCts)`, Z. 150–179) durchgehend im DOM stand und nie neu
   eingefügt wurde.
6. `Expect(#search-q).ToHaveValueAsync(typedValue)` — ohne App-Fix hätte mindestens einer
   der nachgewiesenen Circuit-Frames (Attach-Frames vor dem Klick bzw. die komplette
   `isLoading`-Render-Sequenz der Parameter-Navigation auf derselben Instanz) den `q`-Wert
   per Property-Write zurückgeschrieben; mit Fix bleibt der getippte Wert garantiert stehen.
7. Kommentar: verweist auf den Prerender/Circuit-Doppellauf, die `value`-Emission nur bei
   `q`-Änderung und warum die `page=2`-Content-Barriere (Endzustand „Seite 2 von 2" statt
   DOM-Übergangs-Beobachtung) den abgeschlossenen interaktiven Render nach der Navigation
   beweist — robuster als ein `.spinner-border`-Einfüge-Observer, der beim durchgehend
   sichtbaren Spinner falsch in den Timeout liefe.

Beteiligte Klassen/Komponenten: `RecipeSearchBrowserTests`, `LoadingBarPageObject`
(`Page`, `GotoAsync`), `RecipeSeedHelper`, `LoadingBarBrowserSession`, `RecipeSearch`.

### Neuer E2E-Flusstest `NavbarSearch_WhileOnSearchPage_UpdatesSearchField`

Nutzerfluss „Navbar-Suche, während `/recipes/search` bereits angezeigt wird" (Akzeptanz-
szenario 3, Neu-Mount-Pfad — verifiziert statischer GET-Submit der statischen Navbar):

1. Session + Rezept mit GUID-Titel `titleB` seeden (Treffer für den neuen Begriff).
2. `GotoAsync("/recipes/search?q=<fremder-begriff>")`; auf `.alert-secondary` warten
   (Prerender-Einstieg, kein Treffer für den Fremdbegriff).
3. `pageObject.SubmitNavigationSearchAsync(titleB)` — füllt `#nav-search`, klickt den
   Navbar-Submit. Da `MainLayout` außerhalb der interaktiven Boundary liegt, läuft der
   Submit als natives GET-Formular → neue Dokument-Antwort → Neu-Mount.
4. `WaitForURLAsync("**/recipes/search?q=<titleB>*")` mit `PageContentTimeoutMilliseconds`.
5. `Expect(#search-q).ToHaveValueAsync(titleB)` — das Eingabefeld zeigt den neuen
   `q`-Wert (Emit-Pfad auf dem Neu-Mount).
6. `.list-group-item:has-text('{titleB}')` als Trefferlisten-Nachweis.

### bUnit-Nachweis des Gleich-Instanz-`q`-Wechsels in `RecipeSearchComponentTests`

Der einzige deterministische Weg, `OnParametersSet` mit geändertem `q` auf **derselben**
Komponenteninstanz auszulösen (verifiziert: kein UI-Pfad erreicht dies, siehe
Designentscheidungen). Beide Tests nutzen die vorhandenen Helfer `Authorize()` und
`RegisterRecipeService()` (Mock liefert leeres `SearchResult`):

1. `SearchField_WhenQueryParameterChanges_EmitsUpdatedValue`:
   `RenderComponent<RecipeSearch>(p => p.Add(x => x.q, "a"))` →
   `cut.Find("#search-q").GetAttribute("value")` ist `"a"`; dann
   `cut.SetParametersAndRender(p => p.Add(x => x.q, "b"))` → `GetAttribute("value")` ist
   `"b"` — `OnParametersSet` auf derselben Instanz hat die `q`-Änderung erkannt und
   `_emitSearchValue` gesetzt.
2. `SearchField_WhenQueryParameterUnchanged_SuppressesValueAttribute`:
   `RenderComponent<RecipeSearch>(p => p.Add(x => x.q, "a"))` → Attribut `"a"`;
   `cut.SetParametersAndRender(p => p.Add(x => x.q, "a"))` (gleicher Wert, `OnParametersSet`
   läuft erneut) → `GetAttribute("value")` ist `null` — das Attribut wurde im Diff entfernt
   und würde die DOM-Property im Browser unangetastet lassen. Genau dieser Suppressions-Pfad
   verhindert das Zurückschreiben getippter Werte.

Beteiligte Klassen/Komponenten: `RecipeSearchComponentTests` (bUnit `TestContext`,
`PathBaseNavigationManager`, Mock-`IRecipeService`), `RecipeSearch`.

## Neue Klassen

Keine.

## Änderungen an bestehenden Klassen

### `RecipeSearch` (Razor-Komponente, `Rezepte.Web/Components/Pages/RecipeSearch.razor`)

- **Neue Eigenschaften:** `PrerenderedSearchQuery` (`string?`, `public`, mit
  `[PersistentState]`-Attribut, `Microsoft.AspNetCore.Components`) — trägt das beim
  Prerender gerenderte `q` in die interaktive Instanz; wird in `OnParametersSet` auf `q`
  gesetzt, damit der Wert beim Prerender persistiert wird.
- **Neue Felder:**
  - `_syncedSearchQuery` (`string?`) — das `q`, dessen Wert das Eingabefeld bereits
    repräsentiert bzw. als synchronisiert gilt.
  - `_searchFieldPrimed` (`bool`) — ob `_syncedSearchQuery` bereits aus
    `PrerenderedSearchQuery` initialisiert wurde.
  - `_emitSearchValue` (`bool`) — ob der aktuelle Frame das `value`-Attribut emittieren soll.
- **Neue Methoden:** `OnParametersSet()` (synchrones Override) — Priming von
  `_syncedSearchQuery` beim ersten Aufruf, Berechnung von `_emitSearchValue`
  (`!string.Equals(q, _syncedSearchQuery, StringComparison.Ordinal)`), Nachziehen von
  `_syncedSearchQuery` und `PrerenderedSearchQuery` auf `q`. Läuft vor dem bestehenden
  `OnParametersSetAsync`, das unverändert `RunSearchAsync` aufruft.
- **Geändertes Markup:** `value="@(q ?? string.Empty)"` (Z. 26) →
  `value="@(_emitSearchValue ? (q ?? string.Empty) : null)"`; der Blockkommentar zum
  „plain GET form"-Design (Z. 19–23) wird um einen Satz ergänzt, dass `value` nur bei
  `q`-Änderung emittiert wird, damit der getippte Live-Wert nie zurückgeschrieben wird.
- **Keine Änderung an:** `OnParametersSetAsync`, `RunSearchAsync`, `ChangePage`, `Dispose`,
  allen Zustandsblöcken und der Formularstruktur (kein `@bind`, kein `@oninput`, kein
  `action`-Attribut).

### `RecipeSearchBrowserTests` (Testklasse, `Rezepte.Tests.Browser/RecipeSearchBrowserTests.cs`)

- **Geänderte Methoden:** `InPageSearch_Submit_UpdatesResultList` — die bisherige lineare
  Sequenz `FillAsync` → `Expect(...).ToHaveValueAsync` → `ClickAsync` → `WaitForURLAsync`
  (Z. 165–175) wird in eine Deadline-Retry-Schleife überführt (Budget
  `InteractionRetryTimeoutMilliseconds`, innere Timeouts ~5 s, Vorab-Prüfung auf die
  Ziel-URL `q=<title>`). Assertion-Timeouts (`PlaywrightException` aus `Expect`,
  `TimeoutException` aus `WaitForURLAsync`) werden nur innerhalb der Deadline aufgefangen.
  Der Trefferlisten-Nachweis (Z. 177–179) bleibt; ergänzt wird die Feldwert-Assertion
  `Expect(#search-q).ToHaveValueAsync(title)` nach dem URL-Wait (Emit-Nachweis auf dem
  Neu-Mount-Pfad). Ergänzt wird ein Kommentar, der den Prerender/Circuit-Race als
  Retry-Grund benennt.
- **Neue Methoden:**
  - `InPageSearch_TypedValue_SurvivesInteractiveCircuit` — E2E-Nachweis der
    anwendungsseitigen Absicherung mit der interaktiv erzwungenen Barriere
    (Paginierungs-Klick + `page=2`-Content-Barriere „Seite 2 von 2"; siehe
    Programmabläufe). Komplett mit vorhandenen Playwright-Primitiven umsetzbar
    (`Expect`/`GetByText`/`WaitForAsync` wie in `Pagination_NextPage_ShowsPageTwo`) —
    keine Page-Object-Erweiterung, kein `MutationObserver`/JS-Interop nötig.
  - `NavbarSearch_WhileOnSearchPage_UpdatesSearchField` — E2E-Nutzerfluss der
    `q`-Synchronisation per Navbar-Submit auf der Suchseite (siehe Programmabläufe).
- **Konstanten:** `PageContentTimeoutMilliseconds` (20 s) und
  `InteractionRetryTimeoutMilliseconds` (15 s) werden wiederverwendet und nicht geändert.
- **Keine neuen Events oder Event-Handler.**

### `RecipeSearchComponentTests` (bUnit-Testklasse, `Rezepte.Tests/Components/RecipeSearchComponentTests.cs`)

- **Neue Methoden:** `SearchField_WhenQueryParameterChanges_EmitsUpdatedValue` und
  `SearchField_WhenQueryParameterUnchanged_SuppressesValueAttribute` — Gleich-Instanz-
  `OnParametersSet`-Nachweis per `SetParametersAndRender` (siehe Programmabläufe).
  Vorhandene Helfer (`Authorize`, `RegisterRecipeService`, `SearchAsyncSetup`) werden
  wiederverwendet; der Service-Mock liefert ein leeres `SearchResult`.
- **Keine Änderung an** den sechs bestehenden Tests — sie laufen als Regression-Check des
  App-Fixes unverändert mit (Volltestlauf verifiziert).

## Datenbankmigrationen

Keine.

## Validierungsregeln

Keine.

## Konfigurationsänderungen

Keine. Timeout- und Retry-Budgets bleiben Testkonstanten in `RecipeSearchBrowserTests`.
`[PersistentState]` benötigt keine Registrierung — der Mechanismus steht im Blazor-Web-App-
Modell (`AddRazorComponents`/`MapRazorComponents<App>.AddInteractiveServerRenderMode`,
`Program.cs` Z. 103–104) unter .NET 10 standardmäßig bereit.

## Seiteneffekte und Risiken

- **Verhalten von `#search-q` für echte Nutzer:** Getippter Text wird nie mehr durch
  Re-Renders bei unverändertem `q` zurückgeschrieben — auch nicht beim Circuit-Start und
  nicht durch `isLoading`-Toggles mitten im Tippen (Verbesserung). Nach `q`-Änderungen
  (GET-Submit des In-Page- oder Navbar-Formulars → Neu-Mount; In-Circuit-`Nav.NavigateTo`)
  wird das Feld weiterhin synchronisiert. Bei Pagination bleibt ein ungesendeter getippter
  Begriff erhalten statt zurückgesetzt zu werden — UX-neutrales bis positives Verhalten.
- **DOM-Attribut `value`:** Entfällt in interaktiven Frames ohne `q`-Änderung (beim ersten
  interaktiven Frame ist der Frame ein Insert-/Attach-Frame ohne `value`-Node — mechanisch
  kein Diff gegen den Prerender, Effekt identisch: kein Property-Write); die DOM-Property
  `input.value` bleibt unberührt — der GET-Submit liest den Live-Wert, nicht das Attribut
  (genau der dokumentierte Grund für das „plain GET form"-Design). Für `q == null` entfällt
  das leere `value=""`-Attribut — optisch identisch, keine Testauswirkung.
- **bUnit-Komponententests (`RecipeSearchComponentTests`):** Kein Persistent-State-Kontext
  → `value` wird bei `q != null` emittiert, sonst nicht; die sechs bestehenden Tests
  assertieren nicht auf das Attribut — keine Anpassung erwartet, Volltestlauf verifiziert.
  Zwei neue Tests decken Emit-on-Change und Suppression auf derselben Instanz ab.
- **`[PersistentState]`-Restore-Semantik bei Remounts:** Der persistierte Wert wird bei
  echten Remounts im selben Circuit (Enhanced Navigation mit Neu-Instantiierung) erneut
  wiederhergestellt (`RestoreBehavior` = „always restores"). In-Circuit-`Nav.NavigateTo`
  auf dieselbe Route ist dagegen **kein** Remount — dieselbe Instanz behält ihren Zustand
  und bekommt nur neue Parameter, der Restore-Mechanismus greift dort nicht. Zu verifizieren
  bei der Implementierung (Debug-Lauf mit zwei aufeinanderfolgenden Enhanced Navigationen):
  ob der wiederhergestellte Wert der **ursprüngliche** Prerender-Wert bleibt oder vom
  Enhanced-Nav-Prerender aktualisiert wird. Betroffen ist nur der schmale Edge-Case
  „Remount mit bereits beschriebenem (dirty) Feld": Ein veraltet wiederhergestellter Wert
  könnte das Priming falsch setzen (Feld bliebe bis zur nächsten `q`-Änderung nicht
  synchronisiert bzw. getippter Text bliebe statt `q` stehen); die gewählte Assertion
  (`value` nur bei `q`-Abweichung) ist gegenüber beiden Restore-Varianten fehlerfrei für
  den Issue-#185-Fall (gleiches `q` → Suppression).
- **Neuer E2E-Test `InPageSearch_TypedValue_SurvivesInteractiveCircuit`:** Hängt an der
  Invariante, dass der `.pagination`-Block samt „Seite 2 von 2"-Label nur bei `!isLoading`
  mit Ergebnissen gerendert wird (`RecipeSearch.razor` Z. 74–86) und dass `page=2` in diesem
  Test nur per `@onclick`-In-Circuit-Navigation erreichbar ist (kein SSR-/Dokument-Pfad;
  ein verschluckter Pre-Attach-Klick erreicht `page=2` nie). Die Barriere wartet auf einen
  Endzustand statt auf einen DOM-Übergang und ist damit robust gegen beide Lade-Abläufe:
  sichtbar getoggelter `.spinner-border` ebenso wie durchgehend sichtbarer Spinner bei
  einem Klick während der noch laufenden Initialsuche (Superseded-Guard, Z. 150–179).
  Sollte das Markup je so umgebaut werden, dass Paginierung/Ladezustand anders verdrahtet
  sind, müsste die Barriere angepasst werden; die Umsetzung benötigt keinen JS-Interop und
  verlängert den Test nur um einen Paginierungs-Roundtrip (~Sekundenbereich).
- **CI-Laufzeit:** Im Regelfall läuft die erste Retry-Iteration durch; bei einem
  tatsächlichen Rest-Race kostet jede Wiederholung bis zu ~10 s zusätzlich innerhalb des
  bestehenden 15-s-Budgets — vernachlässigbar. Zwei neue Browser-Tests addieren jeweils
  wenige Sekunden (Session-Setup + ein bis zwei Navigationen).
- **Residualrisiko:** Ein echter Produktivdefekt (Submit funktioniert nie) wird erst nach
  Ablauf der 15-s-Deadline sichtbar — akzeptabel, da `WaitForURLAsync` nach wie vor
  zwischen „verschluckt" und „fehlgeschlagen" unterscheidet.
- **Nicht adressierte Beobachtung:** Der `FillAsync`-Prerender-Race ist prinzipiell auch in
  `LoadingBarPageObject.LoginAsync` (`#username`/`#password`) und
  `SubmitNavigationSearchAsync` (`#nav-search`) möglich; dort ist kein Fehlschlag
  nachgewiesen — bleibt gemäß Scope-Ausschluss 4 unverändert (die `#nav-search`-Fill profitiert
  indirekt, weil `MainLayout` ohnehin `@bind-Value` nutzt).
- **Unabhängiger Vorbefund:** `RegisterTests.Register_WithDemoData_CreatesDemoData` schlug
  im Bestands-Basislauf fehl (Demo-Daten-Timeout, kein Bezug zum Suchpfad) — Anwender-
  Entscheidung 2026-11-15: als eigenes Issue tracken, nicht Teil dieses Scopes; ein
  Volltestlauf kann wegen dieses Tests rot sein und ist dann zu dokumentieren.

## Umsetzungsreihenfolge

1. **Anwendungsseitige Absicherung in `RecipeSearch.razor` einbauen**
   - Voraussetzungen: Keine — `[PersistentState]` steht unter .NET 10 /
     `Microsoft.AspNetCore.Components` zur Verfügung (`@using` bereits vorhanden, Z. 5);
     das Blazor-Web-App-Setup mit `AddInteractiveServerRenderMode` ist vorhanden.
   - Beschreibung: `[PersistentState]`-Property `PrerenderedSearchQuery`, die Felder
     `_syncedSearchQuery`/`_searchFieldPrimed`/`_emitSearchValue` und das synchrone
     `OnParametersSet()`-Override ergänzen; `value` im Markup konditionieren
     (`_emitSearchValue ? (q ?? string.Empty) : null`); Formular-Kommentar ergänzen.
     Dabei kurz die Restore-Semantik des Persistent-State-Mechanismus bei Remounts
     verifizieren (siehe Seiteneffekte) — z. B. per Debug-Lauf mit zwei
     aufeinanderfolgenden Enhanced Navigationen.

2. **bUnit-Nachweise in `RecipeSearchComponentTests` ergänzen**
   - Voraussetzungen: Schritt 1 umgesetzt; vorhandene Helfer `Authorize`/
     `RegisterRecipeService`/`SearchAsyncSetup`; bUnit 1.40 (`SetParametersAndRender`).
   - Beschreibung: `SearchField_WhenQueryParameterChanges_EmitsUpdatedValue`
     (`q` „a"→„b" auf derselben Instanz → `value` = `"b"`) und
     `SearchField_WhenQueryParameterUnchanged_SuppressesValueAttribute` (erneutes
     `SetParametersAndRender` mit gleichem `q` → `value`-Attribut entfernt).

3. **Retry-Schleife in `InPageSearch_Submit_UpdatesResultList` einbauen**
   - Voraussetzungen: Schritt 1 umgesetzt; bestehende Konstanten
     `InteractionRetryTimeoutMilliseconds`, `PageContentTimeoutMilliseconds`;
     Referenzmuster in `Pagination_NextPage_ShowsPageTwo` (Z. 118–142, selbes File).
   - Beschreibung: Schleife um `FillAsync` → `Expect(...).ToHaveValueAsync` (~5 s) →
     `ClickAsync` → `WaitForURLAsync` (~5 s) mit Deadline
     `InteractionRetryTimeoutMilliseconds` und Vorab-Abbruchcheck auf `q=<title>` in
     `Page.Url`; Feldwert-Assertion `Expect(#search-q).ToHaveValueAsync(title)` nach dem
     URL-Wait ergänzen; erklärender Kommentar zum Prerender/Circuit-Race.

4. **Neue E2E-Tests `InPageSearch_TypedValue_SurvivesInteractiveCircuit` und
   `NavbarSearch_WhileOnSearchPage_UpdatesSearchField` hinzufügen**
   - Voraussetzungen: Schritt 1 umgesetzt (die Tests sind Funktionsnachweis bzw.
     Nutzerfluss-Nachweis des App-Fixes); Seed-/Session-Infrastruktur vorhanden;
     zwei Rezepte für die Paginierungs-Barriere (`pageSize=1` → zwei Seiten).
   - Beschreibung `InPageSearch_TypedValue_SurvivesInteractiveCircuit`:
     `GotoAsync` mit Präfix-`q` + `pageSize=1` → `.list-group-item`-Warte →
     `FillAsync("#search-q", typed)` →
     Retry-Klick auf `button[aria-label='Nächste']` bis `page=2` in der URL bzw. das Label
     sichtbar ist →
     Content-Barriere `Expect(.pagination).ToContainTextAsync("Seite 2 von 2")` bzw.
     `GetByText("Seite 2 von 2")` (nur bei `!isLoading` gerendert → beweist den
     abgeschlossenen interaktiven Post-Navigations-Render, unabhängig davon, ob der
     `.spinner-border` getoggelt hat oder durchgehend sichtbar war) →
     `Expect(#search-q).ToHaveValueAsync(typed)`; Kommentar zum Prerender/Circuit-
     Doppellauf und zur Endzustand-Barriere.
   - Beschreibung `NavbarSearch_WhileOnSearchPage_UpdatesSearchField`: `GotoAsync` mit
     Fremd-`q` → `.alert-secondary` → `SubmitNavigationSearchAsync(titleB)` →
     `WaitForURLAsync` → `Expect(#search-q).ToHaveValueAsync(titleB)` →
     `.list-group-item:has-text('{titleB}')`.

5. **Lokale Verifikation per Wiederholungsläufen** (Anwender-Festlegung 2026-11-15)
   - Voraussetzungen: Schritte 1–4 umgesetzt; Release-Build inkl.
     `PublishRezepteWebForBrowserTests`-Publish und installiertes Playwright-Chromium
     (wie in `inventory/tests.md` dokumentiert: `dotnet build Rezepte.sln -c Release` +
     `playwright.ps1 install chromium`).
   - Beschreibung: `dotnet test Rezepte.Tests.Browser/Rezepte.Tests.Browser.csproj -c Release --no-build --filter "FullyQualifiedName~RecipeSearchBrowserTests"` mindestens 10× hintereinander ausführen; alle Läufe müssen grün sein. Der Flake ist lastabhängig und lokal nicht reproduzierbar — die Wiederholungsläufe prüfen daher primär, dass die Umstellung korrekt implementiert ist und keine neuen Fehler einführt.

6. **Vollständiger Testlauf**
   - Voraussetzungen: Schritte 1–5 umgesetzt und grün.
   - Beschreibung: `dotnet test Rezepte.sln --configuration Release --no-build` —
     `Rezepte.Tests` (inkl. `RecipeSearchComponentTests` mit den neuen
     Gleich-Instanz-Tests als Regression-Check des App-Fixes) und
     `Rezepte.Tests.Browser` müssen grün sein; ein eventueller erneuter
     `Register_WithDemoData_CreatesDemoData`-Fehlschlag ist als bekannter, unabhängiger
     Vorbefund zu dokumentieren, nicht als Blocker dieser Änderung.

7. **Pre-Release-Verifikation** (Anwender-Festlegung 2026-11-15)
   - Voraussetzungen: Schritte 1–6 gemergt auf `staging`.
   - Beschreibung: Ein grüner `build & test`-Lauf im Pre-Release-Workflow gilt als
     Mindestnachweis, dass der Flake behoben ist. Tritt das Fehlerbild danach erneut auf,
     ist die Ursachenanalyse wieder zu öffnen.

## Tests

### Neue Tests

| Test / Hilfsmethode | Testklasse | Was wird geprüft / bereitgestellt? |
|--------------------|------------|-------------------------------------|
| `InPageSearch_TypedValue_SurvivesInteractiveCircuit` | `RecipeSearchBrowserTests` | Primärer Funktionsnachweis des App-Fixes: ein per `FillAsync` gesetzter Wert in `#search-q` bleibt nach einem **erzwungenen** interaktiven Render-Zyklus stehen — Barriere = erfolgreicher `@onclick`-Paginierungs-Klick (`page=2`, nur mit attachtem Circuit möglich) plus „Seite 2 von 2"-Content-Barriere: Das Label wird nur bei `!isLoading` mit Ergebnissen gerendert und kann nur vom interaktiven Render nach der In-Circuit-Navigation erzeugt werden (Endzustand statt DOM-Übergang — robust gegen durchgehend sichtbaren `.spinner-border`) |
| `NavbarSearch_WhileOnSearchPage_UpdatesSearchField` | `RecipeSearchBrowserTests` | Nutzerfluss-Nachweis der `q`-Synchronisation: Navbar-Submit, während `/recipes/search` bereits angezeigt ist → URL `q=<neu>`, `#search-q` zeigt den neuen Begriff, Trefferliste aktualisiert (Neu-Mount-Pfad) |
| `SearchField_WhenQueryParameterChanges_EmitsUpdatedValue` | `RecipeSearchComponentTests` (bUnit) | Gleich-Instanz-`OnParametersSet` mit geändertem `q` (`SetParametersAndRender`) → `value`-Attribut emittiert den neuen Wert (`_emitSearchValue`-Pfad) |
| `SearchField_WhenQueryParameterUnchanged_SuppressesValueAttribute` | `RecipeSearchComponentTests` (bUnit) | Gleich-Instanz-`OnParametersSet` mit **unverändertem** `q` → `value`-Attribut wird im Diff entfernt (Suppressions-Pfad — im Browser-DOM nicht von gleichem Write unterscheidbar, daher nur hier deterministisch nachweisbar) |

### Betroffene bestehende Tests

| Test / Testklasse | Grund der Anpassung |
|-------------------|---------------------|
| `RecipeSearchBrowserTests.InPageSearch_Submit_UpdatesResultList` | Flaky durch Prerender/Circuit-Race: `FillAsync`-Wert konnte von einem Circuit-Re-Render des `value`-Attributs überschrieben werden; Umbau auf Deadline-Retry um Fill→Wert-Check→Submit→URL-Wait (Rest-Race-Absicherung zusätzlich zum App-Fix); ergänzte Feldwert-Assertion nach dem URL-Wait |
| `RecipeSearchComponentTests` (6 bestehende bUnit-Tests) | Keine Anpassung vorgesehen, aber betroffen zu prüfen: `value` wird bei `q == null` nicht mehr als leeres Attribut gerendert; zwei neue Tests hinzugefügt; im Volltestlauf verifizieren |

### E2E-Tests (primärer Funktionsnachweis)

| Priorität | Szenario | Testdatei / Testklasse | Abgedecktes Akzeptanzkriterium | Warum E2E nötig ist |
|-----------|----------|------------------------|-------------------------------|-------------------|
| Pflicht | In-Page-Suche: `#search-q`-Begriff ändern und GET-Formular absenden aktualisiert die Trefferliste | `Rezepte.Tests.Browser/RecipeSearchBrowserTests.cs` / `InPageSearch_Submit_UpdatesResultList` (angepasst) | Der per `FillAsync` gesetzte Begriff bleibt bis zum Submit erhalten; Navigation zu `/recipes/search?q=<neuer Begriff>`; Feld und Trefferliste zeigen den geseedeten Begriff/Rezept | Der Fluss ist nur im echten Browser über den Prerender→Circuit-Übergang nachweisbar; bUnit-Tests (`RecipeSearchComponentTests`) bilden weder Prerendering noch das DOM-`value`-Verhalten ab |
| Pflicht | Getippter Wert in `#search-q` überlebt den Prerender→Interactive-Übergang und einen vollständigen interaktiven `isLoading`-Suchzyklus | `Rezepte.Tests.Browser/RecipeSearchBrowserTests.cs` / `InPageSearch_TypedValue_SurvivesInteractiveCircuit` (neu) | Ein getippter Begriff bleibt nach bewiesenermaßen abgeschlossenem interaktivem Post-Navigations-Render-Zyklus im Feld (Endzustand „Seite 2 von 2" erreicht); `value` wird nur bei `q`-Änderung geschrieben | Der `[PersistentState]`-Mechanismus und das Browser-DOM-Verhalten (Property vs. Attribut) sind nur in einer echten Prerender→Circuit-Sequenz verifizierbar; bUnit kann weder Prerender-Kontext noch DOM-Property-Writes herstellen |
| Pflicht | Navbar-Suche auf der bereits angezeigten Suchseite synchronisiert `#search-q` auf das neue `q` | `Rezepte.Tests.Browser/RecipeSearchBrowserTests.cs` / `NavbarSearch_WhileOnSearchPage_UpdatesSearchField` (neu) | Feld zeigt den neuen `q`-Wert nach Navbar-Submit; URL und Trefferliste aktualisieren sich | Nutzer-sichtbarer Sync-Pfad (Neu-Mount per statischem GET-Submit); der Gleich-Instanz-Pfad selbst ist via UI nicht erreichbar und wird per bUnit abgedeckt |

Angepasste bestehende E2E-Tests:

| Test / Testklasse | Grund der Anpassung |
|-------------------|---------------------|
| `InPageSearch_Submit_UpdatesResultList` | Deadline-Retry um die Formularinteraktion plus Feldwert-Assertion (siehe oben) |

## Akzeptanzszenarien

| # | Szenario | Auslöser | Ausgangszustand | Schritte | Sichtbare Wirkung | Nachweis |
|---|----------|----------|------------------|----------|-------------------|----------|
| 1 | Happy Path: stabil grüner Testdurchlauf | `dotnet test` mit Filter `FullyQualifiedName~RecipeSearchBrowserTests` lokal bzw. Job `build & test` im Pre-Release-Workflow | Testrezept existiert noch nicht (wird per `RecipeSeedHelper.SeedRecipesAsync` erzeugt); interaktiver Circuit beim Befüllen bereits aktiv **oder** noch nicht aktiv (Race beliebig) | Session + Seed → `GotoAsync` mit Fremd-`q` → `.alert-secondary` → Retry-Schleife: `FillAsync`, Wert-Check, Submit-Klick, URL-Wait | URL wechselt zu `/recipes/search?q=<title>`; `.list-group-item` mit dem geseedeten Titel erscheint; `#search-q` zeigt den Titel | Test grün in allen ≥10 Wiederholungsläufen und im nächsten Pre-Release-Lauf |
| 2 | Nutzerfluss: getippter Begriff überlebt den Circuit-Start und interaktive Re-Renders | Nutzer tippt in `#search-q`, während `blazor.web.js` den interaktiven Circuit verbindet bzw. eine Circuit-Navigation läuft | Seite ist per SSR sichtbar (Trefferliste), Circuit verbindet sich; `RunSearchAsync` läuft im Circuit erneut mit `isLoading`-Toggle | Tippen → erfolgreicher Paginierungs-Klick (nur mit Circuit möglich) → interaktiver Render nach der In-Circuit-Navigation abgeschlossen („Seite 2 von 2" sichtbar — nur bei `!isLoading` mit Ergebnissen gerendert) | Der getippte Text steht nach allen nachgewiesenen Circuit-Frames noch im Feld; erst ein tatsächliches `q`-Update überschreibt ihn | E2E-Test `InPageSearch_TypedValue_SurvivesInteractiveCircuit` |
| 3 | `q`-Sync bleibt erhalten bei `q`-Änderung | (a) Nutzer submittet die Navbar-Suche, während `/recipes/search` bereits angezeigt ist (Neu-Mount per statischem GET-Submit); (b) `OnParametersSet`-Re-Run mit geändertem `q` auf derselben Instanz (Gleich-Instanz-Pfad — in der App nur via In-Circuit-`Nav.NavigateTo` erreichbar, aktuell kein UI-Auslöser) | (a) Seite `/recipes/search?q=A` geladen; (b) Instanz mit synchronisiertem `q=A` | (a) `#nav-search` füllen, Submit → Dokument-Navigation → `#search-q` zeigt B; (b) `SetParametersAndRender` mit `q=B` → `value` emittiert; `SetParametersAndRender` mit unverändertem `q` → `value` entfällt | Feld zeigt den neuen `q`-Wert auf beiden Pfaden; Suppression bei gleichem `q` | E2E-Test `NavbarSearch_WhileOnSearchPage_UpdatesSearchField` + bestehender `NavbarSearch_ShowsSeededRecipe` (Neu-Mount) **und** bUnit-Tests `SearchField_WhenQueryParameterChanges_EmitsUpdatedValue` / `SearchField_WhenQueryParameterUnchanged_SuppressesValueAttribute` (Gleich-Instanz) |
| 4 | Echter Defekt bleibt sichtbar | Dito wie 1 | Submit oder Suche ist tatsächlich defekt (kein Timing-Problem) | Alle Retry-Iterationen bis zur Deadline schlagen fehl | Test schlägt nach `InteractionRetryTimeoutMilliseconds` fehl — der Defekt wird nicht durch Retries überdeckt | Review des Fehlschlag-Outputs (letzter Fehler wird propagiert) |

## Offene Punkte

Keine. Die zuvor offenen Punkte wurden vom Anwender (2026-11-15) entschieden und sind
eingearbeitet: Verifikation über ≥10 lokale Wiederholungsläufe plus ein grüner
Pre-Release-Lauf (Schritte 5 und 7); der unabhängige Vorbefund
`RegisterTests.Register_WithDemoData_CreatesDemoData` wird als eigenes Issue getrackt und
ist nicht Teil dieses Scopes. Die `[PersistentState]`-Restore-Semantik bei Remounts ist ein
verbleibendes Implementierungs-Verifikationsitem (Schritt 1), kein offener Entscheidpunkt.
