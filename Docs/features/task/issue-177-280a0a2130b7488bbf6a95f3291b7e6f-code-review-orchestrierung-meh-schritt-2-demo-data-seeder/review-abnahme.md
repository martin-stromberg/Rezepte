# Abnahme-Review (fachlich, unabhängig)

## Ergebnis

**Status:** Abnahme empfohlen — alle Prüfpunkte erfüllt; Beobachtungen ohne Nacharbeitszwang.

Geprüft gegen `requirement.md` anhand des Ist-Stands (HEAD d846fd5) und der dokumentierten Vorher-Sequenz (`inventory/handler.md`, `Docs/projects/task/.../inventory/demo-seeding.md`).

## Urteile je Prüfpunkt

| # | Prüfpunkt | Urteil |
|---|-----------|--------|
| 1 | `IDemoDataSeeder.SeedForUserAsync` existiert; komplette fachliche Sequenz 1:1 im Service | Erfüllt |
| 2 | Handler ohne Fachsequenz (nur Payload → Benutzerprüfung → Delegation); `JobTypeName` unverändert | Erfüllt |
| 3 | DI scoped registriert; keine toten Handler-Abhängigkeiten | Erfüllt |
| 4 | Verhalten/Dateninhalte identisch; kein Transaktions-Umbau | Erfüllt |
| 5 | Sequenz-Assertions im Service-Test, Handler-Test nur Delegation/Payload/User/Fehler | Erfüllt |
| 6 | Registrierung/Job-Enqueue unverändert | Erfüllt |
| 7 | `Docs/help/user-accounts.md` an Service-Kapselung angepasst | Erfüllt |
| 8 | Englischer Code, vollständige XML-Doku, konsistenter Stil | Erfüllt |

## Details

### 1. Service-Einstiegspunkt und Sequenz — erfüllt

- `Rezepte.Web/Services/IDemoDataSeeder.cs:15` — `Task SeedForUserAsync(string userId, CancellationToken ct)`, vollständige XML-Doku.
- `DemoDataSeeder.SeedForUserAsync` (`DemoDataSeeder.cs:63-75`): Start-`LogInformation` → `OpenDemoImages` → `SeedCookbooksAsync` → `SeedRecipesAsync` → `SeedCalendarAsync` → `SeedShoppingListAsync` → Abschluss-`LogInformation` — exakt die dokumentierte Vorher-Reihenfolge inkl. Logging-Positionen.
- `SeedCookbooksAsync` (`:83-100`): `Dictionary` mit `StringComparer.Ordinal`, `CreateAsync(userId, name, null, ct)`, `!ok || created is null` → `InvalidOperationException` — identisch.
- `SeedRecipesAsync` (`:112-152`): fehlende `cookbookId` → `InvalidOperationException`; `CreateAsync` mit `uri: null` (`:133`); `AttachImageAsync` je Rezept; `SeededRecipe`-Paare nur aus `DemoDataSource.CalendarCookbookName` — identisch.
- `OpenDemoImages` (`:158-167`): Archiv fehlt → `LogWarning` + `null` (kein Fehler); sonst `ZipArchive` read — identisch.
- `AttachImageAsync` (`:178-204`): Titel-Matching `Path.GetFileNameWithoutExtension(e.Name)` OrdinalIgnoreCase; kein Eintrag → stiller Rücksprung; `AddImageAsync`-Misserfolg nur `LogWarning`, **kein Throw** — identisch.
- `GetImageContentType` (`:211-217`): `.png`/`.jpg`/`.jpeg`-Mapping unverändert.
- `SeedCalendarAsync` (`:227-253`): `Take(CalendarEventCount=5)`, `DateTime.Today.AddDays(i+1)`, `TimeSpan.FromHours(18)`, `Portions > 0 ? Portions : 2`, `RecurrenceType.None`, `WeekDays.None`; Fehler → `InvalidOperationException` — identisch.
- `SeedShoppingListAsync` (`:262-291`): kein geplantes Rezept → Rücksprung; `EnsureDefaultGroupAsync`; `AddItemAsync` je Step×Ingredient des ersten geplanten Rezepts; Fehler → `InvalidOperationException` — identisch.
- Nebenbedingungen: Fehlerabbruch je Schritt, Teilanlage ohne Retry, keine Transaktion — unverändert (expliziter Scope-Ausschluss eingehalten).

