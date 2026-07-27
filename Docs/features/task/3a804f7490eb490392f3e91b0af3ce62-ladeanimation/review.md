# Plan-Review

## Ergebnis

**Status:** Vollständig umgesetzt

Geprüft wurden alle Planelemente aus `plan.md` (Abschnitte „Neue Klassen", „Änderungen an bestehenden Klassen", „Validierungsregeln", „Konfigurationsänderungen", „Umsetzungsreihenfolge", „Tests") gegen den Code im Arbeitsverzeichnis. `dotnet build Rezepte.sln` läuft fehlerfrei durch (0 Fehler).

Hinweis zur Verzeichnisstruktur: Das Repository verwendet kein `src/`-Verzeichnis; die Projekte liegen direkt im Wurzelverzeichnis (`Rezepte.Web/`, `Rezepte.Tests/`, `Rezepte.Tests.Browser/`). Die Prüfung erfolgte entsprechend dort.

## Umgesetzte Planelemente

### Neue Klassen und Dateien

- [x] `LoadingBarOptions` (Options-Klasse, `Rezepte.Web/Configuration/LoadingBarOptions.cs`) — angelegt, `sealed`, englische XML-Dokumentation
- [x] Feld `Enabled` (`bool`, Standard `true`) in `LoadingBarOptions` — vorhanden
- [x] Feld `Height` (`string`, Standard `"3px"`) in `LoadingBarOptions` — vorhanden
- [x] Feld `AnimationDuration` (`string`, Standard `"2s"`) in `LoadingBarOptions` — vorhanden
- [x] Feld `HideDelay` (`string`, Standard `"300ms"`) in `LoadingBarOptions` — vorhanden
- [x] Feld `MaxVisibleDuration` (`string`, Standard `"15s"`) in `LoadingBarOptions` — vorhanden
- [x] Feld `Colors` (`string[]`, sechs Standardfarben laut Plan) in `LoadingBarOptions` — vorhanden
- [x] `LoadingBarViewModel` (unveränderliches `sealed record`, `Rezepte.Web/ViewModels/LoadingBarViewModel.cs`) — angelegt mit `Enabled`, `Height`, `AnimationDuration`, `Colors`, `HideDelayMilliseconds`, `MaxVisibleDurationMilliseconds`
- [x] `ILoadingBarService` (Interface, `Rezepte.Web/Services/ILoadingBarService.cs`) — angelegt mit genau einer Methode `GetSettings()`; kein `ShowAsync`/`HideAsync` (plankonform)
- [x] `LoadingBarService` (Klasse, `Rezepte.Web/Services/LoadingBarService.cs`) — angelegt mit `IOptions<LoadingBarOptions>` und `ILogger<LoadingBarService>`
- [x] Methode `GetSettings()` in `LoadingBarService` (public) — vorhanden, Ergebnis über `Lazy<LoadingBarViewModel>` zwischengespeichert
- [x] `LoadingBar` (Blazor-Komponente, `Rezepte.Web/Components/Layout/LoadingBar.razor`) — angelegt, kein Render-Mode (statisches SSR), Property-Injection von `ILoadingBarService`
- [x] Scoped Stylesheet `Rezepte.Web/Components/Layout/LoadingBar.razor.css` — angelegt
- [x] Clientskript `Rezepte.Web/wwwroot/js/loadingBar.js` — angelegt als selbstinitialisierende IIFE
- [x] Testprojekt `Rezepte.Tests.Browser` (`Rezepte.Tests.Browser/Rezepte.Tests.Browser.csproj`) — angelegt auf `net10.0` mit `Microsoft.NET.Test.Sdk`, `xunit`, `xunit.runner.visualstudio`, `FluentAssertions`, `Microsoft.Playwright` 1.61.0, `Xunit.SkippableFact` 1.5.61
- [x] `RezepteAppFixture` (`Rezepte.Tests.Browser/Infrastructure/RezepteAppFixture.cs`) — angelegt
- [x] `PlaywrightBrowserFixture` (`Rezepte.Tests.Browser/Infrastructure/PlaywrightBrowserFixture.cs`) — angelegt, Feld `BrowsersAvailable` für den Skip-Pfad
- [x] `LoadingBarPageObject` (`Rezepte.Tests.Browser/Infrastructure/LoadingBarPageObject.cs`) — angelegt
- [x] `BrowserTestCollection` (`Rezepte.Tests.Browser/Infrastructure/BrowserTestCollection.cs`) — angelegt als `[CollectionDefinition]` mit `ICollectionFixture<PlaywrightBrowserFixture>` und `ICollectionFixture<RezepteAppFixture>`

