# Bestandsaufnahme: Pluginsystem fuer Rezeptimporte

## Zusammenfassung

Die Anwendung ist eine .NET-Loesung mit einem Webprojekt (`Rezepte.Web`) und einem Testprojekt (`Rezepte.Tests`). Rezeptimporte sind bereits fachlich gebuendelt, aber technisch noch fest im Hauptprojekt implementiert. Die Importauswahl erfolgt derzeit ueber fest registrierte `IImportHandler`-Implementierungen in der Dependency Injection, nicht ueber ein Pluginverzeichnis, Plugin-Metadaten oder eine administrative Reihenfolge.

Die Anforderung betrifft damit vor allem:

- Auslagerung der Importvertraege aus `Rezepte.Web` in ein neues Shared-Projekt.
- Auslagerung der vorhandenen quellenabhaengigen Handler in separate Klassenbibliotheken.
- Ersetzung der aktuellen DI-basierten Handlerliste durch einen `PluginManager`, der Plugin-DLLs aus `plugins` und Unterordnern erkennt, laedt und nach gespeicherter Aktivierung/Reihenfolge anspricht.
- Erweiterung der Admin-Einstellungen um Pluginverwaltung.
- Anpassung der bestehenden Importstrecke auf der Startseite, ohne den fachlichen Importablauf zu veraendern.

## Detaildokumente

- [Importarchitektur](inventory/import-architecture.md)
- [Plugin-Schnittstellen und Projektstruktur](inventory/plugin-contracts-and-projects.md)
- [Persistenz und Administration](inventory/persistence-and-admin.md)
- [Tests, Risiken und offene Entscheidungen](inventory/tests-risks-open-points.md)

## Relevante Ist-Struktur

| Bereich | Ist-Zustand | Relevante Dateien |
|---------|-------------|-------------------|
| Loesung/Projekte | Nur `Rezepte.Web` und `Rezepte.Tests`; kein Shared-Projekt, keine Pluginprojekte. | `Rezepte.sln`, `Rezepte.Web/Rezepte.Web.csproj`, `Rezepte.Tests/Rezepte.Tests.csproj` |
| Importvertraege | `IImportHandler`, `IImportService`, `ImportResult` liegen im Webprojekt. | `Rezepte.Web/Services/Import/IImportHandler.cs`, `Rezepte.Web/Services/Import/IImportService.cs` |
| Handler-Auswahl | Handler werden fest als `IImportHandler` registriert; Reihenfolge entspricht DI-Registrierung. | `Rezepte.Web/Extensions/ServiceCollectionExtensions.cs:139` bis `Rezepte.Web/Extensions/ServiceCollectionExtensions.cs:148` |
| Sessionbasierter Import | `ImportOrchestrator` erstellt Scopes, liest alle `IImportHandler` aus DI und prueft sie sequenziell. | `Rezepte.Web/Services/Import/ImportOrchestrator.cs:61`, `Rezepte.Web/Services/Import/ImportOrchestrator.cs:65` |
| Nicht-sessionbasierter Import | `ImportService` prueft alle injizierten Handler sequenziell. | `Rezepte.Web/Services/Import/ImportService.cs:5`, `Rezepte.Web/Services/Import/ImportService.cs:18` |
| Startseiten-Dialog | `CreateRecipeDialog` startet Import-Sessions fuer Datei und URL und pollt Status/Bestaetigung. | `Rezepte.Web/Components/Shared/CreateRecipeDialog.razor:182`, `Rezepte.Web/Components/Shared/CreateRecipeDialog.razor:320` |
| API-Endpunkte | `CookbooksController` bietet alte direkte Imports und neue sessionbasierte Imports an. | `Rezepte.Web/Controllers/CookbooksController.cs:107`, `Rezepte.Web/Controllers/CookbooksController.cs:304`, `Rezepte.Web/Controllers/CookbooksController.cs:463` |
| Einstellungen | Persistenz existiert mit `AppSetting`/`UserSetting`; Plugin-Konfiguration existiert noch nicht. | `Rezepte.Web/Services/SettingsService.cs`, `Rezepte.Web/Data/RezepteDbContext.cs` |
| Admin-UI | Einstellungsnavigation kennt Admin-only Komponenten, aber keine Pluginverwaltung. | `Rezepte.Web/ViewModels/SettingsViewModel.cs:25`, `Rezepte.Web/ViewModels/SettingsViewModel.cs:31` |

## Vorhandene Importquellen

Die derzeit fest registrierten Importhandler sind:

- `BackupImportHandler`
- `ChefkochReceiptImportHandler`
- `SecondSourceUrlReceiptImportHandler`
- `ThirdSourceUrlReceiptImportHandler`
- `FourthSourceUrlReceiptImportHandler`
- `FifthSourceUrlRecipeImportHandler`
- `SixthSourceUrlRecipeImportHandler`
- `AIFotoImportHandler`
- `AIUrlImportHandler`

Die quellenabhaengigen URL-Handler liegen unter `Rezepte.Web/Services/Import/Url/`. Auffaellig: `FourthSourceUrlReceiptImportHandler.cs` enthaelt zwei Klassen, naemlich `SixthSourceUrlRecipeImportHandler` und `FourthSourceUrlReceiptImportHandler`; das ist fuer eine spaetere Projektaufteilung ein konkreter Aufraeumpunkt.

## Zentrale Luecken gegenueber der Anforderung

- Es gibt kein Shared-Projekt fuer Importvertraege.
- Es gibt keine Klassenbibliotheksprojekte pro Importquelle.
- Es gibt kein Pluginverzeichnis und keine Laufzeit-Erkennung von DLLs.
- Es gibt keinen `PluginManager`.
- Aktivierung und Reihenfolge sind nicht persistiert.
- Die Admin-Einstellungen zeigen keine Pluginliste.
- Deaktivierte Plugins koennen noch nicht vom Import ausgeschlossen werden.
- Neu erkannte Plugins werden noch nicht automatisch an eine bestehende Liste angehaengt.

## Naheliegende Umsetzungsschwerpunkte

1. Shared-Projekt fuer Vertrage und einfache DTOs einfuehren.
2. Plugin-Metadaten definieren, damit Plugins stabil identifizierbar und administrierbar sind.
3. `PluginManager` im Webprojekt einfuehren, der DLLs beim Start erkennt, laedt, instanziiert und mit gespeicherter Konfiguration zusammenfuehrt.
4. Persistenz fuer Plugin-Aktivierung und Reihenfolge ergaenzen, bevorzugt als typisierte Tabelle oder als JSON in `AppSetting`.
5. Admin-Komponente unter Einstellungen ergaenzen.
6. `ImportOrchestrator` und `ImportService` auf `PluginManager` umstellen.
7. Bestehende Handler schrittweise in Pluginprojekte verschieben und Tests fuer Reihenfolge/Aktivierung/Fehlerfaelle ergaenzen.
