# Code-Review

## Ergebnis

**Status:** Befunde vorhanden

## Befunde

### LoadingBarOptions.cs (LoadingBarOptions)

- **Fehlende Kapselung (veraenderlicher gemeinsamer Zustand)** — `public static readonly string[] DefaultColors` (Z. 47) ist ein oeffentlich zugreifbares, veraenderliches Array. `readonly` schuetzt nur die Referenz, nicht den Inhalt: `LoadingBarOptions.DefaultColors[0] = "#000000"` veraendert die Standardpalette prozessweit. `LoadingBarService` Z. 22 umhuellt genau dieses Array mit `new ReadOnlyCollection<string>(LoadingBarOptions.DefaultColors)` — ein `ReadOnlyCollection` ist aber nur eine Sicht auf das zugrunde liegende Array, sodass die in der vorigen Review-Runde geforderte Unveraenderlichkeit des Default-Pfads faktisch nicht erreicht ist.

  Empfehlung: Das Array `private static readonly` machen und den oeffentlichen Zugriff als `public static readonly IReadOnlyList<string> DefaultColors = new ReadOnlyCollection<string>(new[] { ... });` bereitstellen. In `LoadingBarService` Z. 22 entfaellt die erneute Umhuellung; in `LoadingBarRenderingTests.CreateSettings` (Z. 98) und den Assertions der Service-Tests ist `IReadOnlyList<string>` direkt verwendbar.

### loadingBar.js

- **Doppelter Code / fehlende Kapselung** — Der Block zum verzoegerten Auswerten von `event.defaultPrevented`

  ```js
  setTimeout(function () {
    if (!event.defaultPrevented) {
      startAnimation();
    }
  }, 0);
  ```

  steht wortgleich in `handleLinkClick` (Z. 160-164) und in `handleFormSubmit` (Z. 191-195). Der zweite Kommentar (Z. 189-190) verweist bereits auf den ersten — ein Hinweis darauf, dass hier eine gemeinsame Abstraktion fehlt.

  Empfehlung: Eine Funktion `function startAnimationUnlessPrevented(event) { setTimeout(function () { if (!event.defaultPrevented) { startAnimation(); } }, 0); }` einfuehren, den erklaerenden Kommentar einmalig darueber setzen und beide Handler auf `startAnimationUnlessPrevented(event);` reduzieren.

### LoadingBarPageObject.cs (LoadingBarPageObject) / Browser-Testklassen

- **Middle Man / Einheitlichkeit** — `DelayRouteAsync` (Z. 58-61) leitet ohne eigene Logik an `Page.DelayNavigationAsync` weiter. Gleichzeitig umgehen zwei Tests das Page-Object und rufen die Extension direkt auf: `LoadingBarFormNavigationBrowserTests` Z. 18 (`pageObject.Page.DelayNavigationAsync(...)`) und `LoadingBarSafetyTimeoutBrowserTests` Z. 20 (`pageObject.Page.RouteAsync(...)`). Fuer dieselbe Aufgabe existieren damit zwei Wege.

  Empfehlung: Entweder `DelayRouteAsync` entfernen und in allen Tests einheitlich `pageObject.Page.DelayNavigationAsync(...)` verwenden, oder die Extension nur noch ueber das Page-Object aufrufen. Ein Weg, konsequent in allen fuenf Testklassen.

- **Doppelter Code** — `ClickNavigationLinkAsync` (Z. 53-56) kapselt `Page.ClickAsync($"a[href='{href}']", new PageClickOptions { NoWaitAfter = true })`. `LoadingBarSafetyTimeoutBrowserTests` Z. 22 wiederholt exakt diesen Aufruf inline (`Page.ClickAsync("a[href='/shopping-list']", new PageClickOptions { NoWaitAfter = true })`), statt den vorhandenen Helfer zu nutzen.

  Empfehlung: In `LoadingBarSafetyTimeoutBrowserTests` `await pageObject.ClickNavigationLinkAsync("/shopping-list");` verwenden.

- **Inkonsistente Konstantenhaltung** — Fuer das Kochbuch-Ziel existieren die Konstanten `CookbooksHref` und `CookbooksRouteGlob` (Z. 12-13); die ebenso mehrfach benoetigten Ziele `"/shopping-list"` (`LoadingBarSafetyTimeoutBrowserTests` Z. 20/22, `LoadingBarFormNavigationBrowserTests` Z. 36) und `"**/recipes/search*"` stehen dagegen als Literale in den Tests. Dasselbe Konzept wird zweifach unterschiedlich behandelt.

  Empfehlung: Entweder alle Navigationsziele als Konstantenpaar (`Href` + `RouteGlob`) am Page-Object fuehren oder alle Ziele als Literal im jeweiligen Test belassen.

