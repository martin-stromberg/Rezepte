# Bestandsaufnahme: Plugins in separates privates Repository

## Zusammenfassung

Die Rezeptimport-Plugins sind bereits fachlich von der Web-Anwendung getrennt: Der gemeinsame Vertrag liegt in `Rezepte.Import.Abstractions`, produktive Plugins liegen als eigene Klassenbibliotheken unter `Rezepte.Import.Plugins.*`, und `Rezepte.Web` kann externe Plugin-DLLs aus `plugins/` laden. Damit ist eine Auslagerung technisch gut vorbereitet.

Noch nicht getrennt sind Solution-Zugehoerigkeit, Tests und Paket-/Build-Verantwortung. Die produktiven Plugin-Projekte sind Teil von `Rezepte.sln`, Parser-Tests liegen in `Rezepte.Tests`, und das bestehende manuelle Testprogramm ist nur ein kleiner Web-API-Hilfspfad fuer gespeicherte Test-URLs. Ein eigenstaendiges rudimentaeres Testprogramm fuer URL- oder Datei-Eingabe und explizite Plugin-Auswahl existiert noch nicht.

## Detaildokumente

- [Architektur und Importvertrag](inventory/architecture.md)
- [Produktive Plugin-Projekte](inventory/plugin-projects.md)
- [Plugin-Discovery und Laufzeitintegration](inventory/runtime-integration.md)
- [Tests und manueller Nachweis](inventory/testing.md)
- [Auslagerungspunkte und Risiken](inventory/extraction-risks.md)

## Relevante Bestandteile

| Bereich | Dateien/Projekte | Befund |
|---------|------------------|--------|
| Importvertrag | `Rezepte.Import.Abstractions` | Eigenes Projekt mit `IImportPlugin`, `IImportHandler`, Ergebnis- und Rezeptmodellen. |
| Produktive Plugins | `Rezepte.Import.Plugins.Backup`, `Chefkoch`, `SecondSource`, `ThirdSource`, `FourthSource`, `FifthSource`, `SixthSource` | Jeweils eigene Klassenbibliothek mit Referenz auf `Rezepte.Import.Abstractions`. |
| Web-Integration | `Rezepte.Web.Services.Import.Plugins.PluginManager` | Laedt Built-ins plus externe DLLs aus `plugins/` unter ContentRoot oder AppContext.BaseDirectory. |
| Plugin-Einstellungen | `PluginSetting`, `PluginSettingsService`, `PluginSettings.razor` | Status, Aktivierung und Reihenfolge werden in der Web-App verwaltet. |
| Importablauf | `ImportService` | Fragt aktive Handler der Reihe nach ab und persistiert importierte Rezepte ueber Web-seitigen Persister. |
| Tests | `PluginManagerTests`, `ProductiveImportPluginParserTests`, `ImportServicePluginTests` | Gute automatisierte Basis fuer Discovery und Parser-Verhalten, aber im Hauptrepo verankert. |
| Manuelle Tests | `ImportTestController`, `TestRecipeImportService` | Nur Admin-Hilfsendpoint fuer `test.recipe-import.json`; kein eigenstaendiger Plugin-Runner. |

## Erfuellung der Anforderung im Ist-Zustand

| Anforderung | Ist-Zustand | Luecke |
|-------------|-------------|--------|
| Plugins in separates Repository auslagern | Plugins sind als separate Projekte gekapselt. | Sie sind noch Bestandteil der Haupt-Solution und Tests referenzieren sie direkt. |
| Ausgelagerte Plugins weiter nutzbar | Web-App kann externe DLLs aus `plugins/` laden. | Build-/Packaging-Konvention fuer externes Repository fehlt. |
| Rudimentaeres Testprogramm | Es gibt Parser-Unit-Tests und einen Web-Test-URL-Helfer. | Kein CLI/Testprogramm mit URL-/Datei-Eingabe, Plugin-Auswahl und Ergebnisanzeige. |
| Nicht verarbeitbare Eingaben melden | `ImportService` liefert Fehler, wenn kein Handler passt. | Ein manueller Runner muss diese Meldung sichtbar machen. |
| Erfolgreiche Rezeptdaten anzeigen | `ImportResult.ImportedRecipes` enthaelt Daten. | Ein manueller Runner muss diese Daten lesbar ausgeben. |

## Offene Punkte fuer die Planung

1. Zielname und Zielpfad des neuen privaten Plugin-Repositories sind nicht festgelegt.
2. Es ist zu entscheiden, ob `Rezepte.Import.Abstractions` im Hauptrepo bleibt und als Paket referenziert wird, oder ob Vertrag und Plugins gemeinsam ausgelagert werden.
3. Das manuelle Testprogramm sollte voraussichtlich im neuen Plugin-Repository liegen, weil es die Nutzbarkeit der ausgelagerten Plugins nachweist.
4. Fuer den Mindestnachweis sollte mindestens `chefkoch` verwendet werden, weil dieses Plugin konkrete HTML- und Collection-Parser-Tests besitzt.
5. Die Backup-Importlogik ist technisch ein Plugin, aber fachlich kein Rezeptabruf aus bekannten Online-Quellen. Die Planung sollte klaeren, ob sie mit ausgelagert wird.
