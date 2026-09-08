# Logik – Bestandsaufnahme „Demo-Daten bei Registrierung"

## `Register.razor` (UI)
Datei: `Rezepte.Web/Components/Pages/Register.razor`

- Route `/register`; `EditForm` postet per Browser-Formular (`Action="api/auth/register"`, `Method="post"`, `FormName="register"`).
- Felder: `Username`, `Email`, `Password` — Übertragung erfolgt über `name`-Attribute direkt an den API-Endpunkt.
- Fehleranzeige über Query-Parameter `error` (`[SupplyParameterFromQuery]`).
- Internes `RegisterModel` (Zeilen 48–53) mit `Username`, `Email`, `Password`.
- **Keine Checkbox / kein Feld für Demo-Daten vorhanden.**

## `AuthController`
Datei: `Rezepte.Web/Controllers/AuthController.cs`

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `Register(CancellationToken)` | `public` (Route `POST api/auth/register`) | Liest `Username`, `Password`, `Email` aus Formular (`Request.ReadFormAsync`, Zeilen 41–45) oder JSON-Body (`RegisterRequest`, Zeilen 48–54). Validiert Pflichtfelder und Mindestlänge (6 Zeichen). Ruft `_userService.RegisterAsync(username, password, ct)` (Zeile 76) — **ohne Demo-Daten-Parameter**. Bei Form-Post: `LocalRedirect("/login")` bzw. Redirect nach `/register?error=…`; bei JSON: `Ok(AuthResponse)` / `BadRequest`. |
| `RedirectToRegisterError(string)` | `private` | Hilfsmethode für Fehler-Redirect |
| `RegisterRequestForm` (Record) | `public` | Deklariert, aber ungenutzt; keine `CreateDemoData`-Eigenschaft |

Attribute: `[ApiController]`, `[Route("api/[controller]")]`, `[IgnoreAntiforgeryToken]`, `[EnableRateLimiting(RateLimitPolicies.Authentication)]`.

## `UserService`
Datei: `Rezepte.Web/Services/UserService.cs`

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `RegisterAsync(username, password, ct)` | `public` | Validiert Username via `IUsernameValidator`, prüft Eindeutigkeit, setzt `IsAdmin` für ersten Benutzer, hasht Passwort via `PasswordHasher.Hash`, persistiert `Entities.User` (Zeilen 129–150). **Kein `createDemoData`-Parameter, kein Job-Enqueue.** |
| `LoginAsync` | `public` | Prüft Passwort, opportunistischer Rehash |
| `FindByUsernameAsync` / `HasAnyUsersAsync` / `GetByIdAsync` | `public` | Lesemethoden |
| `UpdateProfileAsync` / `ChangePasswordAsync` | `public` | Profil-/Passwortverwaltung |
| `GetAllAsync` / `UpdateUserAsync` / `DeleteAsync` | `public` | Admin-Funktionen |

Abhängigkeiten: `RezepteDbContext`, `IUsernameValidator`. Keine Abhängigkeit zu `IBackgroundJobQueue`.

## `CookbookService`
Datei: `Rezepte.Web/Services/CookbookService.cs`

`CreateAsync(userId, name, description, ct)` (Zeilen 126–149): erfordert Name ≥ 3 Zeichen, setzt `OrderIndex = maxIndex + 1`, `UserId = userId`. Geeignet zum Anlegen der fünf Demo-Kochbücher.

## `RecipeService`
Datei: `Rezepte.Web/Services/RecipeService.cs`

`CreateAsync(userId, cookbookId, title, description, uri, portions, steps, ct)` bzw. Überladung mit `sideDishRecipeIds` (Zeilen 404/425): legt `Recipe` mit `RecipeStep`s/`RecipeIngredient`s an und ordnet es via `RecipeCookbook` dem Kochbuch zu. `AddExistingToCookbookAsync` ordnet bestehende Rezepte zu.

## `CalendarService`
Datei: `Rezepte.Web/Services/CalendarService.cs`

