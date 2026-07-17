# Architektur und Importvertrag

## Importvertrag

Der Plugin-Vertrag liegt im Projekt `Rezepte.Import.Abstractions`. Das Projekt ist eine `net10.0`-Klassenbibliothek und wird von allen produktiven Plugin-Projekten referenziert.

Zentrale Typen:

- `IImportPlugin`: beschreibt Plugin-Metadaten (`Id`, `DisplayName`, `Description`, `Version`, `HandlerType`, `DefaultPriority`).
- `IImportHandler`: definiert `CanHandleAsync` und `HandleAsync` fuer Datei-/Stream-basierte Importe.
- `ImportResult`: transportiert Erfolg, Fehlertext, erzeugte Rezept-IDs und optional noch nicht persistierte `ImportedRecipe`-Modelle.
- `ImportedRecipe`, `ImportedIngredient`, `ImportedRecipeStep`, `ImportedImage`: pluginseitige Rezeptdatenstruktur.
- `ICollectionImportHandler` und Collection-Modelle: optionale Erweiterung fuer Sammlungsimporte.

## Parser-Basis

`UrlRecipeImportHandlerBase` buendelt den groessten Teil der URL-/HTML-Parser-Infrastruktur:

- HTML-Tag- und JSON-LD-Hilfsfunktionen.
- Download von HTML und Bildern per `HttpClient`.
- Cache zwischen `CanHandleAsync` und `HandleAsync`.
- Mapping interner `RecipeImport`-Objekte auf `ImportedRecipe`.
- Rueckgabe `false` bei Parserfehlern in `CanHandleAsync`.

Die produktiven URL-Plugins erben fast alle von dieser Basisklasse. Damit gehoert diese Basisklasse faktisch zum Plugin-SDK und muss bei einer Auslagerung entweder im Abstraktionspaket bleiben oder in ein gemeinsames Plugin-SDK-Projekt wandern.

## Kopplung zur Web-App

Die Plugin-Abstraktionen sind nicht von `Rezepte.Web` abhaengig. Die Web-App haengt umgekehrt vom Abstraktionsprojekt ab und persistiert die importierten Rezeptdaten nach erfolgreicher Plugin-Ausfuehrung.

Wichtig fuer die Auslagerung:

- Plugins duerfen keine Web-Entities oder Services referenzieren.
- Der aktuelle produktive Plugin-Code erfuellt das weitgehend.
- Die Persistierung liegt in `Rezepte.Web` und muss nicht in das Plugin-Repository.

## Technische Einschraenkungen

- Ziel-Framework ist aktuell `net10.0`; ein neues Repository muss dieselbe SDK-/Framework-Basis verwenden oder bewusst absenken.
- `UrlRecipeImportHandlerBase` nutzt `HttpClient` direkt und nicht DI. Ein CLI-Testprogramm kann die Handler daher ohne Web-Host instanziieren.
- Einige Parser verwenden synchrone `.Result`-Aufrufe beim Bilddownload; fuer manuelle Tests ist das vermutlich tolerierbar, fuer robuste CLI-Ausgabe aber zu beachten.
