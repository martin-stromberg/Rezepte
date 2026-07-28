# Code-Review

## Ergebnis

**Status:** Befunde vorhanden

## Befunde

### loadingBar.js

- **Fehlerbehandlung / falsche Vorbedingungsprüfung** — `handleLinkClick` (Z. 129) und `handleFormSubmit` (Z. 168) prüfen `event.defaultPrevented`, beide Listener sind aber in der **Capture**-Phase am `document` registriert (Z. 192-193, dritter Parameter `true`). In der Capture-Phase ist `defaultPrevented` grundsätzlich noch `false`, weil kein nachgelagerter Handler den Event bis dahin behandeln konnte. Die Prüfung ist damit wirkungslos.

  Konkrete Auswirkung bei interaktiven Blazor-Formularen, die bewusst nicht navigieren: `Rezepte.Web/Components/Pages/ShoppingList.razor` Z. 134 (`<form @onsubmit="…" @onsubmit:preventDefault="true">`, `@rendermode InteractiveServer`), `Rezepte.Web/Components/Settings/UserProfile.razor` Z. 27 und Z. 64 sowie `Rezepte.Web/Components/Settings/PluginSettings.razor` Z. 16 (`EditForm` mit `OnValidSubmit`, ohne `Method`/`Action` — Blazor ruft `preventDefault` in seinem delegierten Handler auf). Da `handleFormSubmit` gleichadressige Ziele bewusst **nicht** überspringt (Kommentar Z. 163-166) und `form.action` ohne `action`-Attribut auf die aktuelle Adresse zeigt, startet `startAnimation()` (Z. 189) bei jedem dieser Submits. Es folgt nie ein `enhancedload`- oder `pageshow`-Ereignis, also bleibt die Ladeleiste bis zum Ablauf von `MaxVisibleDuration` sichtbar — mit Standardkonfiguration 15 Sekunden Dauerlauf nach jedem „Artikel hinzufügen", „Profil speichern" und „Passwort ändern".

  Empfehlung: Die Entscheidung erst nach Abschluss der Event-Auslieferung treffen, z. B. in beiden Handlern statt des direkten `startAnimation()`-Aufrufs `setTimeout(function () { if (!event.defaultPrevented) { startAnimation(); } }, 0);` verwenden (das Event-Objekt bleibt gültig und `defaultPrevented` spiegelt dann alle Handler wider). Die Prüfung zu Beginn der Handler kann als schneller Vorab-Ausstieg bestehen bleiben. Zusätzlich einen Browser-Test ergänzen, der belegt, dass ein interaktiver Formular-Submit ohne Navigation (z. B. `/shopping-list`, Artikel hinzufügen) die Leiste **nicht** aktiviert — diese Fehlerklasse ist derzeit von keinem Test abgedeckt.

### LoadingBarOptions.cs (LoadingBarOptions) / appsettings.json

- **Fehlerhafte Konfigurationsbindung — Farbpalette ist nicht überschreibbar** — `Colors` (Z. 36) ist mit einem vorbelegten Array initialisiert. Der .NET-Konfigurationsbinder **hängt** Array-Einträge an ein vorbelegtes Array an, statt es zu ersetzen. `services.Configure<LoadingBarOptions>(configuration.GetSection("LoadingBar"))` (`ServiceCollectionExtensions.cs` Z. 32) bindet daher auf eine Instanz, die bereits 6 Standardfarben enthält.

  Empirisch verifiziert (isoliertes Bindungs-Experiment mit identischer Klasse und identischem JSON): Mit dem ausgelieferten `appsettings.json` (Z. 35, dieselben 6 Farben) enthält `Colors` zur Laufzeit **12** Einträge — jede Farbe doppelt. Setzt ein Betreiber stattdessen zwei eigene Farben, erhält er **8** Einträge: die 6 Standardfarben bleiben erhalten und die eigenen werden nur angehängt. Die dokumentierte Konfigurationsmöglichkeit „Farben austauschen" funktioniert also nicht.

  Bemerkenswert: `LoadingBarWiringTests.Configuration_LoadingBarSection_MatchesDocumentedDefaults` (Z. 53-56) kennt dieses Verhalten und umgeht es per `Colors = Array.Empty<string>()`, statt es zu beheben. Dadurch prüft der Test genau nicht das Verhalten der produktiven Registrierung und kann den Fehler nicht aufdecken.

  Empfehlung: `Colors` in `LoadingBarOptions` auf `Array.Empty<string>()` initialisieren und die Standardpalette als eigenes `public static readonly string[] DefaultColors` in `LoadingBarOptions` bereitstellen. `LoadingBarService.DefaultColors` (Z. 17) auf dieses Feld umstellen; die vorhandene Leer-Fallback-Logik in `ValidateColors` (Z. 100-104) greift dann sowohl für „nicht konfiguriert" als auch für „nur ungültige Werte". Anschließend den Wiring-Test auf `new LoadingBarOptions()` ohne Vorbelegungs-Trick umstellen, damit er die produktive Bindung tatsächlich absichert.

