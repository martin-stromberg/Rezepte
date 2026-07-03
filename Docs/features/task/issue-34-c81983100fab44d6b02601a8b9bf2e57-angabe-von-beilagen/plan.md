# Umsetzungsplan: Angabe von Beilagen

## Ausfuehrung

- Lifecycle-Schritt: 5 - Umsetzungsplanung
- Eingaben:
  - `requirement.md`
  - `inventory.md`
  - Detaildokumente unter `inventory/`
- Hinweis: In dieser Umgebung stand kein separates Unteragenten-Werkzeug zur Verfuegung. Die Planung wurde lokal nach dem `/plan`-Ziel erstellt.

## Zielbild

Rezepte koennen andere Rezepte desselben Benutzers als moegliche Beilagen referenzieren. Diese Beilagen werden in der Rezeptbearbeitung gepflegt, beim Planen eines Kalendertermins fuer das Hauptrezept als einzeln auswaehlbare Zusatztermine vorgeschlagen und im Einkaufslisten-Dialog als eigene Rezeptgruppen angezeigt. Zutaten des Hauptrezepts sind initial ausgewaehlt, Zutaten der Beilagen initial nicht.

## Architekturentscheidungen

- Beilagen werden als neue selbstreferenzielle Join-Entitaet `RecipeSideDish` modelliert.
- Die Beziehung ist benutzerbezogen ueber die beiden beteiligten Rezepte abgesichert; direkte Speicherung eines separaten `UserId` auf der Join-Entitaet ist nicht erforderlich, kann aber fuer einfachere Indizes bewusst ergaenzt werden.
- Selbstreferenzen werden im Service abgelehnt.
- Doppelte Beilagen-IDs werden im Service normalisiert.
- Kalenderbeilagen werden clientseitig ueber mehrere bestehende `POST /api/calendar`-Aufrufe angelegt. Dadurch bleibt die Calendar-API klein; Teilfehler werden im Dialog als Fehler angezeigt.
- Einkaufslistenzutaten werden service-seitig gruppiert geliefert und service-seitig als getrennte Einkaufslistengruppen pro Rezept gespeichert. So bleibt die spaetere Einkaufsliste eindeutig nach Herkunftsrezept getrennt.
- Die bestehende Rezeptsuche unter `api/recipes/search` wird fuer die Beilagenauswahl wiederverwendet.

## Arbeitspakete

### 1. Datenmodell und Migration

- Neue Entitaet `Rezepte.Web/Entities/RecipeSideDish.cs` anlegen:
  - `Id`
  - `RecipeId`
  - `SideDishRecipeId`
  - optional `OrderIndex`
  - Navigation `Recipe`
  - Navigation `SideDishRecipe`
- `Recipe` um Navigationen ergaenzen:
  - `SideDishes`
  - optional `UsedAsSideDishFor`
- `RezepteDbContext` erweitern:
  - `DbSet<RecipeSideDish>`
  - Unique Index auf `{ RecipeId, SideDishRecipeId }`
  - Index auf `SideDishRecipeId`
  - zwei explizite Beziehungen zu `Recipe`
  - Delete-Verhalten so konfigurieren, dass keine mehrdeutigen Cascade-Pfade entstehen. Empfohlen: Hauptrezept-Zuordnungen cascade, Beilagenreferenzen restrict oder no action; beim Rezeptloeschen referenzierende Beilagenzuordnungen im Service vorher entfernen.
- EF-Migration unter `Rezepte.Web/Migrations` erzeugen.

### 2. Rezeptservice und API

- DTO-/Record-Typ fuer Beilagen einfuehren, z. B. `RecipeSideDishDto(string Id, string Title, string? Description, string? ImageUrl, int Portions)`.
- `IRecipeService` erweitern:
  - `CreateAsync(..., IReadOnlyCollection<string> sideDishRecipeIds, ...)`
  - `UpdateAsync(..., IReadOnlyCollection<string> sideDishRecipeIds, ...)`
  - optional `GetSideDishesAsync(userId, recipeId, ct)`, falls Dialoge nicht das komplette Rezept laden sollen.
- `RecipeService.GetByIdAsync` so erweitern, dass Beilagen in stabiler Reihenfolge geladen werden.
- Beim Erstellen und Aktualisieren:
  - Beilagen-IDs trimmen, leere IDs entfernen, Duplikate entfernen.
  - Selbstreferenz ablehnen.
  - Nur Rezepte desselben Benutzers erlauben.
  - Nicht gefundene oder fremde IDs als Fehler behandeln.
  - bestehende Beilagenbeziehungen beim Update ersetzen.
