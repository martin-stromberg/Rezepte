# Konfiguration (fehlend)

## appsettings.json

Datei: `Rezepte.Web/appsettings.json`

Die `appsettings.json` enthält **keine** Konfiguration für die Ladeanimation. Derzeit sind nur folgende Konfigurationen vorhanden:

| Sektion | Inhalt |
|---------|--------|
| `Logging` | LogLevel-Einstellungen für Default und AspNetCore |
| `AllowedHosts` | "*" (alle Hosts erlaubt) |
| `Jwt` | JWT-Authentifizierung (Key, Issuer, Audience, LifetimeMinutes) |
| `Images` | Bildverarbeitung (MaxSizeBytes, CacheMaxAgeSeconds, AllowedContentTypes) |
| `PluginUpdates` | Plugin-Update-Einstellungen (GitHubApiBaseUrl, TimeoutSeconds, UserAgent) |
| `GoogleCredentials` | Google-Authentifizierung (ServiceAccountFilePath, GeminiApiKey) |

## Fehlende LoadingBar-Konfiguration

Die Anforderung definiert folgende Konfigurationsoptionen:

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

Diese müsste in `appsettings.json` hinzugefügt werden.

## Options-Pattern (.NET)

Der bestehende Code in `Rezepte.Web/Extensions/ServiceCollectionExtensions.cs` zeigt ein etabliertes Pattern für Konfiguration-Binding:

```csharp
services.Configure<ImageOptions>(configuration.GetSection("Images"));
services.Configure<AIOptions>(configuration.GetSection("AI"));
services.Configure<PluginUpdateOptions>(configuration.GetSection("PluginUpdates"));
services.Configure<GoogleCredentialsOptions>(configuration.GetSection("GoogleCredentials"));
```

Eine `LoadingBarOptions`-Klasse würde diesem Pattern folgen und ähnlich registriert werden:

```csharp
services.Configure<LoadingBarOptions>(configuration.GetSection("LoadingBar"));
```

## Existierende Options-Klassen

Beispiele im Projekt:
- `Rezepte.Web/Configuration/ImageOptions.cs`
- `Rezepte.Web/Configuration/AIOptions.cs`
- `Rezepte.Web/Configuration/PluginUpdateOptions.cs`
- `Rezepte.Web/Configuration/GoogleCredentialsOptions.cs`

Eine `LoadingBarOptions`-Klasse sollte in `Rezepte.Web/Configuration/` erstellt werden.
