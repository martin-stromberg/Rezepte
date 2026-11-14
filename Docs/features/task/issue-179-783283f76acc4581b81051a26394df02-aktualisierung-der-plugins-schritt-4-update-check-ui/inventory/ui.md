# UI: PluginSettings, Lokalisierung, Icons, Feedback-Muster

## `PluginSettings` (Razor-Komponente)

Datei: `Rezepte.Web/Components/Settings/PluginSettings.razor` (369 Zeilen)

- Eingebunden als `DynamicComponent` in `Rezepte.Web/Components/Pages/Settings.razor:35-37`; der Eintrag ist Admin-sichtbar (`SettingsViewModel.Item` mit `isAdmin`, `Settings.razor:67`). Seite: `@page "/settings"`, `@rendermode InteractiveServer`, `[Authorize]`.
- Injiziert: `IPluginSettingsService PluginSettingsService`, `IStringLocalizer<UiStrings> Localizer` (Z. 2-3).
- Zustand: `plugins`, `sources`, `sourceForm`, `isLoading`, `isSaving`, `message` (Z. 197-202). `isSaving` sperrt global alle Aktionen; `message` wird nur als `alert alert-danger` angezeigt (Z. 9-12) — keine Erfolgs-/Info-Klasse wie in `ApplicationUpdates.razor`.

### Quellen-Sektion (Z. 15-114)

- Überschrift `GitHub-Pluginquellen` (Z. 16, hartkodiert) — hier fehlt der geforderte Erklärtext zum Prüfprinzip.
- `EditForm` zum Anlegen/Bearbeiten inkl. `TrustConfirmed`-Checkbox (Z. 40-43, gesperrt bei vorhandener `Id`).
- Quellen-Tabelle (Z. 61-113), Spalten: `Aktiv` (Switch, `aria-label` Z. 81), `Quelle`, `Status`, `Aktionen`.
- Status-Zelle (Z. 90-103): `LastSuccessfulReleaseTag` als `badge text-bg-success`, `LastError` als `text-danger`, `LastCheckedAt` als `Geprüft: …` (ToLocalTime, `"g"`) — Beschriftungen hartkodiert Deutsch. `LastErrorAt` wird nicht angezeigt.
- Aktions-Zelle (Z. 104-107): Textbuttons `Bearbeiten` → `EditSource(source)`, `Löschen` → `DeleteSourceAsync(source.Id)`, beide `disabled="@isSaving"` — noch keine Symbolbuttons/Icons.
- Kein Aufruf von `CheckSourceForUpdatesAsync`, kein Busy-Zustand je Quelle.

### Plugin-Tabelle (Z. 116-193)

- Lokale Plugins mit Aktiv-Switch, Status-Badge (`StatusClass`, Z. 351-358), Usability-Hinweisen, Reihenfolge-Buttons (`↑`/`↓` mit `title="@Localizer["MoveUp|Down"]"`, Z. 183-186 — Text-Pfeile statt `bi`-Icons, nur Tooltip, kein `aria-label`).

### Code-Behind-Methoden

`LoadAsync` (lädt `GetPluginsAsync` + `GetSourcesAsync`), `SaveSourceAsync`, `SetSourceEnabledAsync`, `DeleteSourceAsync`, `EditSource`, `ResetSourceForm`, `SetEnabledAsync`, `MoveAsync`, `StatusClass`; alle setzen `isSaving`/`message` und rufen danach `LoadAsync` — d. h. ein Reload nach dem Check aktualisiert die Status-Spalten automatisch.

## Lokalisierung

- Marker-Klasse `Rezepte.Web.Resources.UiStrings` (`Rezepte.Web/Resources/UiStrings.cs`), Resource `Rezepte.Web/Resources/UiStrings.resx` — einzige Resx, 219 `<data>`-Einträge, deutsche Werte, keine Satelliten-Dateien (kein `.en.resx` o. ä.).
- Registrierung: `builder.Services.AddLocalization()` (`Program.cs:57`); kein `UseRequestLocalization`/Culture-Setup gefunden → effektiv einsprachig Deutsch.
- Nutzung: `@using Rezepte.Web.Resources` steht in `Rezepte.Web/Components/_Imports.razor`; Komponenten injizieren `IStringLocalizer<UiStrings>`.
- Vorhandene, wiederverwendbare Schlüssel: `Edit` („Bearbeiten"), `Delete` („Löschen"), `CheckNow` („Jetzt prüfen"), `LastChecked` („Zuletzt geprüft"), `UpdatePat`, `TrustThisSource`, `NoGitHubPluginSources`, `NotUsable`, `PluginOrder`, `MoveUp`, `MoveDown`, `NoImportPluginsFound`, `LocalImportPlugins`, `Plugins`, `Updates` (UiStrings.resx Z. 29-30, 139-140, 157-165).
- `PluginSettings.razor` ist nur teilweise lokalisiert — viele Strings (Tabellenköpfe, „Aktiv", „Geprüft", „Privat/Öffentlich", Fehlermeldungen im `@code`) sind hartkodiert.

## Bootstrap-Icons

- Einbindung: `App.razor:20` — CDN `bootstrap-icons@1.11.3`.
- Repo-Muster: `<i class="bi bi-box-seam me-1"></i>` (`BackupRestore.razor:13`, `bi-arrow-clockwise` Z. 30, `bi-download` in `ExportData.razor:22`), `<span class="bi bi-gear-fill" aria-hidden="true"></span>` (`MainLayout.razor:54`), Tooltip via `title`-Attribut (`Cookbooks.razor:75-79` Drag-Handle `bi-arrows-move` + `title="@Localizer["Move"]"`).
- Gängige verfügbare Icons für die Aktionen: `bi-arrow-repeat`/`bi-cloud-download`/`bi-search` (prüfen), `bi-pencil` (bearbeiten), `bi-trash` (löschen) — Auswahl ist Gestaltungsdetail.

## Feedback-/Busy-Muster

- Kein Toast-Framework im Repo; Standard ist ein inline `alert`-Div.
- Direktes Vorbild `ApplicationUpdates.razor`: `isBusy`-Flag sperrt Aktionsbuttons (Z. 43-46), `message` + `messageClass` (`alert alert-success|warning|danger py-2`) über `RunCommandAsync(label, action)` (Z. 126-150), Status wird nach dem Lauf neu geladen.
- Busy-Indikator: `<span class="spinner-border spinner-border-sm" role="status" aria-hidden="true"></span>` innerhalb von Buttons (`CreateRecipeDialog.razor:22,44,67`).