- Beim Loeschen eines Rezepts vorhandene `RecipeSideDish`-Zuordnungen als Haupt- und Beilagenrezept entfernen, bevor das Rezept geloescht wird, falls die DB-Konfiguration nicht alles automatisch entfernt.
- `RecipesController` erweitern:
  - `CreateRecipeRequest` und `UpdateRecipeRequest` um `List<string> SideDishRecipeIds`.
  - `RecipeDto` um `List<RecipeSideDishDto> SideDishes`.
  - optional Endpoint `GET /api/recipes/{id}/side-dishes`, wenn Kalenderdialog und Einkaufslistenlogik gezielt nur Beilagen laden sollen.
- Darauf achten, dass bestehende Clients ohne `sideDishRecipeIds` weiter funktionieren.

### 3. Beilagenverwaltung in der Rezeptbearbeitung

- `RecipeEdit.razor` um einen Abschnitt "Beilagen" ergaenzen:
  - Suchfeld fuer Rezepte.
  - Trefferliste aus `api/recipes/search`.
  - ausgewaehlte Beilagen als Liste mit Entfernen-Aktion.
  - aktuelles Rezept darf nicht auswaehlbar sein.
  - bereits ausgewaehlte Beilagen werden nicht erneut hinzugefuegt.
- Beim Laden eines bestehenden Rezepts `SideDishes` aus dem DTO in den lokalen Zustand uebernehmen.
- Beim Speichern `sideDishRecipeIds` in Create- und Update-Payload senden.
- Optional `RecipePage.razor` um eine nicht-editierbare Beilagenliste ergaenzen, damit die gepflegten Beilagen auch in der Rezeptansicht sichtbar sind.

### 4. Kalenderintegration

- `CalendarEventDialog.razor` erweitern:
  - Wenn ein Rezept ausgewaehlt oder aus `InitialEvent` gesetzt wird, Beilagen des Rezepts laden.
  - Beilagen nur fuer neue Termine (`EditingId == null`) als Vorschlaege anzeigen.
  - Jede Beilage mit Checkbox darstellen; initial ausgewaehlt ist empfehlenswert, aber die Anforderung verlangt nur einzeln auswaehlbar. Falls UX-seitig vorsichtiger gewuenscht, initial nicht ausgewaehlt; die Akzeptanzkriterien bleiben erfuellt.
  - Beim Entfernen oder Wechseln des Hauptrezepts Beilagenzustand zuruecksetzen.
- `SubmitForm` bei neuen Terminen:
  - Haupttermin wie bisher anlegen.
  - Fuer jede ausgewaehlte Beilage einen weiteren `POST /api/calendar` mit gleichem Datum, gleicher Tageszeit, gleicher Wiederholung und gleicher Portionszahl senden, aber `RecipeId` der Beilage verwenden.
  - Fehler sichtbar im Dialog anzeigen und `OnSaved` erst nach erfolgreicher Anlage aller gewuenschten Termine ausloesen.
- Bearbeiten bestehender Termine bleibt unveraendert, da die Anforderung das Einfuegen eines Rezepts beschreibt.

### 5. Einkaufslistenintegration

- Neue Service-DTOs einfuehren, z. B.:
  - `ShoppingListRecipeIngredientGroup(string RecipeId, string RecipeTitle, bool IsMainRecipe, List<ShoppingListRecipeIngredient> Ingredients)`
  - optional `ShoppingListRecipeIngredientSelection(string RecipeId, IReadOnlyCollection<string> IngredientIds)`.
- `IShoppingListService` erweitern:
  - `GetRecipeIngredientGroupsAsync(userId, recipeId, ct)` liefert Hauptrezept plus verlinkte Beilagen.
  - `AddRecipeIngredientGroupsAsync(userId, recipeId, IReadOnlyCollection<...> selections, ct)` speichert ausgewaehlte Zutaten in getrennten Gruppen pro Rezept.
- Servicevalidierung:
  - Hauptrezept muss dem Benutzer gehoeren.
  - Beilagen muessen ueber `RecipeSideDish` mit dem Hauptrezept verlinkt sein.
  - Ausgewaehlte Zutaten muessen zum jeweiligen erlaubten Rezept gehoeren.
  - Leere Auswahl wird abgelehnt.
