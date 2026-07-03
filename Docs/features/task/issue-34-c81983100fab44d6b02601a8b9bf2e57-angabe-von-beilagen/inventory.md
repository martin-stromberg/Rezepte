# Bestandsaufnahme: Angabe von Beilagen

## Ausfuehrung

- Lifecycle-Schritt: 4 - Bestandsaufnahme
- Eingabe: `requirement.md`
- Hinweis: In dieser Umgebung stand kein separates Unteragenten-Werkzeug zur Verfuegung. Die Bestandsaufnahme wurde lokal nach dem `/inventory`-Ziel durchgefuehrt.

## Kurzfazit

Die Anwendung besitzt derzeit keine fachliche Modellierung fuer Beilagen. Rezepte sind nur ueber Kochbuchzuordnungen mit anderen Entitaeten verknuepft; Kalenderereignisse und Einkaufslistengruppen referenzieren jeweils ein einzelnes Rezept. Die Anforderung benoetigt deshalb eine neue benutzerbezogene Rezept-zu-Rezept-Beziehung, Erweiterungen an Rezept-DTOs/API/Service, UI zur Pflege in der Rezeptbearbeitung sowie Anpassungen an Kalender- und Einkaufslisten-Dialogen.

## Detaildokumente

- [Datenmodell und Persistenz](inventory/data-model.md)
- [Rezeptverwaltung und API](inventory/recipe-management.md)
- [Kalenderintegration](inventory/calendar-integration.md)
- [Einkaufslistenintegration](inventory/shopping-list-integration.md)
- [Tests und Absicherung](inventory/tests.md)

## Relevante Bereiche

| Bereich | Aktueller Zustand | Bedeutung fuer Beilagen |
|---------|-------------------|---------------------------|
| Rezeptdatenmodell | `Recipe` enthaelt Stammdaten, Schritte, Bilder und Kochbuchzuordnungen, aber keine Selbstreferenz. | Neue Entitaet/Relation fuer Beilagen erforderlich. |
| Rezept-API | Create/Update/Get DTOs transportieren Titel, Beschreibung, URI, Portionen und Schritte. | DTOs und Service-Signaturen muessen Beilagen-IDs aufnehmen und ausgeben. |
| Rezeptbearbeitung | `RecipeEdit.razor` speichert nur Stammdaten, Quelle, Portionen, Schritte und Zutaten. | UI fuer Auswahl/Entfernung verlinkter Beilagen fehlt. |
| Kalender | `CalendarEventDialog` erstellt ein einzelnes Event fuer ein Rezept. | Vorgeschlagene Beilagen muessen beim Speichern optional als weitere Events angelegt werden. |
| Einkaufsliste | Dialog laedt nur Zutaten des Hauptrezepts und waehlt alle initial aus. | Zutaten verlinkter Beilagen muessen gruppiert angezeigt und initial abgewahlt werden. |
| Tests | Service-Tests fuer Rezept und Einkaufsliste existieren; Kalender-Service-Test fehlt. | Neue Regeln sollten vor allem auf Service-Ebene und fuer Einkaufslistenuebernahme abgedeckt werden. |

## Wichtige Erweiterungspunkte

- `Rezepte.Web/Entities/Recipe.cs`: Navigationen fuer Hauptrezept und Beilagenbeziehungen ergaenzen.
- `Rezepte.Web/Data/RezepteDbContext.cs`: neue Join-Entitaet konfigurieren, inklusive User-Grenzen, Unique Index und Delete-Verhalten.
- `Rezepte.Web/Services/RecipeService.cs`: Beilagen laden, validieren, speichern und in DTO-nahen Ergebnissen verfuegbar machen.
- `Rezepte.Web/Controllers/RecipesController.cs`: Create/Update/Get-DTOs um Beilagen erweitern; optional separaten Endpoint fuer Beilagenvorschlaege bereitstellen.
- `Rezepte.Web/Components/Pages/RecipeEdit.razor`: Beilagenverwaltung in die Rezeptbearbeitung aufnehmen.
- `Rezepte.Web/Components/Shared/CalendarEventDialog.razor`: verlinkte Beilagen fuer das gewaehlte Rezept anzeigen, einzeln auswählbar machen und beim Speichern uebernehmen.
- `Rezepte.Web/Components/Shared/AddRecipeToShoppingListDialog.razor` und `Rezepte.Web/Services/ShoppingListService.cs`: Zutaten von Hauptrezept und Beilagen gruppiert liefern, Hauptrezept initial ausgewaehlt, Beilagen initial nicht ausgewaehlt.

## Offene technische Punkte

- Es gibt keine vorhandene generische Rezeptauswahl-Komponente fuer Mehrfachauswahl; der Kalenderdialog enthaelt eine lokale Suche, die fuer Beilagenverwaltung wiederverwendet oder extrahiert werden kann.
- `RecipeService.GetAvailableForCookbookAsync` liefert nach aktueller Logik Rezepte, die in irgendeinem anderen Kochbuch liegen; fuer Beilagenauswahl ist wahrscheinlich eine eigene Suche sinnvoll.
- Die Kalenderanforderung kann entweder clientseitig durch mehrere bestehende `POST /api/calendar`-Aufrufe oder serverseitig durch einen erweiterten Batch-Endpunkt umgesetzt werden. Der bestehende Service kann bereits einzelne Termine validiert anlegen.
- Einkaufslisten-Gruppierung ist im Persistenzmodell vorhanden (`ShoppingListGroup`), aber der Uebernahmedialog bildet aktuell nur eine Gruppe pro Hauptrezept.
