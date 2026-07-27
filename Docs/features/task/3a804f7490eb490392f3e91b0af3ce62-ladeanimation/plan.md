# Umsetzungsplan: Ladeanimation

## Übersicht

Unterhalb der Navigationsleiste wird eine schmale, horizontale Ladeanimation eingeführt, die bei jeder vom Benutzer ausgelösten Navigation — Klick auf einen Link **und** Absenden eines Formulars (insbesondere der Suchleiste) — sofort erscheint, sich in einer zufällig gewählten Farbe von rechts nach links bewegt und nach Abschluss der Navigation wieder ausgeblendet wird. Betroffen sind die Layout-Komponenten (`MainLayout.razor`, neue `LoadingBar.razor`), eine neue Options-Klasse mit Service-Layer zur Aufbereitung der Konfiguration, ein neues clientseitiges Skript `wwwroot/js/loadingBar.js` sowie die Einbindung in `App.razor`, `appsettings.json` und `ServiceCollectionExtensions.cs`. Zusätzlich wird mit einem neuen Testprojekt `Rezepte.Tests.Browser` (Playwright) erstmals eine Browser-Testinfrastruktur aufgebaut, die das tatsächliche Laufzeitverhalten der Animation prüft.

## Designentscheidungen

Die Bestandsaufnahme hat einen für den Entwurf entscheidenden Umstand offengelegt: `App.razor`, `Routes.razor` und `MainLayout.razor` besitzen **keinen** Render-Mode — sie werden statisch server-seitig gerendert. `@rendermode InteractiveServer` ist ausschließlich auf einzelnen Seiten gesetzt. Da `blazor.web.js` geladen wird, ist die **Enhanced Navigation** aktiv, d. h. Linkklicks werden clientseitig abgefangen und die Antwort wird per Fetch in das bestehende DOM gepatcht. Daraus folgen die ersten Entscheidungen.

| Komponente / Bereich | Gewählter Ansatz | Begründung |
|----------------------|-----------------|------------|
| Auslösen der Animation | Clientseitiges Skript `wwwroot/js/loadingBar.js` mit `click`- **und** `submit`-Listener in der Capture-Phase auf `document`, **nicht** `NavigationManager.LocationChanged` | Die Anforderung verlangt Feedback „auf langsamen Servern", also *bevor* der Server antwortet. `LocationChanged` feuert erst nach Eintreffen der Antwort und in einem statisch gerenderten Layout überhaupt nicht. Nur JavaScript kann im Moment der Interaktion reagieren. Der `submit`-Listener stellt sicher, dass auch die Suchleiste (`OnSubmitSearch()`), das Login-, das Registrierungs- und das Logout-Formular dieselbe Rückmeldung erzeugen. |
| Abschluss-Erkennung | `Blazor.addEventListener('enhancedload', …)` als Primärsignal, `window` `pageshow` als Rückfallebene für vollständige Seitenladungen und bfcache | `enhancedload` ist das offizielle Signal, dass Blazor den neuen Seiteninhalt eingepatcht hat. Ein HTTP-Request-Counter wäre deutlich aufwendiger ohne Mehrwert für eine rein kosmetische Anzeige. |
| Schutz vor DOM-Patching | Das Host-Element trägt `data-permanent` | Die Enhanced Navigation patcht bei jeder Navigation auch die Navigationsleiste und alles darunter. Ohne `data-permanent` würde Blazor die vom Skript gesetzte Aktiv-Klasse und die Farbe im Moment des Einpatchens verwerfen — der Balken würde schlagartig statt nach `HideDelay` verschwinden und ein Neustart der Animation bei schnellen Folgeklicks wäre unzuverlässig. `data-permanent` ist der dafür vorgesehene Blazor-Mechanismus. |
| Render-Mode der `LoadingBar` | Kein Render-Mode (statisches SSR) | Die Komponente rendert nur statisches Markup; die Dynamik liegt vollständig im Skript. So funktioniert die Anzeige auf jeder Seite, auch ohne Circuit, und es entsteht keine zusätzliche SignalR-Last. |
| `ILoadingBarService` | Service Layer als reiner **Assembler**: `GetSettings()` liefert ein normalisiertes, unveränderliches `LoadingBarViewModel`. Keine `ShowAsync()`/`HideAsync()` | Sichtbarkeitszustand existiert nur im Browser; server-seitige `ShowAsync`/`HideAsync` hätten in einem statisch gerenderten Layout keinen Effekt und wären toter Code. Validierung und Normalisierung der Optionen sind dagegen echte, unit-testbare Logik. |
| Lebensdauer des Service | `AddSingleton` statt `AddScoped` | Der Service ist nach dem obigen Zuschnitt zustandslos und liest ausschließlich `IOptions<LoadingBarOptions>`. Das normalisierte Ergebnis wird einmalig berechnet und zwischengespeichert; eine Bindung pro Benutzer/Session hat keinen Nutzen mehr. |
| Zufällige Farbwahl | Auswahl im Browser (JavaScript) aus der vom Server gelieferten Farbliste; die zuletzt verwendete Farbe wird bei mehr als einer verfügbaren Farbe ausgeschlossen | Die Anforderung verlangt eine neue Farbe **pro Navigationsinteraktion**. Bei serverseitiger Wahl hätten alle Klicks auf derselben Seite dieselbe Farbe. Der Ausschluss der Vorgängerfarbe macht den Farbwechsel für den Benutzer sichtbar und ist die Voraussetzung dafür, dass der Browsertest den Wechsel deterministisch prüfen kann. |
| Übergabe der Konfiguration an den Client | CSS-Custom-Properties im `style`-Attribut (`--loading-bar-height`, `--loading-bar-duration`) und `data-*`-Attribute für Farbliste und Zeitwerte | Vermeidet JS-Interop und eine eigene Konfigurations-API. Das Skript liest die Werte direkt vom Host-Element; die Werte bleiben in einem einzigen `appsettings`-Abschnitt gepflegt. |
| Fehlerverhalten bei Fehlkonfiguration | Tolerante Normalisierung mit Warn-Log statt Exception | Die Ladeanimation ist rein kosmetisch. Ein Tippfehler in `appsettings.json` darf das Layout der gesamten Anwendung nicht unbenutzbar machen. |
| Sichtbarkeit ein-/ausblenden | Umschalten von `opacity` bei dauerhaft reserviertem Platz, nicht `display` | Verhindert einen Layout-Shift zwischen Navigationsleiste und Inhalt beim Ein- und Ausblenden. |
| Komponententests | Zweistufig: zuerst Machbarkeitsprüfung von `bunit` unter `net10.0`, danach entweder bUnit-Komponententests **oder** markup-basierte Dateiprüfungen | `Rezepte.Tests` besitzt bisher kein bUnit. Die Kompatibilität mit `net10.0` ist unbestätigt, darf die Umsetzung aber nicht blockieren. Beide Zweige sind unten in der Umsetzungsreihenfolge ausformuliert; die eigentliche Verhaltensabsicherung des Renderings liegt ohnehin bei den Browsertests. |
| Browser-Testinfrastruktur | Neues Testprojekt `Rezepte.Tests.Browser` mit `Microsoft.Playwright` (+ `Microsoft.Playwright.NUnit`-freier, reiner xUnit-Nutzung) und `Xunit.SkippableFact` | Playwright ist die einzige Browser-Automatisierung mit erstklassiger .NET-Integration (NuGet-Paket, kein Node-Toolchain-Zwang) und passt damit in das bestehende .NET-Testprojekt-Setup. Ein **eigenes** Projekt ist nötig, damit die schnellen Unit-Tests nicht von Browser-Binaries abhängen und die Browsertests in CI einen eigenen Installationsschritt bekommen können. |
| Hosting der Anwendung im Browsertest | Start der bereits gebauten Anwendung als **eigener Prozess** (`dotnet run --project Rezepte.Web --no-build`) auf einem freien Port mit temporärer SQLite-Datei, statt `WebApplicationFactory` | `WebApplicationFactory` verwendet den In-Memory-`TestServer`, der von einem echten Browser nicht über HTTP erreichbar ist. Ein realer Kestrel-Prozess liefert außerdem echte Static Files, echtes `blazor.web.js` und echte Enhanced Navigation — genau das, was geprüft werden soll. Zusätzlicher Vorteil: keine Änderung an `Program.cs` (kein `public partial class Program` nötig). |
| Simulation eines langsamen Servers im Browsertest | Playwright-Routen-Interception (`Page.RouteAsync`) mit künstlicher Verzögerung der Zielantwort | Die Anforderung zielt ausdrücklich auf das Verhalten bei langsamen Servern. Nur mit einer kontrolliert verzögerten Antwort lässt sich deterministisch prüfen, dass der Balken *während* der Navigation sichtbar ist und *danach* verschwindet. |
| Überspringen der Browsertests ohne Browser-Binaries | `Xunit.SkippableFact` mit Prüfung auf installierte Playwright-Browser | `dotnet test Rezepte.sln` muss auf einer Entwicklermaschine ohne `playwright install` weiterhin grün durchlaufen. Stilles Bestehen ohne Prüfung wäre irreführend, ein harter Fehlschlag wäre unbrauchbar — ein sichtbarer Skip ist der einzige ehrliche Zwischenweg. |

