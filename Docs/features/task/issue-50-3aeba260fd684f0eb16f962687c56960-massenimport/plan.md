# Umsetzungsplan: Massenimport aus Chefkoch-Rezeptsammlungen

## Zielbild

Der bestehende sessionbasierte Import wird so erweitert, dass Chefkoch neben Einzelrezeptseiten auch Rezeptsammlungen verarbeiten kann. Bei einer Sammlung liefert das Plugin zuerst nur eine Vorschau aus der Sammlungsseite. Die UI zeigt diese Vorschau im Importdialog, laesst den Anwender einzelne Rezepte auswaehlen und fuer jedes ausgewaehlte Rezept ein Zielkochbuch festlegen. Erst nach dem Absenden werden die ausgewaehlten Rezeptseiten geladen, importiert und pro Rezept mit Fortschritt, Erfolg oder Fehler angezeigt.

Bestehende Einzelrezeptimporte und Nicht-Chefkoch-Plugins bleiben rueckwaertskompatibel. Der synchrone Legacy-Pfad `IImportService.ImportAsync(...)` wird nicht fuer Sammlungen ausgebaut.

## Planungsannahmen

- "Kategorie" wird im vorhandenen Datenmodell als Zielkochbuch interpretiert.
- Die Vorschau zeigt die auf der Sammlungsseite ohne Einzelrezeptabruf verfuegbaren Daten: Titel, Rezept-URL und optional Vorschaubild oder Kurzmetadaten, falls robust extrahierbar.
- Eine leere Auswahl kann nicht abgesendet werden; der Import bleibt im Auswahlzustand.
- Teilfehler bei einzelnen Rezepten brechen den Import der uebrigen ausgewaehlten Rezepte nicht ab. Der Gesamtstatus wird als abgeschlossen mit Item-Fehlern dargestellt, solange mindestens ein Item erfolgreich oder bearbeitet wurde.
- Das Schliessen des Dialogs blendet nur die UI aus. Es gibt keine nachtraegliche Benachrichtigung oder Wiederanzeige des Fortschrittsdialogs.

## Arbeitspaket 1: Import-Abstraktionen erweitern

Betroffene Dateien:

- `Rezepte.Import.Abstractions/IImportHandler.cs`
- `Rezepte.Import.Abstractions/IInteractiveImportHandler.cs`
- neue Dateien unter `Rezepte.Import.Abstractions/`

Vorgehen:

1. Neue neutrale Modelle einfuehren:
   - `ImportCollectionPreview` mit Sammlungs-ID, Titel, Quelle und Items.
   - `ImportCollectionItem` mit stabiler ID, Titel, URL, optional Thumbnail-URL und optional Beschreibung/Metadaten.
   - `ImportCollectionSelection` mit ausgewaehlten Items und Zielkochbuch je Item.
   - `ImportCollectionSelectionItem` mit Item-ID, URL und Zielkochbuch-ID.
   - `ImportCollectionItemStatus` mit Item-ID, Titel, URL, Zielkochbuch-ID, Status, Fehler und erzeugter Rezept-ID.
   - `ImportCollectionItemState` mit `Pending`, `Importing`, `Succeeded`, `Failed`.
2. Optionales Interface fuer sammlungsfaehige Handler einfuehren, z. B. `ICollectionImportHandler : IImportHandler`.
3. Das Interface so schneiden, dass bestehende Handler unveraendert bleiben:
   - Vorschau aus dem Eingangsdokument erkennen und lesen.
   - Einzelnes Collection-Item nach Auswahl importieren.
4. `IImportHandler`, `IInteractiveImportHandler` und `ImportResult` unveraendert nutzbar lassen.

Akzeptanz fuer dieses Paket:

- Nicht-Chefkoch-Plugins muessen keine neuen Methoden implementieren.
- Sammlungsvorschau und Auswahlantwort sind typisiert und ohne Prompt-String-Parsen testbar.

## Arbeitspaket 2: Chefkoch-Sammlungserkennung und Item-Import

Betroffene Dateien:

- `Rezepte.Import.Plugins.Chefkoch/ChefkochImportPlugin.cs`
- ggf. neue Hilfsklasse im Chefkoch-Projekt
- Chefkoch-Testfixtures unter `Rezepte.Tests`

Vorgehen:

