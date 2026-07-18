# Detail: Services und Datenmodell

## Datenmodell

`Rezepte.Web/Entities/ShoppingListItem.cs` enthaelt:

- `Amount` als `decimal`
- `Unit` als nullable `string`
- `Name` als `string`
- `IsChecked` fuer den Abhakstatus

`Rezepte.Web/Entities/ShoppingListGroup.cs` gruppiert Eintraege pro Nutzer und optional pro Rezept.

Die Anforderung verlangt keine neue fachliche Trennung und keine Datenmigration. Das bestehende Datenmodell sollte beibehalten werden.

## ShoppingListService

`Rezepte.Web/Services/ShoppingListService.cs` stellt die noetigen Operationen bereits bereit:

- `GetGroupsAsync` liefert Gruppen inklusive sortierter Items.
- `AddItemAsync` persistiert `amount`, `unit`, `name`.
- `UpdateItemAsync` validiert `amount >= 0`, Pflichtfeld `name`, maximale Laengen fuer Name und Einheit, und persistiert `Amount`, `Unit`, `Name`.
- `SetItemCheckedAsync` ist unabhaengig vom Bearbeitungsmodus und bleibt fuer den Ansichtsmodus relevant.
- `DeleteItemAsync` loescht einzelne Eintraege.

Fuer die Anforderung reicht voraussichtlich, die Komponente so anzupassen, dass sie aus einem kombinierten Eingabetext wieder `amount` und `unit` erzeugt und dann den bestehenden `UpdateItemAsync` nutzt.

## SettingsService

`Rezepte.Web/Services/SettingsService.cs` speichert den Einkaufslisten-Bearbeitungsmodus pro Nutzer:

- `GetUserShoppingListEditModeAsync`
- `SetUserShoppingListEditModeAsync`

Die Symbol-Buttons muessen diese bestehende Logik weiter ausloesen. Eine Serviceaenderung ist nicht erforderlich.

## Validierungs- und Parsingfragen

Die bestehende Komponente akzeptiert fuer Menge nur numerische Eingaben. Ein kombiniertes Feld kann nicht mehr als `type="number"` modelliert werden, wenn Einheit im gleichen Feld bearbeitet werden soll. Daraus folgen Planungsentscheidungen:

- Kombiniertes Feld voraussichtlich `type="text"` oder ohne `type`.
- Formatierung beim Anzeigen: bestehende Logik `FormatAmount(item.Amount)` plus `item.Unit`, z. B. `2 kg`.
- Parsing beim Speichern: erster numerischer Anteil als Menge, Rest als Einheit.
- Dezimaltrennzeichen: bestehendes `ParseAmount` akzeptiert InvariantCulture und CurrentCulture. Diese Eigenschaft sollte fuer das kombinierte Feld erhalten bleiben.

## Keine erwarteten Aenderungen

- Keine Migration.
- Keine Aenderung an `ShoppingListItem`.
- Keine Aenderung am `IShoppingListService`-Interface.
- Keine Aenderung an Rezeptuebernahme-Methoden.
