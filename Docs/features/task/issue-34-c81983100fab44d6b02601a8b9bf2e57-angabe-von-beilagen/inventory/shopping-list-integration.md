# Einkaufslistenintegration

## Ist-Zustand

- `Rezepte.Web/Components/Pages/RecipePage.razor:30` bietet die Aktion "Zutaten zur Einkaufsliste".
- `Rezepte.Web/Components/Pages/RecipePage.razor:177` bindet `AddRecipeToShoppingListDialog` mit `RecipeId` und `RecipeTitle` ein.
- `Rezepte.Web/Components/Shared/AddRecipeToShoppingListDialog.razor:73` haelt ausgewaehlte Zutaten-IDs in `selectedIngredientIds`.
- `Rezepte.Web/Components/Shared/AddRecipeToShoppingListDialog.razor:74` haelt eine flache Zutatenliste.
- `Rezepte.Web/Components/Shared/AddRecipeToShoppingListDialog.razor:104` laedt Zutaten ueber `ShoppingListService.GetRecipeIngredientsAsync`.
- `Rezepte.Web/Components/Shared/AddRecipeToShoppingListDialog.razor:106` bis `Rezepte.Web/Components/Shared/AddRecipeToShoppingListDialog.razor:108` waehlt alle geladenen Zutaten initial aus.
- `Rezepte.Web/Components/Shared/AddRecipeToShoppingListDialog.razor:129` speichert die Auswahl ueber `AddRecipeIngredientsAsync`.
- `Rezepte.Web/Services/ShoppingListService.cs:18` liefert Zutaten eines einzelnen Rezepts.
- `Rezepte.Web/Services/ShoppingListService.cs:198` erstellt aus ausgewaehlten Zutaten eine neue Einkaufslistengruppe mit dem Rezepttitel.
- `Rezepte.Web/Components/Pages/ShoppingList.razor:166` zeigt Gruppen und deren Items an.

## Luecke zur Anforderung

Der Dialog kann nur Zutaten eines Rezepts anzeigen. Er kennt keine Beilagen, keine Gruppierung nach mehreren Rezepten und keine initiale Unterscheidung zwischen Hauptrezept und Beilagen. Die Anforderung verlangt, dass Zutaten verlinkter Beilagen im Dialog gruppiert nach Rezept erscheinen und initial nicht ausgewaehlt sind.

## Naheliegende Anpassungen

- Einen gruppierten DTO-Typ einfuehren, z. B. `ShoppingListRecipeIngredientGroup(RecipeId, RecipeTitle, IsMainRecipe, Ingredients)`.
- Service-Methode erweitern oder neu anlegen, die Hauptrezept plus verlinkte Beilagen mit Zutaten laedt.
- Dialogzustand von flacher Liste auf Gruppen umstellen.
- Initialauswahl: Zutaten des Hauptrezepts auswaehlen, Zutaten der Beilagen nicht auswaehlen.
- Beim Speichern entweder pro Rezept eine eigene `ShoppingListGroup` erzeugen oder einen erweiterten Service-Aufruf nutzen, der mehrere Gruppen in einem Vorgang anlegt.
- Gruppentitel sollten Rezepttitel verwenden, damit die spaetere Einkaufsliste die Herkunft weiterhin erkennen laesst.

## Risiken

- `selectedIngredientIds` allein reicht weiterhin zur Auswahl, aber beim Persistieren muss bekannt sein, zu welchem Rezept die IDs gehoeren, wenn mehrere Gruppen erzeugt werden sollen.
- Zutaten-IDs sind global GUIDs, aber die Servicevalidierung sollte weiterhin sicherstellen, dass alle ausgewaehlten Zutaten zu Rezepten des aktuellen Benutzers und zum Hauptrezept/zugelassenen Beilagenkontext gehoeren.
- Wenn eine Beilage keine Zutaten hat, sollte die Gruppe im Dialog entweder als leer erkennbar sein oder ausgelassen werden. Die Anforderung verlangt Anzeige der Zutaten, nicht zwingend leerer Beilagen.
