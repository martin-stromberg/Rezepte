# Detail: Release-Workflow

## Datei

`.github/workflows/release.yml`

## Relevante Struktur

- Trigger: `pull_request_target` auf `main`, Typ `closed`.
- Job: `release`, `runs-on: ubuntu-latest`, `timeout-minutes: 30`.
- Job-Bedingung: `github.event.pull_request.merged == true`.
- .NET Setup: `actions/setup-dotnet@v4` mit `dotnet-version: 10.0.x`.

## Testschritt

Der Testschritt steht in `.github/workflows/release.yml` ab Zeile 102:

```yaml
- name: Test
  run: |
    dotnet build Rezepte.Web/Rezepte.Web.csproj --configuration Release --no-restore
    dotnet build Rezepte.Tests/Rezepte.Tests.csproj --configuration Release --no-restore
    dotnet test Rezepte.sln --configuration Release --no-build
```

## Beobachtungen

- Der Workflow restored vorher die Solution und zusaetzlich `Rezepte.Web` fuer `linux-x64`.
- Der Testschritt baut vor dem Testlauf nur `Rezepte.Web` und `Rezepte.Tests`.
- `Rezepte.Tests.Browser` wird nicht explizit gebaut, obwohl es Teil der Solution ist.
- Es gibt im Release-Workflow keinen Schritt zur Installation der Playwright-Browser.
- Es gibt im Release-Testbereich keinen Publish-Schritt fuer `Rezepte.Web`, bevor die Browser-Tests laufen. Der spaetere Release-Publish beginnt erst nach dem Testschritt.
- Der eigentliche Testaufruf laeuft ueber `Rezepte.sln` mit `--no-build`; fehlende Build- und Publish-Artefakte werden dadurch nicht automatisch nachgezogen.

## Bedeutung fuer die Anforderung

Der sichtbare YAML-Code uebergibt keine `Rezepte.Tests.Browser.dll` direkt an `dotnet test`. Trotzdem ist der Release-Testschritt der betroffene Bereich, weil der Solution-Testlauf beide Testprojekte aus der Solution erfasst. Wenn der .NET-10-Testpfad dabei Assemblies intern an den Testrunner reicht oder aus nicht vollstaendig vorbereiteten Projekten resultierende Argumente erzeugt, kann die Browser-Testassembly als ungueltiges Argument gemeldet werden.

## Aenderungsrelevante Punkte

- Der Release-Testschritt sollte entweder dem PR-Workflow entsprechen oder Testprojekte explizit einzeln testen.
- Wird bei `dotnet test Rezepte.sln --no-build` geblieben, muessen vorher alle Testprojekte gebaut sein.
- Fuer echte Browser-Testausfuehrung muessen Playwright-Browser installiert und `Rezepte.Web` publiziert sein.
- Die Loesung sollte den spaeteren Release-Publish nicht unbeabsichtigt veraendern.
