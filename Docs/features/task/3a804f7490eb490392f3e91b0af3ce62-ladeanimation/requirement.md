# Übersetzte Anforderung: Ladeanimation

## Fachliche Zusammenfassung

Es wird eine **Ladeanimation-Komponente** implementiert, die dem Benutzer visuelles Feedback beim Navigieren zwischen Seiten bietet. Wenn der Benutzer auf einen Link klickt, wird eine schmale, horizontale Ladebalken am unteren Rand der Menüleiste angezeigt. Diese zeigt eine farblich animierte Bewegung von rechts nach links und nutzt bei jedem Navigationsklick eine zufällig gewählte Farbe, um dem Benutzer zu signalisieren, dass seine Interaktion erkannt wurde. Dies verbessert die UX auf langsamen Servern, wo Navigationsverzögerungen auftreten können.

## Betroffene Klassen und Komponenten

### UI-Komponenten
- **`LoadingBar.razor`** (neu): Blazor-Komponente für die Ladeanimation
- **`LoadingBar.razor.css`** (neu): Scoped Styling und CSS-Animationen
- **`MainLayout.razor`** (angepasst): Integration der `LoadingBar`-Komponente unter der Navbar

### Services / Logik
- **`ILoadingBarService`** (optional): Service zur Verwaltung von Animationszustand, Farbauswahl und Sichtbarkeit
  - `ShowAsync()`: Zeigt die Animation an und wählt eine zufällige Farbe
  - `HideAsync()`: Verbirgt die Animation nach erfolgreicher Navigation
  - Abhängigkeitsinjektion als Scoped Service

### Tests
- Unit-Tests für `LoadingBarService` (bei Implementierung eines Services)
- ggf. Integrationstests für Navigations-Binding

## Implementierungsansatz

### Navigation Triggern
1. Die `LoadingBar`-Komponente wird direkt unter dem `<nav>`-Element in `MainLayout.razor` positioniert
2. Bindung an das **`NavigationManager.LocationChanged`-Event** in Blazor
   - Beim Auslösen zeigt die Animation an
   - Nach Abschluss (z. B. nach einem konfigurierbaren Timeout oder beim Erreichen des nächsten NavigationChanged-Events) wird sie ausgeblendet
3. Alternative: Reaktion auf `NavLink`-Klicks mittels `@onclick` Binding für präzisere Kontrolle

### Farbwahl
- Eine Liste vordefinierter **Farben** (z. B. `["#FF6B6B", "#4ECDC4", "#45B7D1", ...]`) wird verwahrt
- Bei jedem `ShowAsync()`-Aufruf wird eine **zufällige Farbe** aus der Liste gewählt
- Die Farbe wird als **CSS-Variable** oder **inline style** an die Komponente übergeben

### CSS-Animation
- Schmal Leiste (`height` z. B. 3–4px, `width: 100%`)
- Animation: **`transform: translateX()`** von rechts nach links (z. B. `translateX(100%)` → `translateX(-100%)`)
- Bewegungsrichtung: rechts (off-screen) → links (off-screen)
- Timing: z. B. über `animation` oder `@keyframes` mit linearer Easing und konfigurierbarer Dauer
- Shadow/Glow optional zur Subtilität

### Event Handling
- `LocationChanged` abonnieren in `OnInitialized` oder `OnInitializedAsync`
- Cleanup: `LocationChanged` in `Dispose` abmelden (IAsyncDisposable)

## Konfiguration

### Applikationseinstellungen
Folgende Werte sollten optional in `appsettings.json` konfigurierbar sein:

```json
{
  "LoadingBar": {
    "Enabled": true,
    "Height": "3px",
    "AnimationDuration": "2s",
    "Colors": ["#FF6B6B", "#4ECDC4", "#45B7D1", "#96CEB4", "#FFEAA7", "#DDA0DD"],
    "HideDelay": "300ms"
  }
}
```

- **`Enabled`**: Feature kann global de-aktiviert werden
- **`Height`**: Dicke der Ladebalken
- **`AnimationDuration`**: Dauer einer kompletten Animation (rechts → links)
- **`Colors`**: Liste der zufällig wählbaren Farben
- **`HideDelay`**: Zeit nach der Navigation, bis die Ladebalken ausgeblendet wird

### Alternatives Pattern: Options-Pattern (.NET)
```csharp
public class LoadingBarOptions
{
    public bool Enabled { get; set; } = true;
    public string Height { get; set; } = "3px";
    public string AnimationDuration { get; set; } = "2s";
    public List<string> Colors { get; set; } = new() { /* defaults */ };
    public string HideDelay { get; set; } = "300ms";
}
```

Registrierung in `Program.cs`:
```csharp
services.Configure<LoadingBarOptions>(configuration.GetSection("LoadingBar"));
```

## Offene Fragen

1. **Sichtbarkeitsdauer**: Soll die Ladebalken automatisch nach einer definierten Dauer (z. B. 5 Sekunden) verschwinden, oder nur beim Erreichen der neuen Seite? Was ist das Verhalten bei sehr langsamen Requests?

2. **Mehrfache Navigation**: Wenn der Benutzer schnell hintereinander mehrere Links klickt, soll:
   - Die Animation neu gestartet werden (mit neuer Farbe)?
   - Eine neue Farbe hinzugefügt werden (z. B. mehrere Balken)?
   - Oder die laufende Animation ignoriert werden?

3. **Ladeabschluss-Erkennung**: Wie wird erkannt, dass die Seite vollständig geladen ist?
   - Via `LocationChanged`-Event (einfach, aber möglicherweise kurz nach Anfang)?
   - Via HTTP-Request-Counter (komplexer, aber präziser)?
   - Via benutzerdefinierter Cascade Parameter?

4. **Responsive Design**: Soll die Balken auf mobilen Geräten anders aussehen oder bleiben (z. B. größere Höhe, andere Animationsdauer)?

5. **Accessibility**: Sollte eine Aria-Label oder Announcement für Screen-Reader ergänzt werden?

6. **Farbliste**: Sollen Farben von einer zentralen Farbpalette des Projekts stammen (falls vorhanden) oder hart-kodiert sein?

7. **Performance**: Sollte die Komponente nur rendern, wenn sichtbar (z. B. `display: none` vs. `Dispose` bei `Enabled: false`)?

## Notizen

- **Abhängigkeit zu `NavigationManager`**: Wird in Blazor-Komponenten automatisch injiziert
- **CSS-Scoping**: Isolation via `.razor.css`-Datei verhindert Interferenzen mit anderen Styles
- **Dependency Injection**: Falls ein Service verwendet wird, sollte dieser als **Scoped** registriert werden (pro Benutzer/Session)
- **Testing**: Mocking von `NavigationManager.LocationChanged` und `RandomColorSelection` in Unit-Tests