1. `ChefkochImportHandler` um das neue Collection-Interface erweitern.
2. Chefkoch-Sammlungsseiten anhand URL-/HTML-Merkmalen erkennen, ohne die vorhandene `UrlRecipeImportHandlerBase.ReadRecipeCollection(...)` zu nutzen.
3. Aus der Sammlungsseite nur Vorschauinformationen extrahieren:
   - Titel der Sammlung, falls vorhanden.
   - Rezepttitel.
   - absolute Rezept-URL.
   - optional Thumbnail-URL und sichtbare Metadaten, falls auf der Sammlungsseite vorhanden.
4. Stabile Item-IDs erzeugen, bevorzugt aus normalisierter Rezept-URL oder Chefkoch-Rezept-ID.
5. Einzelrezeptimport fuer ausgewaehlte Items ueber den bestehenden Einzelrezeptparser wiederverwenden.
6. Sicherstellen, dass Chefkoch-Einzelrezeptseiten weiterhin den bisherigen `CanHandleAsync`/`HandleAsync`-Pfad verwenden.

Akzeptanz fuer dieses Paket:

- Eine Chefkoch-Sammlungsseite erzeugt eine Vorschau, ohne Einzelrezeptseiten zu laden.
- Nur ausgewaehlte Item-URLs werden spaeter nachgeladen.
- Einzelrezeptfixtures bleiben unveraendert erfolgreich.

## Arbeitspaket 3: Orchestrator und Sessionzustand erweitern

Betroffene Dateien:

- `Rezepte.Web/Services/Import/ImportOrchestrator.cs`
- `Rezepte.Web/Services/Import/IImportedRecipePersister.cs`
- `Rezepte.Web/Services/Import/ImportedRecipePersister.cs`

Vorgehen:

1. `ImportSession` um strukturierte Zustandsdaten erweitern:
   - `State`: `Queued`, `Checking`, `SelectionRequired`, `Importing`, `Completed`, `Failed`.
   - `CollectionPreview`.
   - `CollectionItems` als per-Rezept-Statusliste.
   - `ReadOnly` nach Absenden der Auswahl.
2. Im Startlauf nach einem passenden `ICollectionImportHandler` zuerst pruefen, ob eine Sammlungsvorschau vorliegt.
3. Bei Sammlung:
   - Session auf `SelectionRequired` setzen.
   - Hintergrundlauf nicht beenden, sondern auf Auswahl per `TaskCompletionSource<ImportCollectionSelection>` warten.
4. Neue Orchestrator-Methode einfuehren, z. B. `SubmitSelection(sessionId, selection)`.
5. Nach Auswahl:
   - Auswahl validieren.
   - Session auf `Importing` und `ReadOnly = true` setzen.
   - Items sequenziell oder kontrolliert parallel verarbeiten. Konservativ sequenziell starten, damit Fortschritt und Fehler einfach nachvollziehbar bleiben.
   - Pro Item Status auf `Importing`, danach `Succeeded` oder `Failed` setzen.
6. Persistenz pro Item mit dessen Zielkochbuch ausfuehren.
7. Fehler pro Item abfangen und speichern, ohne die restlichen Items zu stoppen.
8. Bestehende interaktive Bestaetigung ueber `IImportInteraction` unveraendert lassen.

Akzeptanz fuer dieses Paket:

- `Confirm(...)` funktioniert weiterhin fuer alte interaktive Plugins.
- `SubmitSelection(...)` ist getrennt von der alten Bestaetigung.
- Ein fehlgeschlagenes Item verhindert nicht die Verarbeitung weiterer ausgewaehlter Items.

## Arbeitspaket 4: Persistenz pro ausgewaehltem Rezept

Betroffene Dateien:

- `Rezepte.Web/Services/Import/ImportedRecipePersister.cs`
- `Rezepte.Web/Services/Import/IImportedRecipePersister.cs`
- ggf. neue kleine Ergebnis-/Requestmodelle im Web-Projekt

Vorgehen:

1. Bestehendes `PersistAsync(ImportResult, targetCookbookId, ...)` fuer Einzelimporte beibehalten.
2. Neue Methode oder Hilfsroutine fuer genau ein importiertes Rezept mit Zielkochbuch einfuehren.
3. Die bestehende Logik fuer `FindByUri`, Update/Create, Step-Konvertierung und Bilder wiederverwenden.
4. Bei Persistenzfehlern ein Item-spezifisches Fehlerergebnis zurueckgeben statt den gesamten Massenimport global abzubrechen.
5. Erzeugte oder aktualisierte Rezept-ID im jeweiligen Itemstatus speichern.

