# Produktive Plugin-Projekte

## Uebersicht

Alle produktiven Plugin-Projekte sind einfache `Microsoft.NET.Sdk`-Klassenbibliotheken mit:

- `TargetFramework` `net10.0`
- `Nullable` aktiviert
- `ImplicitUsings` aktiviert
- `ProjectReference` auf `..\Rezepte.Import.Abstractions\Rezepte.Import.Abstractions.csproj`

## Projekte

| Projekt | Plugin-ID | Zweck | Bemerkung |
|---------|-----------|-------|-----------|
| `Rezepte.Import.Plugins.Backup` | `backup` | Importiert Backup-ZIP-Dateien mit `recipes.json`. | Fachlich kein Online-Rezeptabruf; Auslagerungsumfang klaeren. |
| `Rezepte.Import.Plugins.Chefkoch` | `chefkoch` | Importiert Chefkoch-Rezepte und kann Chefkoch-Sammlungen voranzeigen. | Staerkster Kandidat fuer manuellen Nachweis. |
| `Rezepte.Import.Plugins.SecondSource` | `second-source` | Parst JSON-LD-Rezeptdaten aus einer zweiten Quelle. | Quelle ist anonymisiert/generisch benannt. |
| `Rezepte.Import.Plugins.ThirdSource` | `third-source` | Parst strukturierte Rezeptdaten aus JSON-LD. | Quelle ist anonymisiert/generisch benannt. |
| `Rezepte.Import.Plugins.FourthSource` | `fourth-source` | Parst Next.js-/PageProps-artige Rezeptdaten. | Quelle ist anonymisiert/generisch benannt. |
| `Rezepte.Import.Plugins.FifthSource` | `fifth-source` | Parst `@graph`-JSON-LD mit Recipe-Knoten. | Quelle ist anonymisiert/generisch benannt. |
| `Rezepte.Import.Plugins.SixthSource` | `sixth-source` | Parst `@graph`-JSON-LD mit Recipe-Knoten. | Quelle ist anonymisiert/generisch benannt. |

## Solution-Verankerung

`Rezepte.sln` enthaelt alle produktiven Plugin-Projekte. Fuer eine echte Auslagerung muessen diese Projektverweise aus der Haupt-Solution entfernt oder durch externe Paket-/Artifact-Nutzung ersetzt werden.

## Namens- und Datenschutzaspekt

Bis auf `Chefkoch` und `Backup` sind die Quellen neutral als `SecondSource` bis `SixthSource` benannt. Das spricht dafuer, dass beim neuen privaten Repository entweder diese neutralen Namen beibehalten oder die echten Quellnamen bewusst nur privat dokumentiert werden sollten.

## Build-Ausgabe

Die Web-App erwartet externe Plugins als DLLs direkt unter `plugins/` oder in einem Unterordner, dessen Name idealerweise dem Assembly-Namen entspricht. Ein ausgelagertes Repository braucht daher ein klares Publish-/Copy-Ziel, zum Beispiel:

- `plugins/Rezepte.Import.Plugins.Chefkoch/Rezepte.Import.Plugins.Chefkoch.dll`
- daneben alle notwendigen Abhaengigkeiten ausser `Rezepte.Import.Abstractions.dll`, die vom Host bevorzugt geteilt wird
