# Testergebnisse

Status: **Keine Fehler**

## Reale End-to-End-Abnahme
- Fehlschlag vorab reproduziert (vor dem Fix): `Register_WithDemoData_CreatesDemoData`
  → `TimeoutException ... calendar=0 ... seed job status=Succeeded` nach 2 m 9 s.
- Seed-Dauer verifiziert: ~1,2 s (BackgroundJobs-Zeitstempel); alle 5 CalendarEvents
  committed; `/api/calendar` liefert sie; Seite rendert sie → App-Seite fehlerfrei.
- Playwright-Trace belegt die Zähle im Prerender→Interactive-Umschaltfenster sowie den
  20-s-Totwait auf eine nie eintreffende Browser-Response.

## Nach dem Fix (Release, `--no-build`)
- `Register_WithDemoData_CreatesDemoData`: **Bestanden in 14 s** (zuvor Timeout 2 m 9 s).
- `Register_WithoutDemoData_LeavesListsEmpty`: Bestanden (33–36 s).
- Gesamte `Rezepte.Tests.Browser`-Suite: **27/27 Bestanden**, Gesamtzeit ~50 s.

## Reale End-to-End-Abnahme (Szenario aus plan.md)
Registrierung mit Demo-Daten über die echte UI → alle vier Zähler (Kochbücher 5,
Rezepte 43, Kalender 5, Einkaufsliste ≥1) werden im Browser geprüft → bestanden.

## Folgekorrektur (Issue-Kommentar): `InPageSearch_TypedValue_SurvivesInteractiveCircuit`

- Gleicher Flake zweimal in CI beobachtet (issue-185-Branch + erster Lauf auf diesem
  Branch): Feld zeigt dauerhaft `q` statt des getippten Werts.
- Lokale Instrumentierung (MutationObserver + value-Hook): Attach **ersetzt** das
  Input-Element; `rezepteSyncSearchInput` stellte den Snapshot genau **einmal**
  wieder her; die `Nächste`-Navigation schrieb lokal nichts. → Jeder *spätere*
  Framework-Write (Remount, 2. Attach-Batch, Enhanced-Nav-Patch, verlorener erster
  Interop-Call) gewann dauerhaft → Fehlerbild in CI.
- Fix (App-Seite): Snapshot ist nun an den `q`-Parameter gebunden und wird in
  `rezepteSyncSearchInput` **bei jedem Render** erzwungen (nicht einmalig
  konsumiert); fremde Snapshots werden verworfen; leeres Feld weiterhin mit `q`
  befüllt. `RecipeSearch.OnAfterRenderAsync` ruft den Sync unbeschränkt auf.
- Verifikation lokal: simuliertes spätes Clobbern (`el.value = q`) wird durch den
  nächsten Render repariert → `E2E-Getippt-…` wiederhergestellt.
- `RecipeSearchComponentTests` (bUnit): 9/9 bestanden.
- Browser-Suite `RecipeSearchBrowserTests` + `RegisterTests`: 9/9 bestanden;
  Gesamtsuite siehe unten.