### Beantwortete Fragen aus der Anforderung

| Frage | Entscheidung |
|-------|--------------|
| Sichtbarkeitsdauer / sehr langsame Requests | Die Animation läuft als Endlos-Sweep, solange die Navigation dauert, und wird bei `enhancedload` bzw. `pageshow` nach `HideDelay` ausgeblendet. Zusätzlich beendet ein Sicherheits-Timeout `MaxVisibleDuration` (Standard `15s`) die Anzeige, damit sie bei abgebrochener oder fehlgeschlagener Navigation nicht dauerhaft stehen bleibt. |
| Mehrfache Navigation | Die laufende Animation wird neu gestartet und erhält eine neue Zufallsfarbe. Es wird immer nur **ein** Balken angezeigt. |
| Ladeabschluss-Erkennung | Über `enhancedload` (Primär) und `pageshow` (Rückfall). Kein HTTP-Request-Counter, kein Cascading Parameter. |
| Responsive Design | Keine gerätespezifischen Varianten. Der Balken ist immer 100 % breit; Höhe und Dauer sind global konfiguriert. |
| Accessibility | Der Balken ist rein dekorativ und wird mit `aria-hidden="true"` von Screenreadern ausgeblendet. Die Seitenansage übernimmt bereits `FocusOnNavigate` in `Routes.razor`. Zusätzlich wird `prefers-reduced-motion: reduce` respektiert: statt der Bewegung erscheint ein statischer, farbiger Balken. |
| Farbliste | Die Standardfarben werden hart in `LoadingBarOptions` hinterlegt und sind über `appsettings.json` überschreibbar. Die Projektpalette aus `wwwroot/app.css` (`--app-primary` `#5A1F1F`, `--app-accent` `#2e7d32`) wird **nicht** verwendet, weil diese Töne auf dem dunklen Bordeaux-Verlauf der `.app-navbar` praktisch unsichtbar wären. |
| Performance | Bei `Enabled: false` rendert `LoadingBar.razor` überhaupt kein Markup; das Skript findet das Host-Element nicht und registriert keine Listener. |
| Formularbasierte Navigation (Suchleiste) | **Eingeschlossen.** Ein `submit`-Listener in der Capture-Phase auf `document` startet die Animation analog zum Klick. Damit verhält sich die Suchleiste identisch zu einem Linkklick. |
| Kompatibilität von `bunit` mit `net10.0` | Wird vor Umsetzung der Komponententests durch einen Testbuild geprüft. Beide Ergebnisse sind als Umsetzungsschritte ausformuliert (Schritte 14a und 14b). |
| Tiefe der E2E-Abdeckung | Es wird eine echte Browser-Testinfrastruktur mit Playwright in einem neuen Projekt `Rezepte.Tests.Browser` aufgebaut, die das Laufzeitverhalten prüft. Die dateibasierten Verdrahtungstests bleiben als schnelle, browserunabhängige Absicherung der Kette Konfiguration → Registrierung → Layout → Skript zusätzlich bestehen. |

## Programmabläufe

### Rendern des Layouts

1. Eine beliebige Seite wird gerendert und verwendet `MainLayout` als Layout.
2. `MainLayout.razor` rendert die Navigationsleiste und direkt nach dem schließenden `</nav>`-Element die Komponente `LoadingBar`.
3. `LoadingBar` bezieht über Property-Injection den `ILoadingBarService` und ruft `GetSettings()` auf.
4. `LoadingBarService` liefert das beim ersten Aufruf berechnete und zwischengespeicherte `LoadingBarViewModel`.
5. Ist `LoadingBarViewModel.Enabled` `false`, rendert die Komponente nichts und der Ablauf endet.
6. Andernfalls rendert die Komponente das Host-Element mit fester Id, `aria-hidden="true"`, `data-permanent`, den CSS-Custom-Properties für Höhe und Animationsdauer sowie den `data-*`-Attributen für Farbliste, `HideDelayMilliseconds` und `MaxVisibleDurationMilliseconds`. Darin liegt das Indikator-Element, das die eigentliche Bewegung ausführt.

Beteiligte Klassen/Komponenten: `MainLayout`, `LoadingBar`, `ILoadingBarService`, `LoadingBarService`, `LoadingBarViewModel`

### Initialisierung des Clientskripts

