# Code-Review

## Ergebnis

**Status:** Befunde vorhanden

## Befunde

### LoadingBarService.cs (LoadingBarService)

- **Fehlende Validierung von Vorbedingungen** — Die Invariante `MaxVisibleDuration > HideDelay` wird nach dem Fallback nicht erneut hergestellt. `BuildSettings` (Z. 49-58) ersetzt einen verletzenden Wert durch `DefaultMaxVisibleDurationMilliseconds` (15.000 ms), prueft danach aber nicht erneut gegen `hideDelayMilliseconds`. `HideDelay` darf laut `HideDelayMaxMilliseconds` (Z. 17) bis zu 60.000 ms betragen. Konkreter Fall: `HideDelay = "30s"` und `MaxVisibleDuration = "20s"` — beide Werte liegen einzeln im erlaubten Bereich, die Bedingung auf Z. 49 greift, und das Ergebnis ist `MaxVisibleDurationMilliseconds = 15000` bei `HideDelayMilliseconds = 30000`. Die zugesicherte und in `Docs/help/loading-bar-configuration.md` (Z. 15) dokumentierte Invariante ist damit weiterhin verletzt, ohne dass eine weitere Warnung erfolgt. Zur Laufzeit blendet der Safety-Timer nach 15 s aus, waehrend der Hide-Timer erst nach 30 s laeuft.

  Empfehlung: Nach dem Fallback erneut pruefen und im Konfliktfall auch `hideDelayMilliseconds` auf `DefaultHideDelayMilliseconds` zuruecksetzen (mit eigener Warnung) — alternativ `HideDelayMaxMilliseconds` so waehlen, dass es kleiner als `DefaultMaxVisibleDurationMilliseconds` ist. Zusaetzlich einen Test `GetSettings_WithHideDelayAboveDefaultMaxVisibleDuration_KeepsInvariant` in `LoadingBarServiceValidationTests` ergaenzen, der `HideDelay = "30s"`, `MaxVisibleDuration = "20s"` setzt und `result.MaxVisibleDurationMilliseconds > result.HideDelayMilliseconds` assertiert.

