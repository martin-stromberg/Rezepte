# Code-Review

## Ergebnis

**Status:** Befunde vorhanden

## Befunde

### LoadingBarService.cs (LoadingBarService)

- **Doppelter Code** — `ValidateCssLength` (Z. 62-71), `ValidateCssTime` (Z. 73-82) und `ValidateCssTimeAsMilliseconds` (Z. 84-93) haben denselben Aufbau: Null-/Whitespace-Pruefung, Regex-Match, sonst identische Warn-Log-Zeile und Fallback. Nur Regex und Rueckgabetyp unterscheiden sich.

  Empfehlung: Eine private Hilfsmethode `TryValidate(string? value, Regex pattern, string fieldName, out string validValue)` (oder `ValidateAgainstPattern(string? value, Regex pattern, string fallback, string fieldName)`) einfuehren und die drei Methoden darauf aufbauen, sodass die Log-Zeile nur einmal existiert.

- **Fehlende Validierung von Vorbedingungen** — `ToMilliseconds` (Z. 119-125) fuehrt `CssTimePattern.Match(cssTime)` aus und greift danach ohne `match.Success`-Pruefung auf `match.Groups[1].Value` zu. Bei einem nicht passenden Eingabewert wirft `double.Parse` eine `FormatException` ohne Kontext zum betroffenen Feld. Die Methode ist damit nur zufaellig sicher, weil alle heutigen Aufrufer vorher validieren.

  Empfehlung: In `ToMilliseconds` `if (!match.Success)` pruefen und eine `ArgumentException` mit dem Parameternamen und dem Wert werfen — oder die Methode zu `TryToMilliseconds(string cssTime, out int milliseconds)` umbauen und in `ValidateCssTimeAsMilliseconds` direkt verwenden, sodass der Regex nur einmal statt zweimal ausgefuehrt wird.

- **Fehlende Kapselung (veraenderlicher gemeinsamer Zustand)** — `ValidateColors` (Z. 113) gibt `Defaults.Colors` zurueck. Das ist das `string[]` der statischen `Defaults`-Instanz. Ueber einen Cast von `IReadOnlyList<string>` zurueck auf `string[]` kann ein Aufrufer die prozessweiten Default-Farben veraendern.

  Empfehlung: Statt `Defaults.Colors` eine eigene `private static readonly IReadOnlyList<string> DefaultColors = new LoadingBarOptions().Colors.ToArray();` als `ReadOnlyCollection<string>` bereitstellen oder eine defensive Kopie (`Defaults.Colors.ToArray()`) zurueckgeben.

### LoadingBarViewModel.cs (LoadingBarViewModel)

- **Namenskonventionen / Einheitlichkeit** — Der Typ enthaelt normalisierte Konfiguration und wird von `ILoadingBarService.GetSettings()` geliefert; er haelt keinerlei Ansichtszustand. Die uebrigen Typen in `Rezepte.Web/ViewModels` (z. B. `SettingsViewModel`) sind komponentenbezogene Zustandsobjekte mit Verhalten. Der Name `...ViewModel` und die Ablage im Ordner `ViewModels` weichen damit vom bestehenden Muster ab; zusaetzlich lautet die Methode `GetSettings()`, liefert aber ein "ViewModel".

  Empfehlung: Typ in `LoadingBarSettings` umbenennen und nach `Rezepte.Web/Configuration` (neben `LoadingBarOptions`) verschieben. Die Referenzen in `ILoadingBarService`, `LoadingBarService`, `LoadingBar.razor` und den Tests entsprechend anpassen.

- **Fehlerhafte XML-Dokumentation** — Z. 12 verwendet `<returns>` auf einer Typdeklaration. `<returns>` ist nur fuer Methoden/Properties gueltig und wird hier ignoriert.

  Empfehlung: Die `<returns>`-Zeile entfernen; der Inhalt steht bereits sinngemaess in der `<summary>`.

### LoadingBar.razor (LoadingBar)

