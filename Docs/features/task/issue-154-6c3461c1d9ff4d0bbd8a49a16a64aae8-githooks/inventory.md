# Bestandsaufnahme: Git-Hooks aus Pattern-Collection und ausgelöste Prüffehler

Bestandsaufnahme zum Ist-Zustand des Repositories bezogen auf die Anforderung `requirement.md` (Übernahme der Git-Hooks aus `Pattern-Collection` und Behebung der dadurch ausgelösten Prüffehler). Alle sieben Prüfungen wurden tatsächlich gegen das Repo ausgeführt (Repo-Root: `D:\Repositories\softwareschmiede\6c3461c1-d9ff-4d0b-bd8a-49a16a64aae8`).

## Zusammenfassung

- **Hooks sind vollständig übernommen und aktiv:** `.githooks/` enthält `pre-commit`, `pre-push`, 6 Python-Checks, `check-encoding.ps1` und die beiden Install-Skripte; `git config core.hooksPath` = `.githooks` (verifiziert). Alle neuen Dateien sind bereits **gestaged** (`git status`: `A`/ `M` im Index).
- **Aktueller Git-Zustand:** Branch `task/issue-154-6c3461c1d9ff4d0bbd8a49a16a64aae8-githooks`. Gestaged sind außer den Hooks: `CLAUDE.md` (+ „Fact-Based Work"-Regel) und `Rezepte.Web/Components/Pages/RecipeEdit.razor` („geloescht" → „gelöscht", Zeilen 384/392, verifiziert per `git diff --cached`). `Rezepte.Web/Extensions/LoggingExtensions.cs` ist **nicht verändert** (kein Eintrag in `git status` — die behauptete `dotnet format`-Änderung existiert aktuell nicht als Diff).
- **Check-Ergebnisse (`--all`, blockierende Exit-Codes):**
  - `translation-check.py --all` → **OK** (Exit 0): „No .resx files found; nothing to check." Es existiert **keine** `.resx`-Datei und keine `IStringLocalizer`-Infrastruktur im Repo (verifiziert: `git ls-files '*.resx'` = 0, Grep findet `IStringLocalizer` nur in `translation-check.py` selbst).
  - `csproj-xmldoc-check.py --all` → **Fehlgeschlagen** (Exit 1): 24 `.cs`-Dateien mit unvollständiger XML-Dokumentation (fehlende `<param>`/`<returns>`-Tags) + **10 `.csproj`-Dateien** ohne `<GenerateDocumentationFile>true</GenerateDocumentationFile>` und ohne CS1591-als-Fehler-Konfiguration.
  - `razor-l10n-check.py --all` → **Fehlgeschlagen** (Exit 1): **35 `.razor`-Dateien** mit hartkodierten UI-Strings (title/placeholder/alt/aria-label/Textknoten). Der Check empfiehlt `@L["Schlüssel"]`, bietet aber **keinen** Unterdrückungs-/Opt-out-Mechanismus — und es existiert kein `L`-Localizer im Projekt.
  - `no-notimplemented-check.py --all --strict` → **Fehlgeschlagen** (Exit 1): **13 Fundstellen in 5 Dateien, alle in Testprojekten**. Keine `NotImplementedException`, sondern throw-only-Stubs in Test-Fakes (`InvalidOperationException`, `NotSupportedException`, `PluginPackageInstallException`). Der Check hat keine Ausnahmemarkierung.
  - `razor-usage-check.py --all --strict` → **Fehlgeschlagen** (Exit 1): **18 „verwaiste" .razor-Dateien — sämtlich falsch-positive Befunde** (Analyse unten).
  - `enum-coverage-check.py --all --strict` → **Fehlgeschlagen** (Exit 1): 3 Enums mit in Tests nicht abgedeckten Werten (`ImportCollectionItemState`, `WeekDays`, `BackgroundJobStatus`).
  - `check-encoding.ps1` (gesamtes Repo) → **OK** (Exit 0): „Encoding check passed." (Das Skript prüft zusätzlich ASCII-Transliterierungen wie „geloescht" — die Korrektur in `RecipeEdit.razor` war nötig und ist gestaged.)
