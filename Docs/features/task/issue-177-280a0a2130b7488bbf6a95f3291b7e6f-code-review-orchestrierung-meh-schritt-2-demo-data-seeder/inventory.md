# Bestandsaufnahme: Service-Einstiegspunkt für „Demo-Daten für einen Benutzer anlegen"

Analyse des Demo-Seeding-Bereichs (`DemoDataSeedingJobHandler`, `DemoDataSource`, Job-Infrastruktur, Auslöser `UserService.RegisterAsync`) bezogen auf die Anforderung, die fachliche Sequenz in einen Service-Einstiegspunkt `IDemoDataSeeder` zu kapseln.

## Zusammenfassung

- Der Geschäftsvorfall ist heute **vollständig** im `DemoDataSeedingJobHandler` (318 Zeilen) komponiert: `HandleAsync` orchestriert `SeedCookbooksAsync` → `SeedRecipesAsync` (inkl. `OpenDemoImages`/`AttachImageAsync` für `demo-images.zip`) → `SeedCalendarAsync` → `SeedShoppingListAsync`.
- Alle benötigten Domänen-Services (`ICookbookService`, `IRecipeService`, `ICalendarService`, `IShoppingListService`) sind scoped registriert und können direkt in einen scoped Seeder-Service injiziert werden — die `scopeServices`-Auflösung ist nur Handler-Infrastruktur.
- Demo-Daten liegen in der statischen Klasse `DemoDataSource` (5 `DemoCookbook`, 43 `DemoRecipe`), `demo-images.zip` wird per `Rezepte.Web.csproj` ins Output-Verzeichnis kopiert (steht dadurch auch im Test-Output `Rezepte.Tests/bin/Debug/net10.0/DemoData/` zur Verfügung).
- Auslöser: `UserService.RegisterAsync` enqueut bei `createDemoData == true` einen Job `seed-demo-data` mit `DemoDataSeedPayload`. Tests dazu existieren in `UserServiceTests`.
- Es gibt **kein** bestehendes `IDemoDataSeeder`- oder `ExecutePassAsync`-Muster im Repo; Namens-/Platzierungskonvention für Demo-Artefakte ist flach unter `Rezepte.Web/Services/` (`DemoDataSource.cs`, `DemoCookbook.cs`, `DemoRecipe.cs`).
- Test-Ausgangszustand: **707/707 Tests erfolgreich** (`dotnet test Rezepte.Tests`), keine bekannten Fehlschläge — Details und Nachweis in [tests.md](inventory/tests.md).

## Details

- [Job-Handler und Sequenz](inventory/handler.md) — `DemoDataSeedingJobHandler` im Detail, Fehlerverhalten, Nebenbedingungen
- [Job-Infrastruktur und Auslöser](inventory/infrastructure.md) — `DemoDataSeedPayload`, `IBackgroundJobQueue`, `BackgroundJobHostedService`, `UserService.RegisterAsync`, DI-Registrierungen
- [Demo-Daten-Quelle](inventory/demodata.md) — `DemoDataSource`, `DemoCookbook`, `DemoRecipe`, `demo-images.zip`, Domänen-Service-Signaturen
- [Tests](inventory/tests.md) — bestehende Testklassen, Hilfsmethoden und projektweiter Test-Ausgangszustand (Baseline)
- [Dokumentation](inventory/docs.md) — betroffene Doku-Fundstellen
