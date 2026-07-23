# Tasks: Nutzbarkeitsprüfung der Plugins

| # | Bereich | Aufgabe | Status | Testnachweis |
|---|---------|---------|--------|--------------|
| 1 | Datenmodell | `PluginUsabilityIssue` Record in `Rezepte.Import.Abstractions` anlegen | Offen | — |
| 2 | Datenmodell | `PluginUsabilityResult` Record in `Rezepte.Import.Abstractions` anlegen (inkl. statischer „nutzbar"-Instanz) | Offen | — |
| 3 | Abstraktion | `IImportPlugin` um Default-Methode `CheckUsabilityAsync(IServiceProvider, CancellationToken)` erweitern | Offen | — |
| 4 | Datenmodell | `ImportPluginDescriptor` um Eigenschaft `PluginType` (`Type?`) erweitern | Offen | — |
| 5 | Logik | `PluginManager`-Descriptor-Erzeugung anpassen (`PluginType` in `DiscoverFromAssembly` setzen, `null` in übrigen Konstruktionen) | Offen | — |
| 6 | Logik | `IPluginManager` um `GetPluginsUsabilityAsync` (Default = leeres Dictionary) erweitern | Offen | — |
| 7 | Logik | `PluginManager.GetPluginsUsabilityAsync` implementieren (Live-Prüfung geladener Plugins mit Ausnahmebehandlung) | Offen | — |
| 8 | Datenmodell | `PluginSettingsItem` um Feld `Usability` (`PluginUsabilityResult?`) erweitern | Offen | — |
| 9 | Logik | `PluginSettingsService`-Konstruktor um `IPluginManager` und `IServiceProvider` erweitern | Offen | — |
| 10 | Logik | `PluginSettingsService.GetPluginsAsync` um Anreicherung mit Nutzbarkeitsergebnis erweitern | Offen | — |
| 11 | Plugin | `AIUrlImportPlugin.CheckUsabilityAsync` implementieren (globales KI, Gemini-Auth, globales Gemini) | Offen | — |
| 12 | Plugin | `AIFotoImportPlugin.CheckUsabilityAsync` implementieren (zusätzlich Vision-Service-Account, globales Vision) | Offen | — |
| 13 | UI | `PluginSettings.razor` um Nutzbarkeitsanzeige unterhalb des Status-Badges erweitern | Offen | — |
| 14 | Tests | Hilfstyp: Fake-Plugin mit steuerbarem `CheckUsabilityAsync` bereitstellen | Offen | — |
| 15 | Tests | Hilfstyp: Fake `IPluginManager` mit steuerbarem `GetPluginsUsabilityAsync` bereitstellen | Offen | — |
| 16 | Tests | `AIUrlImportPluginTests`: nutzbar bei vollständiger Konfiguration | Offen | — |
| 17 | Tests | `AIUrlImportPluginTests`: fehlende Gemini-Authentifizierung meldet Issue | Offen | — |
| 18 | Tests | `AIUrlImportPluginTests`: deaktivierter globaler Gemini-Schalter meldet Issue | Offen | — |
| 19 | Tests | `AIFotoImportPluginTests`: nutzbar bei vollständiger Konfiguration | Offen | — |
| 20 | Tests | `AIFotoImportPluginTests`: fehlende Vision-Service-Account-Datei meldet Issue | Offen | — |
| 21 | Tests | `AIFotoImportPluginTests`: deaktiviertes globales Google Vision meldet Issue | Offen | — |
| 22 | Tests | `PluginManagerTests`: Plugin ohne Override gilt als nutzbar | Offen | — |
| 23 | Tests | `PluginManagerTests`: `GetPluginsUsabilityAsync` liefert Ergebnisse je geladenem Plugin | Offen | — |
| 24 | Tests | `PluginManagerTests`: Ausnahme in `CheckUsabilityAsync` → nicht nutzbar, kein Abbruch | Offen | — |
| 25 | Tests | `PluginSettingsServiceTests`: `GetPluginsAsync` befüllt `Usability` für geladene Plugins | Offen | — |
| 26 | Tests | Bestehende `PluginSettingsServiceTests` an neuen Konstruktor anpassen | Offen | — |
| 27 | Tests | Bestehende `PluginManagerTests` an neuen `PluginType`-Parameter anpassen (falls Descriptor direkt konstruiert) | Offen | — |
| 28 | E2E-Tests | `PluginSettingsServiceTests`: nicht nutzbares KI-Plugin liefert Nutzbarkeitsstatus mit Fehlerursache und Hinweis (Happy Path) | Offen | — |
| 29 | E2E-Tests | `PluginSettingsServiceTests`: vollständig konfiguriertes Plugin ist nutzbar (keine Issues) | Offen | — |