- **Einheitlichkeit / Abweichung vom Codebasis-Stil** — Die Komponente injiziert per `[Inject] public ILoadingBarService LoadingBarService { get; set; } = default!;` (Z. 19-20). In `Rezepte.Web/Components` verwenden 31 von 32 Razor-Komponenten die `@inject`-Direktive; diese Datei ist die einzige Ausnahme. Zusaetzlich ist die Property `public`, obwohl sie nur komponentenintern genutzt wird.

  Empfehlung: `[Inject]`-Property durch `@inject ILoadingBarService LoadingBarService` am Dateikopf ersetzen.

### LoadingBar.razor.css

- **Hardcodierte Werte** — Der Fallback `var(--loading-bar-color, #45B7D1)` (Z. 20) dupliziert einen Farbwert aus der Default-Palette in `LoadingBarOptions.Colors`. Ebenso sind die Deckkraft-Transition `200ms` (Z. 6) und die Indikatorbreite `30%` (Z. 17/19, zusaetzlich in den Keyframes Z. 33 als `-30%` wiederholt) fest verdrahtet, obwohl Hoehe und Animationsdauer konfigurierbar sind.

  Empfehlung: Die Indikatorbreite als CSS-Custom-Property (`--loading-bar-indicator-width`) definieren und in Regel und Keyframes darueber referenzieren, damit der Wert nur an einer Stelle steht. Fuer die Fallback-Farbe einen neutralen, nicht aus der Palette stammenden Wert verwenden oder die Farbe wie Hoehe/Dauer aus den Options in die Inline-Custom-Property schreiben.

### loadingBar.js

- **Doppelter Code** — Der Deaktivierungsblock

  ```js
  const el = getHost();
  if (el) {
    el.classList.remove(ACTIVE_CLASS);
  }
  ```

  steht wortgleich im Safety-Timer (Z. 82-85) und im Hide-Timer (Z. 97-100).

  Empfehlung: Eine Funktion `deactivate()` einfuehren und in beiden Timer-Callbacks aufrufen.

- **Toter/inkonsistenter Guard** — Der Modul-Guard `const host = getHost(); if (!host) { return; }` (Z. 9-12) beendet die Initialisierung dauerhaft, wenn das Host-Element beim ersten Laden fehlt. Danach werden keine Listener mehr registriert und die Funktion bleibt fuer die gesamte Sitzung tot, obwohl jede weitere Funktion das Element ohnehin ueber `getHost()` neu aufloest. Die lokale Variable `host` wird ausserhalb des Guards nie verwendet.

  Empfehlung: Den Guard entfernen und die Listener-Registrierung unabhaengig vom initialen Vorhandensein durchfuehren, oder — falls der Guard als Deaktivierungsschalter gedacht ist — dies im Kommentar explizit machen und die redundanten `getHost()`-Aufrufe in den uebrigen Funktionen durch die Variable `host` ersetzen. Beide Wege sind konsistent, der aktuelle Mischzustand nicht.

- **Still geschluckte Fehlerbedingung** — Fehlt `window.Blazor` bzw. `window.Blazor.addEventListener` (Z. 195-197), wird das Abschlusssignal ohne jede Rueckmeldung nie registriert. Die Leiste wird dann ausschliesslich vom Safety-Timeout (Default 15s) beendet; die Ursache ist im Betrieb nicht diagnostizierbar.

  Empfehlung: Im `else`-Zweig ein `console.warn('loadingBar: Blazor enhanced navigation events unavailable; falling back to the safety timeout.')` ausgeben.

- **Inkonsistente Vorpruefung** — `handleLinkClick` bricht bei gleicher Zieladresse ab (`isFragmentOrCurrentAddress`, Z. 159-161), `handleFormSubmit` (Z. 166-189) besitzt keine vergleichbare Pruefung und startet die Animation fuer jede Same-Origin-Action. Dieselbe fachliche Regel ("keine Animation, wenn nichts navigiert wird") wird in den zwei Handlern unterschiedlich behandelt, ohne dass der Grund im Code steht.

  Empfehlung: Entweder in `handleFormSubmit` fuer GET-Formulare dieselbe Adresspruefung anwenden oder einen Kommentar ergaenzen, warum die Pruefung fuer Formulare bewusst entfaellt.

### LoadingBarMarkupTests_Rendering.cs (LoadingBarMarkupTests_Rendering)

