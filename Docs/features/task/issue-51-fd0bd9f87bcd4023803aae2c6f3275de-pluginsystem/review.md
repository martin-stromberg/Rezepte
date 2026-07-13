# Plan-Review: Pluginsystem fuer Rezeptimporte

Status: Offene Aufgaben vorhanden

## Umgesetzte Planelemente seit Fortsetzung

- Die Solution enthaelt nun eigene Projekte fuer alle neun geforderten Importquellen: Backup, Chefkoch, SecondSource, ThirdSource, FourthSource, FifthSource, SixthSource, AIFoto und AIUrl.
- Die neuen Pluginprojekte referenzieren ausschliesslich `Rezepte.Import.Abstractions`.
- `ImportResult` kann neutrale `ImportedRecipe`-Daten transportieren.
- Der Host persistiert neutrale Importdaten zentral ueber `ImportedRecipePersister`.
- Datei-/URL-Imports fuer Backup und URL-Basishandler liefern neutrale DTOs an den Host-Persistenzpfad.
- Die PluginManager-Regeln fuer Root-Abhaengigkeiten, Runtime-Handlerfehler und Duplicate-IDs wurden konkretisiert und getestet.
- `dotnet build` und `dotnet test` laufen erfolgreich.

## Offene Aufgaben

- Die neuen Produktiv-Pluginprojekte enthalten aktuell nur Placeholder-Handler. Die vorhandenen produktiven Parser sind noch nicht vollstaendig in diese Projekte verschoben.
- Der Built-in-Katalog bleibt als aktive Migrationsbruecke bestehen. Placeholder-Plugins werden bewusst nicht gegen Built-ins priorisiert, damit bestehende Imports funktionsfaehig bleiben.
- AI-Foto und AI-URL persistieren weiterhin ueber den bestehenden Web-internen Basishandler; sie sind noch nicht auf den neutralen DTO-Rueckgabeweg umgestellt.
- Allgemeine Parser-/Basishilfen, insbesondere `BaseImportHandler`, liegen weiterhin im Webprojekt.
- Die Akzeptanzpunkte "Alle vorhandenen Importquellen liegen produktiv in separaten Pluginprojekten" und "Pluginprojekte enthalten die ausgelagerte Importlogik" sind noch nicht vollstaendig erfuellt.

## Bewertung

Die Fortsetzung hat mehrere Review-Befunde aus Iteration 3 geschlossen und die Projektstruktur vorbereitet. Der Zielzustand ist aber noch nicht vollstaendig erreicht, weil die eigentliche produktive Parserauslagerung in die Pluginprojekte aussteht.