### 2. Handler-Reduktion — erfüllt

- `DemoDataSeedingJobHandler.cs` (38 Zeilen): `job`-Nullcheck + `ct.ThrowIfCancellationRequested` (`:19-20`), `DemoDataSeedPayload.FromJson` + leere UserId → `InvalidOperationException` (`:22-26`), `IUserService.GetByIdAsync` + User `null` → `InvalidOperationException` (`:28-33`), `IDemoDataSeeder` via `scopeServices` + `SeedForUserAsync(payload.UserId, ct)` (`:35-36`).
- `JobTypeName = "seed-demo-data"` (`:11`) und `JobType` (`:14`) unverändert; keine verwaisten privaten Methoden oder toten Usings.
- Identische Fehlermeldungen zu fehlender UserId/fehlendem Benutzer wie zuvor.

### 3. DI — erfüllt

- `ServiceCollectionExtensions.cs:197`: `services.AddScoped<IDemoDataSeeder, DemoDataSeeder>()`; Handler-Registrierung (`:200`) unverändert.
- `DemoDataSeeder` injiziert genau `ICookbookService`, `IRecipeService`, `ICalendarService`, `IShoppingListService`, `ILogger<DemoDataSeeder>` (`DemoDataSeeder.cs:48-60`) — alle scoped; Handler hat keinen Konstruktor → keine toten Abhängigkeiten.

### 4. Verhaltensgleichheit — erfüllt

- `DemoDataSource.cs` (630 Zeilen) in inventarisierter Struktur (5 Kochbücher, 43 Rezepte, `CalendarCookbookName = "Abendessen"`); `DemoDataSeedPayload.cs` unverändert; `demo-images.zip`-Copy via `Rezepte.Web.csproj:36` unverändert.
- E2E-Nachweis der Dateninhalte: Seed-Job `Succeeded` mit cookbooks=5, recipes=43, shopping=6 (`test-results.md`); 5 Kalenderevents zwingend angelegt (Sequenz wirft sonst → Job `Failed`).
- Gewollte Abweichung: Log-Kategorie von `ILogger<DemoDataSeedingJobHandler>` auf `ILogger<DemoDataSeeder>` — im Plan als Seiteneffekt benannt.

### 5. Tests — erfüllt

- `DemoDataSeederTests_Sequence` (4 Tests): 5× `CreateAsync` mit `description: null`; 43× `IRecipeService.CreateAsync` mit `uri: null`; 5 Kalenderevents `Today+1..5` à 18:00 `RecurrenceType.None`/`WeekDays.None`; `EnsureDefaultGroupAsync` Once + 6× `AddItemAsync` in Gruppe `"1"`.
- `DemoDataSeederTests_Errors` (5 Tests): `InvalidOperationException` bei Fehlschlag jedes Sequenzschritts; `AddImageAsync`-Misserfolg tolerant (Kalender + Shopping laufen danach verifiziert weiter).
- `DemoDataSeedingJobHandlerTests` (4 Tests): Delegation mit korrekter UserId (`Times.Once`), fehlender Benutzer und leere UserId → `InvalidOperationException` mit `Times.Never`-Negativnachweis auf dem Seeder, Exception-Durchreichung inkl. Message-Assertion. Kein nachgebauter Sequenz-Mock-Zoo; Mock-Landschaft zentral in `DemoDataSeederMocks`.
- Testzahl nachvollziehbar: Baseline 707 + 9 neue Seeder-Tests − 2 umgezogene Handler-Sequenz-Tests = 714/714 grün (`test-results.md`).