1. `App.razor` lädt `_framework/blazor.web.js` und danach `js/loadingBar.js`.
2. Das Skript sucht das Host-Element der Ladeanimation. Wird es nicht gefunden (Feature deaktiviert), beendet es sich ohne Nebenwirkungen.
3. Das Skript liest Farbliste, `HideDelay` und `MaxVisibleDuration` aus den `data-*`-Attributen des Host-Elements.
4. Es registriert einen `click`-Listener auf `document` in der Capture-Phase.
5. Es registriert einen `submit`-Listener auf `document` in der Capture-Phase.
6. Es registriert einen `pageshow`-Listener auf `window`.
7. Ist das globale `Blazor`-Objekt vorhanden, registriert es zusätzlich einen `enhancedload`-Listener.

Beteiligte Komponenten: `App.razor`, `wwwroot/js/loadingBar.js`

### Benutzer klickt einen Navigationslink

1. Der `click`-Listener ermittelt über `closest` den nächstgelegenen Anker mit `href`.
2. Der Klick wird ignoriert, wenn kein Anker gefunden wird, wenn das Ereignis bereits abgebrochen wurde (`defaultPrevented`), wenn nicht die primäre Maustaste verwendet wurde, wenn eine Modifikatortaste gedrückt ist, wenn der Anker `target` ungleich `_self` oder `download` besitzt, wenn das Schema `mailto:`, `tel:` oder `javascript:` lautet, wenn das Ziel ein reiner Fragment-Link oder mit der aktuellen Adresse identisch ist oder wenn das Ziel auf einem fremden Origin liegt.
3. Andernfalls wird der gemeinsame Ablauf „Animation starten" ausgeführt.

Beteiligte Komponenten: `wwwroot/js/loadingBar.js`, `LoadingBar.razor.css`

### Benutzer sendet ein Formular ab (Suchleiste, Login, Registrierung, Abmelden)

1. Der `submit`-Listener ermittelt das auslösende `<form>`-Element aus `event.target`.
2. Der Vorgang wird ignoriert, wenn kein Formular ermittelt werden kann, wenn das Ereignis bereits abgebrochen wurde (`defaultPrevented`), wenn `target` ungleich `_self` gesetzt ist oder wenn die effektive Zieladresse (aus `action` bzw. dem Absende-Button `formaction`) auf einem fremden Origin liegt.
3. Andernfalls wird der gemeinsame Ablauf „Animation starten" ausgeführt.
4. Bei der Suchleiste in `MainLayout` bedeutet das: Der Balken erscheint im selben Moment, in dem das Formular abgesendet wird — unabhängig davon, ob die Navigation über `OnSubmitSearch()`/`NavigationManager.NavigateTo`, über Enhanced Forms oder über einen vollständigen Seitenwechsel erfolgt.

Beteiligte Komponenten: `wwwroot/js/loadingBar.js`, `MainLayout` (`OnSubmitSearch()`)

### Animation starten (gemeinsamer Ablauf für Klick und Submit)

1. Das Host-Element wird aufgelöst; ist es nicht vorhanden, endet der Ablauf.
2. Ein eventuell laufender Ausblend-Timer wird abgebrochen.
3. Aus der Farbliste wird zufällig eine Farbe gewählt; enthält die Liste mehr als einen Eintrag, wird die zuletzt verwendete Farbe ausgeschlossen. Die Farbe wird als CSS-Custom-Property `--loading-bar-color` auf dem Host-Element gesetzt.
4. Die Aktiv-Klasse wird entfernt, ein Reflow erzwungen und die Aktiv-Klasse wieder gesetzt. Dadurch startet die CSS-Animation auch bei schnell aufeinanderfolgenden Interaktionen sichtbar neu.
5. Der Sicherheits-Timer über `MaxVisibleDurationMilliseconds` wird neu gestartet.

Beteiligte Komponenten: `wwwroot/js/loadingBar.js`, `LoadingBar.razor.css`

### Navigation ist abgeschlossen

1. Blazor patcht den neuen Seiteninhalt ein und löst `enhancedload` aus (bei vollständiger Seitenladung stattdessen `pageshow`). Das Host-Element bleibt wegen `data-permanent` unverändert erhalten.
2. Der Handler löst das Host-Element defensiv neu auf und bricht den Sicherheits-Timer ab.
3. Nach Ablauf von `HideDelayMilliseconds` wird die Aktiv-Klasse entfernt; der Balken blendet über die CSS-Transition aus und die Animation stoppt.

Beteiligte Komponenten: `wwwroot/js/loadingBar.js`, `LoadingBar.razor.css`

### Navigation bleibt aus oder schlägt fehl

1. Weder `enhancedload` noch `pageshow` treten ein.
2. Nach `MaxVisibleDurationMilliseconds` läuft der Sicherheits-Timer ab.
3. Der Handler entfernt die Aktiv-Klasse unmittelbar, ohne `HideDelay` abzuwarten.

Beteiligte Komponenten: `wwwroot/js/loadingBar.js`

### Browsertest: Anwendung starten und Benutzer anmelden

1. `RezepteAppFixture` legt ein temporäres Verzeichnis mit einer eigenen SQLite-Datei an und ermittelt einen freien TCP-Port.
2. Die Fixture startet `dotnet run --project Rezepte.Web --no-build --configuration Release` mit den Umgebungsvariablen `ASPNETCORE_URLS`, `ConnectionStrings__Default` sowie optionalen Überschreibungen des `LoadingBar`-Abschnitts (`LoadingBar__Enabled`, `LoadingBar__MaxVisibleDuration`, …).
3. Die Fixture pollt die Basisadresse bis zur ersten erfolgreichen Antwort oder bis zum Ablauf eines Startzeitfensters.
4. Über `POST api/auth/register` (JSON) wird ein Testbenutzer angelegt, damit die `RedirectToRegisterMiddleware` nicht mehr auf `/register` umleitet.
5. Der Browsertest öffnet `/login`, füllt `#username` und `#password` aus und sendet das Formular ab; die Anwendung setzt das Auth-Cookie und leitet auf `/` um.
6. Am Ende beendet die Fixture den Prozess und löscht das temporäre Verzeichnis.

Beteiligte Klassen: `RezepteAppFixture`, `PlaywrightBrowserFixture`, `LoadingBarPageObject`

### Browsertest: Ladeanimation bei langsamer Navigation

1. Der Test verzögert über `Page.RouteAsync` die Antwort auf die Zielseite künstlich.
2. Der Test klickt einen Navigationslink.
3. Noch während der laufenden Anfrage prüft der Test, dass das Host-Element die Aktiv-Klasse trägt, sichtbar ist (Opazität > 0) und eine `--loading-bar-color` aus der konfigurierten Farbliste gesetzt hat.
4. Nach Abschluss der Navigation prüft der Test, dass die Aktiv-Klasse innerhalb einer Wartefrist wieder entfernt wurde.

