# Anforderungsübersetzung

## Metadaten

- Aufgaben-ID: `6c3461c1-d9ff-4d0b-bd8a-49a16a64aae8`
- Branch: `task/issue-154-6c3461c1d9ff4d0bbd8a49a16a64aae8-githooks`
- Thema: Übernahme der Git-Hooks aus dem Repository `Pattern-Collection` und Behebung der dadurch ausgelösten Prüffehler in der Anwendung

## Fachliche Zusammenfassung

Die Git-Hooks aus dem Referenz-Repository `https://github.com/martin-stromberg/Pattern-Collection.git` (Verzeichnis `Git-Hooks/githooks/`) wurden in das aktuelle Repository nach `.githooks/` übernommen und über `git config core.hooksPath .githooks` aktiviert. Diese Hooks führen bei `git commit` und `git push` automatisierte Qualitätsprüfungen gegen den Codebase aus. Alle Verstöße, die diese Prüfungen im vorhandenen Anwendungscode aufdecken, sind in der Anwendung zu korrigieren; die Prüfungen selbst (Hook-Skripte und Check-Logik) dürfen nicht abgeschwächt, umgangen oder entfernt werden.

## Betroffene Klassen und Komponenten

Neu übernommene Artefakte (bereits im Repository vorhanden, Verzeichnis `.githooks/`):

- `pre-commit` – blockiert Commits auf `main`/`staging` und führt folgende Prüfungen aus:
  - `translation-check.py` (blockierend) – prüft Übersetzungs-/Lokalisierungsvollständigkeit
  - `csproj-xmldoc-check.py` (blockierend) – prüft XML-Dokumentation in `.csproj`- und `.cs`-Dateien
  - `razor-l10n-check.py` (blockierend) – prüft Lokalisierung in Razor-Komponenten
  - `razor-usage-check.py` (im pre-commit nur warnend)
  - `no-notimplemented-check.py` (im pre-commit nur warnend) – erkennt `NotImplementedException`/Stubs
  - `enum-coverage-check.py` (im pre-commit nur warnend) – prüft Enum-Abdeckung (z. B. in `switch`-Ausdrücken/Mappings)
  - `SecretScan.csproj` und `MarkdownLinkCheck.csproj` (sofern im Repo gefunden, sonst übersprungen)
  - `dotnet format <solution> --verify-no-changes --no-restore` (aus dem bisherigen pre-commit übernommen, blockierend)
  - `check-encoding.ps1 -Staged` (aus dem bisherigen pre-commit übernommen, blockierend)
- `pre-push` – blockiert direkte Pushes auf `main`/`staging` und führt repo-weit blockierend aus:
  - `no-notimplemented-check.py --all --strict`
  - `razor-usage-check.py --all --strict`
  - `enum-coverage-check.py --all --strict`
- `install-hooks.cmd` / `install-hooks.sh` – Installationsskripte, die `core.hooksPath` auf `.githooks` setzen

Voraussichtlich zu ändernde Anwendungsartefakte (abhängig von den tatsächlich gemeldeten Verstößen):

- C#-Quellcode in den Projekten der `Rezepte.sln` (u. a. `Rezepte.Web`, `Rezepte.Import.*`, `Rezepte.Tests*`), z. B.:
  - Klassen/Methoden mit fehlender oder fehlerhafter XML-Dokumentation (`/// <summary>` etc.)
  - Stellen mit `throw new NotImplementedException(...)` oder sonstigen Stub-Implementierungen
  - `switch`-Ausdrücke/Mappings mit unvollständiger Enum-Abdeckung
- `.csproj`-Dateien mit fehlenden XML-Dokumentations-Einstellungen (z. B. `GenerateDocumentationFile`, Dokumentations-Kommentare gemäß `csproj-xmldoc-check.py`)
- Razor-Komponenten (`*.razor`, `*.razor.cs`) mit Lokalisierungsverstößen (hartkodierte, nicht über den Localizer aufgelöste Benutzertexte) und Verstößen gegen die Razor-Usage-Regeln
- Lokalisierungs-/Ressourcendateien (z. B. `.resx`), falls `translation-check.py` fehlende Übersetzungsschlüssel meldet
- Dateien mit Formatierungsverstößen (`dotnet format`) oder fehlerhafter Kodierung (`check-encoding.ps1`)

Konkrete Klassen- und Dateinamen können erst nach Ausführung der Prüfungen (Inventur-Schritt) benannt werden; die Liste oben beschreibt die betroffenen Artefakttypen.

