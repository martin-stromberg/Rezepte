# Detail: csproj-xmldoc-check --all (Exit 1)

## A) Unvollständige XML-Dokumentation in `.cs`-Dateien (24 Dateien)

| Datei | Befund |
|-------|--------|
| `Rezepte.Web/Configuration/LoadingBarOptions.cs` | Z. 35 `Empty`: `<returns>` fehlt; Z. 48 `ReadOnlyCollection`: `<param>` für `DefaultColorsArray` + `<returns>` fehlen |
| `Rezepte.Web/Configuration/LoadingBarSettings.cs` | Z. 3 `LoadingBarSettings`: `<returns>` fehlt |
| `Rezepte.Web/Controllers/AdminExportCleanupController.cs` | Z. 34 `GetSettings` (`ct` + returns); Z. 44 `UpdateSettings` (`request`, `ct` + returns); Z. 60 `Run` (`ct` + returns) |
| `Rezepte.Web/Controllers/AdminExportsController.cs` | Z. 27 `ExportAll` (`includeImages`, `includePdf`, `ct` + returns); Z. 63 `Restore` (`file`, `ct` + returns) |
| `Rezepte.Web/Controllers/AdminUsersController.cs` | Z. 8 Ktor (`users` + returns); Z. 18 `GetAll`, Z. 30 `Create`, Z. 60 `Update`, Z. 74 `Delete` (je returns); Z. 87 `CreateUserRequest` (`Username`, `Email`, `Password`, `IsAdmin` + returns); Z. 90 `UpdateUserRequest` (`Username`, `Email`, `IsAdmin` + returns) |
| `Rezepte.Web/Controllers/AuthController.cs` | Z. 9 Ktor (`userService` + returns); Z. 90 `RegisterRequestForm` (`Email`, `Username`, `Password` + returns) |
| `Rezepte.Web/Controllers/ExportsController.cs` | Z. 23 `ExportMyRecipes` (`format`, `includeImages`, `includePdf`, `ct` + returns) |
| `Rezepte.Web/Controllers/JobsController.cs` | Z. 25 `EnqueueUserExport`, Z. 47 `EnqueueAdminExport`, Z. 69 `GetJobStatus`, Z. 99 `DownloadJobResult` (je params + returns) |
| `Rezepte.Web/Controllers/RecipesController.cs` | Z. 352 `SearchAsync` (`q`, `tags`, `cookbookId`, `page`, `pageSize`, `sort`, `ct` + returns) |
| `Rezepte.Web/Controllers/SessionController.cs` | Z. 76 `LoginDto` (returns) |
| `Rezepte.Web/Controllers/UserExportFilesController.cs` | Z. 28 `GetMyFiles`, Z. 62 `Download`, Z. 101 `Delete` (je `ct`/`id` + returns) |
| `Rezepte.Web/Controllers/UsersController.cs` | Z. 10 Ktor (`users` + returns) |
| `Rezepte.Web/Extensions/FormFileExtensions.cs` | Z. 5 `ReadToMemoryStreamAsync` (`file`, `ct` + returns) |
| `Rezepte.Web/Extensions/JobQueueServiceCollectionExtensions.cs` | Z. 8 `AddBackgroundJobQueue` (`services` + returns) |
| `Rezepte.Web/Extensions/LoggingExtensions.cs` | Z. 10 `ConfigureSerilog` (`<param>` für `builder` fehlt) |
| `Rezepte.Web/Services/ExportCleanupService.cs` | Z. 80 `IsCleanupDue` (`settings`, `now` + returns) |
| `Rezepte.Web/Services/ExportService.cs` | Z. 15 `ExportUserAsync`, Z. 20 `ExportAllAsync`, Z. 25 `RestoreFromZipAsync` (nur params gemeldet), Z. 39 `GenerateRecipePdfAsync` (params + returns) |
| `Rezepte.Web/Services/IAiUsageService.cs` | Z. 7 `RecordRequestAsync` (params); Z. 12 `TryRecordRequestAsync` (params + returns); Z. 17 `GetCountAsync` (params + returns) |
| `Rezepte.Web/Services/ICalendarService.cs` | Z. 18 `GetOccurrencesAsync` (params + returns) |
| `Rezepte.Web/Services/IGoogleCredentialsProvider.cs` | Z. 28 `GetDiagnostics` (returns) |
| `Rezepte.Web/Services/UserService.cs` | Z. 10 `User` (returns) |
| `Rezepte.Web/Services/BackgroundJobs/IBackgroundJobHandler.cs` | Z. 14 `HandleAsync` (`job`, `scopeServices`, `ct`) |
| `Rezepte.Web/Services/BackgroundJobs/IBackgroundJobQueue.cs` | Z. 5 `EnqueueAsync`, Z. 11 `GetJobAsync` (je params + returns) |
| `Rezepte.Web/Services/Http/RemoteContentFetcher.cs` | Z. 19 `FetchAsync` (`uri`, `ct` + returns) |
| `Rezepte.Tests.Browser/Infrastructure/ConfiguredRezepteAppFixture.cs` | Z. 3 Ktor (`environmentOverrides` + returns) |
| `Rezepte.Tests/Services/Import/ImportOrchestratorTests.cs` | Z. 270 `CancellingStream` (`content` + returns) |

## B) Fehlende XML-Doc-Konfiguration in `.csproj` (10 Projekte)

Bei allen fehlt `<GenerateDocumentationFile>true</GenerateDocumentationFile>` **und** eine CS1591-als-Fehler-Konfiguration (`<WarningsAsErrors>CS1591</WarningsAsErrors>` oder `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>`):

- `Rezepte.Import.Abstractions/Rezepte.Import.Abstractions.csproj`
- `Rezepte.Import.PluginSdk/Rezepte.Import.PluginSdk.csproj`
- `Rezepte.Import.Plugins.AIFoto/Rezepte.Import.Plugins.AIFoto.csproj`
- `Rezepte.Import.Plugins.AIUrl/Rezepte.Import.Plugins.AIUrl.csproj`
- `Rezepte.Import.Plugins.Backup/Rezepte.Import.Plugins.Backup.csproj`
- `Rezepte.Tests.Browser/Rezepte.Tests.Browser.csproj`
- `Rezepte.Tests.PluginFixture/Rezepte.Tests.PluginFixture.csproj`
- `Rezepte.Tests/Rezepte.Tests.csproj`
- `Rezepte.Updater.TestHost/Rezepte.Updater.TestHost.csproj`
- `Rezepte.Web/Rezepte.Web.csproj`