Akzeptanz fuer dieses Paket:

- Jedes ausgewaehlte Rezept wird dem individuell gewaehlten Zielkochbuch zugeordnet.
- Einzelimport mit globalem Zielkochbuch bleibt unveraendert.

## Arbeitspaket 5: API-Endpunkte und Status-DTOs

Betroffene Dateien:

- `Rezepte.Web/Controllers/CookbooksController.cs`
- ggf. neue DTO-Dateien unter `Rezepte.Web`

Vorgehen:

1. Statusantwort der bestehenden Status-Endpunkte rueckwaertskompatibel erweitern:
   - bisherige Felder `status`, `waitingForConfirmation`, `confirmationPrompt`, `result` beibehalten.
   - neue Felder `state`, `readOnly`, `collection`, `items` ergaenzen.
2. Auswahl-Endpunkte ergaenzen:
   - `POST api/cookbooks/{cookbookId}/import-session/{sessionId}/selection`
   - `POST api/cookbooks/import-session/{sessionId}/selection`
3. Payload validieren:
   - Session existiert.
   - Session ist im Zustand `SelectionRequired`.
   - mindestens ein Item ist ausgewaehlt.
   - alle Item-IDs existieren in der Vorschau.
   - Zielkochbuch-ID je Item ist gesetzt.
4. Fehlerhafte Requests mit passenden HTTP-Statuscodes beantworten.
5. Start-Endpunkte unveraendert lassen.

Akzeptanz fuer dieses Paket:

- Bestehendes UI-Polling bricht durch die erweiterten Statusantworten nicht.
- Auswahl kann fuer Starts mit und ohne initiales Kochbuch abgesendet werden.

## Arbeitspaket 6: Blazor-UI fuer Sammlungen

Betroffene Dateien:

- `Rezepte.Web/Components/Shared/CreateRecipeDialog.razor`
- ggf. neue Unterkomponente, z. B. `ImportCollectionSelection.razor`

Vorgehen:

1. Statusmodell in der Komponente um Collection-Status erweitern.
2. Beim Polling `selectionRequired`/`SelectionRequired` erkennen und die Sammlungsauswahl anzeigen.
3. UI im bestehenden Importdialog oder als Unterkomponente umsetzen:
   - Liste der Vorschau-Rezepte.
   - Checkbox pro Rezept.
   - Kochbuchauswahl pro ausgewaehltem Rezept.
   - Absenden-Button, deaktiviert bei leerer Auswahl oder fehlender Zielzuordnung.
   - Schreibgeschuetzte Darstellung nach Absenden.
   - Statussymbol pro Item: wartend, laeuft, Erfolg, Warnung.
   - Fehlermeldung pro Item ueber vorhandenes Warnsymbol/Tooltip oder aufklappbare Detailzeile.
4. Schliessen-Button so belassen bzw. anpassen, dass er nur den Dialog ausblendet und keine Session-Cancellation ausloest.
5. Bestehende einfache Bestaetigungs-UI weiter fuer `waitingForConfirmation` nutzen.
6. Falls die Komponente aktuell keine Kochbuchliste fuer freie Zielauswahl besitzt, vorhandene Cookbook-Datenquelle wiederverwenden oder minimal einen API-/State-Pfad anbinden, der die bereits im UI verfuegbaren Kochbuecher nutzt.

Akzeptanz fuer dieses Paket:

- Vor Absenden sind Auswahl und Zielkochbuch aenderbar.
- Nach Absenden sind Auswahl und Zielkochbuch gesperrt.
- Fortschritt und Fehler bleiben pro Rezept sichtbar, solange der Dialog offen ist.
- Schliessen beendet den Import nicht.

## Arbeitspaket 7: Tests

Betroffene Dateien:

- `Rezepte.Tests/Services/Import/ImportOrchestratorTests.cs`
- `Rezepte.Tests/Services/Import/ProductiveImportPluginParserTests.cs`
- ggf. Controller-/Komponententests, falls im Projekt bereits Muster vorhanden sind

Vorgehen:

