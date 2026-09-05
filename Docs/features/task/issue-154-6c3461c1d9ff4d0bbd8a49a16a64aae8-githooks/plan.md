# Umsetzungsplan: Git-Hooks aus Pattern-Collection übernehmen und ausgelöste Prüffehler beheben

## Übersicht

Die Git-Hooks aus `Pattern-Collection` sind bereits nach `.githooks/` übernommen und über `core.hooksPath` aktiviert. Umgesetzt werden muss die Behebung sämtlicher Befunde, die die (unverändert zu belassenden) Check-Skripte im Anwendungscode aufdecken: hartkodierte UI-Strings in 35 `.razor`-Dateien, fehlende/unvollständige XML-Dokumentation (24 `.cs`-Dateien + 10 `.csproj` ohne `GenerateDocumentationFile`/CS1591-Konfiguration), 13 throw-only-Test-Stubs, 18 razor-usage-Falschbefunde und fehlende Enum-Abdeckung für 3 Enums. Da `pre-commit` jede gestagte `.razor` auf Lokalisierung und jede gestagte `.cs` inklusive ihres nächsten `.csproj` auf XML-Doku prüft, muss zuerst eine Lokalisierungsinfrastruktur aufgebaut und die `.csproj`-Konfiguration projektweise atomar mit der vollständigen Member-Doku eingeführt werden.

## Designentscheidungen

| Komponente / Bereich | Gewählter Ansatz | Begründung |
|----------------------|-----------------|------------|
| Lokalisierungsinfrastruktur | `IStringLocalizer<UiStrings>` mit Shared-Resource-Marker-Klasse `Rezepte.Web/Resources/UiStrings.cs` und neutraler `UiStrings.resx` (bisherige deutsche Texte als Werte); Registrierung via `builder.Services.AddLocalization()` in `Program.cs`; Injektion je Komponente als `@inject IStringLocalizer<UiStrings> Localizer` | `razor-l10n-check.py` akzeptiert jede Attribut-/Textstelle, die mit `@` beginnt bzw. einen `@`-Ausdruck enthält — `IStringLocalizer` ist der Standardweg, den `translation-check.py` kennt. Die Benennung `Localizer` ist bewusst gewählt: Der Schlüssel-Regex `LOCALIZER_VAR` matcht nur Variablennamen, die auf `localizer`/`Localizer` enden — so wird die Schlüssel-Vollständigkeit gegen `.resx` tatsächlich erzwungen. Neutrale `.resx` mit den bestehenden deutschen Texten ⇒ keine sichtbare UI-Änderung, bestehende Browser-Tests bleiben gültig. |
| `.resx`-Sprachumfang | Nur eine neutrale `UiStrings.resx` (deutsche Texte), keine Kulturvarianten | `translation-check.py` verlangt bei mehreren Sprachdateien im selben Verzeichnis identische Schlüsselmengen; Mehrsprachigkeit ist nicht angefordert. |
| CS1591-Kaskade | Pro `.csproj` atomar im selben Commit wie die erste `.cs`-Änderung: `<GenerateDocumentationFile>true</GenerateDocumentationFile>` + `<WarningsAsErrors>CS1591</WarningsAsErrors>` **direkt in der `.csproj`** + vollständige `///`-Doku aller öffentlich sichtbaren Member des gesamten Projekts | `csproj-xmldoc-check.py` parst die `.csproj`-XML direkt — `Directory.Build.props` zählt nicht (verifiziert: Check iteriert `root.iter("PropertyGroup")` der jeweiligen Datei). Sobald CS1591 als Fehler konfiguriert ist, schlägt der Build bei jedem undokumentierten `public`/`protected` Member fehl; die Doku muss daher projektweit vollständig sein, nicht nur in den 24 bekannten Dateien. |
| razor-usage: Settings-Komponenten | Die `typeof(...)`-Navigationsliste aus `Rezepte.Web/ViewModels/SettingsViewModel.cs` (Z. 33–41) in die `@code`-Sektion von `Components/Pages/Settings.razor` verlagern; `SettingsViewModel` konsumiert sie von dort | `razor-usage-check.py` durchsucht nur `.razor`-Dateien (verifiziert: `all_razor_files`, Z. 55–61). `@code`-Blöcke werden von `razor-l10n` übersprungen, sodass die `typeof`-Liste dort keinen l10n-Befund erzeugt. |
| razor-usage: `MainLayout` | In `Components/Routes.razor` Z. 5 `typeof(Layout.MainLayout)` → `typeof(MainLayout)` plus `@using Rezepte.Web.Components.Layout` | Das Check-Template `typeof\s*\(\s*MainLayout\s*\)` matcht keine qualifizierten Namen (verifiziert, `TYPEOF_TEMPLATE` Z. 33). |
| razor-usage: BOM | UTF-8-BOM aus den 8 `@page`-Dateien entfernen (`Calendar`, `CookbookDetails`, `CookbookPage`, `Cookbooks`, `Error`, `Home`, `RecipePage`, `Settings`) — fällt mit der ohnehin nötigen Lokalisierung derselben Dateien zusammen | Der Check liest mit `encoding='utf-8'`; `\ufeff` ist kein Python-Whitespace, `^\s*@page` matcht nicht. `check-encoding.ps1` verlangt UTF-8 ohnehin nur valide — BOM-Entfernung ist zulässig. |
| Test-Stubs (no-notimplemented) | Throw-only-Bodies so umbauen, dass der Body nicht mehr nur aus `throw` besteht: bevorzugt `Task.FromException`/`Task.FromResult`-Rückgaben bzw. einen Aufrufzähler vor dem `throw` | Der Check meldet Member, deren gesamter Body ein einzelnes `throw` ist; er hat keinen Suppressionsmechanismus. Die Stubs simulieren absichtlich Fehlerfälle — Semantik muss erhalten bleiben. |
| Enum-Abdeckung | Neue Testfälle in `Rezepte.Tests`, die die fehlenden Enum-Werte `ImportCollectionItemState.Pending`/`.Importing`, `WeekDays.Tuesday`/`.Saturday`, `BackgroundJobStatus.Running`/`.Failed`/`.Cancelled` referenzieren | `enum-coverage-check.py` verlangt Vorkommen jedes Enum-Werts in mindestens einer Testdatei. |

