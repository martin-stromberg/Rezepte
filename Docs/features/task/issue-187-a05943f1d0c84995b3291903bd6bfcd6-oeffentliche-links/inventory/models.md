# Datenmodell — Bestandsaufnahme

Analysierte Entitäten im Kontext der Anforderung „Öffentliche Links". Eine Share-Link-Entität existiert **nicht** — die folgenden Tabellen beschreiben den Ist-Stand der betroffenen Bestandsklassen.

## `Recipe`
Datei: `Rezepte.Web/Entities/Recipe.cs`

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `Id` | `string` | GUID ohne Trennzeichen (`Guid.NewGuid().ToString("n")`), Zeile 12 |
| `UserId` | `string` | Besitzer-Referenz; Grundlage aller `userId`-gebundenen Zugriffe, Zeile 16 |
| `Title` | `string` | Rezepttitel, Zeile 20 |
| `Description` | `string?` | Beschreibung, Zeile 24 |
| `CreatedAt` | `DateTime` | Erstellungszeitpunkt, Default `DateTime.UtcNow`, Zeile 28 |
| `Steps` | `ICollection<RecipeStep>` | Zubereitungsschritte, Zeile 34 |
| `Images` | `ICollection<RecipeImage>` | Rezeptbilder, Zeile 39 |
| `RecipeCookbooks` | `ICollection<RecipeCookbook>` | Kochbuch-Zuordnungen, Zeile 44 |
| `SideDishes` | `ICollection<RecipeSideDish>` | Verlinkte Beilagenrezepte (Rezept → Beilage), Zeile 49 |
| `UsedAsSideDishFor` | `ICollection<RecipeSideDish>` | Inverse Navigation (Beilage → Hauptrezepte), Zeile 54 |
| `Uri` | `string?` | Externe Quellseite des Rezepts (`sourceUri`); Setter ist `internal`, Zeile 58 |
| `Portions` | `int` | Portionszahl, Default 0, Zeile 62 |

EF-Konfiguration: `RezepteDbContext.OnModelCreating`, Zeilen 149–157 (`HasIndex` auf `{UserId, Title}`; `Title` max. 200, `Description` max. 4000).

## `RecipeSideDish`
Datei: `Rezepte.Web/Entities/RecipeSideDish.cs`

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `Id` | `string` | GUID ohne Trennzeichen, Zeile 12 |
| `RecipeId` | `string` | FK auf Hauptrezept, Zeile 16 |
| `SideDishRecipeId` | `string` | FK auf Beilagenrezept, Zeile 20 |
| `OrderIndex` | `int` | Sortierung der Beilagen, Zeile 24 |
| `Recipe` | `Recipe?` | Navigation zum Hauptrezept, Zeile 29 |
| `SideDishRecipe` | `Recipe?` | Navigation zum Beilagenrezept, Zeile 33 |

EF-Konfiguration: `RezepteDbContext` Zeilen 173–192 — `Recipe`-Löschung kaskadiert, `SideDishRecipe`-Löschung ist `Restrict`; Unique-Index auf `{RecipeId, SideDishRecipeId}`.

## `RecipeImage`
Datei: `Rezepte.Web/Entities/RecipeImage.cs`

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `Id` | `string` | GUID ohne Trennzeichen, Zeile 12 |
| `RecipeId` | `string` | FK auf Rezept, Zeile 16 |
| `Recipe` | `Recipe` | Navigation (required), Zeile 20 |
| `FileName` | `string` | Dateiname, Zeile 24 |
| `ContentType` | `string` | MIME-Typ, Zeile 28 |
| `Data` | `byte[]` | Binärdaten, Zeile 33 |
| `CreatedAt` | `DateTime` | Erstellungszeitpunkt, Zeile 37 |
| `Url` | `string` | **Nur gelesen, berechnet** — zeigt auf `/api/recipes/{RecipeId}/image/{Id}` (authentifizierter Endpunkt, Zeile 41) |

Beziehung: `Recipe.Images` ↔ `RecipeImage` mit `OnDelete(Cascade)`, `RezepteDbContext` Zeilen 218–222. Hinweis: `Url` ist fest auf den authentifizierten Bild-Endpunkt verdrahtet — für den anonymen Zugriff liefert sie keine gültige unauthentifizierte Quelle.

## `RecipeStep`
Datei: `Rezepte.Web/Entities/RecipeStep.cs`

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `Id` | `string` | GUID |
| `RecipeId` | `string` | FK auf Rezept |
| `StepIndex` | `int` | Reihenfolge (Unique-Index mit `RecipeId`, DbContext Z. 204) |
| `Title` | `string?` | Schritt-Titel (max. 200) |
| `Description` | `string` | Beschreibung (required, max. 4000) |
| `DurationMinutes` | `int` | Dauer in Minuten |
| `RequiresOvernightRest` | `bool` | Ruhe-über-Nacht-Flag |
| `Ingredients` | `ICollection<RecipeIngredient>` | Zutaten des Schritts |

## `RecipeIngredient`
Datei: `Rezepte.Web/Entities/RecipeIngredient.cs`

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `Id` | `string` | GUID |
| `StepId` | `string` | FK auf `RecipeStep` (Cascade-Delete) |
| `Amount` | `decimal` | Menge |
| `Unit` | `string?` | Einheit (max. 64) |
| `Name` | `string` | Bezeichnung (required, max. 200) |

## `AppSetting`
Datei: `Rezepte.Web/Entities/AppSetting.cs`

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `Key` | `string` | Einstellungsschlüssel, Primärschlüssel (max. 128), Zeile 11 |
| `Value` | `string` | Wert (required), Zeile 15 |

EF-Konfiguration: `RezepteDbContext` Zeilen 323–328. Wird von `SettingsService` als generischer Key-Value-Store für globale Programmeinstellungen genutzt (bestehende Keys u. a. `AiEnabled`, `GlobalMaxRequestsPerHour`, `GlobalMaxRequestsPerDay`, `GlobalDisableOnLimitReached`, `SecurityTxt.*`, `ShoppingListEditMode:<userId>`). Ein Key für die Link-Gültigkeitsdauer existiert **nicht**.

## `RezepteDbContext`
Datei: `Rezepte.Web/Data/RezepteDbContext.cs`

- Vorhandene `DbSet`s: `Users` (Z. 19), `Cookbooks` (Z. 24), `Recipes` (Z. 29), `RecipeSteps` (Z. 34), `RecipeCookbooks` (Z. 39), `RecipeSideDishes` (Z. 44), `RecipeIngredients` (Z. 49), `RecipeImages` (Z. 53), `AiRequestLogs` (Z. 58), `UserSettings` (Z. 64), `AppSettings` (Z. 69), `PluginSettings`/`PluginSources`/`PluginSourceReleases` (Z. 74–84), `CalendarEvents` (Z. 91), `ShoppingListGroups`/`ShoppingListItems` (Z. 96–101), `BackgroundJobs` (Z. 106), `UserExportFiles` (Z. 111).
- Kein `DbSet` und keine Entity-Konfiguration für einen Share-Link.
- Migrationen: 27 Migrationen unter `Rezepte.Web/Migrations/` (letzte: `20260822083747_AddUserExportFiles`); keine Share-bezogene Migration.

## Nicht vorhanden

- Keine Entität für öffentliche Links (`RecipeShareLink` o. ä.) — weder Entitätsklasse noch `DbSet` noch Migration.