### Methoden der neuen Klassen

- [x] Methode `LoginAsync` in `LoadingBarPageObject` (public) — vorhanden
- [x] Methode `GotoAsync` in `LoadingBarPageObject` (public) — vorhanden
- [x] Methode `IsLoadingBarActiveAsync` in `LoadingBarPageObject` (public) — vorhanden
- [x] Methode `GetLoadingBarColorAsync` in `LoadingBarPageObject` (public) — vorhanden
- [x] Methode `GetLoadingBarOpacityAsync` in `LoadingBarPageObject` (public) — vorhanden
- [x] Methode `WaitUntilLoadingBarHiddenAsync` in `LoadingBarPageObject` (public) — vorhanden, Playwright-`WaitForFunctionAsync` statt fester Wartezeit
- [x] Methode `DelayNextNavigationAsync` in `LoadingBarPageObject` (public) — vorhanden, per `Page.RouteAsync`
- [x] Freien TCP-Port ermitteln, temporäres Verzeichnis mit eigener SQLite-Datei anlegen, Anwendung als Kindprozess starten, auf Erreichbarkeit pollen in `RezepteAppFixture` — vorhanden
- [x] Überschreibung des `LoadingBar`-Abschnitts per Umgebungsvariablen in `RezepteAppFixture` — vorhanden über `protected virtual GetEnvironmentOverrides()`
- [x] Seeding eines Testbenutzers über `POST api/auth/register` in `RezepteAppFixture` — vorhanden
- [x] Aufräumlogik `DisposeAsync` in `RezepteAppFixture` — vorhanden mit `Kill(entireProcessTree: true)` und `Directory.Delete(recursive: true)`

### Validierungsregeln (`LoadingBarService`)

- [x] `Height`: CSS-Länge `px`/`rem`/`em`, sonst Rückfall auf `3px` + Warn-Log — umgesetzt (`CssLengthPattern`)
- [x] `AnimationDuration`: CSS-Zeit `ms`/`s`, sonst Rückfall auf `2s` + Warn-Log — umgesetzt (`CssTimePattern`)
- [x] `HideDelay`: CSS-Zeit, Umrechnung nach Millisekunden, sonst Rückfall auf 300 ms + Warn-Log — umgesetzt
- [x] `MaxVisibleDuration`: CSS-Zeit, Umrechnung nach Millisekunden, muss größer als `HideDelay` sein, sonst Rückfall auf 15000 ms + Warn-Log — umgesetzt
- [x] `Colors` (Einzeleintrag): Hex `#RGB`/`#RRGGBB`, sonst Entfernen + Warn-Log — umgesetzt (`HexColorPattern`)
- [x] `Colors` (Liste): mindestens ein Eintrag nach Filterung, sonst Standardliste + Warn-Log — umgesetzt
- [x] Es wird nie eine Exception geworfen; alle Log-Meldungen sind englischsprachig — eingehalten

### Änderungen an bestehenden Dateien

