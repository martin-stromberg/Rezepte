# Detail: Hook-Ist-Zustand

## Verzeichnis `.githooks/` (Repo-Root)

| Datei | Zweck |
|-------|-------|
| `pre-commit` | Blockiert Commits auf `main`/`staging`; führt blockierend `translation-check.py`, `csproj-xmldoc-check.py`, `razor-l10n-check.py`, `dotnet format <sln> --verify-no-changes --no-restore`, `check-encoding.ps1 -Staged` aus; warnend `razor-usage-check.py`, `no-notimplemented-check.py`, `enum-coverage-check.py`; sucht dynamisch `SecretScan.csproj`/`MarkdownLinkCheck.csproj` (Zeilen 56–71) und überspringt sie, wenn nicht gefunden |
| `pre-push` | Blockiert direkte Pushes auf `main`/`staging`; führt blockierend `no-notimplemented-check.py --all --strict`, `razor-usage-check.py --all --strict`, `enum-coverage-check.py --all --strict` aus |
| `translation-check.py` | Scannt `.cs`/`.razor`/`.cshtml` nach `IStringLocalizer`-Indexer-Verwendungen und prüft Schlüssel gegen `.resx`. Ohne `.resx`: „nothing to check" |
| `csproj-xmldoc-check.py` | Prüft `///`-Dokumentation (fehlende `<param>`, `<returns>`) in `.cs` und `GenerateDocumentationFile` + CS1591-als-Fehler in `.csproj` |
| `razor-l10n-check.py` | Meldet hartkodierte UI-Strings in `.razor` (Attribute `title`, `placeholder`, `alt`, `aria-label`, `label`, `tooltip` sowie mehrwortige Textknoten). Überspringt `@code {}`-Blöcke und `@* ... *@`-/HTML-Kommentare. **Kein Unterdrückungsmechanismus** |
| `razor-usage-check.py` | Meldet .razor-Dateien ohne `@page`, die in keiner anderen `.razor`-Datei als Tag, `@layout` oder `typeof(Name)` referenziert sind. Durchsucht **nur `.razor`-Dateien** (Funktion `all_razor_files`, Zeilen 55–61, 108–113). Ausnahmen: `App.razor`, `Routes.razor`, Dateien mit führendem `_`, Dateien mit `^\s*@page` |
| `no-notimplemented-check.py` | Verbietet `NotImplementedException` sowie Methoden/Properties/Accessoren, deren gesamter Body ein einzelnes `throw`-Statement ist (beliebiger Exception-Typ). Kommentare und String-Literale werden vor dem Match entfernt. **Keine Ausnahmemarkierung vorgesehen** |
| `enum-coverage-check.py` | Verlangt, dass jeder `public`/`internal` Enum-Wert in mindestens einer Testdatei vorkommt |
| `check-encoding.ps1` | Prüft UTF-8-Validität, U+FFFD, Mojibake-Muster und eine Liste von ~160 ASCII-Transliterierungen (u. a. `geloescht`, `fuer`, `koennen`). Parameter `-Staged` für pre-commit, sonst `git ls-files` repo-weit |
| `install-hooks.cmd` / `install-hooks.sh` | Setzen `git config core.hooksPath .githooks` |

## Verifizierte Konfiguration

- `git config core.hooksPath` → `.githooks` (gesetzt).
- Aktueller Branch: `task/issue-154-6c3461c1d9ff4d0bbd8a49a16a64aae8-githooks`.
- `git status` (gestaged): alle `.githooks/*` neu (`A`), `pre-commit` geändert (`M`), `CLAUDE.md` (`M`, +„Fact-Based Work"-Regel, Zeilen 8–11), `Rezepte.Web/Components/Pages/RecipeEdit.razor` (`M`, „geloescht"→„gelöscht" in Zeilen 384/392). Ungestaged/untracked: `.agents/`, `AGENTS.md`, `Docs/features/task/issue-154-.../`.
- `Rezepte.Web/Extensions/LoggingExtensions.cs` zeigt **keine** Modifikation in `git status` — die erwähnte `dotnet format`-Änderung ist derzeit nicht als Diff vorhanden.
- Solution-Datei: `Rezepte.sln` im Repo-Root (wird vom pre-commit für `dotnet format` verwendet).

## Vergleich mit Quell-Repo Pattern-Collection

Clone: `C:\Users\Martin\AppData\Local\Temp\pattern-collection` (von `https://github.com/martin-stromberg/Pattern-Collection.git`).

- `Git-Hooks/githooks/` enthält exakt: `csproj-xmldoc-check.py`, `enum-coverage-check.py`, `install-hooks.cmd`, `install-hooks.sh`, `no-notimplemented-check.py`, `pre-commit`, `pre-push`, `razor-l10n-check.py`, `razor-usage-check.py`, `translation-check.py` — alle hier vorhanden.
- `check-encoding.ps1` stammt aus dem bisherigen Repo-Hook (nicht aus Pattern-Collection).
- **`SecretScan.csproj` und `MarkdownLinkCheck.csproj` existieren im Quell-Repo nicht** — der pre-commit überspringt diese Prüfungen (`find` liefert nichts → „not found – skipping"). Kein Handlungsbedarf.
- Weitere Dateien im Quell-Repo (`readme.md`, `Git-Hooks/readme.md`, `CI-Workflows/instructions.md`) sind hook-irrelevant.
