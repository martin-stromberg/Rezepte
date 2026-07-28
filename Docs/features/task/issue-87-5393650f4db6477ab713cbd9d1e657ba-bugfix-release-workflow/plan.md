# Umsetzungsplan: Bugfix Release-Workflow Testlauf

## Zielbild

Der Release-Workflow bereitet im Testschritt dieselben Voraussetzungen fuer die Browser-Tests vor wie der PR-Workflow und startet die betroffenen Testprojekte mit einer von .NET 10 unterstuetzten Projektaufrufsyntax. Dabei wird keine gebaute Testassembly als freies Argument an den Testrunner uebergeben.

## Betroffene Dateien

| Datei | Aenderung |
|-------|-----------|
| `.github/workflows/release.yml` | Testvorbereitung fuer `Rezepte.Tests.Browser` ergaenzen und Testausfuehrung auf explizite Projektaufrufe umstellen. |
| `Docs/help/github-actions.md` | Nur im spaeteren Dokumentationsschritt aktualisieren, falls die CI-Dokumentation den Release-Testablauf beschreiben soll. |

## Umsetzungsschritte

1. Release-Testschritt in `.github/workflows/release.yml` aufteilen oder erweitern.
   - `Rezepte.Web` weiterhin mit `dotnet build Rezepte.Web/Rezepte.Web.csproj --configuration Release --no-restore` bauen.
   - `Rezepte.Tests` weiterhin mit `dotnet build Rezepte.Tests/Rezepte.Tests.csproj --configuration Release --no-restore` bauen.
   - `Rezepte.Tests.Browser` zusaetzlich mit `dotnet build Rezepte.Tests.Browser/Rezepte.Tests.Browser.csproj --configuration Release --no-restore` bauen.

2. Playwright-Browser im Release-Workflow installieren.
   - Nach dem Build von `Rezepte.Tests.Browser` das vorhandene Muster aus `.github/workflows/pr.yml` uebernehmen:
     - `playwright.ps1` unter `Rezepte.Tests.Browser/bin/Release` suchen.
     - Bei fehlendem Skript mit klarer Fehlermeldung abbrechen.
     - `pwsh "$PLAYWRIGHT_SCRIPT" install --with-deps chromium` ausfuehren.

3. `Rezepte.Web` vor den Browser-Tests publishen.
   - Vor der Testausfuehrung `dotnet publish Rezepte.Web/Rezepte.Web.csproj --configuration Release --no-restore` ausfuehren.
   - Keinen Runtime-spezifischen Publish fuer diesen Test-Publish verwenden, damit die vorhandene Browser-Testfixture den erwarteten Pfad `Rezepte.Web/bin/Release/net10.0/publish/Rezepte.Web.dll` findet.
   - Den spaeteren Release-Publish nach `artifacts/publish` unveraendert lassen.

4. Testausfuehrung auf explizite Projektaufrufe umstellen.
   - Den bisherigen Solution-weiten Aufruf `dotnet test Rezepte.sln --configuration Release --no-build` im Release-Testbereich ersetzen durch:

     ```bash
     dotnet test Rezepte.Tests/Rezepte.Tests.csproj --configuration Release --no-build
     dotnet test Rezepte.Tests.Browser/Rezepte.Tests.Browser.csproj --configuration Release --no-build
     ```

   - Dadurch werden beide geforderten Testprojekte weiterhin ausgefuehrt, ohne dass der Testrunner eine gebaute `.dll` als ungueltiges freies Argument erhalten kann.

5. YAML-Struktur konsistent halten.
   - Die bestehende Job-Reihenfolge `Restore` -> `Restore publish runtime` -> `Test` -> `Publish` beibehalten.
   - Keine Release-Artefakt-Erzeugung, Versionierung, Tag-Erstellung oder Contract-Export-Logik veraendern.
   - Die Testvorbereitung kann im bestehenden `Test`-Step bleiben oder in mehrere benannte Steps analog zum PR-Workflow aufgeteilt werden. Bevorzugt ist eine Aufteilung in sprechende Steps, weil GitHub-Actions-Logs dann den fehlschlagenden Teil klarer zeigen.