- **razor-usage-Fehlbefunde im Detail (verifiziert):**
  - **12 der 18** Dateien haben eine UTF-8-BOM. Der Check liest mit `encoding='utf-8'`, die BOM bleibt als `\ufeff` stehen; `\ufeff` ist in Python **kein** Whitespace, daher matcht `^\s*@page` nicht → 8 `@page`-Dateien (`Calendar`, `CookbookDetails`, `CookbookPage`, `Cookbooks`, `Error`, `Home`, `RecipePage`, `Settings`) werden fälschlich als verwaist gemeldet. (BOM verifiziert per Byte-Read.)
  - `MainLayout.razor` (BOM, kein `@page`) wird in `Routes.razor` Zeile 5 als `DefaultLayout="typeof(Layout.MainLayout)"` referenziert — das Check-Pattern `typeof\s*\(\s*MainLayout\s*\)` matcht den qualifizierten Namen nicht.
  - Die **9 Settings-Komponenten** (`UserProfile`, `AiSettings`, `UserAdmin`, `PluginSettings`, `ApplicationUpdates`, `SecurityTxtSettings`, `ExportData`, `BackupRestore`, `UsageStats`) werden per `typeof(...)` ausschließlich in `Rezepte.Web/ViewModels/SettingsViewModel.cs` (Zeilen 33–41) referenziert — der Check durchsucht nur `.razor`-Dateien (`all_razor_files` / `file_contents`).
  - Da die Hook-Skripte laut Anforderung nicht abgeschwächt werden dürfen, müssen diese Befunde **anwendungsseitig** aufgelöst werden (BOM entfernen fällt wegen Check-Logik nichts — die Dateien müssen stattdessen so referenziert/umbenannt werden, dass der Check sie findet, bzw. `@page`-Erkennung funktioniert nach BOM-Entfernung; Verbot gilt für die Check-Logik, nicht für Repo-Dateien).
- **Pattern-Collection-Vergleich (verifiziert, Clone `C:\Users\Martin\AppData\Local\Temp\pattern-collection`):** Das Quell-Repo enthält unter `Git-Hooks/githooks/` exakt dieselben Dateien wie hier vorhanden. **`SecretScan.csproj` und `MarkdownLinkCheck.csproj` existieren dort nicht** — der `pre-commit` sucht sie dynamisch per `find` und überspringt sie, wenn nicht gefunden (Zeilen 56–71). Kein fehlendes Artefakt.
- **Nicht ausgeführt:** `dotnet format Rezepte.sln --verify-no-changes --no-restore` (läuft erst im `pre-commit`-Kontext; in dieser Inventur nicht gestartet).

## Details

- [Hook-Ist-Zustand und Ausführungslogik](inventory/hooks-ist-zustand.md) — Inhalt von `.githooks/`, pre-commit/pre-push-Ablauf, Vergleich mit Pattern-Collection, Git-Status.
- [Check-Ergebnisse: Übersicht](inventory/check-ergebnisse.md) — alle 7 Prüfungen mit Exit-Code und Befundmenge.
- [XML-Dokumentation / .csproj](inventory/xmldoc.md) — vollständige Liste der 24 `.cs`- und 10 `.csproj`-Verstöße.
- [Razor-Lokalisierung](inventory/razor-l10n.md) — alle 33 betroffenen `.razor`-Dateien mit Zeilen und Fundtexten.
- [Razor-Usage (verwaiste Komponenten)](inventory/razor-usage.md) — 18 Dateien, BOM-/Referenzanalyse, Falsch-Positiv-Begründung.
- [NotImplemented/Stub-Check](inventory/notimplemented.md) — 13 Fundstellen in Test-Fakes.
- [Enum-Abdeckung](inventory/enum-coverage.md) — 3 Enums, fehlende Werte.
