# UI- und API-Integrationspunkte

## Controller

`CookbooksController` stellt bereits sessionbasierte Endpunkte bereit:

- `POST api/cookbooks/{cookbookId}/import-session/start` ab `Rezepte.Web/Controllers/CookbooksController.cs:303`
- `GET api/cookbooks/{cookbookId}/import-session/{sessionId}/status` ab `Rezepte.Web/Controllers/CookbooksController.cs:341`
- `POST api/cookbooks/{cookbookId}/import-session/{sessionId}/confirm` ab `Rezepte.Web/Controllers/CookbooksController.cs:355`
- Varianten ohne `cookbookId` ab `CookbooksController.cs:364`, `CookbooksController.cs:437`, `CookbooksController.cs:451`
- Datei-Session-Endpunkte ab `CookbooksController.cs:460` und `CookbooksController.cs:492`
- gemeinsamer Start-Helfer `StartImportSessionFromStreamAsync(...)` ab `CookbooksController.cs:520`

Die Statusantwort enthaelt aktuell nur:

- `status`
- `waitingForConfirmation`
- `confirmationPrompt`
- `result`

## Benötigte API-Erweiterungen

Fuer die Sammlungsauswahl braucht der Status eine strukturierte Antwort, z. B.:

- `state`: `checking`, `selectionRequired`, `importing`, `completed`, `failed`
- `collection`: Titel/Quelle und Items fuer die Vorschau
- `items`: per-Rezept-Status, Fehler und erzeugte Rezept-ID
- `readOnly`: true nach Absenden

Zusaetzlich braucht es einen Endpunkt zum Absenden der Auswahl, z. B.:

- `POST api/cookbooks/{cookbookId}/import-session/{sessionId}/selection`
- `POST api/cookbooks/import-session/{sessionId}/selection`

Payload sollte mindestens enthalten:

- ausgewaehlte Item-IDs oder URLs
- Zielkochbuch/Kategorie je ausgewähltem Rezept

## Blazor-Komponente

`CreateRecipeDialog.razor` ist der zentrale UI-Ort:

- Dateiimport startet in `OnFileChange(...)` ab `Rezepte.Web/Components/Shared/CreateRecipeDialog.razor:182`.
- URL-Import startet in `ImportFromUrlAsync(...)` ab `CreateRecipeDialog.razor:320`.
- Statuspolling fragt `statusEndpoint` in beiden Flows ab.
- Die einfache Bestaetigung wird ueber `ShowUserConfirmationAsync(...)` ab `CreateRecipeDialog.razor:434` angezeigt.
- Der Testlauf nutzt denselben Sessionpfad ab `CreateRecipeDialog.razor:524`.

## Benötigte UI-Erweiterungen

Die vorhandene `confirmVisible`-UI reicht nicht aus. Erforderlich ist ein strukturierter Sammlungsdialog im bestehenden Importdialog oder als neue Unterkomponente:

- Liste der Vorschau-Rezepte.
- Checkbox je Rezept.
- Kategorie-/Kochbuchauswahl je ausgewaehltem Rezept.
- Absenden-Button.
- Nach Absenden schreibgeschuetzte Darstellung.
- Per-Rezept Statusanzeige mit Erfolgshaken oder Warnsymbol.
- Anzeige der konkreten Fehlermeldung beim Warnsymbol.
- Schliessen-Button, der nur die UI ausblendet und die Session nicht abbricht.

## Daten fuer Kategorieauswahl

Die Anforderung spricht von Kategoriezuordnung. Im Code entspricht das wahrscheinlich Kochbuchzuordnung:

- `CreateRecipeDialog.Show(string? cookbookId = null)` kann einen Zielkontext erhalten.
- `ImportedRecipePersister` speichert aktuell in ein `targetCookbookId`.

Falls "Kategorie" fachlich nicht identisch mit Kochbuch ist, fehlt im aktuellen Datenmodell ein separates Kategorieobjekt. Das sollte vor der Implementierung geklaert werden.

