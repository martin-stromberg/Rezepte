# Laufzeit und Plugin-Discovery

## Bestehender Ablauf

`PluginStartupService` implementiert `IHostedService` und ruft beim Start `IPluginManager.InitializeAsync` auf. `PluginManager.InitializeAsync` führt Discovery aus, hält erfolgreiche Deskriptoren in einem geschützten Speicher und synchronisiert die gefundenen Plugins in `RezepteDbContext.PluginSettings`.

Die Discovery umfasst eingebaute Plugins sowie DLLs aus `<ContentRoot>/plugins` und `<AppContext.BaseDirectory>/plugins`. Unterordner werden unterstützt; dort wird bevorzugt eine DLL mit dem Verzeichnisnamen gesucht. Plugin-Typen werden über `IImportPlugin` erkannt, instanziiert und auf einen `IImportHandler` geprüft.

Beim Erzeugen aktiver Handler werden nur aktivierte Einträge mit Status `Loaded` verwendet. Die Handler werden per `ActivatorUtilities.CreateInstance` aus dem aktuellen Service Provider erzeugt. Laufzeitfehler beim Erzeugen werden als `RuntimeFailed` in `PluginSetting` gespeichert.

## Bestehende Dateien

- `Rezepte.Web/Services/Import/Plugins/PluginManager.cs`
- `Rezepte.Web/Services/Import/Plugins/IPluginManager.cs`
- `Rezepte.Web/Services/Import/Plugins/PluginStartupService.cs`
- `Rezepte.Web/Services/Import/Plugins/ImportPluginDescriptor.cs`
- `Rezepte.Web/Services/Import/Plugins/PluginStatus.cs`
- `Rezepte.Web/Services/Import/ImportService.cs`
- `Rezepte.Web/Services/Import/ImportOrchestrator.cs`

## Relevante Statuswerte

Vorhanden sind `Loaded`, `Missing`, `Incompatible`, `LoadFailed` und `RuntimeFailed`. Ein Statusmodell für Quelle, Release, Download, ZIP-Validierung, Runtime-Validierung, Installation oder dauerhaft übersprungene Versionen fehlt.

## Konsequenzen für die Erweiterung

Die Anforderung braucht klar getrennte Komponenten für GitHub-Releaseermittlung, Download, sichere ZIP-Entpackung, Discovery aus einem temporären Verzeichnis und Installation. Der vorhandene Discovery-Code sollte dafür wiederverwendbare Prüfoperationen anbieten, ohne während der Vorprüfung den aktiven Bestand oder den persistenten Status zu verändern.

Die Synchronisierung muss gegen parallele manuelle und automatische Updates geschützt werden. Vor einem Reload müssen aktive Handler und die Lebensdauer der vorhandenen `AssemblyLoadContext`s berücksichtigt werden; ein einfacher Dateiaustausch kann wegen geladener Assemblies unzureichend sein.
