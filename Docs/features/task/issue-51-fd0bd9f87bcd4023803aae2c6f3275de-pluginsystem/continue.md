# Offene Aufgaben

Erstellt am: 2026-07-14
Abbruchgrund: Restpunkte nach Continue-Lauf

Die folgenden Aufgaben konnten im automatisierten Zyklus nicht abgeschlossen werden
und muessen manuell oder in einem erneuten Lauf bearbeitet werden.

## Offene Planelemente

- [x] Die vorhandenen Backup-/URL-Quellenhandler muessen aus `Rezepte.Web` ausgelagert werden. `BuiltInImportPluginCatalog` registriert fuer diese Quellen keine Web-Handler mehr, und die alten Webhandlerdateien wurden entfernt.
- [x] Die Architektur "Plugins liefern neutrale Rezeptdaten, der Host persistiert" muss vollstaendig umgesetzt werden. Der neutrale Rueckgabeweg existiert und wird fuer Backup/URL-Basishandler genutzt; AI-Handler und produktive externe Parser stehen noch aus.
- [x] Allgemeine Parser-/Basishilfen muessen aus `Rezepte.Web` in das Shared-Projekt verschoben werden, soweit sie keine Web-Entity- oder Host-Service-Abhaengigkeiten haben.
- [ ] Die Akzeptanzpunkte "Alle vorhandenen Importquellen liegen in separaten Pluginprojekten" und "Pluginprojekte referenzieren nur `Rezepte.Import.Abstractions` und noetige externe Pakete" sind noch nicht vollstaendig erfuellt, weil `Rezepte.Import.Plugins.AIFoto` und `Rezepte.Import.Plugins.AIUrl` weiterhin Placeholder-Handler enthalten.
- [x] Die vorbereitete Build-/Publish-Kopierlogik fuer `Rezepte.Import.Plugins.*` muss mit echten produktiven Pluginprojekten genutzt und validiert werden.

## Code-Review-Befunde

- [x] Produktive Importquellen sind weiter Web-interne Built-ins statt separate Pluginprojekte. Die vorhandenen Quellen muessen in separate Klassenbibliotheken verschoben werden.
- [ ] AI-Foto und AI-URL sind weiterhin Web-interne Built-ins statt produktive externe Pluginprojekte. Fuer eine vollstaendige Auslagerung braucht es einen neutralen Contract fuer hostbereitgestellte AI-/Vision-/Settings-/Usage-Services oder einen bewussten Architekturentscheid, AI als Hostadapter zu belassen.
- [ ] Dedizierte Tests fuer produktive externe Pluginparser pro Quelle fehlen.

## Fehlgeschlagene Tests

Keine.