- **Tests pruefen Implementierungsdetails statt fachliches Verhalten** — Alle fuenf Tests lesen ueber `ReadLoadingBarMarkup()` den **Quelltext** der `.razor`-Datei und pruefen Teilzeichenketten, u. a. `markup.Should().Contain("data-colors=\"@string.Join(\",\", Settings.Colors)\"")` (Z. 45) und `markup.Should().Contain("@if (Settings.Enabled)")` (Z. 24). Es wird nichts gerendert. Jede rein formale Umstellung (Leerzeichen, Umbenennung der lokalen Property, Extraktion in eine Methode) bricht die Tests, obwohl sich das Verhalten nicht aendert. `Render_WhenDisabled_RendersNoMarkup` (Z. 20-29) prueft ausschliesslich die Reihenfolge zweier Zeichenketten-Indizes und beweist nicht, dass bei `Enabled = false` kein Markup entsteht.

  Empfehlung: Die Tests auf echtes Komponenten-Rendering umstellen (bUnit-Paket `bunit` zu `Rezepte.Tests.csproj` ergaenzen), `ILoadingBarService` mit dem bereits vorhandenen Moq stubben und gegen das gerenderte DOM assertieren: bei `Enabled = false` `RenderedComponent.Markup` leer, bei `Enabled = true` Attribute `id`, `data-colors`, `data-hide-delay`, `data-max-visible-duration` und die Style-Custom-Properties am gerenderten Element pruefen.

- **Namenskonventionen** — Klassenname `LoadingBarMarkupTests_Rendering` verwendet einen Unterstrich-Suffix. Die bestehende Codebasis benennt thematische Aufteilungen ohne Unterstrich (`RecipeServiceStepValidationTests`, `ExportServiceRestoreTests`, `RecipeServiceTests`). Dasselbe gilt fuer `LoadingBarServiceTests_Defaults`, `LoadingBarServiceTests_DurationParsing` und `LoadingBarServiceTests_Validation`.

  Empfehlung: Klassen und Dateien umbenennen zu `LoadingBarRenderingTests`, `LoadingBarServiceDefaultsTests`, `LoadingBarServiceDurationParsingTests`, `LoadingBarServiceValidationTests`.

- **Doppelter Code** — `FindRepositoryRoot` (Z. 66-81) ist eine wortgleiche Kopie derselben Methode in `LoadingBarWiringTests.cs`, `RezepteAppFixture.cs` und bereits bestehend in `DeploymentDocumentationTests.cs`, `CsprojCredentialCopyTests.cs`, `ContractExportScriptTests.cs` und `PluginManagerTests.cs`.

  Empfehlung: Eine `internal static class RepositoryPaths` in `Rezepte.Tests/TestHelpers` mit `FindRepositoryRoot()` und `ReadRepositoryFile(params string[] parts)` anlegen und die neuen Testklassen darauf umstellen. (Die Umstellung der bestehenden Kopien kann separat erfolgen; die neuen Dateien duerfen die Duplikation nicht weiter vermehren.)

### LoadingBarWiringTests.cs (LoadingBarWiringTests)

- **Doppelter Code (bereits vorhandene Logik)** — `ReadRepositoryFile` (Z. 56-68) und `FindRepositoryRoot` (Z. 70-86) sind zeichengleiche Kopien der privaten Helfer in `DeploymentDocumentationTests` (Z. 96-108 bzw. folgend) — beide Klassen liegen sogar im selben Namespace `Rezepte.Tests.Deployment`.

  Empfehlung: Wie oben auf den gemeinsamen Helfer in `Rezepte.Tests/TestHelpers` umstellen.

- **Test prueft Implementierungsdetails statt Verhalten** — `Configuration_ShouldProvideLoadingBarSectionAndServiceRegistration` (Z. 36-45) prueft `ServiceCollectionExtensions.cs` per exaktem Zeilenvergleich (`"services.AddSingleton<ILoadingBarService, LoadingBarService>()"`). Eine aequivalente, aber anders formatierte Registrierung (z. B. `AddSingleton<ILoadingBarService, LoadingBarService>();` mit anderem Umbruch) laesst den Test fehlschlagen, waehrend eine kaputte Registrierung mit passendem Text durchginge.

  Empfehlung: Stattdessen eine `ServiceCollection` mit einer `ConfigurationBuilder`-Instanz aufbauen, die Registrierungsmethode aufrufen, den Provider bauen und pruefen, dass `GetRequiredService<ILoadingBarService>()` eine `LoadingBarService`-Instanz liefert und `IOptions<LoadingBarOptions>` die Werte aus der `LoadingBar`-Sektion enthaelt.

