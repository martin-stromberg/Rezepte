# Umsetzungsplan - Einkaufsliste

## Schritte

1. Datenmodell ergaenzen
   - `ShoppingListGroup` mit `UserId`, `Name`, optional `RecipeId`, Sortierung und Zeitstempeln.
   - `ShoppingListItem` mit `GroupId`, Menge, Einheit, Name, `IsChecked`, Sortierung und Zeitstempeln.
   - DbSets, Beziehungen und Indizes in `RezepteDbContext`.
   - EF-Migration und Snapshot aktualisieren.

2. Service-Schicht implementieren
   - `IShoppingListService` und `ShoppingListService`.
   - Automatische Standardgruppe bei leerer Liste.
   - CRUD fuer Gruppen und Eintraege.
   - Toggle fuer Abhaken.
   - Rezeptuebernahme mit vorausgewaehlten Zutaten und neuer Rezeptgruppe.
   - Benutzerisolation konsequent ueber `userId`.

3. UI implementieren
   - Hauptnavigation um `Einkaufsliste` erweitern.
   - Seite `/shopping-list` mit Gruppen, direkten Eingabefeldern, Checkboxen, Bearbeiten und Loeschen.
   - Rezeptdetailseite um Button und Bestaetigungsdialog fuer Zutatenuebernahme erweitern.

4. Tests ergaenzen
   - Service-Tests fuer Standardgruppe, manuelle Eintraege, Abhaken, Rezeptuebernahme und Benutzerisolation.

5. Dokumentation aktualisieren
   - Hilfe unter `Docs/help/shopping-list.md`.
   - README-Funktionsumfang und weiterfuehrende Dokumentation ergaenzen.

6. Verifikation
   - `dotnet build`
   - `dotnet test`

## Offene Punkte

Keine.
