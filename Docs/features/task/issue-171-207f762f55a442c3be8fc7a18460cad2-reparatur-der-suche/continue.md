# Offene Aufgaben

Erstellt am: 2026-10-25
Abbruchgrund: Maximale Iterationsanzahl erreicht

Die folgenden Aufgaben konnten im automatisierten Zyklus nicht abgeschlossen werden
und müssen manuell oder in einem erneuten Lauf bearbeitet werden.

## Offene Planelemente

- Keine — `review.md` trägt den Status „Vollständig umgesetzt". (Tasks-#34 „Produktiv-Verifikation nach Deployment" ist dokumentiert als nicht ausführbar in diesem Zyklus.)

## Code-Review-Befunde

- [ ] `RecipeSearch.razor` — `Dispose()` (Z. 171–175): `cts` wird gecancelt+disposed, aber nicht auf `null` gesetzt. Ein durch Komponenten-Teardown abgebrochener laufender Suchlauf wird im `ReferenceEquals(cts, localCts)`-Check fälschlich als Timeout/Fehlschlag klassifiziert → `LogWarning` „was cancelled or timed out" (verwässert das Diagnosesignal für Plan-Schritt 10), `searchFailed = true` und `StateHasChanged()` auf der abgebauten Komponente. Empfehlung: `cts = null` nach Dispose oder `_disposed`-Flag.
- [ ] `MainLayout.razor` — `OnSubmitSearch` (Z. 115): `searchQuery.Trim()` ohne Null-Guard; die Vorgängerversion prüfte `IsNullOrWhiteSpace`. `InputText` bindet auf `string?`-Ebene; die Codebasis nutzt auf identisch deklarierten Feldern `?.Trim()` (`CalendarEventDialog.razor:490`, `RecipeEdit.razor:416`). Empfehlung: `searchQuery?.Trim() ?? string.Empty`.

## Fehlgeschlagene Tests

- [ ] `Rezepte.Tests.Browser.RegisterTests.Register_WithDemoData_CreatesDemoData` — `System.TimeoutException: Demo data state did not match expected value True within 120000ms` (seed job `Succeeded`, Zählpfad liefert wechselnd `recipes=0`/`calendar=0`/`calendar=5`). Nachweislich pre-existing Flake außerhalb des Issue-Scopes: schlägt auch auf HEAD ohne die Issue-Änderungen (verifiziert via Worktree-Lauf), bekannte Baustelle (Commits 78b2850, f407b6f).
