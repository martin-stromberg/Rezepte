# Plan-Gegenprüfung

## Ergebnis

**Status:** Plan lückenhaft

## Abgleich Akzeptanzkriterien

| Akzeptanzkriterium | Umsetzung im Plan | Testnachweis im Plan | Status |
|--------------------|-------------------|----------------------|--------|
| `InPageSearch_Submit_UpdatesResultList` läuft im Pre-Release-Workflow stabil grün (FillAsync-Wert bleibt bis zum Submit erhalten bzw. wird wiederholt gesetzt, Submit navigiert zu `?q=<neu>`, Trefferliste zeigt geseedetes Rezept) | App-Fix „Emit-on-q-Change" (`[PersistentState]`-Priming + konditionierte `value`-Emission) plus Deadline-Retry-Schleife um Fill→Wert-Check→Submit→URL-Wait (Umsetzungsreihenfolge Schritte 1–2) | Angepasster E2E-Test + ≥10 lokale Wiederholungsläufe + ein grüner Pre-Release-Lauf (Schritte 4, 6) | Abgedeckt |
| Getippter Wert in `#search-q` überlebt Prerender→Circuit-Übergang und `isLoading`-Re-Renders (planseitig ergänzte erwartete Wirkung des App-Fixes) | `value` nur bei `q`-Änderung emittieren; nach Circuit-Start ist kein `value`-Frame mehr im Render (technisch korrekt, verifiziert) | Neuer E2E-Test `InPageSearch_TypedValue_SurvivesInteractiveCircuit` — **aber wirksamkeitslos wie spezifiziert**: Warte auf `.list-group-item` trifft bereits den SSR-Prerender-Inhalt, `Expect(ToHaveValue)` kann vor dem Circuit-Render grün werden | Lücke (Testnachweis) |
| Feld-Synchronisation bei tatsächlicher `q`-Änderung (Parameter-Navigation, z. B. Navbar-Suche auf der Suchseite) bleibt erhalten | Emit-on-Change-Pfad in `OnParametersSet` (`q != _syncedSearchQuery` → `value` emittieren → DOM-Property-Write) | Akzeptanzszenario 3 benennt `NavbarSearch_ShowsSeededRecipe` + Code-Review — **Nachweis ungültig**: der Test navigiert von einer anderen Seite zur Suchseite (neue Instanz), nicht per `Nav.NavigateTo` auf derselben Instanz; der Gleich-Instanz-Pfad ist ungetestet | Lücke (Testnachweis) |
| Kein Umbau auf `@bind`-Zwei-Wege-Bindung / „plain GET form" bleibt erhalten | Konditionierte `value`-Emission verändert nur die Renderzeit-Entscheidung; kein `@bind`, kein `@oninput`, kein `action`-Attribut | Vorhandene E2E-Tests laufen weiter | Abgedeckt |
| Keine Änderung an Suchlogik, CI-Workflow, übrigen Browser-Tests | Scope-Ausschlüsse 1, 3, 4 (alle `bestätigt`) | — | Abgedeckt |
| Testdaten über `RecipeSeedHelper.SeedRecipesAsync` mit GUID-Titel | Wiederverwendung unverändert | In beiden Tests enthalten | Abgedeckt |

## Fehlende oder unvollständige Testanforderungen

