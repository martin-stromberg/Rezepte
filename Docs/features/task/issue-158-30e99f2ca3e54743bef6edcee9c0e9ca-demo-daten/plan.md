# Umsetzungsplan: Demo-Daten bei Registrierung

## Übersicht

Bei der Registrierung kann ein neuer Anwender über eine Checkbox in `Register.razor` wählen, ob Demo-Daten angelegt werden. Bei positivem Flag enqueut `UserService` nach erfolgreicher Registrierung über `IBackgroundJobQueue` einen Hintergrundjob, der fünf Kochbücher, 43 Rezepte, fünf Kalendereinträge und Einkaufslisteneinträge im Kontext des neuen Benutzers anlegt. Die Umsetzung erweitert die Authentifizierungs-DTOs, den `AuthController`, `IUserService`/`UserService` sowie die Registrierungsseite und fügt einen neuen `IBackgroundJobHandler` mit einer statischen Demo-Datenquelle hinzu.

## Designentscheidungen

| Komponente / Bereich | Gewählter Ansatz | Begründung |
|----------------------|------------------|------------|
| Demo-Datenquelle | Statische C#-Klasse `DemoDataSource` mit `IReadOnlyList<DemoCookbook>` und `IReadOnlyList<RecipeCreateStep>`-basierten Rezepten | Compile-Time-Typisierung, keine Laufzeit-Deserialisierung, keine Datei-I/O, direkte Wiederverwendung der bestehenden `RecipeCreateStep`/`RecipeCreateIngredient`-DTOs (siehe `Rezepte.Web/Services/RecipeService.cs` Zeilen 251/261) |
| Seeding-Scope im Handler | Verwendet das vom `BackgroundJobHostedService` bereitgestellte `IServiceProvider scopeServices` direkt (vgl. `ExportUserJobHandler`) | Bestehendes Muster, vermeidet unnötige verschachtelte `IServiceScopeFactory`-Scopes und doppelte `DbContext`-Instanzen |
| Kalender-Auswahl | Erste fünf Rezepte aus dem Kochbuch "Abendessen" in der im `DemoDataSource` definierten Reihenfolge | Aus der Anforderung abgeleitet; deterministisch und planbar |
| Einkaufslisten-Auswahl | Rezept des ersten Kalendereintrags für `DateTime.Today.AddDays(1)`; alle Zutaten dieses Rezepts in die Standardgruppe | Einfache direkte Ableitung aus dem zuvor angelegten Kalender |
| Globaler Feature-Schalter | Kein zentraler Schalter; Steuerung ausschließlich über das `CreateDemoData`-Flag | Anforderung macht keine Konfiguration erforderlich; minimiert Konfigurationsoberfläche |
| Auswahl-/Identifikationsaufgaben | Keine benannte Entitäten zur Auswahl; es werden lediglich feste Demo-Daten angelegt | Keine Such-/Dropdown-Komponente nötig |
| E2E-Job-Warte-Logik | Polling der sichtbaren UI-Listen (`/cookbooks`, `/recipes/search`, `/calendar`, `/shopping-list`) nach Login, begrenzt durch Timeout; alternativ kann `GET /api/jobs/{jobId}` nach Login gepollt werden, wenn die Test-Infrastruktur die Job-Id übermittelt bekommt | Der asynchrone Hintergrundjob hat keinen synchronen Rückkanal ins UI; sichtbare Ergebnisse sind der zuverlässigste End-2-End-Nachweis |

## Programmabläufe

### Registrierungsfluss mit Demo-Daten

1. Benutzer wählt auf `Register.razor` die Checkbox "Demo-Daten anlegen" (`CreateDemoData`, Default `false`).
2. Das Formular posted per `EditForm` (Action `api/auth/register`, Method `post`) die Felder `Username`, `Email`, `Password` und `createDemoData`.
   Alternativ sendet ein Client `POST api/auth/register` mit JSON-Body `RegisterRequest`, das `CreateDemoData` enthält.