- [x] `MainLayout.razor` — `<LoadingBar />` steht direkt nach `</nav>` und vor `<main class="container py-4">`; `searchQuery`, `OnSubmitSearch()` und die `NavigationManager`-Injektion sind unverändert
- [x] `App.razor` — `<script src="js/loadingBar.js"></script>` unmittelbar nach `_framework/blazor.web.js`
- [x] `ServiceCollectionExtensions.AddRezepteServices` — `services.Configure<LoadingBarOptions>(configuration.GetSection("LoadingBar"))` nach `GoogleCredentialsOptions` ergänzt
- [x] `ServiceCollectionExtensions.AddRezepteServices` — `services.AddSingleton<ILoadingBarService, LoadingBarService>()` im Block der Anwendungsdienste ergänzt
- [x] `MainLayout.razor.css` — unverändert (plankonform)
- [x] `appsettings.json` — neuer Abschnitt `LoadingBar` mit `Enabled`, `Height`, `AnimationDuration`, `HideDelay`, `MaxVisibleDuration`, `Colors` und den geplanten Standardwerten
- [x] `Rezepte.sln` — Projekteintrag `Rezepte.Tests.Browser` inkl. Debug/Release für `Any CPU`, `x64` und `x86`
- [x] `Rezepte.Tests/Rezepte.Tests.csproj` — unverändert (Zweig 14b gewählt, kein `bunit`-Paketverweis)
- [x] `.github/workflows/pr.yml` — Build-Schritt für `Rezepte.Tests.Browser`, Installationsschritt für Chromium über `playwright.ps1 install --with-deps chromium`, Zeitlimit von 20 auf 30 Minuten angehoben, `dotnet test Rezepte.sln --configuration Release --no-build` unverändert

### Markup- und Skriptvertrag

- [x] Host-Element mit fester Id `loading-bar` und Kindelement `.loading-bar-indicator` — vorhanden
- [x] Bedingtes Rendern über `@if (Settings.Enabled)` — vorhanden
- [x] `aria-hidden="true"` und `data-permanent` auf dem Host-Element — vorhanden
- [x] CSS-Custom-Properties `--loading-bar-height` und `--loading-bar-duration` im `style`-Attribut — vorhanden
- [x] `data-colors`, `data-hide-delay`, `data-max-visible-duration` — vorhanden
- [x] `LoadingBar.razor.css`: Grundmaße, Farbe über `--loading-bar-color`, Opazitäts-Transition statt `display` — vorhanden
- [x] Eindeutig präfigierte `@keyframes rezepte-loading-bar-sweep` (100 % → -30 %, also rechts nach links) — vorhanden
- [x] `@media (prefers-reduced-motion: reduce)`-Variante ohne Bewegung (statischer Balken über volle Breite) — vorhanden
- [x] Skript beendet sich ohne Nebenwirkungen, wenn das Host-Element fehlt — vorhanden
- [x] Auslesen von Farbliste, `HideDelay`, `MaxVisibleDuration` aus den `data-*`-Attributen — vorhanden
- [x] `click`-Listener auf `document` in der Capture-Phase mit Anker-Auflösung über `closest('a[href]')` — vorhanden
- [x] Klick-Filterung: `defaultPrevented`, Nicht-Primärtaste, Modifikatortasten, `target` ungleich `_self`, `download`, `mailto:`/`tel:`/`javascript:`, Fragment-/identische Adresse, fremder Origin — vorhanden
- [x] `submit`-Listener auf `document` in der Capture-Phase mit Formularauflösung aus `event.target` — vorhanden
- [x] Submit-Filterung: `defaultPrevented`, `target` ungleich `_self`, fremder Origin aus `formaction`/`action` — vorhanden
- [x] Gemeinsamer Ablauf „Animation starten" (`startAnimation`): Host auflösen, Ausblend-Timer abbrechen, Farbe wählen und als `--loading-bar-color` setzen, Aktiv-Klasse entfernen → Reflow → setzen, Sicherheits-Timer neu starten — vorhanden
- [x] Zufällige Farbwahl mit Ausschluss der zuletzt verwendeten Farbe bei mehr als einer Farbe — vorhanden (`pickColor`)
- [x] Abschluss-Erkennung über `Blazor.addEventListener('enhancedload', …)` mit defensiver Neuauflösung des Host-Elements — vorhanden
- [x] Rückfallebene `pageshow` auf `window` — vorhanden
- [x] Ausblenden nach `HideDelayMilliseconds`, Sicherheits-Timeout nach `MaxVisibleDurationMilliseconds` ohne `HideDelay` — vorhanden

