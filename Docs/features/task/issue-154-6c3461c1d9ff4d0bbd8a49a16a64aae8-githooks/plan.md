# Umsetzungsplan: Git-Hooks aus Pattern-Collection übernehmen und ausgelöste Prüffehler beheben

## Übersicht

Die Git-Hooks aus `Pattern-Collection` sind bereits nach `.githooks/` übernommen und über `core.hooksPath` aktiviert. Ziel ist die Behebung aller vom Hook ausgelösten Befunde im Anwendungscode, ohne die Prüflogik selbst abzuschwächen: 35 `.razor`-Dateien mit hartkodierten UI-Strings werden lokalisiert, 10 `.csproj`-Dateien mit `GenerateDocumentationFile` und CS1591-als-Fehler konfiguriert (inkl. der vollständigen XML-Doku aller öffentlichen Member), 13 throw-only-Test-Stubs umgebaut und 3 Enums in Tests abgedeckt. Da `pre-commit` jede gestagte `.razor` auf Lokalisierung und jede gestagte `.cs` inklusive ihres nächsten `.csproj` auf XML-Doku prüft, muss die Arbeit in einer strikten Commit-Reihenfolge erfolgen, bei der jeder Commit die Hooks besteht.

## Designentscheidungen

| Komponente / Bereich | Gewählter Ansatz | Begründung |
|----------------------|-----------------|------------|
| Lokalisierungsumfang | **Alle** 35 von `razor-l10n-check --all` gemeldeten `.razor`-Dateien werden lokalisiert (strenge Lesart). | Sonst bleibt der `--all`-Lauf dauerhaft rot; jede spätere Änderung an einer unlokalisierten Datei würde den Commit blockieren. |
| Lokalisierungsinfrastruktur | `IStringLocalizer<UiStrings>` mit Shared-Resource-Marker-Klasse `Rezepte.Web/Resources/UiStrings.cs` und neutralem `UiStrings.resx`; Registrierung via `builder.Services.AddLocalization()` in `Program.cs`; Injektion als `@inject IStringLocalizer<UiStrings> Localizer`. | `razor-l10n-check` akzeptiert `@`-Ausdrücke. Der Variablenname `Localizer` matcht den `LOCALIZER_VAR`-Regex in `translation-check.py`, sodass Schlüssel gegen `.resx` validiert werden. |
| `.resx`-Sprachumfang | Nur eine neutrale `UiStrings.resx` (ohne `.en.resx` o. ä.). | `translation-check.py` erfordert identische Schlüsselmengen, sobald mehrere Kulturdateien im selben Verzeichnis liegen. Mehrsprachigkeit ist nicht angefordert. |
| Resx-Werte | Deutsche UI-Texte als Werte; Ressourcen-Schlüssel sind englische CamelCase-Bezeichner. | Schlüssel sind Code-Identifikatoren und müssen daher Englisch sein (`CLAUDE.md`). Die Werte bleiben deutsch, um die bestehende Oberfläche 1:1 wiederzugeben; Ausnahme `Error.razor` wird in die resx überführt. |
| Localizer-Name | `Localizer` (`@inject IStringLocalizer<UiStrings> Localizer`). | Der `translation-check` validiert `Localizer["..."]`-Schlüssel gegen die `.resx`. Andere Namen würden die Validierung umgehen. |
| XML-Doku-Regime | Pro `.csproj` `<GenerateDocumentationFile>true</GenerateDocumentationFile>` **direkt in der `.csproj`** und `<WarningsAsErrors>CS1591</WarningsAsErrors>`. Keine `NoWarn`-/`<WarningsNotAsErrors>`-Einträge für XML-Doc-Codes, keine `#pragma warning disable` für CS1591/CS157x/CS158x. | `csproj-xmldoc-check.py` parst die jeweilige `.csproj`-Datei; `Directory.Build.props` zählt nicht. Sobald die Konfiguration gesetzt ist, werden fehlende `///`-Kommentare zu Buildfehlern. |
| XML-Doku-Vollständigkeit | **Alle** 10 Projekte werden konfiguriert und vollständig dokumentiert, inkl. `Rezepte.Web` (~2.900 CS1591-Diagnosen) und der Testprojekte. | Die strenge Lesart der Anforderung verbietet `NoWarn`/Pragmas; die Konfiguration wird daher gemeinsam mit der vollständigen Projektdoku eingeführt. |
| razor-usage: BOM | UTF-8-BOM aus allen betroffenen `.razor`-Dateien entfernen, während sie ohnehin für die Lokalisierung bearbeitet werden (8 `@page`-Dateien, `MainLayout`, `AiSettings`, `UsageStats`, `UserAdmin`). | Der `razor-usage-check` liest mit `encoding='utf-8'`; die BOM bleibt als `\ufeff` stehen und verhindert das `@page`-Matching. |
| razor-usage: `MainLayout` | In `Components/Routes.razor` `typeof(Layout.MainLayout)` → `typeof(MainLayout)` plus `@using Rezepte.Web.Components.Layout`. | `TYPEOF_TEMPLATE` matcht nur unqualifizierte `typeof(MainLayout)`. |
| razor-usage: Settings-Komponenten | In `Components/Pages/Settings.razor` eine `@code`-Sektion mit den 9 `typeof(...)`-Referenzen einfügen; `Rezepte.Web/ViewModels/SettingsViewModel.cs` daran anpassen (Liste entfernen oder von der Komponente beziehen). | `razor-usage-check` durchsucht nur `.razor`-Dateien; `@code`-Blöcke werden von `razor-l10n` übersprungen. |
| Test-Stubs | Throw-only-Bodies so umbauen, dass der Body nicht mehr nur aus `throw` besteht (z. B. `Task.FromException`, Aufrufzähler oder lokale Variable vor dem `throw`). | `no-notimplemented-check` meldet jeden Member, dessen Body ein einzelnes `throw` ist. Die Fehlersimulation muss erhalten bleiben. |
| Enum-Abdeckung | Neue Testmethoden in `Rezepte.Tests`, die die fehlenden Werte `ImportCollectionItemState.Pending`/`.Importing`, `WeekDays.Tuesday`/`.Saturday`, `BackgroundJobStatus.Running`/`.Failed`/`.Cancelled` referenzieren. | `enum-coverage-check.py` verlangt das Vorkommen jedes Enum-Werts in mindestens einer Testdatei. |
| Hooks | Prüfungen/Hooks werden **nicht** entschärft. | Anforderung verbietet jegliche Abschwächung oder Umgehung der Skripte. |

