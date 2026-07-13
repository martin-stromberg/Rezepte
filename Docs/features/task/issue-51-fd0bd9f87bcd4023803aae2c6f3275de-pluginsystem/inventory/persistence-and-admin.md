# Detail: Persistenz und Administration

## Bestehende Persistenz fuer Einstellungen

Die Anwendung hat bereits zwei Einstellungsmodelle:

- `UserSetting`
- `AppSetting`

Belege:

- DbSets: `Rezepte.Web/Data/RezepteDbContext.cs:20`, `Rezepte.Web/Data/RezepteDbContext.cs:21`
- `UserSetting`-Konfiguration: `Rezepte.Web/Data/RezepteDbContext.cs:226`
- `AppSetting`-Konfiguration: `Rezepte.Web/Data/RezepteDbContext.cs:235`
- `AppSetting.Key` als Primaerschluessel: `Rezepte.Web/Data/RezepteDbContext.cs:237`
- Key-Laenge 128: `Rezepte.Web/Data/RezepteDbContext.cs:238`

`SettingsService` nutzt `AppSetting` bereits fuer globale Einstellungen und per-User UI-Zustaende mit Key-Prefix. Beispiele sind globale AI-Schalter und `ShoppingListEditMode:{userId}`.

Belege:

- Globaler AI-Key: `Rezepte.Web/Services/SettingsService.cs:40`
- Lesen globaler Einstellung: `Rezepte.Web/Services/SettingsService.cs:50`
- Schreiben globaler Einstellung: `Rezepte.Web/Services/SettingsService.cs:57`
- ShoppingList-Prefix: `Rezepte.Web/Services/SettingsService.cs:48`
- Per-User ShoppingList-Mode ueber `AppSetting`: `Rezepte.Web/Services/SettingsService.cs:147`, `Rezepte.Web/Services/SettingsService.cs:154`

## Optionen fuer Plugin-Konfiguration

Fuer Aktivierung und Reihenfolge gibt es zwei naheliegende Speicheransaetze:

| Ansatz | Vorteil | Nachteil |
|--------|---------|----------|
| JSON in `AppSetting` | Keine neue Tabelle; passt zum bestehenden Key-Value-Stil. | Schwerer gezielt zu migrieren/validieren; Reihenfolge und einzelne Pluginzustaende nur als Blob. |
| Neue Tabelle `PluginSetting` | Saubere Indizes, eindeutige Plugin-IDs, einfaches Anhaengen neuer Plugins, gut testbar. | Neue Entity und Migration erforderlich. |

Da die Anforderung eine dauerhafte, administrierbare Pluginliste mit Reihenfolge und Aktivierung verlangt, ist eine eigene Tabelle fachlich robuster. Ein moegliches Modell:

- `PluginId` oder `Key` als stabile ID
- `DisplayName`
- `AssemblyName`
- `TypeName`
- `Enabled`
- `OrderIndex`
- `DiscoveredAt`
- `LastSeenAt`
- optional `Status` und `Error`

Falls moeglich sollte die stabile Plugin-ID aus Plugin-Metadaten kommen und nicht aus Dateipfad oder Assemblyversion, damit Updates nicht als neue Plugins erscheinen.

## Bestehende Admin-UI

Die Einstellungen sind unter `/settings` erreichbar und verwenden `SettingsViewModel` als Navigationsmodell. Admin-Sichtbarkeit wird ueber die Rolle `Admin` bestimmt. Es gibt bereits Admin-only Eintraege wie Benutzer und Sicherung.

Belege:

- Settings-Seite: `Rezepte.Web/Components/Pages/Settings.razor`
- Admin-Ermittlung: `Rezepte.Web/ViewModels/SettingsViewModel.cs:25`
- Items-Liste: `Rezepte.Web/ViewModels/SettingsViewModel.cs:27`
- Admin-only Benutzer: `Rezepte.Web/ViewModels/SettingsViewModel.cs:31`
- Admin-only Sicherung: `Rezepte.Web/ViewModels/SettingsViewModel.cs:33`

Eine Pluginverwaltung kann als neue Komponente unter `Rezepte.Web/Components/Settings/` umgesetzt und in `SettingsViewModel` als Admin-only Eintrag ergaenzt werden.

## Anforderungen an die Admin-Funktion

Die Admin-Komponente muss mindestens:

- alle gefundenen Plugins anzeigen
- Aktivierung/Deaktivierung pro Plugin speichern
- Reihenfolge anzeigen und aendern
- neue Plugins automatisch am Ende der Liste sichtbar machen
- Fehlerhafte/inkompatible Plugins sinnvoll anzeigen, falls diese Entscheidung getroffen wird

Fuer die Reihenfolge existiert bereits ein Muster im Projekt: Kochbuecher haben `OrderIndex` und einen Reorder-Endpunkt. Das laesst sich als fachliches Muster fuer Pluginreihenfolge nutzen, ohne Code direkt zu kopieren.

Belege:

- Cookbook `OrderIndex`: `Rezepte.Web/Data/RezepteDbContext.cs:47`
- Reorder-Endpunkt: `Rezepte.Web/Controllers/CookbooksController.cs:287`

## Konsequenz fuer PluginManager

Der PluginManager braucht beim Start oder bei Initialisierung eine Synchronisation:

1. DLLs unter `plugins` und direkten Unterordnern finden.
2. Plugin-Metadaten und kompatible Importhandler ermitteln.
3. Persistierte Konfiguration laden.
4. Neue Plugin-IDs hinten anhaengen.
5. Nicht mehr vorhandene Plugins in der Konfiguration behalten oder als nicht gefunden markieren.
6. Nur aktivierte Plugins in gespeicherter Reihenfolge fuer Imports liefern.

Die Anforderung sagt nur Programmstart-Erkennung; ein Laufzeit-Rescan ist nicht gefordert.

