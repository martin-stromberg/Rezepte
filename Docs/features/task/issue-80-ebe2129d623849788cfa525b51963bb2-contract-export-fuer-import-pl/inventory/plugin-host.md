# Plugin-Host, Import und bestehende Paketsicherheit

## Erkennung und Laden

`Rezepte.Web/Services/Import/Plugins/PluginManager.cs` kombiniert den
eingebauten Katalog mit DLLs aus `plugins` im Content- bzw. App-Basisverzeichnis.
DLLs werden direkt oder aus direkten Unterverzeichnissen gefunden und ueber
`IImportPlugin` reflektiert. Aktivierte Plugins mit Status `Loaded` werden in
der gespeicherten Reihenfolge fuer Imports verwendet.

`PluginStartupService` initialisiert diese Erkennung beim Anwendungsstart.
Der Host benoetigt fuer das Contract-Exportartefakt weiterhin eine getrennte
Build-/Releasegrenze; Laufzeit-DLL-Erkennung validiert keine Quellmanifest-
Hashes.

## Bestehender GitHub-Updatepfad

`PluginUpdateService` fragt fuer konfigurierte Pluginquellen das neueste
GitHub-Release ab, waehlt ein ZIP-Asset, laedt es in ein temporaeres Verzeichnis,
validiert es und installiert erkannte Pluginverzeichnisse. Der Pfad ist fuer
Host-Pluginpakete gedacht und identifiziert Releases anhand Quelle, Release-Tag
und Asset-ID. Eine Pruefung gegen einen vom Plugin-Repository vorgegebenen
SHA-256-Hash ist dort nicht erkennbar.

Die Dokumentation unter `Docs/help/import-plugins.md` sagt, dass aktivierte
Quellen einmalig beim Start geprueft werden und kein regelmaessiger Lauf oder
manueller Update-Button existiert. Das ist vom geforderten manuellen Contract-
Import im Plugin-Repository zu unterscheiden.

## Paketvalidierung

`PluginPackageValidator` verhindert absolute Pfade, `..`-Segmente und das
Verlassen des temporaeren Extraktionsverzeichnisses. Top-Level-Dateien sind
stark eingeschraenkt; unter Verzeichnissen werden Plugin-DLLs erkannt und
reflektiert. Die Validierung sucht jedoch Pluginassemblies und prueft kein
`contract-export.json`, keine `files`-Vollstaendigkeit und keine Datei-Hashes.

Diese bestehende ZIP-Sicherheit kann als lokales Muster fuer Pfadvalidierung
dienen, ist aber keine Implementierung des Contract-Export- oder Importvertrags.