Beteiligte Klassen: `LoadingBarVisibilityBrowserTests`, `LoadingBarPageObject`

## Neue Klassen

| Klasse | Typ | Zweck |
|--------|-----|-------|
| `LoadingBarOptions` | Options-/Konfigurationsklasse (`Rezepte.Web/Configuration/`) | Bindung des `LoadingBar`-Abschnitts aus `appsettings.json` inklusive Standardwerten |
| `LoadingBarViewModel` | Unveränderliches Record (`Rezepte.Web/ViewModels/`) | Normalisierte, gerenderte Darstellungswerte: `Enabled`, `Height`, `AnimationDuration`, `Colors`, `HideDelayMilliseconds`, `MaxVisibleDurationMilliseconds` |
| `ILoadingBarService` | Interface (`Rezepte.Web/Services/`) | Vertrag mit einer Methode `GetSettings()` |
| `LoadingBarService` | Klasse (`Rezepte.Web/Services/`) | Validiert und normalisiert `LoadingBarOptions`, cached das Ergebnis, protokolliert verworfene Werte |
| `LoadingBar` | Blazor-Komponente (`Rezepte.Web/Components/Layout/LoadingBar.razor`) | Rendert das Host-Element der Ladeanimation |
| — | Scoped Stylesheet (`Rezepte.Web/Components/Layout/LoadingBar.razor.css`) | Maße, Farbverlauf, `@keyframes` für den Rechts-nach-links-Sweep, `prefers-reduced-motion`-Variante |
| — | Clientskript (`Rezepte.Web/wwwroot/js/loadingBar.js`) | Klick- und Submit-Erkennung, Farbwahl, Start/Neustart und Ausblenden der Animation |
| `Rezepte.Tests.Browser` | Neues Testprojekt (`Rezepte.Tests.Browser/Rezepte.Tests.Browser.csproj`) | Beheimatet die Playwright-basierten Browsertests, getrennt von den schnellen Unit-Tests |
| `RezepteAppFixture` | Testinfrastruktur-Klasse (`Rezepte.Tests.Browser/Infrastructure/`) | Startet die Anwendung als eigenen Prozess mit temporärer Datenbank und konfigurierbaren `LoadingBar`-Umgebungsvariablen, seedet einen Testbenutzer, räumt auf |
| `PlaywrightBrowserFixture` | Testinfrastruktur-Klasse (`Rezepte.Tests.Browser/Infrastructure/`) | Initialisiert Playwright, startet Chromium headless, erkennt fehlende Browser-Binaries für den Skip-Pfad |
| `LoadingBarPageObject` | Testinfrastruktur-Klasse (`Rezepte.Tests.Browser/Infrastructure/`) | Kapselt Anmeldung, Zugriff auf Host-Element, Aktiv-Zustand, aktuelle Farbe und das Verzögern von Antworten |
| `BrowserTestCollection` | xUnit-Collection-Definition (`Rezepte.Tests.Browser/Infrastructure/`) | Teilt Anwendungs- und Browser-Fixture über alle Standard-Browsertestklassen |

## Änderungen an bestehenden Klassen

### `MainLayout` (Blazor-Layout-Komponente, `Rezepte.Web/Components/Layout/MainLayout.razor`)

- **Geänderte Struktur:** Direkt nach dem schließenden `</nav>`-Element (aktuell Zeile 87) und vor `<main class="container py-4">` wird `<LoadingBar />` eingefügt.
- Keine Änderungen an `searchQuery`, `OnSubmitSearch()` oder der `NavigationManager`-Injektion. Die Anbindung der Suchleiste erfolgt ausschließlich über den `submit`-Listener im Clientskript und erfordert keine Änderung an der Komponente.

### `App` (Blazor-Root-Komponente, `Rezepte.Web/Components/App.razor`)

- **Geänderte Struktur:** Im `<body>` wird nach `<script src="_framework/blazor.web.js"></script>` ein `<script src="js/loadingBar.js"></script>` ergänzt. Die Reihenfolge ist zwingend, damit das globale `Blazor`-Objekt für `addEventListener('enhancedload', …)` verfügbar ist.

### `ServiceCollectionExtensions` (statische Klasse, `Rezepte.Web/Extensions/ServiceCollectionExtensions.cs`)

- **Geänderte Methode:** `AddRezepteServices` — im Konfigurationsblock (nach `services.Configure<GoogleCredentialsOptions>(...)`, aktuell Zeile 31) wird `services.Configure<LoadingBarOptions>(configuration.GetSection("LoadingBar"))` ergänzt; im Registrierungsblock der Anwendungsdienste wird `services.AddSingleton<ILoadingBarService, LoadingBarService>()` ergänzt.

### `MainLayout.razor.css` (Scoped Stylesheet)

- Keine Änderung. Sämtliche Stile der Ladeanimation liegen ausschließlich in `LoadingBar.razor.css`, damit die Scoped-CSS-Isolation greift.

### `appsettings.json`

- **Neuer Abschnitt:** `LoadingBar` mit den Schlüsseln `Enabled`, `Height`, `AnimationDuration`, `HideDelay`, `MaxVisibleDuration` und `Colors`.

### `Rezepte.sln`

- **Neuer Projekteintrag:** `Rezepte.Tests.Browser` inklusive Konfigurationszuordnungen für `Debug`/`Release` und die vorhandenen Plattformen, damit `dotnet build`, `dotnet test` und `dotnet format` das Projekt erfassen.

### `Rezepte.Tests/Rezepte.Tests.csproj`

- **Möglicher neuer Paketverweis:** `bunit` — nur im Zweig „bUnit funktioniert" (siehe Schritt 14a). Im Zweig 14b bleibt die Datei unverändert.

### `.github/workflows/pr.yml`

- **Neuer Build-Schritt:** Build von `Rezepte.Tests.Browser` in Release.
- **Neuer Installationsschritt:** Installation der Chromium-Browser-Binaries über das vom Playwright-Paket erzeugte `playwright.ps1` (mit `--with-deps`), ausgeführt nach dem Build und vor `dotnet test`.
- Der bestehende Schritt `dotnet test Rezepte.sln --configuration Release --no-build` bleibt unverändert und führt die Browsertests dadurch mit aus.

## Datenbankmigrationen

Keine.

## Validierungsregeln

Alle Regeln werden in `LoadingBarService` beim Aufbau des `LoadingBarViewModel` angewendet. Es wird **nie** eine Exception geworfen; ungültige Werte werden verworfen, durch den Standardwert ersetzt und über `ILogger<LoadingBarService>` als Warnung in englischer Sprache protokolliert.