3. `AuthController.Register` liest `createDemoData` aus `Request.Form` (Form-Post) bzw. `RegisterRequest.CreateDemoData` (JSON-Body).
4. `AuthController.Register` ruft `IUserService.RegisterAsync(username, password, createDemoData, ct)` auf.
5. `UserService.RegisterAsync` validiert den Benutzernamen, prüft Eindeutigkeit, legt `User` an und persistiert ihn.
6. Ist `createDemoData == true` und die Registrierung erfolgreich, enqueut `UserService` über `IBackgroundJobQueue.EnqueueAsync` einen Job mit `JobType = "seed-demo-data"` und Payload `DemoDataSeedPayload { UserId = user.Id }`.
7. Form-Post führt den Browser zu `/login` weiter; JSON-Endpunkt liefert `AuthResponse`.

Beteiligte Klassen/Komponenten: `Register.razor`, `AuthController`, `RegisterRequest`, `RegisterRequestForm`, `IUserService`, `UserService`, `IBackgroundJobQueue`, `BackgroundJobQueue`.

### Demo-Daten-Seeding im Hintergrundjob

1. `BackgroundJobHostedService` liest die Job-Id aus dem Channel und ruft `ProcessJobAsync` auf.
2. `ProcessJobAsync` erstellt einen neuen DI-Scope, lädt den `BackgroundJob`, setzt `Running`/`StartedAt` und wählt `DemoDataSeedingJobHandler` anhand `JobType`.
3. `DemoDataSeedingJobHandler.HandleAsync` parst `BackgroundJob.PayloadJson` in `DemoDataSeedPayload` und ermittelt `UserId`.
4. Der Handler validiert, dass der Benutzer existiert (`IUserService.GetByIdAsync(userId, ct)`); bei Fehlen wird eine Exception geworfen, die der Hosted Service als `Failed` protokolliert.
5. Für jeden festen Kochbuch-Namen (`"Frühstück"`, `"Abendessen"`, `"Snacks"`, `"Weihnachtszeit"`, `"Grillsaison"`) wird `ICookbookService.CreateAsync(userId, name, description, ct)` aufgerufen und das erzeugte `Cookbook` in einer Map `Name -> Id` gespeichert.
6. Für jedes `DemoRecipe` in `DemoDataSource` wird `IRecipeService.CreateAsync(userId, cookbookId, title, description, uri, portions, steps, ct)` aufgerufen, wobei `cookbookId` über `DemoRecipe.CookbookName` aus der Map ermittelt wird.
7. Für die ersten fünf Rezepte im Kochbuch "Abendessen" werden `ICalendarService.CreateEventAsync(userId, recipeId, startDate, timeOfDay, portions, RecurrenceType.None, WeekDays.None, ct)` für `startDate = DateTime.Today.AddDays(1)` bis `AddDays(5)` und `timeOfDay = TimeSpan.FromHours(18)` aufgerufen.
8. Für das Rezept des ersten dieser fünf Kalendereinträge (Tag +1) wird `IShoppingListService.EnsureDefaultGroupAsync(userId, ct)` abgefragt und für jede Zutat dieses Rezepts `IShoppingListService.AddItemAsync(userId, group.Id, amount, unit, name, ct)` aufgerufen.
9. Liefert ein Service-Aufruf `(ok: false, ...)` zurück, bricht der Handler ab und wirft eine Exception, sodass `BackgroundJobHostedService` den Job auf `Failed` setzt.
10. Bei Erfolg setzt `BackgroundJobHostedService` den Job auf `Succeeded`/`CompletedAt`.

Beteiligte Klassen/Komponenten: `BackgroundJobHostedService`, `DemoDataSeedingJobHandler`, `DemoDataSeedPayload`, `DemoDataSource`, `IUserService`, `ICookbookService`, `IRecipeService`, `ICalendarService`, `IShoppingListService`, `RezepteDbContext`.

## Neue Klassen