- **Testmethode prueft mehr als einen fachlichen Fall** — Dieselbe Methode `Configuration_ShouldProvideLoadingBarSectionAndServiceRegistration` prueft zwei unabhaengige Artefakte (`appsettings.json`-Sektion und DI-Registrierung in `ServiceCollectionExtensions.cs`).

  Empfehlung: In zwei Testmethoden aufteilen.

### LoadingBarServiceTests_Defaults.cs / LoadingBarServiceTests_DurationParsing.cs / LoadingBarServiceTests_Validation.cs

- **Doppelter Code** — Die private Factory

  ```csharp
  private static LoadingBarService CreateService(LoadingBarOptions options)
      => new LoadingBarService(Options.Create(options), NullLogger<LoadingBarService>.Instance);
  ```

  ist in allen drei Klassen identisch vorhanden (`_Defaults` Z. 71-74, `_DurationParsing` Z. 42-45, `_Validation` Z. 72-75).

  Empfehlung: In eine gemeinsame `internal static class LoadingBarServiceTestFactory` (bzw. `Rezepte.Tests/TestHelpers`) auslagern und aus allen drei Klassen aufrufen.

- **Doppelte Konstanten** — Die Default-Farbpalette `{ "#FF6B6B", "#4ECDC4", "#45B7D1", "#96CEB4", "#FFEAA7", "#DDA0DD" }` ist woertlich dupliziert in `LoadingBarServiceTests_Defaults` (Z. 24), `LoadingBarServiceTests_Validation` (Z. 69), `LoadingBarColorBrowserTests` (Z. 12-13), `LoadingBarOptions` (Z. 36) und `appsettings.json`. Eine Aenderung der Palette erfordert fuenf synchrone Anpassungen.

  Empfehlung: In den Tests `new LoadingBarOptions().Colors` als Erwartungswert verwenden statt das Literal zu wiederholen.

- **Unzureichende Testabdeckung** — Die dokumentierte Zusage der Klasse ("Invalid values ... are replaced with documented defaults **and logged as warnings**") wird von keinem Test geprueft; alle Tests verwenden `NullLogger`. Ausserdem fehlen Faelle fuer `Colors = null`, `Colors = Array.Empty<string>()`, `Height = null` und `MaxVisibleDuration` gleich `HideDelay` (Grenzfall der `<=`-Bedingung in `LoadingBarService` Z. 40).

  Empfehlung: Einen `FakeLogger`/`Mock<ILogger<LoadingBarService>>` verwenden und fuer mindestens einen Ungueltigkeitsfall verifizieren, dass ein Warning geloggt wurde; die genannten Grenz- und Null-Faelle als eigene Testmethoden ergaenzen.

### appsettings.json

- **Doppelter Code / doppelte Defaults** — Die Sektion `LoadingBar` (Z. 29-37) wiederholt saemtliche Werte, die in `LoadingBarOptions` bereits als C#-Defaults stehen (`Enabled`, `Height`, `AnimationDuration`, `HideDelay`, `MaxVisibleDuration`, `Colors`). Die beiden Quellen koennen unbemerkt auseinanderlaufen, ohne dass ein Test dies erkennt.

  Empfehlung: Entweder die Sektion in `appsettings.json` auf die Werte reduzieren, die vom C#-Default bewusst abweichen sollen, oder — falls die Sektion als Dokumentation der Schalter dienen soll — einen Test ergaenzen, der die JSON-Sektion gegen `new LoadingBarOptions()` vergleicht.

### RezepteAppFixture.cs (RezepteAppFixture)

