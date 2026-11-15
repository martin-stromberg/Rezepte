# Detail: `DemoDataSeedingJobHandler`

Datei: `Rezepte.Web/Services/BackgroundJobs/Handlers/DemoDataSeedingJobHandler.cs` (318 Zeilen), `IBackgroundJobHandler`-Implementierung.

## Konstanten/Felder

| Member | Wert | Zeile |
|--------|------|-------|
| `JobTypeName` | `"seed-demo-data"` (public const) | 17 |
| `CalendarEventCount` | `5` (private const) | 22 |
| `DefaultPortions` | `2` (Fallback bei `Portions <= 0`) | 27 |
| `DinnerHour` | `18` | 32 |
| `DemoImagesPath` | `Path.Combine(AppContext.BaseDirectory, "DemoData", "demo-images.zip")` | 38 |
| `JobType` | Property → `JobTypeName` | 41 |

## `HandleAsync(job, scopeServices, ct)` — Zeilen 44–78

1. `job` null → `ArgumentNullException`; `ct.ThrowIfCancellationRequested()` (46–47).
2. `DemoDataSeedPayload.FromJson(job.PayloadJson)`; `UserId` leer → `InvalidOperationException("Demo data seeding job has no user id.")` (49–53).
3. `IUserService` via `scopeServices`, `GetByIdAsync` — User `null` → `InvalidOperationException("User '{id}' does not exist.")` (55–60).
4. Services via `scopeServices`: `ICookbookService`, `IRecipeService`, `ICalendarService`, `IShoppingListService`, `ILogger<DemoDataSeedingJobHandler>` (62–66).
5. `LogInformation("Starting demo data seeding …")` (68); `using var demoImages = OpenDemoImages(logger)` (70).
6. Fachsequenz (72–75): `SeedCookbooksAsync` → `SeedRecipesAsync` → `SeedCalendarAsync` → `SeedShoppingListAsync`.
7. `LogInformation("Demo data seeding … completed")` (77).

## Private Sequenzmethoden

| Methode | Zeilen | Verhalten |
|---------|--------|-----------|
| `SeedCookbooksAsync` | 87–105 | Iteriert `DemoDataSource.Cookbooks`; `ICookbookService.CreateAsync(userId, name, null, ct)`; `!ok \|\| created == null` → `InvalidOperationException`; liefert `Dictionary<Name, Id>` (Ordinal). |
| `SeedRecipesAsync` | 119–161 | Je Kochbuch × `DemoRecipe` → `IRecipeService.CreateAsync(userId, cookbookId, Title, Description, uri: null, Portions, Steps, ct)`; Fehler → `InvalidOperationException`; `AttachImageAsync`; Rezepte des Kochbuchs `DemoDataSource.CalendarCookbookName` als `SeededRecipe`-Paare gesammelt. Fehlende `cookbookId` → `InvalidOperationException`. |
| `OpenDemoImages` | 168–177 | `demo-images.zip` fehlt → `LogWarning` + `null` (kein Fehler); sonst `ZipArchive` read. |
| `AttachImageAsync` | 190–218 | ZIP-Eintrag per `Path.GetFileNameWithoutExtension(e.Name) == demoRecipe.Title` (OrdinalIgnoreCase); kein Eintrag → Rücksprung; `IRecipeService.AddImageAsync(userId, recipe.Id, stream, entry.Name, GetImageContentType(entry.Name), ct)`; Misserfolg nur `LogWarning` — **kein Throw**. |
| `GetImageContentType` | 225–231 | `.png`→`image/png`, `.jpg`/`.jpeg`→`image/jpeg`, sonst `application/octet-stream`. |
| `SeedCalendarAsync` | 242–269 | `Take(5)` der Kalender-Kochbuch-Rezepte; je Index `DateTime.Today.AddDays(i+1)`, `TimeSpan.FromHours(18)`, `Portions > 0 ? Portions : 2`, `RecurrenceType.None`, `WeekDays.None` → `ICalendarService.CreateEventAsync`; Fehler → `InvalidOperationException`; liefert geplante `SeededRecipe`-Liste. |
| `SeedShoppingListAsync` | 279–309 | Kein geplantes Rezept → Rücksprung; `IShoppingListService.EnsureDefaultGroupAsync(userId, ct)`; je Step×Ingredient des ersten geplanten Rezepts `AddItemAsync(userId, group.Id, Amount, Unit, Name, ct)`; Fehler → `InvalidOperationException`. |
| `SeededRecipe` | 317 | `private sealed record SeededRecipe(DemoRecipe Demo, Recipe Recipe)` |

## Fehlerverhalten/Nebenbedingungen

- Jeder Domänenaufruf außer `AddImageAsync` wirft bei Misserfolg `InvalidOperationException` → Job landet auf `Failed` (`BackgroundJobHostedService`), **kein Retry, keine Transaktion** — Teildaten bleiben bestehen.
- Fehlende `demo-images.zip` oder fehlende Einzelbilder sind kein Fehler (`LogWarning` bzw. still).
- `demo-images.zip` wird per `Rezepte.Web.csproj:36–40` (`Content Include="DemoData\demo-images.zip"`, PreserveNewest) ins Output kopiert — und steht transitiv auch im Test-Output (`Rezepte.Tests/bin/Debug/net10.0/DemoData/demo-images.zip`) bereit. In Unit-Tests findet `OpenDemoImages` das Archiv daher; `AttachImageAsync` ruft dann `IRecipeService.AddImageAsync` auf (Mock-Default: `(false, null, null)` → nur `LogWarning`, kein Fehler).
