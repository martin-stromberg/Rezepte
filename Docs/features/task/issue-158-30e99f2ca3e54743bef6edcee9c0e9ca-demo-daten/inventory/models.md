# Datenmodell – Bestandsaufnahme „Demo-Daten bei Registrierung"

Entities liegen unter `Rezepte.Web/Entities/`, DTOs unter `Rezepte.Web/Contracts/` bzw. direkt in den Service-/Controller-Dateien.

## `User`
Datei: `Rezepte.Web/Entities/User.cs`

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `Id` | `string` | Primärschlüssel, `Guid.NewGuid().ToString("n")` |
| `Username` | `string` | Eindeutiger Benutzername |
| `Email` | `string` | E-Mail (optional, leerer String) |
| `PasswordHash` | `string` | Gehashtes Passwort |
| `IsAdmin` | `bool` | Erstregistrierter Benutzer wird Admin |
| `CreatedAt` | `DateTime` | Default `DateTime.UtcNow` |

## `Cookbook`
Datei: `Rezepte.Web/Entities/Cookbook.cs`

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `Id` | `string` | Primärschlüssel (Guid „n") |
| `UserId` | `string` | Besitzer |
| `Name` | `string` | Name des Kochbuchs |
| `Description` | `string?` | Optionale Beschreibung |
| `CreatedAt` | `DateTime` | Erstellungszeitpunkt |
| `OrderIndex` | `int` | Sortierreihenfolge (Drag&Drop) |
| `RecipeCookbooks` | `ICollection<RecipeCookbook>` | Navigation zur Zuordnung |

## `Recipe`
Datei: `Rezepte.Web/Entities/Recipe.cs`

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `Id` | `string` | Primärschlüssel (Guid „n") |
| `UserId` | `string` | Besitzer |
| `Title` | `string` | Titel |
| `Description` | `string?` | Beschreibung |
| `CreatedAt` | `DateTime` | Default `DateTime.UtcNow` |
| `Steps` | `ICollection<RecipeStep>` | Zubereitungsschritte |
| `Images` | `ICollection<RecipeImage>` | Bilder |
| `RecipeCookbooks` | `ICollection<RecipeCookbook>` | Kochbuch-Zuordnungen |
| `SideDishes` / `UsedAsSideDishFor` | `ICollection<RecipeSideDish>` | Beilagen-Beziehungen |
| `Uri` | `string?` | Quell-URI (`internal set`) |
| `Portions` | `int` | Portionen, Default 0 |

## `RecipeCookbook`
Datei: `Rezepte.Web/Entities/RecipeCookbook.cs`

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `Id` | `string` | Primärschlüssel |
| `CookbookId` / `Cookbook` | `string` / `Cookbook` | FK + Navigation zum Kochbuch |
| `RecipeId` / `Recipe` | `string` / `Recipe` | FK + Navigation zum Rezept |

## `RecipeStep`
Datei: `Rezepte.Web/Entities/RecipeStep.cs`

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `Id` | `string` | Primärschlüssel |
| `RecipeId` | `string` | FK zum Rezept |
| `StepIndex` | `int` | Reihenfolge |
| `Title` | `string?` | Optionaler Schritt-Titel |
| `Description` | `string` | Beschreibung (Pflicht) |
| `DurationMinutes` | `int` | Dauer |
| `RequiresOvernightRest` | `bool` | Über-Nacht-Ruhe |
| `Ingredients` | `ICollection<RecipeIngredient>` | Zutaten des Schritts |

## `RecipeIngredient`
Datei: `Rezepte.Web/Entities/RecipeIngredient.cs`

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `Id` | `string` | Primärschlüssel |
| `StepId` | `string` | FK zum Schritt |
| `Amount` | `decimal` | Menge |
| `Unit` | `string?` | Einheit |
| `Name` | `string` | Zutatenname |

## `CalendarEvent`
Datei: `Rezepte.Web/Entities/CalendarEvent.cs`

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `Id` | `string` | Primärschlüssel (`Guid.NewGuid().ToString()`) |
| `UserId` | `string` | Besitzer (`[Required]`) |
| `StartDate` | `DateTime` | Datum bzw. Startdatum |
| `TimeOfDay` | `TimeSpan` | Uhrzeit |
| `RecipeId` | `string?` | Optionales Rezept |
| `Portions` | `int` | Portionen |
| `Recurrence` | `RecurrenceType` | Wiederholung |
| `RecurrenceDays` | `WeekDays` | Wochentage-Bitmask |
| `CreatedAt` / `ModifiedAt` | `DateTime` / `DateTime?` | Zeitstempel |
| `Recipe` | `Recipe?` | Navigation |

## `ShoppingListGroup`
Datei: `Rezepte.Web/Entities/ShoppingListGroup.cs`

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `Id` | `string` | Primärschlüssel |
| `UserId` | `string` | Besitzer |
| `Name` | `string` | Gruppenname (Default-Gruppe: „Einkaufsliste") |
| `RecipeId` | `string?` | Optionale Rezeptverknüpfung |
| `OrderIndex` | `int` | Sortierung |
| `CreatedAt` / `ModifiedAt` | `DateTime` / `DateTime?` | Zeitstempel |
| `Recipe` / `Items` | `Recipe?` / `ICollection<ShoppingListItem>` | Navigationen |

## `ShoppingListItem`
Datei: `Rezepte.Web/Entities/ShoppingListItem.cs`

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `Id` | `string` | Primärschlüssel |
| `GroupId` | `string` | FK zur Gruppe |
| `Amount` | `decimal` | Menge |
| `Unit` | `string?` | Einheit |
| `Name` | `string` | Artikelname |
| `IsChecked` | `bool` | Abgehakt |
| `OrderIndex` | `int` | Sortierung |
| `CreatedAt` / `ModifiedAt` | `DateTime` / `DateTime?` | Zeitstempel |
| `Group` | `ShoppingListGroup?` | Navigation |

## `BackgroundJob`
Datei: `Rezepte.Web/Services/BackgroundJobs/BackgroundJob.cs` (Tabelle `BackgroundJobs`)

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `Id` | `Guid` | Primärschlüssel |
| `JobType` | `string` | `[Required]`, Typname (z. B. `export:user`, `export:all`) |
| `InitiatorUserId` | `string?` | Auslösender Benutzer |
| `CreatedAt` / `StartedAt` / `CompletedAt` | `DateTime` / `DateTime?` | Zeitstempel |
| `Status` | `BackgroundJobStatus` | Default `Pending` |
| `PayloadJson` | `string?` | JSON-Payload, Konvention: Handler parst selbst |
| `Progress` | `int` | Fortschritt 0–100 |
| `ResultMessage` / `Error` | `string?` | Ergebnis-/Fehlertext |

## DTOs

### `RegisterRequest`
Datei: `Rezepte.Web/Contracts/AuthDtos.cs` (Zeilen 12–16)

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `Email` | `string?` | `[EmailAddress]` |
| `Username` | `string` | `[Required]` |
| `Password` | `string` | `[Required, MinLength(6)]` |

Kein `CreateDemoData`-Feld vorhanden.

### `RegisterRequestForm`
Datei: `Rezepte.Web/Controllers/AuthController.cs` (Zeile 103)

Record `RegisterRequestForm(string? Email, string Username, string Password)` — deklariert, wird im Controller aber **nicht verwendet**; der Controller liest die Formularfelder direkt via `Request.ReadFormAsync`. Kein `CreateDemoData`-Feld.

### `RecipeCreateStep` / `RecipeCreateIngredient`
Datei: `Rezepte.Web/Services/RecipeService.cs` (Zeilen 251, 261)

- `RecipeCreateIngredient(decimal Amount, string? Unit, string Name)`
- `RecipeCreateStep(string? Title, string Description, int DurationMinutes, bool RequiresOvernightRest, IReadOnlyList<RecipeCreateIngredient> Ingredients)`

Eingabe-DTOs für `IRecipeService.CreateAsync`/`UpdateAsync` — geeignet, um Demo-Rezepte samt Schritten und Zutaten anzulegen.