- **Hardcodierte Werte** — `FindApplicationDll` (Z. 163) baut den Pfad fest aus `"bin", "Release", "net10.0", "publish"`. Ein Debug-Testlauf oder ein TFM-Wechsel bricht die gesamte Browser-Test-Suite mit einer `FileNotFoundException`, obwohl das Testprojekt selbst nichts an `net10.0` bindet.

  Empfehlung: Konfiguration und TFM aus dem Testassembly ableiten (z. B. aus `AppContext.BaseDirectory` die Segmente `bin/<Config>/<Tfm>` uebernehmen) oder den Pfad ueber eine Umgebungsvariable (`REZEPTE_PUBLISH_DIR`) mit dem heutigen Wert als Fallback ueberschreibbar machen.

- **Hardcodierte Werte** — Weitere Magic Numbers/Strings ohne Benennung: Startup-Deadline `60` Sekunden (Z. 102), Poll-Intervall `250` ms (Z. 121), Kill-Wartezeit `5000` ms (Z. 70), Registrierungsendpunkt `"api/auth/register"` (Z. 132), Test-E-Mail `"browsertest@example.invalid"` (Z. 136).

  Empfehlung: Als `private const` mit sprechendem Namen am Klassenkopf definieren (z. B. `StartupTimeoutSeconds`, `ReadinessPollIntervalMilliseconds`, `ShutdownGraceMilliseconds`, `RegisterEndpoint`, `TestEmail`), analog zu den vorhandenen `TestUsername`/`TestPassword`.

- **Inkonsistente Fehlerbehandlung** — Fehlende Playwright-Browser fuehren zum Skip (`PlaywrightBrowserFixture.BrowsersAvailable`), ein fehlendes Publish-Verzeichnis dagegen zur `FileNotFoundException` aus `InitializeAsync` (Z. 167-170). Damit schlaegt `dotnet test Rezepte.sln` — der in `.github/workflows/pr.yml` und lokal ueblich genutzte Befehl — fuer jeden Entwickler fehl, der `dotnet publish Rezepte.Web` nicht vorher ausgefuehrt hat. Zwei gleichartige Umgebungsvoraussetzungen werden also unterschiedlich behandelt.

  Empfehlung: Analog zu `BrowsersAvailable` eine Eigenschaft `ApplicationAvailable` einfuehren, bei fehlendem Publish-Output kein Werfen, sondern `ApplicationAvailable = false` setzen, und in den Testklassen `Skip.IfNot(appFixture.ApplicationAvailable, "...")` ergaenzen.

- **God-Methode (mehrere konzeptuell getrennte Aufgaben)** — `InitializeAsync` (Z. 22-62) erledigt hintereinander: Temp-Verzeichnis anlegen, freien Port ermitteln, `ProcessStartInfo` inkl. Umgebungsvariablen aufbauen, Prozess starten, Ausgabepipes anzapfen, Bereitschaft abwarten und einen Testbenutzer anlegen.

  Empfehlung: In `CreateTemporaryDatabaseAsync()`, `StartApplicationProcess()` und die bereits vorhandenen `WaitUntilReadyAsync()`/`RegisterTestUserAsync()` aufteilen; `InitializeAsync` bleibt als Orchestrierung von vier Aufrufen.

### PlaywrightBrowserFixture.cs (PlaywrightBrowserFixture)

- **Still geschluckte Exception** — `catch (PlaywrightException) { BrowsersAvailable = false; }` (Z. 26-29) verwirft die Ausnahme vollstaendig. Ein echter Startfehler (z. B. fehlende Systembibliotheken, Sandbox-Problem, Zeitueberschreitung) ist danach nicht von "Browser nicht installiert" unterscheidbar, und die Skip-Meldung `"Playwright Chromium browser is not installed."` in allen Testklassen ist dann irrefuehrend.

  Empfehlung: Die Ausnahme in einer Eigenschaft `public string? UnavailableReason { get; private set; }` festhalten (`ex.Message`) und die Testklassen `Skip.IfNot(browserFixture.BrowsersAvailable, browserFixture.UnavailableReason ?? "...")` verwenden lassen.

### LoadingBarPageObject.cs (LoadingBarPageObject)

- **Mehrere Verantwortlichkeiten** — Die Klasse buendelt laut eigenem Kommentar vier Aufgaben: Login, Zugriff auf das Host-Element, Zustands-/Farbabfrage sowie Netzwerk-Stubbing (`DelayNextNavigationAsync`, Z. 77-84). Das Verzoegern von HTTP-Antworten ist keine Page-Object-Aufgabe, sondern Testinfrastruktur.

  Empfehlung: `DelayNextNavigationAsync` in einen eigenen Helfer (z. B. `NetworkDelayHelper` mit einer Extension-Methode auf `IPage`) verschieben.

