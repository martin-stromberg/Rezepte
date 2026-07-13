# Detail: Tests, Risiken und offene Entscheidungen

## Aktuelle Testlage

Es gibt Tests fuer viele Services und fuer `SettingsService`, aber keine dedizierten Importhandler-, ImportOrchestrator- oder Plugin-Tests. Eine Suche nach Import-bezogenen Testdateien liefert keine `*Import*.cs`-Tests.

Belege:

- `Rezepte.Tests/Rezepte.Tests.csproj`
- `Rezepte.Tests/Services/SettingsServiceTests.cs:23`
- `Rezepte.Tests/Services/SettingsServiceTests.cs:52`
- `Rezepte.Tests/Services/SettingsServiceTests.cs:97`

Die vorhandenen `SettingsServiceTests` zeigen, dass InMemory-EF fuer Einstellungslogik bereits genutzt wird. Das ist ein guter Ansatz fuer neue Tests der Plugin-Persistenz.

## Wichtige neue Testfaelle

Plugin-Erkennung:

- DLL direkt unter `plugins` wird erkannt.
- DLL in direktem Unterordner von `plugins` wird erkannt.
- nicht kompatible DLL wird ignoriert oder als fehlerhaft gemeldet.
- neues Plugin wird an bestehende Liste hinten angehaengt.
- bestehende Reihenfolge bleibt erhalten, wenn neue Plugins auftauchen.

Plugin-Auswahl:

- deaktivierte Plugins werden nicht angesprochen.
- aktivierte Plugins werden exakt in gespeicherter Reihenfolge angesprochen.
- erstes passendes Plugin liefert das Ergebnis.
- spaetere passende Plugins werden nicht mehr angesprochen.
- kein passendes Plugin fuehrt zu definierter Fehlermeldung.

Integration:

- `ImportService` verwendet PluginManager statt DI-Handlerliste.
- `ImportOrchestrator` verwendet PluginManager und erhaelt interaktive Handler-Unterstuetzung.
- Admin-API/Service speichert Aktivierung und Reihenfolge.
- Settings-Komponente zeigt gefundene Plugins und kann Reihenfolge/Aktivierung aendern.

Projektstruktur:

- Shared-Projekt wird von Webprojekt und Pluginprojekten referenziert.
- Pluginprojekte werden in die Solution aufgenommen.
- Build/Publish kopiert Plugin-DLLs in die erwartete Struktur oder dokumentiert die Ablage.

## Technische Risiken

### AssemblyLoadContext und Abhaengigkeiten

Plugin-DLLs koennen eigene Abhaengigkeiten mitbringen. Laden aus Unterordnern spricht fuer einen eigenen `AssemblyLoadContext` mit `AssemblyDependencyResolver` pro Pluginordner. Ein einfacher `Assembly.LoadFrom`-Ansatz kann bei Versionskonflikten oder Neben-DLLs schnell instabil werden.

### Handler-Lebensdauer und Zustand

Mehrere bestehende Handler sind nicht rein stateless. `BaseUrlReceiptImportHandler` cached das Parse-Ergebnis zwischen `CanHandleAsync` und `HandleAsync`; `BaseAIImportHandler` cached ebenfalls Importdaten. Der PluginManager darf daher nicht eine Singleton-Handlerinstanz parallel fuer mehrere Imports wiederverwenden.

Belege:

- URL-Handler-Cache/CanHandle: `Rezepte.Web/Services/Import/Url/BaseUrlReceiptImportHandler.cs:451`
- URL-Handler erwartet vorheriges `CanHandleAsync`: `Rezepte.Web/Services/Import/Url/BaseUrlReceiptImportHandler.cs:476`
- AI-Handler-CanHandle und interaktives Handling: `Rezepte.Web/Services/Import/BaseAIImportHandler.cs:61`, `Rezepte.Web/Services/Import/BaseAIImportHandler.cs:199`

### Host-Service-Abhaengigkeiten

Die heutigen Handler speichern Rezepte direkt ueber `IRecipeService` und nutzen weitere Host-Services. Werden sie in externe Projekte verschoben, muss klar sein, welche Services Plugins bekommen duerfen. Ohne klaren Host-Service-Vertrag entsteht eine enge Kopplung an `Rezepte.Web`.

### Sicherheitsmodell

DLL-Plugins sind voll vertrauenswuerdiger Code. Die Anforderung beschreibt kein Sandboxing, Signaturen oder Rechtekonzept. Das sollte mindestens dokumentiert werden, selbst wenn fuer die erste Umsetzung lokale Admin-Kontrolle als Vertrauensmodell gilt.

### Fehlerhafte Plugins

Die Anforderung nennt nicht, wie fehlerhafte oder inkompatible Plugin-DLLs angezeigt werden sollen. Fuer eine Admin-Verwaltung ist ein Status pro Plugin sinnvoll, z. B. `Loaded`, `Missing`, `Incompatible`, `LoadFailed`.

### Build- und Publish-Prozess

Derzeit gibt es keine Pluginprojekte und keine Publish-Konfiguration fuer `plugins`. Die Umsetzung muss klaeren, wie vorhandene Pluginprojekte gebaut und ihre DLLs im Programmverzeichnis unter `plugins` abgelegt werden.

## Offene Entscheidungen aus der Anforderung

- Welche vorhandenen Importquellen muessen zwingend in eigene Pluginprojekte ausgelagert werden?
- Sollen Backup-, AI-Foto- und AI-URL-Import ebenfalls Plugins sein oder nur quellenabhaengige URL-Handler?
- Wird Plugin-Konfiguration in einer neuen Tabelle oder als JSON in `AppSetting` gespeichert?
- Was passiert, wenn kein aktiviertes Plugin die Quelle verarbeiten kann?
- Wie werden fehlerhafte oder inkompatible Plugins in der UI angezeigt?
- Soll es nur Programmstart-Erkennung geben oder spaeter einen manuellen Rescan?
- Soll ein Plugin neutrale Rezeptdaten liefern oder direkt ueber Host-Services Rezepte speichern?

