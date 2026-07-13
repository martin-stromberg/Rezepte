# Code-Review: Pluginsystem fuer Rezeptimporte

Status: Befunde vorhanden

## Befunde

### 1. Direkte Plugin-DLLs unter `plugins` erzeugen falsche Fehler-/Inkompatibilitaetseintraege fuer Abhaengigkeiten

Schweregrad: Mittel

Fundstelle:
- `Rezepte.Web/Services/Import/Plugins/PluginManager.cs:95`
- `Rezepte.Web/Services/Import/Plugins/PluginManager.cs:96`
- `Rezepte.Web/Services/Import/Plugins/PluginManager.cs:129`
- `Rezepte.Web/Services/Import/Plugins/PluginManager.cs:131`
- `Rezepte.Tests/Services/Import/PluginManagerTests.cs:17`
- `Rezepte.Tests/Services/Import/PluginManagerTests.cs:223`

Der `PluginManager` scannt direkt unter `plugins` jede `*.dll` als Plugin-Kandidat. Damit funktioniert zwar eine einzelne Plugin-DLL im Root, aber sobald diese DLL dort eine Abhaengigkeit neben sich braucht, wird auch die Abhaengigkeits-DLL geladen und als `Incompatible` persistiert, weil sie keine `IImportPlugin`-Implementierung enthaelt. Der bestehende Test fuer Root-Plugins kopiert sogar `Rezepte.Import.Abstractions.dll` neben die Fixture-DLL, prueft aber nur, dass das eigentliche Plugin geladen wurde; der zusaetzliche Inkompatibilitaetseintrag bleibt unbemerkt.

Auswirkung: Ein laut Plan unterstuetztes Layout (`plugins/*.dll`) verschmutzt die Plugin-Verwaltung mit Nicht-Plugin-DLLs. Bei nativen oder nicht ladbaren Abhaengigkeiten koennen zudem `LoadFailed`-Eintraege entstehen, obwohl die eigentliche Plugin-DLL korrekt waere.

Empfehlung: Fuer Root-Plugins eine klare Manifest-/Namenskonvention oder bevorzugt das Unterordnerlayout erzwingen. Alternativ beim Root-Scan bekannte Contract-/Dependency-Assemblies nicht persistieren und einen Test ergaenzen, der sicherstellt, dass nebenliegende Abhaengigkeits-DLLs keinen Admin-Eintrag erzeugen.

### 2. Nicht erzeugbare Handler bleiben in der Admin-Konfiguration als geladen und aktiviert stehen

Schweregrad: Mittel

Fundstelle:
- `Rezepte.Web/Services/Import/Plugins/PluginManager.cs:49`
- `Rezepte.Web/Services/Import/Plugins/PluginManager.cs:51`
- `Rezepte.Web/Services/Import/Plugins/PluginManager.cs:67`
- `Rezepte.Web/Services/Import/Plugins/PluginManager.cs:70`
- `Rezepte.Web/Services/Import/Plugins/PluginManager.cs:72`

`GetActiveHandlersAsync` liest nur `Enabled && Status == Loaded` und versucht dann, den Handler per `ActivatorUtilities.CreateInstance` zu erzeugen. Scheitert die Erzeugung, wird nur geloggt; der persistierte Pluginstatus bleibt `Loaded`, die Fehlermeldung wird nicht in `PluginSetting.Error` sichtbar, und das Plugin bleibt aktiviert.

Auswirkung: Ein externes Plugin mit fehlender DI-Abhaengigkeit, defektem Konstruktor oder fehlerhafter Laufzeitabhaengigkeit erscheint Admins als geladen und aktiv, wird bei Imports aber still uebersprungen. Nutzer erhalten dann im Zweifel nur `No suitable import plugin found for this file or URL.`, obwohl ein aktiviertes Plugin konfiguriert ist.

Empfehlung: Instanziierungsfehler als sichtbaren Status behandeln, z. B. `LoadFailed`/`Incompatible` oder einen eigenen Runtime-Fehlerstatus in `PluginSetting` schreiben. Dazu einen Test mit einem Pluginhandler ergaenzen, dessen Konstruktor nicht aufloesbar ist.

### 3. Externe Produktiv-Plugins mit gleicher ID wie Built-ins koennen nicht wirksam werden

Schweregrad: Mittel