`CreateEventAsync(userId, recipeId, startDate, timeOfDay, portions, recurrence, recurrenceDays, ct)` (ab Zeile 75): validiert `userId` und `portions > 0`; lädt bei angegebenem `recipeId` das Rezept via `IRecipeService`. Geeignet für die fünf Kalendereinträge.

## `ShoppingListService`
Datei: `Rezepte.Web/Services/ShoppingListService.cs`

`EnsureDefaultGroupAsync(userId, ct)` stellt Standardgruppe „Einkaufsliste" sicher; `AddItemAsync(userId, groupId, amount, unit, name, ct)` fügt Artikel hinzu; `AddRecipeIngredientsAsync(userId, recipeId, ingredientIds, ct)` / `AddRecipeIngredientGroupsAsync` legen rezeptbezogene Gruppen an.

## `BackgroundJobQueue`
Datei: `Rezepte.Web/Services/BackgroundJobs/BackgroundJobQueue.cs`

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `EnqueueAsync(jobType, payloadJson, initiatorUserId, ct)` | `public` | Erzeugt `BackgroundJob` in eigenem DI-Scope (persistiert sofort), schreibt Job-Id in Bounded `Channel<Guid>` (Kapazität 100, `FullMode.Wait`) |
| `GetJobAsync` | `public` | Liest Job aus DB |
| `Reader` | `internal` | ChannelReader für den HostedService |

Registriert als Singleton über `JobQueueServiceCollectionExtensions.AddBackgroundJobQueue()` (aufgerufen in `ServiceCollectionExtensions.cs` Zeile 235).

## `BackgroundJobHostedService`
Datei: `Rezepte.Web/Services/BackgroundJobs/BackgroundJobHostedService.cs`

- `ExecuteAsync`: liest Job-Ids aus dem Channel und ruft `ProcessJobAsync`.
- `ProcessJobAsync` (Zeilen 56–117): erstellt **pro Job einen neuen `IServiceScope`**, lädt den Job aus `RezepteDbContext`, setzt `Running`/`StartedAt`, löst alle `IBackgroundJobHandler` im Scope auf, wählt per `JobType` (OrdinalIgnoreCase), ruft `handler.HandleAsync(job, scope.ServiceProvider, ct)` und setzt abschließend `Succeeded`/`Failed`/`Cancelled` inkl. `Error`-Text. Kein Retry-Mechanismus.

## Vorhandene Job-Handler
- `ExportUserJobHandler` — `JobType => "export:user"` (`Rezepte.Web/Services/BackgroundJobs/Handlers/ExportUserJobHandler.cs`). Löst Scoped-Services aus `scopeServices`, parst `PayloadJson` (`ExportJobPayload.FromJson`), nutzt `job.InitiatorUserId` als Zielbenutzer, aktualisiert `job.Progress`/`ResultMessage`. Dient als Muster für einen `DemoDataSeedingJobHandler`.
- `ExportAllJobHandler` — `JobType => "export:all"` (`.../Handlers/ExportAllJobHandler.cs`).

## DI-Registrierung
Datei: `Rezepte.Web/Extensions/ServiceCollectionExtensions.cs`

- Zeilen 187–189: `IUserService`, `ICookbookService`, `IRecipeService` (Scoped)
- Zeilen 222–223: `ICalendarService`, `IShoppingListService` (Scoped)
- Zeilen 197–198: `IBackgroundJobHandler`-Implementierungen `ExportUserJobHandler`, `ExportAllJobHandler` (Scoped)
- Zeile 235: `services.AddBackgroundJobQueue()` — registriert `BackgroundJobQueue` (Singleton + `IBackgroundJobQueue`) und `BackgroundJobHostedService` (Hosted Service), siehe `Rezepte.Web/Extensions/JobQueueServiceCollectionExtensions.cs`.

## Aufrufer von `IUserService.RegisterAsync`
- `AuthController.Register` (Zeile 76) — einziger Aufruf im Produktivcode.
- Tests: `UserServiceTests` (mehrere Aufrufe mit 3 Argumenten), `AuthControllerTests` (Moq-Setup mit 3 Argumenten).
