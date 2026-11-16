# Release Notes

## Important Notes Before Update

- Recipe imports that require user interaction (collection selection or confirmation) are now rejected with an error pointing to the interactive `import-session/*` endpoints when called via the synchronous import endpoints (`POST api/cookbooks/{id}/import`, `POST api/cookbooks/{id}/import-url` and the variants without a cookbook); API consumers must use the session endpoints for such imports.
- Plugins installed from plugin sources are now stored in versioned directories `plugins/<Name>@<ReleaseTag>/`; previously installed, non-versioned plugin folders become ineffective once a versioned directory exists and can be removed manually.

## What's New

- A search term typed into the search field on the recipe search page is no longer reset while the page is still loading or the result list refreshes in the background (e.g. when paging); a freshly inserted search field is filled with the current query.
- All recipe import entry points now run through a single execution path; URL imports store the source URL on the recipe so a repeated import of the same URL updates the existing recipe instead of creating a duplicate.
- Source-installed plugins are installed into versioned directories `plugins/<Name>@<ReleaseTag>/` instead of replacing the previous plugin folder in place.
- New install state file `plugins/.install-state.json` tracks the effective plugin version per plugin; directories that cannot be deleted (e.g. locked assemblies) are marked and retried at startup and on the next update.
- Marked and superseded plugin directories are excluded from plugin discovery at startup.
- Plugins removed from a source's new release are marked for deletion and shown as `Missing` in plugin management.
- Plugins shipped with the application (`plugins/<ProjectName>/`) and loose DLLs placed directly in `plugins` remain unaffected.
- Each configured plugin source now has a “Check for updates” action in plugin management (administrators only) that runs the update check for that source immediately — same preconditions as the startup check — and reports the result right away.
- Plugins marked as `Missing` now have a remove action in plugin management (administrators only) that permanently deletes the management entry; if the plugin is discovered again later it is added like a new plugin.

## Wichtige Hinweise vor dem Update

- Rezeptimporte, die eine Benutzerinteraktion erfordern (Auswahl aus einer Sammlung oder Bestätigung), werden über die synchronen Import-Endpunkte (`POST api/cookbooks/{id}/import`, `POST api/cookbooks/{id}/import-url` sowie die Varianten ohne Kochbuch) künftig mit einer Fehlermeldung abgelehnt, die auf die interaktiven `import-session/*`-Endpunkte verweist; API-Konsumenten müssen solche Importe über die Session-Endpunkte ausführen.
- Aus Pluginquellen installierte Plugins liegen nun in versionsbezogenen Verzeichnissen `plugins/<Name>@<ReleaseTag>/`; bisherige, nicht versionierte Plugin-Ordner werden unwirksam, sobald ein Versionsverzeichnis existiert, und können manuell entfernt werden.

## Neuerungen

- Ein in das Suchfeld der Rezeptsuche eingetippter Begriff geht nicht mehr verloren, während die Seite noch lädt oder die Trefferliste im Hintergrund aktualisiert wird (z. B. beim Blättern); ein neu eingesetztes Suchfeld wird mit der aktuellen Suche befüllt.
- Alle Einstiege des Rezeptimports laufen nun über denselben Ausführungspfad; URL-Importe speichern die Quell-URL auf dem Rezept, sodass ein erneuter Import derselben URL das bestehende Rezept aktualisiert statt ein Duplikat anzulegen.
- Quelleninstallierte Plugins werden in versionsbezogene Verzeichnisse `plugins/<Name>@<ReleaseTag>/` installiert, statt den bisherigen Plugin-Ordner an Ort und Stelle zu ersetzen.
- Neue Zustandsdatei `plugins/.install-state.json` führt die wirksame Plugin-Version je Plugin; nicht löschbare Verzeichnisse (z. B. gesperrte Assemblies) werden markiert und beim Start sowie beim nächsten Update erneut versucht.
- Markierte und überholte Plugin-Verzeichnisse werden bei der Plugin-Erkennung beim Start übersprungen.
- Plugins, die im neuen Release einer Quelle nicht mehr enthalten sind, werden zum Löschen markiert und in der Pluginverwaltung als `Missing` angezeigt.
- Mit der Anwendung veröffentlichte Plugins (`plugins/<Projektname>/`) und direkt in `plugins` abgelegte DLL-Dateien bleiben unverändert.
- Jede konfigurierte Pluginquelle erhält in der Pluginverwaltung eine Aktion „Auf Updates prüfen“ (nur für Administratoren), die die Update-Prüfung für genau diese Quelle sofort ausführt — unter denselben Vorbedingungen wie die Start-Prüfung — und das Ergebnis unmittelbar meldet.
- Als `Missing` markierte Plugins erhalten in der Pluginverwaltung eine Entfernen-Aktion (nur für Administratoren), die den Verwaltungseintrag dauerhaft löscht; wird das Plugin später wieder entdeckt, wird es wie ein neues Plugin aufgenommen.