| Klasse | Typ | Zweck |
|--------|-----|-------|
| `DemoDataSeedingJobHandler` | Klasse (`IBackgroundJobHandler`) | Führt das Anlegen der Demo-Daten im Hintergrundjob aus |
| `DemoDataSeedPayload` | Record | JSON-Payload für den Job; enthält `UserId` |
| `DemoDataSource` | Statische Klasse | Zentraler Read-Only-Halter der 43 Demo-Rezepte, Kochbücher und Einkaufslisten-Zuordnung |
| `DemoCookbook` | Record | Name einer Demo-Kochbuchgruppe und deren Rezepte |
| `DemoRecipe` | Record | Demo-Rezept mit `Title`, `Description`, `Portions`, `CookbookName` und `Steps` (`IReadOnlyList<RecipeCreateStep>`) |

## Änderungen an bestehenden Klassen

### `Register.razor` (Razor-Komponente)

- **Neue Eigenschaft im `RegisterModel`:** `CreateDemoData` (`bool`, Default `false`) — speichert Zustand der Checkbox.
- **Neues UI-Element:** `<InputCheckbox name="createDemoData" ... />` bzw. HTML-Äquivalent, das den bestehenden Formular-Post (`name`-Attribute an `api/auth/register`) nutzt.

### `AuthController` (Controller)

- **Geänderte Methode `Register`:** Liest `createDemoData` aus `Request.Form` (Form-Post) und `RegisterRequest.CreateDemoData` (JSON-Body); übergibt es als drittes Argument an `IUserService.RegisterAsync`.
- **Neues Feld im Record `RegisterRequestForm`:** `bool CreateDemoData` (Default `false`) — fachliches Gegenstück zu `RegisterRequest`, deklariert für Konsistenz.

### `RegisterRequest` (DTO)

- **Neue Eigenschaft:** `bool CreateDemoData` (Default `false`) — übernimmt das Flag im JSON-Body.

### `IUserService` / `UserService` (Service)

- **Geänderte Methode `RegisterAsync`:** Signatur erweitert zu `RegisterAsync(string username, string password, bool createDemoData, CancellationToken ct)`.
- **Neue Dependency:** `IBackgroundJobQueue` (über Konstruktor).
- **Geänderte Methode `RegisterAsync`:** Bei erfolgreicher Erstellung eines neuen Benutzers und `createDemoData == true` wird `IBackgroundJobQueue.EnqueueAsync("seed-demo-data", payloadJson, user.Id, ct)` aufgerufen.

### `ServiceCollectionExtensions` (Erweiterungsmethoden)

- **Neue Registrierung:** `services.AddScoped<IBackgroundJobHandler, DemoDataSeedingJobHandler>();` hinzufügen.

## Datenbankmigrationen

Keine. Es werden bestehende Tabellen (`Users`, `Cookbooks`, `Recipes`, `RecipeCookbooks`, `CalendarEvents`, `ShoppingListGroups`, `ShoppingListItems`, `BackgroundJobs`) und die bestehende `BackgroundJob`-Infrastruktur genutzt.

## Validierungsregeln

| Feld / Objekt | Regel | Fehlerfall |
|---------------|-------|------------|
| `createDemoData` (Formular) | Wert wird via `bool.TryParse` gelesen; fehlender oder unlesbarer Wert gilt als `false` | Kein Hard-Fail; ungültige Eingaben werden als `false` behandelt |
| `RegisterRequest.CreateDemoData` | Optionales `bool` mit Default `false` | Model-Binding-Fehler bei nicht-boolschen Werten (400 Bad Request via `[ApiController]`) |
| `DemoDataSeedPayload.UserId` | Nicht leer und Benutzer muss existieren | Job wird auf `Failed` gesetzt |
| Service-Ergebnisse im `DemoDataSeedingJobHandler` | `ok == false` jeder einzelnen Service-Operation bricht den Job ab | Job `Failed` mit Fehlertext |

## Konfigurationsänderungen

Keine. Die Steuerung erfolgt ausschließlich über das `CreateDemoData`-Flag der Registrierung.

## Seiteneffekte und Risiken