### RezepteAppFixture.cs (RezepteAppFixture)

- **Fehlerbehandlung — Ressourcenleck bei fehlgeschlagenem Start** — `InitializeAsync` (Z. 34-49) startet in `StartApplicationProcess` (Z. 44) den Anwendungsprozess und legt in `CreateTemporaryDatabase` (Z. 43) ein Temp-Verzeichnis an. Wirft danach `WaitUntilReadyAsync` (Z. 46, `TimeoutException`/`InvalidOperationException`) oder `RegisterTestUserAsync` (Z. 47, `EnsureSuccessStatusCode`), ruft xUnit `DisposeAsync` für eine Fixture, deren `InitializeAsync` fehlgeschlagen ist, nicht auf. Der `dotnet Rezepte.Web.dll`-Kindprozess läuft dann für die restliche Testsitzung weiter, hält den Port und das Temp-Verzeichnis bleibt liegen — auf CI ein hängender Job statt eines sauberen Fehlers.

  Empfehlung: Den Rumpf ab `CreateTemporaryDatabase` in `try { … } catch { await DisposeAsync(); throw; }` klammern, damit Prozess und Temp-Verzeichnis auch im Fehlerfall abgeräumt werden.

### RepositoryPaths.cs (RepositoryPaths)

- **Doppelter Code — bereits vorhandene Logik nicht abgelöst** — `FindRepositoryRoot` (Z. 5-20) wurde in diesem Branch als gemeinsamer Helfer neu angelegt, aber die bereits existierenden, zeichengleichen privaten Kopien wurden nicht entfernt: `Rezepte.Tests/Deployment/CsprojCredentialCopyTests.cs` Z. 33-49 und `Rezepte.Tests/Deployment/DeploymentDocumentationTests.cs` Z. 110-126 enthalten dieselbe Schleife samt identischer Fehlermeldung. Statt einer Vereinheitlichung liegt die Logik jetzt dreifach im selben Testprojekt.

  Empfehlung: Beide privaten `FindRepositoryRoot`-Methoden löschen und die Aufrufstellen in `CsprojCredentialCopyTests` und `DeploymentDocumentationTests` auf `RepositoryPaths.FindRepositoryRoot()` umstellen.

### LoadingBarVisibilityBrowserTests.cs / LoadingBarColorBrowserTests.cs

- **Doppelter Code und fehlende Kapselung** — Der Dreisatz „Navigation verzögern, Cookbooks-Link mit `NoWaitAfter` klicken, auf aktive Leiste warten" steht fünfmal nahezu identisch im Branch: `LoadingBarVisibilityBrowserTests` Z. 16-19, Z. 29-32, Z. 42-45 sowie `LoadingBarColorBrowserTests` Z. 19-21, Z. 34-41. Der URL-Glob `"**/cookbooks"`, der Selektor `"a[href='/cookbooks']"`, die `PageClickOptions { NoWaitAfter = true }` und die Verzögerungswerte sind dabei jedes Mal als Literale wiederholt. Ändert sich die Route, sind fünf Stellen in zwei Dateien anzupassen.

  Empfehlung: In `LoadingBarPageObject` eine Methode wie `ClickNavigationLinkAsync(string href)` sowie `DelayRouteAsync(string urlGlobPattern, int delayMilliseconds)` ergänzen und die Cookbooks-Route als Konstante (z. B. `private const string CookbooksRoute = "/cookbooks";`) im Page Object führen. Die Tests reduzieren sich damit auf eine fachliche Zeile pro Navigation.

### LoadingBarService.cs (LoadingBarService)

- **Doppelter Code / überflüssige Objekterzeugung** — Z. 16 legt `Defaults = new()` an, Z. 17 erzeugt für `DefaultColors` eine **zweite** `LoadingBarOptions`-Instanz (`new LoadingBarOptions().Colors`), obwohl `Defaults.Colors` dieselbe Standardpalette liefert. Zusätzlich wird `ToDefaultMilliseconds(Defaults.HideDelay)` bzw. `…(Defaults.MaxVisibleDuration)` in Z. 38, Z. 39 und Z. 49 bei jedem Aufbau erneut per Regex geparst, obwohl es sich um Kompilierzeit-Konstanten handelt. Der `ArgumentException`-Pfad in `ToDefaultMilliseconds` (Z. 128) — ein reiner Programmierfehler-Schutz für einen fehlerhaften Standardwert — schlägt dadurch erst beim ersten `GetSettings()`-Aufruf zu, statt beim Typ-Laden.

  Empfehlung: `DefaultColors` aus `Defaults.Colors` ableiten und zwei zusätzliche statische Felder einführen: `private static readonly int DefaultHideDelayMilliseconds = ToDefaultMilliseconds(Defaults.HideDelay);` und `private static readonly int DefaultMaxVisibleDurationMilliseconds = ToDefaultMilliseconds(Defaults.MaxVisibleDuration);`, die in Z. 38, 39 und 49 verwendet werden.

