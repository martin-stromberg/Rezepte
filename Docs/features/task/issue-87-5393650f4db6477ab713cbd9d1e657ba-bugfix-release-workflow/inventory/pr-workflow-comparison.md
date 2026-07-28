# Detail: PR-Workflow und CI-Vergleich

## Datei

`.github/workflows/pr.yml`

## Relevante Struktur

- Trigger: `pull_request` auf `main`, Typen `opened`, `synchronize`, `reopened`.
- Job: `verify`, `runs-on: ubuntu-latest`, `timeout-minutes: 30`.
- .NET Setup: `actions/setup-dotnet@v4` mit `dotnet-version: 10.0.x`.

## Testvorbereitung im PR-Workflow

Der PR-Workflow fuehrt vor `dotnet test` diese Schritte aus:

- Restore der Solution.
- Build von `Rezepte.Web`.
- Build von `Rezepte.Tests`.
- Build von `Rezepte.Tests.Browser`.
- Installation der Playwright-Browser:

```bash
PLAYWRIGHT_SCRIPT=$(find Rezepte.Tests.Browser/bin/Release -name "playwright.ps1" | head -n 1)
if [ -z "$PLAYWRIGHT_SCRIPT" ]; then
  echo "playwright.ps1 not found under Rezepte.Tests.Browser/bin/Release - did the browser test build succeed?" >&2
  exit 1
fi
pwsh "$PLAYWRIGHT_SCRIPT" install --with-deps chromium
```

- Publish von `Rezepte.Web`:

```bash
dotnet publish Rezepte.Web/Rezepte.Web.csproj --configuration Release --no-restore
```

- Anschliessend Testlauf:

```bash
dotnet test Rezepte.sln --configuration Release --no-build
```

## Abweichungen zum Release-Workflow

| Bereich | PR-Workflow | Release-Workflow |
|---------|-------------|------------------|
| Browser-Testprojekt bauen | Ja | Nein |
| Playwright Chromium installieren | Ja | Nein |
| Web-App vor Browser-Tests publishen | Ja | Nein |
| Testaufruf | `dotnet test Rezepte.sln --configuration Release --no-build` | gleich |

## Bedeutung fuer die Anforderung

Der PR-Workflow zeigt bereits die fuer Browser-Tests benoetigte Abfolge. Der Release-Workflow nutzt denselben Solution-Testaufruf, stellt aber nicht dieselben Voraussetzungen her. Damit ist die naheliegendste Korrektur, den Release-Testbereich funktional an den PR-Testbereich anzugleichen oder die Tests im Release-Workflow explizit pro Testprojekt zu starten.

## Dokumentationshinweis

`Docs/help/github-actions.md` beschreibt die PR-Pruefung noch mit `dotnet build Rezepte.sln --configuration Release --no-restore`. Die aktuelle `pr.yml` nutzt stattdessen einzelne Build-Schritte inklusive Browser-Testbuild und Playwright-Installation. Eine reine Bugfix-Umsetzung muss diese Dokumentation nicht zwingend aendern, aber ein spaeterer Dokumentationsschritt sollte die Abweichung beruecksichtigen.
