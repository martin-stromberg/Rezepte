# Services (fehlend)

Weder `ILoadingBarService` noch `LoadingBarService` existieren derzeit im Projekt.

## Erwartete Service-Struktur

Basierend auf der Anforderung und den etablierten Mustern im Projekt sollten folgende Interfaces und Klassen erstellt werden:

### Interface: `ILoadingBarService`

Datei: `Rezepte.Web/Services/ILoadingBarService.cs` (zu erstellen)

Erwartete Methoden:

| Methode | Parameter | Rückgabewert | Zweck |
|---------|-----------|--------------|-------|
| `ShowAsync()` | (keine) | `Task` | Zeigt die Animation an und wählt eine zufällige Farbe aus der Liste |
| `HideAsync()` | (keine) | `Task` | Verbirgt die Animation nach erfolgreicher Navigation |

### Implementierung: `LoadingBarService`

Datei: `Rezepte.Web/Services/LoadingBarService.cs` (zu erstellen)

Erwartete Verantwortlichkeiten:

- Verwaltung des Animationszustands (sichtbar/verborgen)
- Auswahl einer zufälligen Farbe aus der konfigurierten Liste
- Verwaltung von Timeouts und Verzögerungen
- Zugriff auf `LoadingBarOptions` über `IOptions<LoadingBarOptions>`

## Service-Registrierung

Die Registrierung sollte in `Rezepte.Web/Extensions/ServiceCollectionExtensions.cs` erfolgen:

```csharp
// Nach Zeile 31:
services.Configure<LoadingBarOptions>(configuration.GetSection("LoadingBar"));

// Nach Zeile 161 (mit anderen Application Services):
services.AddScoped<ILoadingBarService, LoadingBarService>();
```

Das Service sollte als **Scoped Service** registriert werden (pro Benutzer/Session), wie andere UI-bezogene Services im Projekt.

## Entwurfsmuster im Projekt

Bestehende Services folgen diesem Pattern:

- Interface-Definition in `Rezepte.Web/Services/I*.cs`
- Implementierung in `Rezepte.Web/Services/*.cs`
- Registrierung in `ServiceCollectionExtensions.cs` als Scoped oder Singleton
- Konstruktor-Injektion in Komponenten oder anderen Services

Beispiele:
- `IUserService` / `UserService`
- `ICookbookService` / `CookbookService`
- `IRecipeService` / `RecipeService`
- `ICalendarService` / `CalendarService`