## Programmabläufe

### Commit-Gating durch die Hooks

1. `pre-commit` (staged, blockierend): `translation-check` (prüft `Localizer["key"]`-Schlüssel gegen alle `.resx` repo-weit + resx-Header + Paketkonsistenz), `csproj-xmldoc-check` (jede gestagte `.cs`: `///`-Vollständigkeit der dokumentierten Member + nächstes `.csproj` muss `GenerateDocumentationFile` + CS1591-als-Fehler haben), `razor-l10n-check` (jede gestagte `.razor` frei von hartkodierten UI-Strings, `@code`-Blöcke übersprungen), danach `dotnet format Rezepte.sln --verify-no-changes --no-restore` und `check-encoding.ps1 -Staged`.
2. `pre-push` (repo-weit, blockierend): `no-notimplemented-check --all --strict`, `razor-usage-check --all --strict`, `enum-coverage-check --all --strict`.
3. Konsequenz: Jeder Commit darf nur `.razor`-Dateien enthalten, die bereits vollständig lokalisiert sind, und nur `.cs`-Dateien, deren Projekt bereits CS1591-konfiguriert und deren Doku vollständig ist.

### Lokalisierung einer Razor-Komponente

1. `@inject IStringLocalizer<UiStrings> Localizer` ergänzen (oder zentral in `_Imports.razor`).
2. Jede Fundstelle (lokalisierbare Attribute `title`/`placeholder`/`alt`/`aria-label`/`label`/`tooltip` und mehrwortige Textknoten) durch `@Localizer["Schlüssel"]` bzw. `@Localizer["Schlüssel", arg]` ersetzen.
3. Schlüssel + deutschen Text in `UiStrings.resx` eintragen.

Beteiligte Klassen/Komponenten: `UiStrings`, `UiStrings.resx`, `IStringLocalizer<T>`, alle 35 gemeldeten `.razor`-Dateien, `Program.cs`, `_Imports.razor`.

### Behebung der razor-usage-Falschbefunde

