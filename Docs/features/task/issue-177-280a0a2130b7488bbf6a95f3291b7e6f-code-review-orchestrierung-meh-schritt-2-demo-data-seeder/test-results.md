# Testergebnisse

Branch: `task/issue-177-280a0a2130b7488bbf6a95f3291b7e6f-code-review-orchestrierung-meh-schritt-2-demo-data-seeder`
Datum: 2026-11-15, .NET SDK 10.0.403, Windows

## Ergebnis

**Status:** Fehler vorhanden — ausschließlich der bekannte, als preexisting nachgewiesene Browser-Test; alle automatisierten Unit-/Integrationstests grün.

## Testläufe

| Lauf | Befehl inkl. Filter | Ergebnis | Nachweis |
|------|--------------------|----------|----------|
| Baseline (vor Umsetzung, @9780812) | `dotnet test Rezepte.Tests/Rezepte.Tests.csproj` | 707 erfolgreich, 0 fehlgeschlagen, 0 übersprungen | [inventory/test-results/baseline-rezepte-tests.txt](inventory/test-results/baseline-rezepte-tests.txt) |
| Nach Umsetzung | `dotnet build Rezepte.sln` | 0 Fehler, 0 Warnungen | Build-Ausgabe |
| Nach Umsetzung | `dotnet test Rezepte.Tests/Rezepte.Tests.csproj` | 714 erfolgreich, 0 fehlgeschlagen, 0 übersprungen | Testlauf (2× wiederholt, identisch) |
| E2E Lauf 1 | `dotnet test Rezepte.Tests.Browser --filter FullyQualifiedName~RegisterTests` (beide Tests der Klasse) | 1 erfolgreich (`Register_WithoutDemoData_LeavesListsEmpty`), 1 fehlgeschlagen | [inventory/test-results/browser-registertests.txt](inventory/test-results/browser-registertests.txt) |
| E2E Lauf 2 (Wiederholung) | `dotnet test Rezepte.Tests.Browser --filter FullyQualifiedName~RegisterTests.Register_WithDemoData_CreatesDemoData` | 1 fehlgeschlagen — identisches Fehlerbild | [inventory/test-results/browser-registertests-retry.txt](inventory/test-results/browser-registertests-retry.txt) |
| E2E Lauf 3 (Basis-Kontrolle) | gleicher Befehl auf gestashtem Stand (= Merge-Commit 665b58e + Planungsartefakte, ohne Implementierung) | 1 fehlgeschlagen — **identisches Fehlerbild** | Konsolenausgabe (siehe Abschnitt „Basis-Kontrolle") |

## Fehlgeschlagene Tests

### `Rezepte.Tests.Browser.RegisterTests.Register_WithDemoData_CreatesDemoData`

- **Fehlerbild (3× identisch, mit und ohne Codeänderung):** `System.TimeoutException : Demo data state did not match expected value True within 120000ms. Last observed: cookbooks=5, recipes=43, calendar=0, shopping=6; seed job status=Succeeded, error=(none)`
- **Einordnung:** Der Seeding-Job läuft erfolgreich durch (`status=Succeeded`), alle per Job erzeugten Daten sind da: 5 Kochbücher, 43 Rezepte, 6 Einkaufslisteneinträge — und damit zwingend auch die 5 Kalenderevents, denn `SeedCalendarAsync` liegt vor der Einkaufslisten-Befüllung und wirft bei Fehlschlag (Job wäre sonst `Failed`). Fehlschlagend ist ausschließlich das **UI-Seitenzählen der Kalenderseite** (`.recipe-item, .event-item` über zwei Monatsansichten), das durchgehend 0 liefert.
- **Preexisting-Nachweis (Nachweisregel aus inventory.md erfüllt):** Derselbe Test mit demselben Fehlerbild schlägt auf dem Basis-Stand ohne die Implementierungsänderung fehl (Lauf 3, gestashter Working Tree). Die Änderung verursacht den Fehlschlag damit nachweislich **nicht**. Zusätzlich wurde im Projekt-Kontext bereits vorher eine Flakiness desselben Tests mit Kalender-Read=0 dokumentiert.
- **Klassifizierung:** Umgebungs-/Test-Infrastrukturproblem der Kalender-Seitenzählung (Rendering), kein Seeding-Defekt. Test wurde **nicht** angepasst, um ihn grün zu machen.

## Reale End-to-End-Abnahme

Die Anforderung berührt Persistenz und Hintergrund-Verarbeitung; die reale Abnahme läuft über den bestehenden E2E-Test (Registrierung → Job → sichtbare Daten):

| # | Szenario | Ergebnis |
|---|----------|----------|
| 1 | Happy Path: Registrierung mit Demo-Daten → 5 Kochbücher/43 Rezepte/5 Kalenderevents/Einkaufsliste | **Teilweise belegt, ein Prüfschritt fehlgeschlagen:** Seeding fachlich erfolgreich (Job `Succeeded`; Kochbücher, Rezepte, Einkaufsliste in der UI gezählt), aber Kalender-Seitenzählung liefert 0 → Test schlägt fehl. Dasselbe Bild auf dem unveränderten Basis-Stand → Umgebungsproblem, keine Regression durch diesen Schritt. |
| 2 | Registrierung ohne Demo-Daten → leere Listen | **Bestanden** (`Register_WithoutDemoData_LeavesListsEmpty`, Lauf 1) |

**Status der Abnahme:** `Nicht erfolgreich nachweisbar (Umgebung)` — das einzige fehlschlagende Teilkriterium (Kalender-UI-Zählung) ist nachweislich preexisting (identischer Fehlschlag auf Basis-Commit). Das fachliche Seeding-Ergebnis ist über Job-Status (`Succeeded`) und die übrigen UI-Zählungen belegt; die Kalenderdaten sind zwangsläufig angelegt (Sequenz wirft sonst → Job `Failed`).

**Eskalation an den Anwender (gemäß Lifecycle Schritt 10):** Zwei Auflösungswege stehen offen — manuelle Abnahme des Kalender-Szenarios durch den Anwender (in einer Umgebung mit funktionierender Kalender-Seitenanzeige) oder dokumentierter Verzicht mit Begründung. Bis zur Entscheidung bleibt das Feature-Verzeichnis erhalten (wie in Schritt 1).

## Zusammenfassung Zahlen

- `Rezepte.Tests`: **714/714 erfolgreich** (Baseline 707 + 9 neue `DemoDataSeeder`-Tests − 2 Handler-Sequenz-Tests, die in `DemoDataSeederTests_*` umgezogen sind)
- `Rezepte.Tests.Browser`: 1/2 erfolgreich; `Register_WithDemoData_CreatesDemoData` fehlgeschlagen (3× identisch, inkl. Basis-Kontrolle → preexisting/environmental)
- Geänderte Produktivcode-Pfade zeigen kein abweichendes Testverhalten gegenüber der Baseline