- **Bestehende Tests:** `UserServiceTests` rufen `RegisterAsync` mehrfach mit drei Parametern auf; alle Aufrufe und der `CreateSut`-Helper müssen angepasst werden. `AuthControllerTests` muss Moq-Setups für `IUserService.RegisterAsync` aktualisieren.
- **Binary-Size:** 43 statische Rezepte vergrößern die Assembly um einen merkbaren, aber unkritischen Datenblock.
- **Datenqualität:** Demo-Daten werden über die regulären Service-Methoden angelegt und sind nicht von manuell erfassten Daten unterscheidbar; es gibt keine Gruppenkennzeichnung für späteres Löschen.
- **Bilddaten:** Demo-Rezepte enthalten keine Bilder; die UI zeigt ggf. Platzhalter oder leere Bildbereiche.
- **Asynchronität:** Der Seeding-Job läuft im Hintergrund; E2E-Tests benötigen daher eine Warte- und Poll-Strategie, bis die Daten in den UI-Listen sichtbar werden.

## Umsetzungsreihenfolge

1. **DTOs erweitern**
   - Voraussetzungen: Keine
   - Beschreibung: Füge `CreateDemoData` zu `RegisterRequest` und `RegisterRequestForm` hinzu.

2. **Demo-Datenquelle anlegen**
   - Voraussetzungen: `RecipeCreateStep`/`RecipeCreateIngredient` (vorhanden in `Rezepte.Web/Services/RecipeService.cs`)
   - Beschreibung: Erstelle `DemoDataSource`, `DemoCookbook` und `DemoRecipe` mit den 43 Demo-Rezepten, Schritten und Zutaten.

3. **Job-Payload anlegen**
   - Voraussetzungen: Keine
   - Beschreibung: Erstelle `DemoDataSeedPayload`.

4. **Hintergrundjob-Handler implementieren**
   - Voraussetzungen: `DemoDataSource`, `DemoDataSeedPayload`, `IBackgroundJobHandler`-Interface, `ICookbookService`, `IRecipeService`, `ICalendarService`, `IShoppingListService`, `IUserService`
   - Beschreibung: Implementiere `DemoDataSeedingJobHandler` und registriere ihn in `ServiceCollectionExtensions`.

5. **`IUserService`/`UserService` erweitern**
   - Voraussetzungen: `DemoDataSeedingJobHandler` und `IBackgroundJobQueue` (vorhanden)
   - Beschreibung: Erweitere `RegisterAsync` um `bool createDemoData` und enqueue den Seeding-Job bei `true`.

6. **`AuthController` erweitern**
   - Voraussetzungen: `IUserService.RegisterAsync` mit neuem Signatur
   - Beschreibung: Lese `createDemoData` aus Formular/JSON und übergib es an `UserService`.

7. **UI-Seite ergänzen**
   - Voraussetzungen: `AuthController` liest das Feld
   - Beschreibung: Füge Checkbox in `Register.razor` hinzu und binde sie an `RegisterModel.CreateDemoData` (direktes `name`-Attribut).

8. **Unit-Tests anpassen und ergänzen**
   - Voraussetzungen: Implementierte Klassen
   - Beschreibung: Passe `UserServiceTests` und `AuthControllerTests` an; füge `DemoDataSeedingJobHandlerTests` inkl. Fehlerpfaden hinzu.

9. **E2E-Fixtures und Page-Objects anlegen**
   - Voraussetzungen: Playwright-Infrastruktur vorhanden (`Rezepte.Tests.Browser/Infrastructure`)
   - Beschreibung: Lege `RegisterPage`-Page-Object, Unique-Username-Hilfsmethode und `DemoDataWaitHelper` an.

10. **E2E-Tests ergänzen**
    - Voraussetzungen: Laufende Anwendung, Playwright-Infrastruktur, E2E-Fixtures
    - Beschreibung: Füge `Register_WithDemoData` und `Register_WithoutDemoData` in `Rezepte.Tests.Browser/Auth/RegisterTests.cs` hinzu.

