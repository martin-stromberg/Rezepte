# Release Notes

## Important Notes Before Update

- Plugins installed from plugin sources are now stored in versioned directories `plugins/<Name>@<ReleaseTag>/`; previously installed, non-versioned plugin folders become ineffective once a versioned directory exists and can be removed manually.

## What's New

- Source-installed plugins are installed into versioned directories `plugins/<Name>@<ReleaseTag>/` instead of replacing the previous plugin folder in place.
- New install state file `plugins/.install-state.json` tracks the effective plugin version per plugin; directories that cannot be deleted (e.g. locked assemblies) are marked and retried at startup and on the next update.
- Marked and superseded plugin directories are excluded from plugin discovery at startup.
- Plugins removed from a source's new release are marked for deletion and shown as `Missing` in plugin management.
- Plugins shipped with the application (`plugins/<ProjectName>/`) and loose DLLs placed directly in `plugins` remain unaffected.

## Wichtige Hinweise vor dem Update

- Aus Pluginquellen installierte Plugins liegen nun in versionsbezogenen Verzeichnissen `plugins/<Name>@<ReleaseTag>/`; bisherige, nicht versionierte Plugin-Ordner werden unwirksam, sobald ein Versionsverzeichnis existiert, und können manuell entfernt werden.

## Neuerungen

- Quelleninstallierte Plugins werden in versionsbezogene Verzeichnisse `plugins/<Name>@<ReleaseTag>/` installiert, statt den bisherigen Plugin-Ordner an Ort und Stelle zu ersetzen.
- Neue Zustandsdatei `plugins/.install-state.json` führt die wirksame Plugin-Version je Plugin; nicht löschbare Verzeichnisse (z. B. gesperrte Assemblies) werden markiert und beim Start sowie beim nächsten Update erneut versucht.
- Markierte und überholte Plugin-Verzeichnisse werden bei der Plugin-Erkennung beim Start übersprungen.
- Plugins, die im neuen Release einer Quelle nicht mehr enthalten sind, werden zum Löschen markiert und in der Pluginverwaltung als `Missing` angezeigt.
- Mit der Anwendung veröffentlichte Plugins (`plugins/<Projektname>/`) und direkt in `plugins` abgelegte DLL-Dateien bleiben unverändert.