### Tests

- [x] `LoadingBarServiceTests_Defaults` (`Rezepte.Tests/Services/`) — Hilfsmethode `CreateService(LoadingBarOptions)` sowie `GetSettings_WithDefaultOptions_ReturnsDocumentedDefaults`, `GetSettings_WithEnabledFalse_ReturnsDisabledSettings`, `GetSettings_CalledTwice_ReturnsSameInstance`, `GetSettings_WithValidCustomOptions_ReturnsConfiguredValues`
- [x] `LoadingBarServiceTests_Validation` (`Rezepte.Tests/Services/`) — `GetSettings_WithInvalidHeight_FallsBackToDefaultHeight`, `GetSettings_WithInvalidAnimationDuration_FallsBackToDefaultDuration`, `GetSettings_WithInvalidHideDelay_FallsBackToDefaultHideDelay`, `GetSettings_WithMaxVisibleDurationBelowHideDelay_FallsBackToDefault`, `GetSettings_WithInvalidColorEntries_RemovesInvalidEntries`, `GetSettings_WithOnlyInvalidColors_FallsBackToDefaultColors`
- [x] `LoadingBarServiceTests_DurationParsing` (`Rezepte.Tests/Services/`) — `GetSettings_WithHideDelayInMilliseconds_ConvertsToMilliseconds`, `GetSettings_WithHideDelayInSeconds_ConvertsToMilliseconds`, `GetSettings_WithMaxVisibleDurationInSeconds_ConvertsToMilliseconds`
- [x] Zweig 14b gewählt: `LoadingBarMarkupTests_Rendering` (`Rezepte.Tests/Components/`) mit allen fünf geplanten Rendering-Prüfungen (`Render_WhenEnabled_RendersHostElementWithConfiguredId`, `Render_WhenDisabled_RendersNoMarkup`, `Render_WhenEnabled_WritesHeightAndDurationAsCssCustomProperties`, `Render_WhenEnabled_WritesColorsAndTimingsAsDataAttributes`, `Render_WhenEnabled_MarksBarAsDecorativeAndPermanent`)
- [x] `LoadingBarWiringTests` (`Rezepte.Tests/Deployment/`) — `Layout_ShouldPlaceLoadingBarDirectlyBelowNavigation`, `App_ShouldLoadLoadingBarScriptAfterBlazorScript`, `Configuration_ShouldProvideLoadingBarSectionAndServiceRegistration`, `Script_ShouldRegisterClickAndSubmitListenersInCapturePhase`
- [x] `LoadingBarVisibilityBrowserTests` — `LinkClick_WithDelayedResponse_ShowsAnimatedBarBelowNavigation`, `AfterNavigationCompleted_HidesLoadingBar`
- [x] `LoadingBarColorBrowserTests` — `LinkClick_UsesColorFromConfiguredPalette`, `SecondClickDuringRunningAnimation_ChangesColor`
- [x] `LoadingBarFormNavigationBrowserTests` — `SearchSubmit_ShowsLoadingBar`
- [x] `LoadingBarSafetyTimeoutBrowserTests` — `WhenNavigationNeverCompletes_HidesBarAfterMaxVisibleDuration` mit eigener Fixture-Instanz (`LoadingBar__MaxVisibleDuration=800ms`)
- [x] `LoadingBarDisabledBrowserTests` — `WhenFeatureDisabled_PageContainsNoLoadingBarElement` mit eigener Fixture-Instanz (`LoadingBar__Enabled=false`)
- [x] Alle Browsertests sind `[SkippableFact]` mit `Skip.IfNot(browserFixture.BrowsersAvailable, …)` — plankonform
- [x] Keine bestehenden Tests angepasst — plankonform