11. **Testlauf abschließen**
    - Voraussetzungen: Alle Änderungen
    - Beschreibung: Führe `Rezepte.Tests` und `Rezepte.Tests.Browser` aus und behebe Fehler.

## Tests

### Neue Tests

| Test / Hilfsmethode | Testklasse | Was wird geprüft / bereitgestellt? |
|---------------------|------------|------------------------------------|
| `RegisterAsync_ShouldEnqueueDemoDataJob_WhenCreateDemoDataIsTrue` | `UserServiceTests` | `UserService` enqueued `seed-demo-data`-Job, wenn Flag `true` |
| `RegisterAsync_ShouldNotEnqueueDemoDataJob_WhenCreateDemoDataIsFalse` | `UserServiceTests` | Kein Job enqueued, wenn Flag `false` |
| `Register_ShouldCallUserServiceWithCreateDemoDataTrue_WhenFormCheckboxChecked` | `AuthControllerTests` | `AuthController` übergibt `true` bei Form-Post mit Checkbox |
| `Register_ShouldCallUserServiceWithCreateDemoDataFalse_WhenFormCheckboxUnchecked` | `AuthControllerTests` | `AuthController` übergibt `false` bei fehlendem/unchecked Feld |
| `Register_ShouldCallUserServiceWithCreateDemoDataFromJson_WhenProvided` | `AuthControllerTests` | JSON-Body `CreateDemoData` wird korrekt durchgereicht |
| `HandleAsync_ShouldCreateFiveCookbooks` | `DemoDataSeedingJobHandlerTests` | Fünf Kochbücher werden angelegt |
| `HandleAsync_ShouldCreateFortyThreeRecipes` | `DemoDataSeedingJobHandlerTests` | 43 Rezepte werden angelegt und Kochbüchern zugeordnet |
| `HandleAsync_ShouldCreateFiveCalendarEvents` | `DemoDataSeedingJobHandlerTests` | Fünf Kalendereinträge für `Tag+1` bis `Tag+5` |
| `HandleAsync_ShouldAddIngredientsToDefaultShoppingList` | `DemoDataSeedingJobHandlerTests` | Zutaten des Rezepts für `Tag+1` in der Einkaufsliste |
| `HandleAsync_ShouldThrow_WhenUserDoesNotExist` | `DemoDataSeedingJobHandlerTests` | Nicht existenter Benutzer wirft Exception; `BackgroundJobHostedService` setzt Job auf `Failed` |
| `HandleAsync_ShouldThrow_WhenAnyServiceOperationFails` | `DemoDataSeedingJobHandlerTests` | `ok == false` einer beliebigen Service-Operation wirft Exception; Job endet mit `Failed` |
| `RegisterPage` | `Rezepte.Tests.Browser/Auth/RegisterPage.cs` (neu) | Page-Object für `/register` (`GotoAsync`, `FillUsernameAsync`, `FillEmailAsync`, `FillPasswordAsync`, `CheckCreateDemoDataAsync`, `SubmitAsync`, `GetErrorAsync`) |
| `UniqueUsernameGenerator` | `Rezepte.Tests.Browser/Auth/RegisterTests.cs` (lokale Hilfsmethode) | Erzeugt pro Testlauf eindeutigen `demo_e2e_{guid}_{timestamp}`-Benutzer, um parallele Browser-Tests nicht zu verfälschen |
| `DemoDataWaitHelper` | `Rezepte.Tests.Browser/Auth/RegisterTests.cs` (lokale Hilfsmethode) | Pollt die relevanten UI-Listen und/oder `GET /api/jobs/{jobId}` nach Login, bis der asynchrone Seeding-Job innerhalb des Timeouts sichtbare Ergebnisse produziert |

### Betroffene bestehende Tests

| Test / Testklasse | Grund der Anpassung |
|-------------------|---------------------|
| `UserServiceTests` | Alle `RegisterAsync`-Aufrufe und `CreateSut`-Setups müssen vier statt drei Argumente übergeben |
| `AuthControllerTests` | Moq-Setup und Aufrufe für `IUserService.RegisterAsync` erweitern sich um `bool createDemoData` |

