# Bestandsaufnahme

## Relevante Komponenten

- `Rezepte.Web/Components/Settings/PluginSettings.razor`: Einstellungs-UI fuer Pluginstatus und Sortierung.
- `Rezepte.Web/Services/Import/Plugins/PluginSettingsService.cs`: Liefert und speichert die Pluginliste fuer die Einstellungen.
- `Rezepte.Web/Services/Import/Plugins/PluginManager.cs`: Verwaltet geladene Importplugins und Status.
- `Rezepte.Web/Services/Import/Plugins/BuiltInImportPluginCatalog.cs`: Enthaltene Fallback-/Builtin-Plugin-Deskriptoren.
- `Rezepte.Web/Services/Import/Plugins/PluginStartupService.cs`: Synchronisiert Pluginsettings beim Start.
- `Rezepte.Import.Plugins.*`: Ausgelagerte Importplugins fuer Backup und Webseitenquellen.
- `Rezepte.Tests/Services/Import/*`: Tests fuer Pluginmanager und Pluginsettings.

## Beobachtung

Der Praxistest zeigt, dass in der Einstellungsseite nur die AI-Plugins sichtbar sind. Die ausgelagerten Plugins existieren im Repository als Projekte, erscheinen aber nicht in der Settings-Liste.

## Zu pruefende Ursachen

- Die externen Pluginprojekte werden eventuell nicht in die Web-App kopiert oder referenziert.
- Der Pluginmanager entdeckt eventuell nur Assemblies, die bereits geladen oder in einem bestimmten Verzeichnis vorhanden sind.
- Der Settings-Service verwendet eventuell nur die aktuell erfolgreich geladenen Plugins und ergaenzt fehlende bekannte Plugin-Deskriptoren nicht.
- Tests decken den Fall "bekannte, aber nicht physisch geladene ausgelagerte Plugins in Settings sichtbar" eventuell noch nicht ab.

## Risiken

- Wenn sichtbare Plugins ohne geladene Assembly aktiviert werden, darf der Import nicht fehlschlagen; der vorhandene Statusmechanismus muss diesen Zustand sauber abbilden.
- Sortierung und Aktivierung muessen weiterhin benutzerspezifisch persistiert werden.
