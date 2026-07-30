# Konfiguration, appsettings und Settings-Klassen

## appsettings.json

`Rezepte.Web/appsettings.json` enthaelt aktuell Sections fuer:

- `Logging`
- `AllowedHosts`
- `Jwt`
- `Images`
- `PluginUpdates`
- `GoogleCredentials`
- `LoadingBar`

`Rezepte.Web/appsettings.Development.json` ueberschreibt nur Logging.

## Options-Konvention

Technische Konfiguration wird in `AddRezepteServices` mit `services.Configure<TOptions>(configuration.GetSection("..."))` registriert. Options-Klassen liegen unter `Rezepte.Web/Configuration`.

Vorhandene Beispiele:

- `ImageOptions`
- `AIOptions`
- `PluginUpdateOptions`
- `GoogleCredentialsOptions`
- `LoadingBarOptions`
- `LoadingBarSettings`

Die Klassen sind einfache `sealed class`-Optionstypen mit Properties und Defaults. Es gibt keine zentrale Options-Validation-Konvention, einzelne Services validieren bei Nutzung.

## DB-basierte AppSettings

Zusaetzlich existiert `Rezepte.Web/Entities/AppSetting` mit Key/Value und `SettingsService`. Diese Werte liegen in der Datenbank und werden fuer globale Laufzeit-Schalter verwendet:

- AI global aktiv
- Google Vision global aktiv
- Gemini global aktiv
- Request-Limits
- Disable-on-limit
- Shopping-List-Edit-Modus pro User als Key-Prefix

Diese DB-Settings sind nicht identisch mit `appsettings.json`.

## Konsequenz fuer Update-Backups

Die Anforderung spricht von `appSettings`. Im bestehenden Code gibt es zwei Konzepte:

- JSON-Konfiguration via `appsettings.json` und Options-Klassen
- DB-Tabelle `AppSettings`

Fuer Backup-Pfad und Retention ist die JSON-/Options-Variante naheliegender, weil:

- Pfade und technische Update-Parameter bereits beim Startup/Updater relevant sind.
- Sie ohne Datenbankzugriff und ohne Admin-UI verfuegbar sind.
- Die bestehende `PluginUpdates`-Konfiguration ebenfalls ueber `appsettings.json` laeuft.

Eine moegliche neue Section:

```json
"Updater": {
  "Backups": {
    "Directory": "update-backups",
    "RetentionCount": 5,
    "IncludeImages": true,
    "IncludePdf": false
  }
}
```

Alternativ:

```json
"UpdateBackups": {
  "Directory": "update-backups",
  "RetentionCount": 5
}
```

Die konkrete Benennung sollte in der Planung festgelegt werden. Wichtig ist eine eigene Options-Klasse, damit die Konfiguration typsicher auslesbar bleibt.

## Validierungsempfehlungen

- `Directory` darf nicht leer sein.
- Relative Pfade sollten gegen `IHostEnvironment.ContentRootPath` aufgeloest werden.
- Vollpfade sollten erlaubt sein, aber mit Logging sichtbar gemacht werden.
- `RetentionCount` sollte `>= 1` sein, wenn nach erfolgreichem Backup nicht sofort geloescht werden soll.
- Fehlerhafte Konfiguration sollte die Installation verhindern, nicht stillschweigend in ein Default-Verzeichnis schreiben, wenn dadurch ein Backup verloren gehen koennte.
