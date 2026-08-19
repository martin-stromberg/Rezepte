# Ladeanimation — Konfiguration für Administratoren

Die Ladeanimation ist ein visuelles Feedback-Element, das während der Navigation angezeigt wird. Sie kann vollständig deaktiviert oder über `appsettings.json` angepasst werden.

## Konfigurationsabschnitt

Alle Einstellungen befinden sich unter dem Abschnitt `LoadingBar` in der Datei `appsettings.json` oder der umgebungsspezifischen Konfiguration (z. B. `appsettings.Development.json`). Die folgenden Parameter sind verfügbar:

| Parameter | Typ | Standardwert | Beschreibung |
|-----------|-----|--------------|--------------|
| `LoadingBar:Enabled` | `bool` | `true` | Deaktiviert die Ladeanimation komplett, wenn auf `false` gesetzt. Bei Deaktivierung wird kein HTML-Markup für die Komponente gerendert. |
| `LoadingBar:Height` | `string` | `"3px"` | Hoehe des Ladebalkens. Muss eine CSS-Laenge sein: `{Zahl}px`, `{Zahl}rem` oder `{Zahl}em` (z. B. `"4px"`, `"0.25rem"`). Ungültige Werte fallen auf den Standardwert zurück. |
| `LoadingBar:AnimationDuration` | `string` | `"2s"` | Dauer eines kompletten Animationssweeps von rechts nach links. Muss eine CSS-Zeit sein: `{Zahl}ms` oder `{Zahl}s` (z. B. `"2s"`, `"1500ms"`). Ungültige Werte fallen auf den Standardwert zurück. |
| `LoadingBar:HideDelay` | `string` | `"300ms"` | Verzoegerung nach dem Abschluss der Navigation, bis der Ladebalken ausgeblendet wird. Muss eine CSS-Zeit sein (z. B. `"300ms"`, `"0.5s"`). Ungültige Werte fallen auf den Standardwert zurück. |
| `LoadingBar:MaxVisibleDuration` | `string` | `"15s"` | Maximale Sichtbarkeitsdauer — ein Sicherheits-Timeout. Falls die Navigation nicht abgeschlossen wird oder kein Abschlusssignal sendet, wird der Balken nach dieser Zeit automatisch ausgeblendet. Muss groesser als `HideDelay` sein. Muss eine CSS-Zeit sein (z. B. `"15s"`). Ungültige oder zu kleine Werte fallen auf den Standardwert zurück. |
| `LoadingBar:Colors` | `string[]` | `["#FF6B6B", "#4ECDC4", "#45B7D1", "#96CEB4", "#FFEAA7", "#DDA0DD"]` | Liste der Farben, aus denen bei jeder Navigation eine zufällig gewählt wird. Alle Eintraege müssen gültige Hex-Farbwerte sein (`#RGB` oder `#RRGGBB`). Ungültige Eintraege werden entfernt; falls die Liste leer wird, wird die Standardfarbliste verwendet. |

## Beispiele

### Ladeanimation deaktivieren

```json
{
  "LoadingBar": {
    "Enabled": false
  }
}
```

### Animationsdauer verlaengern

```json
{
  "LoadingBar": {
    "AnimationDuration": "3s"
  }
}
```

### Benutzerdefinierte Farbliste

```json
{
  "LoadingBar": {
    "Colors": [ "#0066cc", "#00cc66", "#cc6600", "#cc0066" ]
  }
}
```

### Groessere Hoehe für bessere Sichtbarkeit

```json
{
  "LoadingBar": {
    "Height": "5px"
  }
}
```

## Validierung und Fehlerbehandlung

Alle Konfigurationsparameter werden beim ersten Rendern der Ladeanimation (verzögerte Auswertung) validiert. Bei ungültigen Werten:

1. Der ungültige Wert wird **nicht** zur Anwendung gefuehrt
2. Ein Standardwert wird verwendet
3. Eine Warnung wird in das Anwendungsprotokoll geschrieben (Log-Level: **Warning**)
4. Die Anwendung wird **nicht** unterbrochen — selbst ein komplett fehlerhafter `LoadingBar`-Abschnitt verhindert nicht das Hochfahren der Anwendung

Dies ist beabsichtigt, da die Ladeanimation rein kosmetisch ist und ein Konfigurationsfehler nicht die Funktionalitaet der Anwendung beeintraechtigen darf.

### Beispiel: Ungültige CSS-Laenge

Eingabe: `"Height": "3"` (fehlende Einheit)  
Protokoll: `Invalid LoadingBar:Height value '3'. Falling back to default '3px'.`  
Ergebnis: Der Balken wird mit der Standardhoehe `3px` gerendert

### Beispiel: Ungültige Farbe

Eingabe: `"Colors": [ "#FF6B6B", "rot", "#4ECDC4" ]`  
Protokoll: `Invalid LoadingBar:Colors entry 'rot'. Removing it from the color list.`  
Ergebnis: Nur die beiden gültigen Farben werden verwendet; `"rot"` wird ignoriert

## Umgebungsvariablen

Die `appsettings.json`-Werte können auch über Umgebungsvariablen überschrieben werden. Das ist besonders nuetzlich in Produktionsumgebungen oder in Containern.

Umgebungsvariablenformat: `LoadingBar__{ParameterName}` mit doppeltem Unterstrich als Trennzeichen.

Beispiele:

```bash
# Deaktivieren
export LoadingBar__Enabled=false

# Farben überschreiben
export LoadingBar__Colors__0="#000000"
export LoadingBar__Colors__1="#FFFFFF"

# Hoehe vergroessern
export LoadingBar__Height="6px"
```

## Barrierefreiheit

Die Ladeanimation wird automatisch von Screenreadern ausgeblendet. Sie respektiert die Benutzereinstellung `prefers-reduced-motion: reduce` — statt einer kontinuierlichen Bewegung erscheint in diesem Fall ein statischer, farbiger Balken.

Diese Einstellung kann vom Benutzer nicht deaktiviert werden; sie ist beabsichtigt, um eine zugaengliche Erfahrung zu gewährleisten.

## Problembehandlung

### Der Ladebalken wird überhaupt nicht angezeigt

1. Prüfen Sie, dass `LoadingBar:Enabled` nicht auf `false` gesetzt ist
2. Prüfen Sie die Anwendungsprotokolle auf Validierungswarnungen
3. Überprüfen Sie, dass der Browser JavaScript aktiviert hat

### Der Ladebalken ist sehr hell oder sehr dunkel

Dies kann bei benutzerdefinierten Farben vorkommen, besonders auf dem dunklen Navigationsleisten-Hintergrund. Wählen Sie Farben mit ausreichendem Kontrast aus.

Farbkontrast-Tipps:
- Verwenden Sie helle Farben auf dunklem Hintergrund
- Testen Sie mit einem Kontrast-Checker, z. B. https://webaim.org/resources/contrastchecker/

### Die Animation läuft nicht

Falls `prefers-reduced-motion: reduce` auf dem Benutzergeraet aktiviert ist, wird nur ein statischer farbiger Balken angezeigt statt einer Animation. Dies ist beabsichtigt und respektiert die Benutzer-Praeferenzen.