| Feld / Objekt | Regel | Fehlerfall |
|---------------|-------|------------|
| `LoadingBarOptions.Height` | Muss eine CSS-Länge der Form Zahl + `px`/`rem`/`em` sein | Rückfall auf `3px`, Warn-Log |
| `LoadingBarOptions.AnimationDuration` | Muss eine CSS-Zeit der Form Zahl + `ms`/`s` sein | Rückfall auf `2s`, Warn-Log |
| `LoadingBarOptions.HideDelay` | Muss eine CSS-Zeit der Form Zahl + `ms`/`s` sein; wird nach Millisekunden umgerechnet | Rückfall auf `300ms`, Warn-Log |
| `LoadingBarOptions.MaxVisibleDuration` | Muss eine CSS-Zeit der Form Zahl + `ms`/`s` sein; wird nach Millisekunden umgerechnet; muss größer als `HideDelay` sein | Rückfall auf `15s`, Warn-Log |
| `LoadingBarOptions.Colors` (Einzeleintrag) | Muss ein Hex-Farbwert der Form `#RGB` oder `#RRGGBB` sein | Eintrag wird aus der Liste entfernt, Warn-Log |
| `LoadingBarOptions.Colors` (Liste) | Muss nach dem Filtern mindestens einen Eintrag enthalten | Rückfall auf die vollständige Standardfarbliste, Warn-Log |

## Konfigurationsänderungen

Neuer Abschnitt `LoadingBar` in `Rezepte.Web/appsettings.json`, gebunden an `LoadingBarOptions`.

| Eintrag | Typ | Standardwert | Zweck |
|---------|-----|--------------|-------|
| `LoadingBar:Enabled` | `bool` | `true` | Schaltet das Feature global ab; bei `false` wird kein Markup gerendert |
| `LoadingBar:Height` | `string` | `"3px"` | Höhe des Balkens, als CSS-Custom-Property `--loading-bar-height` gerendert |
| `LoadingBar:AnimationDuration` | `string` | `"2s"` | Dauer eines vollständigen Sweeps von rechts nach links, als `--loading-bar-duration` gerendert |
| `LoadingBar:HideDelay` | `string` | `"300ms"` | Wartezeit nach abgeschlossener Navigation bis zum Ausblenden |
| `LoadingBar:MaxVisibleDuration` | `string` | `"15s"` | Sicherheitsgrenze, nach der die Anzeige auch ohne Abschlusssignal endet |
| `LoadingBar:Colors` | `string[]` | `["#FF6B6B", "#4ECDC4", "#45B7D1", "#96CEB4", "#FFEAA7", "#DDA0DD"]` | Farben, aus denen pro Navigationsinteraktion zufällig gewählt wird |

Die Browsertests überschreiben diese Werte prozessweise über Umgebungsvariablen (`LoadingBar__Enabled`, `LoadingBar__MaxVisibleDuration`, `LoadingBar__Colors__0`, …). Es entstehen dadurch keine zusätzlichen Konfigurationsdateien.

## Seiteneffekte und Risiken

- **`MainLayout`:** Zwischen Navigationsleiste und `<main>` entsteht ein zusätzliches Element. Da die Sichtbarkeit über `opacity` und nicht über `display` gesteuert wird, ist der Platz dauerhaft reserviert und es entsteht kein Layout-Shift beim Ein- oder Ausblenden. Der Abstand zum Seiteninhalt verschiebt sich jedoch dauerhaft um die konfigurierte Höhe.
- **Formularbasierte Navigation:** Durch den `submit`-Listener erscheint der Balken künftig auch beim Login-, Registrierungs- und Logout-Formular sowie bei allen anderen Formularen der Anwendung, nicht nur bei der Suchleiste. Das ist beabsichtigt und konsistent, wurde bisher aber nicht so ausgeliefert. Formulare, deren Submit von einer interaktiven Komponente per `preventDefault` abgefangen wird, lösen dank der `defaultPrevented`-Prüfung keine Animation aus.
- **`data-permanent`:** Das Attribut nimmt das Host-Element vom DOM-Patching der Enhanced Navigation aus. Wird die Struktur oder die Konfiguration der `LoadingBar` später geändert, greifen die neuen Werte erst nach einem vollständigen Seiten-Reload, nicht nach einer Enhanced Navigation. Für rein clientseitig gesteuerte Werte ist das unkritisch.
- **Enhanced Navigation:** Der Entwurf setzt voraus, dass die Enhanced Navigation aktiv bleibt. Wird sie später global über `data-enhance-nav="false"` deaktiviert, greift nur noch der `pageshow`-Rückfall — der Balken wird dann erst beim Rendern der Zielseite ausgeblendet, was funktional korrekt, aber weniger präzise ist.
- **`App.razor`:** Eine weitere Skriptdatei wird geladen. Da sie klein und ohne Abhängigkeiten ist, ist die Auswirkung auf die Ladezeit vernachlässigbar; die Position nach `blazor.web.js` ist jedoch zwingend und darf beim Umsortieren der Skripte nicht verletzt werden.
- **Scoped-CSS-Bundle:** `Rezepte.Web.styles.css` wird um die Regeln der neuen Komponente erweitert. Die `@keyframes`-Definition ist global sichtbar und benötigt daher einen eindeutigen, präfigierten Namen, um Kollisionen mit bestehenden Animationen auszuschließen.
- **Interaktive Seiten:** Auf Seiten mit `@rendermode InteractiveServer` erfolgt die Navigation weiterhin über Blazor. Klicks auf `NavLink`-Elemente lösen ebenfalls den Capture-Listener aus; das Ausblenden erfolgt in diesem Fall über `enhancedload`.
- **Neues Testprojekt / CI-Laufzeit:** Die Browsertests starten die Anwendung als eigenen Prozess und laden einen Chromium-Browser. Die CI-Laufzeit steigt dadurch spürbar (Browser-Installation plus Anwendungsstart je Fixture). Das bestehende `timeout-minutes: 20` in `pr.yml` muss beobachtet und bei Bedarf angehoben werden.
- **Portbelegung und Prozessleben:** `RezepteAppFixture` bindet einen freien Port und startet einen Kindprozess. Bricht ein Testlauf hart ab, kann ein verwaister `dotnet`-Prozess zurückbleiben. Die Fixture muss den Prozess im `DisposeAsync` zuverlässig beenden (inkl. Kindprozessen) und die temporäre Datenbankdatei löschen.
- **Determinismus der Browsertests:** Zeitabhängige Prüfungen (Sichtbarkeit *während* der Navigation) sind grundsätzlich flakeanfällig. Die Verzögerung wird deshalb über Routen-Interception erzwungen und nicht über Annahmen zur Serverlaufzeit; Wartebedingungen werden über Playwright-Polling statt über feste Wartezeiten formuliert.
- **Testprojekt `Rezepte.Tests`:** Im Zweig 14a kommt mit `bunit` eine neue Testabhängigkeit hinzu; im Zweig 14b bleibt das Projekt unverändert.

