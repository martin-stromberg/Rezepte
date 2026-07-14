# Code-Review: Pluginsystem fuer Rezeptimporte

Status: Befunde vorhanden

## Behobene Befunde

- Root-Pluginlayout mit nebenliegenden Contract-Abhaengigkeiten erzeugt keinen falschen Admin-Eintrag fuer `Rezepte.Import.Abstractions.dll` mehr.
- Runtime-Fehler bei Handler-Erzeugung werden jetzt als `RuntimeFailed` in `PluginSetting` gespeichert.
- Duplicate-ID-Verhalten ist festgelegt: echte externe Plugins koennen Built-ins ersetzen; Placeholder-Pluginprojekte werden nicht gegen Built-ins priorisiert.
- Die hostneutrale Rezeptuebergabe ist im Contract und Host-Persistenzpfad vorhanden; Backup und URL-Basishandler nutzen den neuen Rueckgabeweg.

## Befunde

### 1. Produktive Pluginprojekte enthalten noch Placeholder-Handler

Schweregrad: Hoch

Die neun neuen `Rezepte.Import.Plugins.*`-Projekte bauen und referenzieren nur `Rezepte.Import.Abstractions`, enthalten aber noch keine produktiven Parser. Ihre Handler melden `CanHandleAsync = false` und sind per `0.0.0-placeholder` markiert, damit der Host weiterhin die Built-ins verwendet.

Auswirkung: Die Projektstruktur ist vorhanden, aber die fachliche Auslagerung der Importlogik ist noch nicht abgeschlossen.

### 2. AI-Handler sind noch nicht hostneutral ausgelagert

Schweregrad: Mittel

`BaseAIImportHandler` persistiert weiterhin direkt ueber `IRecipeService`. Damit sind AI-Foto und AI-URL noch Web-interne Built-ins und nicht produktive externe Plugins.

### 3. Gemeinsame Parserhilfen liegen weiterhin im Webprojekt

Schweregrad: Mittel

`BaseImportHandler` und URL-Basishilfen wurden nicht in `Rezepte.Import.Abstractions` oder ein neutrales Shared-Paket verschoben. Produktive Pluginparser koennen diese Logik daher noch nicht ohne Web-Abhaengigkeit wiederverwenden.

## Fehlende Tests

- Tests fuer produktive externe Pluginparser pro Quelle fehlen, weil die Pluginprojekte noch Placeholder-Handler enthalten.
- Tests fuer AI-Imports ueber neutrale DTOs fehlen.

## Ausgefuehrte Pruefungen

- `dotnet build Rezepte.sln --no-restore`: erfolgreich.
- `dotnet test Rezepte.sln --no-restore --logger "console;verbosity=minimal"`: erfolgreich. 137 Tests bestanden, 0 fehlgeschlagen, 0 uebersprungen.