## Programmabläufe

### Commit-Gating durch die Hooks

1. `pre-commit` (staged, blockierend): `translation-check` (resx-Header, Paketkonsistenz, `Localizer`-Schlüssel gegen `.resx`), `csproj-xmldoc-check` (jede gestagte `.cs` muss vollständige `///`-Blöcke haben; nächstes `.csproj` muss `GenerateDocumentationFile` und CS1591-als-Fehler haben), `razor-l10n-check` (jede gestagte `.razor` ohne hartkodierte UI-Strings), danach `dotnet format Rezepte.sln --verify-no-changes --no-restore` und `check-encoding.ps1 -Staged`.
2. `pre-push` (repo-weit, blockierend): `no-notimplemented-check --all --strict`, `razor-usage-check --all --strict`, `enum-coverage-check --all --strict`.
3. Konsequenz: Jeder Commit darf nur `.razor`-Dateien enthalten, die bereits vollständig lokalisiert sind, und nur `.cs`-Dateien, deren Projekt bereits CS1591-konfiguriert und deren Doku vollständig ist.

### Lokalisierung einer Razor-Komponente

1. Sicherstellen, dass `@inject IStringLocalizer<UiStrings> Localizer` (bzw. zentral in `_Imports.razor`) verfügbar ist.
2. Jede Fundstelle (lokalisierbare Attribute `title`/`placeholder`/`alt`/`aria-label`/`label`/`tooltip` und mehrwortige Textknoten) durch `@Localizer["<EnglishKey>"]` ersetzen.
3. Schlüssel und deutsche Werte in `UiStrings.resx` eintragen.
4. Bei BOM-Dateien die BOM entfernen, sobald die Datei ohnehin bearbeitet wird.

Beteiligte Klassen/Komponenten: `UiStrings`, `UiStrings.resx`, `IStringLocalizer<T>`, alle 35 gemeldeten `.razor`-Dateien, `Program.cs`, `_Imports.razor`.

### Behebung der razor-usage-Falschbefunde