1. BOM aus den 8 `@page`-Dateien entfernen → `^\s*@page` matcht wieder.
2. `Routes.razor`: unqualifiziertes `typeof(MainLayout)`.
3. `Settings.razor`: `@code`-Sektion mit der `typeof(...)`-Komponentenliste; `SettingsViewModel.cs` referenziert diese Liste statt sie zu definieren.

Beteiligte Klassen/Komponenten: `Settings.razor`, `SettingsViewModel`, `Routes.razor`, `MainLayout.razor`, die 8 BOM-`@page`-Dateien.

### Behebung der Test-Stubs und Enum-Abdeckung

1. In den 5 Testdateien die 13 throw-only-Member umbauen (z. B. `=> Task.FromException<T>(new InvalidOperationException(...))` oder Aufrufzähler + `throw`), Testverhalten unverändert.
2. Neue Testmethoden, die die fehlenden Enum-Werte durchlaufen (z. B. `Enum.GetValues`-Iteration mit Assert auf Vollständigkeit oder gezielte Mapping-Tests).

Beteiligte Klassen/Komponenten: `TestImportPlugin`, `FailingBackupService`, `FailingPreInstallHandler`, `RecordingExportService`, `FailingExportService`, `ThrowingHandler`, `FailingPackageInstaller`, neue/erweiterte Testklassen in `Rezepte.Tests`.

## Neue Klassen

| Klasse | Typ | Zweck |
|--------|-----|-------|
| `Rezepte.Web/Resources/UiStrings.cs` | Marker-Klasse (leer) | Generisches Argument für `IStringLocalizer<UiStrings>`, bindet `UiStrings.resx` |
| `Rezepte.Web/Resources/UiStrings.resx` | Ressourcendatei | Alle UI-Schlüssel mit deutschen Texten (neutrale Kultur) |
| Enum-Coverage-Testklasse(n) in `Rezepte.Tests` | Testklasse | Abdeckung der fehlenden Enum-Werte |

## Änderungen an bestehenden Klassen

### `Program.cs` (Rezepte.Web)

- **Geändert:** `builder.Services.AddLocalization()` registrieren, damit `IStringLocalizer<T>` injizierbar ist.

### `Rezepte.Web/Components/_Imports.razor`

- `@using Microsoft.Extensions.Localization` und `@using Rezepte.Web.Resources` ergänzen.

### `Rezepte.Web/Rezepte.Web.csproj` (+ 9 weitere `.csproj`)

- `<GenerateDocumentationFile>true</GenerateDocumentationFile>` und `<WarningsAsErrors>CS1591</WarningsAsErrors>` in eine `PropertyGroup` direkt in der Datei aufnehmen. Betroffen: `Rezepte.Web`, `Rezepte.Tests`, `Rezepte.Tests.Browser`, `Rezepte.Tests.PluginFixture`, `Rezepte.Updater.TestHost`, `Rezepte.Import.Abstractions`, `Rezepte.Import.PluginSdk`, `Rezepte.Import.Plugins.AIFoto`, `Rezepte.Import.Plugins.AIUrl`, `Rezepte.Import.Plugins.Backup`.
- `Rezepte.Web`: ggf. `PackageReference` `Microsoft.Extensions.Localization` ergänzen (nur falls nicht transitiv über das Framework vorhanden — vor Implementierung per `dotnet list package` verifizieren).

### 24 `.cs`-Dateien mit unvollständiger XML-Doku

- Fehlende `<param>`-, `<typeparam>`-, `<returns>`- und `<response>`-Tags ergänzen gemäß Liste in `inventory/xmldoc.md` (u. a. alle Controller in `Rezepte.Web/Controllers`, `LoadingBarOptions`, `LoadingBarSettings`, Extensions, Services/Interfaces, `CancellingStream` in `ImportOrchestratorTests`, `ConfiguredRezepteAppFixture`).
- Zusätzlich: **alle** öffentlich sichtbaren Member der konfigurierten Projekte müssen `///`-Doku erhalten (CS1591-Buildkaskade — Umfang wird beim ersten Build nach der `.csproj`-Konfiguration sichtbar).

### `Rezepte.Web/ViewModels/SettingsViewModel.cs`

- **Geändert:** Die `typeof(...)`-Komponentenliste (Z. 33–41) wird nach `Settings.razor` `@code` verlagert; `SettingsViewModel` bezieht die Liste von dort (oder die Komponente verwendet sie direkt).

