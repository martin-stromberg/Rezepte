# Tasks: Service-Einstiegspunkt für „Demo-Daten für einen Benutzer anlegen"

| # | Bereich | Aufgabe | Status | Testnachweis |
|---|---------|---------|--------|--------------|
| 1 | Logik | `IDemoDataSeeder` Interface mit `SeedForUserAsync(string userId, CancellationToken ct)` anlegen (`Rezepte.Web/Services/IDemoDataSeeder.cs`) | Erledigt | `DemoDataSeedingJobHandlerTests.HandleAsync_WithValidJob_CallsSeedForUserAsync` |
| 2 | Logik | `DemoDataSeeder` Klasse anlegen (`Rezepte.Web/Services/DemoDataSeeder.cs`): Sequenzmethoden, Konstanten, `SeededRecipe` 1:1 aus Handler übernehmen, Konstruktor-Injektion | Erledigt | `DemoDataSeederTests_Sequence`, `DemoDataSeederTests_Errors` |
| 3 | Logik | `DemoDataSeedingJobHandler` auf Auslöserolle reduzieren (Payload, Benutzerprüfung, `SeedForUserAsync`-Aufruf; Sequenzmember entfernen) | Erledigt | `DemoDataSeedingJobHandlerTests` (4 Tests: Delegation, fehlender Benutzer, leere UserId, Fehlerdurchreichung) |
| 4 | Konfiguration/DI | `AddScoped<IDemoDataSeeder, DemoDataSeeder>()` in `ServiceCollectionExtensions` registrieren | Erledigt | Kein direkter Test — indirekt über Build + E2E (`RegisterTests.Register_WithDemoData_CreatesDemoData`) |
| 5 | Tests | `DemoDataSeederTests_Sequence` anlegen: Cookbooks/Rezepte/Kalender/ShoppingList-Assertions (aus bisherigen Handler-Tests übernommen) | Erledigt | 4 Tests grün (5 Cookbooks, 43 Rezepte, 5 Kalenderevents, 6 Einkaufslisten-Einträge) |
| 6 | Tests | `DemoDataSeederTests_Errors` anlegen: Fehlerabbruch je Schritt + toleranter Bild-Fehlerfall | Erledigt | 5 Tests grün (Cookbook/Recipe/Calendar/ShoppingItem-Fehler → `InvalidOperationException`; ImageAttach-Fehler → tolerant) |
| 7 | Tests | `DemoDataSeedingJobHandlerTests` umbauen: nur Payload-Parsing, Benutzerprüfung, Delegation, Fehlerdurchreichung | Erledigt | 4 Tests grün |
| 8 | Build | `dotnet build Rezepte.sln` + `dotnet test Rezepte.Tests` grün | Erledigt | Build 0 Fehler/0 Warnungen; 714/714 Tests erfolgreich |
| 9 | Doku | `Docs/help/user-accounts.md` Abschnitt „Technischer Hintergrund" an Service-Kapselung anpassen | Offen | — (Lifecycle-Schritt 12) |
| 10 | E2E | `RegisterTests.Register_WithDemoData_CreatesDemoData` ausführen (unveränderter E2E-Nachweis) | Offen | — (Lifecycle-Schritt 10) |
