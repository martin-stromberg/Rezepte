# Tasks: Update-Prüfung je Pluginquelle in der Pluginverwaltung

| # | Bereich | Aufgabe | Status | Testnachweis |
|---|---------|---------|--------|--------------|
| 1 | Lokalisierung | Neue `UiStrings.resx`-Keys anlegen: `PluginSourcesInfo`, `CheckForUpdates`, `PluginCheckNoUpdate`, `PluginCheckInstalled`, `PluginCheckFailed`, `PluginCheckSourceNotFound`, `PluginCheckSourceDisabled`, `PluginCheckSourceNotTrusted`, `PluginCheckNoPat` | Offen | — |
| 2 | Lokalisierung | Tabellen-/Form-Keys anlegen: `GitHubPluginSources`, `RepositoryUrl`, `Private`, `Public`, `Active`, `Name`, `Assembly`, `Source`, `Status`, `Actions`, `SecretStored`, `SecretMissing`, `SourceLastErrorAt`, `EnableSourceFor`, `EnablePluginFor`, `Loading` (bestehenden `Cancel`-Key wiederverwenden) | Offen | — |
| 3 | Lokalisierung | Fehler-Keys anlegen: `PluginsLoadFailed`, `SourceSaveFailed`, `SourceDeleteFailed`, `PluginSaveFailed`, `OrderSaveFailed` | Offen | — |
| 4 | UI | `PluginSettings.razor`: Alert-Restrukturierung — `message`/`messageClass` innerhalb Haupt-`else` über den Sektionen (`ApplicationUpdates`-Muster) | Offen | — |
| 5 | UI | `PluginSettings.razor`: Erklärtext `Localizer["PluginSourcesInfo"]` unter der Quellen-Überschrift | Offen | — |
| 6 | UI | `PluginSettings.razor`: Aktions-Zelle → `btn-group` Icon-Buttons (`bi-arrow-repeat`, `bi-pencil`, `bi-trash`) mit lokalisiertem `title`/`aria-label` | Offen | — |
| 7 | UI | `PluginSettings.razor`: `isCheckingSourceId`-State + Busy-Spinner (`spinner-border`) + Zeilen-Sperrung | Offen | — |
| 8 | UI | `PluginSettings.razor`: `disabled`-Logik `!Enabled || !TrustConfirmed` + Tooltip-Grund via `CheckButtonTitle` | Offen | — |
| 9 | UI | `PluginSettings.razor`: `CheckSourceAsync` Click-Handler (Result-Mapping, Message nach `LoadAsync`) | Offen | — |
| 10 | UI | `PluginSettings.razor`: `MapCheckError`-Helper (bekannte englische `Error`-Strings → lokalisierte Keys, sonst Detail-Durchreichung) | Offen | — |
| 11 | UI | `PluginSettings.razor`: `LastErrorAt` in Status-Zelle ergänzen | Offen | — |
| 12 | UI | `PluginSettings.razor`: alle hartkodierten Strings lokalisieren (Labels, Tabellenköpfe, aria-labels, `@code`-Meldungen) — Hook `razor-l10n-check.py` | Offen | — |
| 13 | Tests | `Rezepte.Tests/Components/PluginSettingsComponentTests.cs` (bUnit): Info-Text, deaktivierter Button + Grund, Klick→Service-Aufruf, `Installed`/`NoUpdate`/`Failed`-Meldungen, Busy/Sperrung, Reload, Tooltips, `LastErrorAt` | Offen | — |
| 14 | E2E-Infrastruktur | E2E-Infrastruktur: `PluginSettingsAppFixture` anlegen (`DatabasePath` ist bereits public — kein Fixture-Umbau) | Offen | — |
| 15 | E2E-Infrastruktur | `GitHubStubServer` (HttpListener, `releases/latest` → konfigurierbare Antwort) | Offen | — |
| 16 | E2E-Infrastruktur | `PluginSettingsAppFixture` (Env-Override `PluginUpdates__GitHubApiBaseUrl`, `SeedInstalledReleaseAsync` via Sqlite) + `PluginSettingsTestCollection` | Offen | — |
| 17 | E2E-Infrastruktur | `PluginSettingsPageObject` (Login, Plugins-Tab, Quelle anlegen/togglen, Prüf-Button, Alert/Status abfragen) | Offen | — |
| 18 | E2E-Tests | `PluginSettingsBrowserTests`: Button sichtbar + Erklärtext; Klick→`alert-danger` (kein PAT); deaktivierte Quelle→Button disabled; `NoUpdate(alreadyInstalled)`→`alert-success` (Stub+Seed) | Offen | — |
| 19 | Verifikation | `dotnet build` + `dotnet test Rezepte.Tests` + Browser-Suite (Release/Chromium) + Pre-Commit-Hooks | Offen | — |