### `Rezepte.Web/Components/Routes.razor`

- `DefaultLayout="typeof(Layout.MainLayout)"` → `typeof(MainLayout)` + `@using Rezepte.Web.Components.Layout`.

### 35 `.razor`-Dateien (razor-l10n)

- Vollständige Liste mit Zeilen/Fundtexten in `inventory/razor-l10n.md`. Alle Fundstellen durch `@Localizer[...]` ersetzen; zugehörige Schlüssel in `UiStrings.resx`. Bei den 12 BOM-Dateien gleichzeitig die BOM entfernen.

### 5 Testdateien (no-notimplemented)

- `Rezepte.Tests.PluginFixture/TestImportPlugin.cs` (Z. 31), `ApplicationUpdatePreInstallHandlerTests.cs` (Z. 101, 118), `UpdateBackupServiceTests.cs` (Z. 162, 174, 180, 183, 186), `ImportOrchestratorTests.cs` (Z. 389), `PluginUpdateServiceTests.cs` (Z. 160): Bodies umbauen, Fehlersimulation unverändert.

### `Rezepte.Web/Components/Pages/RecipeEdit.razor`

- Die bereits gestagede „geloescht"→„gelöscht"-Korrektur (Z. 384, 392) wird im Lokalisierungs-Commit dieser Datei mit übernommen.

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

- **CS1591-Buildkaskade:** Sobald ein `.csproj` CS1591 als Fehler konfiguriert, schlägt `dotnet build` bei **jedem** undokumentierten öffentlichen Member des Projekts fehl — der tatsächliche Dokumentationsumfang in `Rezepte.Web` ist deutlich größer als die 24 vom Check gemeldeten Dateien (der Check prüft nur Vollständigkeit vorhandener `///`-Blöcke, nicht deren Vorhandensein). Umfang erst nach Konfiguration per Build ermittelbar → Zeitrisiko.
- **translation-check-Aktivierung:** Sobald die erste `.resx` existiert, werden bei jedem Commit repo-weit resx-Header und Paketkonsistenz geprüft sowie in gestagten Dateien verwendete `Localizer`-Schlüssel gegen `.resx` validiert. `Localizer["Schlüssel"]`-Verwendungen ohne resx-Eintrag blockieren den Commit.
- **`dotnet format`:** Jede `.cs`-/`.razor`-Änderung muss formatkonform sein (`--verify-no-changes` läuft bei jedem Commit); nach größeren Edits `dotnet format` ausführen.
- **`check-encoding.ps1`:** Neue `.resx`-Dateien müssen valides UTF-8 sein; deutsche Umlaute sind erlaubt, ASCII-Transliterierungen (`fuer`, `koennen` etc.) in gestagten Dateien sind blockierend — auch in `///`-Kommentaren und resx-Texten darauf achten.
- **Browser-Tests (`Rezepte.Tests.Browser`):** Falls Tests auf konkrete deutsche UI-Texte asserten, bleiben sie durch die neutrale resx mit identischen Texten unverändert; Abweichungen (z. B. geänderte Formulierungen) würden Tests brechen — Texte 1:1 übernehmen.
- **`Routes.razor`-Änderung:** `@using`-Hinzufügung kann Namenskonflikte erzeugen, falls ein zweites `MainLayout` im Scope existiert — beim Build prüfen.
- **`SettingsViewModel`-Refaktor:** Die verlagerte Komponentenliste ändert die Zuständigkeit (UI-nahe Liste wandert in die Komponente); bestehende Unit-Tests für `SettingsViewModel` müssen ggf. angepasst werden.

## Umsetzungsreihenfolge

Commit-Reihenfolge so gewählt, dass jeder Commit die aktivierten `pre-commit`-Prüfungen besteht (gestagte Dateien sind frei von l10n-/xmldoc-Befunden, `.resx` konsistent, `dotnet format` und Encoding grün). `pre-push`-Befunde (stubs/usage/enum, `--all --strict`) sind spätestens vor dem finalen Push behoben, werden aber frühzeitig angegangen, da sie `.razor`-/`.cs`-Änderungen erzwingen, die wiederum Lokalisierung/XML-Doku voraussetzen.

