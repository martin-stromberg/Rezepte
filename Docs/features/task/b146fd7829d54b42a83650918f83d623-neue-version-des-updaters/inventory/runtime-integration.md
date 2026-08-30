# Laufzeitintegration

## Web-Anwendung

Die Web-Anwendung bindet `msTools.Updater` in folgenden Bereichen ein:

- `Rezepte.Web/Program.cs`: Registrierung und Konfiguration der Auto-Update-Dienste.
- `Rezepte.Web/Extensions/ServiceCollectionExtensions.cs`: Registrierung von `ApplicationUpdateHostedService` und den anwendungsinternen Update-Services.
- `Rezepte.Web/Controllers/UpdatesController.cs`: Auflösung des Auto-Update-Service und Nutzung von `AutoUpdateOptions`/`AutoUpdateInstallationTarget`.
- `Rezepte.Web/Services/Updates/ApplicationUpdateHostedService.cs`: Event-Aggregator, Pre-Install-Handler und Updater-Ereignisse.
- `Rezepte.Web/Services/Updates/ApplicationUpdateSettingsService.cs`: Status-, Check-, Download- und Install-Kommandos sowie `AutoUpdateResult` und zugehörige Ergebnis-/Status-Typen.

Die UI-Komponente `Rezepte.Web/Components/Settings/ApplicationUpdates.razor` spricht gegen die anwendungsinterne Abstraktion `IApplicationUpdateSettingsService`; eine direkte Paketabhängigkeit der Komponente ist nicht erkennbar.

## Konfiguration

`Rezepte.Web/Configuration/ApplicationUpdateOptions.cs` beschreibt bestehende Optionen für automatische Updates, Hintergrunddienste, Health-/Lock-Timeout und die Update-Einheit. Die Anforderung fordert keine neue Konfiguration. Die Kompatibilitätsprüfung muss daher klären, ob `0.10.0` die aktuell verwendeten Options- und Service-APIs unverändert anbietet.

## Änderungsrisiko

Das Risiko liegt in möglichen API- oder Verhaltensänderungen zwischen der Release-Candidate-Version und `0.10.0`. Besonders relevant sind `UseAutoUpdate`, `IAutoUpdateServiceResolver`, `IAutoUpdateEventAggregator`, `IAutoUpdateCommandHandler`, `IAutoUpdateStatusProvider`, `IAutoUpdateOrchestrator`, `IAutoUpdateEnvironment`, `IAutoUpdateProcessRunner` sowie die `AutoUpdate*`-Ergebnis- und Konfigurationstypen. Der Bestand enthält keine Hinweise auf eine notwendige fachliche Anpassung; Compilerfehler und Tests sind der erste Kompatibilitätsnachweis.
