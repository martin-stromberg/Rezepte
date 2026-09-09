# Tests und Qualitätssicherung

## Tests ausführen

```powershell
dotnet test
```

`Rezepte.Tests` deckt zentrale Services wie Benutzer, Kochbücher, Rezepte, Einstellungen und KI-Nutzung ab. `Rezepte.Tests.Browser` enthält Browser-/E2E-Tests (u. a. für den security.txt-Flow inklusive Admin-UI und öffentlichen Endpunkten).

## Browser-Tests

Für Browser-Tests muss zusätzlich Playwright-Chromium installiert sein:

```powershell
dotnet build Rezepte.Tests.Browser/Rezepte.Tests.Browser.csproj -c Release
pwsh (Get-ChildItem Rezepte.Tests.Browser/bin/Release -Recurse -Filter playwright.ps1 | Select-Object -First 1 -ExpandProperty FullName) install --with-deps chromium
dotnet test Rezepte.Tests.Browser/Rezepte.Tests.Browser.csproj -c Release --no-build
```

## NuGet-Sicherheitscheck

Vor Pull Requests mit Paketänderungen sollte der vollständige transitive NuGet-Sicherheitscheck ausgeführt werden:

```powershell
dotnet list Rezepte.sln package --vulnerable --include-transitive
```

Der Check ist in der CI ein blockierendes Gate. Die aktuelle Auflösung und der Sicherheitsnachweis für die Dependencies stehen in [Docs/dependencies.md](dependencies.md); CI-Ablauf und Fehlerbehebung sind in [Docs/help/github-actions.md](help/github-actions.md) dokumentiert.

## Git-Hooks

Das Repository enthält unter `.githooks/` Git-Hooks, die automatisierte Qualitätsprüfungen vor jedem Commit und Push ausführen. Sie sperren direkte Änderungen auf `main` und `staging` und verhindern, dass unformatierter, unlokalisierter oder unzureichend dokumentierter Code eingecheckt wird.

Aktivierung nach dem Klonen:

```powershell
# Windows
install-hooks.cmd

# Linux/macOS
./install-hooks.sh
```

Das Skript führt `git config --local core.hooksPath .githooks` aus. Danach zeigt `git config --local core.hooksPath` auf `.githooks`.

`pre-commit` prüft gestagte Dateien u. a. mit `translation-check.py`, `csproj-xmldoc-check.py`, `razor-l10n-check.py`, `dotnet format --verify-no-changes` und `check-encoding.ps1 -Staged`. `pre-push` läuft repo-weit mit `no-notimplemented-check.py --all --strict`, `razor-usage-check.py --all --strict` und `enum-coverage-check.py --all --strict`.

Alle Projekte der Solution sind so konfiguriert, dass fehlende XML-Dokumentation öffentlicher Member als Build-Fehler behandelt wird (`<GenerateDocumentationFile>true</GenerateDocumentationFile>` und `<WarningsAsErrors>CS1591</WarningsAsErrors>`). UI-Texte in Razor-Komponenten werden über `IStringLocalizer<UiStrings>` aus `Rezepte.Web/Resources/UiStrings.resx` aufgelöst.

Details stehen in [Docs/help/git-hooks/index.md](help/git-hooks/index.md).
