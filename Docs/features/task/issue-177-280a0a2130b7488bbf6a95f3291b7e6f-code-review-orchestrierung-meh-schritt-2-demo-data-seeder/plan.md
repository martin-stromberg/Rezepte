# Umsetzungsplan: Service-Einstiegspunkt für „Demo-Daten für einen Benutzer anlegen"

## Übersicht

Die fachliche Sequenz des Geschäftsvorfalls „Demo-Daten für einen Benutzer anlegen" wird aus `DemoDataSeedingJobHandler` in einen neuen scoped Service `IDemoDataSeeder`/`DemoDataSeeder` unter `Rezepte.Web/Services/` verlagert. Der Handler reduziert sich auf die Auslöserolle (Nutzlast parsen, Benutzer prüfen, `SeedForUserAsync` aufrufen). Tests ziehen entsprechend um; Doku-Abschnitt in `Docs/help/user-accounts.md` wird angepasst.

## Scope-Klärung

|| Feld | Festlegung |
||------|------------|
|| Verarbeitungseinheit | Eine Ausführung des Geschäftsvorfalls „Demo-Daten für einen Benutzer anlegen" für genau einen Benutzer |
|| Auslösende Ereignisse | Abarbeitung des `seed-demo-data`-Jobs durch `BackgroundJobHostedService` → `DemoDataSeedingJobHandler.HandleAsync` (Job-Einstellung unverändert bei Registrierung) |
|| Erwartete Wirkung | Unveränderte Demo-Daten nach Registrierung: 5 Kochbücher, 43 Rezepte inkl. Bilder (soweit ZIP vorhanden), 5 Kalenderevents 18:00 auf Folgetagen, befüllte Einkaufsliste in Standardgruppe |
|| Umgang mit fehlenden Objekten | Benutzer fehlt → verwerfen (`InvalidOperationException`, im Handler); `demo-images.zip`/Einzelbild fehlt → tolerieren (`LogWarning`, ohne Bild weiter); Standardgruppe fehlt → erzeugen (`EnsureDefaultGroupAsync`); Domänenaufruf-Fehlschlag → abbrechen (`InvalidOperationException`, Teildaten bleiben) |

### Scope-Ausschlüsse

|| # | Einschränkung / Ausschluss | Begründung | Status |
||---|----------------------------|------------|--------|
|| 1 | Kein transaktionaler Umbau: Teilanlage bei Fehlern ohne Retry bleibt | Explizites Nicht-Ziel des Entwicklungsschritts (geklärte Randbedingung) | bestätigt |
|| 2 | Kein externer Auslöser (Endpunkt/UI) für `IDemoDataSeeder`; Aufruf nur durch den Job-Handler | Auslöseweg laut Entwicklungsschritt unverändert | bestätigt |

## Designentscheidungen

| Komponente / Bereich | Gewählter Ansatz | Begründung |
|----------------------|-----------------|------------|
| Platzierung `IDemoDataSeeder`/`DemoDataSeeder` | Flach unter `Rezepte.Web/Services/` (Namespace `Rezepte.Web.Services`), Geschwisterdateien `IDemoDataSeeder.cs` + `DemoDataSeeder.cs` | Entspricht der bestehenden Konvention für Demo-Artefakte (`DemoDataSource.cs`, `DemoCookbook.cs`, `DemoRecipe.cs`) und für Service-Paare (`ICalendarService`/`CalendarService`). Kein neuer Unterordner für eine einzelne Schnittstelle. |
| Service-Typ / Lebensdauer | Scoped Service mit Konstruktor-Injektion von `ICookbookService`, `IRecipeService`, `ICalendarService`, `IShoppingListService`, `ILogger<DemoDataSeeder>` | Alle Abhängigkeiten sind scoped (`ServiceCollectionExtensions`); Muster der übrigen fachlichen Service-Einstiegspunkte. Die `scopeServices`-Auflösung war nur Handler-Infrastruktur. |
| Schnittstelle | `Task SeedForUserAsync(string userId, CancellationToken ct)` — Richtung `ExecutePassAsync`-Stil (kein Ergebnistupel; Fehler per Exception) | Review-Vorgabe; die Sequenz signalisiert Fehler heute bereits per `InvalidOperationException` — ein Rückgabewert wäre neues Verhalten. |
| Handler-Auflösung | `IUserService` und `IDemoDataSeeder` weiterhin per `scopeServices.GetRequiredService` im `HandleAsync` | Handler werden ohne Konstruktor-Injection über `GetServices<IBackgroundJobHandler>` aufgelöst — Infrastruktur unverändert. |
| Konstanten | `CalendarEventCount`, `DefaultPortions`, `DinnerHour`, `DemoImagesPath` ziehen als private Member in `DemoDataSeeder` um | Gehören zur fachlichen Sequenz, nicht zur Job-Auslösung. `JobTypeName` bleibt im Handler. |

