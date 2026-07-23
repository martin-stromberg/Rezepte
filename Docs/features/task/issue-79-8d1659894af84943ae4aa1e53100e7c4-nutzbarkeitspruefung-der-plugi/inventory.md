# Bestandsaufnahme: Nutzbarkeitsprüfung der Plugins

Diese Analyse dokumentiert den aktuellen Zustand der Plugin-Infrastruktur bezogen auf die Anforderung zur Implementierung einer Nutzbarkeitsprüfung (Usability Check) für Import-Plugins.

## Zusammenfassung

Der Projektcode enthält eine etablierte Plugin-Verwaltungsinfrastruktur mit Plugin-Discovery, -Loading und Verwalten von Plugin-Settings. Die Infrastruktur verfügt über:

- **Vorhanden:**
  - Plugin-Interface `IImportPlugin` und Handler-Interface `IImportHandler`
  - Umfassende Plugin-Discovery- und Loading-Logik in `PluginManager`
  - Entity `PluginSetting` mit Status- und Error-Feldern für Plugin-Zustände
  - Service `PluginSettingsService` zur Verwaltung von Plugin-Einstellungen
  - Razor-Komponente `PluginSettings.razor` zur Anzeige von Plugins in der Admin-UI
  - Enum `PluginStatus` mit verschiedenen Plugin-Zustandskategorien
  - Umfangreiche Unit- und Integrationstests für Plugin-Manager und Settings

- **Nicht vorhanden:**
  - Nutzbarkeitsprüfungs-Interface oder -Struktur (z. B. `PluginUsabilityResult`, `GetUsabilityAsync()`)
  - Methode zur Nutzbarkeits-Prüfung in `IImportPlugin` oder `IImportHandler`
  - Service für zentrale Nutzbarkeitsverwaltung (z. B. `IPluginUsabilityService`)
  - Persistierung von Nutzbarkeitsinformationen in `PluginSetting` (z. B. `UsabilityStatus`, `UsabilityErrors`, `UsabilityHints`)
  - DTO-Erweiterungen in `PluginSettingsItem` für Nutzbarkeitsfelder
  - UI-Darstellung von Nutzbarkeitsinformationen in der Admin-Komponente
  - Tests für Nutzbarkeitsprüfung

## Details

- [Interfaces und Abstraktion](inventory/interfaces.md)
- [Datenmodelle und Entities](inventory/models.md)
- [Services und Manager](inventory/logic.md)
- [Enums](inventory/enums.md)
- [Tests](inventory/tests.md)
