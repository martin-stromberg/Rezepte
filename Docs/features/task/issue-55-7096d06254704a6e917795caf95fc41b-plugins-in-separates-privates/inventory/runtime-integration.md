# Plugin-Discovery und Laufzeitintegration

## Discovery

`PluginManager` entdeckt Plugins aus drei Quellen:

1. Built-in-Plugins aus `BuiltInImportPluginCatalog`.
2. DLLs direkt unter `{ContentRoot}/plugins`.
3. DLLs direkt unter `{AppContext.BaseDirectory}/plugins`.

Bei Unterordnern wird bevorzugt eine DLL gesucht, die wie der Unterordner heisst. Existiert sie nicht, werden alle DLLs im Unterordner betrachtet.

## Laden externer Assemblies

Externe Plugins werden ueber einen eigenen `AssemblyLoadContext` mit `AssemblyDependencyResolver` geladen. Fuer `Rezepte.Import.Abstractions` gibt der LoadContext `null` zurueck, sodass die Abstraktionsassembly des Hosts verwendet wird. Das ist wichtig, damit `IImportPlugin` und `IImportHandler` typidentisch bleiben.

## Validierung

Beim Laden prueft `PluginManager`:

- Assembly enthaelt mindestens einen konkreten `IImportPlugin`-Typ.
- Plugin kann parameterlos instanziiert werden.
- Plugin hat eine stabile `Id`.
- `HandlerType` implementiert `IImportHandler`.

Fehlerhafte Assemblies werden als `LoadFailed` oder `Incompatible` in den Plugin-Einstellungen sichtbar.

## Aktivierung und Reihenfolge

Gefundene Plugins werden in `PluginSetting` synchronisiert. Dort werden Status, Fehler, Aktivierung und Reihenfolge gehalten. `GetActiveHandlersAsync` instanziiert nur aktivierte Plugins mit Status `Loaded`, sortiert nach `OrderIndex` und `DisplayName`.

## Importablauf

`ImportService.ImportAsync`:

1. Holt aktive Plugin-Handler.
2. Setzt den Stream vor jedem Versuch auf Position 0.
3. Ruft `CanHandleAsync` auf.
4. Beim ersten passenden Handler ruft es `HandleAsync` auf.
5. Persistiert `ImportedRecipes` ueber `IImportedRecipePersister`.
6. Gibt bei keinem Treffer `No suitable import plugin found for this file or URL.` zurueck.

Diese Logik kann fuer die Web-App unveraendert bleiben, wenn ausgelagerte Plugins als DLLs bereitgestellt werden.

## Built-ins

`BuiltInImportPluginCatalog` enthaelt aktuell nur `ai-foto` und `ai-url`, beide aus `Rezepte.Web`. Die produktiven Quell-Plugins sind nicht als Built-ins registriert, sondern werden im Test bereits als externe Plugin-DLLs behandelt.