### LoadingBarColorBrowserTests.cs (LoadingBarColorBrowserTests)

- **Test ohne wirksame Synchronisation (Flakiness-Risiko)** — `SecondClickDuringRunningAnimation_ChangesColor` (Z. 28-44) ruft nach dem zweiten Klick erneut `WaitUntilLoadingBarActiveAsync()` (Z. 40) auf. Zu diesem Zeitpunkt traegt das Host-Element die Klasse `loading-bar-active` bereits aus dem ersten Klick, die Wartebedingung ist also sofort erfuellt und der Aufruf wirkungslos. `loadingBar.js` startet die Animation aber erst im aufgeschobenen `setTimeout(..., 0)`-Callback (Z. 160-164), sodass `GetLoadingBarColorAsync()` (Z. 41) die Farbe des ersten Klicks lesen kann. Der Test synchronisiert also nicht auf das Ereignis, das er prueft.

  Empfehlung: Statt auf „aktiv" auf die tatsaechliche Farbaenderung warten, z. B. `await pageObject.Page.WaitForFunctionAsync($"expected => getComputedStyle(document.querySelector('#loading-bar')).getPropertyValue('--loading-bar-color').trim() !== expected", firstColor);` und danach die Farbe auslesen. Alternativ eine `WaitUntilLoadingBarColorChangedAsync(string previousColor)`-Methode am Page-Object ergaenzen.

### LoadingBarWiringTests.cs (LoadingBarWiringTests)

- **Test prueft Implementierungsdetails statt fachliches Verhalten** — `Layout_ShouldPlaceLoadingBarBetweenNavigationAndMainContent` (Z. 21-33) sucht in `MainLayout.razor` nach der exakten Zeichenkette `"<main class=\"container py-4\">"` (Z. 27). Die Bootstrap-Abstandsklasse `py-4` hat mit der geprueften Aussage (Ladebalken liegt zwischen Navigation und Inhalt) nichts zu tun; eine reine Styling-Aenderung an `<main>` laesst den Test fehlschlagen. Fuer `</nav>` und `<LoadingBar />` wird bereits bewusst locker gesucht (Regex mit `\s*`), fuer `<main>` dagegen exakt inklusive Attributwert.

  Empfehlung: Die Suche auf das Element reduzieren, z. B. `markup.IndexOf("<main", StringComparison.Ordinal)` oder analog zu den anderen beiden Stellen eine Regex `<main[\s>]`.

- **Testmethode prueft mehr als einen fachlichen Fall** — `ServiceCollectionExtensions_ShouldRegisterLoadingBarOptionsAndService` (Z. 69-90) prueft zwei unabhaengige Verdrahtungen: die Bindung der `LoadingBar`-Sektion an `IOptions<LoadingBarOptions>` (Z. 88) und die DI-Registrierung von `ILoadingBarService` (Z. 89). Der Name benennt die Doppelung mit „...OptionsAndService" selbst.

  Empfehlung: In `ServiceCollectionExtensions_ShouldBindLoadingBarOptionsSection` und `ServiceCollectionExtensions_ShouldRegisterLoadingBarService` aufteilen; der Aufbau von `ConfigurationBuilder`/`ServiceCollection` gehoert dann in eine gemeinsame private Hilfsmethode der Testklasse.

### LoadingBar.razor.css

- **Doppelte Defaults** — `height: var(--loading-bar-height, 3px)` (Z. 4) und `animation: rezepte-loading-bar-sweep var(--loading-bar-duration, 2s) ...` (Z. 25) wiederholen die in `LoadingBarOptions.Height`/`AnimationDuration` und in `appsettings.json` gefuehrten Standardwerte an einer dritten Stelle. Da `LoadingBar.razor` (Z. 11) beide Custom Properties immer inline setzt, sind die Fallbacks im Normalbetrieb unerreichbar und koennen unbemerkt von den C#-Defaults abweichen. Fuer den Indikator wurde dieses Problem bereits durch einen bewusst palettenfremden Wert (`#9E9E9E`, Z. 21) entschaerft.

  Empfehlung: Die Fallbacks entfernen (`var(--loading-bar-height)` / `var(--loading-bar-duration)`), da die Komponente die Werte garantiert setzt, oder — falls sie als Notfallwert gewollt sind — bewusst neutrale, nicht aus der Konfiguration stammende Werte verwenden und dies per Kommentar festhalten.

### RezepteAppFixture.cs (RezepteAppFixture)

