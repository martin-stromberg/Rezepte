# Tasks: Entfernen-Aktion für vermisste Plugins (Schritt 5, Issue #179)

| # | Bereich | Aufgabe | Status | Testnachweis |
|---|---------|---------|--------|--------------|
| 1 | Lokalisierung | resx-Keys `RemovePluginFor`, `PluginRemoved`, `PluginRemoveFailed` in `UiStrings.resx` anlegen | Erledigt | Indirekt über bUnit-Tooltip-/Alert-Tests |
| 2 | UI | Entfernen-Symbolbutton (`bi-trash`, `btn-outline-danger`, Wrapper-`span title` + `title` + `aria-label`) in der Plugin-Aktionszelle nur für `Status == PluginStatus.Missing` rendern; Spaltenbreite 112px → 160px | Erledigt | `RemoveButton_OnlyForMissingStatus`, `RemoveButton_HasLocalizedTooltipAndAriaLabel` |
| 3 | UI | `RemovePluginEntryAsync(PluginSettingsItem)`-Handler in `PluginSettings.razor` (Guard `Missing`/`isSaving`, `RemovePluginAsync`-Aufruf, Success-/Danger-Alert, `LoadAsync()` bei Erfolg) | Erledigt | `RemovePluginAsync_CallsService_AndReloads`, `RemovePluginAsync_WhenServiceThrows_ShowsDangerAlert` |
| 4 | UI | `RemoveButtonTitle(plugin)`-Helper (Busy → `PluginCheckBusyTooltip`, sonst `RemovePluginFor` mit DisplayName) | Erledigt | `RemoveButton_HasLocalizedTooltipAndAriaLabel` |
| 5 | Tests | `WithPlugins(...)`-Helper + `Plugin(...)`-Fabrik in `PluginSettingsComponentTests` | Erledigt | Wird von allen neuen Facts genutzt |
| 6 | Tests | bUnit `RemoveButton_OnlyForMissingStatus` | Erledigt | Bestanden (37/37) |
| 7 | Tests | bUnit `RemoveButton_HasLocalizedTooltipAndAriaLabel` | Erledigt | Bestanden (37/37) |
| 8 | Tests | bUnit `RemovePluginAsync_CallsService_AndReloads` (Service-Verify, Reload, Success-Alert, Zeile weg) | Erledigt | Bestanden (37/37) |
| 9 | Tests | bUnit `RemovePluginAsync_WhenServiceThrows_ShowsDangerAlert` | Erledigt | Bestanden (37/37) |
| 10 | E2E-Tests | `SeedMissingPluginAsync` in `PluginSettingsAppFixture` (SQLite-INSERT in `PluginSettings`, `Status='Missing'`) | Erledigt | `MissingPlugin_CanBeRemoved` |
| 11 | E2E-Tests | `PluginSettingsPageObject` um `PluginRow`/`RemovePluginRowAsync`/`IsPluginRowVisibleAsync` erweitern | Erledigt | `MissingPlugin_CanBeRemoved` |
| 12 | E2E-Tests | `SkippableFact MissingPlugin_CanBeRemoved` in `PluginSettingsBrowserTests` (Seed → Panel → Entfernen → Zeile weg + Success-Alert) | Erledigt | Bestanden (5/5) |