1. BOM aus den 8 `@page`-Dateien entfernen, damit `^\s*@page` wieder matcht.
2. `Routes.razor`: unqualifiziertes `typeof(MainLayout)` verwenden.
3. `Settings.razor`: `@code`-Sektion mit `typeof(...)`-Referenzen auf die 9 Settings-Komponenten; `SettingsViewModel.cs` anpassen.

Beteiligte Klassen/Komponenten: `Settings.razor`, `SettingsViewModel`, `Routes.razor`, `MainLayout.razor`, die BOM-betroffenen `@page`-Dateien.

### Behebung der Test-Stubs und Enum-Abdeckung

1. In den 5 Testdateien die 13 throw-only-Member umbauen, sodass der Body nicht ausschließlich `throw` enthält.
2. Neue Testmethoden, die die fehlenden Enum-Werte referenzieren.

Beteiligte Klassen/Komponenten: `TestImportPlugin`, `FailingBackupService`, `FailingPreInstallHandler`, `RecordingExportService`, `FailingExportService`, `ThrowingHandler`, `FailingPackageInstaller`, neue/erweiterte Testklassen in `Rezepte.Tests`.

## Neue Klassen

| Klasse | Typ | Zweck |
|--------|-----|-------|
| `Rezepte.Web/Resources/UiStrings.cs` | Marker-Klasse | Generisches Argument für `IStringLocalizer<UiStrings>`, bindet `UiStrings.resx` |
| `Rezepte.Web/Resources/UiStrings.resx` | Ressourcendatei | Neutrale Ressource mit deutschen UI-Texten |
| `Rezepte.Tests/EnumCoverageTests.cs` (oder erweiterte bestehende Klasse) | Testklasse | Referenzierung der fehlenden Enum-Werte |

## Änderungen an bestehenden Klassen

### `Program.cs` (`Rezepte.Web`)

- `builder.Services.AddLocalization()` registrieren, damit `IStringLocalizer<T>` injizierbar ist.
- XML-Doku der öffentlichen Member vervollständigen.

### `Rezepte.Web/Components/_Imports.razor`

- `@using Microsoft.Extensions.Localization` und `@using Rezepte.Web.Resources` ergänzen.

### `Rezepte.Web/Rezepte.Web.csproj` (+ 9 weitere `.csproj`)

- `<GenerateDocumentationFile>true</GenerateDocumentationFile>` und `<WarningsAsErrors>CS1591</WarningsAsErrors>` in eine `PropertyGroup` direkt in der Datei aufnehmen.
- Betroffen: `Rezepte.Web`, `Rezepte.Tests`, `Rezepte.Tests.Browser`, `Rezepte.Tests.PluginFixture`, `Rezepte.Updater.TestHost`, `Rezepte.Import.Abstractions`, `Rezepte.Import.PluginSdk`, `Rezepte.Import.Plugins.AIFoto`, `Rezepte.Import.Plugins.AIUrl`, `Rezepte.Import.Plugins.Backup`.
- `Rezepte.Web`: ggf. `PackageReference` `Microsoft.Extensions.Localization` ergänzen (nur falls nicht transitiv vorhanden).

### Alle öffentlichen C#-Member in den 10 Projekten

- Vollständige `/// <summary>`-Dokumentation für alle öffentlich/protected sichtbaren Typen und Member ergänzen, damit `CS1591` nicht anschlägt.
- Die 24 vom `csproj-xmldoc-check` gemeldeten `.cs`-Dateien erhalten zusätzlich fehlende `<param>`, `<typeparam>`, `<returns>` und `<response code="...">`-Tags.

### `Rezepte.Web/Components/Routes.razor`

- `DefaultLayout="typeof(Layout.MainLayout)"` → `typeof(MainLayout)` + `@using Rezepte.Web.Components.Layout`.

### `Rezepte.Web/Components/Pages/Settings.razor`

- Vollständige Lokalisierung der 2 UI-Strings.
- `@code`-Sektion mit `typeof(...)`-Referenzen auf die 9 Settings-Komponenten ergänzen.

### `Rezepte.Web/ViewModels/SettingsViewModel.cs`

- Die `typeof(...)`-Navigationsliste (Z. 33–41) entfernen oder auf die in `Settings.razor` zentrale Liste verweisen.
- XML-Doku anpassen.

### 35 `.razor`-Dateien (razor-l10n)

- Vollständige Liste mit Zeilen/Fundtexten in `inventory/razor-l10n.md`. Alle Fundstellen durch `@Localizer[...]` ersetzen; zugehörige Schlüssel in `UiStrings.resx`.

