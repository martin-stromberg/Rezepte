# Code-Review

## Ergebnis

**Status:** Befunde vorhanden

## Befunde

### PlaywrightBrowserFixture.cs (PlaywrightBrowserFixture)

- **Toter Code** — Die Eigenschaft `UnavailableReason` (Z. 16) wird in `InitializeAsync` (Z. 31) gesetzt, aber an keiner Stelle der Lösung gelesen. Alle acht `Skip.IfNot`-Aufrufe in den fünf Browser-Testklassen verwenden weiterhin den festen Text `"Playwright Chromium browser is not installed."`. Schlägt der Chromium-Start aus einem anderen Grund fehl (fehlende Systembibliotheken, Sandbox-Problem, Timeout), meldet der Testlauf eine nachweislich falsche Ursache und die tatsächliche Fehlermeldung geht verloren.

  Empfehlung: In allen Browser-Testklassen `Skip.IfNot(browserFixture.BrowsersAvailable, browserFixture.UnavailableReason ?? "Playwright Chromium browser is not installed.")` verwenden. Alternativ die Eigenschaft entfernen, falls sie nicht genutzt werden soll — der aktuelle Zwischenzustand ist beides nicht.

### LoadingBarSafetyTimeoutBrowserTests.cs (LoadingBarSafetyTimeoutBrowserTests)

- **Test ohne gesicherte Vorbedingung (Fehlalarm-Risiko)** — `WhenNavigationNeverCompletes_HidesBarAfterMaxVisibleDuration` (Z. 15-31) klickt den Link und wartet anschließend nur darauf, dass die Leiste **nicht** aktiv ist (`WaitUntilLoadingBarHiddenAsync`, Z. 28). Wird die Leiste nie aktiviert — also genau im Fehlerfall, dass das Feature gar nicht anspricht — ist die Wartebedingung sofort erfüllt und die Assertion (Z. 30) grün. Der Test kann den Safety-Timeout also nicht von einem komplett toten Feature unterscheiden.

  Empfehlung: Nach dem Klick zuerst `await pageObject.WaitUntilLoadingBarActiveAsync();` und `(await pageObject.IsLoadingBarActiveAsync()).Should().BeTrue();` einfügen und erst danach auf das Verschwinden warten — analog zu `LoadingBarVisibilityBrowserTests.AfterNavigationCompleted_HidesLoadingBar` (Z. 57-62), wo dieses Muster bereits korrekt umgesetzt ist.

### LoadingBarFormNavigationBrowserTests.cs (LoadingBarFormNavigationBrowserTests)

- **Test ohne Synchronisation (nichtdeterministisch)** — `SearchSubmit_ShowsLoadingBar` (Z. 12-27) prüft den Zustand der Leiste unmittelbar nach `ClickAsync` (Z. 24-26), ohne die Navigation zu verzögern. Der Kommentar (Z. 20-22) beschreibt das Problem, löst es aber nicht: Ob die Assertion den Zustand noch sieht, hängt vom Wettlauf zwischen Playwright-Roundtrip und Commit der Navigation ab. Alle übrigen Browser-Tests machen ihre Beobachtung durch `DelayNextNavigationAsync` deterministisch.

  Empfehlung: Vor dem Klick die Zielnavigation verzögern (das Formular postet nach `recipes/search`, siehe `MainLayout.razor` Z. 38), z. B. `await pageObject.Page.DelayNextNavigationAsync("**/recipes/search*", 1500);`, und danach `await pageObject.WaitUntilLoadingBarActiveAsync();` statt der sofortigen Abfrage verwenden. Damit bleibt das alte Dokument während der Prüfung am Leben.

### NetworkDelayHelper.cs (NetworkDelayHelper)

- **Methodenname beschreibt nicht, was die Methode tut** — `DelayNextNavigationAsync` (Z. 11) registriert über `page.RouteAsync` eine dauerhafte Route, die **jede** künftige passende Anfrage verzögert, nicht nur die nächste. `LoadingBarColorBrowserTests.SecondClickDuringRunningAnimation_ChangesColor` (Z. 42-48) verlässt sich sogar genau darauf, dass beide Klicks verzögert werden. Der Name führt beim Lesen der Tests in die Irre.

  Empfehlung: Methode in `DelayNavigationAsync` (oder `DelayMatchingRequestsAsync`) umbenennen und die vier Aufrufstellen in `LoadingBarVisibilityBrowserTests`, `LoadingBarColorBrowserTests` anpassen. Den XML-Kommentar (Z. 6-8) auf „delays every matching request" präzisieren.

### RezepteAppFixture.cs (RezepteAppFixture)

