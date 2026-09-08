# Test-Ergebnisse

## Ergebnis

**Status:** Keine Fehler

## E2E-Abdeckung

| Szenario | Test / Testklasse | Ergebnis |
|----------|-------------------|----------|
| Registrierung mit angehakter Demo-Daten-Checkbox → 5 Kochbücher, 43 Rezepte, 5 Kalendereinträge, Einkaufslisteneinträge | `RegisterTests.Register_WithDemoData_CreatesDemoData` (`Rezepte.Tests.Browser/Auth/RegisterTests.cs`) | Bestanden |
| Registrierung ohne Demo-Daten-Checkbox → keine Demo-Daten sichtbar | `RegisterTests.Register_WithoutDemoData_LeavesListsEmpty` (`Rezepte.Tests.Browser/Auth/RegisterTests.cs`) | Bestanden |

## Zusammenfassung

`dotnet build Rezepte.sln` — erfolgreich, 0 Warnungen, 0 Fehler.

### Rezepte.Tests

- Gesamt: 590
- Bestanden: 590
- Fehlgeschlagen: 0
- Übersprungen: 0

### Rezepte.Tests.Browser

- Gesamt: 15
- Bestanden: 15
- Fehlgeschlagen: 0
- Übersprungen: 0

## Testabdeckung

**Abdeckung:** Nicht messbar (Coverage-Lauf wurde nicht ausgeführt; es wurden die explizit angeforderten Befehle `dotnet test Rezepte.Tests` und `dotnet test Rezepte.Tests.Browser` ohne Coverage-Collector ausgeführt)

## Fehlende Tests

Quelle: `Dateinamen-Konvention`

- Keine Befunde. Alle im Plan genannten neuen Testklassen existieren und liefen erfolgreich: `DemoDataSeedingJobHandlerTests` (`Rezepte.Tests/Services/BackgroundJobs/DemoDataSeedingJobHandlerTests.cs`, alle 6 geplanten Tests einschließlich Fehlerpfade bestanden), `UserServiceTests.RegisterAsync_ShouldEnqueueDemoDataJob_WhenCreateDemoDataIsTrue`, `UserServiceTests.RegisterAsync_ShouldNotEnqueueDemoDataJob_WhenCreateDemoDataIsFalse` sowie beide geplanten E2E-Tests in `Rezepte.Tests.Browser/Auth/RegisterTests.cs`.