### E2E-Tests (primärer Funktionsnachweis)

Für jede neue oder geänderte Benutzerinteraktion muss E2E-Abdeckung geplant werden. E2E-Tests sind bei Benutzerflüssen der wichtigste Nachweis, dass die Anwendung tatsächlich funktioniert; Unit- und Integrationstests ergänzen diese Abdeckung, ersetzen sie aber nicht.

Der Happy Path jedes neuen oder geänderten Benutzerflusses muss durch einen E2E-Test abgedeckt sein. Fehlerfälle, Berechtigungs-/Sichtbarkeitsregeln und kritische Edge Cases müssen ebenfalls als E2E-Szenarien geplant werden, sofern sie für Anwender sichtbar oder über die UI auslösbar sind.

| Priorität | Szenario | Testdatei / Testklasse | Abgedecktes Akzeptanzkriterium | Warum E2E nötig ist |
|-----------|----------|------------------------|-------------------------------|-------------------|
| Pflicht | **Setup:** Frische Browser-Test-Instanz mit leerem SQLite-DB. Eindeutiger Username `demo_e2e_{guid}` generieren. Navigiere zu `/register`. **Aktion:** Fülle `Username`, `Email` und `Password`; aktiviere die Checkbox `[name="createDemoData"]`. Sende das Formular ab. Warte auf Weiterleitung zu `/login`, melde dich mit dem neuen Account an. Warte über `DemoDataWaitHelper`, bis der Hintergrundjob abgeschlossen ist (Polling der Seiten oder `GET /api/jobs/{jobId}`). **Sichtbares Ergebnis:** Auf `/cookbooks` sind genau 5 Karten sichtbar; `/recipes/search` zeigt 43 Rezepte; `/calendar` enthält für `Tag+1` bis `Tag+5` je einen Eintrag; `/shopping-list` zeigt mindestens einen Eintrag in der Standardgruppe. | `Rezepte.Tests.Browser/Auth/RegisterTests.cs` (neu) | Demo-Daten werden nach Registrierung asynchron angelegt und sind über die UI sichtbar | Der vollständige Fluss (UI-Checkbox → API → Hintergrundjob → UI-Listen) lässt sich nicht durch Unit-Tests abbilden |
| Pflicht | **Setup:** Frische Browser-Test-Instanz, eindeutiger Username. Navigiere zu `/register`. **Aktion:** Fülle `Username`, `Email` und `Password`; lasse `CreateDemoData` deaktiviert. Sende das Formular ab, melde dich an. **Sichtbares Ergebnis:** `/cookbooks` zeigt den Hinweis "NoCookbooksYet" (0 Karten); `/recipes/search` liefert keine Ergebnisse; `/calendar` zeigt keine Termine; `/shopping-list` zeigt keine Artikel. | `Rezepte.Tests.Browser/Auth/RegisterTests.cs` (neu) | Demo-Daten-Seeding ist optional und deaktivierbar | Negative Pfad des UI-Flusses, der über Unit-Tests nicht abgedeckt ist |

Welche bestehenden E2E-Tests müssen angepasst werden?

| Test / Testklasse | Grund der Anpassung |
|-------------------|---------------------|
| — | Keine bestehenden E2E-Tests betroffen |

## Offene Punkte

Keine. Alle in der Anforderung aufgeführten offenen Fragen wurden vom Auftraggeber bestätigt (erste fünf "Abendessen"-Rezepte für den Kalender; Rezept des ersten Kalendereintrags für `Tag+1` für die Einkaufsliste; JSON-API unterstützt `CreateDemoData`; Fehler werden geloggt und der Job auf `Failed` gesetzt, ohne automatischen Retry; Demo-Rezepte enthalten keine Bilder; Demo-Daten sind reguläre Datensätze ohne Gruppenlöschfunktion; Hintergrundjob wird in die Warteschlange eingereiht).