### 5 Testdateien (no-notimplemented)

- `Rezepte.Tests.PluginFixture/TestImportPlugin.cs` (Z. 31), `Rezepte.Tests/Services/ApplicationUpdatePreInstallHandlerTests.cs` (Z. 101, 118), `Rezepte.Tests/Services/UpdateBackupServiceTests.cs` (Z. 162, 174, 180, 183, 186), `Rezepte.Tests/Services/Import/ImportOrchestratorTests.cs` (Z. 389), `Rezepte.Tests/Services/Import/PluginUpdateServiceTests.cs` (Z. 160): Bodies umbauen, Fehlersimulation unverändert.

### `Rezepte.Web/Components/Pages/RecipeEdit.razor`

- Die bereits vorbereitete `geloescht` → `gelöscht`-Korrektur (Z. 384, 392) wird im Lokalisierungs-Commit dieser Datei mit übernommen.

## Datenbankmigrationen

Keine.

## Validierungsregeln

Keine.

## Konfigurationsänderungen

| Eintrag | Typ | Standardwert | Zweck |
|---------|-----|--------------|-------|
| `<GenerateDocumentationFile>true</GenerateDocumentationFile>` | `.csproj`-Property (10 Projekte) | — | XML-Dokudatei erzeugen (Voraussetzung für CS1591) |
| `<WarningsAsErrors>CS1591</WarningsAsErrors>` | `.csproj`-Property (10 Projekte) | — | Fehlende XML-Doku bricht den Build |
| `builder.Services.AddLocalization()` | DI-Registrierung (`Program.cs`) | — | `IStringLocalizer<T>` verfügbar machen |
| `Microsoft.Extensions.Localization` | NuGet-PackageReference (nur falls nicht transitiv) | — | `AddLocalization`/`IStringLocalizer` bereitstellen |
| `git config core.hooksPath .githooks` | Repo-Git-Konfiguration | bereits gesetzt | Hook-Aktivierung (für andere Entwickler über `install-hooks.cmd`/`.sh`) |

## Seiteneffekte und Risiken

- **CS1591-Buildkaskade in `Rezepte.Web`:** Sobald das `.csproj` CS1591 als Fehler konfiguriert, schlägt `dotnet build` bei jedem undokumentierten öffentlichen Member fehl. Der tatsächliche Umfang liegt bei ca. 2.900 Diagnosen und ist erst nach der Konfiguration per Build ermittelbar.
- **UI-Text in `Error.razor`:** Die Seite enthält bisher englische Texte. Da die resx deutsch sein soll, werden diese in die resx überführt und gegebenenfalls übersetzt; bestehende Browser-Tests auf die englischen Texte müssen ggf. angepasst werden.
- **translation-check-Aktivierung:** Sobald die erste `.resx` existiert, werden bei jedem Commit repo-weit resx-Header und Paketkonsistenz geprüft sowie in gestagten Dateien verwendete `Localizer`-Schlüssel gegen `.resx` validiert.
- **`dotnet format`:** Jede `.cs`-/`.razor`-Änderung muss formatkonform sein (`--verify-no-changes` läuft bei jedem Commit); nach größeren Edits `dotnet format Rezepte.sln` ausführen.
- **`check-encoding.ps1`:** Neue `.resx`-Dateien müssen valides UTF-8 sein; deutsche Umlaute sind erlaubt, ASCII-Transliterierungen (`fuer`, `koennen`, `geloescht` etc.) in gestagten Dateien sind blockierend — auch in `///`-Kommentaren und resx-Texten darauf achten.
- **Browser-Tests (`Rezepte.Tests.Browser`):** Die resx-Werte entsprechen weitgehend den bisherigen deutschen Strings; abweichende Texte (insbesondere in `Error.razor`) können Tests brechen.
- **`Routes.razor`-Änderung:** `@using`-Hinzufügung kann Namenskonflikte erzeugen, falls ein zweites `MainLayout` im Scope existiert — beim Build prüfen.
- **`SettingsViewModel`-Refaktor:** Die verlagerte Komponentenliste ändert die Zuständigkeit; bestehende Unit-Tests für `SettingsViewModel` müssen ggf. angepasst werden.

## Umsetzungsreihenfolge

Die Reihenfolge ist so gewählt, dass jeder Commit die `pre-commit`-Prüfungen besteht. `pre-push`-Befunde (Stubs, Usage, Enum) sind spätestens vor dem finalen Push behoben.

