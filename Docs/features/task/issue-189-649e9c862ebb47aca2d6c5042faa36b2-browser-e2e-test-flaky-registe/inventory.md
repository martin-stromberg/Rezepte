# Bestandsaufnahme

## Symptom
`Register_WithDemoData_CreatesDemoData` schlägt sporadisch mit
`TimeoutException: ... calendar=0 ... seed job status=Succeeded` fehl.
Lokal reproduziert (2026-11-16, Release, exakt gleiche Signatur):
`cookbooks=5, recipes=43, calendar=0, shopping=6`.

## Fakten (verifiziert)
- Der Seed-Job benötigt real nur ~1,2 s (DB: StartedAt→CompletedAt); alle 5
  CalendarEvents werden korrekt committed (StartDate lokale Tage +1..+5, TimeOfDay 18:00).
- `GET /api/calendar?from=..&to=..` liefert alle 5 Occurrences korrekt zurück.
- Die gerenderte `/calendar`-Seite (SSR-Prerender) enthält die 5 `.recipe-item`-Karten.
- Playwright-Trace des Fehlschlags zeigt pro Kalender-Read:
  1. `GotoAsync /calendar` → Prerendered DOM enthält bereits `calendar-root` + 5 Items.
  2. ~3 ms später verbindet der interactive Blazor-Circuit → `OnInitializedAsync` läuft
     erneut → `isLoading=true` → DOM fällt auf `lade…` zurück.
  3. `WaitForSelector('.calendar-root, .alert-danger')` war schon gegen das
     prerendered DOM erfolgreich → `CountAsync('.recipe-item, .event-item')` liest
     exakt im Re-Render-Fenster → **0** (deterministisch auf diesem System).
  4. `WaitForResponseAsync(url enthält '/api/calendar')` kann **niemals** auslösen:
     der Fetch läuft serverseitig im Circuit (HttpClient der App), nicht im Browser.
     Jeder Read verbrennt ~20 s totes Timeout → nur ~5 Pollzyklen pro 120 s.
  5. Dezember-Ansicht zeigt korrekt 0 (Events liegen im November).
- Gleiche Race trifft alle Zähler: `cookbook-card=0` wurde ebenfalls transient beobachtet.

## Ursache
Der Test zählt DOM-Elemente im Umschaltfenster Prerender→Interactive-Render
(`isLoading`-Platzhalter) und wartet zusätzlich auf eine Browser-Response, die
architekturbedingt nie eintreffen kann. Keine App-Änderung nötig — Daten/API/UI korrekt.

## Betroffene Stellen
- `Rezepte.Tests.Browser/Auth/RegisterTests.cs`
  - `DemoDataWaitHelper.ReadCountsAsync` / `WaitForDemoDataAsync` (Polling)
  - `CountCalendarEventsAsync` (Month-Nav + Zählen)
  - `CountCookbooksAsync`, `CountRecipesAsync`, `CountShoppingListItemsAsync` (gleiche Race)
