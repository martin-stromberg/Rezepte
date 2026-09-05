# Detail: Check-Ergebnisse (tatsächlich ausgeführt)

Ausgeführt am Repo-Root `D:\Repositories\softwareschmiede\6c3461c1-d9ff-4d0b-bd8a-49a16a64aae8`, jeweils mit den im pre-push/Inventur relevanten Flags.

| Prüfung | Kommando | Exit | Ergebnis |
|---------|----------|------|----------|
| translation-check | `python .githooks/translation-check.py --all` | 0 | „No .resx files found; nothing to check." Keine `.resx` im Repo, keine `IStringLocalizer`-Nutzung |
| csproj-xmldoc-check | `python .githooks/csproj-xmldoc-check.py --all` | 1 | 24 `.cs`-Dateien mit unvollständiger XML-Doku + 10 `.csproj` ohne `GenerateDocumentationFile`/CS1591-Konfiguration → [xmldoc.md](xmldoc.md) |
| razor-l10n-check | `python .githooks/razor-l10n-check.py --all` | 1 | 35 `.razor`-Dateien mit hartkodierten UI-Strings → [razor-l10n.md](razor-l10n.md) |
| no-notimplemented-check | `python .githooks/no-notimplemented-check.py --all --strict` | 1 | 13 throw-only-Stubs in 5 Testdateien → [notimplemented.md](notimplemented.md) |
| razor-usage-check | `python .githooks/razor-usage-check.py --all --strict` | 1 | 18 „verwaiste" Komponenten — alle falsch-positiv → [razor-usage.md](razor-usage.md) |
| enum-coverage-check | `python .githooks/enum-coverage-check.py --all --strict` | 1 | 3 Enums mit unabgedeckten Werten → [enum-coverage.md](enum-coverage.md) |
| check-encoding | `powershell -NoProfile -ExecutionPolicy Bypass -File .githooks/check-encoding.ps1` | 0 | „Encoding check passed." (repo-weit, `git ls-files`) |

## Zusätzlich verifiziert

- `git ls-files '*.resx'` → 0 Treffer. `IStringLocalizer` kommt nur in `translation-check.py` (als Regex) vor — es existiert **keine Lokalisierungsinfrastruktur** in der Anwendung.
- „geloescht" in `RecipeEdit.razor` ist bereits zu „gelöscht" korrigiert und **gestaged** (Zeilen 384, 392). Zeile 374 enthält ebenfalls „gelöscht" (unverändert, korrekt).
- `dotnet format Rezepte.sln --verify-no-changes --no-restore` wurde **nicht** ausgeführt (läuft erst beim Commit).