- **Doppelter Code (bereits vorhandene Logik)** — `FindRepositoryRoot` (Z. 211-226) ist eine zeichengleiche Kopie von `Rezepte.Tests/TestHelpers/RepositoryPaths.FindRepositoryRoot` (Z. 5-20), die im selben Branch neu angelegt wurde, um genau diese Duplikation zu beseitigen. Die Kopie im Browser-Testprojekt hat den Helfer wieder vervielfacht.

  Empfehlung: `RepositoryPaths.cs` per Link in `Rezepte.Tests.Browser.csproj` einbinden (`<Compile Include="..\Rezepte.Tests\TestHelpers\RepositoryPaths.cs" Link="TestHelpers\RepositoryPaths.cs" />`) und `RezepteAppFixture.FindRepositoryRoot` entfernen; alternativ den Helfer in ein gemeinsames Test-Support-Projekt verschieben, das beide Testprojekte referenzieren.

- **Irreführende Async-Signatur** — `CreateTemporaryDatabaseAsync` (Z. 85-90) trägt das `Async`-Suffix und liefert `Task<string>`, führt aber keinerlei asynchrone Arbeit aus (`Directory.CreateTempSubdirectory` + `Task.FromResult`). Der Aufrufer in `InitializeAsync` (Z. 42) erzwingt dafür ein unnötiges `await`.

  Empfehlung: Methode zu `private string CreateTemporaryDatabase()` umbauen und in `InitializeAsync` synchron aufrufen.