## Implementierungsansatz

1. **Ausgangszustand verifiziert:** Die Hooks wurden bereits aus `Git-Hooks/githooks/` des Pattern-Collection-Repos nach `.githooks/` kopiert; `git config core.hooksPath` zeigt auf `.githooks`. Der bisherige pre-commit (`dotnet format` + `check-encoding.ps1`) wurde in den neuen `pre-commit` integriert, sodass keine bestehende Prüfung entfällt.
2. **Prüfungen ausführen und Befunde sammeln:** Jede der neuen Check-Skripte (`translation-check.py`, `csproj-xmldoc-check.py`, `razor-l10n-check.py`, `razor-usage-check.py`, `no-notimplemented-check.py`, `enum-coverage-check.py`) sowie die pre-push-Prüfungen im `--all --strict`-Modus gegen das Repository laufen lassen und die vollständige Fehlerliste erfassen (Inventur).
3. **Fehler im Anwendungscode beheben:** Für jeden gemeldeten Verstoß die Anwendung anpassen, z. B. XML-Dokumentation ergänzen, `NotImplementedException`-Stubs durch echte Implementierungen oder begründete Alternativen ersetzen, fehlende Enum-Fälle ergänzen, hartkodierte UI-Texte über den bestehenden Lokalisierungsmechanismus (Localizer/`.resx`) auflösen, fehlende Übersetzungsschlüssel ergänzen.
4. **Bestehende Prüfungen weiterhin erfüllen:** `dotnet format --verify-no-changes` und `check-encoding.ps1` müssen weiterhin fehlerfrei durchlaufen; Formatierungs- und Kodierungsverstöße werden ebenfalls behoben.
5. **Absicherung:** Solution bauen und die Testsuite (`Rezepte.Tests`, ggf. `Rezepte.Tests.Browser`) ausführen, um Regressionen durch die Korrekturen auszuschließen.
6. **Verifikation:** Ein `git commit` und die pre-push-Prüfungen müssen ohne blockierende Fehlermeldung durchlaufen.

Erweiterungspunkte/Abhängigkeiten: Es werden keine neuen Interfaces oder Services benötigt; relevant sind ausschließlich die Hook-Skripte als Ausführungseinstiegspunkte sowie der bestehende Lokalisierungsmechanismus der Anwendung. `python`, `dotnet` und `powershell.exe` sind Laufzeitabhängigkeiten der Hooks. Falls `SecretScan.csproj`/`MarkdownLinkCheck.csproj` im Repository nicht vorhanden sind, werden diese Prüfungen vom Hook übersprungen (Annahme: kein Handlungsbedarf, solange die Prüfung nicht aktiv fehlschlägt).

## Konfiguration

- Die Hook-Aktivierung erfolgt repository-lokal über `git config core.hooksPath .githooks` (bereits gesetzt; für andere Entwickler über `install-hooks.cmd` bzw. `install-hooks.sh` reproduzierbar).
- Die Anforderung verlangt keine anwendungsseitige Konfigurierbarkeit; die Prüfungen sind fest verdrahtet und dürfen nicht konfigurierbar abschaltbar gemacht werden.

## Offene Fragen

- Konkrete Fehlerliste: Welche Verstöße melden die Prüfungen tatsächlich? (Wird im Inventur-Schritt ermittelt, ist vor der Implementierung zu erfassen.)
- Sind im Pattern-Collection-Repo noch weitere Hook-relevante Dateien enthalten (z. B. `SecretScan.csproj`, `MarkdownLinkCheck.csproj` oder Konfigurationsdateien für die Python-Checks), die hier nicht mitkopiert wurden und ggf. benötigt werden?
- Gilt der Branch-Blocker (`main`/`staging`) für dieses Repository wie gewünscht, da hier `main` verwendet wird, `staging` aber ggf. nicht existiert? (Annahme: Verhalten ist korrekt, da die Branch-Namen aus dem Quell-Repo übernommen wurden.)
- Für `no-notimplemented-check.py --all --strict` und `razor-usage-check.py --all --strict`: Falls bestehender Code legitime `NotImplementedException`-Würfe enthält (z. B. in Serialisierungs-Fallbacks), ist zu klären, ob diese als Verstoß gelten oder durch eine vom Check vorgesehene Ausnahmemarkierung (z. B. Kommentar/Attribut, sofern das Skript eine solche unterstützt) behandelt werden dürfen.
