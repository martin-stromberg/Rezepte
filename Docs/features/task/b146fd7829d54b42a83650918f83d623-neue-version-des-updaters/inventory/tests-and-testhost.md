# Tests und Testhost

## Updater-bezogene Tests

Die folgenden Testklassen sind für die Regression der bestehenden Integration relevant:

- `Rezepte.Tests/Services/ApplicationUpdateSettingsServiceTests.cs`: Statusabbildung, Check/Download/Install, Fehlermeldungen und Prerelease-Einstellungen.
- `Rezepte.Tests/Services/ApplicationUpdatePreInstallHandlerTests.cs`: Verhalten vor der Installation eines Anwendungsupdates.
- `Rezepte.Tests/Services/UpdateBackupServiceTests.cs`: Backup-Verhalten im Updateablauf.
- `Rezepte.Tests/Services/Import/PluginUpdateServiceTests.cs`: Plugin-Updateablauf; die Implementierung verwendet eigene Plugin-Abstraktionen, läuft aber im selben Web-Projekt mit der aktualisierten Paketabhängigkeit.

## Eigenständiger Testhost

`Rezepte.Updater.TestHost` ist ein ausführbares Projekt mit eigener `msTools.Updater`-Referenz. `Program.cs` konfiguriert die Modi `preflight`, `check`, `download`, `install` und `run`, registriert die benutzerdefinierten Implementierungen `ConfigurableAutoUpdateEnvironment` und `LoggingAutoUpdateProcessRunner` und verwendet die Paket-Orchestrierungs- und Service-APIs direkt. Die drei Hilfsklassen im Projekt müssen daher mindestens gebaut werden; die CLI-Modi sind der relevante manuelle bzw. automatisierte Smoke-Test.

## Erwartete Verifikation

1. Restore mit `NuGet.config` und der lokalen Quelle `lib/nuget`.
2. Build der Solution einschließlich `Rezepte.Web`, `Rezepte.Tests` und `Rezepte.Updater.TestHost`.
3. Ausführung der bestehenden Tests, mindestens der vier oben genannten Testklassen.
4. Prüfung, dass keine Referenz auf `0.7.0-rc.11` verbleibt und `0.10.0` tatsächlich aus der lokalen Quelle verwendet wird.