## Offene Aufgaben

Keine.

## Hinweise

Die folgenden Punkte sind keine Lücken gegenüber dem Plan, sollten aber für Code-Review und Dokumentation bekannt sein:

1. **Start der Anwendung im Browsertest weicht vom Plan ab.** Der Plan sah `dotnet run --project Rezepte.Web --no-build --configuration Release` vor. `RezepteAppFixture.FindApplicationDll()` startet stattdessen die **publizierte** Anwendung (`Rezepte.Web/bin/Release/net10.0/publish/Rezepte.Web.dll`). Begründung ist im Code dokumentiert: Ein reiner `dotnet build`-Output liefert statische Dateien über `MapStaticAssets()` außerhalb von `dotnet run` mit leerem Body aus, wodurch `js/loadingBar.js` nicht geladen würde. Folgeänderung: In `.github/workflows/pr.yml` wurde ein zusätzlicher, im Plan nicht vorgesehener Schritt „Publish application for browser tests" ergänzt (in der Tasks-Datei als neue Aufgabe 79 nachgetragen). Wer die Browsertests lokal ausführt, muss vorher `dotnet publish Rezepte.Web -c Release` aufrufen, sonst schlagen sie mit `FileNotFoundException` fehl.

2. **Ergebnis der bUnit-Machbarkeitsprüfung (Planschritt 13) ist nicht schriftlich dokumentiert.** Die Weichenstellung ist nur implizit erkennbar: Zweig 14b wurde umgesetzt (kein `bunit`-`PackageReference` in `Rezepte.Tests.csproj`, `LoadingBarMarkupTests_Rendering` statt `LoadingBarComponentTests_Rendering`). Falls die Begründung nachvollziehbar bleiben soll, gehört sie in die Feature-Dokumentation (Schritt 9 des Lifecycles).

3. **Zusätzliche Bedingung in der Klick-Filterung.** Der Plan verlangt, Klicks auf die aktuelle Adresse zu ignorieren. Implementiert ist `if (!isBarCurrentlyActive() && isFragmentOrCurrentAddress(url)) return;` — bei bereits laufender Animation wird ein erneuter Klick auf dieselbe Adresse also **nicht** ignoriert, sondern startet die Animation mit neuer Farbe neu. Das deckt sich mit der Planentscheidung „Mehrfache Navigation: laufende Animation wird neu gestartet und erhält eine neue Zufallsfarbe" und ist Voraussetzung für `SecondClickDuringRunningAnimation_ChangesColor`, geht aber über den Wortlaut der Filterliste hinaus. Zusätzlich wird über `lastConfirmedUrl` statt `window.location.href` verglichen, weil Enhanced Navigation die Adresse optimistisch vor dem Antworteingang setzt (im Code kommentiert).

4. **`LoadingBarViewModel` trägt ein `<returns>`-Element in der XML-Dokumentation**, obwohl es sich um einen Record-Typ und nicht um eine Methode handelt. Rein kosmetisch, für das Code-Review relevant.

5. **Farbrückfall gibt das statische Defaults-Array zurück.** `ValidateColors` liefert im Fehlerfall `Defaults.Colors` — also das `string[]` einer statischen `LoadingBarOptions`-Instanz — direkt als `IReadOnlyList<string>` zurück. Das Array ist damit theoretisch über einen Cast veränderbar; das `LoadingBarViewModel` ist an dieser Stelle nicht vollständig unveränderlich. Für das Code-Review, nicht für die Planerfüllung.

6. **CI-Laufzeit.** Das Zeitlimit wurde wie geplant von 20 auf 30 Minuten angehoben. Ob das mit Chromium-Installation, `dotnet publish` und je einem Anwendungsstart pro Fixture (drei Fixture-Instanzen: Standard, Safety-Timeout, Disabled) ausreicht, zeigt erst der erste vollständige CI-Lauf.