## Programmabläufe

### Demo-Seeding-Ausführung (unveränderte Fachlogik, neuer Träger)

1. `BackgroundJobHostedService` erstellt Scope, löst `DemoDataSeedingJobHandler` über `JobType` auf und ruft `HandleAsync`.
2. `HandleAsync`: `DemoDataSeedPayload.FromJson(job.PayloadJson)`; `UserId` leer → `InvalidOperationException`.
3. `HandleAsync`: `IUserService.GetByIdAsync` — User `null` → `InvalidOperationException`.
4. `HandleAsync`: `IDemoDataSeeder` via `scopeServices` auflösen, `SeedForUserAsync(payload.UserId, ct)` aufrufen; Exception propagiert → Job `Failed`.
5. `DemoDataSeeder.SeedForUserAsync`: `LogInformation` Start; `OpenDemoImages` (ZIP fehlt → `LogWarning` + `null`); `SeedCookbooksAsync` (5× `ICookbookService.CreateAsync`, Fehler → `InvalidOperationException`); `SeedRecipesAsync` (43× `IRecipeService.CreateAsync` mit `uri: null`, `AttachImageAsync` tolerant; Kalender-Kochbuch-Paare sammeln); `SeedCalendarAsync` (5× `ICalendarService.CreateEventAsync`, `Today+1..5`, 18:00, `Portions > 0 ? Portions : 2`); `SeedShoppingListAsync` (`EnsureDefaultGroupAsync` + `AddItemAsync` je Zutat des ersten geplanten Rezepts); `LogInformation` Abschluss.

Beteiligte Klassen/Komponenten: `DemoDataSeedingJobHandler`, `IDemoDataSeeder`, `DemoDataSeeder`, `DemoDataSource`, `DemoDataSeedPayload`, `IUserService`, `ICookbookService`, `IRecipeService`, `ICalendarService`, `IShoppingListService`, `IBackgroundJobQueue`, `BackgroundJobHostedService`.

## Neue Klassen

| Klasse | Typ | Zweck |
|--------|-----|-------|
| `IDemoDataSeeder` | Interface (`Rezepte.Web/Services/IDemoDataSeeder.cs`) | Fachlicher Einstiegspunkt: `Task SeedForUserAsync(string userId, CancellationToken ct)` |
| `DemoDataSeeder` | Klasse (`Rezepte.Web/Services/DemoDataSeeder.cs`), scoped | Enthält die komplette Sequenz: private Methoden `SeedCookbooksAsync`, `SeedRecipesAsync`, `OpenDemoImages`, `AttachImageAsync`, `GetImageContentType`, `SeedCalendarAsync`, `SeedShoppingListAsync`, Konstanten und `SeededRecipe`-Record — inhaltlich 1:1 vom Handler übernommen |

## Änderungen an bestehenden Klassen

### `DemoDataSeedingJobHandler` (Job-Handler)

