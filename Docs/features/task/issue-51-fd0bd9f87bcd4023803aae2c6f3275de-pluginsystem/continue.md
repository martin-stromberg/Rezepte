# Offene Aufgaben

Erstellt am: 2026-07-13
Abbruchgrund: Maximale Iterationsanzahl erreicht

Die folgenden Aufgaben konnten im automatisierten Zyklus nicht abgeschlossen werden
und muessen manuell oder in einem erneuten Lauf bearbeitet werden.

## Offene Planelemente

- [ ] Die geforderten Pluginprojekte pro vorhandener Importquelle fehlen weiterhin. Es muessen Projekte wie `Rezepte.Import.Plugins.Backup`, `Rezepte.Import.Plugins.Chefkoch`, `Rezepte.Import.Plugins.SecondSource`, `Rezepte.Import.Plugins.ThirdSource`, `Rezepte.Import.Plugins.FourthSource`, `Rezepte.Import.Plugins.FifthSource`, `Rezepte.Import.Plugins.SixthSource`, `Rezepte.Import.Plugins.AIFoto` und `Rezepte.Import.Plugins.AIUrl` angelegt und in die Solution aufgenommen werden.
- [ ] Die vorhandenen Quellenhandler muessen aus `Rezepte.Web` ausgelagert werden. `BuiltInImportPluginCatalog` registriert alle neun Web-Handler weiterhin direkt als Built-in-Plugins.
- [ ] Die Architektur "Plugins liefern neutrale Rezeptdaten, der Host persistiert" muss vollstaendig umgesetzt werden. Die neutralen DTOs existieren, werden von den bestehenden Importhandlern aber noch nicht als Rueckgabeweg verwendet.
- [ ] Hostseitiges Mapping von neutralen Import-DTOs nach `Recipe`, `RecipeStep`, `RecipeIngredient`, Bildern und Cookbook-Zuordnung fehlt. Die bestehenden Handler speichern weiter direkt ueber `IRecipeService`.
- [ ] Allgemeine Parser-/Basishilfen muessen aus `Rezepte.Web` in das Shared-Projekt verschoben werden, soweit sie keine Web-Entity- oder Host-Service-Abhaengigkeiten haben.
- [ ] Die Akzeptanzpunkte "Alle vorhandenen Importquellen liegen in separaten Pluginprojekten" und "Pluginprojekte referenzieren nur `Rezepte.Import.Abstractions` und noetige externe Pakete" sind noch nicht erfuellt.
- [ ] Die vorbereitete Build-/Publish-Kopierlogik fuer `Rezepte.Import.Plugins.*` muss mit echten produktiven Pluginprojekten genutzt und validiert werden.

## Code-Review-Befunde

- [ ] Direkte Plugin-DLLs unter `plugins` erzeugen falsche Fehler-/Inkompatibilitaetseintraege fuer nebenliegende Abhaengigkeiten. Fuer Root-Plugins muss eine klare Manifest-/Namenskonvention, ein bevorzugtes Unterordnerlayout oder ein Dependency-Filter mit Test eingefuehrt werden.
- [ ] Nicht erzeugbare Handler bleiben in der Admin-Konfiguration als geladen und aktiviert stehen. Instanziierungsfehler muessen als sichtbarer Pluginstatus oder Fehler in `PluginSetting` gespeichert und getestet werden.
- [ ] Externe Produktiv-Plugins mit gleicher ID wie Built-ins koennen wegen `First()`-Prioritaet nicht wirksam werden. Duplicate-ID-Verhalten zwischen Built-ins und externen Plugins muss festgelegt und getestet werden.
- [ ] Produktive Importquellen sind weiter Web-interne Built-ins statt separate Pluginprojekte. Die vorhandenen Quellen muessen in separate Klassenbibliotheken verschoben werden.
- [ ] Der Shared-Contract bildet die geplante hostneutrale Rezeptuebergabe noch nicht vollstaendig ab. `IImportHandler.HandleAsync` gibt weiterhin `ImportResult` mit erzeugten Rezept-IDs zurueck; neutrale DTOs und Host-Mapping muessen in den Importfluss integriert werden.

## Fehlgeschlagene Tests

Keine.
