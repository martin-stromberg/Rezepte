# Repository-Struktur und Vertragsdateien

## Vertragsnahe Verzeichnisse

| Pfad | Bestand und Rolle |
|---|---|
| `Rezepte.Import.Abstractions/` | Oeffentliche Interfaces, Records, Enums und Importdatenmodelle. |
| `Rezepte.Import.Plugins.Backup/` | Produktives Plugin im Hauptrepository; referenziert Abstractions. |
| `Rezepte.Import.Plugins.AIFoto/` | Produktives KI-Plugin; referenziert Abstractions und zusaetzlich `Rezepte.Web` sowie externe NuGet-Pakete. |
| `Rezepte.Import.Plugins.AIUrl/` | Produktives KI-Plugin; referenziert Abstractions und zusaetzlich `Rezepte.Web` sowie Logging. |
| `Rezepte.Tests.PluginFixture/` | Testfixture gegen Abstractions. |
| `Rezepte.Web/` | Host, Plugin-Erkennung, Installation, Persistenz und Importausfuehrung. |
| `.github/workflows/` | PR- und Release-Automatisierung. |
| `Docs/help/import-plugins.md` | Beschreibung des Plugin-Frameworks und der behaupteten privaten SDK-Struktur. |

## Solution und Projektverweise

`Rezepte.sln` enthaelt `Rezepte.Import.Abstractions`, die drei produktiven
Plugins, `Rezepte.Web` sowie Testprojekte. Die produktiven Plugins verweisen
direkt auf `Rezepte.Import.Abstractions.csproj`. Ein Projekt mit dem Namen
`Rezepte.Import.PluginSdk` ist weder in der Solution noch im Dateisystem
vorhanden.

## Mindestumfang im Vergleich zum Ist-Zustand

Die Anforderung nennt `Directory.Build.props`, den kompletten Ordner
`Rezepte.Import.Abstractions/` und den kompletten Ordner
`Rezepte.Import.PluginSdk/`. Im Ist-Zustand existieren nur die Abstractions;
`Directory.Build.props` und PluginSdk fehlen. Ein Exporter muss deshalb fehlende
Pfadgruppen als Fehler behandeln und darf nicht stillschweigend ein unvollstaendiges
ZIP erzeugen.

## Buildisolierung

`Rezepte.Import.Abstractions.csproj` ist ein eigenstaendiges SDK-Projekt fuer
`net10.0` mit Nullable und Implicit Usings. Es hat keine Projekt- oder
NuGet-Abhaengigkeiten und ist daher grundsaetzlich isoliert baubar.

Die beiden KI-Plugin-Projekte enthalten dagegen einen bedingten Assembly-
Verweis auf `Rezepte.Web.dll`. Dieser Verweis wird im normalen Build aus
`Rezepte.Web/bin/...` aufgeloest oder ueber `RezepteWebReferencePath` gesetzt.
Das ist eine Hostkopplung und kein geeigneter Bestandteil eines isolierten
externen Plugin-Vertrags, sofern die Plugins in den Contract-Export gelangen
sollen.