- **Geänderte Methoden:** `HandleAsync` — entfernt die Auflösung der vier Domänen-Services und den Logger sowie die gesamte Sequenz; ruft stattdessen `IDemoDataSeeder.SeedForUserAsync(payload.UserId, ct)` auf. Payload-Parsing und Benutzerprüfung bleiben.
- **Entfernte Member:** `CalendarEventCount`, `DefaultPortions`, `DinnerHour`, `DemoImagesPath`, `SeedCookbooksAsync`, `SeedRecipesAsync`, `OpenDemoImages`, `AttachImageAsync`, `GetImageContentType`, `SeedCalendarAsync`, `SeedShoppingListAsync`, `SeededRecipe` (Umzug in `DemoDataSeeder`).
- **Unverändert:** `JobTypeName`, `JobType`, Fehlermeldungen zu fehlender UserId/fehlendem Benutzer.

### `ServiceCollectionExtensions` (DI)

- **Neu:** `services.AddScoped<IDemoDataSeeder, DemoDataSeeder>();` (bei den übrigen Service-Registrierungen, vor den `IBackgroundJobHandler`-Einträgen).

## Datenbankmigrationen

Keine.

## Validierungsregeln

Keine neuen — Payload-Validierung (leere `UserId`) und Benutzerprüfung bleiben unverändert im Handler.

## Konfigurationsänderungen

Keine.

## Seiteneffekte und Risiken

- **Unit-Tests mit vorhandenem ZIP:** `demo-images.zip` steht auch im Test-Output; Seeder-Tests laufen daher mit geöffnetem Archiv — `AttachImageAsync` ruft `IRecipeService.AddImageAsync` für passende Rezepttitel auf. Mock-Default `(false, null, null)` ist toleriert (nur `LogWarning`), Tests müssen das Setup trotzdem bewusst verdrahten, damit Assertions über Bild-Anhänge möglich sind.
- **Logger-Typ ändert sich:** `ILogger<DemoDataSeedingJobHandler>` wird zu `ILogger<DemoDataSeeder>` — bestehende `BuildProvider`-Testinfrastruktur muss angepasst werden.
- **Verhalten `DateTime.Today`:** Kalender-Datums-Assertions bleiben tagesgenau wie bisher (keine Änderung).
- Keine weiteren bekannten Seiteneffekte — öffentliche API-Oberfläche, Job-Typ, Auslöser und Dateninhalte unverändert.

## Umsetzungsreihenfolge

1. **`IDemoDataSeeder` + `DemoDataSeeder` anlegen**
   - Voraussetzungen: Keine (alle Abhängigkeiten vorhanden).
   - Beschreibung: Neue Dateien `Rezepte.Web/Services/IDemoDataSeeder.cs` und `DemoDataSeeder.cs`; Sequenzmethoden, Konstanten und `SeededRecipe` 1:1 aus dem Handler übernehmen (Konstruktor-Injektion statt `scopeServices`-Parameter; Logger als injiziertes Feld).

2. **`DemoDataSeedingJobHandler` reduzieren**
   - Voraussetzungen: Schritt 1.
   - Beschreibung: `HandleAsync` auf Payload-Parsing, Benutzerprüfung und `IDemoDataSeeder`-Auflösung/-Aufruf reduzieren; umgezogene Member entfernen.

3. **DI-Registrierung**
   - Voraussetzungen: Schritt 1.
   - Beschreibung: `AddScoped<IDemoDataSeeder, DemoDataSeeder>()` in `ServiceCollectionExtensions`.

4. **Tests umbauen**
   - Voraussetzungen: Schritte 1–3.
   - Beschreibung: Sequenz-Tests aus `DemoDataSeedingJobHandlerTests` in neue `DemoDataSeeder`-Testklassen verlagern (Mock-Landschaft ohne `ServiceProvider`); Handler-Tests auf Payload-Parsing, Benutzerprüfung, Delegation, Fehlerdurchreichung reduzieren.

5. **Build + Tests grün**
   - Voraussetzungen: Schritte 1–4.
   - Beschreibung: `dotnet build Rezepte.sln` und `dotnet test Rezepte.Tests` ausführen, Fehler beheben.

6. **Doku anpassen**
   - Voraussetzungen: Schritte 1–5.
   - Beschreibung: `Docs/help/user-accounts.md` Abschnitt „Technischer Hintergrund" an Service-Kapselung anpassen.

