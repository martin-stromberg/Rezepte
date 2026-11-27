# Tasks: Fehler beim Aktualisieren von Plugins (Issue #239)

| # | Bereich | Aufgabe | Status | Testnachweis |
|---|---------|---------|--------|--------------|
| 1 | Fehlermodell | Konstante `WorkDirectoryUnavailable` in `PluginUpdateCheckErrors` ergänzen | Offen | — |
| 2 | Fehlermodell | `PluginWorkspaceException` anlegen (direkt von `Exception`, `Message` = `PluginUpdateCheckErrors.WorkDirectoryUnavailable`) | Offen | — |
| 3 | Logik | Interface `IPluginWorkspaceProvider` mit `CreateWorkDirectory(purpose)` anlegen | Offen | — |
| 4 | Logik | `PluginWorkspaceProvider` implementieren (Kandidaten `WorkDirectory` → `Path.GetTempPath()` → `{ContentRoot}/plugins/.work`, Beschreibbarkeitsprobe, Warnlog, `Func<string>`-Testnaht) | Offen | — |
| 5 | Logik | `PluginUpdateService` um Konstruktorparameter `IPluginWorkspaceProvider` erweitern und `workRoot` über den Provider beschaffen | Offen | — |
| 6 | Logik | `PluginPackageValidator` um Konstruktorparameter `IPluginWorkspaceProvider` erweitern und `extractRoot` über den Provider beschaffen | Offen | — |
| 7 | Logik | `PluginPackageInstaller` um Konstruktorparameter `IPluginWorkspaceProvider` erweitern und `backupPath` über den Provider beschaffen | Offen | — |
| 8 | Konfiguration | `PluginUpdateOptions.WorkDirectory` (`string?`, Standard `null`) ergänzen | Offen | — |
| 9 | Konfiguration | `IPluginWorkspaceProvider` als Singleton in `ServiceCollectionExtensions` registrieren | Offen | — |
| 10 | UI | Neuen `case PluginUpdateCheckErrors.WorkDirectoryUnavailable` in `PluginSettings.razor` (`MapCheckError`) ergänzen | Offen | — |
| 11 | UI | Lokalisierungseintrag `PluginCheckWorkDirectoryUnavailable` in `UiStrings.resx` ergänzen | Offen | — |
| 12 | Tests | `PluginWorkspaceProviderTests` anlegen (Konfig-Pfad, Temp-Default, Temp-Ausfall → Fallback, Totalausfall → Exception mit Konstante, Pfadstruktur `<basis>/<purpose>/<guid>`) | Offen | — |
| 13 | Tests | `PluginUpdateServiceTestsBase.CreateSut` um Workspace-Provider-Fake (`FakeWorkspaceProvider`/`ThrowingWorkspaceProvider`) erweitern | Offen | — |
| 14 | Tests | `PluginUpdateServiceTests_CheckSource`-Test: Workspace-Fehler → `Failed` mit `WorkDirectoryUnavailable` und `LastError` = Konstante | Offen | — |
| 15 | Tests | `PluginPackageValidatorTests`: Konstruktoraufrufe auf Provider-Fake umstellen; neuer Test `PluginWorkspaceException` propagiert | Offen | — |
| 16 | Tests | `PluginPackageInstallerTests.CreateInstaller` auf Provider-Fake umstellen; `InstallAsync_ShouldRemovePartialBackup_WhenBackupCopyFails` auf provider-basierten Backup-Root anpassen; neuer Test Backup-Workspace-Fehler → `PluginPackageInstallException` (`InstallFailed`, Konstante) | Offen | — |
| 17 | Tests | `PluginSettingsComponentTests`-Test: `Failed(WorkDirectoryUnavailable)` → lokalisierte Alert-Meldung | Offen | — |
| 18 | Tests | `PluginManagerTests.InitializeAsync_ShouldIgnoreWorkDirectory`: `{PluginRoot}/.work` mit Purpose/GUID-Resten (inkl. Fixture-DLL ab Tiefe 2) erzeugt keinen Discovery-Eintrag; ergänzend `[InlineData(".work")]` in `PluginDirectoryRegistryTests_Names.TryParseVersionedDirectoryName_ShouldReturnFalse_WhenNameIsNotVersioned` | Offen | — |
| 19 | E2E-Tests | `GitHubStubServer` um Asset-Download-Endpunkt (ZIP-Binär-Body) erweitern | Offen | — |
| 20 | E2E-Tests | `Rezepte.Tests.Browser`: `ProjectReference` auf `Rezepte.Tests.PluginFixture` ergänzen (echte Plugin-DLL für das Update-ZIP) | Offen | — |
| 21 | E2E-Tests | `PluginWorkspaceAppFixture` anlegen (Sperrdatei + `TMP`/`TEMP`/`TMPDIR`-Overrides auf blockierten Pfad; verifizieren, dass App damit startet) | Offen | — |
| 22 | E2E-Tests | `PluginWorkspaceBrowserTests`: defektes System-Temp → Update-Check installiert Release erfolgreich über Fallback | Offen | — |
| 23 | E2E-Tests | `PluginWorkspaceBrowserTests`: alle Kandidaten defekt → lokalisierte Fehlermeldung im Alert (Soll) | Offen | — |
