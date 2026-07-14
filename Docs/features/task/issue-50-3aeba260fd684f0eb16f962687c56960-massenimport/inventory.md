# Bestandsaufnahme: Massenimport aus Rezeptsammlungen

## Kurzfazit

Der bestehende Import ist bereits pluginbasiert und besitzt einen sessionbasierten Hintergrundlauf mit Polling und einfacher Benutzerbestaetigung. Fuer die geforderte Chefkoch-Sammlungsunterstuetzung fehlen jedoch strukturierte Sammlungsmodelle, eine Auswahlinteraktion mit Payload, per-Rezept-Fortschritt sowie eine UI, die Auswahl, Kategoriezuordnung und Status pro Rezept abbildet.

Die wichtigste technische Entscheidung fuer die Planung ist, den bestehenden sessionbasierten Importpfad zu erweitern statt den alten synchronen `IImportService`-Pfad auszubauen. Der alte Pfad importiert sofort und kann keine Zwischenansicht abbilden.

## Detaildokumente

- [Importarchitektur und Laufzeitfluss](inventory/import-architecture.md)
- [Import-Abstraktionen und Plugin-Vertrag](inventory/import-abstractions.md)
- [Chefkoch-Parser und Sammlungsfaehigkeit](inventory/chefkoch-parser.md)
- [UI- und API-Integrationspunkte](inventory/ui-api.md)
- [Persistenz, Kategorien und Rezeptzuordnung](inventory/persistence-cookbooks.md)
- [Tests, Risiken und Rueckwaertskompatibilitaet](inventory/tests-risks.md)

## Betroffene Bereiche

| Bereich | Relevante Dateien | Bedeutung fuer die Anforderung |
| --- | --- | --- |
| Import-Abstraktionen | `Rezepte.Import.Abstractions/*` | Definiert Plugin-Vertrag, Ergebnisformat und interaktive Handler. Muss fuer Sammlungs-Vorschau und Auswahl erweitert werden. |
| Chefkoch-Plugin | `Rezepte.Import.Plugins.Chefkoch/ChefkochImportPlugin.cs` | Aktuell Einzelrezeptparser; Sammlungserkennung und Vorschau muessen hier oder in einer spezialisierten Erweiterung entstehen. |
| Import-Orchestrierung | `Rezepte.Web/Services/Import/ImportOrchestrator.cs` | Bestehender Hintergrundlauf mit Sessionstatus; geeigneter Ort fuer Auswahlzustand und per-Rezept-Fortschritt. |
| Controller | `Rezepte.Web/Controllers/CookbooksController.cs` | Stellt Start-, Status- und Confirm-Endpunkte bereit; braucht strukturierte Statusantworten und Auswahl-Endpunkt. |
| Blazor UI | `Rezepte.Web/Components/Shared/CreateRecipeDialog.razor` | Startet Import-Sessions und pollt Status; muss Zwischendialog, Auswahl, Kategorie pro Rezept und Fortschritt anzeigen. |
| Persistenz | `Rezepte.Web/Services/Import/ImportedRecipePersister.cs`, `IRecipeService` | Speichert importierte Rezepte heute in ein Zielkochbuch; fuer Massenimport braucht jedes ausgewaehlte Rezept eigene Zielzuordnung. |
| Tests | `Rezepte.Tests/Services/Import/*` | Vorhandene Tests fuer Pluginreihenfolge, interaktive Bestaetigung und Parser koennen erweitert werden. |

## Bestehender Importfluss

1. Die UI startet ueber `CreateRecipeDialog.razor` eine Import-Session fuer URL oder Datei.
2. `CookbooksController` laedt URL-Inhalte in einen `MemoryStream` und ruft `ImportOrchestrator.StartImportAsync(...)` auf.
3. Der Orchestrator erstellt eine in-memory `ImportSession`, laeuft im Hintergrund ueber aktive Plugins und ruft `CanHandleAsync` plus `HandleAsync` oder `HandleInteractiveAsync` auf.
4. Das Ergebnis wird ueber `ImportedRecipePersister` gespeichert.
5. Die UI pollt den Sessionstatus und zeigt entweder eine einfache Bestaetigung oder den Abschluss/Fehler.

## Festgestellte Luecken zur Anforderung

- Es gibt kein neutrales Modell fuer eine Rezeptsammlung mit Vorschauinformationen.
- `IImportInteraction` kann nur Textbestaetigung und Statusmeldung, aber keine Liste auswählbarer Rezepte liefern oder Auswahlantworten empfangen.
- `ImportResult` repraesentiert fertige importierte Rezepte, nicht Teilstatus einzelner Sammlungseintraege.
- `ImportedRecipePersister.PersistAsync(...)` nimmt genau ein `targetCookbookId` fuer alle Rezepte entgegen.
- `CreateRecipeDialog.razor` kennt nur einen globalen Busy-/Message-Zustand und keine per-Rezept-Statusliste.
- Der bestehende `UrlRecipeImportHandlerBase.ReadRecipeCollection(...)` lädt alle gefundenen Links sofort nach. Das widerspricht der Anforderung, Einzelrezeptseiten erst nach Auswahl abzurufen.

## Naheliegende Umsetzungspunkte

- Neue Abstraktionen fuer Sammlungsvorschau und Auswahlantwort in `Rezepte.Import.Abstractions`.
- Chefkoch-spezifische Sammlungserkennung vor dem Einzelrezept-Nachladen.
- Erweiterung der Session um Auswahlzustand, schreibgeschuetzten Laufzustand und per-Rezept-Status.
- Controller-Endpunkte fuer Status inklusive Auswahlmodell und Absenden der Auswahl.
- UI-Komponente im Importdialog fuer Sammlungslisten, Checkboxen, Kategorieauswahl und Warn-/Erfolgsstatus.
- Persistenzpfad, der pro importiertem Rezept die gewaehlte Zielkategorie beziehungsweise das gewaehlte Kochbuch verwendet.

## Offene fachliche Punkte aus der Anforderung

- Welche Vorschauinformationen aus der Chefkoch-Sammlung angezeigt werden sollen.
- Wie eine leere Auswahl bestaetigt oder verhindert werden soll.
- Ob einzelne Fehler den Gesamtstatus als teilweise erfolgreich oder fehlgeschlagen markieren.
- Ob nach geschlossenem Dialog ein Abschluss-/Fehlerhinweis erfolgen soll.

