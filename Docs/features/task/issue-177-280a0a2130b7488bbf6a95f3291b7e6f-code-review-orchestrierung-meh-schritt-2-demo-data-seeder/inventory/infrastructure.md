# Detail: Job-Infrastruktur, Auslöser und DI

## `DemoDataSeedPayload`

Datei: `Rezepte.Web/Services/BackgroundJobs/DemoDataSeedPayload.cs`

- `record DemoDataSeedPayload(string UserId)`; `ToJson()` (Zeile 18); `FromJson(string?)` (Zeile 26): leerer JSON → leere `UserId`; ungültiges JSON → `InvalidOperationException`.

## `IBackgroundJobHandler` / Handler-Auflösung

- `Rezepte.Web/Services/BackgroundJobs/IBackgroundJobHandler.cs:7–26` — `string JobType` + `Task HandleAsync(BackgroundJob job, IServiceProvider scopeServices, CancellationToken ct)`.
- `Rezepte.Web/Services/BackgroundJobs/BackgroundJobHostedService.cs:56–117` — eigener DI-Scope je Job (60), Handlerauflösung via `GetServices<IBackgroundJobHandler>()` + `JobType`-Match (84–89), Status-Transitions `Pending`→`Running`→`Succeeded`/`Failed`/`Cancelled`; Exception im Handler → `Failed` (109–115), kein Retry.

## `IBackgroundJobQueue`

- `Rezepte.Web/Services/BackgroundJobs/IBackgroundJobQueue.cs:20` — `Task<Guid> EnqueueAsync(string jobType, string? payloadJson = null, string? initiatorUserId = null, CancellationToken ct = default)`; `GetJobAsync` (31).
- `Rezepte.Web/Services/BackgroundJobs/BackgroundJobQueue.cs` — persistiert `BackgroundJob`-Row + Channel-Write. Verdrahtung via `services.AddBackgroundJobQueue()` (`ServiceCollectionExtensions.cs:236`), das auch `BackgroundJobHostedService` registriert.

## Auslöser: `UserService.RegisterAsync`

Datei: `Rezepte.Web/Services/UserService.cs`

- `IUserService.RegisterAsync(username, password, createDemoData, ct) → (bool ok, string? error, User? user)` — Interface Zeile 36; `IUserService.GetByIdAsync(id, ct) → Task<User?>` — Interface Zeile 68.
- Implementierung Zeilen 133–162: Username-Validierung, Duplikat-Check, `isFirst` → `IsAdmin`, `SaveChangesAsync`, **Job-Anlage 154–159**: `new DemoDataSeedPayload(user.Id)` + `_backgroundJobQueue.EnqueueAsync(DemoDataSeedingJobHandler.JobTypeName, payload.ToJson(), user.Id, ct)`.
- Abhängigkeiten: `RezepteDbContext`, `IUsernameValidator`, `IBackgroundJobQueue` (Zeile 125).
- Aufrufer: `AuthController.cs:79` (`createDemoData` aus Form/JSON), `AdminUsersController.cs:53` (immer `false`).

## DI-Registrierungen (`Rezepte.Web/Extensions/ServiceCollectionExtensions.cs`)

| Zeile | Registrierung |
|-------|---------------|
| 187 | `services.AddScoped<IUserService, UserService>();` |
| 188 | `services.AddScoped<ICookbookService, CookbookService>();` |
| 189 | `services.AddScoped<IRecipeService, RecipeService>();` |
| 197 | `services.AddScoped<IBackgroundJobHandler, ExportUserJobHandler>();` |
| 198 | `services.AddScoped<IBackgroundJobHandler, ExportAllJobHandler>();` |
| 199 | `services.AddScoped<IBackgroundJobHandler, DemoDataSeedingJobHandler>();` |
| 223 | `services.AddScoped<ICalendarService, CalendarService>();` |
| 224 | `services.AddScoped<IShoppingListService, ShoppingListService>();` |

Alle für die Sequenz benötigten Services sind scoped — ein `IDemoDataSeeder` kann sie direkt per Konstruktor injizieren.

## Weitere Handler (legitime Orchestrierung, unverändert)

- `ExportUserJobHandler`, `ExportAllJobHandler` — `Rezepte.Web/Services/BackgroundJobs/Handlers/`.

## Namens-/Platzierungskonvention Demo-Artefakte

Flach unter `Rezepte.Web/Services/`: `DemoDataSource.cs`, `DemoCookbook.cs`, `DemoRecipe.cs` (Namespace `Rezepte.Web.Services`). Interface + Implementierung liegen als Geschwisterdateien (`ICalendarService.cs`/`CalendarService.cs`, `IUserService.cs`/`UserService.cs` — letztere in derselben Datei teils; `IRecipeService.cs`/`RecipeService.cs` getrennt). Kein bestehender `DemoData/`-Unterordner.
