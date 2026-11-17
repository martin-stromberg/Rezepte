# Code-Review

Status: **Keine Befunde** (5 Minor-Hinweise des ersten Reviews wurden in Iteration 2 umgesetzt)

Geprüfter Diff: `Rezepte.Tests.Browser/Auth/RegisterTests.cs`

## Umgesetzte Review-Punkte (Iteration 2)
1. Stabilitätsfenster vergrößert (8 Reads à 400 ms) + Early-Exit bei zwei
   aufeinanderfolgenden gleichen Nicht-Null-Zählungen → schneller im Regelfall,
   robuster auf langsamen Maschinen.
2. Monats-Klick-Wiederholung: Settle-Fenster pro Klick auf 5 s erhöht
   (`MonthNavClickSettleMilliseconds`), damit ein verspätet gerenderter
   Monatswechsel nicht versehentlich zwei Monate weiterschaltet und den
   eventtragenden Monat überspringt.
3. Zweiter Count nur noch bei tatsächlich gewechseltem Monatslabel
   (`navigated`-Gate) → kein Double-Counting desselben Monats.
4. Monatsnavigation in `try/catch (TimeoutException)` → fehlender Header
   (Redirect/Fehlerseite) führt nicht mehr zum harten Abbruch.
5. Toter `WaitForResponseAsync` auf `/api/calendar` entfernt (Browser sieht den
   serverseitigen Circuit-Fetch nie; ~20 s pro Read gespart).
