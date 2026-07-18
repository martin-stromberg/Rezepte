# Detail: Oberflaeche und CSS

## Komponente

`Rezepte.Web/Components/Pages/ShoppingList.razor` ist eine autorisierte Interactive-Server-Blazor-Seite fuer `/shopping-list`.

Wichtige Stellen:

- Zeilen 17-33: Kopfbereich mit Button-Group fuer `Ansicht` und `Bearbeiten`; beide Buttons haben sichtbaren Text und `aria-pressed`, aber keine Symbol-only-Darstellung.
- Zeilen 95-130: Eintragszeile. Die Checkbox wird immer vor dem `isEditMode`-Block gerendert; dadurch erscheint sie auch im Bearbeitungsmodus.
- Zeilen 103-117: Im Bearbeitungsmodus existieren drei getrennte Inputs fuer Menge, Einheit und Zutat.
- Zeilen 118-124: Der Loeschen-Button ist bereits ein Symbol-Button mit `aria-label` und `title`.
- Zeilen 252-269: `SetEditModeAsync` persistiert den Modus und fokussiert bei einer Gruppe das Draft-Mengenfeld.
- Zeilen 271-290: Bestehende Update-Handler speichern Menge, Einheit und Name ueber `UpdateItemAsync`.
- Zeilen 329-347: `ParseAmount`, `FormatAmount` und `FormatIngredient` sind vorhandene Hilfsfunktionen fuer getrennte Menge/Einheit und Anzeigeformat.

## CSS

`Rezepte.Web/Components/Pages/ShoppingList.razor.css` definiert die komplette Einkaufslistenstruktur.

Wichtige Stellen:

- Zeilen 13-19: Gruppenheader, Hinzufuegeformular und Eintragszeilen sind CSS-Grids mit `gap: 0.5rem`.
- Zeilen 30-32: Das Hinzufuegeformular nutzt vier Spalten fuer Menge, Einheit, Zutat und Button.
- Zeilen 40-42: Eine Bearbeitungszeile nutzt fuenf Spalten: Checkbox, Menge, Einheit, Zutat, Button.
- Zeilen 44-46: Der Ansichtsmodus wird ueber `:has(.shopping-item-text)` auf Checkbox plus Text reduziert.
- Zeilen 54-74: Mobile Breakpoint-Regeln reduzieren `.shopping-add-row` und `.shopping-item` auf drei Spalten, setzen aber alle `.form-control` auf volle Breite. Diese Regel verursacht die Stapelung der Eingabefelder.

## Umsetzungsimplikationen

- Das Markup sollte im Bearbeitungsmodus eine eindeutige Struktur erhalten, z. B. ohne Checkbox und mit kombiniertem Mengen-/Einheitsfeld.
- Eine eigene CSS-Klasse fuer editierbare Eintragszeilen waere robuster als reine Kindselektoren, weil der Ansichtsmodus weiterhin Checkbox plus Text behalten soll.
- Das mobile Grid fuer eine Bearbeitungszeile sollte drei Spalten abbilden: kombiniertes Mengenfeld, Zutatenfeld, Loeschen-Button. Die Spalten muessen mit `minmax(0, ...)` und begrenzten Button-/Inputbreiten gegen Overflow abgesichert werden.
- Die Modusbuttons koennen vorhandene Bootstrap Icons verwenden, da Bootstrap Icons global in `App.razor` eingebunden sind.

## Nicht direkt betroffene Oberflaechen

- `Rezepte.Web/Components/Shared/AddRecipeToShoppingListDialog.razor` nutzt den ShoppingListService zum Uebernehmen von Rezeptzutaten, ist aber nicht die Bearbeitungsansicht der Einkaufsliste.
- `Rezepte.Web/Components/Pages/RecipePage.razor` oeffnet den Dialog; keine direkte Layoutrelevanz fuer `/shopping-list`.
- `Rezepte.Web/Components/Layout/MainLayout.razor` verlinkt auf die Einkaufsliste; keine Aenderung erforderlich.
