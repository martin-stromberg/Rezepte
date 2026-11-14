# Plan-Gegenprüfung

## Ergebnis

**Status:** Plan lückenhaft

Begründung: Die fachliche Abdeckung (Akzeptanzkriterien, Scope-Klärung, Akzeptanzszenarien, E2E-Abdeckung) ist vollständig und sauber verifizierbar. Der Plan enthält jedoch drei faktisch falsche bzw. inkonsistente Angaben, die bei wörtlicher Umsetzung zu Build-/Kompilierfehlern führen (doppelter resx-Key `Cancel`, bereits vorhandene Fixture-Property `DatabasePath`) bzw. das selbst gesetzte Ziel der vollständigen Lokalisierung verfehlen (fehlende Keys für `Name`/`Assembly`).

## Abgleich Akzeptanzkriterien

| Akzeptanzkriterium | Umsetzung im Plan | Testnachweis im Plan | Status |
|--------------------|-------------------|----------------------|--------|
| Manueller Update-Check je Quelle (`CheckSourceForUpdatesAsync` je Zeile) | `CheckSourceAsync` + Prüf-Button `bi-arrow-repeat` (plan.md Programmablauf, Änderungen Markup) | bUnit `CheckSourceAsync_CallsService_AndShowsInstalled`, `...ShowsNoUpdate`, `...ReloadsSources`; E2E Szenarien 2+4 | Abgedeckt |
| Erklärtext zum Prüfprinzip in der Quellen-Sektion | `PluginSourcesInfo` als `text-muted small` unter Überschrift | bUnit `InfoText_IsRendered`; E2E Szenario 1 | Abgedeckt |
| Ergebnis-Rückmeldung `NoUpdate`/`Installed`/`Failed` inline als Alert | `message`/`messageClass` im `ApplicationUpdates`-Muster; Alert in Haupt-`else` verschoben | bUnit `Installed`/`NoUpdate`/`Failed`; E2E `NoUpdate` (Stub+Seed) und `Failed` (kein PAT) | Abgedeckt (s. Hinweis `Installed`) |
| Vorbedingung `Enabled && TrustConfirmed` → Button deaktiviert + Grund | `disabled`-Logik + `CheckButtonTitle`-Tooltip | bUnit `CheckButton_Disabled_...`; E2E Szenario 3 | Abgedeckt |
| Busy-/Sperrzustand während eigenem Lauf | `isCheckingSourceId` + `spinner-border` + Zeilen-Sperrung | bUnit `CheckSourceAsync_SetsBusy_AndDisablesRow` (E2E bewusst nicht, Timing — begründet) | Abgedeckt |
| Icon-Buttons mit lokalisiertem Tooltip/`aria-label` für alle Quellen-Aktionen | `btn-group btn-group-sm` mit `bi-arrow-repeat`/`bi-pencil`/`bi-trash` | bUnit `ActionButtons_HaveLocalizedTooltipAndAriaLabel`; E2E Szenario 1 | Abgedeckt |
| Status-Spalte aktualisiert sich (`LastCheckedAt`, `LastSuccessfulReleaseTag`, `LastError`) | Reload via `LoadAsync()` im `finally`, Message erst nach Reload gesetzt (Reihenfolge korrekt erkannt, plan.md Z. 50) | bUnit `...ReloadsSources`; E2E Szenario 2 prüft Status-Zelle | Abgedeckt |
| Alle neuen Texte via `UiStrings.resx` lokalisiert | Neue-Keys-Tabelle + Komplett-Lokalisierung der Datei | Hook `razor-l10n-check.py` beim Commit; bUnit-Asserts auf deutsche Texte | Lücke im Detail: `Cancel` fälschlich als neu deklariert (existiert bereits, resx:88); Keys für Plugin-Tabellenköpfe `Name`/`Assembly` fehlen |
| Scope-Ausschlüsse (kein „Alle prüfen", keine Start-/Intervall-Änderung, kein Toast, kein Schritt-5-UI, Busy nur eigener Lauf) | Ausschluss-Tabelle mit Begründung + Status `offen` | — | Abgedeckt |

Verifizierte Faktenbasis:

- `PluginSettings.razor`: `else if (message)` (Z. 9-12) blendet tatsächlich den kompletten Inhalt aus → Alert-Umzug in den Haupt-`else`-Zweig ist korrekt geplant. `message`-Reset am Anfang von `LoadAsync` (Z. 212) → Ergebnismeldung erst nach `LoadAsync()` setzen ist zwingend richtig erkannt.
- `Enabled && TrustConfirmed` entspricht den Server-Vorbedingungen (`PluginUpdateService.cs:60-87`); Fehler-Strings stimmen: „Plugin source was not found.", „Plugin source is disabled.", „Plugin source is not confirmed as trusted.", „Private source has no stored PAT." — das `MapCheckError`-Mapping ist damit exakt umsetzbar.
- `IStringLocalizer<UiStrings>` ist über `_Imports.razor` (Z. 9 + Z. 13) verfügbar; bereits injiziert (`PluginSettings.razor:3`).
- bUnit 1.40.0 + Moq 4.20.72 sind Paket-Referenzen in `Rezepte.Tests.csproj` (Z. 21, 23); `RecipeSearchComponentTests` nutzt `TestContext`, `AddLocalization`, Moq-Registrierung — Muster passt.
- `RezepteAppFixture` hat `protected virtual GetEnvironmentOverrides()` (Z. 134) und startet App-Prozess mit Env-Vars (Z. 204-207); erster registrierter User = Admin (`UserService.cs:143-149`, `IsAdmin = isFirst`) — `SecurityTxtBrowserTests`-Muster trifft zu.
- `PluginUpdateOptions.GitHubApiBaseUrl` existiert (`PluginUpdateOptions.cs:11`, gebunden `PluginUpdates` in `ServiceCollectionExtensions.cs:44`) → Env-Override `PluginUpdates__GitHubApiBaseUrl` funktioniert.
- `SeedInstalledReleaseAsync` ist umsetzbar: `ProcessSourceAsync` überspringt nur bei `PluginSourceId` + `ReleaseTag == tag_name` + `AssetId == asset.Id` + `Status == 'Installed'` (`PluginUpdateService.cs:122-128`); Entity `PluginSourceRelease` hat alle Spalten (`PluginSourceRelease.cs:14-42`). Kein Download nötig (`DownloadAssetAsync` nutzt `asset.DownloadUrl`, wird beim already-installed-Pfad nicht aufgerufen).
- `PublishRezepteWebForBrowserTests`-Target existiert (`Rezepte.Tests.Browser.csproj:32`); `PluginSourceReleases`-Tabelle existiert; `Microsoft.Data.Sqlite` transitiv über `Rezepte.Web`-ProjectReference verfügbar.
- Tasks-Mapping (`...-update-check-ui-tasks.md`): 19 Tasks decken alle Plan-Schritte 1-6 lückenlos ab.

## Fehlende oder unvollständige Testanforderungen

Keine — die Testplanung (bUnit + E2E) deckt alle Akzeptanzkriterien ab.

## E2E-Abdeckung

| Benutzerfluss / Akzeptanzkriterium | Geplanter E2E-Test | Status |
|------------------------------------|--------------------|--------|
| Admin → `/settings` → Plugins-Tab → Quelle anlegen → Prüf-Button + Erklärtext sichtbar | `PluginSettingsBrowserTests` Szenario 1 | Abgedeckt |
| Klick → `Failed` ohne PAT → `alert-danger` lokalisiert + Status-Zelle (`LastError`/`LastCheckedAt`) | Szenario 2 (deterministisch ohne Netzwerk, `PluginUpdateService.cs:102-106`) | Abgedeckt |
| Deaktivierte/nicht vertraute Quelle → Button `disabled` + Grund | Szenario 3 | Abgedeckt |
| Klick → `NoUpdate(alreadyInstalled)` → `alert-success` | Szenario 4 mit `GitHubStubServer` + `SeedInstalledReleaseAsync` | Abgedeckt |
| Klick → `Installed` „Version {Tag} installiert" | Nur bUnit (`...ShowsInstalled`) | Nicht erforderlich mit Begründung: echte Installation erfordert gültiges ZIP-Paket + Plugin-Manager-Reload; der identische Klick→Alert-Fluss ist über `NoUpdate`/`Failed` E2E-nachgewiesen — Hinweis unten |
| Busy/Spinner während Lauf | bUnit (Timing in E2E zu fragil — begründet) | Nicht erforderlich mit Begründung |

## Fehlende oder unvollständige Planbestandteile

- [ ] **`Cancel` fälschlich als neuer resx-Key geplant** (plan.md Z. 110, tasks.md #2): `<data name="Cancel">Abbrechen</data>` existiert bereits (`UiStrings.resx:88`). Ein zweiter Eintrag erzeugt einen resx-Duplikat-Fehler beim Build. `Cancel` muss in die Liste „Wiederverwendete vorhandene Schlüssel" wandern.
- [ ] **`RezepteAppFixture.DatabasePath` existiert bereits** (plan.md Z. 130, tasks.md #14): Die Property ist bereits `public` (`RezepteAppFixture.cs:47`). Das geplante Hinzufügen von `protected string? DatabasePath => _databasePath;` würde CS0102 auslösen und ist obsolet — die Fixture-Änderung entfällt komplett, `PluginSettingsAppFixture` kann `DatabasePath` direkt nutzen.
- [ ] **Resx-Key-Liste unvollständig gegenüber dem eigenen Vollständigkeitsanspruch** (plan.md Z. 88, tasks.md #12): Die Plugin-Tabellenköpfe `Name` und `Assembly` (`PluginSettings.razor:129-130`) sind hartkodiert, haben weder vorhandene noch geplante Keys. (Der Hook würde sie als Ein-Wort-Textknoten nicht bemängeln, aber „alle hartkodierten Strings lokalisieren" verlangt sie.)

## Hinweise

- **Hook-Reichweite überschätzt:** `razor-l10n-check.py` überspringt `@code`-Blöcke komplett (Z. 80-88) und Ein-Wort-Textknoten (Z. 113). Die Begründung „Hook erzwingt Lokalisierung der ganzen Datei" trifft nur auf Mehrwort-Textknoten und lokalisierte Attribute zu; die volle Lokalisierung inkl. `@code`-Meldungen ist Plan-eigenes Ziel, nicht Hook-Pflicht. Das Ziel selbst bleibt richtig.
- **Tooltip auf `disabled`-Button:** `title` an einem `disabled`-Button wird in Browsern praktisch nicht als Tooltip angezeigt (keine Hover-Events auf deaktivierten Controls). Der bUnit-Assert auf das Attribut besteht, die sichtbare Blockade-Erklärung für den Anwender aber ggf. nicht — gängiges Muster: `title` auf ein umschließendes `<span>`. In Nachplanung/Review prüfen.
- **`HttpListener`-Stub-Risiko:** `HttpListener` benötigt auf Windows ggf. eine URL-ACL-Reservierung/Admin-Rechte. GitHub-hosted Windows-Runner laufen als Admin; auf lokalen Nicht-Admin-Konten kann der Stub mit „Access denied" scheitern. Alternative: `TcpListener` oder Kestrel-basierter Stub.
- **Stub-Startreihenfolge:** `GetEnvironmentOverrides()` muss eine konkrete Stub-URL mit bekanntem Port liefern — der `GitHubStubServer` (oder zumindest der reservierte Port) muss vor `base.InitializeAsync()`/`StartApplicationProcess` stehen. Implementierungsdetail, das die Fixture-Struktur bestimmt.
- **`Installed`-Outcome** ist nur bUnit-nachgewiesen (s. E2E-Tabelle). Falls doch ein E2E gewünscht: Stub müsste zusätzlich `asset.DownloadUrl` mit gültigem ZIP-Paket bedienen — deutlich mehr Aufwand.
- **Seed benötigt `PluginSourceId`:** `SeedInstalledReleaseAsync` muss die zur Laufzeit generierte `Id` der angelegten Quelle aus `PluginSources` lesen — im Plan implizit, sollte im Task-Detail stehen.