- `AddRecipeToShoppingListDialog.razor` von flacher Zutatenliste auf Gruppen umstellen:
  - Hauptrezeptgruppe zuerst anzeigen.
  - Beilagengruppen darunter mit erkennbarem Rezepttitel anzeigen.
  - Zutaten des Hauptrezepts initial auswaehlen.
  - Zutaten der Beilagen initial nicht auswaehlen.
  - Speichern sendet die Auswahl gruppiert an den neuen Service-Aufruf.
- Wenn alle Gruppen keine Zutaten enthalten, bleibt die vorhandene Info-Meldung sinngemaess erhalten.

### 6. Tests

- `Rezepte.Tests/Services/RecipeServiceTests.cs` erweitern:
  - Beilagen werden beim Erstellen gespeichert.
  - Beilagen werden beim Aktualisieren ersetzt.
  - Selbstreferenz wird abgelehnt.
  - fremde Benutzerrezepte werden abgelehnt.
  - Duplikate werden nicht doppelt gespeichert.
  - `GetByIdAsync` liefert Beilagen sortiert zurueck.
- `Rezepte.Tests/Services/ShoppingListServiceTests.cs` erweitern:
  - gruppierte Zutaten enthalten Hauptrezept und Beilagen.
  - `IsMainRecipe` ist korrekt gesetzt.
  - Persistieren einer Auswahl erzeugt getrennte Einkaufslistengruppen.
  - manipulierte Zutaten aus nicht verlinkten Rezepten werden nicht uebernommen.
- Falls Kalenderlogik rein in der Blazor-Komponente bleibt, ist sie in diesem Testsetup voraussichtlich nur manuell pruefbar. Falls sie in einen Service extrahiert wird, Unit-Tests fuer die Mehrfachanlage ergaenzen.

## Verifikation

- `dotnet test`
- Anwendung lokal starten und manuell pruefen:
  - Rezept bearbeiten, Beilage suchen, hinzufuegen, entfernen, speichern und neu laden.
  - Rezept in Kalender einfuegen, Beilagenvorschlaege einzeln auswaehlen und Termine pruefen.
  - Rezept zur Einkaufsliste uebernehmen, Gruppierung und Initialauswahl pruefen.

## Akzeptanzkriterien-Abdeckung

| Akzeptanzkriterium | Umsetzung |
|--------------------|-----------|
| Benutzer koennen Beilagen als Rezeptverlinkungen zu einem Rezept verwalten. | Datenmodell `RecipeSideDish`, API-Erweiterung und Beilagenabschnitt in `RecipeEdit.razor`. |
| Beim Einfuegen eines Rezepts in den Kalender werden verlinkte Beilagen vorgeschlagen. | `CalendarEventDialog.razor` laedt und zeigt Beilagen fuer das ausgewaehlte Rezept. |
| Benutzer koennen jede vorgeschlagene Beilage einzeln zum Kalender hinzufuegen. | Checkbox-Auswahl und zusaetzliche `POST /api/calendar`-Aufrufe fuer ausgewaehlte Beilagen. |
| Beim Eintragen eines Rezepts in die Einkaufsliste werden Zutaten der verlinkten Beilagen im Dialog angezeigt. | `GetRecipeIngredientGroupsAsync` liefert Hauptrezept und Beilagen; Dialog rendert Gruppen. |
| Zutaten der Beilagen sind im Einkaufslisten-Dialog standardmaessig nicht ausgewaehlt. | Initialauswahl im Dialog setzt nur Zutaten der Hauptrezeptgruppe. |
| Zutaten im Einkaufslisten-Dialog sind nach dem jeweiligen Rezept gruppiert. | Gruppiertes DTO und UI-Abschnitte pro Rezept; Speicherung als getrennte Einkaufslistengruppen. |

## Risiken und Gegenmassnahmen

- Selbstreferenzielle EF-Beziehungen koennen mehrere Cascade-Pfade erzeugen. Gegenmassnahme: Delete-Verhalten explizit konfigurieren und Rezeptloeschung im Service absichern.
- Mehrere clientseitige Kalender-POSTs sind nicht atomar. Gegenmassnahme: Fehler im Dialog anzeigen; bei spaeterem Bedarf Batch-Endpunkt nachruesten.
- Rezeptsuche kann das aktuelle Rezept zurueckliefern. Gegenmassnahme: UI und Service validieren Selbstreferenzen.
- Einkaufslisten-Manipulation ueber Ingredient-IDs ist moeglich, wenn nur IDs geprueft werden. Gegenmassnahme: Service prueft Rezeptzuordnung jeder ausgewaehlten Zutat gegen Hauptrezept oder verlinkte Beilage.

## Offene Punkte

- Keine.
