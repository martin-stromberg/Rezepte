# Code-Review: Pluginsystem fuer Rezeptimporte

Status: Befunde vorhanden

## Behobene Befunde

- Produktive Backup- und URL-Pluginprojekte enthalten keine Placeholder-Handler mehr und melden Version `1.0.0`.
- Die portierten Pluginhandler referenzieren weder `Rezepte.Web` noch `IRecipeService`.
- Allgemeine Zutaten-/Zeitparser und URL-Parserhilfen wurden in `Rezepte.Import.Abstractions` verschoben.
- Der Web-Built-in-Katalog registriert keine Backup-/URL-Handler mehr direkt.
- Die alten Web-internen Backup-/URL-Handlerdateien wurden geloescht.
- AI-Handler erzeugen nun neutrale `ImportedRecipe`-DTOs; Persistenz erfolgt zentral ueber `ImportedRecipePersister`.
- `dotnet build Rezepte.sln --no-restore` ist erfolgreich und validiert, dass die echten Pluginprojekte gebaut und in den Web-Output kopiert werden.

## Befunde

### 1. AI-Pluginprojekte enthalten weiterhin Placeholder-Handler

Schweregrad: Mittel

`Rezepte.Import.Plugins.AIFoto` und `Rezepte.Import.Plugins.AIUrl` sind weiterhin mit `0.0.0-placeholder` markiert und liefern `CanHandleAsync = false`. Die produktiven AI-Handler liegen weiter in `Rezepte.Web` als Built-ins, auch wenn ihr Rueckgabepfad jetzt hostneutral ist.

Auswirkung: Zwei der neun Importquellen sind noch nicht als produktive externe Pluginprojekte ausgelagert. Die Runtime-Funktion bleibt ueber Hostadapter erhalten.

### 2. Dedizierte produktive Pluginparser-Tests fehlen

Schweregrad: Niedrig

Die bestehende Testsuite prueft PluginManager, Auswahl, Persistenz und Orchestrierung, aber nicht die neu portierten produktiven Parser je Quelle mit repräsentativen HTML-/ZIP-Fixtures.

Auswirkung: Regressionen in einzelnen externen Parsern wuerden aktuell vor allem durch Build und manuelle/Integrationspruefung auffallen.

## Ausgefuehrte Pruefungen

- `dotnet build Rezepte.sln --no-restore`: erfolgreich, 38 Warnungen; darunter NU1903 zu `SQLitePCLRaw.lib.e_sqlite3` und bestehende Nullable-/Obsolete-Warnungen.
- `dotnet test Rezepte.sln --no-restore --logger "console;verbosity=minimal"`: erfolgreich, 137 Tests bestanden, 0 fehlgeschlagen, 0 uebersprungen.
