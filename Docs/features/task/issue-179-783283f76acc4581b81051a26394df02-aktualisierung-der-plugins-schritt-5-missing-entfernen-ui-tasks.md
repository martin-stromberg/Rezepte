# Tasks: Entfernen-Aktion für vermisste Plugins (Schritt 5, Issue #179)

| # | Bereich | Aufgabe | Status | Testnachweis |
|---|---------|---------|--------|--------------|
| 1 | Lokalisierung | resx-Keys `RemovePluginFor`, `PluginRemoved`, `PluginRemoveFailed` in `UiStrings.resx` anlegen | Offen | — |
| 2 | UI | Entfernen-Symbolbutton (`bi-trash`, `btn-outline-danger`, Wrapper-`span title` + `title` + `aria-label`) in der Plugin-Aktionszelle nur für `Status == PluginStatus.Missing` rendern; Spaltenbreite 112px → 160px | Offen | — |
| 3 | UI | `RemovePluginEntryAsync(PluginSettingsItem)`-Handler in `PluginSettings.razor` (Guard `Missing`/`isSaving`, `RemovePluginAsync`-Aufruf, Success-/Danger-Alert, `LoadAsync()` bei Erfolg) | Offen | — |
| 4 | UI | `RemoveButtonTitle(plugin)`-Helper (Busy → `PluginCheckBusyTooltip`, sonst `RemovePluginFor` mit DisplayName) | Offen | — |
| 5 | Tests | `WithPlugins(...)`-Helper + `Plugin(...)`-Fabrik in `PluginSettingsComponentTests` | Offen | — |
| 6 | Tests | bUnit `RemoveButton_OnlyForMissingStatus` | Offen | — |
| 7 | Tests | bUnit `RemoveButton_HasLocalizedTooltipAndAriaLabel` | Offen | — |
| 8 | Tests | bUnit `RemovePluginAsync_CallsService_AndReloads` (Service-Verify, Reload, Success-Alert, Zeile weg) | Offen | — |
| 9 | Tests | bUnit `RemovePluginAsync_WhenServiceThrows_ShowsDangerAlert` | Offen | — |
| 10 | E2E-Tests | `SeedMissingPluginAsync` in `PluginSettingsAppFixture` (SQLite-INSERT in `PluginSettings`, `Status='Missing'`) | Offen | — |
| 11 | E2E-Tests | `PluginSettingsPageObject` um `PluginRow`/`RemovePluginRowAsync`/`IsPluginRowVisibleAsync` erweitern | Offen | — |
| 12 | E2E-Tests | `SkippableFact MissingPlugin_CanBeRemoved` in `PluginSettingsBrowserTests` (Seed → Panel → Entfernen → Zeile weg + Success-Alert) | Offen | — |