- **Uneinheitliche Validierungstiefe / fehlende Plausibilitaetspruefung** — `HideDelay` und `MaxVisibleDuration` werden ueber `ValidateCssTimeAsMilliseconds` (Z. 82-104) zusaetzlich gegen Min-/Max-Grenzen geprueft. `Height` (Z. 42) und `AnimationDuration` (Z. 43) durchlaufen dagegen mit `ValidateAgainstPattern` (Z. 71-80) nur eine Formatpruefung. Dadurch sind `"Height": "0px"` und `"AnimationDuration": "0s"` gueltige Konfigurationen, die die Ladeanimation faktisch unsichtbar bzw. bewegungslos machen — ohne Warnung im Protokoll. Dasselbe fachliche Konzept („unbrauchbarer Wert wird auf den Standard zurueckgesetzt") wird fuer vier gleichartige Parameter unterschiedlich streng umgesetzt.

  Empfehlung: `ValidateAgainstPattern` um eine Untergrenze ergaenzen bzw. fuer `AnimationDuration` `ValidateCssTimeAsMilliseconds` mit `[1, 60_000]` verwenden und den validierten Wert wieder als CSS-Zeit ausgeben; fuer `Height` einen numerischen Mindestwert (> 0) pruefen. Die neuen Grenzen als benannte Konstanten analog zu `HideDelayMinMilliseconds` fuehren und in `Docs/help/loading-bar-configuration.md` dokumentieren (dort fehlen aktuell alle vier Bereichsgrenzen).

### RezepteAppFixture.cs (RezepteAppFixture)

- **Fehlerbehandlung: `DisposeAsync` ist nicht mehrfach aufrufbar** — `InitializeAsync` ruft im neuen `catch`-Block selbst `await DisposeAsync()` auf (Z. 52-56) und wirft anschliessend weiter. `DisposeAsync` (Z. 59-87) gibt `_process` per `_process?.Dispose()` (Z. 71) frei, setzt das Feld aber nicht auf `null`. Das Testframework ruft `DisposeAsync` fuer die bereits erzeugte Fixture ein zweites Mal auf; dann trifft `_process.HasExited` (Z. 63) auf ein bereits freigegebenes `Process`-Objekt und wirft `InvalidOperationException("No process is associated with this object.")`. Die urspruengliche Ursache (z. B. `TimeoutException` aus `WaitUntilReadyAsync` oder ein fehlgeschlagenes `RegisterTestUserAsync`) wird dadurch von einem irrefuehrenden Teardown-Fehler ueberlagert.

  Empfehlung: `DisposeAsync` idempotent machen — im `finally`-Block nach `_process?.Dispose()` `_process = null;` und nach dem Loeschen des Verzeichnisses `_tempDirectory = null;` setzen. Alternativ den `HasExited`-Zugriff in `try/catch (InvalidOperationException)` kapseln; die Nullsetzung ist der klarere Weg.

- **Fehlermeldung ohne aussagekraeftigen Kontext** — `ApplicationUnavailableSkipReason` (Z. 17-18) fordert fest `dotnet publish Rezepte.Web -c Release`. `ResolveApplicationDllPath` leitet Konfiguration und TFM aber bewusst aus dem Ausgabeverzeichnis der Testassembly ab (Z. 219-224, inkl. erklaerendem Kommentar). Bei einem Debug-Testlauf sucht die Fixture in `bin/Debug/net10.0/publish`, nennt dem Entwickler jedoch einen Release-Publish, der die Ursache nicht behebt — die Tests bleiben nach Befolgen der Anweisung weiterhin uebersprungen.

  Empfehlung: Die Skip-Meldung zur Laufzeit aus der ermittelten Konfiguration und dem erwarteten Pfad zusammensetzen (z. B. `$"Rezepte.Web is not published at '{dllPath}'. Run 'dotnet publish Rezepte.Web -c {configuration}' before running the browser tests."`). Dazu die Konstante durch eine Instanz-Property ersetzen und `LoadingBarBrowserSession` (Z. 16) darauf umstellen.

### loadingBar.js

- **Doppelter Code** — Die Ermittlung des Navigationsziels steht nahezu identisch in beiden Handlern: die Target-Pruefung `if (X.target && X.target !== '_self') { return; }` in `handleLinkClick` (Z. 146-148) und `handleFormSubmit` (Z. 181-184) sowie der Block `const url = resolveUrl(...); if (!url || !isSameOriginNavigation(url)) { return; }` in `handleLinkClick` (Z. 159-162) und `handleFormSubmit` (Z. 188-191). Es handelt sich um dieselbe fachliche Regel („nur Same-Origin-Ziele im eigenen Frame loesen die Animation aus"), die an zwei Stellen gepflegt werden muss.

  Empfehlung: Eine Funktion `function resolveSameOriginTarget(element, rawUrl) { if (element.target && element.target !== '_self') { return null; } const url = resolveUrl(rawUrl); return url && isSameOriginNavigation(url) ? url : null; }` einfuehren und beide Handler auf `const url = resolveSameOriginTarget(anchor, anchor.href);` bzw. `const url = resolveSameOriginTarget(form, rawAction);` mit anschliessendem `if (!url) { return; }` reduzieren.

### LoadingBarPageObject.cs (LoadingBarPageObject) / NetworkDelayHelper.cs (NetworkDelayHelper)

- **Middle Man / Lazy Class** — `DelayRouteAsync` (Z. 61-64) delegiert ohne eigene Logik an die Extension-Methode `NetworkDelayHelper.DelayNavigationAsync`. Diese Extension-Klasse enthaelt genau eine Methode (Z. 11-18) und hat nach der Vereinheitlichung der Aufrufe nur noch diesen einen Aufrufer. Die direkt daneben stehende Schwestermethode `BlockRouteAsync` (Z. 66-69) implementiert ihr `Page.RouteAsync` dagegen inline im Page-Object. Fuer dieselbe Aufgabe (Route-Stubbing im Test) existieren damit zwei unterschiedliche Ablageorte, und die separate Klasse traegt keinen Nutzen mehr.

  Empfehlung: Den Rumpf von `DelayNavigationAsync` nach `LoadingBarPageObject.DelayRouteAsync` ziehen und `NetworkDelayHelper.cs` samt `using`-Eintraegen entfernen — damit liegen beide Route-Helfer an derselben Stelle. (Umgekehrt waere auch `BlockRouteAsync` in die Extension-Klasse zu verschieben; wichtig ist ein einheitlicher Ort.)

### LoadingBarFormNavigationBrowserTests.cs (LoadingBarFormNavigationBrowserTests)

- **Fehlende Kapselung / inkonsistente Nutzung des Page-Objects** — Saemtliche Selektoren der Anwendung sind sonst im Page-Object gebuendelt (`#username`, `#password`, `button.btn-accent[type=submit]` in `LoadingBarPageObject.LoginAsync`, `#loading-bar` als `HostSelector`, Navigationsziele als Konstanten Z. 12-16). Diese Testklasse umgeht das Page-Object und arbeitet direkt auf `pageObject.Page` mit anwendungsspezifischen Selektoren: `#nav-search` (Z. 19), `button[aria-label='Suche starten']` (Z. 20) sowie `button[aria-label='Bearbeiten']`, `button[title='Gruppe hinzufuegen']`, `form.shopping-add-row`, `input[aria-label='Zutat hinzufuegen']` (Z. 37-42). Aendert sich eine dieser UI-Beschriftungen, muss an zwei konzeptuell getrennten Orten nachgezogen werden, und der eigentliche Testablauf ist hinter Playwright-Details verborgen.

  Empfehlung: Die beiden Abläufe als Methoden am Page-Object kapseln, z. B. `SubmitNavigationSearchAsync(string term)` und `SubmitInteractiveShoppingListItemAsync(string itemName)`, und die Selektoren wie die uebrigen als `private const` am Page-Object fuehren. Die Testmethoden bestehen dann nur noch aus fachlichen Schritten und Assertion.

### LoadingBarBrowserSession.cs (LoadingBarBrowserSession)

- **Ressourcenleck im Fehlerpfad** — `StartLoggedInSessionAsync` erzeugt das `LoadingBarPageObject` (Z. 18) und fuehrt danach `LoginAsync` (Z. 19) aus. Schlaegt die Anmeldung fehl (z. B. `WaitForURLAsync`-Timeout nach 10 s), wird das Page-Object nie zurueckgegeben; das `await using` der aufrufenden Testmethode kommt damit nie zustande und der erzeugte `IBrowserContext` wird nicht geschlossen. Bei acht Testmethoden bleiben im Fehlerfall acht Browser-Kontexte bis zum Ende des Testlaufs offen.

  Empfehlung: Den Aufruf absichern: `var pageObject = await LoadingBarPageObject.CreateAsync(...); try { await pageObject.LoginAsync(...); } catch { await pageObject.DisposeAsync(); throw; } return pageObject;`

## Geprüfte Dateien

- `.github/workflows/pr.yml`
- `README.md`
- `Docs/help/index.md`
- `Docs/help/loading-bar-configuration.md`
- `Docs/help/navigation.md`
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
- `Rezepte.Tests/Deployment/CsprojCredentialCopyTests.cs`
- `Rezepte.Tests/Deployment/DeploymentDocumentationTests.cs`
- `Rezepte.Tests/Deployment/LoadingBarWiringTests.cs`
- `Rezepte.Tests/Services/LoadingBarServiceDefaultsTests.cs`
- `Rezepte.Tests/Services/LoadingBarServiceDurationParsingTests.cs`
- `Rezepte.Tests/Services/LoadingBarServiceValidationTests.cs`
- `Rezepte.Tests/TestHelpers/LoadingBarServiceTestFactory.cs`
- `Rezepte.Tests/TestHelpers/RepositoryPaths.cs`
- `Rezepte.Tests.Browser/Rezepte.Tests.Browser.csproj`
- `Rezepte.Tests.Browser/Infrastructure/BrowserTestCollection.cs`
- `Rezepte.Tests.Browser/Infrastructure/ConfiguredRezepteAppFixture.cs`
- `Rezepte.Tests.Browser/Infrastructure/LoadingBarBrowserSession.cs`
- `Rezepte.Tests.Browser/Infrastructure/LoadingBarPageObject.cs`
- `Rezepte.Tests.Browser/Infrastructure/NetworkDelayHelper.cs`
- `Rezepte.Tests.Browser/Infrastructure/PlaywrightBrowserFixture.cs`
- `Rezepte.Tests.Browser/Infrastructure/RezepteAppFixture.cs`
- `Rezepte.Tests.Browser/LoadingBarColorBrowserTests.cs`
- `Rezepte.Tests.Browser/LoadingBarDisabledBrowserTests.cs`
- `Rezepte.Tests.Browser/LoadingBarFormNavigationBrowserTests.cs`
- `Rezepte.Tests.Browser/LoadingBarSafetyTimeoutBrowserTests.cs`
- `Rezepte.Tests.Browser/LoadingBarVisibilityBrowserTests.cs`