- **Nachbau vorhandener Framework-Funktionalitaet** — `WaitUntilUrlNoLongerContainsAsync` (Z. 91-105) implementiert eine eigene Polling-Schleife, obwohl Playwright `Page.WaitForURLAsync` mit Praedikat und Timeout anbietet — im selben Typ wird mit `WaitUntilLoadingBarHiddenAsync` (Z. 69-75) bereits der Playwright-Weg (`WaitForFunctionAsync`) genutzt. Zwei unterschiedliche Wartemuster in einer Klasse.

  Empfehlung: Auf `await Page.WaitForURLAsync(url => !url.Contains(fragment, StringComparison.OrdinalIgnoreCase), new PageWaitForURLOptions { Timeout = timeoutMilliseconds })` umstellen und die manuelle Schleife entfernen.

### LoadingBarVisibilityBrowserTests.cs / LoadingBarColorBrowserTests.cs

- **Doppelter Code** — `WaitUntilActiveAsync` ist zeichengleich in `LoadingBarVisibilityBrowserTests` (Z. 46-58) und `LoadingBarColorBrowserTests` (Z. 54-66) vorhanden.

  Empfehlung: Als `WaitUntilLoadingBarActiveAsync(int timeoutMilliseconds = 1000)` nach `LoadingBarPageObject` verschieben und dort — analog zu `WaitUntilLoadingBarHiddenAsync` — ueber `Page.WaitForFunctionAsync` implementieren.

- **Wartehelfer schluckt den Timeout** — `WaitUntilActiveAsync` kehrt nach Ablauf des Timeouts kommentarlos zurueck, statt zu scheitern. Der Fehlerfall aeussert sich dadurch erst in der nachfolgenden Assertion und ohne Zeitinformation.

  Empfehlung: Nach der Schleife eine `TimeoutException` mit Timeout-Wert und Selektor werfen (bzw. bei Umstellung auf `WaitForFunctionAsync` entfaellt das Problem).

- **Testmethode prueft mehrere fachliche Faelle / irrefuehrender Name** — `LinkClick_WithDelayedResponse_ShowsAnimatedBarBelowNavigation` (Z. 13-29) prueft drei unabhaengige Aussagen (aktive CSS-Klasse, Deckkraft > 0, gesetzte Farbe). Der Namensbestandteil `BelowNavigation` wird von keiner Assertion abgedeckt — die Position wird nirgends geprueft.

  Empfehlung: In `LinkClick_WithDelayedResponse_ActivatesLoadingBar` und `LinkClick_WithDelayedResponse_MakesLoadingBarVisible` aufteilen; die Farbpruefung ist bereits durch `LoadingBarColorBrowserTests` abgedeckt und kann hier entfallen. Den Namensbestandteil `BelowNavigation` streichen (die Positionierung deckt `LoadingBarWiringTests.Layout_ShouldPlaceLoadingBarDirectlyBelowNavigation` ab).

- **Test ohne gesicherte Vorbedingung (Fehlalarm-Risiko)** — `AfterNavigationCompleted_HidesLoadingBar` (Z. 31-44) klickt ohne Antwortverzoegerung und wartet dann darauf, dass die Leiste **nicht** aktiv ist. Wenn die Leiste nie aktiviert wurde — also genau im Fehlerfall, dass das Feature gar nicht anspricht — ist die Bedingung sofort erfuellt und der Test gruen.

  Empfehlung: Wie in den anderen Tests eine Route-Verzoegerung setzen, zuerst `IsLoadingBarActiveAsync()` als `true` assertieren, die Verzoegerung aufheben und erst danach `WaitUntilLoadingBarHiddenAsync()` pruefen.

### LoadingBarDisabledBrowserTests.cs / LoadingBarSafetyTimeoutBrowserTests.cs