## Akzeptanzkriterien-Abdeckung

| Akzeptanzkriterium | Planabdeckung |
|--------------------|---------------|
| Gueltiger Testaufruf fuer die betroffenen Testprojekte | Explizite `dotnet test <Projekt>.csproj --configuration Release --no-build`-Aufrufe. |
| `Rezepte.Tests.Browser` wird nicht als ungueltiges Argument uebergeben | Keine direkte oder indirekt gebuendelte Uebergabe mehr ueber einen gemeinsamen Solution-Testlauf im Release-Testbereich. |
| `Rezepte.Tests` bleibt beruecksichtigt | Separater Projektaufruf fuer `Rezepte.Tests/Rezepte.Tests.csproj`. |
| Kompatibel mit .NET 10 | Projektbasierte `dotnet test`-Syntax und bestehendes `actions/setup-dotnet@v4` mit `10.0.x`. |
| GitHub Actions auf `ubuntu-latest` kommt ueber den Testschritt hinaus | Browser-Testbuild, Playwright-Installation und Web-Publish werden vor dem Browser-Testlauf hergestellt. |

## Validierung

1. YAML statisch pruefen.
   - Einrueckung, Step-Namen und multiline `run`-Bloecke in `.github/workflows/release.yml` kontrollieren.

2. Lokal Build- und Testsyntax pruefen, soweit die Umgebung es erlaubt.
   - `dotnet restore Rezepte.sln`
   - `dotnet build Rezepte.Web/Rezepte.Web.csproj --configuration Release --no-restore`
   - `dotnet build Rezepte.Tests/Rezepte.Tests.csproj --configuration Release --no-restore`
   - `dotnet build Rezepte.Tests.Browser/Rezepte.Tests.Browser.csproj --configuration Release --no-restore`
   - `dotnet publish Rezepte.Web/Rezepte.Web.csproj --configuration Release --no-restore`
   - `dotnet test Rezepte.Tests/Rezepte.Tests.csproj --configuration Release --no-build`
   - `dotnet test Rezepte.Tests.Browser/Rezepte.Tests.Browser.csproj --configuration Release --no-build`

3. GitHub-Actions-Validierung einplanen.
   - Entscheidend ist ein Release-Workflowlauf auf `ubuntu-latest`, weil Playwright-Systemabhaengigkeiten und der .NET-10-Testrunner dort dem gemeldeten Fehlerkontext entsprechen.

## Risiken und Gegenmassnahmen

| Risiko | Gegenmassnahme |
|--------|----------------|
| Playwright-Installationsskript wird nicht gefunden | Browser-Testprojekt vor der Installation bauen und denselben Guard wie im PR-Workflow verwenden. |
| Browser-Tests werden wegen fehlendem Publish-Output uebersprungen oder schlagen fehl | `Rezepte.Web` vor dem Browser-Testlauf framework-abhaengig publishen. |
| Release-Publish wird durch den Test-Publish beeinflusst | Test-Publish ohne `--output` ausfuehren und bestehenden finalen Publish nach `artifacts/publish` unveraendert lassen. |
| Lokale Windows-Validierung bildet `ubuntu-latest` nicht vollstaendig ab | Lokale Pruefung auf Syntax/Build begrenzen und GitHub-Actions-Lauf als massgebliche Validierung behandeln. |

## Nicht umzusetzen

- Keine Entfernung oder dauerhafte Deaktivierung von `Rezepte.Tests.Browser`.
- Keine fachlichen Aenderungen an Web-App, Rezeptlogik, Importlogik oder UI.
- Keine Aenderung an Versionsermittlung, Release-Tagging, Artefaktupload oder Import-Contract-Export.

## Offene Punkte

Keine.