1. **Arbeitspaket 1 — Hooks-Baseline und Dokumentationsartefakte committen**
   - Voraussetzungen: Keine.
   - Dateien: `.githooks/*`, `install-hooks.cmd`, `install-hooks.sh`, `CLAUDE.md`, `Docs/features/task/issue-154-6c3461c1d9ff4d0bbd8a49a16a64aae8-githooks/*`.
   - Beschreibung: `Rezepte.Web/Components/Pages/RecipeEdit.razor` vorher aus dem Index nehmen (`git restore --staged`), da es hartkodierte UI-Strings enthält und später lokalisiert wird. Danach die Hook- und Dokumentationsdateien committen.
   - Akzeptanzkriterien: Gestagte Dateien enthalten keine `.cs` / `.razor` / `.resx` / `.csproj`; `python .githooks/translation-check.py`, `csproj-xmldoc-check.py`, `razor-l10n-check.py` laufen OK; `dotnet format Rezepte.sln --verify-no-changes --no-restore` = 0; `check-encoding.ps1 -Staged` = 0; `pre-commit` durchläuft ohne Fehler.

2. **Arbeitspaket 2 — Rezepte.Web: XML-Doku-Konfiguration, vollständige XML-Doku und Lokalisierungsinfrastruktur**
   - Voraussetzungen: AP 1; Verifizieren, ob `Microsoft.Extensions.Localization` transitiv verfügbar ist (`dotnet list Rezepte.Web package --include-transitive`), sonst PackageReference ergänzen.
   - Dateien: `Rezepte.Web/Rezepte.Web.csproj`, `Rezepte.Web/Resources/UiStrings.cs`, `Rezepte.Web/Resources/UiStrings.resx`, `Rezepte.Web/Program.cs`, `Rezepte.Web/Components/_Imports.razor`, `.gitignore` ggf., **sämtliche** `.cs` in `Rezepte.Web` mit öffentlichen/protected Membern.
   - Beschreibung: `.csproj` um `GenerateDocumentationFile` und `<WarningsAsErrors>CS1591</WarningsAsErrors>` erweitern; Lokalisierungsinfrastruktur anlegen; alle öffentlichen Member in `Rezepte.Web` vollständig mit `///`-Doku versehen (ca. 2.900 CS1591-Diagnosen).
   - Akzeptanzkriterien: `dotnet build Rezepte.Web` = 0; `python .githooks/csproj-xmldoc-check.py --all` = 0; `python .githooks/translation-check.py --all` = 0; `python .githooks/razor-l10n-check.py` (für `_Imports.razor`) = 0; `dotnet format ...` = 0; `check-encoding.ps1 -Staged` = 0.

3. **Arbeitspaket 3 — Razor-Usage-Befunde und Lokalisierung: Pages/Layout**
   - Voraussetzungen: AP 2 (Rezepte.Web csproj konfiguriert, Localizer verfügbar, Build grün).
   - Dateien: `MainLayout.razor`, `Calendar.razor`, `CookbookDetails.razor`, `CookbookPage.razor`, `Cookbooks.razor`, `Error.razor`, `Home.razor`, `RecipePage.razor`, `Settings.razor`, `Routes.razor`, `SettingsViewModel.cs`.
   - Beschreibung: BOM entfernen und alle UI-Strings der 8 `@page`-Dateien + `MainLayout` lokalisieren; `Routes.razor` auf `typeof(MainLayout)` umstellen; in `Settings.razor` `@code`-Sektion mit 9 `typeof(...)`-Komponentenreferenzen ergänzen; `SettingsViewModel.cs` anpassen.
   - Akzeptanzkriterien: `python .githooks/razor-usage-check.py --all --strict` = 0; `python .githooks/razor-l10n-check.py` (gestagte .razor) = 0; `python .githooks/csproj-xmldoc-check.py` (für `SettingsViewModel.cs`) = 0; `translation-check` grün; `dotnet format` = 0; `check-encoding` = 0.