- **Fehlende Validierung — keine Obergrenze für Zeitwerte** — `ValidateCssTimeAsMilliseconds` (Z. 74-83) akzeptiert jeden regex-konformen Wert. `TryToMilliseconds` (Z. 120) rechnet über `(int)(value * 1000)`; die Konvertierung sättigt bei Überlauf auf `int.MaxValue`. `LoadingBar:HideDelay = "999999999s"` wird damit still als 2.147.483.647 ms übernommen und als `data-hide-delay` in die Seite geschrieben — die Leiste bliebe nach jeder Navigation faktisch dauerhaft sichtbar. Für `MaxVisibleDuration` existiert nur die relative Prüfung gegen `HideDelay` (Z. 41), keine absolute Plausibilitätsgrenze.

  Empfehlung: In `ValidateCssTimeAsMilliseconds` einen Gültigkeitsbereich prüfen (z. B. `HideDelay` 0–60.000 ms, `MaxVisibleDuration` 100–300.000 ms) und bei Überschreitung — wie bei allen anderen ungültigen Werten — eine Warnung loggen und auf den Standardwert zurückfallen. Passende Testfälle in `LoadingBarServiceValidationTests` ergänzen.

- **Doppelter Code (geringfügig)** — `clearSafetyTimer` (loadingBar.js Z. 52-57) und `clearHideTimer` (Z. 59-64) sind bis auf die Variable identisch.

  Empfehlung: Durch eine Funktion `function clearTimer(id) { if (id) { clearTimeout(id); } return null; }` ersetzen und an den Aufrufstellen `safetyTimer = clearTimer(safetyTimer);` bzw. `hideTimer = clearTimer(hideTimer);` verwenden.

### LoadingBarWiringTests.cs (LoadingBarWiringTests)

- **Test prüft Implementierungsdetail und nicht das benannte Verhalten** — `Layout_ShouldPlaceLoadingBarDirectlyBelowNavigation` (Z. 19-30) liest den Razor-Quelltext als Zeichenkette und sucht das exakte Literal `"<LoadingBar />"` (Z. 24). Eine rein formale Änderung wie `<LoadingBar/>` oder ein Zeilenumbruch im Tag lässt den Test scheitern, obwohl sich das Verhalten nicht ändert. Zudem deckt sich der Testname nicht mit den Assertions: geprüft wird nur „irgendwo zwischen `</nav>` und `<main …>`" (Z. 27-29), nicht „direkt unterhalb". `App_ShouldLoadLoadingBarScriptAfterBlazorScript` (Z. 33-42) hat dieselbe Textabhängigkeit auf `"js/loadingBar.js"` und `"_framework/blazor.web.js"`.

  Empfehlung: Das Literal durch einen toleranten Regex (`<LoadingBar\s*/>`) ersetzen und den Test in `Layout_ShouldPlaceLoadingBarBetweenNavigationAndMainContent` umbenennen, damit Name und Assertion übereinstimmen. Alternativ beide Tests entfernen, da die tatsächliche Wirkung bereits durch die Browser-Tests (`LoadingBarVisibilityBrowserTests`) abgedeckt ist.

- **Toter Code** — `using System;` (Z. 1) ist überflüssig; `Rezepte.Tests.csproj` (Z. 6) aktiviert `ImplicitUsings`, und keine andere neue Datei im Branch führt dieses `using` auf.

  Empfehlung: Zeile 1 entfernen.

## Geprüfte Dateien

- `Rezepte.Web/Configuration/LoadingBarOptions.cs`
- `Rezepte.Web/Configuration/LoadingBarSettings.cs`
- `Rezepte.Web/Services/ILoadingBarService.cs`
- `Rezepte.Web/Services/LoadingBarService.cs`
- `Rezepte.Web/Components/Layout/LoadingBar.razor`
- `Rezepte.Web/Components/Layout/LoadingBar.razor.css`
- `Rezepte.Web/Components/Layout/MainLayout.razor`
- `Rezepte.Web/Components/App.razor`
- `Rezepte.Web/Extensions/ServiceCollectionExtensions.cs`
- `Rezepte.Web/wwwroot/js/loadingBar.js`
- `Rezepte.Web/appsettings.json`
- `Rezepte.Tests/Components/LoadingBarRenderingTests.cs`
- `Rezepte.Tests/Deployment/LoadingBarWiringTests.cs`
- `Rezepte.Tests/Services/LoadingBarServiceDefaultsTests.cs`
- `Rezepte.Tests/Services/LoadingBarServiceDurationParsingTests.cs`
- `Rezepte.Tests/Services/LoadingBarServiceValidationTests.cs`
- `Rezepte.Tests/TestHelpers/LoadingBarServiceTestFactory.cs`
- `Rezepte.Tests/TestHelpers/RepositoryPaths.cs`
- `Rezepte.Tests/Rezepte.Tests.csproj`
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
- `.github/workflows/pr.yml`
- `Rezepte.sln`
