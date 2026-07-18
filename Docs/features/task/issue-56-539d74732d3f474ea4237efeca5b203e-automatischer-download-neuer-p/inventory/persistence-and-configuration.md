# Persistenz und Konfiguration

## Bestehendes Modell

`RezepteDbContext` exponiert `DbSet<PluginSetting> PluginSettings`. `PluginSetting` enthält aktuell den stabilen `PluginId`, Anzeige- und Assemblyinformationen, Aktivierung, Reihenfolge, Status, Fehlertext sowie Discovery-Zeitpunkte. Die Konfiguration wird in `RezepteDbContext` mit einem Schlüssel auf `PluginId` abgebildet.

Die Anwendung nutzt SQLite über `ServiceCollectionExtensions.AddDbContext` und wendet beim Start vorhandene EF-Core-Migrationen an. Migrationen liegen unter `Rezepte.Web/Migrations`.

## Fehlende Persistenz

Für die Anforderung fehlen mindestens:

- eine Quelle mit Repository-URL, Sichtbarkeit, Vertrauensbestätigung, Aktivierung und Besitzer-/Benutzerbezug;
- Release- bzw. Paketdaten mit ermittelter Version, Assetidentität, Download-/Validierungs-/Installationsstatus und Zeitpunkten;
- eine Zuordnung zwischen Quelle, Release und den daraus geladenen Pluginversionen;
- ein dauerhaftes Fehler- und Wiederholungsmodell, damit fehlerhafte Versionen nicht bei jedem Intervall erneut verarbeitet werden.

PAT-Werte dürfen nicht in `PluginSetting`, einer normalen Datenbankausgabe oder einem UI-Modell gespeichert werden. Die Anwendung braucht stattdessen eine serverseitige Secret-Referenz bzw. Konfigurationsoption und einen GitHub-Client, der das Secret nur im Backend liest.

## Konfigurationspunkte

`Rezepte.Web/appsettings.json` enthält die vorhandene Anwendungs- und Providerkonfiguration. Für Updateintervall, GitHub-API, Timeout/Retry und Secretnamen sollte ein typisiertes Optionsmodell ergänzt werden. Die Defaults müssen so gewählt werden, dass ein Updateprozess nicht unkontrolliert häufig läuft.

## Migrations- und Konsistenzbedarf

Neue Tabellen oder Spalten benötigen eine EF-Core-Migration. Statusänderungen sollten in einer Transaktion erfolgen, soweit Download/Dateisystem und Datenbank dies zulassen. Für den Dateiaustausch ist zusätzlich ein Wiederherstellungs- oder Backupzustand erforderlich, damit ein Fehler nach der Validierung den bisherigen Pluginbestand unverändert lässt.
