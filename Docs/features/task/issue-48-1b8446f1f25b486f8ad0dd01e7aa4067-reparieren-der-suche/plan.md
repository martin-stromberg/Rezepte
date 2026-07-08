# Umsetzungsplan: Reparatur der Rezeptsuche

## Ziel

Die bestehende Rezeptsuche wird minimal-invasiv repariert, sodass vorhandene passende Rezepte wieder gefunden werden. Der Akzeptanzfall `Honig` muss das Rezept `Honig - Senf - Sojamarinade` liefern.

## Leitplanken

- Keine neue Suchfunktionalitaet ueber die bestehende Suche nach Titel, Beschreibung, Schritten und Zutaten hinaus einfuehren.
- Den Suchpfad an die bestehende Besitzlogik der Rezept-Endpunkte angleichen.
- Bestehende API-Antwortform (`Items`, `Total`) und UI-Aufrufer beibehalten.
- UI-Verhalten nur anfassen, wenn die Backend-Reparatur und Tests zeigen, dass der Fehler dort nicht vollstaendig behoben ist.

## Umsetzungsschritte

1. `IRecipeService.SearchAsync` um den Parameter `string userId` erweitern.
   - Betroffen: `Rezepte.Web/Services/RecipeService.cs`
   - Die Signatur soll weiterhin Suchbegriff, Tags, optionalen Cookbook-Filter, Paging, Sortierung und CancellationToken unterstuetzen.

2. `RecipeService.SearchAsync` auf den authentifizierten Benutzer scopen.
   - Die Basisquery wird direkt nach `_db.Recipes.AsNoTracking()` um `r.UserId == userId` eingeschraenkt.
   - Die vorhandene Suchlogik mit `EF.Functions.Like` fuer Titel, Beschreibung, Schritte und Zutaten bleibt fachlich unveraendert.
   - Paging, Sortierung und DTO-Projektion bleiben unveraendert, soweit keine Signaturanpassung noetig ist.

3. `RecipesController.SearchAsync` an die uebrigen Rezept-Endpunkte angleichen.
   - `GetUserId()` lesen.
   - Bei fehlender UserId `Unauthorized()` zurueckgeben.
   - Die UserId an `_recipes.SearchAsync(...)` weitergeben.

4. Cookbook-Filter im Suchvertrag konsistent bewerten und bei Bedarf korrigieren.
   - Wenn der Search-Endpunkt aktuell `int? cookbookId` verwendet, soll auf `string? cookbookId` umgestellt werden, weil `RecipeCookbook.CookbookId` als `string` modelliert ist und andere Endpunkte string-basierte Cookbook-IDs nutzen.
   - In `RecipeService.SearchAsync` dann ohne `ToString()` gegen `RecipeCookbook.CookbookId` filtern.
   - UI-Aufrufer bleiben kompatibel, solange sie Query-Strings senden; sie muessen nur angepasst werden, falls lokal typisierte Methoden oder DTOs `int?` erzwingen.

5. Fokussierte Tests fuer die Suche ergaenzen.
   - In den bestehenden Service-Tests unter `Rezepte.Tests/Services` einen Test anlegen oder erweitern:
     - Benutzer A besitzt ein Rezept mit Titel `Honig - Senf - Sojamarinade`.
     - Suche mit `q = "Honig"` und UserId von Benutzer A.
     - Erwartung: Ergebnisliste enthaelt `Honig - Senf - Sojamarinade`; `Total` ist mindestens `1`.
   - Zusaetzlich einen Scope-Test ergaenzen:
     - Benutzer B sucht nach `Honig`.
     - Erwartung: Das Rezept von Benutzer A wird nicht geliefert.
   - Falls der Cookbook-Vertrag auf `string?` korrigiert wird, einen kleinen Test fuer string-basierte CookbookId ergaenzen oder einen vorhandenen Suchtest entsprechend aufbauen.

6. Bestehende Aufrufer der Service-Signatur nachziehen.
   - Primaer `RecipesController.SearchAsync`.
   - Weitere direkte Aufrufer per Suche nach `SearchAsync(` pruefen und nur die notwendigen Signaturanpassungen vornehmen.

7. Verifikation ausfuehren.
   - `dotnet test`
   - Erwartet: Der neue Suchtest `Honig` -> `Honig - Senf - Sojamarinade` ist gruen, bestehende Rezepttests bleiben gruen.

## Nicht geplant

- Keine Umstellung auf eine neue Suchengine.
- Keine Aenderung der Rezeptdaten.
- Keine Erweiterung der Suche um Tags, solange keine Tag-Entity im Datenmodell vorhanden ist.
- Keine kosmetischen UI-Aenderungen an der Suchseite.

## Risiken

- Die Signaturaenderung an `IRecipeService.SearchAsync` erfordert alle direkten Aufrufer anzupassen.
- Eine Umstellung von `cookbookId` auf `string?` ist fachlich konsistent, kann aber typisierte Test- oder Clientstellen betreffen.
- Provider-spezifische Case-Sensitivity von `EF.Functions.Like` bleibt unveraendert; der geforderte Akzeptanzfall nutzt jedoch die gleiche Schreibweise `Honig`.

## Offene Punkte

