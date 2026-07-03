# Datenmodell und Persistenz

## Ist-Zustand

- `Rezepte.Web/Entities/Recipe.cs:3` definiert `Recipe` mit `Id`, `UserId`, `Title`, `Description`, `CreatedAt`, `Steps`, `Images`, `RecipeCookbooks`, `Uri` und `Portions`.
- `Rezepte.Web/Data/RezepteDbContext.cs:11` bis `Rezepte.Web/Data/RezepteDbContext.cs:15` registriert Rezepte, Schritte, Kochbuchzuordnungen, Zutaten und Bilder als DbSets.
- `Rezepte.Web/Data/RezepteDbContext.cs:58` konfiguriert `Recipe` mit Pflicht-`UserId`, Pflicht-`Title`, optionaler Beschreibung und Index auf `{ UserId, Title }`.
- `Rezepte.Web/Data/RezepteDbContext.cs:68` konfiguriert die vorhandene Join-Entitaet `RecipeCookbook` mit Unique Index auf `{ CookbookId, RecipeId }`.
- `Rezepte.Web/Data/RezepteDbContext.cs:82` und `Rezepte.Web/Data/RezepteDbContext.cs:95` modellieren Schritte und Zutaten als Rezept-Unterstruktur.
- `Rezepte.Web/Entities/CalendarEvent.cs` referenziert optional genau ein Rezept ueber `RecipeId`.
- `Rezepte.Web/Entities/ShoppingListGroup.cs` referenziert optional genau ein Rezept ueber `RecipeId`.

## Luecke zur Anforderung

Es existiert keine Entitaet fuer "Rezept A hat Rezept B als moegliche Beilage". Eine Beilage ist fachlich kein Bestandteil eines Rezeptschritts und keine Kochbuchzuordnung, sondern eine selbstreferenzielle Rezeptverknuepfung innerhalb desselben Benutzers.

## Konsequenzen

- Eine neue Join-Entitaet ist erforderlich, z. B. `RecipeSideDish` mit `RecipeId`, `SideDishRecipeId`, optional `OrderIndex` und Navigationen.
- Die Beziehung muss verhindern, dass ein Rezept sich selbst als Beilage referenziert.
- Die Beziehung muss benutzerbezogen validiert werden, damit keine Beilagen auf Rezepte anderer Benutzer zeigen.
- Ein Unique Index auf `{ RecipeId, SideDishRecipeId }` verhindert Duplikate.
- Delete-Verhalten muss bewusst gewaehlt werden. Naheliegend: Loeschen eines Hauptrezepts entfernt dessen Beilagenzuordnungen; Loeschen einer Beilage entfernt ebenfalls referenzierende Zuordnungen oder wird restriktiv behandelt. Wegen zweier FKs auf `Recipe` ist EF-Core-Konfiguration explizit noetig.
- Eine neue EF-Migration ist erforderlich. Migrationen liegen unter `Rezepte.Web/Migrations`.

## Risiken

- Selbstreferenzielle Cascade-Konfigurationen koennen bei relationalen Datenbanken mehrere Cascade-Pfade erzeugen. SQLite ist toleranter, aber die Modellkonfiguration sollte dennoch eindeutig sein.
- `GetByIdAsync` nutzt `AsNoTracking()` und Includes. Neue Navigationen muessen explizit geladen werden, wenn DTOs oder Dialoge Beilagen anzeigen sollen.