4. **Arbeitspaket 4 — Lokalisierung: Restliche Pages**
   - Voraussetzungen: AP 2.
   - Dateien: `Login.razor`, `RecipeEdit.razor` (inkl. `geloescht`-Fix), `RecipeSearch.razor`, `Register.razor`, `ScheduledRecipes.razor`, `ShoppingList.razor`.
   - Beschreibung: Jede gestagte `.razor`-Datei vollständig lokalisieren; Schlüssel/Werte in `UiStrings.resx`.
   - Akzeptanzkriterien: `python .githooks/razor-l10n-check.py --all` für diese Dateien = 0; `python .githooks/translation-check.py` = 0; `dotnet format` = 0; `check-encoding` = 0.

5. **Arbeitspaket 5 — Lokalisierung: Settings-Komponenten**
   - Voraussetzungen: AP 2.
   - Dateien: `AiSettings.razor`, `ApplicationUpdates.razor`, `BackupRestore.razor`, `ExportData.razor`, `ExportFilesList.razor`, `PluginSettings.razor`, `SecurityTxtSettings.razor`, `UsageStats.razor`, `UserAdmin.razor`, `UserProfile.razor` (BOM entfernen bei `AiSettings`, `UsageStats`, `UserAdmin`).
   - Akzeptanzkriterien: `python .githooks/razor-l10n-check.py --all` für Settings-Komponenten = 0; `translation-check` grün; `dotnet format` = 0; `check-encoding` = 0.

6. **Arbeitspaket 6 — Lokalisierung: Shared-Komponenten**
   - Voraussetzungen: AP 2.
   - Dateien: `AddRecipeToShoppingListDialog.razor`, `AssignToCookbooksOverlay.razor`, `CalendarEventDialog.razor`, `CreateRecipeDialog.razor`, `ImageCropper.razor`, `LatestRecipes.razor`, `MultiAssignToCookbooksOverlay.razor`, `PhotoOverlay.razor`, `RandomFromCookbooks.razor`, `RecipeSelectDialog.razor`.
   - Akzeptanzkriterien: `python .githooks/razor-l10n-check.py --all` = 0 (gesamtes Repo); `translation-check` grün; `dotnet format` = 0; `check-encoding` = 0.

7. **Arbeitspaket 7 — Rezepte.Tests + Rezepte.Tests.PluginFixture: XML-Doku, Stubs und Enum-Abdeckung**
   - Voraussetzungen: AP 2 (Formatierungs- und Build-Konsistenz).
   - Dateien: `Rezepte.Tests/Rezepte.Tests.csproj`, `Rezepte.Tests.PluginFixture/Rezepte.Tests.PluginFixture.csproj`, alle öffentlichen `.cs` in beiden Projekten, die 5 Test-Fake-Dateien mit throw-only-Membern, neue/erweiterte Enum-Testklasse.
   - Beschreibung: `.csproj` XML-Doc konfigurieren; `///`-Doku vervollständigen; 13 throw-only-Bodies umbauen; fehlende Enum-Werte in Tests referenzieren.
   - Akzeptanzkriterien: `dotnet build Rezepte.Tests` und `dotnet build Rezepte.Tests.PluginFixture` = 0; `python .githooks/csproj-xmldoc-check.py --all` = 0; `python .githooks/no-notimplemented-check.py --all --strict` = 0; `python .githooks/enum-coverage-check.py --all --strict` = 0; `dotnet format` = 0; `check-encoding` = 0.

8. **Arbeitspaket 8 — Rezepte.Tests.Browser: XML-Doku-Konfiguration**
   - Voraussetzungen: Keine.
   - Dateien: `Rezepte.Tests.Browser/Rezepte.Tests.Browser.csproj`, `Rezepte.Tests.Browser/Infrastructure/ConfiguredRezepteAppFixture.cs`, weitere öffentliche `.cs` im Projekt.
   - Beschreibung: `.csproj` konfigurieren; `///`-Doku vervollständigen; `dotnet build Rezepte.Tests.Browser` fehlerfrei.
   - Akzeptanzkriterien: `dotnet build Rezepte.Tests.Browser` = 0; `python .githooks/csproj-xmldoc-check.py --all` = 0; `dotnet format` = 0; `check-encoding` = 0.

