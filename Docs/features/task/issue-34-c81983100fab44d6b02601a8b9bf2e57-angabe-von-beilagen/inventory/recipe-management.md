# Rezeptverwaltung und API

## Ist-Zustand Service

- `Rezepte.Web/Services/RecipeService.cs:13` definiert `IRecipeService`.
- `Rezepte.Web/Services/RecipeService.cs:15` stellt `GetByIdAsync` bereit.
- `Rezepte.Web/Services/RecipeService.cs:18` und `Rezepte.Web/Services/RecipeService.cs:19` erstellen und aktualisieren Rezepte mit Titel, Beschreibung, URI, Portionen und Schritten.
- `Rezepte.Web/Services/RecipeService.cs:35` und `Rezepte.Web/Services/RecipeService.cs:36` definieren die Create-Step-Records.
- `Rezepte.Web/Services/RecipeService.cs:54` laedt ein Rezept mit Kochbuchzuordnungen, Schritten und Zutaten.
- `Rezepte.Web/Services/RecipeService.cs:81` erstellt Rezepte und optional eine Kochbuchzuordnung.
- `Rezepte.Web/Services/RecipeService.cs:143` aktualisiert Stammdaten und ersetzt Schritte/Zutaten vollstaendig.

## Ist-Zustand API

- `Rezepte.Web/Controllers/RecipesController.cs:79` laedt ein Rezept per `GetByIdAsync`.
- `Rezepte.Web/Controllers/RecipesController.cs:83` baut das `RecipeDto`.
- `Rezepte.Web/Controllers/RecipesController.cs:122` definiert `CreateRecipeRequest`.
- `Rezepte.Web/Controllers/RecipesController.cs:146` definiert `UpdateRecipeRequest`.
- `Rezepte.Web/Controllers/RecipesController.cs:323` definiert `RecipeDto` ohne Beilageninformationen.
- Die Rezeptsuche liegt in `RecipesController.SearchAsync` und `RecipeService.SearchAsync`; sie liefert Suchergebnisse, aber keine Beilagenrelationen.

## Ist-Zustand UI

- `Rezepte.Web/Components/Pages/RecipeEdit.razor:44` bis `Rezepte.Web/Components/Pages/RecipeEdit.razor:57` bearbeitet Titel, Beschreibung, Portionen und Quelle.
- `Rezepte.Web/Components/Pages/RecipeEdit.razor:108` bis `Rezepte.Web/Components/Pages/RecipeEdit.razor:118` bearbeitet Zutaten je Schritt.
- `Rezepte.Web/Components/Pages/RecipeEdit.razor:226` und `Rezepte.Web/Components/Pages/RecipeEdit.razor:264` erstellen die Payloads fuer Create/Update.
- `Rezepte.Web/Components/Pages/RecipeEdit.razor:331` definiert ein lokales `RecipeDto` ohne Beilagen.
- `Rezepte.Web/Components/Pages/RecipePage.razor` zeigt Rezeptdetails und Aktionen, aber keine Beilagenliste.

## Luecke zur Anforderung

Benutzer koennen derzeit keine Liste moeglicher Beilagen pflegen. Es gibt auch keine API-Repräsentation, mit der Kalender- oder Einkaufslistendialoge verlinkte Beilagen direkt erhalten.

## Naheliegende Anpassungen

- Service-Signaturen um `IReadOnlyCollection<string> sideDishRecipeIds` oder separates Upsert-Modell erweitern.
- `GetByIdAsync` oder ein neuer Query-Endpoint muss Beilagen mit mindestens `Id`, `Title`, optional `Description`, `ImageUrl` und Portionen liefern.
- Create/Update DTOs in `RecipesController` um Beilagen-IDs erweitern.
- `RecipeEdit.razor` braucht eine Mehrfachauswahl fuer Rezepte, inklusive Suche, Anzeige vorhandener Beilagen und Entfernen.
- `RecipePage.razor` kann die verwalteten Beilagen anzeigen, ist fuer die Akzeptanzkriterien aber weniger zentral als Edit, Kalender und Einkaufsliste.

## Wiederverwendbare Bausteine

- `CalendarEventDialog.razor` enthaelt bereits lokale Rezeptsuche mit `RecipeSearchResultDto`, Suchfeld und Auswahl.
- Es gibt keine dedizierte Shared-Komponente fuer Mehrfach-Rezeptauswahl. Eine Extraktion kann Duplikation reduzieren, ist aber nicht zwingend fuer eine erste Umsetzung.