1. Chefkoch-Parsertests:
   - Einzelrezept bleibt importierbar.
   - Sammlungsseite wird erkannt.
   - Vorschau enthaelt erwartete Items.
   - Vorschaupfad laedt keine Einzelrezeptseiten.
2. Orchestrator-Tests:
   - Collection-Handler setzt Session auf `SelectionRequired`.
   - Leere oder ungueltige Auswahl wird abgelehnt.
   - Mehrere ausgewaehlte Items werden verarbeitet.
   - Nicht ausgewaehlte Items werden nicht verarbeitet.
   - Fehler eines Items stoppt andere Items nicht.
   - Alte interaktive Bestaetigung funktioniert weiterhin.
3. Persistenztests:
   - Zielkochbuch wird pro Item verwendet.
   - Persistenzfehler bleibt item-spezifisch.
4. API-Tests, sofern vorhandene Testinfrastruktur passt:
   - Status enthaelt Collection-DTO.
   - Selection-Endpunkt validiert Zustand und Payload.
5. UI-nahe Tests oder manuelle Verifikation:
   - Auswahl ist nach Absenden schreibgeschuetzt.
   - Warnsymbol zeigt Fehlerdetails.
   - Dialogschliessen beendet die Session nicht.

## Reihenfolge der Umsetzung

1. Modelle und optionales Collection-Interface in `Rezepte.Import.Abstractions`.
2. Chefkoch-Vorschauparser mit Fixtures und Tests.
3. Orchestrator-Sessionzustand und `SubmitSelection(...)`.
4. Persistenz pro Item.
5. Controller-DTOs und Selection-Endpunkte.
6. Blazor-UI fuer Auswahl, Zielkochbuch und Fortschritt.
7. Vollstaendige Testabdeckung und Regressionstest fuer Einzelimporte.

Diese Reihenfolge haelt die fachliche Logik testbar, bevor die UI angebunden wird.

## Rueckwaertskompatibilitaet

- Bestehende `IImportHandler`-Implementierungen bleiben gueltig.
- Bestehende `IInteractiveImportHandler`-Implementierungen bleiben gueltig.
- `ImportResult` bleibt fuer Einzelimporte unveraendert nutzbar.
- Nicht-Chefkoch-Plugins liefern weiterhin genau ein Rezept.
- Der vorhandene Confirm-Endpunkt bleibt fuer einfache interaktive Bestaetigungen bestehen.
- Die Statusantwort wird nur additiv erweitert.

## Risiken und Gegenmassnahmen

- Chefkoch-HTML kann sich aendern: Parser mit lokalen HTML-Fixtures absichern und Extraktion defensiv halten.
- Vorzeitiges Nachladen von Einzelrezepten waere ein Akzeptanzkriteriumsbruch: Collection-Pfad darf `ReadRecipeCollection(...)` der Basisklasse nicht verwenden.
- "Kategorie" koennte fachlich nicht "Kochbuch" bedeuten: Umsetzung dokumentiert die Annahme; bei spaeterer Klaerung muss nur die Zielzuordnungsschicht ausgetauscht werden.
- In-memory Sessions verlieren Fortschritt bei Serverneustart: entspricht dem aktuellen Architekturstand und wird nicht in dieser Anforderung geloest.
- Parallelimport koennte externe Seiten und UI-Status verkomplizieren: initial sequenziell implementieren, spaeter optional optimieren.

## Manuelle Pruefung

Nach Implementierung manuell pruefen:

1. Chefkoch-Einzelrezept importieren und bestaetigen, dass der bisherige Ablauf unveraendert ist.
2. Chefkoch-Sammlungs-URL `https://www.chefkoch.de/rezeptsammlung/3212418/Erdbeerzeit.html` starten.
3. Sicherstellen, dass vor Auswahl keine Einzelrezeptseiten geladen werden.
4. Mehrere Rezepte mit unterschiedlichen Zielkochbuechern auswaehlen und absenden.
5. Fortschritt, Erfolgshaken und Fehleranzeige pro Item pruefen.
6. Dialog waehrend des Imports schliessen und serverseitig sicherstellen, dass der Import weiterlaeuft.
7. Nicht-Chefkoch-Import mit bestehendem Plugin pruefen.

## Offene Punkte

Keine. Die offenen Punkte aus der Anforderung sind durch die Planungsannahmen fuer diese Umsetzung festgelegt.