## Umsetzungsreihenfolge

1. **`LoadingBarOptions` anlegen**
   - Voraussetzungen: Keine — `Rezepte.Web/Configuration/` existiert bereits mit vergleichbaren Klassen (`ImageOptions`, `PluginUpdateOptions`).
   - Beschreibung: Sealed Options-Klasse mit den sechs Eigenschaften und den in diesem Plan festgelegten Standardwerten, XML-Dokumentation in englischer Sprache.

2. **`LoadingBarViewModel` anlegen**
   - Voraussetzungen: Keine.
   - Beschreibung: Unveränderliches Record im Ordner `Rezepte.Web/ViewModels/` mit den normalisierten Werten inklusive der bereits in Millisekunden umgerechneten Zeitangaben.

3. **`ILoadingBarService` anlegen**
   - Voraussetzungen: Schritt 2 (`LoadingBarViewModel` als Rückgabetyp).
   - Beschreibung: Interface mit der Methode `GetSettings()`.

4. **`LoadingBarService` implementieren**
   - Voraussetzungen: Schritte 1–3.
   - Beschreibung: Implementierung mit `IOptions<LoadingBarOptions>` und `ILogger<LoadingBarService>`; Validierung und Normalisierung gemäß Abschnitt „Validierungsregeln"; einmalige Berechnung und Zwischenspeicherung des Ergebnisses.

5. **Service und Optionen registrieren**
   - Voraussetzungen: Schritte 1, 3 und 4.
   - Beschreibung: In `AddRezepteServices` die `Configure<LoadingBarOptions>`-Bindung im Konfigurationsblock und `AddSingleton<ILoadingBarService, LoadingBarService>()` im Dienste-Block ergänzen.

6. **`appsettings.json` erweitern**
   - Voraussetzungen: Schritt 1 (Schlüsselnamen müssen den Eigenschaften entsprechen).
   - Beschreibung: Abschnitt `LoadingBar` mit den sechs Einträgen und den Standardwerten aus diesem Plan ergänzen.

7. **`LoadingBar.razor` anlegen**
   - Voraussetzungen: Schritte 2–5 (Service muss auflösbar sein).
   - Beschreibung: Komponente, die `ILoadingBarService` injiziert, bei `Enabled == false` nichts rendert und andernfalls Host- und Indikator-Element mit fester Id, `aria-hidden="true"`, `data-permanent`, CSS-Custom-Properties und `data-*`-Attributen ausgibt.

8. **`LoadingBar.razor.css` anlegen**
   - Voraussetzungen: Schritt 7 (Markup-Struktur und Klassennamen stehen fest).
   - Beschreibung: Grundmaße, Farbverlauf über `--loading-bar-color`, Opazitäts-Transition, eindeutig benannte `@keyframes` für die Bewegung von rechts nach links, `prefers-reduced-motion`-Variante ohne Bewegung.

9. **`wwwroot/js/loadingBar.js` anlegen**
   - Voraussetzungen: Schritte 7 und 8 (Vertrag über Id, Klassennamen und `data-*`-Attribute muss feststehen).
   - Beschreibung: Selbstinitialisierendes Skript nach dem Muster von `randomFromCookbooks.js`, mit dem gemeinsamen Ablauf „Animation starten", der Klick-Filterung, der Submit-Filterung, Farbwahl, Animations-Neustart, Ausblende-Verzögerung, Sicherheits-Timeout sowie den Handlern für `enhancedload` und `pageshow`.

10. **Skript in `App.razor` einbinden**
    - Voraussetzungen: Schritt 9.
    - Beschreibung: `<script src="js/loadingBar.js"></script>` unmittelbar nach `_framework/blazor.web.js` einfügen.

11. **`LoadingBar` in `MainLayout.razor` einbinden**
    - Voraussetzungen: Schritte 7 und 10.
    - Beschreibung: `<LoadingBar />` direkt nach dem schließenden `</nav>`-Element und vor `<main>` einsetzen.

12. **Unit-Tests für `LoadingBarService` schreiben**
    - Voraussetzungen: Schritt 4; `xunit`, `FluentAssertions` und `Moq` sind bereits in `Rezepte.Tests` vorhanden.
    - Beschreibung: Testklassen `LoadingBarServiceTests_Defaults`, `LoadingBarServiceTests_Validation` und `LoadingBarServiceTests_DurationParsing` gemäß Abschnitt „Tests" anlegen.

13. **Kompatibilität von `bunit` mit `net10.0` prüfen (Weichenstellung)**
    - Voraussetzungen: Schritt 7 (eine renderbare Komponente muss existieren).
    - Beschreibung: `PackageReference` auf die aktuelle stabile `bunit`-Version in `Rezepte.Tests.csproj` ergänzen, `dotnet restore` und `dotnet build Rezepte.Tests/Rezepte.Tests.csproj` ausführen sowie einen minimalen Render-Smoke-Test für `LoadingBar` laufen lassen. Das Ergebnis entscheidet zwischen Schritt 14a und 14b; genau einer der beiden Schritte wird ausgeführt.

14a. **Zweig „bUnit funktioniert": Komponententests mit bUnit schreiben**
    - Voraussetzungen: Schritt 13 mit erfolgreichem Build und erfolgreichem Smoke-Test.
    - Beschreibung: Der `bunit`-Paketverweis bleibt bestehen. Testklasse `LoadingBarComponentTests_Rendering` mit gemocktem `ILoadingBarService` anlegen und die fünf Rendering-Tests aus dem Abschnitt „Tests" implementieren.

14b. **Zweig „bUnit funktioniert nicht": markup-basierte Prüfungen schreiben**
    - Voraussetzungen: Schritt 13 mit fehlgeschlagenem Restore, Build oder Smoke-Test.
    - Beschreibung: Den `bunit`-Paketverweis aus `Rezepte.Tests.csproj` wieder entfernen. Stattdessen die Testklasse `LoadingBarMarkupTests_Rendering` nach dem dateilesenden Muster von `DeploymentDocumentationTests` anlegen, die `LoadingBar.razor` liest und den Markup-Vertrag prüft (bedingtes Rendern über `Enabled`, feste Id, `aria-hidden="true"`, `data-permanent`, die beiden CSS-Custom-Properties, die `data-*`-Attribute). Die Verhaltensabsicherung des tatsächlichen Renderings übernehmen in diesem Zweig vollständig die Browsertests aus den Schritten 16–21. Die Unit-Tests aus Schritt 12 bleiben unverändert.

15. **Dateibasierte Verdrahtungstests schreiben**
    - Voraussetzungen: Schritte 5, 6 und 9–11.
    - Beschreibung: Testklasse `LoadingBarWiringTests` in `Rezepte.Tests` nach dem dateilesenden Muster von `DeploymentDocumentationTests`, die die Kette Konfiguration → Registrierung → Layout → Skripteinbindung schnell und browserunabhängig absichert.

