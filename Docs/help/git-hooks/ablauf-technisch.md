← [Zurück zur Übersicht](../index.md)

# Git-Hooks — Technischer Ablauf

## Übersicht

Vor einem Commit werden blockierende Prüfungen auf gestagten Dateien ausgeführt. Vor einem Push werden zusätzliche blockierende Prüfungen auf dem gesamten Repository ausgeführt. Beide Hooks sperren `main` und `staging`.

## Ablauf

### 1. Branch-Blocker

`pre-commit` und `pre-push` ermitteln den aktuellen Branch. Commits und Pushes auf `main` oder `staging` werden mit Fehlermeldung abgelehnt.

### 2. pre-commit — gestagte Dateien

Beteiligte Komponenten:

- `.githooks/pre-commit` — Einstiegspunkt.
- `.githooks/translation-check.py` — prüft RESX-Header, Paketkonsistenz und `Localizer`-Schlüssel gegen `.resx`.
- `.githooks/csproj-xmldoc-check.py` — prüft `GenerateDocumentationFile` und `CS1591`-Fehler in `.csproj` sowie Vollständigkeit der XML-Doku in gestagten `.cs`.
- `.githooks/razor-l10n-check.py` — prüft, dass gestagte `.razor`-Dateien keine hartkodierten UI-Strings enthalten.
- `.githooks/razor-usage-check.py` — warnt bei falscher Razor-Komponentenverwendung (im pre-commit nicht blockierend).
- `.githooks/no-notimplemented-check.py` — warnt bei `throw`-only Membern (im pre-commit nicht blockierend).
- `.githooks/enum-coverage-check.py` — warnt bei Enums, die in Tests nicht ausreichend abgedeckt sind (im pre-commit nicht blockierend).
- `.githooks/check-encoding.ps1 -Staged` — prüft Kodierung (BOM, ASCII-Ersatzschreibungen).
- `dotnet format <solution> --verify-no-changes --no-restore` — prüft Formatierung.

### 3. pre-push — Repository-weit

Beteiligte Komponenten:

- `.githooks/pre-push` — Einstiegspunkt.
- `.githooks/no-notimplemented-check.py --all --strict` — blockierend: keine `throw`-only Member.
- `.githooks/razor-usage-check.py --all --strict` — blockierend: alle `@page`-Dateien und `typeof(...)`-Referenzen korrekt.
- `.githooks/enum-coverage-check.py --all --strict` — blockierend: jeder Enum-Wert mindestens einmal in Tests referenziert.

## Fehlerbehandlung

Wenn ein Check fehlschlägt, wird der Prozess mit einem Exit-Code ungleich 0 beendet und die Fehlermeldungen werden auf der Konsole ausgegeben.
