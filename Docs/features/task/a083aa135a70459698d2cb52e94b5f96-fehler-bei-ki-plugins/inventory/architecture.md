# Detail: Architektur und Importfluss

## Projektstruktur

Die Loesung ist eine .NET-10-Anwendung mit Webprojekt, Testprojekt, Import-Abstraktionen und mehreren Import-Plugin-Projekten:

- `Rezepte.Web`
- `Rezepte.Tests`
- `Rezepte.Import.Abstractions`
- `Rezepte.Import.Plugins.Backup`
- `Rezepte.Import.Plugins.AIFoto`
- `Rezepte.Import.Plugins.AIUrl`

`Rezepte.Web/Rezepte.Web.csproj` baut die Plugin-Projekte mit und kopiert deren Ausgabe unter `plugins/<PluginName>/` in Build- und Publish-Ausgaben. Externe Plugin-Artefakte koennen zusaetzlich aus `external/rezepte-import-plugins-private/artifacts/plugins` uebernommen werden.

## Runtime-Registrierung

`Rezepte.Web/Extensions/ServiceCollectionExtensions.cs` registriert die relevanten Dienste:

- `IGoogleCredentialsProvider` als Singleton
- `IGeminiClient` als Scoped-Service
- `IPluginManager` als Singleton
- `ImportOrchestrator` als Singleton
- `IImportService`, `IAiUsageService`, `ISettingsService` und Persister als Scoped-Services

Die Konfigurationssektionen `AI`, `PluginUpdates` und `GoogleCredentials` werden ueber Options gebunden.

## Plugin-Discovery und Aktivierung

`PluginManager` entdeckt Plugins aus:

- Built-in-Katalog `BuiltInImportPluginCatalog.GetPlugins()`
- `ContentRootPath/plugins`
- `AppContext.BaseDirectory/plugins`

Der Built-in-Katalog ist aktuell leer. Die drei Plugin-Projekte werden daher faktisch ueber die Build-/Publish-Kopie in den Plugin-Ordner geladen.

Aktive Handler entstehen aus Datenbankeintraegen in `PluginSettings`, gefiltert nach:

- `Enabled == true`
- `Status == Loaded`
- Reihenfolge `OrderIndex`, dann `DisplayName`

Kann ein Handler zur Laufzeit nicht erstellt werden, setzt `PluginManager` den Pluginstatus auf `RuntimeFailed` und speichert die Fehlermeldung.

## Importablauf

`ImportService.ImportAsync()` und `ImportOrchestrator.StartImportAsync()` folgen demselben Grundmuster:

1. Aktive Plugin-Handler vom `PluginManager` abrufen.
2. Pro Handler den Eingabestream zuruecksetzen.
3. `CanHandleAsync()` ausfuehren.
4. Bei `false` ohne Diagnose zum naechsten Handler wechseln.
5. Bei `true` `HandleAsync()` bzw. `HandleInteractiveAsync()` ausfuehren.
6. Ergebnis persistieren und Sessionstatus setzen.

Der Orchestrator loggt Fehler in `CanHandleAsync()` als Warning und Fehler waehrend des Imports ebenfalls als Warning. `ImportService` loggt Importfehler als Error.

## Relevanz fuer die Anforderung

Die KI-gestuetzte Rezepterfassung scheitert wahrscheinlich vor oder waehrend `CanHandleAsync()`. Wenn ein Handler wegen fehlender Aktivierung `false` liefert, entsteht kein konkreter Credential-Fehler, sondern nur ein generischer "No suitable import plugin found"-Fehler.
