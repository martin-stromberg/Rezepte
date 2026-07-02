# Bestandsaufnahme - Einkaufsliste

## Projektkontext

- `Rezepte.Web` ist eine Blazor-Server-Anwendung mit Interactive Server Components.
- Persistenz erfolgt ueber EF Core mit SQLite, Tests nutzen EF Core InMemory.
- Authentifizierte Seiten verwenden `[Authorize]`; aktuelle Benutzer werden in Komponenten ueber `AuthenticationStateProvider` ermittelt.
- Services werden in `Rezepte.Web/Extensions/ServiceCollectionExtensions.cs` registriert.

## Relevante Dateien

- `Rezepte.Web/Data/RezepteDbContext.cs`: DbSets und Modellkonfiguration.
- `Rezepte.Web/Entities/*`: bestehende Entitaeten fuer Rezepte, Kochbuecher, Zutaten und Kalender.
- `Rezepte.Web/Services/RecipeService.cs`: bestehendes Service-Pattern mit userId-Parametern und EF-Operationen.
- `Rezepte.Web/Components/Layout/MainLayout.razor`: Hauptnavigation.
- `Rezepte.Web/Components/Pages/RecipePage.razor`: Rezeptdetailseite mit Zutatenanzeige und Aktionsbuttons.
- `Rezepte.Tests/Services/*`: Service-Tests mit InMemory-Datenbank, FluentAssertions und Moq.

## Fachliche Beobachtungen

- Rezeptzutaten sind als `RecipeIngredient` einzelnen `RecipeStep`-Datensaetzen zugeordnet.
- Eine Einkaufsliste braucht eigene persistente Entitaeten, weil Nutzer Eintraege abhaken, manuell ergaenzen und gruppieren koennen.
- Rezeptgruppen sollten optional mit `RecipeId` verknuepft sein; beim Loeschen eines Rezepts sollte die Einkaufsliste erhalten bleiben.

## Technische Leitplanken

- Neue Entitaeten: `ShoppingListGroup` und `ShoppingListItem`.
- Neuer Service: `IShoppingListService`/`ShoppingListService` fuer Gruppen, Eintraege, Check-State und Rezeptuebernahme.
- Neue Blazor-Seite: `Components/Pages/ShoppingList.razor`.
- Neue Shared-Komponente fuer Rezeptuebernahme-Dialog: `Components/Shared/AddRecipeToShoppingListDialog.razor`.
- EF-Konfiguration inklusive Indizes und optionaler Recipe-Beziehung.
- Migration und ModelSnapshot muessen erweitert werden.
- Tests im Stil von `RecipeServiceTests`.

## Risiken

- Manuelle EF-Migration kann von spaeteren EF-generierten Migrationen abweichen; Build und Tests pruefen die Kompilierbarkeit.
- `RecipePage.razor` verwendet lokale DTO-Records; Dialogintegration muss diese Struktur respektieren.
- Einige vorhandene Dateien enthalten Encoding-Artefakte; neue Dateien bleiben ASCII.