16. **Testprojekt `Rezepte.Tests.Browser` anlegen**
    - Voraussetzungen: Keine.
    - Beschreibung: Neues xUnit-Testprojekt auf `net10.0` mit `Microsoft.NET.Test.Sdk`, `xunit`, `xunit.runner.visualstudio`, `FluentAssertions`, `Microsoft.Playwright` und `Xunit.SkippableFact`; Projektverweis auf `Rezepte.Web` nur, soweit für Startpfadauflösung nötig; Eintrag in `Rezepte.sln` inklusive aller vorhandenen Konfigurations-/Plattformkombinationen.

17. **`PlaywrightBrowserFixture` implementieren**
    - Voraussetzungen: Schritt 16.
    - Beschreibung: Fixture, die Playwright initialisiert, Chromium headless startet und bei fehlenden Browser-Binaries eine auswertbare Kennzeichnung setzt, damit die Tests über `Skip.IfNot(...)` übersprungen statt fehlgeschlagen werden.

18. **`RezepteAppFixture` implementieren**
    - Voraussetzungen: Schritte 5, 6, 10, 11 und 16 (die Anwendung muss die Ladeanimation bereits ausliefern) sowie ein vorhandener Release-Build von `Rezepte.Web`.
    - Beschreibung: Fixture, die einen freien Port ermittelt, ein temporäres Verzeichnis mit eigener SQLite-Datei anlegt, die Anwendung als Kindprozess mit `ASPNETCORE_URLS`, `ConnectionStrings__Default` und optionalen `LoadingBar__*`-Überschreibungen startet, auf Erreichbarkeit pollt, über `POST api/auth/register` einen Testbenutzer anlegt und im `DisposeAsync` Prozess und Verzeichnis zuverlässig aufräumt.

19. **`LoadingBarPageObject` und `BrowserTestCollection` implementieren**
    - Voraussetzungen: Schritte 17 und 18.
    - Beschreibung: Page-Object mit `LoginAsync`, `GotoAsync`, `IsLoadingBarActiveAsync`, `GetLoadingBarColorAsync`, `GetLoadingBarOpacityAsync`, `WaitUntilLoadingBarHiddenAsync` und `DelayNextNavigationAsync` (Routen-Interception); dazu die xUnit-Collection-Definition, die Anwendungs- und Browser-Fixture für die Standardtestklassen teilt.

20. **Browsertests für Sichtbarkeit, Farbwechsel und Formularnavigation schreiben**
    - Voraussetzungen: Schritt 19.
    - Beschreibung: Testklassen `LoadingBarVisibilityBrowserTests`, `LoadingBarColorBrowserTests` und `LoadingBarFormNavigationBrowserTests` gemäß Abschnitt „E2E-Tests" implementieren.

21. **Browsertests mit abweichender Konfiguration schreiben**
    - Voraussetzungen: Schritt 19.
    - Beschreibung: Testklassen `LoadingBarSafetyTimeoutBrowserTests` und `LoadingBarDisabledBrowserTests`, die jeweils eine eigene `RezepteAppFixture`-Instanz mit überschriebenen Umgebungsvariablen (`LoadingBar__MaxVisibleDuration` bzw. `LoadingBar__Enabled=false`) starten.

22. **CI-Workflow für Browsertests erweitern**
    - Voraussetzungen: Schritte 16–21.
    - Beschreibung: In `.github/workflows/pr.yml` einen Build-Schritt für `Rezepte.Tests.Browser` und danach einen Schritt zur Installation der Chromium-Binaries über das erzeugte `playwright.ps1` (`install --with-deps chromium`) ergänzen; anschließend prüfen, ob das Zeitlimit des Jobs weiterhin ausreicht, und es andernfalls anheben.

## Tests

### Neue Tests

| Test / Hilfsmethode | Testklasse | Was wird geprüft / bereitgestellt? |
|--------------------|------------|-------------------------------------|
| `CreateService(LoadingBarOptions)` (Hilfsmethode) | `LoadingBarServiceTests_Defaults` | Erzeugt `LoadingBarService` mit `Options.Create(...)` und einem Null-Logger |
| `GetSettings_WithDefaultOptions_ReturnsDocumentedDefaults` | `LoadingBarServiceTests_Defaults` | Leere Konfiguration liefert `3px`, `2s`, 300 ms, 15000 ms und sechs Standardfarben |
| `GetSettings_WithEnabledFalse_ReturnsDisabledSettings` | `LoadingBarServiceTests_Defaults` | `Enabled: false` wird unverändert durchgereicht |
| `GetSettings_CalledTwice_ReturnsSameInstance` | `LoadingBarServiceTests_Defaults` | Das normalisierte Ergebnis wird zwischengespeichert |
| `GetSettings_WithValidCustomOptions_ReturnsConfiguredValues` | `LoadingBarServiceTests_Defaults` | Gültige abweichende Werte werden übernommen, nicht durch Standardwerte ersetzt |
| `GetSettings_WithInvalidHeight_FallsBackToDefaultHeight` | `LoadingBarServiceTests_Validation` | Ungültige Höhenangabe wird durch `3px` ersetzt |
| `GetSettings_WithInvalidAnimationDuration_FallsBackToDefaultDuration` | `LoadingBarServiceTests_Validation` | Ungültige Animationsdauer wird durch `2s` ersetzt |
| `GetSettings_WithInvalidHideDelay_FallsBackToDefaultHideDelay` | `LoadingBarServiceTests_Validation` | Ungültige Verzögerung wird durch 300 ms ersetzt |
| `GetSettings_WithMaxVisibleDurationBelowHideDelay_FallsBackToDefault` | `LoadingBarServiceTests_Validation` | Sicherheitsgrenze kleiner als die Ausblende-Verzögerung wird verworfen |
| `GetSettings_WithInvalidColorEntries_RemovesInvalidEntries` | `LoadingBarServiceTests_Validation` | Nicht-Hex-Einträge werden aus der Farbliste entfernt, gültige bleiben erhalten |
| `GetSettings_WithOnlyInvalidColors_FallsBackToDefaultColors` | `LoadingBarServiceTests_Validation` | Leere oder vollständig ungültige Farbliste führt zur Standardliste |
| `GetSettings_WithHideDelayInMilliseconds_ConvertsToMilliseconds` | `LoadingBarServiceTests_DurationParsing` | `"250ms"` wird zu 250 |
| `GetSettings_WithHideDelayInSeconds_ConvertsToMilliseconds` | `LoadingBarServiceTests_DurationParsing` | `"0.5s"` wird zu 500 |
| `GetSettings_WithMaxVisibleDurationInSeconds_ConvertsToMilliseconds` | `LoadingBarServiceTests_DurationParsing` | `"20s"` wird zu 20000 |
| `Render_WhenEnabled_RendersHostElementWithConfiguredId` | `LoadingBarComponentTests_Rendering` (Zweig 14a) bzw. `LoadingBarMarkupTests_Rendering` (Zweig 14b) | Host-Element mit vereinbarter Id und Indikator-Kindelement wird ausgegeben |
| `Render_WhenDisabled_RendersNoMarkup` | `LoadingBarComponentTests_Rendering` / `LoadingBarMarkupTests_Rendering` | Bei `Enabled: false` entsteht kein Markup |
| `Render_WhenEnabled_WritesHeightAndDurationAsCssCustomProperties` | `LoadingBarComponentTests_Rendering` / `LoadingBarMarkupTests_Rendering` | `--loading-bar-height` und `--loading-bar-duration` stehen im `style`-Attribut |
| `Render_WhenEnabled_WritesColorsAndTimingsAsDataAttributes` | `LoadingBarComponentTests_Rendering` / `LoadingBarMarkupTests_Rendering` | Farbliste, `HideDelay` und `MaxVisibleDuration` stehen in den `data-*`-Attributen |
| `Render_WhenEnabled_MarksBarAsDecorativeAndPermanent` | `LoadingBarComponentTests_Rendering` / `LoadingBarMarkupTests_Rendering` | `aria-hidden="true"` und `data-permanent` sind gesetzt |
| `Layout_ShouldPlaceLoadingBarDirectlyBelowNavigation` | `LoadingBarWiringTests` | `<LoadingBar />` steht in `MainLayout.razor` zwischen `</nav>` und `<main>` |
| `App_ShouldLoadLoadingBarScriptAfterBlazorScript` | `LoadingBarWiringTests` | `js/loadingBar.js` wird in `App.razor` nach `_framework/blazor.web.js` geladen |
| `Configuration_ShouldProvideLoadingBarSectionAndServiceRegistration` | `LoadingBarWiringTests` | `appsettings.json` enthält den Abschnitt `LoadingBar`; `ServiceCollectionExtensions` bindet Optionen und registriert den Service |
| `Script_ShouldRegisterClickAndSubmitListenersInCapturePhase` | `LoadingBarWiringTests` | `loadingBar.js` registriert beide Listener in der Capture-Phase |
| `RezepteAppFixture` (Hilfsklasse) | `Rezepte.Tests.Browser/Infrastructure` | Startet die Anwendung mit temporärer Datenbank und konfigurierbaren `LoadingBar__*`-Umgebungsvariablen, seedet einen Testbenutzer |
| `PlaywrightBrowserFixture` (Hilfsklasse) | `Rezepte.Tests.Browser/Infrastructure` | Stellt Chromium bereit und meldet fehlende Browser-Binaries für den Skip-Pfad |
| `LoadingBarPageObject` (Hilfsklasse) | `Rezepte.Tests.Browser/Infrastructure` | Anmeldung, Zugriff auf Aktiv-Zustand, Farbe und Opazität des Balkens, künstliche Verzögerung der nächsten Navigation |

