# Plan-Review: Pluginsystem fuer Rezeptimporte

Status: Offene Aufgaben vorhanden

## Umgesetzte Planelemente seit Fortsetzung

- Backup ist kein Placeholder mehr: `Rezepte.Import.Plugins.Backup` erkennt Backup-ZIP-Dateien und liefert neutrale `ImportedRecipe`-Daten.
- Die produktiven URL-Parser fuer Chefkoch, SecondSource, ThirdSource, FourthSource, FifthSource und SixthSource wurden in die jeweiligen `Rezepte.Import.Plugins.*`-Projekte portiert.
- Gemeinsame Parser-/URL-Basishilfen liegen nun in `Rezepte.Import.Abstractions` (`ImportParserBase`, `UrlRecipeImportHandlerBase`, `ParsedIngredient`, `StringParsingExtensions`).
- Die portierten Pluginprojekte referenzieren weiterhin nur `Rezepte.Import.Abstractions` und benoetigte BCL-/Frameworkpakete.
- Der Web-Built-in-Katalog registriert fuer Backup und URL-Quellen keine Web-internen Handler mehr; dort bleiben nur die AI-Hostadapter.
- Die alten Web-internen Backup-/URL-Handlerdateien wurden entfernt.
- AI-Foto und AI-URL persistieren nicht mehr direkt ueber `IRecipeService`, sondern liefern neutrale `ImportedRecipe`-Daten an den zentralen Host-Persistenzpfad.
- Build-/Output-Kopierlogik wurde durch `dotnet build Rezepte.sln --no-restore` mit echten Pluginprojekten validiert.

## Offene Aufgaben

- `Rezepte.Import.Plugins.AIFoto` und `Rezepte.Import.Plugins.AIUrl` enthalten weiterhin Placeholder-Handler. Die produktive AI-Logik liegt weiterhin als Hostadapter in `Rezepte.Web`, weil sie Hostservices wie AI-Konfiguration, Usage-Limits, Gemini/Vision-Clients und interaktive Sessionbestaetigung benoetigt.
- Die Akzeptanzpunkte "Alle vorhandenen Importquellen liegen produktiv in separaten Pluginprojekten" und "Pluginprojekte enthalten die ausgelagerte Importlogik" sind fuer AI-Foto und AI-URL noch nicht vollstaendig erfuellt.
- Es fehlen weiterhin dedizierte Parser-/Importtests fuer die produktiven externen Pluginparser pro Quelle sowie fuer AI-Imports ueber den neutralen DTO-Rueckgabeweg.

## Bewertung

Die offenen Continue-Punkte fuer Backup und alle sechs klassischen URL-Quellen sind umgesetzt. Der verbleibende Rest ist auf die AI-Quellen begrenzt und benoetigt eine zusaetzliche Contract-Erweiterung fuer hostbereitgestellte AI-Services oder einen bewussten Architekturentscheid, dass AI-Imports Hostadapter bleiben.
