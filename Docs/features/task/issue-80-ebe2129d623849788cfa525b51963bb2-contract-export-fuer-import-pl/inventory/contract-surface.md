# Vertragssurface und Abhaengigkeiten

## Oeffentliche Typen

`Rezepte.Import.Abstractions` stellt aktuell folgende Vertragsteile bereit:

- `IImportPlugin`: stabile Plugin-ID, Anzeigename, Beschreibung, Version,
  Handler-Typ und Standardprioritaet.
- `IImportHandler`: `UserId`, Erkennung einer Quelle und Verarbeitung eines
  Imports mit Stream, Dateiname, URI, Kochbuchziel und CancellationToken.
- `ICollectionImportHandler`: Vorschau und itemweiser Import von Sammlungen.
- `IInteractiveImportHandler` und `IImportInteraction`: interaktive
  Verarbeitung, Bestaetigung und Statusmeldungen.
- `ImportResult`: Erfolg, Fehler, erzeugte Rezept-IDs und optionale neutrale
  Rezeptdaten.
- Collection-Records und `ImportCollectionItemState`.
- Neutrale Importmodelle `ImportedRecipe`, `ImportedIngredient`,
  `ImportedRecipeStep` und `ImportedImage`.

Die Typen befinden sich in einzelnen C#-Dateien unter
`Rezepte.Import.Abstractions/`. Es gibt keine expliziten XML-Dokumentations-
Kommentare, keine sichtbare Paketversion im Projektfile und keine ApiCompat-
Baseline.

## Abhaengigkeitsbild

Das Abstractions-Projekt selbst ist abhaengigkeitsfrei und verwendet nur das
`Microsoft.NET.Sdk`. Die produktiven Plugins referenzieren es per
`ProjectReference`. Die Host-Anwendung referenziert es ebenfalls und laedt
Plugins zur Laufzeit ueber Assembly-Erkennung.

`Docs/help/import-plugins.md` beschreibt ein SDK-Projekt im separaten privaten
Plugin-Repository fuer Parser- und URL-Hilfen. Dieses Repository ist nicht Teil
des vorliegenden Checkouts; seine tatsaechlichen Dateien, Versionierung und
Exportfaehigkeit koennen hier nicht verifiziert werden.

## Versionierungsbeobachtung

Die Plugin-Klassen tragen aktuell jeweils `Version => "1.0.0"`; das ist die
Pluginversion und kein zentraler Contract-Versionseintrag. Eine
`contractVersion` nach SemVer sowie eine Zuordnung zu `sourceCommit` existiert
noch nicht.

Die API besteht aus Interfaces, Records und oeffentlichen Membern. Aenderungen
an Signaturen, Rueckgabetypen, Nullability oder Membern muessen deshalb als
oeffentlicher Vertrag behandelt und gegen eine freigegebene Assembly-Baseline
geprueft werden.