### Betroffene bestehende Tests

| Test / Testklasse | Grund der Anpassung |
|-------------------|---------------------|
| — | Keine. Es werden ausschließlich neue Dateien angelegt; bestehende Signaturen und Datenstrukturen bleiben unverändert. Berührt werden nur `Rezepte.Tests.csproj` (nur im Zweig 14a, durch eine zusätzliche `PackageReference`) und `Rezepte.sln` (neuer Projekteintrag) — beides ohne Auswirkung auf bestehendes Testverhalten. |

### E2E-Tests (Pflicht)

Die E2E-Abdeckung erfolgt in zwei Stufen: echte Browsertests in `Rezepte.Tests.Browser` prüfen das Laufzeitverhalten, die dateibasierten `LoadingBarWiringTests` in `Rezepte.Tests` sichern die Verdrahtung schnell und ohne Browserabhängigkeit ab (siehe Tabelle „Neue Tests").

| Szenario | Testdatei / Testklasse | Abgedecktes Akzeptanzkriterium |
|----------|------------------------|-------------------------------|
| Happy Path: Klick auf einen Navigationslink zeigt bei verzögerter Antwort einen sichtbaren, animierten Balken unterhalb der Menüleiste | `Rezepte.Tests.Browser/LoadingBarVisibilityBrowserTests.cs` — `LinkClick_WithDelayedResponse_ShowsAnimatedBarBelowNavigation` | Ladebalken erscheint beim Linkklick am unteren Rand der Menüleiste |
| Happy Path: Balken verschwindet nach Abschluss der Navigation | `LoadingBarVisibilityBrowserTests` — `AfterNavigationCompleted_HidesLoadingBar` | Ladebalken verschwindet nach abgeschlossener Navigation |
| Happy Path: Der Balken zeigt einen Farbverlauf in einer Farbe aus der konfigurierten Liste | `Rezepte.Tests.Browser/LoadingBarColorBrowserTests.cs` — `LinkClick_UsesColorFromConfiguredPalette` | Farbe stammt aus der konfigurierten Palette |
| Happy Path: Erneuter Klick während laufender Animation wechselt die Farbe | `LoadingBarColorBrowserTests` — `SecondClickDuringRunningAnimation_ChangesColor` | Bei jeder Navigationsinteraktion wird eine neue Zufallsfarbe verwendet, Interaktion wird sichtbar quittiert |
| Happy Path: Absenden der Suchleiste zeigt den Balken | `Rezepte.Tests.Browser/LoadingBarFormNavigationBrowserTests.cs` — `SearchSubmit_ShowsLoadingBar` | Formularbasierte Navigation verhält sich konsistent zum Linkklick |
| Fehlerfall: Balken bleibt bei ausbleibendem Abschlusssignal nicht dauerhaft stehen | `Rezepte.Tests.Browser/LoadingBarSafetyTimeoutBrowserTests.cs` — `WhenNavigationNeverCompletes_HidesBarAfterMaxVisibleDuration` | Ladebalken bleibt bei sehr langsamen oder abgebrochenen Requests nicht dauerhaft stehen |
| Fehlerfall: Feature global deaktiviert | `Rezepte.Tests.Browser/LoadingBarDisabledBrowserTests.cs` — `WhenFeatureDisabled_PageContainsNoLoadingBarElement` | `Enabled: false` schaltet das Feature vollständig ab |
| Fehlerfall: fehlerhafte Konfiguration bricht das Layout nicht | `Rezepte.Tests/Services/LoadingBarServiceTests_Validation.cs` — `GetSettings_WithInvalidColorEntries_RemovesInvalidEntries` und die übrigen Validierungstests | Fehlkonfiguration führt nicht zu einem Fehler in der Anwendung |

Welche bestehenden E2E-Tests müssen angepasst werden?

Keine.

## Offene Punkte

Keine.