### 6. Auslöser unverändert — erfüllt

- `UserService.cs:154-159`: `createDemoData` → `DemoDataSeedPayload` + `EnqueueAsync(DemoDataSeedingJobHandler.JobTypeName, …)` unverändert.
- `UserServiceTests.cs:309-346` (Enqueue-Tests) und `Rezepte.Tests.Browser/Auth/RegisterTests.cs` unverändert.

### 7. Doku — erfüllt

- `Docs/help/user-accounts.md:42` beschreibt die neue Kette korrekt: Handler liest Nutzlast und prüft Benutzer, delegiert an `IDemoDataSeeder.SeedForUserAsync`; Sequenzdetails, Bild-Toleranz, Fehlerverhalten und `DemoDataSource`-Herkunft stimmen mit dem Code überein.

### 8. Sprache/Stil — erfüllt

- Neuer Code durchgängig englisch, XML-Doku vollständig (Interface, Klasse, alle privaten Member, Record). `ConfigureAwait(false)` konsistent mit der Codebasis. Platzierung/Benennung nach Konvention (`I{Name}`/`{Name}` flach unter `Rezepte.Web/Services/`).

## Weitere Beobachtungen

1. **Reihenfolgen-Assertion fehlt:** Die Seeder-Tests verifizieren Anzahl und Argumente der Service-Aufrufe, nicht die strikte Schritt-Reihenfolge (kein `MockSequence`/`It.IsInSequence`). Die Reihenfolge ist nur indirekt über Datenabhängigkeiten (cookbookIds, plannedRecipes) gesichert; bei linearem `await`-Ablauf geringes Risiko.
2. **E2E-Fehlschlag preexisting:** `Register_WithDemoData_CreatesDemoData` schlägt 3× identisch fehl — inkl. Kontrolllauf auf dem Basis-Stand ohne die Änderung (Kalender-UI-Seitenzählung liefert 0 bei Job-Status `Succeeded`). Umgebungs-/Rendering-Problem, kein Seeding-Defekt; Entscheidung über manuelle Abnahme oder dokumentierten Verzicht obliegt dem Anwender (`test-results.md`).
3. **„ZIP fehlt"-Pfad nicht unit-testbar** ohne Änderung der statischen `DemoImagesPath`-Bindung — im Plan begründeter Verzicht; der tolerante `AddImageAsync`-Pfad setzt das transitiv vorhandene Archiv im Test-Output voraus.
4. **Portions-Fallback** (`Portions <= 0` → 2) nicht dediziert assertiert (Kalender-Verifies nutzen `It.IsAny<int>()`) — pre-existing Testtiefe.
5. Kosmetik: `<returns>`-XML-Tag auf dem `DemoImagesPath`-Feld (`DemoDataSeeder.cs:31`) — 1:1 aus dem Alt-Code übernommen.

## Nacharbeitspunkte

Keine zwingenden. Optional:

- Explizite Reihenfolgen-Absicherung in `DemoDataSeederTests_Sequence` (z. B. `MockSequence` über die vier Domänen-Services).
- Anwender-Entscheidung zum preexisting E2E-Kalender-Zählproblem (manuelle Abnahme vs. dokumentierter Verzicht).

## Gesamturteil

**Abnahme empfohlen.** Der Geschäftsvorfall ist vollständig und verhaltensgleich in `IDemoDataSeeder` gekapselt: Die Sequenz samt Fehlerabbruch, Bild-Toleranz und Nebenbedingungen wurde 1:1 übernommen, der Handler ist auf die Auslöserolle reduziert, DI ist sauber, Auslöser und Dateninhalte unverändert. Die Testverlagerung ist korrekt und aussagekräftig. Der einzige fehlschlagende E2E-Prüfschritt ist nachweislich preexisting (identischer Fehlschlag auf der Basis) und betrifft die Kalender-Seitenanzeige, nicht das Seeding.
