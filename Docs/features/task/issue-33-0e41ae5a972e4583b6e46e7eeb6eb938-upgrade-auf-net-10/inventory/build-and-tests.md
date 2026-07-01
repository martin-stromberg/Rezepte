# Build- und Test-Baseline

## Restore

`dotnet list Rezepte.sln package --outdated` fuehrte vor der Paketpruefung einen Restore aus:

- `Rezepte.Web/Rezepte.Web.csproj` wurde wiederhergestellt.
- `Rezepte.Tests/Rezepte.Tests.csproj` wurde wiederhergestellt.
- Paketquellen: NuGet.org und lokale Microsoft SDK NuGet Packages.

## Build

Befehl:

```powershell
dotnet build Rezepte.sln --no-restore -clp:Summary
```

Ergebnis:

- Build erfolgreich
- `Rezepte.Web -> Rezepte.Web/bin/Debug/net9.0/Rezepte.Web.dll`
- `Rezepte.Tests -> Rezepte.Tests/bin/Debug/net9.0/Rezepte.Tests.dll`
- Zusammenfassung: 0 Warnungen, 0 Fehler

Hinweis:

- Der Build war inkrementell nach vorherigem Restore/Testkontext. Fuer die Umsetzung sollte ein Clean Build nach der Umstellung auf `net10.0` eingeplant werden.

## Tests

Befehl:

```powershell
dotnet test Rezepte.sln --no-restore
```

Ergebnis:

- Testprojekt: `Rezepte.Tests.dll`
- Framework: `.NETCoreApp,Version=v9.0`
- 33 Tests gefunden
- 33 erfolgreich
- 0 fehlgeschlagen
- 0 uebersprungen
- Dauer: ca. 1 s fuer den Testlauf

## Bestehende Warnungen im Testbuild

Beim `dotnet test` wurden vor dem erfolgreichen Testlauf zahlreiche bestehende Compilerwarnungen aus `Rezepte.Web` ausgegeben. Kategorien:

- doppelte `using`-Direktiven in Razor-Komponenten, z. B. `CS0105`
- Nullable-Warnungen, z. B. `CS8600`, `CS8601`, `CS8602`, `CS8603`, `CS8604`, `CS8618`, `CS8619`, `CS8625`
- ungenutzte Felder/Variablen, z. B. `CS0168`, `CS0169`, `CS0649`
- Obsolete-Warnung in `Rezepte.Web/Services/PdfGenerator.cs` durch QuestPDF API

Beispiele fuer betroffene Bereiche:

- `Rezepte.Web/Components/Shared/*`
- `Rezepte.Web/Components/Settings/ExportData.razor`
- `Rezepte.Web/Controllers/*`
- `Rezepte.Web/Services/RecipeService.cs`
- `Rezepte.Web/Services/Import/*`
- `Rezepte.Web/Services/PdfGenerator.cs`

Upgrade-Relevanz:

- Diese Warnungen sind bereits vor dem Upgrade vorhanden und sollten nicht automatisch als .NET-10-Regression gewertet werden.
- Nach Paketupdates koennen zusaetzliche Analyzer- oder API-Warnungen auftreten.
- Falls die Planung "build ohne Warnungen" als Ziel interpretiert, muss geklaert werden, ob bestehende Warnungen Teil des Upgrade-Scopes sind.
