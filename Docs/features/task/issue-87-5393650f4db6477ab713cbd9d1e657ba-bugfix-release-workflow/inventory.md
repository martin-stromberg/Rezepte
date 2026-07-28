# Bestandsaufnahme: Bugfix Release-Workflow Testlauf

## Kontext

Die Anforderung betrifft den Release-Workflow in GitHub Actions. Im Release-Kontext soll der Testschritt unter `ubuntu-latest` mit .NET `10.0.x` sowohl `Rezepte.Tests` als auch `Rezepte.Tests.Browser` ausfuehren, ohne dass die gebaute Browser-Testassembly `Rezepte.Tests.Browser.dll` als ungueltiges Argument beim Testrunner landet.

## Detaildokumente

- [Release-Workflow](inventory/release-workflow.md)
- [PR-Workflow und CI-Vergleich](inventory/pr-workflow-comparison.md)
- [Testprojekte und Browser-Testvoraussetzungen](inventory/test-projects.md)
- [Lokale Pruefungen](inventory/local-checks.md)

## Relevante Dateien

| Datei | Rolle |
|-------|-------|
| `.github/workflows/release.yml` | Betroffener Release-Workflow; enthaelt den fehlschlagenden Testschritt. |
| `.github/workflows/pr.yml` | Vergleichsworkflow mit bereits vorhandener Vorbereitung fuer Browser-Tests. |
| `Rezepte.sln` | Enthaelt beide Testprojekte, daher erfasst `dotnet test Rezepte.sln` beide. |
| `Rezepte.Tests/Rezepte.Tests.csproj` | Normales xUnit-Testprojekt. |
| `Rezepte.Tests.Browser/Rezepte.Tests.Browser.csproj` | Browser-Testprojekt mit Playwright-Abhaengigkeit. |
| `Rezepte.Tests.Browser/Infrastructure/RezepteAppFixture.cs` | Erwartet einen Publish-Output von `Rezepte.Web`. |
| `Rezepte.Tests.Browser/Infrastructure/PlaywrightBrowserFixture.cs` | Startet headless Chromium und setzt installierte Playwright-Browser voraus. |

## Ist-Zustand

- Der Release-Workflow nutzt `actions/setup-dotnet@v4` mit `dotnet-version: 10.0.x`.
- Der Release-Testschritt baut `Rezepte.Web` und `Rezepte.Tests`, nicht aber `Rezepte.Tests.Browser`, und ruft danach `dotnet test Rezepte.sln --configuration Release --no-build` auf.
- Die Solution enthaelt `Rezepte.Tests.Browser`, sodass der Solution-Testlauf dieses Projekt grundsaetzlich beruecksichtigt.
- Der PR-Workflow baut die Browser-Tests explizit, installiert Playwright Chromium mit `playwright.ps1 install --with-deps chromium`, publisht `Rezepte.Web` und startet dann ebenfalls `dotnet test Rezepte.sln --configuration Release --no-build`.
- Die Browser-Testfixture sucht ohne Override den Publish-Output unter `Rezepte.Web/bin/<Configuration>/<Tfm>/publish/Rezepte.Web.dll`. Ein reiner Build-Output reicht laut Codekommentar nicht aus.

## Wahrscheinliche Fehlerursache

Der Release-Workflow ist gegenueber dem PR-Workflow unvollstaendig. Er fuehrt den Solution-Testlauf aus, ohne die Browser-Test-Vorbedingungen herzustellen:

- `Rezepte.Tests.Browser` wird vor `--no-build` nicht gebaut.
- Playwright Chromium wird im Release-Workflow nicht installiert.
- `Rezepte.Web` wird vor dem Browser-Testlauf nicht publiziert.

Die gemeldete Fehlermeldung zur ungueltigen `Rezepte.Tests.Browser.dll` weist zusaetzlich darauf hin, dass der in .NET 10 verwendete Testpfad oder Testrunner empfindlich auf die Art der Projekt-/Assembly-Uebergabe reagiert. Der vorhandene Workflow uebergibt zwar im sichtbaren YAML keine DLL direkt, der Solution-Testlauf entdeckt aber beide Testprojekte und kann intern gebaute Testassemblies an den Runner weiterreichen.

## Naheliegender Loesungsraum

- Release-Testschritt an den PR-Workflow angleichen: Browser-Testprojekt bauen, Playwright-Browser installieren, `Rezepte.Web` fuer Browser-Tests publishen.
- Alternativ oder ergaenzend Testprojekte getrennt auf Projektebene ausfuehren, z. B. je ein `dotnet test <Testprojekt>.csproj --configuration Release --no-build`, statt beide Testprojekte ueber einen gemeinsamen Solution-Aufruf zu starten.
- Falls der Fehler spezifisch durch Solution-weite Testausfuehrung unter .NET 10 entsteht, ist ein expliziter Projektaufruf robuster, weil keine gebauten Test-DLLs als freie Zusatzargumente an einen gemeinsamen Runner-Aufruf geraten.

## Risiken und Abhaengigkeiten

- Browser-Tests benoetigen auf `ubuntu-latest` Playwright-Systemabhaengigkeiten; ohne `install --with-deps chromium` koennen sie ausfallen oder uebersprungen werden.
- `dotnet publish Rezepte.Web/Rezepte.Web.csproj --configuration Release --no-restore` muss vor Browser-Tests erfolgen, damit statische Assets korrekt bereitstehen.
- Release-Workflow nutzt `--runtime linux-x64` spaeter fuer das Release-Artefakt. Der Browser-Test-Publish im PR-Workflow ist framework-abhaengig ohne Runtime; diese Variante passt zur Fixture-Suche nach `bin/Release/net10.0/publish/Rezepte.Web.dll`.
- Aenderungen sollten keine Tests entfernen oder dauerhaft deaktivieren, da dies explizit ein Nicht-Ziel der Anforderung ist.

## Empfohlene Validierung

- YAML-Struktur pruefen.
- Lokal, soweit moeglich: `dotnet restore Rezepte.sln`, `dotnet build Rezepte.Tests.Browser/Rezepte.Tests.Browser.csproj --configuration Release --no-restore`, `dotnet publish Rezepte.Web/Rezepte.Web.csproj --configuration Release --no-restore`, danach gezielt `dotnet test` fuer beide Testprojekte.
- In GitHub Actions: Release-Workflow oder ein gleichwertiger Workflowlauf auf `ubuntu-latest` muss bis ueber den Testschritt hinaus kommen, sofern die Tests fachlich gruen sind.