- [ ] `InPageSearch_TypedValue_SurvivesInteractiveCircuit` weist die behauptete Wirkung nicht nach: Die geplante Barriere „Warten auf `.list-group-item`" (plan.md Z. 130–133) ist inhaltlich falsch begründet — die Ergebnisliste steht bereits im SSR-Prerender-HTML (`OnParametersSetAsync` awaited `RunSearchAsync` vollständig, `RecipeSearch.razor` Z. 112–115; bestätigt durch den Kommentar „the prerendered result list is briefly hidden again while the interactive circuit repeats the same search" in `RecipeSearchBrowserTests.cs` Z. 66–68). Die Warte kehrt sofort zurück und `Expect(#search-q).ToHaveValueAsync` kann beim ersten Poll — noch vor dem interaktiven Re-Render — erfolgreich sein. Der Test würde damit im Regelfall auch **ohne** App-Fix grün und kann den Prerender/Circuit-Fix nicht verifizieren. Es fehlt eine Barriere, die den interaktiven Re-Render-/`isLoading`-Zyklus nachweisbar abgewartet hat (z. B. Erscheinen+Verschwinden des `.spinner-border`-Zustands oder ein anderes Signal für den Circuit-Render).
- [ ] Für die Gleich-Instanz-`q`-Synchronisation (Navbar-Suche mit geändertem `q`, während `/recipes/search` bereits angezeigt ist — der einzige Pfad, auf dem `_emitSearchValue = true` interaktiv wirksam wird) gibt es keinen Testnachweis: `NavbarSearch_ShowsSeededRecipe` startet die Suche von einer anderen Route und mountet `RecipeSearch` neu — der `OnParametersSet`-Re-Run auf derselben Instanz bleibt ungetestet (nur „Code-Review"). Ein E2E- bzw. bUnit-Test auf `#search-q`-Wert nach `Nav.NavigateTo` mit geändertem `q` auf derselben Seite fehlt.

## E2E-Abdeckung

| Benutzerfluss / Akzeptanzkriterium | Geplanter E2E-Test | Status |
|------------------------------------|--------------------|--------|
| In-Page-Suche: Begriff ändern + GET-Submit aktualisiert Trefferliste (Flaky-Test stabil grün) | `InPageSearch_Submit_UpdatesResultList` mit Deadline-Retry (angepasst) | Abgedeckt |
| Getippter Wert überlebt Prerender→Circuit-Übergang und `isLoading`-Re-Renders | `InPageSearch_TypedValue_SurvivesInteractiveCircuit` (neu) | Lücke — Barriere (`.list-group-item`-Warte) liegt im Prerender, Nachweis greift nicht |
| Feld-Sync bei `q`-Änderung auf derselben Komponenteninstanz (Navbar-Suche auf der Suchseite) | `NavbarSearch_ShowsSeededRecipe` + Code-Review | Lücke — genannter Test deckt nur den Neu-Mount-Pfad ab, nicht den Parameter-Update-Pfad |

## Fehlende oder unvollständige Planbestandteile

- [ ] Der E2E-Test zum App-Fix benötigt eine wirksame „interaktiver Re-Render abgeschlossen"-Barriere vor der Wert-Assertion; die spezifizierte `.list-group-item`-Warte erfüllt diese Rolle nicht (siehe oben). Entweder Barriere korrigieren oder die behauptete Beweiskraft des Tests einschränken.
- [ ] Der Nachweis für Akzeptanzszenario 3 (Feld-Sync bei gleichseitiger `q`-Navigation) ist ungültig benannt: `NavbarSearch_ShowsSeededRecipe` testet den Neu-Mount, nicht den `OnParametersSet`-Re-Run derselben Instanz. Konkreter Nachweis für den Gleich-Instanz-Pfad ergänzen (z. B. E2E: auf `/recipes/search?q=a` per Navbar-Formular `b` submitten und `#search-q`-Wert `b` assertieren).

## Hinweise

- Technisch korrekt verifiziert: `[PersistentState]` existiert in .NET 10 (`Microsoft.AspNetCore.Components`, erbt `CascadingParameterAttributeBase`, Ref-Assembly 10.0.14 geprüft); die Wiederherstellung erfolgt über `SupplyValueFromPersistentComponentStateProvider` vor `OnInitialized`/erstem `OnParametersSet` — die Priming-Reihenfolge des Plans stimmt.
- `value="@(… : null)"`: Ein `null`-Attributwert wird nicht emittiert bzw. im Diff entfernt und lässt die DOM-Property `input.value` unberührt; bei Emission setzt Blazor `value` als DOM-Property (auch für „dirty" Inputs) — die `q`-Sync-Richtung funktioniert wie geplant. Die Formulierung „beim ersten interaktiven Frame als Diff-Entfernung" (plan.md Z. 213) ist mechanisch unpräzise (erster Frame ist ein Insert-/Attach-Frame, kein Diff gegen den Prerender), das Ergebnis — kein Write auf die Property — stimmt dennoch.
- Die Take-/Restore-Semantik (`RestoreBehavior` default = „always restores") ist im Plan korrekt als Restrisiko für clientseitige Rück-Navigationen mit identischem `q` benannt und zur Implementierungsverifikation vorgesehen.
- Die planseitige Begründung `q ?? string.Empty` (leerer String beim `q`→`null`-Übergang statt Attribut-Entfernung) ist korrekt und verhindert einen echten Stale-Value-Bug.
