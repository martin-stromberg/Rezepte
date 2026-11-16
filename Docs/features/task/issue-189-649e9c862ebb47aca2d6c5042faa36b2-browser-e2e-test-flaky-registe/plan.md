# Umsetzungsplan

## Ziel
Der Test zählt Kalender- und Listen-Elemente erst, wenn die Seite einen stabilen,
geladenen Zustand erreicht hat — unabhängig vom Prerender→Interactive-Übergang.

## Änderungen (nur `Rezepte.Tests.Browser/Auth/RegisterTests.cs`)

1. **Stabile Zählung** (`CountStablyAsync`): Mehrfach-Read über ein kurzes
   Stabilitätsfenster (~4 Reads à 400 ms), Rückgabe des Maximums. Transient renders
   (Ladeplatzhalter) zeigen nur *weniger* Items, nie mehr (Blazor wendet Renderbatches
   atomar an) → Maximum ist der korrekte Wert. Angewendet auf alle vier Zähler:
   `.cookbook-card`, `.list-group-item`, `.recipe-item, .event-item`, `.shopping-item`.
2. **Toten Response-Wait entfernen**: `WaitForResponseAsync` auf `/api/calendar` in
   `CountCalendarEventsAsync` streichen (Browser sieht den serverseitigen Fetch nie;
     spart ~20 s pro Read). Monatswechsel-Sicherung bleibt über Label-Änderung +
     `WaitForSettledContentAsync` erhalten.
3. **Monats-Klick robuster**: Klick auf `›` wird wiederholt, solange das Monatslabel
   unverändert bleibt (Klicks vor Circuit-Connect gehen lautlos verloren), begrenzt
   durch `PageContentTimeoutMilliseconds`.

## Akzeptanz
- `Register_WithDemoData_CreatesDemoData` läuft lokal (Release) grün.
- `Register_WithoutDemoData_LeavesListsEmpty` bleibt grün.
- Gesamte Browser-Suite ohne Regressionen.

## Offene Punkte
Keine.