9. **Arbeitspaket 9 — Import- und Updater-Projekte: XML-Doku-Konfiguration**
   - Voraussetzungen: Keine.
   - Dateien: `Rezepte.Import.Abstractions.csproj`, `Rezepte.Import.PluginSdk.csproj`, `Rezepte.Import.Plugins.AIFoto.csproj`, `Rezepte.Import.Plugins.AIUrl.csproj`, `Rezepte.Import.Plugins.Backup.csproj`, `Rezepte.Updater.TestHost.csproj` sowie alle öffentlichen `.cs` in diesen Projekten.
   - Beschreibung: Pro `.csproj` `GenerateDocumentationFile` + `WarningsAsErrors>CS1591` setzen und alle CS1591-Buildfehler beheben.
   - Akzeptanzkriterien: `dotnet build` für jedes dieser Projekte = 0; `python .githooks/csproj-xmldoc-check.py --all` = 0; `dotnet format` = 0; `check-encoding` = 0.

10. **Arbeitspaket 10 — Endverifikation und finaler Push**
    - Voraussetzungen: AP 1–9.
    - Beschreibung: Gesamtlösung bauen und testen, alle repo-weiten Checks laufen lassen.
    - Akzeptanzkriterien: `dotnet build Rezepte.sln` = 0; `dotnet test Rezepte.Tests` grün; `Rezepte.Tests.Browser` grün (sofern ausführbar); `dotnet format Rezepte.sln --verify-no-changes --no-restore` = 0; `python .githooks/translation-check.py --all` = 0; `python .githooks/csproj-xmldoc-check.py --all` = 0; `python .githooks/razor-l10n-check.py --all` = 0; `python .githooks/razor-usage-check.py --all --strict` = 0; `python .githooks/no-notimplemented-check.py --all --strict` = 0; `python .githooks/enum-coverage-check.py --all --strict` = 0; `python .githooks/check-encoding.ps1` (repo-weit) = 0; finaler `git commit` und `git push` durchlaufen beide Hooks ohne Blocker.

## Tests

### Neue Tests

| Test / Hilfsmethode | Testklasse | Was wird geprüft / bereitgestellt? |
|--------------------|------------|-------------------------------------|
| Enum-Wert-Abdeckung `ImportCollectionItemState` | `Rezepte.Tests` (neu oder bestehende Klasse) | `Pending` und `Importing` werden in Testcode referenziert |
| Enum-Wert-Abdeckung `WeekDays` | `Rezepte.Tests` (neu oder bestehende Klasse) | `Tuesday` und `Saturday` werden referenziert |
| Enum-Wert-Abdeckung `BackgroundJobStatus` | `Rezepte.Tests` (neu oder bestehende Klasse) | `Running`, `Failed` und `Cancelled` werden referenziert |

### Betroffene bestehende Tests

| Test / Testklasse | Grund der Anpassung |
|-------------------|---------------------|
| `TestImportPlugin.CheckUsabilityAsync` | Throw-only-Body verboten → Umbau, Semantik identisch |
| `ApplicationUpdatePreInstallHandlerTests` (`FailingBackupService`, `FailingPreInstallHandler`) | Throw-only-Bodies umbauen |
| `UpdateBackupServiceTests` (`RecordingExportService`, `FailingExportService`) | Throw-only-Bodies umbauen |
| `ImportOrchestratorTests` (`ThrowingHandler`, `CancellingStream`) | ThrowingHandler: throw-only-Body umbauen; CancellingStream: XML-Doku vervollständigen |
| `PluginUpdateServiceTests` (`FailingPackageInstaller`) | Throw-only-Body umbauen |
| Ggf. `SettingsViewModel`-Tests | Komponentenliste wandert nach `Settings.razor` |
| `Rezepte.Tests.Browser` (gesamte Suite) | Nur bei versehentlich geänderten resx-Texten; Lauf als Regressionstest eingeplant |

### E2E-Tests (primärer Funktionsnachweis)

Keine neuen E2E-Tests erforderlich: Die Anforderung führt keinen neuen oder geänderten Benutzerfluss ein — sie betrifft ausschließlich Qualitätsprüfungen und deren Befundbehebung. Die Lokalisierung lagert bestehende Texte 1:1 in `UiStrings.resx` aus (bis auf die englischen `Error.razor`-Texte, die in die resx überführt werden). Als Regressionssicherung ist der Lauf der bestehenden `Rezepte.Tests.Browser`-Suite im Endverifikations-Arbeitspaket eingeplant.

## Offene Punkte

Keine. Sämtliche zuvor offenen Fragen (Lokalisierungsumfang, resx-Sprachen, Localizer-Name, XML-Doku-Umfang, Hook-Abschwächung) wurden verbindlich geklärt und in die Designentscheidungen und Arbeitspakete eingearbeitet.
