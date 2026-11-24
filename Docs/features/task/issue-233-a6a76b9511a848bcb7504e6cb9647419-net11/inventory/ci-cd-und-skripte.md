# CI/CD, Composite Actions und Skripte

Bestandsaufnahme der Workflow- und Skript-Dateien mit .NET-10-Bezügen.

## Workflows mit `actions/setup-dotnet`-Pins

Alle SDK-Pins stehen auf `10.0.x` bei `actions/setup-dotnet@v6`:

| Datei | Zeilen mit `dotnet-version: '10.0.x'` | Jobs/Kontext |
|-------|--------------------------------------|--------------|
| `.github/workflows/pr-staging-ci.yml` | 80, 116, 227 | `static-checks` (Format/Security-Scan/Static-Analysis), `build-and-test` (Build, `Category!=OsInterface`-Tests, Coverage-Gate 70 %, Contract-Export), `browser-tests` (best-effort, `continue-on-error: true`) |
| `.github/workflows/staging-ci.yml` | 75, 105, 210 | gleiche Job-Struktur wie PR-CI; zusätzlich `version` (semantic-release) und `prerelease` (nutzt Composite Action `build-and-package`) |
| `.github/workflows/release.yml` | 46 | `release`-Job: Restore/Build/Test-Gate, `build-and-package`, Contract-Export als Release-Asset |
| `.github/workflows/security-scan.yml` | 31 | wöchentlicher `dotnet list package --vulnerable --include-transitive`-Scan |

## Composite Actions

### `.github/actions/build-and-package/action.yml`

- Beschreibungstext (Zeile 4): „Provisions the .NET 10 SDK"
- Schritt „Setup .NET 10" (Zeilen 25–28): `actions/setup-dotnet@v6`, `dotnet-version: '10.0.x'`
- Publish linux-x64 (Zeilen 45–52): `dotnet publish Rezepte.Web/Rezepte.Web.csproj … --framework net10.0 --runtime linux-x64 --self-contained true --output artifacts/publish`
- Publish win-x64 (Zeilen 60–67): gleiches mit `--framework net10.0 --runtime win-x64 --output artifacts/publish-win-x64`
- Weitere Schritte (TFM-unabhängig): `release-metadata.json` je RID, ZIP-Erzeugung `release-linux-x64.zip`/`release-win-x64.zip`, `update.json`-Manifest mit SHA-256/Größe/Asset-URLs

### `.github/actions/security-scan/action.yml`

- Kein SDK-Pin (nutzt das vom aufrufenden Workflow bereitgestellte SDK); führt `dotnet list <sln> package --vulnerable --include-transitive` aus und wertet die Ausgabe per `grep` aus. Keine .NET-10-Referenzen.

## Workflows ohne .NET-Bezug

Verifiziert per Grep (`dotnet`, `.NET`, `net10`, `setup-dotnet`): `pages.yml`, `review-architecture-security.yml`, `review-compliance.yml`, `review-nuget-packages.yml`, `staging-to-main-promotion.yml`, `sync-staging-with-main.yml`, `verify-pr-source.yml` enthalten keine SDK-Pins oder `net10.0`-Referenzen. Einziger Texttreffer: `review-architecture-security.yml` Zeile 54 nennt „ASP.NET Core, Blazor, .NET" als Review-Thema (kein Versionsbezug).

## Skripte

| Datei | Zeile | .NET-10-Bezug |
|-------|-------|----------------|
| `scripts/Export-ImportContract.ps1` | 585 | `$dllPath = Join-Path $exportRoot "$projectName/bin/Release/net10.0/$projectName.dll"` — hart codierter Ausgabepfad der Vertrags-Assemblies; schlägt mit `Fail "baseline assembly was not built"` fehl, wenn der Pfad nicht existiert |
| `scripts/screenshots/generate-screenshots.js` | 10 | `path.join(repoRoot, 'Rezepte.Web', 'bin', 'Release', 'net10.0', 'publish', 'Rezepte.Web.dll')` — hart codierter Pfad zur publizierten App-DLL |

Skripte ohne `net10.0`-/`TargetFramework`-Bezug (Grep-verifiziert): `Analyze-FlakyTestResults.ps1`, `Retry-FlakyTests.ps1`, `Build-Site.ps1`, `check-site.mjs`, `resolve-release-version.mjs`, `scripts/lib/*`.

## Aufgerufene CLI-Befehle in den Workflows (projektübliche Befehle)

- `dotnet restore Rezepte.sln`
- `dotnet format Rezepte.sln --verify-no-changes --no-restore --severity error`
- `dotnet build Rezepte.sln --configuration Release --no-restore -p:TreatWarningsAsErrors=true` (Static-Analysis-Gate)
- `dotnet build Rezepte.Web/Rezepte.Web.csproj --configuration Release --no-restore`
- `dotnet build Rezepte.Tests/Rezepte.Tests.csproj --configuration Release --no-restore`
- `dotnet build Rezepte.Tests.Browser/Rezepte.Tests.Browser.csproj --configuration Release --no-restore`
- `dotnet test Rezepte.Tests/Rezepte.Tests.csproj --configuration Release --no-build --filter "Category!=OsInterface" --collect:"XPlat Code Coverage"`
- `dotnet test Rezepte.Tests/Rezepte.Tests.csproj --configuration Release --no-build --filter "Category=OsInterface"`
- `dotnet test Rezepte.Tests.Browser/Rezepte.Tests.Browser.csproj --configuration Release --no-build`
- `dotnet publish Rezepte.Web/Rezepte.Web.csproj --configuration Release --no-restore` (für Browser-Tests)
- `dotnet tool install -g dotnet-reportgenerator-globaltool --version 5.5.11` + ReportGenerator-TextSummary mit 70-%-Line-Coverage-Gate
- `dotnet tool install Microsoft.DotNet.ApiCompat.Tool --tool-path ./.tools` (nur wenn `contract-baselines/import-contract` existiert — es existiert)
- `pwsh "$PLAYWRIGHT_SCRIPT" install --with-deps chromium`