1. **Commit 1 — Hooks-Baseline committen**
   - Voraussetzungen: Keine.
   - Beschreibung: `Rezepte.Web/Components/Pages/RecipeEdit.razor` aus dem Index nehmen (`git restore --staged`), dann `.githooks/*`, `CLAUDE.md` und die Docs-Artefakte (`Docs/features/...`) committen. Gestaged sind dann keine `.cs`/`.razor`/`.resx` → translation-/csproj-/l10n-Checks laufen leer durch; `dotnet format` ist verifiziert grün; `check-encoding -Staged` prüft nur die Hook-/Doc-Dateien.

2. **Commit 2 — Rezepte.Web: XML-Doku-Konfiguration + Lokalisierungsinfrastruktur**
   - Voraussetzungen: Commit 1. Verifizieren, ob `Microsoft.Extensions.Localization` transitiv verfügbar ist (`dotnet list Rezepte.Web package --include-transitive`); sonst `PackageReference` ergänzen.
   - Beschreibung: `Rezepte.Web.csproj` um `GenerateDocumentationFile` + `WarningsAsErrors>CS1591` erweitern; `UiStrings.cs`-Marker + `UiStrings.resx` anlegen; `Program.cs` `AddLocalization()`; `_Imports.razor` `@using`/`@inject`-Vorkehrungen. Anschließend `dotnet build Rezepte.Web` → alle CS1591-Fehler im gesamten Projekt beheben (vollständige `///`-Doku aller öffentlichen Member, inkl. der 24 Inventur-Dateien). Jede gestagte `.cs` erfüllt danach den Vollständigkeitscheck; `_Imports.razor` enthält keine UI-Strings → l10n-clean.
   - Hinweis: Dies ist der größte Commit; erst danach dürfen weitere `.cs`-Dateien in `Rezepte.Web` angefasst werden.

3. **Commit 3 — razor-usage-Falschbefunde beheben (inkl. Lokalisierung der betroffenen Dateien)**
   - Voraussetzungen: Commit 2 (Localizer-Infrastruktur + Rezepte.Web.csproj konfiguriert).
   - Beschreibung: BOM-Entfernung + vollständige Lokalisierung der 8 BOM-`@page`-Dateien (`Calendar`, `CookbookDetails`, `CookbookPage`, `Cookbooks`, `Error`, `Home`, `RecipePage`, `Settings`); `Routes.razor` `typeof(MainLayout)`-Fix; `Settings.razor` `@code`-Liste mit den 9 `typeof(...)`-Referenzen + Anpassung `SettingsViewModel.cs`; `MainLayout.razor` lokalisieren (11 Fundstellen) inkl. BOM-Entfernung. Da alle gestagten `.razor` vollständig lokalisiert sein müssen, ist die Lokalisierung dieser Dateien Teil desselben Commits. `razor-usage-check.py --all --strict` muss danach Exit 0 liefern.