## Tests

### Neue Tests

| Test / Hilfsmethode | Testklasse | Was wird geprüft / bereitgestellt? |
|--------------------|------------|-------------------------------------|
| `SeedForUserAsync_HappyPath_CreatesFiveCookbooks` | `DemoDataSeederTests_Sequence` | 5× `ICookbookService.CreateAsync` mit `description: null` |
| `SeedForUserAsync_HappyPath_CreatesAllDemoRecipes` | `DemoDataSeederTests_Sequence` | 43× `IRecipeService.CreateAsync` mit `uri: null` |
| `SeedForUserAsync_HappyPath_CreatesFiveCalendarEvents` | `DemoDataSeederTests_Sequence` | 5× `ICalendarService.CreateEventAsync` (`Today+1..5`, 18:00, `RecurrenceType.None`, `WeekDays.None`) |
| `SeedForUserAsync_HappyPath_AddsIngredientsToDefaultShoppingList` | `DemoDataSeederTests_Sequence` | 6× `IShoppingListService.AddItemAsync` in der Default-Gruppe; `EnsureDefaultGroupAsync` aufgerufen |
| `SeedForUserAsync_WhenCookbookCreationFails_Throws` | `DemoDataSeederTests_Errors` | `InvalidOperationException` bei fehlschlagendem `ICookbookService.CreateAsync` |
| `SeedForUserAsync_WhenRecipeCreationFails_Throws` | `DemoDataSeederTests_Errors` | `InvalidOperationException` bei fehlschlagendem `IRecipeService.CreateAsync` |
| `SeedForUserAsync_WhenCalendarEventCreationFails_Throws` | `DemoDataSeederTests_Errors` | `InvalidOperationException` bei fehlschlagendem `ICalendarService.CreateEventAsync` |
| `SeedForUserAsync_WhenShoppingListItemFails_Throws` | `DemoDataSeederTests_Errors` | `InvalidOperationException` bei fehlschlagendem `IShoppingListService.AddItemAsync` |
| `SeedForUserAsync_WhenImageAttachFails_Continues` | `DemoDataSeederTests_Errors` | `AddImageAsync` liefert `ok: false` → kein Throw, Seeding läuft zu Ende (Kalender/Shopping verifiziert) |
| Mock-Fabrik `CreateHappyPathMocks` o. ä. | beide `DemoDataSeederTests_*` | Moq-Mocks der vier Domänen-Services + `ILogger<DemoDataSeeder>` (aus bisheriger `HappyPathMocks`-Landschaft übernommen, ohne `ServiceProvider`/`IUserService`) |

### Betroffene bestehende Tests

| Test / Testklasse | Grund der Anpassung |
|-------------------|---------------------|
| `DemoDataSeedingJobHandlerTests` (`HandleAsync_ShouldCreateFiveCookbooks`, `…FortyThreeRecipes`, `…FiveCalendarEvents`, `…AddIngredientsToDefaultShoppingList`, `…Throw_WhenAnyServiceOperationFails`) | Sequenz-Assertions ziehen in `DemoDataSeederTests_*` um; Handler enthält keine Sequenz mehr |
| `DemoDataSeedingJobHandlerTests` (neu/umgewidmet) | Verifiziert nur noch: `SeedForUserAsync` mit korrekter UserId aufgerufen; `InvalidOperationException` bei fehlendem Benutzer (Seeder **nicht** aufgerufen); `InvalidOperationException` bei leerer UserId; Exception des Seeders wird durchgereicht |
| `UserServiceTests` (Enqueue-Tests), `Rezepte.Tests.Browser/Auth/RegisterTests.cs` | Unverändert — Nachweis des unveränderten Auslösers bzw. E2E-Ergebnisses |

### E2E-Tests (primärer Funktionsnachweis)