- **Fehlermeldung ohne aussagekraeftigen Kontext** — Ist `REZEPTE_PUBLISH_DIR` gesetzt, aber falsch (Tippfehler, veraltetes Verzeichnis), liefert `ResolveApplicationDllPath` (Z. 194-199) `null`. Der Aufrufer setzt daraufhin `ApplicationAvailable = false`, und alle Browser-Tests werden mit der Meldung `ApplicationUnavailableSkipReason` uebersprungen: „Rezepte.Web is not published. Run 'dotnet publish ...'". Diese Meldung nennt die falsche Ursache — die Anwendung ist unter Umstaenden publiziert, nur die Umgebungsvariable zeigt ins Leere. Eine bewusst gesetzte, aber fehlerhafte Konfiguration wird damit stillschweigend als „nicht publiziert" ausgegeben.

  Empfehlung: Im Override-Zweig unterscheiden — ist die Variable gesetzt und die DLL fehlt, eine `InvalidOperationException` mit dem aufgeloesten Pfad und dem Variablennamen werfen (bewusste Fehlkonfiguration), statt in den Skip-Pfad zu laufen. Nur der Fallback-Zweig ohne gesetzte Variable darf `null` liefern.

### README.md

- **Fehlerhafte Dokumentstruktur** — Der neue Abschnitt `### Ladebalken und visuelles Feedback` (Z. 121) endet mit `**Konfigurierbare Parameter:**` (Z. 132), unmittelbar gefolgt von der allgemeinen Einstellungstabelle (Z. 134-149). Diese Tabelle beschreibt jedoch saemtliche Anwendungseinstellungen (`ConnectionStrings:Default`, `Jwt:*`, `Images:*`, `AI:*`, `GoogleCredentials:*`, `PluginUpdates:*`). Durch die neue Ueberschrift steht die gesamte Konfigurationstabelle nun unter „Ladebalken und visuelles Feedback" und wird als dessen Parameterliste ausgewiesen.

  Empfehlung: Den Ladebalken-Abschnitt hinter die allgemeine Tabelle verschieben oder ihn auf einen kurzen Absatz mit Verweis auf `Docs/help/loading-bar-configuration.md` reduzieren. In jedem Fall die Zeile `**Konfigurierbare Parameter:**` (Z. 132) entfernen, da sie eine falsche Zuordnung erzeugt.

### Docs/help/loading-bar-configuration.md

- **Falsche Angabe zum Umgebungsvariablenformat** — Z. 87 nennt das Format `LoadingBar___{ParameterName}` (drei Unterstriche), waehrend derselbe Satz „doppeltem Unterstrich als Trennzeichen" schreibt und alle Beispiele (Z. 93-100) korrekt zwei Unterstriche verwenden. Ein Administrator, der die Formatzeile uebernimmt, erhaelt eine wirkungslose Variable.

  Empfehlung: Z. 87 auf `LoadingBar__{ParameterName}` korrigieren.

- **Falsche Angabe zum Validierungszeitpunkt** — Z. 62 behauptet „Alle Konfigurationsparameter werden beim Start der Anwendung validiert". `LoadingBarService` validiert ueber `Lazy<LoadingBarSettings>` (Z. 30/35) erst beim ersten Aufruf von `GetSettings()`, also beim ersten Rendern der Komponente. Die dokumentierten Warnungen erscheinen im Protokoll deshalb nicht beim Hochfahren, sondern beim ersten Seitenaufruf — Punkt 2 der Problembehandlung („Pruefen Sie die Anwendungsprotokolle auf Validierungswarnungen", Z. 114) fuehrt sonst in die Irre.

  Empfehlung: Formulierung auf „beim ersten Rendern der Ladeanimation (verzoegerte Auswertung)" aendern.

### README.md / Docs/help/navigation.md / Docs/help/loading-bar-configuration.md

- **Fehler in neu hinzugefuegtem, benutzersichtbarem Text** — In den neuen Abschnitten stehen mehrere Schreibfehler und ein falsches Sonderzeichen: `navigation.md` Z. 34 verwendet `Formulareneː` mit dem Unicode-Zeichen U+02D0 (MODIFIER LETTER TRIANGULAR COLON) statt eines Doppelpunkts, ausserdem Z. 36 `neu gewaelt` (statt `gewaehlt`) und durchgehend `Die Ladebalke` (statt `Der Ladebalken`, Z. 36/40). `README.md` Z. 125 `beim Benutzerinterakt`, Z. 128 `auslaueft`, Z. 130 `standarmaessig`. `loading-bar-configuration.md` Z. 14 `der Ladebalke`, Z. 15 `keinen Abschlusssignal`, Z. 81 `die beiden gueltig Farben`.

  Empfehlung: Die genannten Stellen korrigieren; `navigation.md` Z. 34 zusaetzlich auf ein normales `:` umstellen, damit die Datei keine unerwarteten Unicode-Zeichen enthaelt.

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