4. **Commits 4–6 — Restliche `.razor`-Dateien lokalisieren (thematisch gebatcht)**
   - Voraussetzungen: Commit 2.
   - Beschreibung: Jeweils ein Commit pro Gruppe, jede gestagte Datei vollständig lokalisiert, Schlüssel in `UiStrings.resx`:
     - Commit 4: Restliche Pages — `Login`, `RecipeEdit` (inkl. der „geloescht"-Korrektur), `RecipeSearch`, `Register`, `ScheduledRecipes`, `ShoppingList`.
     - Commit 5: Settings-Komponenten — `AiSettings`, `ApplicationUpdates`, `BackupRestore`, `ExportData`, `ExportFilesList`, `PluginSettings`, `SecurityTxtSettings`, `UsageStats`, `UserAdmin`, `UserProfile` (BOM-Entfernung bei `AiSettings`, `UsageStats`, `UserAdmin` optional, aber empfohlen für Konsistenz — geänderte Dateien sind ohnehin lokalisiert).
     - Commit 6: Shared-Komponenten — `AddRecipeToShoppingListDialog`, `AssignToCookbooksOverlay`, `CalendarEventDialog`, `CreateRecipeDialog`, `ImageCropper`, `LatestRecipes`, `MultiAssignToCookbooksOverlay`, `PhotoOverlay`, `RandomFromCookbooks`, `RecipeSelectDialog`.
   - Danach: `razor-l10n-check.py --all` = Exit 0.

5. **Commit 7 — Testprojekte: Stubs, Enum-Abdeckung, XML-Doku (`Rezepte.Tests`, `Rezepte.Tests.PluginFixture`)**
   - Voraussetzungen: Keine neuen (unabhängig von Commit 2, aber nach 2 sinnvoll wegen Format-Konsistenz).
   - Beschreibung: `Rezepte.Tests.csproj` und `Rezepte.Tests.PluginFixture.csproj` konfigurieren; `dotnet build` beider Projekte → CS1591-Fehler vollständig beheben (Doku aller öffentlichen Member); die 13 throw-only-Stubs umbauen; neue Enum-Coverage-Tests ergänzen (alle fehlenden Werte referenzieren). Danach `no-notimplemented-check --all --strict` und `enum-coverage-check --all --strict` = Exit 0.

6. **Commit 8 — `Rezepte.Tests.Browser`: csproj-Konfiguration + Doku**
   - Voraussetzungen: Keine.
   - Beschreibung: `.csproj` konfigurieren, `ConfiguredRezepteAppFixture.cs`-Doku vervollständigen, CS1591-Buildfehler beheben.

7. **Commit 9 — Übrige Projekte: `Rezepte.Import.Abstractions`, `Rezepte.Import.PluginSdk`, `Rezepte.Import.Plugins.{AIFoto,AIUrl,Backup}`, `Rezepte.Updater.TestHost`**
   - Voraussetzungen: Keine.
   - Beschreibung: Je `.csproj` die XML-Doc-Konfiguration ergänzen und die CS1591-Buildfehler des Projekts beheben. Kann bei kleinem Umfang in einem Commit gebündelt werden (jede gestagte `.cs` zieht nur ihr eigenes Projekt in die Prüfung; gestagte `.csproj` werden direkt geprüft).

8. **Abschluss-Verifikation**
   - `dotnet build Rezepte.sln` fehlerfrei; `dotnet test` (Rezepte.Tests) und `Rezepte.Tests.Browser` grün; `dotnet format Rezepte.sln --verify-no-changes` = 0; alle Check-Skripte einzeln mit `--all` bzw. `--all --strict` ausführen = Exit 0; finaler `git commit`/`git push` durchläuft beide Hooks ohne Blocker.

## Tests

### Neue Tests

| Test / Hilfsmethode | Testklasse | Was wird geprüft / bereitgestellt? |
|--------------------|------------|-------------------------------------|
| Enum-Wert-Abdeckung `ImportCollectionItemState` | neue/bestehende Testklasse in `Rezepte.Tests` | `Pending` und `Importing` werden in Testcode referenziert (z. B. Iteration über `Enum.GetValues` mit Verhalten-Assert) |
| Enum-Wert-Abdeckung `WeekDays` | Testklasse in `Rezepte.Tests` (z. B. Kalender-/Recurrence-Tests) | `Tuesday` und `Saturday` werden referenziert (z. B. wöchentliche Serientermin-Berechnung über alle Wochentage) |
| Enum-Wert-Abdeckung `BackgroundJobStatus` | Testklasse in `Rezepte.Tests` (z. B. Job-Status-Tests) | `Running`, `Failed`, `Cancelled` werden referenziert (z. B. Statusübergänge/Darstellungsmapping) |

### Betroffene bestehende Tests

| Test / Testklasse | Grund der Anpassung |
|-------------------|---------------------|
| `TestImportPlugin.CheckUsabilityAsync` (`Rezepte.Tests.PluginFixture`) | Throw-only-Body verboten → `Task.FromException`-Umbau, Semantik identisch |
| `ApplicationUpdatePreInstallHandlerTests` (`FailingBackupService`, `FailingPreInstallHandler`) | Throw-only-Bodies umbauen |
| `UpdateBackupServiceTests` (`RecordingExportService`, `FailingExportService`) | Throw-only-Bodies umbauen |
| `ImportOrchestratorTests` (`ThrowingHandler`, `CancellingStream`) | Throw-only-Body + fehlende XML-Doku |
| `PluginUpdateServiceTests` (`FailingPackageInstaller`) | Throw-only-Body umbauen |
| Ggf. `SettingsViewModel`-Tests | Komponentenliste wandert nach `Settings.razor` |
| Browser-Tests (`Rezepte.Tests.Browser`) | Nur betroffen, falls Assertions auf deutschen UI-Texten von den resx-Texten abweichen — Texte 1:1 übernehmen, um Brüche zu vermeiden |

### E2E-Tests (primärer Funktionsnachweis)

Keine neuen E2E-Tests erforderlich: Die Anforderung führt keinen neuen oder geänderten Benutzerfluss ein — sie betrifft ausschließlich Qualitätsprüfungen und deren Befundbehebung. Die Lokalisierung ersetzt Texte 1:1 (neutrale resx mit den bisherigen deutschen Strings), sodass kein anwendersichtbares Verhalten entsteht, das einen neuen E2E-Nachweis erfordert. Als Regressionssicherung ist der Lauf der bestehenden `Rezepte.Tests.Browser`-Suite im Verifikationsplan eingeplant; schlagen dort textbasierte Assertions fehl, ist das ein Indikator für abweichende resx-Texte und zu korrigieren.

| Test / Testklasse | Grund der Anpassung |
|-------------------|---------------------|
| `Rezepte.Tests.Browser` (gesamte Suite) | Nur bei versehentlich geänderten UI-Texten — ansonsten unverändert, Lauf als Regressionstest eingeplant |

## Offene Punkte

| # | Offener Punkt | Empfohlener Vorschlag |
|---|---------------|----------------------|
| 1 | Umfang der Lokalisierung: Die Anforderung verlangt, „alle Verstöße" zu korrigieren — `razor-l10n` läuft im `pre-commit` aber nur über gestagte Dateien. Sollen alle 35 gemeldeten `.razor`-Dateien lokalisiert werden (strenge Lesart) oder nur die ohnehin anzufassenden (~11 für razor-usage/BOM)? | Strenge Lesart: alle 35 Dateien lokalisieren (Commit-Batches 4–6). Der `--all`-Lauf bleibt sonst dauerhaft rot und jede spätere Änderung an einer unlokalisierten Datei blockiert den Commit. Falls der Anwender den Umfang begrenzen will, entfallen Commits 4–6. |
| 2 | Sprachumfang der `UiStrings.resx`: nur neutrale Datei mit deutschen Texten oder zusätzlich `UiStrings.en.resx` o. ä.? | Nur neutral (deutsche Texte). Mehrsprachigkeit ist nicht angefordert; jede weitere Kulturdatei muss per `translation-check` schlüsselidentisch gepflegt werden. |
| 3 | Naming des injizierten Localizers: `Localizer` (Schlüssel werden von `translation-check` gegen `.resx` validiert, da der Regex nur `*localizer`-Namen matcht) vs. `L` (kürzer, aber der Check sieht die Schlüssel nicht → fehlende resx-Einträge würden erst zur Laufzeit als Schlüsselname sichtbar). | `Localizer` — die Validierung ist gewünscht und verhindert stillschweigend fehlende Schlüssel. |
| 4 | CS1591-Volldoku in `Rezepte.Web`: Der tatsächliche Umfang undokumentierter öffentlicher Member ist erst nach der `.csproj`-Konfiguration per Build ermittelbar und kann erheblich sein. Eine Reduktion ist ohne Check-Änderung nicht möglich (verboten); `NoWarn`/`WarningsNotAsErrors` werden vom Check ebenfalls als Verstoß gemeldet. | Umfang in Commit 2 per `dotnet build` ermitteln und vollständig dokumentieren; bei unerwartet großem Umfang den Anwender informieren, bevor weitergemacht wird. Kein Vorschlag zur Umgehung möglich. |
| 5 | Ob die fachlich reinen Hilfsprojekte (`Rezepte.Import.*`, `Rezepte.Updater.TestHost`) ebenfalls CS1591-konfiguriert werden sollen, obwohl dort keine `.cs`-Änderungen anstehen — der `--all`-Inventurlauf meldet sie, der `pre-commit` würde sie nur bei zukünftigen `.cs`-Edits einfordern. | Konfigurieren (strenge Lesart der Anforderung „alle Verstöße"), gebündelt in Commit 9; der Dokumentationsaufwand in den schlanken Plugin-Projekten ist überschaubar. |