- **Zu enge Ausnahmebehandlung beim Aufräumen** — `DisposeAsync` (Z. 64-74) fängt beim Löschen des Temp-Verzeichnisses ausschließlich `IOException`. Unter Windows liefert eine noch gehaltene SQLite-Datei ebenso häufig eine `UnauthorizedAccessException`; diese schlägt dann bis in den Testlauf durch und lässt eine ansonsten grüne Suite im Teardown scheitern. Der Kommentar („Best effort cleanup; a lingering temp file must not fail the test run.") beschreibt eine Zusage, die der Code so nicht einhält.

  Empfehlung: Filter auf `catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)` erweitern.

### LoadingBarVisibilityBrowserTests.cs / LoadingBarColorBrowserTests.cs / LoadingBarDisabledBrowserTests.cs / LoadingBarFormNavigationBrowserTests.cs / LoadingBarSafetyTimeoutBrowserTests.cs

- **Doppelter Code** — Der Einstiegsblock aus zwei `Skip.IfNot`-Aufrufen, `LoadingBarPageObject.CreateAsync(browserFixture.Browser!, appFixture.BaseAddress)` und `LoginAsync(RezepteAppFixture.TestUsername, RezepteAppFixture.TestPassword)` steht wortgleich in allen acht Testmethoden der fünf Browser-Testklassen (je 4-5 Zeilen, insgesamt rund 35 Zeilen reine Wiederholung). Eine Änderung an der Anmeldung oder an den Skip-Bedingungen erfordert acht synchrone Anpassungen.

  Empfehlung: In `Rezepte.Tests.Browser/Infrastructure` einen Helfer ergänzen, z. B. `internal static Task<LoadingBarPageObject> StartLoggedInSessionAsync(PlaywrightBrowserFixture browserFixture, RezepteAppFixture appFixture)`, der beide `Skip.IfNot`-Prüfungen ausführt, das Page-Object erzeugt und die Anmeldung durchführt. Jede Testmethode beginnt dann mit einer Zeile.

### LoadingBarWiringTests.cs (LoadingBarWiringTests)

- **Test prüft Implementierungsdetails statt fachliches Verhalten** — `Script_ShouldRegisterClickAndSubmitListenersInCapturePhase` (Z. 100-106) vergleicht den **Quelltext** von `loadingBar.js` per exakter Zeichenkette inklusive der internen Funktionsnamen (`document.addEventListener('click', handleLinkClick, true)`). Ein Umbenennen von `handleLinkClick` oder ein Wechsel auf doppelte Anführungszeichen lässt den Test fehlschlagen, obwohl sich das Verhalten nicht ändert; umgekehrt würde ein kaputter, aber textlich passender Handler durchgehen. Das tatsächlich abgesicherte Verhalten (Leiste reagiert auf Link-Klick und Formular-Submit) decken `LoadingBarVisibilityBrowserTests` und `LoadingBarFormNavigationBrowserTests` bereits end-to-end ab.

  Empfehlung: Testmethode ersatzlos entfernen.

- **Redundanter Test** — `Configuration_ShouldProvideLoadingBarSection` (Z. 44-50) prüft nur, dass `appsettings.json` die Zeichenkette `"LoadingBar"` enthält. Diese Aussage ist vollständig in `Configuration_LoadingBarSection_MatchesDocumentedDefaults` (Z. 52-73) enthalten: Fehlt die Sektion, bleibt `options.Colors` leer und die Assertion auf Z. 72 schlägt fehl.

  Empfehlung: `Configuration_ShouldProvideLoadingBarSection` entfernen.

### LoadingBarService.cs (LoadingBarService)

- **Fehlende Kapselung (veränderlicher gemeinsamer Zustand)** — `ValidateColors` (Z. 85-107) gibt bei mindestens einer gültigen Farbe die interne `List<string>` (Z. 87) direkt als `IReadOnlyList<string>` zurück. `LoadingBarService` ist Singleton und das erzeugte `LoadingBarSettings` wird per `Lazy` prozessweit zwischengespeichert (Z. 23/28); ein Cast zurück auf `List<string>` verändert damit die Konfiguration aller Anfragen. Für den Default-Pfad wurde dieses Risiko bewusst über `new ReadOnlyCollection<string>(...)` (Z. 17) ausgeschlossen — der Rückgabepfad auf Z. 106 ist dazu inkonsistent.

  Empfehlung: Auf Z. 106 `return new ReadOnlyCollection<string>(validColors);` zurückgeben, damit beide Rückgabepfade dieselbe Zusicherung geben.

### LoadingBarRenderingTests.cs (LoadingBarRenderingTests)

- **Testname beschreibt nicht, was geprüft wird** — `Render_WhenEnabled_RendersHostElementWithConfiguredId` (Z. 17-23) assertiert ausschließlich, dass das Kindelement `.loading-bar-indicator` existiert (Z. 22). Der Namensbestandteil `WithConfiguredId` wird nur implizit dadurch abgedeckt, dass `Find("#loading-bar")` wirft; der eigentliche Prüfgegenstand (Indikator-Element) taucht im Namen nicht auf.

  Empfehlung: Methode in `Render_WhenEnabled_RendersHostElementWithIndicator` umbenennen.

## Geprüfte Dateien

- `.github/workflows/pr.yml`
- `Rezepte.sln`
- `Rezepte.Web/appsettings.json`
- `Rezepte.Web/Components/App.razor`
- `Rezepte.Web/Components/Layout/LoadingBar.razor`
- `Rezepte.Web/Components/Layout/LoadingBar.razor.css`
- `Rezepte.Web/Components/Layout/MainLayout.razor`
- `Rezepte.Web/Configuration/LoadingBarOptions.cs`
- `Rezepte.Web/Configuration/LoadingBarSettings.cs`
- `Rezepte.Web/Extensions/ServiceCollectionExtensions.cs`
- `Rezepte.Web/Services/ILoadingBarService.cs`
- `Rezepte.Web/Services/LoadingBarService.cs`
- `Rezepte.Web/wwwroot/js/loadingBar.js`
- `Rezepte.Tests/Rezepte.Tests.csproj`
- `Rezepte.Tests/Components/LoadingBarRenderingTests.cs`
- `Rezepte.Tests/Deployment/LoadingBarWiringTests.cs`
- `Rezepte.Tests/Services/LoadingBarServiceDefaultsTests.cs`
- `Rezepte.Tests/Services/LoadingBarServiceDurationParsingTests.cs`
- `Rezepte.Tests/Services/LoadingBarServiceValidationTests.cs`
- `Rezepte.Tests/TestHelpers/LoadingBarServiceTestFactory.cs`
- `Rezepte.Tests/TestHelpers/RepositoryPaths.cs`
- `Rezepte.Tests.Browser/Rezepte.Tests.Browser.csproj`
- `Rezepte.Tests.Browser/Infrastructure/BrowserTestCollection.cs`
- `Rezepte.Tests.Browser/Infrastructure/ConfiguredRezepteAppFixture.cs`
- `Rezepte.Tests.Browser/Infrastructure/LoadingBarPageObject.cs`
- `Rezepte.Tests.Browser/Infrastructure/NetworkDelayHelper.cs`
- `Rezepte.Tests.Browser/Infrastructure/PlaywrightBrowserFixture.cs`
- `Rezepte.Tests.Browser/Infrastructure/RezepteAppFixture.cs`
- `Rezepte.Tests.Browser/LoadingBarColorBrowserTests.cs`
- `Rezepte.Tests.Browser/LoadingBarDisabledBrowserTests.cs`
- `Rezepte.Tests.Browser/LoadingBarFormNavigationBrowserTests.cs`
- `Rezepte.Tests.Browser/LoadingBarSafetyTimeoutBrowserTests.cs`
- `Rezepte.Tests.Browser/LoadingBarVisibilityBrowserTests.cs`
