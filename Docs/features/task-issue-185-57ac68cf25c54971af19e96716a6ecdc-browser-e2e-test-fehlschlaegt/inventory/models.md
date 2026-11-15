# Datenmodelle — Bestandsaufnahme

Keine Datenmodell-Änderungen zu erwarten (Scope-Ausschluss). Nachfolgend nur die im
Suchfluss verwendeten Modelle als Bestand.

## `RecipeSearch.RecipeResult` (View-Model der Suchseite)

Datei: `Rezepte.Web/Components/Pages/RecipeSearch.razor` (Zeile 200, `private sealed record`)

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `Id` | `string` | Rezept-ID, Ziel des `.list-group-item`-Links `/recipes/{Id}` |
| `Title` | `string` | Angezeigter Titel (Test selektiert `:has-text('{title}')`) |
| `Snippet` | `string` | Kurztext unter dem Titel |
| `PrimaryImageUrl` | `string?` | Optional; steuert `<img>` im Listeneintrag |
| `Cookbooks` | `string[]?` | Vorhanden, im Markup nicht gerendert |
| `Tags` | `string[]?` | Badge-Liste im Listeneintrag |
| `PublishedLabel` | `string?` | Datums-Label oben rechts im Eintrag |

## `RecipeService.SearchResult`

Datei: `Rezepte.Web/Services/RecipeService.cs` (Zeilen 966–980, `public sealed class`)

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `TotalCount` | `int` | Gesamttrefferzahl vor Paging → `RecipeSearch.totalItems`/`TotalPages` |
| `Items` | `IEnumerable<SearchResultItem>` | Treffer der aktuellen Seite |

## `RecipeService.SearchResultItem`

Datei: `Rezepte.Web/Services/RecipeService.cs` (Zeilen 1028–1045, `public sealed class`)

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `Id` | `string` | Rezept-ID |
| `Title` | `string` | Titel |
| `CreatedAt` | `DateTime` | Erstellungsdatum |
| `PrimaryImageUrl` | `string?` | Erstes Bild (neuestes `CreatedAt`) |
| `Snippet` | `string` | Kurztext (Schritt-Beschreibung, sonst Zutaten/Beschreibung; auf 200 Zeichen gekappt) |
| `PublishedLabel` | `string?` | `CreatedAt` formatiert `yyyy-MM-dd` |
| `Tags` | `string[]` | Derzeit immer leer (Tags sind nicht modelliert, siehe Kommentar Zeile 914) |