Die Anforderung ändert keinen Benutzerfluss — Registrierung mit Demo-Daten bleibt fachlich identisch. Der bestehende E2E-Test `RegisterTests.Register_WithDemoData_CreatesDemoData` (Playwright, `Rezepte.Tests.Browser`) ist der verbindliche End-to-End-Nachweis, dass das sichtbare Ergebnis unverändert ist; er wird nach der Umsetzung ausgeführt. Neue E2E-Tests sind nicht erforderlich, da kein neuer oder geänderter Nutzer-Auslöser entsteht (Begründung: rein interne Verlagerung der Sequenz; der Auslöseweg über die Registrierung ist durch den bestehenden Test abgedeckt).

| Priorität | Szenario | Testdatei / Testklasse | Abgedecktes Akzeptanzkriterium | Warum E2E nötig ist |
|-----------|----------|------------------------|-------------------------------|---------------------|
| Pflicht | Registrierung mit Demo-Daten erzeugt unveränderte Inhalte | `Rezepte.Tests.Browser/Auth/RegisterTests.cs` (bestehend, unverändert) | AK 2, 3, 4 | Einziger Nachweis der kompletten Kette Registrierung → Job → Seeder → sichtbare Daten |

## Akzeptanzszenarien

|| # | Szenario | Auslöser | Ausgangszustand | Schritte | Sichtbare Wirkung | Nachweis |
||---|----------|----------|------------------|----------|-------------------|----------|
|| 1 | Happy Path: Registrierung mit Demo-Daten | Nutzer registriert sich über die UI mit aktivierter Checkbox „Demo-Daten anlegen" | Benutzer existiert noch nicht; `demo-images.zip` im Output vorhanden | `RegisterAsync` enqueut `seed-demo-data`-Job → `BackgroundJobHostedService` → `DemoDataSeedingJobHandler` → `IDemoDataSeeder.SeedForUserAsync` | Nach kurzer Wartezeit: 5 Kochbücher mit 43 Rezepten inkl. Bildern, 5 Kalenderevents 18:00 auf Folgetagen, Einkaufsliste in Standardgruppe gefüllt | `RegisterTests.Register_WithDemoData_CreatesDemoData` (bestehend) |
|| 2 | Benutzer der Job-Nutzlast existiert nicht | `seed-demo-data`-Job mit Payload eines nicht existierenden Benutzers | Benutzer wurde zwischen Enqueue und Ausführung gelöscht | `HandleAsync` → `GetByIdAsync` → `null` → `InvalidOperationException` | Job landet auf `Failed`; `SeedForUserAsync` wird nicht aufgerufen; kein Retry | `DemoDataSeedingJobHandlerTests` (Delegation/Fehler) |
|| 3 | `demo-images.zip` fehlt | Wie Szenario 1 | Archiv nicht im Output-Verzeichnis | Sequenz läuft, `OpenDemoImages` → `LogWarning` + `null` | Demo-Daten ohne Bilder, sonst unverändert; Job `Succeeded` | Unit-Test-Ebene abgedeckt durch toleranten `AttachImageAsync`-Pfad (Archiv-Verhalten unverändert übernommen); kein separater Test geplant — Logik 1:1 verschoben, fehlende Einzelbilder indirekt durch nicht-matchende Titel abgedeckt |
|| 4 | Domänenaufruf schlägt fehl | Wie Szenario 1 | z. B. `IRecipeService.CreateAsync` liefert `ok: false` | Sequenz bricht mit `InvalidOperationException` ab | Job `Failed`, Teildaten bleiben bestehen, kein Retry (unverändert) | `DemoDataSeederTests_Errors` |

Wartesituationen: Der Job läuft asynchron im Hintergrund; der Nutzer sieht die Daten „kurz nach der Anmeldung" (bestehendes Verhalten, `WaitForDemoDataAsync` pollt im E2E-Test die Datenbank). Wiederaufnahme: kein Retry — Fehler bleiben manuell sichtbar über den Job-Status (unverändert).

## Offene Punkte

Keine. Platzierung und Schnittstelle sind durch Anforderung + Konventionen festgelegt; das ZIP-Verhalten in Tests ist geklärt (Archiv vorhanden, `AddImageAsync` toleriert Fehler).
