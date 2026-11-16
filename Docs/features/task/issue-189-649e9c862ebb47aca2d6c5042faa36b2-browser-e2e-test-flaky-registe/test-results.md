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
