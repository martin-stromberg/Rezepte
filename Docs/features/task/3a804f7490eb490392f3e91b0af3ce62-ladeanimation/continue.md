# Offene Aufgaben

Erstellt am: 2026-07-27
Abbruchgrund: Maximale Iterationsanzahl erreicht

Die folgenden Aufgaben konnten im automatisierten Zyklus nicht abgeschlossen werden
und müssen manuell oder in einem erneuten Lauf bearbeitet werden.

## Offene Planelemente

Keine — der Plan-Review (`review.md`) meldet „Vollständig umgesetzt", keine offenen Planelemente.

## Code-Review-Befunde

- [ ] `loadingBar.js`: `handleLinkClick`/`handleFormSubmit` prüfen `event.defaultPrevented`, sind aber in der Capture-Phase registriert, wo dieser Wert immer `false` ist. Dadurch startet die Ladeanimation auch bei interaktiven Formular-Submits ohne Navigation (`ShoppingList.razor`, `UserProfile.razor`, `PluginSettings.razor`) und bleibt bis zu `MaxVisibleDuration` (Standard 15s) sichtbar. Empfehlung: Entscheidung per `setTimeout(..., 0)` nach Abschluss der Event-Auslieferung treffen; Browser-Test ergänzen, der belegt, dass ein Formular-Submit ohne Navigation die Leiste nicht aktiviert.
- [ ] `LoadingBarOptions.Colors` ist mit einem vorbelegten Array initialisiert; der .NET-Konfigurationsbinder hängt konfigurierte Werte an statt sie zu ersetzen, wodurch die Farbpalette aus `appsettings.json` nicht wie dokumentiert überschreibbar ist (Standardkonfiguration liefert 12 statt 6 Farben zur Laufzeit). `LoadingBarWiringTests.Configuration_LoadingBarSection_MatchesDocumentedDefaults` umgeht das Problem per `Colors = Array.Empty<string>()`, statt es aufzudecken. Empfehlung: `Colors` auf `Array.Empty<string>()` initialisieren, Standardpalette als eigenes `DefaultColors`-Feld führen, Wiring-Test auf `new LoadingBarOptions()` ohne Vorbelegungs-Trick umstellen.
- [ ] `RezepteAppFixture.InitializeAsync`: Schlägt `WaitUntilReadyAsync` oder `RegisterTestUserAsync` fehl, ruft xUnit `DisposeAsync` nicht auf — Anwendungsprozess und Temp-Verzeichnis bleiben hängen. Empfehlung: Rumpf ab `CreateTemporaryDatabase` in `try { … } catch { await DisposeAsync(); throw; }` klammern.
- [ ] `RepositoryPaths.FindRepositoryRoot` wurde als gemeinsamer Helfer angelegt, aber die zeichengleichen privaten Kopien in `CsprojCredentialCopyTests` und `DeploymentDocumentationTests` wurden nicht abgelöst (Logik liegt dreifach vor). Empfehlung: Beide privaten Methoden löschen, Aufrufstellen auf `RepositoryPaths.FindRepositoryRoot()` umstellen.
- [ ] `LoadingBarVisibilityBrowserTests`/`LoadingBarColorBrowserTests`: Der Dreisatz „Navigation verzögern, Cookbooks-Link mit `NoWaitAfter` klicken, auf aktive Leiste warten" ist fünffach nahezu identisch dupliziert. Empfehlung: `ClickNavigationLinkAsync(string href)` und `DelayRouteAsync(string urlGlobPattern, int delayMilliseconds)` in `LoadingBarPageObject` ergänzen, Cookbooks-Route als Konstante führen.
- [ ] `LoadingBarService`: `DefaultColors` erzeugt unnötig eine zweite `LoadingBarOptions`-Instanz statt `Defaults.Colors` zu nutzen; `ToDefaultMilliseconds(Defaults.HideDelay/MaxVisibleDuration)` wird bei jedem Aufbau neu geparst statt einmalig als statisches Feld berechnet zu werden. Empfehlung: `DefaultColors` aus `Defaults.Colors` ableiten; `DefaultHideDelayMilliseconds`/`DefaultMaxVisibleDurationMilliseconds` als statische Felder einführen.
- [ ] `LoadingBarService.ValidateCssTimeAsMilliseconds` prüft keine Obergrenze für Zeitwerte; `TryToMilliseconds` sättigt bei Überlauf auf `int.MaxValue`, wodurch z. B. `HideDelay = "999999999s"` still übernommen wird und die Leiste faktisch dauerhaft sichtbar bliebe. Empfehlung: Gültigkeitsbereich einführen (z. B. HideDelay 0–60.000 ms, MaxVisibleDuration 100–300.000 ms), bei Überschreitung Warnung loggen und auf Standardwert zurückfallen; passende Tests in `LoadingBarServiceValidationTests` ergänzen.
- [ ] `loadingBar.js`: `clearSafetyTimer`/`clearHideTimer` sind bis auf die Variable identisch. Empfehlung: Durch gemeinsame Funktion `clearTimer(id)` ersetzen.
- [ ] `LoadingBarWiringTests.Layout_ShouldPlaceLoadingBarDirectlyBelowNavigation` prüft ein exaktes String-Literal (`"<LoadingBar />"`) statt tolerantem Muster, und der Testname deckt sich nicht mit der tatsächlichen Assertion („irgendwo zwischen nav und main" statt „direkt unterhalb"). Gleiches Muster bei `App_ShouldLoadLoadingBarScriptAfterBlazorScript`. Empfehlung: Literal durch Regex `<LoadingBar\s*/>` ersetzen, Test in `Layout_ShouldPlaceLoadingBarBetweenNavigationAndMainContent` umbenennen, oder Tests entfernen (Abdeckung bereits durch Browser-Tests gegeben).
- [ ] `LoadingBarWiringTests.cs`: Überflüssiges `using System;` in Zeile 1 entfernen (ImplicitUsings ist aktiviert).

## Fehlgeschlagene Tests

Keine — `test-results.md` meldet „Keine Fehler" (263 bestanden, 8 übersprungen mangels Playwright-Browsern/Release-Publish in dieser Umgebung, 0 fehlgeschlagen).
