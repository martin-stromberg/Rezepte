# Abnahmeprüfung – Entwicklungsschritt 1

## Ergebnis

**Status:** Anforderung vollständig erfüllt

## Abweichungen

Keine.

## Hinweise

- **Versionsverzeichnisse statt In-place-Ersetzung:** `PluginPackageInstaller.InstallWithReloadTrackingAsync` (`Rezepte.Web/Services/Import/Plugins/PluginPackageInstaller.cs:40-49`) bildet Zielverzeichnisse als `<Basisname>@<ReleaseTag>` über `PluginDirectoryRegistry.CreateVersionedDirectoryName` (`PluginDirectoryRegistry.cs:72-91`); ungültige Zeichen und `@` im Release-Tag werden zu `-` ersetzt. Das Namensschema ist damit festgelegt und in `IPluginDirectoryRegistry` sowie der Benutzerdokumentation dokumentiert.
- **Entfernung der Vorgängerverzeichnisse beim Austausch:** `UpdateInstallState` (`PluginPackageInstaller.cs:137-177`) markiert sämtliche älteren Versionsverzeichnisse desselben Basisnamens sowie das bisher registrierte Verzeichnis als zu löschen; `CleanupMarkedDirectories` (`PluginDirectoryRegistry.cs:209-253`) entfernt sie noch im selben koordinierten Austausch (`PluginManager.InitializeCoreAsync` → `CleanupPendingDirectories`, `PluginManager.cs:106-119`).
- **Persistente Löschmarkierung:** Die Markierung liegt in `plugins/.install-state.json` (`PluginInstallState.PendingDeletes`), wird atomar über Temp-Datei + Move gespeichert (`PluginDirectoryRegistry.cs:59-69`) und übersteht Anwendungsneustarts. Nicht löschbare Verzeichnisse verbleiben in `PendingDeletes` (Zeilen 241-249).
- **Erkennungsausschluss:** `PluginManager.GetPluginDirectories` (`PluginManager.cs:276-287`) schließt für das verwaltete Stammverzeichnis alle von `GetExcludedDirectoryNames` (`PluginDirectoryRegistry.cs:115-175`) gelieferten Namen aus: als zu löschen markierte Verzeichnisse, nicht wirksame `name@tag`-Verzeichnisse und flache Verzeichnisse, die von einem gleichnamigen Versionsverzeichnis beschattet werden.
- **Bereinigung beim Start / beim Austausch:** `CleanupPendingDirectories` läuft am Ende jeder Initialisierung (Anwendungsstart über `PluginStartupService` sowie jeder koordinierte Austausch); zusätzlich räumt `ReplaceDirectories` (`PluginPackageInstaller.cs:97`) ausstehende Löschungen bereits vor dem Kopieren der neuen Verzeichnisse.
- **Missing-Status:** `MarkDroppedSourceEntries` (`PluginPackageInstaller.cs:179-196`) entfernt Install-Einträge derselben Quelle (`SourceKey`), die im neuen Release fehlen, und markiert ihre Verzeichnisse zur Löschung; die bestehende Synchronisation in `SynchronizeSettingsAsync` (`PluginManager.cs:468-473`) setzt nicht mehr erkannte Plugins auf `PluginStatus.Missing`.
- **Rollback/Fehlerverhalten:** `RollBackAsync` (`PluginPackageInstaller.cs:198-241`) stellt den vorherigen State-Snapshot wieder her, spielt Backups registrierter Verzeichnisse zurück (Same-Tag-Reinstallation), markiert bzw. entfernt teilinstallierte neue Zielverzeichnisse und erhält die bisherige Statusunterscheidung `InstallFailed`/`ReloadFailed` über `PluginPackageInstallException`.
- **Ausnahme der Publish-Plugins:** Nur `{ContentRootPath}/plugins` ist verwaltetes Stammverzeichnis (`IsManagedRoot`, `PluginDirectoryRegistry.cs:25-33`); der Publish-Plugin-Root `{AppContext.BaseDirectory}/plugins` (`PluginManager.GetPluginRoots`, `PluginManager.cs:289-301`) wird weder versioniert noch gefiltert oder gelöscht. Flache Verzeichnisse werden grundsätzlich nie gelöscht.
- Die Markierung zum Löschen erfolgt implementierungsseitig bereits vor dem Löschversuch (Altverzeichnisse werden zunächst als `PendingDelete` erfasst und anschließend best-effort entfernt); das Ergebnis entspricht der Anforderung, da nicht löschbare Verzeichnisse dauerhaft markiert bleiben.
- `InstallWithReloadTrackingAsync` lädt den Install-State zweimal hintereinander (`PluginPackageInstaller.cs:59-60`) – funktional unkritische Redundanz.
- Verifikation: `dotnet test` für `PluginDirectoryRegistryTests*`, `PluginPackageInstallerTests`, `PluginManagerTests`, `PluginUpdateServiceTests` – 73 Tests, alle bestanden.
- Dokumentation (`Docs/help/import-plugins.md`, `Docs/RELEASE_NOTES.md`) sowie `.gitignore` (`/Rezepte.Web/plugins/`) wurden konsistent nachgezogen.
