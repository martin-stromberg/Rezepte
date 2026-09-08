# Offene Aufgaben

Erstellt am: 2026-09-08
Abbruchgrund: Maximale Iterationsanzahl erreicht

Die folgenden Aufgaben konnten im automatisierten Zyklus nicht abgeschlossen werden
und müssen manuell oder in einem erneuten Lauf bearbeitet werden.

## Offene Planelemente

Keine.

## Code-Review-Befunde

- [ ] `DemoRecipe.CookbookName` (`Rezepte.Web/Services/DemoRecipe.cs:11`) wird auf allen 43 Demo-Rezepten in `DemoDataSource.cs` gesetzt, aber nirgends gelesen; `DemoDataSeedingJobHandler.cs` nutzt ausschließlich `DemoCookbook.Name`. Toter Code — Parameter entfernen.
- [ ] `Rezepte.Tests.Browser/Auth/RegisterTests.cs` — Erwartungswerte (5/43/5/>0) doppelt hartkodiert: im `DemoDataWaitHelper` (Z. 166) und in den Assertions (Z. 64–67). Benannte Konstanten verwenden oder redundante Assertions streichen.
- [ ] `Rezepte.Tests.Browser/Auth/RegisterPage.cs` (Z. 139–148) — `LoginAsync` bedient die Login-Seite (andere URL, andere Selektoren) und gehört nicht zum Register-Page-Object; besser eigenes `LoginPage`-Objekt.

## Usability-Befunde

Keine.

## Fehlgeschlagene Tests

Keine.
