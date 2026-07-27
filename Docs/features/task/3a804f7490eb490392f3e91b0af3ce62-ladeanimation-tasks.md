# Tasks: Ladeanimation

| # | Bereich | Aufgabe | Status | Testnachweis |
|---|---------|---------|--------|--------------|
| 1 | Konfiguration | `LoadingBarOptions` in `Rezepte.Web/Configuration/` anlegen (Enabled, Height, AnimationDuration, HideDelay, MaxVisibleDuration, Colors mit Standardwerten) | Offen | — |
| 2 | Konfiguration | Abschnitt `LoadingBar` in `Rezepte.Web/appsettings.json` ergänzen | Offen | — |
| 3 | Konfiguration | `services.Configure<LoadingBarOptions>(configuration.GetSection("LoadingBar"))` in `ServiceCollectionExtensions.AddRezepteServices` ergänzen | Offen | — |
| 4 | Datenmodell | `LoadingBarViewModel` als unveränderliches Record in `Rezepte.Web/ViewModels/` anlegen | Offen | — |
| 5 | Logik | `ILoadingBarService` mit Methode `GetSettings()` in `Rezepte.Web/Services/` anlegen | Offen | — |
| 6 | Logik | `LoadingBarService` implementieren (Abhängigkeiten `IOptions<LoadingBarOptions>`, `ILogger<LoadingBarService>`) | Offen | — |
| 7 | Logik | Zwischenspeicherung des normalisierten `LoadingBarViewModel` in `LoadingBarService` implementieren | Offen | — |
| 8 | Logik | `services.AddSingleton<ILoadingBarService, LoadingBarService>()` in `ServiceCollectionExtensions` registrieren | Offen | — |
| 9 | Validierung | Prüfung und Rückfall für `Height` (CSS-Länge px/rem/em) in `LoadingBarService` implementieren | Offen | — |
| 10 | Validierung | Prüfung und Rückfall für `AnimationDuration` (CSS-Zeit ms/s) implementieren | Offen | — |
| 11 | Validierung | Prüfung, Rückfall und Millisekunden-Umrechnung für `HideDelay` implementieren | Offen | — |
| 12 | Validierung | Prüfung, Rückfall und Millisekunden-Umrechnung für `MaxVisibleDuration` inklusive Mindestabstand zu `HideDelay` implementieren | Offen | — |
| 13 | Validierung | Hex-Farbprüfung je Eintrag von `Colors` mit Entfernen ungültiger Einträge implementieren | Offen | — |
| 14 | Validierung | Rückfall auf die Standardfarbliste bei leerer oder vollständig ungültiger `Colors`-Liste implementieren | Offen | — |
| 15 | Validierung | Warn-Logging (englischsprachig) für alle verworfenen Konfigurationswerte ergänzen | Offen | — |
| 16 | UI | `LoadingBar.razor` in `Rezepte.Web/Components/Layout/` anlegen (Host- und Indikator-Element) | Offen | — |
| 17 | UI | Bedingtes Rendern in `LoadingBar.razor`: bei `Enabled == false` kein Markup ausgeben | Offen | — |
| 18 | UI | `aria-hidden="true"` und `data-permanent` auf dem Host-Element in `LoadingBar.razor` setzen | Offen | — |
| 19 | UI | CSS-Custom-Properties `--loading-bar-height` und `--loading-bar-duration` in `LoadingBar.razor` ausgeben | Offen | — |
| 20 | UI | `data-*`-Attribute für Farbliste, `HideDelay` und `MaxVisibleDuration` in `LoadingBar.razor` ausgeben | Offen | — |
| 21 | UI | `LoadingBar.razor.css` anlegen: Maße, Farbverlauf über `--loading-bar-color`, Opazitäts-Transition | Offen | — |
| 22 | UI | Eindeutig benannte `@keyframes` für den Sweep von rechts nach links in `LoadingBar.razor.css` definieren | Offen | — |
| 23 | UI | `prefers-reduced-motion: reduce`-Variante ohne Bewegung in `LoadingBar.razor.css` ergänzen | Offen | — |
| 24 | UI | `<LoadingBar />` in `MainLayout.razor` direkt nach `</nav>` einbinden | Offen | — |
| 25 | UI | `<script src="js/loadingBar.js"></script>` in `App.razor` nach `_framework/blazor.web.js` einbinden | Offen | — |
| 26 | Client-Skript | `Rezepte.Web/wwwroot/js/loadingBar.js` anlegen mit Auflösung des Host-Elements und Auslesen der `data-*`-Konfiguration | Offen | — |
| 27 | Client-Skript | Gemeinsamen Ablauf „Animation starten" (Timer-Abbruch, Farbwahl, Neustart, Sicherheits-Timer) als eine Funktion implementieren | Offen | — |
| 28 | Client-Skript | Click-Listener in der Capture-Phase auf `document` mit Anker-Auflösung über `closest` implementieren | Offen | — |
| 29 | Client-Skript | Klick-Filterung implementieren (`defaultPrevented`, Modifikatortasten, Nicht-Primärtaste, `target` ungleich `_self`, `download`, `mailto:`/`tel:`/`javascript:`, Fragment-Links, identische Adresse, fremder Origin) | Offen | — |
| 30 | Client-Skript | Submit-Listener in der Capture-Phase auf `document` implementieren (Formular aus `event.target` auflösen) | Offen | — |
| 31 | Client-Skript | Submit-Filterung implementieren (`defaultPrevented`, `target` ungleich `_self`, fremder Origin aus `action`/`formaction`) | Offen | — |
| 32 | Client-Skript | Zufällige Farbwahl mit Ausschluss der zuletzt verwendeten Farbe implementieren und als `--loading-bar-color` setzen | Offen | — |
| 33 | Client-Skript | Neustart der Animation über Entfernen/Reflow/Setzen der Aktiv-Klasse implementieren | Offen | — |
| 34 | Client-Skript | Ausblenden nach `HideDelay` inklusive Abbruch laufender Timer implementieren | Offen | — |
| 35 | Client-Skript | Sicherheits-Timeout über `MaxVisibleDuration` implementieren | Offen | — |
| 36 | Client-Skript | `Blazor.addEventListener('enhancedload', …)` als Abschlusssignal registrieren (inkl. defensiver Neuauflösung des Host-Elements) | Offen | — |
| 37 | Client-Skript | `pageshow`-Listener auf `window` als Rückfallebene registrieren (inkl. bfcache-Rücksprung) | Offen | — |
| 38 | Tests | Hilfsmethode `CreateService(LoadingBarOptions)` in `LoadingBarServiceTests_Defaults` bereitstellen | Offen | — |
| 39 | Tests | `GetSettings_WithDefaultOptions_ReturnsDocumentedDefaults` in `LoadingBarServiceTests_Defaults` schreiben | Offen | — |
| 40 | Tests | `GetSettings_WithEnabledFalse_ReturnsDisabledSettings` in `LoadingBarServiceTests_Defaults` schreiben | Offen | — |
| 41 | Tests | `GetSettings_CalledTwice_ReturnsSameInstance` in `LoadingBarServiceTests_Defaults` schreiben | Offen | — |
| 42 | Tests | `GetSettings_WithValidCustomOptions_ReturnsConfiguredValues` in `LoadingBarServiceTests_Defaults` schreiben | Offen | — |
| 43 | Tests | `GetSettings_WithInvalidHeight_FallsBackToDefaultHeight` in `LoadingBarServiceTests_Validation` schreiben | Offen | — |
| 44 | Tests | `GetSettings_WithInvalidAnimationDuration_FallsBackToDefaultDuration` in `LoadingBarServiceTests_Validation` schreiben | Offen | — |
| 45 | Tests | `GetSettings_WithInvalidHideDelay_FallsBackToDefaultHideDelay` in `LoadingBarServiceTests_Validation` schreiben | Offen | — |
| 46 | Tests | `GetSettings_WithMaxVisibleDurationBelowHideDelay_FallsBackToDefault` in `LoadingBarServiceTests_Validation` schreiben | Offen | — |
| 47 | Tests | `GetSettings_WithInvalidColorEntries_RemovesInvalidEntries` in `LoadingBarServiceTests_Validation` schreiben | Offen | — |
| 48 | Tests | `GetSettings_WithOnlyInvalidColors_FallsBackToDefaultColors` in `LoadingBarServiceTests_Validation` schreiben | Offen | — |
| 49 | Tests | `GetSettings_WithHideDelayInMilliseconds_ConvertsToMilliseconds` in `LoadingBarServiceTests_DurationParsing` schreiben | Offen | — |
| 50 | Tests | `GetSettings_WithHideDelayInSeconds_ConvertsToMilliseconds` in `LoadingBarServiceTests_DurationParsing` schreiben | Offen | — |
| 51 | Tests | `GetSettings_WithMaxVisibleDurationInSeconds_ConvertsToMilliseconds` in `LoadingBarServiceTests_DurationParsing` schreiben | Offen | — |
| 52 | Tests | Kompatibilität von `bunit` mit `net10.0` per Testbuild und Render-Smoke-Test prüfen und das Ergebnis dokumentieren (Weiche für Aufgabe 53 oder 54) | Offen | — |
| 53 | Tests | Zweig „bUnit funktioniert": `bunit`-`PackageReference` in `Rezepte.Tests.csproj` belassen und `LoadingBarComponentTests_Rendering` mit gemocktem `ILoadingBarService` und den fünf Rendering-Tests schreiben | Offen | — |
| 54 | Tests | Zweig „bUnit funktioniert nicht": `bunit`-`PackageReference` wieder entfernen und `LoadingBarMarkupTests_Rendering` als markup-basierte Dateiprüfung mit denselben fünf Prüfpunkten schreiben | Offen | — |
| 55 | Tests | `Layout_ShouldPlaceLoadingBarDirectlyBelowNavigation` in `LoadingBarWiringTests` schreiben | Offen | — |
| 56 | Tests | `App_ShouldLoadLoadingBarScriptAfterBlazorScript` in `LoadingBarWiringTests` schreiben | Offen | — |
| 57 | Tests | `Configuration_ShouldProvideLoadingBarSectionAndServiceRegistration` in `LoadingBarWiringTests` schreiben | Offen | — |
| 58 | Tests | `Script_ShouldRegisterClickAndSubmitListenersInCapturePhase` in `LoadingBarWiringTests` schreiben | Offen | — |
| 59 | E2E-Infrastruktur | Testprojekt `Rezepte.Tests.Browser` (net10.0, xunit, FluentAssertions, `Microsoft.Playwright`, `Xunit.SkippableFact`) anlegen | Offen | — |
| 60 | E2E-Infrastruktur | `Rezepte.Tests.Browser` in `Rezepte.sln` inklusive aller Konfigurations-/Plattformzuordnungen eintragen | Offen | — |
| 61 | E2E-Infrastruktur | `PlaywrightBrowserFixture` implementieren (Chromium headless, Erkennung fehlender Browser-Binaries für den Skip-Pfad) | Offen | — |
| 62 | E2E-Infrastruktur | `RezepteAppFixture` implementieren: freien Port ermitteln, temporäres Verzeichnis mit eigener SQLite-Datei anlegen, Anwendung als Kindprozess starten, auf Erreichbarkeit pollen | Offen | — |
| 63 | E2E-Infrastruktur | `RezepteAppFixture` um Überschreibung des `LoadingBar`-Abschnitts per Umgebungsvariablen (`LoadingBar__*`) erweitern | Offen | — |
| 64 | E2E-Infrastruktur | Seeding eines Testbenutzers über `POST api/auth/register` in `RezepteAppFixture` implementieren | Offen | — |
| 65 | E2E-Infrastruktur | Aufräumlogik in `RezepteAppFixture.DisposeAsync` implementieren (Kindprozess zuverlässig beenden, temporäres Verzeichnis löschen) | Offen | — |
| 66 | E2E-Infrastruktur | `LoadingBarPageObject` implementieren (`LoginAsync`, `GotoAsync`, `IsLoadingBarActiveAsync`, `GetLoadingBarColorAsync`, `GetLoadingBarOpacityAsync`, `WaitUntilLoadingBarHiddenAsync`) | Offen | — |
| 67 | E2E-Infrastruktur | `DelayNextNavigationAsync` per Playwright-Routen-Interception in `LoadingBarPageObject` implementieren | Offen | — |
| 68 | E2E-Infrastruktur | `BrowserTestCollection` als xUnit-Collection-Definition für gemeinsame Anwendungs- und Browser-Fixture anlegen | Offen | — |
| 69 | E2E-Tests | `LinkClick_WithDelayedResponse_ShowsAnimatedBarBelowNavigation` in `LoadingBarVisibilityBrowserTests` schreiben | Offen | — |
| 70 | E2E-Tests | `AfterNavigationCompleted_HidesLoadingBar` in `LoadingBarVisibilityBrowserTests` schreiben | Offen | — |
| 71 | E2E-Tests | `LinkClick_UsesColorFromConfiguredPalette` in `LoadingBarColorBrowserTests` schreiben | Offen | — |
| 72 | E2E-Tests | `SecondClickDuringRunningAnimation_ChangesColor` in `LoadingBarColorBrowserTests` schreiben | Offen | — |
| 73 | E2E-Tests | `SearchSubmit_ShowsLoadingBar` in `LoadingBarFormNavigationBrowserTests` schreiben | Offen | — |
| 74 | E2E-Tests | `WhenNavigationNeverCompletes_HidesBarAfterMaxVisibleDuration` in `LoadingBarSafetyTimeoutBrowserTests` mit eigener Fixture-Instanz und kurzem `LoadingBar__MaxVisibleDuration` schreiben | Offen | — |
| 75 | E2E-Tests | `WhenFeatureDisabled_PageContainsNoLoadingBarElement` in `LoadingBarDisabledBrowserTests` mit eigener Fixture-Instanz und `LoadingBar__Enabled=false` schreiben | Offen | — |
| 76 | CI | Build-Schritt für `Rezepte.Tests.Browser` in `.github/workflows/pr.yml` ergänzen | Offen | — |
| 77 | CI | Installationsschritt für Chromium über `playwright.ps1 install --with-deps chromium` in `.github/workflows/pr.yml` ergänzen | Offen | — |
| 78 | CI | Zeitlimit des Jobs `verify` in `.github/workflows/pr.yml` prüfen und bei Bedarf anheben | Offen | — |