Fundstelle:
- `Rezepte.Web/Services/Import/Plugins/PluginManager.cs:81`
- `Rezepte.Web/Services/Import/Plugins/PluginManager.cs:82`
- `Rezepte.Web/Services/Import/Plugins/PluginManager.cs:83`
- `Rezepte.Web/Services/Import/Plugins/PluginManager.cs:29`
- `Rezepte.Web/Services/Import/Plugins/PluginManager.cs:31`
- `Rezepte.Web/Services/Import/Plugins/PluginManager.cs:32`
- `Rezepte.Web/Services/Import/Plugins/PluginManager.cs:198`
- `Rezepte.Web/Services/Import/Plugins/BuiltInImportPluginCatalog.cs:9`
- `Rezepte.Web/Services/Import/Plugins/BuiltInImportPluginCatalog.cs:17`

Die Discovery-Liste enthaelt zuerst die Built-in-Handler und danach externe Plugins. Beim Aufbau von `_loadedPlugins` und beim Synchronisieren der Settings wird bei doppelten IDs jeweils `First()` genommen. Wenn spaetere echte Produktiv-Pluginprojekte dieselben stabilen IDs wie die vorhandenen Built-ins verwenden, z. B. `chefkoch` oder `ai-url`, gewinnt weiterhin der Web-interne Built-in-Handler. Das externe Plugin wird weder als aktiver Handler noch in den Settings sichtbar.

Auswirkung: Der Built-in-Katalog kann als Migrationsbruecke die eigentliche Auslagerung blockieren. Nach Einfuehrung echter Produktiv-Plugins waere schwer erkennbar, dass weiter die alten Web-Handler laufen.

Empfehlung: Entweder Built-ins entfernen, sobald die Produktiv-Plugins existieren, oder eine eindeutige Prioritaetsregel implementieren und testen. Wenn externe Plugins Built-ins ersetzen sollen, muessen externe Deskriptoren bei gleicher ID Vorrang bekommen oder Duplicate-IDs als Konfigurationsfehler sichtbar werden.

### 4. Produktive Importquellen sind weiter Web-interne Built-ins statt separate Pluginprojekte

Schweregrad: Hoch

Fundstelle:
- `Rezepte.sln:10`
- `Rezepte.sln:12`
- `Rezepte.Web/Services/Import/Plugins/BuiltInImportPluginCatalog.cs:7`
- `Rezepte.Web/Services/Import/Plugins/BuiltInImportPluginCatalog.cs:9`
- `Rezepte.Web/Services/Import/Plugins/BuiltInImportPluginCatalog.cs:17`
- `Rezepte.Web/Services/Import/BackupImportHandler.cs:13`
- `Rezepte.Web/Services/Import/Url/FourthSourceUrlReceiptImportHandler.cs:12`
- `Rezepte.Web/Services/Import/Url/SixthSourceUrlRecipeImportHandler.cs:10`

Die geplanten Produktiv-Pluginprojekte wie `Rezepte.Import.Plugins.Backup`, `Rezepte.Import.Plugins.Chefkoch`, `Rezepte.Import.Plugins.AIFoto` und `Rezepte.Import.Plugins.AIUrl` fehlen weiterhin. Stattdessen registriert `BuiltInImportPluginCatalog` alle vorhandenen Web-Handler als geladene Plugins.

Auswirkung: Die Hauptanforderung, quellenabhaengige Importlogik aus `Rezepte.Web` herauszuloesen, ist noch nicht erreicht. Die Admin-UI und der PluginManager arbeiten zwar gegen Plugin-Deskriptoren, die produktive Importlogik bleibt aber an Webprojekt, Host-Services und Web-Entity-Typen gekoppelt.

Empfehlung: Die vorhandenen Quellen tatsaechlich in separate Klassenbibliotheken verschieben und den Built-in-Katalog nur temporaer oder gar nicht mehr verwenden. Die Solution sollte die Produktiv-Pluginprojekte enthalten und deren Output ueber das bereits korrigierte Unterordnerlayout nach `plugins/<PluginName>/` kopieren.

### 5. Der Shared-Contract bildet die geplante hostneutrale Rezeptuebergabe noch nicht ab

Schweregrad: Mittel

