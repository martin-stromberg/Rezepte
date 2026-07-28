# Offene Aufgaben

Erstellt am: 2026-07-28
Abbruchgrund: Maximale Iterationsanzahl erreicht (Fortsetzungslauf, 3 Iterationen)

Die folgenden Aufgaben konnten im automatisierten Zyklus nicht abgeschlossen werden
und müssen manuell oder in einem erneuten Lauf bearbeitet werden.

## Offene Planelemente

Keine — der Plan-Review (`review.md`) meldet weiterhin „Vollständig umgesetzt", keine offenen Planelemente.

## Code-Review-Befunde

- [ ] `LoadingBarService.cs`: Die Invariante `MaxVisibleDuration > HideDelay` wird nach einem Fallback nicht erneut hergestellt. `BuildSettings` ersetzt einen verletzenden `MaxVisibleDuration`-Wert durch `DefaultMaxVisibleDurationMilliseconds` (15.000 ms), prüft danach aber nicht erneut gegen `hideDelayMilliseconds`. Bei `HideDelay = "30s"` und `MaxVisibleDuration = "20s"` (beide einzeln gültig) entsteht `MaxVisibleDurationMilliseconds = 15000` bei `HideDelayMilliseconds = 30000` — die dokumentierte Invariante bleibt verletzt, ohne Warnung. Empfehlung: Nach dem Fallback erneut prüfen und im Konfliktfall auch `hideDelayMilliseconds` auf den Standardwert zurücksetzen (mit eigener Warnung); Regressionstest `GetSettings_WithHideDelayAboveDefaultMaxVisibleDuration_KeepsInvariant` ergänzen.
- [ ] `LoadingBarService.cs`: Uneinheitliche Validierungstiefe — `HideDelay`/`MaxVisibleDuration` werden über `ValidateCssTimeAsMilliseconds` zusätzlich gegen Min-/Max-Grenzen geprüft, `Height`/`AnimationDuration` durchlaufen nur eine Formatprüfung (`ValidateAgainstPattern`). Dadurch sind `"Height": "0px"` und `"AnimationDuration": "0s"` gültige Konfigurationen, die die Ladeanimation faktisch unsichtbar bzw. bewegungslos machen. Empfehlung: Für `AnimationDuration` `ValidateCssTimeAsMilliseconds` mit sinnvollem Bereich verwenden (validierten Wert wieder als CSS-Zeit ausgeben), für `Height` einen numerischen Mindestwert (> 0) prüfen; Grenzen dokumentieren.
- [ ] `RezepteAppFixture.cs`: `DisposeAsync` ist nicht mehrfach aufrufbar — `_process` wird per `_process?.Dispose()` freigegeben, aber nicht auf `null` gesetzt. Ruft xUnit `DisposeAsync` nach einem bereits im `catch`-Block von `InitializeAsync` erfolgten Aufruf erneut auf, wirft `_process.HasExited` eine `InvalidOperationException`, die den eigentlichen Fehler (z. B. Timeout aus `WaitUntilReadyAsync`) überlagert. Empfehlung: Im `finally`-Block `_process = null;` und `_tempDirectory = null;` setzen, damit `DisposeAsync` idempotent ist.
- [ ] `RezepteAppFixture.cs`: `ApplicationUnavailableSkipReason` nennt fest `dotnet publish Rezepte.Web -c Release`, obwohl `ResolveApplicationDllPath` Konfiguration und TFM aus dem Ausgabeverzeichnis der Testassembly ableitet. Bei einem Debug-Testlauf sucht die Fixture in `bin/Debug/net10.0/publish`, die Meldung verweist aber auf Release und behebt die Ursache nicht. Empfehlung: Skip-Meldung zur Laufzeit aus der ermittelten Konfiguration und dem erwarteten Pfad zusammensetzen.
- [ ] `loadingBar.js`: Die Ermittlung des Navigationsziels ist in `handleLinkClick` und `handleFormSubmit` dupliziert (Target-Prüfung `element.target !== '_self'` sowie `resolveUrl`/`isSameOriginNavigation`-Block) — dieselbe fachliche Regel wird an zwei Stellen gepflegt. Empfehlung: Gemeinsame Funktion `resolveSameOriginTarget(element, rawUrl)` einführen und beide Handler darauf reduzieren.
- [ ] `LoadingBarPageObject.cs` / `NetworkDelayHelper.cs`: `DelayRouteAsync` delegiert ohne eigene Logik an `NetworkDelayHelper.DelayNavigationAsync`, die Extension-Klasse hat nur diesen einen Aufrufer, während die Schwestermethode `BlockRouteAsync` ihre Logik inline im Page-Object implementiert — zwei unterschiedliche Ablageorte für dieselbe Aufgabe. Empfehlung: Logik vereinheitlichen (z. B. `DelayNavigationAsync`-Rumpf nach `LoadingBarPageObject` ziehen, `NetworkDelayHelper.cs` entfernen).
- [ ] `LoadingBarFormNavigationBrowserTests.cs`: Umgeht das Page-Object und arbeitet direkt mit anwendungsspezifischen Selektoren (`#nav-search`, `button[aria-label='Suche starten']`, `button[aria-label='Bearbeiten']`, `form.shopping-add-row` u. a.), während alle übrigen Tests Selektoren im Page-Object kapseln. Empfehlung: Abläufe als Methoden am Page-Object kapseln (z. B. `SubmitNavigationSearchAsync`, `SubmitInteractiveShoppingListItemAsync`), Selektoren als `private const` führen.
- [ ] `LoadingBarBrowserSession.cs`: Ressourcenleck im Fehlerpfad — `StartLoggedInSessionAsync` erzeugt das `LoadingBarPageObject` und ruft danach `LoginAsync` auf; schlägt die Anmeldung fehl, wird das Page-Object nie zurückgegeben, das `await using` der aufrufenden Testmethode kommt nie zustande, der `IBrowserContext` bleibt offen. Empfehlung: `LoginAsync`-Aufruf mit `try { … } catch { await pageObject.DisposeAsync(); throw; }` absichern.

## Fehlgeschlagene Tests

Keine — `test-results.md` meldet „Keine Fehler" (275 gesamt, 266 bestanden, 9 übersprungen mangels Playwright-Browsern/Release-Publish in dieser Umgebung, 0 fehlgeschlagen).
