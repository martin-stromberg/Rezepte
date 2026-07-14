# Import-Abstraktionen und Plugin-Vertrag

## Bestehende Vertrage

`IImportHandler` in `Rezepte.Import.Abstractions/IImportHandler.cs` definiert:

- `UserId`
- `CanHandleAsync(...)`
- `HandleAsync(...)`

`IInteractiveImportHandler` in `Rezepte.Import.Abstractions/IInteractiveImportHandler.cs` erweitert dies um:

- `HandleInteractiveAsync(..., IImportInteraction interaction, ...)`

`IImportInteraction` in `Rezepte.Import.Abstractions/IImportInteraction.cs` kann aktuell nur:

- `AskForConfirmationAsync(string prompt, ...)`
- `ReportStatusAsync(string status, ...)`

`ImportResult` in `Rezepte.Import.Abstractions/ImportResult.cs` enthaelt:

- globales `Success`
- globales `Error`
- `CreatedRecipeIds`
- optionale `ImportedRecipes`

## Bestehende URL-Parser-Basis

`UrlRecipeImportHandlerBase` in `Rezepte.Import.Abstractions/UrlRecipeImportHandlerBase.cs:7` ist fuer HTML-basierte Rezeptquellen zentral. Der Ablauf ist:

- `CanHandleAsync(...)` liest den Stream und ruft zuerst `ReadSingleRecipe(...)`.
- Wenn kein Einzelrezept gefunden wird, ruft es `ReadRecipeCollection(...)` auf.
- `ReadRecipeCollection(...)` verwendet `ExtractRecipeUriCollection(...)` und laedt anschliessend jeden Link sofort herunter.
- `HandleAsync(...)` gibt das zuvor gecachte Parse-Ergebnis als `ImportResult` zurueck.

Relevante Stellen:

- `ExtractRecipeUriCollection(...)`: `Rezepte.Import.Abstractions/UrlRecipeImportHandlerBase.cs:282`
- `ReadRecipeCollection(...)`: `Rezepte.Import.Abstractions/UrlRecipeImportHandlerBase.cs:284`
- `CanHandleAsync(...)`: `Rezepte.Import.Abstractions/UrlRecipeImportHandlerBase.cs:313`
- `HandleAsync(...)`: `Rezepte.Import.Abstractions/UrlRecipeImportHandlerBase.cs:331`

## Konflikt mit der Anforderung

Die vorhandene Collection-Hook-Struktur ist fuer die neue Anforderung ungeeignet, weil `ReadRecipeCollection(...)` alle Rezeptseiten sofort nachlaedt. Gefordert ist dagegen:

- Sammlungsseite erkennen.
- Nur auf der Sammlungsseite vorhandene Daten anzeigen.
- Einzelrezeptseiten erst nach Auswahl abrufen.

## Erforderliche Erweiterungen

Naheliegende neue Modelle in `Rezepte.Import.Abstractions`:

- `ImportCollectionPreview` oder `RecipeCollectionPreview`
- `ImportCollectionItem` mit stabiler ID, Titel, URL und optionalen Vorschaufeldern
- `ImportCollectionSelection` mit ausgewaehlten Items und Zielkochbuch/Kategorie je Item
- `ImportCollectionItemStatus` fuer Fortschritt und Fehler

Naheliegende neue Vertragsoption:

- Optionales Interface wie `ICollectionImportHandler`, das Vorschau erkennt und spaeter einzelne Items importiert.
- Alternativ Erweiterung von `IImportInteraction` um eine strukturierte Auswahlabfrage. Das ist riskanter, weil alle interaktiven Handler denselben Vertrag sehen.

## Wichtige Rueckwaertskompatibilitaet

`ImportResult` und `IImportHandler` sollten fuer bestehende Plugins unveraendert nutzbar bleiben. Neue Sammlungserkennung sollte optional sein, damit Nicht-Chefkoch-Plugins weiter genau ein Rezept liefern koennen.