Fundstelle:
- `Rezepte.Import.Abstractions/IImportHandler.cs:7`
- `Rezepte.Import.Abstractions/IImportHandler.cs:9`
- `Rezepte.Import.Abstractions/ImportResult.cs:3`
- `Rezepte.Web/Services/Import/Url/BaseUrlReceiptImportHandler.cs:8`
- `Rezepte.Web/Services/Import/Url/BaseUrlReceiptImportHandler.cs:383`
- `Rezepte.Web/Services/Import/Url/BaseUrlReceiptImportHandler.cs:385`
- `Rezepte.Web/Services/Import/BaseAIImportHandler.cs:11`
- `Rezepte.Web/Services/Import/BaseAIImportHandler.cs:135`

Die neutralen DTOs existieren, aber `IImportHandler.HandleAsync` gibt weiterhin nur `ImportResult` mit erzeugten Rezept-IDs zurueck. Die bestehenden Handler persistieren Rezepte direkt ueber `IRecipeService`, statt neutrale Rezeptdaten an den Host zu liefern. Eine Host-Mapping-Schicht von `ImportedRecipe` nach `Recipe`, Schritten, Zutaten, Bildern und Cookbook-Zuordnung ist nicht vorhanden.

Auswirkung: Externe Plugins koennen das Zielbild nicht sauber erfuellen, ohne Host-Persistenzdienste zu kennen. Damit bleibt die Kopplung an `Rezepte.Web` fachlich bestehen, auch wenn die Interfaces in ein Shared-Projekt verschoben wurden.

Empfehlung: Den Contract um einen hostneutralen Ergebnisweg erweitern und die Persistenz in eine Host-Mapping-Schicht verschieben. Erst danach sollten Produktiv-Plugins nur noch `Rezepte.Import.Abstractions` und fachlich notwendige Pakete referenzieren.

## Behobene Punkte aus dem vorherigen Code-Review

- Das Build-/Publish-Kopierlayout wurde verbessert: Pluginprojekt-Outputs werden jetzt unter `plugins\<PluginName>\...` kopiert statt pro DLL-Dateiname in getrennte Ordner (`Rezepte.Web/Rezepte.Web.csproj:50` bis `Rezepte.Web/Rezepte.Web.csproj:73`).
- Fehlerhafte DLLs, DLLs ohne `IImportPlugin` und Plugins mit ungueltigem HandlerType sind jetzt durch Tests abgedeckt (`Rezepte.Tests/Services/Import/PluginManagerTests.cs:49`, `Rezepte.Tests/Services/Import/PluginManagerTests.cs:65`, `Rezepte.Tests/Services/Import/PluginManagerTests.cs:80`).
- Der sessionbasierte `ImportOrchestrator` hat Tests fuer Reihenfolge, interaktive Bestaetigung und Fehlerabbruch nach dem ersten passenden Plugin (`Rezepte.Tests/Services/Import/ImportOrchestratorTests.cs:13`, `Rezepte.Tests/Services/Import/ImportOrchestratorTests.cs:30`, `Rezepte.Tests/Services/Import/ImportOrchestratorTests.cs:47`).

## Fehlende Tests

- Root-Pluginlayout mit nebenliegenden Dependency-DLLs darf keine zusaetzlichen `Incompatible`-/`LoadFailed`-Admin-Eintraege fuer reine Abhaengigkeiten erzeugen.
- Handler mit nicht aufloesbarer Konstruktor-/DI-Abhaengigkeit muss sichtbar als Fehlerstatus landen und darf nicht nur im Log erscheinen.
- Duplicate-ID-Verhalten zwischen Built-in- und externen Plugins muss festgelegt und getestet werden.
- Produktiv-Pluginprojekte fuer alle vorhandenen Quellen muessen gebaut und aus dem realen `plugins/<PluginName>/`-Layout geladen werden.
- Hostneutrale DTO-Mapping-Schicht muss getestet werden, sobald der Contract entsprechend erweitert ist.

## Ausgefuehrte Pruefungen

- `dotnet test Rezepte.sln --no-restore --logger "console;verbosity=minimal"`: erfolgreich. 137 Tests bestanden, 0 fehlgeschlagen, 0 uebersprungen.

## Hinweise

- Der Testlauf meldet weiterhin `NU1903` fuer `SQLitePCLRaw.lib.e_sqlite3` Version `2.1.11` mit hoher Sicherheitsrelevanz. Das ist nicht offensichtlich durch den Plugin-Umbau verursacht, sollte aber separat behoben werden.
- Die Review-Bewertung basiert auf dem aktuellen uncommitted Repository-Zustand nach Iteration 3.
