# Test-Ergebnisse

## Ergebnis

**Status:** Fehler vorhanden

## Fehlgeschlagene Tests

### Rezepte.Tests.Browser.Auth.RegisterTests

- **Register_WithDemoData_CreatesDemoData** — System.TimeoutException: Timeout 30000ms exceeded. waiting for Locator("#register-username")
- **Register_WithoutDemoData_LeavesListsEmpty** — System.TimeoutException: Timeout 30000ms exceeded. waiting for Locator("#register-username")

## E2E-Abdeckung

| Szenario | Test / Testklasse | Ergebnis |
|----------|-------------------|----------|
| Registrierung mit angehaktem Demo-Daten-Checkbox -> anschliessend 5 Kochbuecher, 43 Rezepte, 5 Kalendereintraege, Einkaufslisteneintraege | `RegisterTests.Register_WithDemoData_CreatesDemoData` | Fehlgeschlagen |
| Registrierung ohne Demo-Daten-Checkbox -> keine Demo-Daten sichtbar | `RegisterTests.Register_WithoutDemoData_LeavesListsEmpty` | Fehlgeschlagen |

## Zusammenfassung

- Gesamt: 605
- Bestanden: 603
- Fehlgeschlagen: 2
- Uebersprungen: 0

Details der ausgefuehrten Projekte:

- `Rezepte.Tests`: 590 bestanden, 0 fehlgeschlagen, 0 uebersprungen
- `Rezepte.Tests.Browser`: 13 bestanden, 2 fehlgeschlagen, 0 uebersprungen

## Testabdeckung

**Abdeckung:** 66.02 %

| Datei | Abdeckung |
|-------|-----------|
| Gesamt (Rezepte.Tests, Zeilenabdeckung) | 66.02 % |

## Fehlgeschlagene Szenarien / Fehlender Funktionsnachweis

Die beiden im `plan.md` geforderten Playwright-E2E-Szenarien (`Register_WithDemoData` und `Register_WithoutDemoData`) wurden ausgefuehrt, aber beide mit einem Timeout auf dem Element `#register-username` in `Rezepte.Tests.Browser/Auth/RegisterPage.cs` (Zeile 67) beendet. Dadurch ist der UI-/Benutzerfluss "Checkbox im Registrierungsformular -> Demo-Daten im Hintergrund" derzeit nicht erfolgreich getestet.
