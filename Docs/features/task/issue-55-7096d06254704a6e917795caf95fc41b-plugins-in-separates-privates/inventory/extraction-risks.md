# Auslagerungspunkte und Risiken

## Direkt auslagerbar

Die folgenden Projekte sind technisch gute Kandidaten fuer das neue private Repository:

- `Rezepte.Import.Plugins.Chefkoch`
- `Rezepte.Import.Plugins.SecondSource`
- `Rezepte.Import.Plugins.ThirdSource`
- `Rezepte.Import.Plugins.FourthSource`
- `Rezepte.Import.Plugins.FifthSource`
- `Rezepte.Import.Plugins.SixthSource`

`Rezepte.Import.Plugins.Backup` ist technisch ebenfalls ein Plugin, aber fachlich ein Backup-Import. Die Anforderung spricht von Rezeptabruf aus bekannten Quellen; deshalb sollte die Planung entscheiden, ob Backup im Hauptrepo bleibt.

## Vertrag und SDK

Die groesste Architekturentscheidung betrifft `Rezepte.Import.Abstractions`:

- Bleibt das Projekt im Hauptrepo, braucht das neue Plugin-Repository eine stabile Paket- oder Projekt-Referenz.
- Wandert es mit in das neue Repository, muss die Web-App den Vertrag von dort beziehen.
- Wird es als NuGet-Paket gefuehrt, muessen Versionierung und Host-/Plugin-Kompatibilitaet explizit geregelt werden.

`UrlRecipeImportHandlerBase` ist mehr als eine reine Abstraktion; sie ist ein Parser-SDK. Eine saubere Auslagerung sollte diese Rolle bewusst benennen.

## Testverschiebung

`ProductiveImportPluginParserTests` referenziert produktive Plugin-Typen direkt. Nach der Auslagerung darf das Hauptrepo diese Plugin-Projekte nicht mehr direkt referenzieren, sonst bleibt die Kopplung bestehen.

Optionen:

- Parser-Tests in das neue Plugin-Repository verschieben.
- Im Hauptrepo nur Discovery-/Host-Tests mit Fixture-Plugin behalten.
- Optional einen Integrationstest ergaenzen, der gebaute Plugin-Artefakte aus einem `plugins/`-Ordner laedt.

## Build- und Packaging-Risiken

- Externe Plugins muessen mit derselben `Rezepte.Import.Abstractions`-Assembly kompatibel sein wie der Host.
- `PluginManager` ignoriert die externe Abstraktionsassembly bewusst und nutzt die Host-Version. Versionsabweichungen koennen deshalb erst zur Laufzeit sichtbar werden.
- Das neue Repository braucht eine dokumentierte Ausgabeform fuer `plugins/<AssemblyName>/<AssemblyName>.dll`.
- Bei anonymisierten Quellnamen muss klar bleiben, welches Plugin welche Quelle nachweist.

## Manuelle Testbarkeit

Das neue Testprogramm sollte Handler direkt instanziieren koennen. Das funktioniert fuer die aktuellen produktiven Plugins, weil sie keine DI-Abhaengigkeiten benoetigen. Falls kuenftige Plugins Services brauchen, muss der Runner entweder DI unterstuetzen oder solche Plugins gesondert behandeln.

## Nicht geloeste fachliche Punkte

- Zielrepository und Name fehlen.
- Speicherort des Testprogramms ist offen, fachlich spricht aber viel fuer das neue Plugin-Repository.
- Mindestumfang der bekannten Quellen ist offen.
- Live-Abrufe koennen durch Webseiten-Aenderungen, Bot-Schutz oder Netzprobleme instabil sein; lokale Beispiel-Dateien sollten den manuellen Nachweis ergaenzen.
