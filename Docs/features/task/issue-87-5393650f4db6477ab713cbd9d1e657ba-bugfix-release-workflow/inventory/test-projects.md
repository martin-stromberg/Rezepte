# Detail: Testprojekte und Browser-Testvoraussetzungen

## Solution

`Rezepte.sln` enthaelt beide betroffenen Testprojekte:

- `Rezepte.Tests` in `Rezepte.Tests/Rezepte.Tests.csproj`
- `Rezepte.Tests.Browser` in `Rezepte.Tests.Browser/Rezepte.Tests.Browser.csproj`

Beide Projekte haben Release-Konfigurationszuordnungen in der Solution. Ein Solution-weiter `dotnet test Rezepte.sln --configuration Release --no-build` erfasst daher beide Testprojekte.

## Normales Testprojekt

`Rezepte.Tests/Rezepte.Tests.csproj`:

- Target Framework: `net10.0`
- Test SDK: `Microsoft.NET.Test.Sdk` Version `18.7.0`
- Testframework: xUnit `2.9.3`
- Runner: `xunit.runner.visualstudio` Version `3.1.5`
- Weitere Testabhaengigkeiten: FluentAssertions, EF Core InMemory, bUnit, Moq, coverlet.
- Referenziert `Rezepte.Web` und Import-/Plugin-Projekte.

## Browser-Testprojekt

`Rezepte.Tests.Browser/Rezepte.Tests.Browser.csproj`:

- Target Framework: `net10.0`
- Test SDK: `Microsoft.NET.Test.Sdk` Version `18.7.0`
- Testframework: xUnit `2.9.3`
- Runner: `xunit.runner.visualstudio` Version `3.1.5`
- Browser-Abhaengigkeit: `Microsoft.Playwright` Version `1.61.0`
- Skippable Tests: `Xunit.SkippableFact` Version `1.5.61`
- Referenziert `Rezepte.Web`.
- Linkt `Rezepte.Tests/TestHelpers/RepositoryPaths.cs`.

## Playwright-Voraussetzung

`Rezepte.Tests.Browser/Infrastructure/PlaywrightBrowserFixture.cs` erstellt Playwright und startet headless Chromium:

```csharp
_playwright = await Playwright.CreateAsync();
Browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
```

Falls Chromium fehlt, wird eine `PlaywrightException` abgefangen und `BrowsersAvailable` auf `false` gesetzt. Tests koennen dadurch je nach Testcode uebersprungen werden, aber ein Release-Workflow, der Browser-Tests wirklich ausfuehren soll, muss Chromium installieren.

## Publish-Voraussetzung

`Rezepte.Tests.Browser/Infrastructure/RezepteAppFixture.cs` startet `Rezepte.Web` als separaten Prozess. Die Fixture sucht standardmaessig:

```text
Rezepte.Web/bin/<Configuration>/<Tfm>/publish/Rezepte.Web.dll
```

Alternativ kann `REZEPTE_PUBLISH_DIR` gesetzt werden. Ist diese Variable gesetzt und zeigt nicht auf eine vorhandene `Rezepte.Web.dll`, wirft die Fixture eine Fehlkonfiguration. Ohne Override wird bei fehlendem Publish-Output ein Skip-Grund gesetzt.

Der Codekommentar erklaert, dass ein normaler Build-Output nicht ausreicht, weil statische Assets ausserhalb von `dotnet run` nicht korrekt ueber `MapStaticAssets()` ausgeliefert werden. Browser-Tests brauchen deshalb einen Publish-Output.

## Bedeutung fuer den Release-Bug

Der Release-Workflow muss die Browser-Test-Voraussetzungen vor `dotnet test` herstellen, wenn `Rezepte.Tests.Browser` Teil des Release-Testlaufs bleibt. Nur `Rezepte.Web` und `Rezepte.Tests` zu bauen reicht nicht aus, da das Browser-Testprojekt eigene Build-Artefakte, Playwright-Installationsskript und den Web-Publish erwartet.
