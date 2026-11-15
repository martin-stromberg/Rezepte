# Tasks: Service-Einstiegspunkt für „Demo-Daten für einen Benutzer anlegen"

| # | Bereich | Aufgabe | Status | Testnachweis |
|---|---------|---------|--------|--------------|
| 1 | Logik | `IDemoDataSeeder` Interface mit `SeedForUserAsync(string userId, CancellationToken ct)` anlegen (`Rezepte.Web/Services/IDemoDataSeeder.cs`) | Offen | — |
| 2 | Logik | `DemoDataSeeder` Klasse anlegen (`Rezepte.Web/Services/DemoDataSeeder.cs`): Sequenzmethoden, Konstanten, `SeededRecipe` 1:1 aus Handler übernehmen, Konstruktor-Injektion | Offen | — |
| 3 | Logik | `DemoDataSeedingJobHandler` auf Auslöserolle reduzieren (Payload, Benutzerprüfung, `SeedForUserAsync`-Aufruf; Sequenzmember entfernen) | Offen | — |
| 4 | Konfiguration/DI | `AddScoped<IDemoDataSeeder, DemoDataSeeder>()` in `ServiceCollectionExtensions` registrieren | Offen | — |
| 5 | Tests | `DemoDataSeederTests_Sequence` anlegen: Cookbooks/Rezepte/Kalender/ShoppingList-Assertions (aus bisherigen Handler-Tests übernommen) | Offen | — |
| 6 | Tests | `DemoDataSeederTests_Errors` anlegen: Fehlerabbruch je Schritt + toleranter Bild-Fehlerfall | Offen | — |
| 7 | Tests | `DemoDataSeedingJobHandlerTests` umbauen: nur Payload-Parsing, Benutzerprüfung, Delegation, Fehlerdurchreichung | Offen | — |
| 8 | Build | `dotnet build Rezepte.sln` + `dotnet test Rezepte.Tests` grün | Offen | — |
| 9 | Doku | `Docs/help/user-accounts.md` Abschnitt „Technischer Hintergrund" an Service-Kapselung anpassen | Offen | — |
| 10 | E2E | `RegisterTests.Register_WithDemoData_CreatesDemoData` ausführen (unveränderter E2E-Nachweis) | Offen | — |