- **Doppelter Code** — Beide Dateien deklarieren dieselbe verschachtelte Fixture-Boilerplate (`sealed class ...Fixture : RezepteAppFixture` mit ueberschriebenem `GetEnvironmentOverrides`, das lediglich ein einzelnes Dictionary-Paar zurueckgibt): `LoadingBarDisabledBrowserTests` Z. 25-34, `LoadingBarSafetyTimeoutBrowserTests` Z. 31-40.

  Empfehlung: `RezepteAppFixture` einen `protected virtual` Konstruktor-/Property-basierten Mechanismus geben oder eine kleine Basisklasse `ConfiguredRezepteAppFixture` einfuehren, die ein `IReadOnlyDictionary<string, string?>` entgegennimmt, sodass die Ableitungen auf eine Zeile schrumpfen.

- **Effizienz** — Beide Klassen binden `PlaywrightBrowserFixture` per `IClassFixture` statt ueber die vorhandene `BrowserTestCollection`. Dadurch wird Chromium pro Testlauf dreimal statt einmal gestartet.

  Empfehlung: `PlaywrightBrowserFixture` in eine eigene Collection-Definition auslagern, die alle Browser-Testklassen teilen, und pro Klasse nur noch die abweichende App-Fixture als `IClassFixture` binden.

### .github/workflows/pr.yml

- **Fehlende Validierung / Fehlermeldung ohne Kontext** — Der Schritt "Install Playwright browsers" setzt `PLAYWRIGHT_SCRIPT=$(find ... -name "playwright.ps1" | head -n 1)` (Z. 39-40). Findet `find` nichts, ist die Variable leer und `pwsh "" install --with-deps chromium` scheitert mit einer nichtssagenden pwsh-Fehlermeldung, statt die eigentliche Ursache (Build-Output fehlt) zu nennen.

  Empfehlung: Nach dem `find` pruefen und mit klarer Meldung abbrechen, z. B. `if [ -z "$PLAYWRIGHT_SCRIPT" ]; then echo "playwright.ps1 not found under Rezepte.Tests.Browser/bin/Release — did the browser test build succeed?" >&2; exit 1; fi`.

## Geprüfte Dateien

- `.github/workflows/pr.yml`
- `Rezepte.sln`
- `Rezepte.Web/appsettings.json`
- `Rezepte.Web/Components/App.razor`
- `Rezepte.Web/Components/Layout/LoadingBar.razor`
- `Rezepte.Web/Components/Layout/LoadingBar.razor.css`
- `Rezepte.Web/Components/Layout/MainLayout.razor`
- `Rezepte.Web/Configuration/LoadingBarOptions.cs`
- `Rezepte.Web/Extensions/ServiceCollectionExtensions.cs`
- `Rezepte.Web/Services/ILoadingBarService.cs`
- `Rezepte.Web/Services/LoadingBarService.cs`
- `Rezepte.Web/ViewModels/LoadingBarViewModel.cs`
- `Rezepte.Web/wwwroot/js/loadingBar.js`
- `Rezepte.Tests/Components/LoadingBarMarkupTests_Rendering.cs`
- `Rezepte.Tests/Deployment/LoadingBarWiringTests.cs`
- `Rezepte.Tests/Services/LoadingBarServiceTests_Defaults.cs`
- `Rezepte.Tests/Services/LoadingBarServiceTests_DurationParsing.cs`
- `Rezepte.Tests/Services/LoadingBarServiceTests_Validation.cs`
- `Rezepte.Tests.Browser/Rezepte.Tests.Browser.csproj`
- `Rezepte.Tests.Browser/Infrastructure/BrowserTestCollection.cs`
- `Rezepte.Tests.Browser/Infrastructure/LoadingBarPageObject.cs`
- `Rezepte.Tests.Browser/Infrastructure/PlaywrightBrowserFixture.cs`
- `Rezepte.Tests.Browser/Infrastructure/RezepteAppFixture.cs`
- `Rezepte.Tests.Browser/LoadingBarColorBrowserTests.cs`
- `Rezepte.Tests.Browser/LoadingBarDisabledBrowserTests.cs`
- `Rezepte.Tests.Browser/LoadingBarFormNavigationBrowserTests.cs`
- `Rezepte.Tests.Browser/LoadingBarSafetyTimeoutBrowserTests.cs`
- `Rezepte.Tests.Browser/LoadingBarVisibilityBrowserTests.cs`
