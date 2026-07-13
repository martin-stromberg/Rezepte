# Detail: Plugin-Schnittstellen und Projektstruktur

## Aktueller Projektzuschnitt

Die Solution enthaelt nur:

- `Rezepte.Web`
- `Rezepte.Tests`

Es gibt kein Shared-Projekt und keine separaten Klassenbibliotheksprojekte fuer Importquellen. Das Webprojekt referenziert alle Importhandler direkt und enthaelt auch deren Basisklassen.

Belege:

- `Rezepte.sln`
- `Rezepte.Web/Rezepte.Web.csproj`
- `Rezepte.Tests/Rezepte.Tests.csproj`

## Aktuelle Importvertraege

`IImportHandler` und `ImportResult` liegen in `Rezepte.Web/Services/Import/IImportHandler.cs`.

Der Vertrag enthaelt:

- `ImportResult`
- `IImportHandler.UserId`
- `CanHandleAsync(Stream, string, CancellationToken)`
- `HandleAsync(Stream, string, string? uri, string targetCookbookId, string userId, CancellationToken)`

Belege:

- `Rezepte.Web/Services/Import/IImportHandler.cs:3`
- `Rezepte.Web/Services/Import/IImportHandler.cs:5`
- `Rezepte.Web/Services/Import/IImportHandler.cs:7`
- `Rezepte.Web/Services/Import/IImportHandler.cs:11`
- `Rezepte.Web/Services/Import/IImportHandler.cs:16`

`IImportService` liegt ebenfalls im Webprojekt und ist damit kein geeigneter Pluginvertrag.

## Basisklassen und Abhaengigkeiten

`BaseImportHandler` enthaelt allgemeine Parsing-Hilfen fuer Zutaten und ISO-Dauerangaben. URL-Handler erben von `BaseUrlReceiptImportHandler`; AI-Handler erben von `BaseAIImportHandler`.

Belege:

- `Rezepte.Web/Services/Import/BaseImportHandler.cs`
- `Rezepte.Web/Services/Import/Url/BaseUrlReceiptImportHandler.cs:8`
- `Rezepte.Web/Services/Import/BaseAIImportHandler.cs:14`

Die URL-Basisklasse ist stark an Webprojekt-Services gekoppelt, insbesondere `IRecipeService`. Sie erstellt oder aktualisiert Rezepte direkt.

Belege:

- Rezept-Erstellung: `Rezepte.Web/Services/Import/Url/BaseUrlReceiptImportHandler.cs:385`
- Vorhandenes Rezept ueber URI: `Rezepte.Web/Services/Import/Url/BaseUrlReceiptImportHandler.cs:528`
- Update: `Rezepte.Web/Services/Import/Url/BaseUrlReceiptImportHandler.cs:531`

Das bedeutet: Ein reines Shared-Projekt darf nicht einfach alle heutigen Basisklassen unveraendert aufnehmen, wenn dadurch Webprojekt-Abhaengigkeiten in Plugins erzwungen oder zyklische Referenzen erzeugt werden.

## Vorhandene Quellenhandler

Aktuelle Quellenhandler:

| Handler | Datei | Bemerkung |
|---------|-------|-----------|
| `BackupImportHandler` | `Rezepte.Web/Services/Import/BackupImportHandler.cs` | Importiert Backup-ZIP mit `recipes.json`. |
| `ChefkochReceiptImportHandler` | `Rezepte.Web/Services/Import/Url/ChefkochReceiptImportHandler.cs` | Quelle Chefkoch. |
| `SecondSourceUrlReceiptImportHandler` | `Rezepte.Web/Services/Import/Url/SecondSourceUrlReceiptImportHandler.cs` | Kommentar nennt lecker.de. |
| `ThirdSourceUrlReceiptImportHandler` | `Rezepte.Web/Services/Import/Url/ThirdSourceUrlReceiptImportHandler.cs` | Kommentar nennt Lidl und Aldi Sued. |
| `FourthSourceUrlReceiptImportHandler` | `Rezepte.Web/Services/Import/Url/FourthSourceUrlReceiptImportHandler.cs` | Kommentar nennt Kabel Eins. |
| `FifthSourceUrlRecipeImportHandler` | `Rezepte.Web/Services/Import/Url/FifthSourceUrlRecipeImportHandler.cs` | Kommentar nennt kochkarussell.com. |
| `SixthSourceUrlRecipeImportHandler` | `Rezepte.Web/Services/Import/Url/FourthSourceUrlReceiptImportHandler.cs` | Kommentar nennt daskochrezept.de; Klasse liegt in derselben Datei wie Fourth. |
| `AIFotoImportHandler` | `Rezepte.Web/Services/Import/AIFotoImportHandler.cs` | Interaktiver AI-Bildimport. |
| `AIUrlImportHandler` | `Rezepte.Web/Services/Import/AIUrlImportHandler.cs` | Interaktiver AI-URL-Import. |

Belege:

- Registrierung aller Handler: `Rezepte.Web/Extensions/ServiceCollectionExtensions.cs:140` bis `Rezepte.Web/Extensions/ServiceCollectionExtensions.cs:148`
- URL-Basisklasse und Handlerklassen: `Rezepte.Web/Services/Import/Url/BaseUrlReceiptImportHandler.cs:451`, `Rezepte.Web/Services/Import/Url/ChefkochReceiptImportHandler.cs:15`, `Rezepte.Web/Services/Import/Url/FourthSourceUrlReceiptImportHandler.cs:12`, `Rezepte.Web/Services/Import/Url/FourthSourceUrlReceiptImportHandler.cs:119`
- AI-Aktivierungslogik: `Rezepte.Web/Services/Import/AIFotoImportHandler.cs:48`, `Rezepte.Web/Services/Import/AIUrlImportHandler.cs:38`

## Konsequenz fuer Shared-Projekt und Pluginprojekte

Das Shared-Projekt sollte mindestens enthalten:

- `IImportHandler`
- `IInteractiveImportHandler`
- `IImportInteraction`
- `ImportResult`
- stabile Importkontext-/Metadaten-Typen, falls benoetigt
- allgemeine, nicht webprojektspezifische Basishilfen

Nicht unkritisch fuer das Shared-Projekt:

- `IRecipeService`-Abhaengigkeit
- EF-Core-Entities
- Webprojekt-spezifische Services wie `ISettingsService`, `IAiUsageService`, `IGeminiClient`

Fuer Pluginprojekte gibt es zwei sinnvolle Richtungen:

1. Plugins verarbeiten Quellen und liefern neutrale Rezeptdaten zurueck; das Hauptprojekt speichert Rezepte.
2. Plugins duerfen ueber definierte Host-Services Rezepte selbst speichern.

Variante 1 entkoppelt Plugins staerker und ist fuer ein langfristig stabiles Plugin-API sauberer. Variante 2 ist naeher am bestehenden Code, koppelt Plugins aber an Host-Services und erfordert eine bewusst versionierte Host-Service-Schnittstelle.

