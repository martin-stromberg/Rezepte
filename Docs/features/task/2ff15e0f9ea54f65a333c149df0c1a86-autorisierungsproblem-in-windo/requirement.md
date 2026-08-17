# Anforderung: Autorisierungsproblem in Windows

## Fachliche Zusammenfassung

Die Blazor-Server-Anwendung läuft unter Windows im IIS. Interaktive Komponenten mit Server-Render-Modus werden zweimal initialisiert: einmal während des Server-Prerenderings (mit `HttpContext`) und anschließend im Blazor-Circuit (ohne `HttpContext`). Der aktuelle `ApiAuthHandler` holt den JWT-Bearer-Token ausschließlich aus `IHttpContextAccessor.HttpContext.User`. Im Circuit steht kein `HttpContext` mehr zur Verfügung, daher fehlt der `Authorization`-Header bei API-Aufrufen, und der Server antwortet mit `401 Unauthorized`. Ziel ist es, die Autorisierung so auszubauen, dass API-Aufrufe aus interaktiven Blazor-Komponenten sowohl während des Prerenderings als auch im Circuit korrekt authentifiziert werden.

## Betroffene Klassen und Komponenten

- `Rezepte.Web.Services.ApiAuthHandler` – aktuell nur `IHttpContextAccessor`-basiert
- `Rezepte.Web.ApiClient` – typed `HttpClient`, der den `Authorization`-Header benötigt
- `Rezepte.Web.Services.ITokenService` / `TokenService` – Erzeugung und Cache der JWT-Tokens
- `Rezepte.Web.Extensions.ServiceCollectionExtensions` – DI-Registrierung von `ApiClient`, `ApiAuthHandler` und `AntiForgeryHandler`
- `Microsoft.AspNetCore.Components.Authorization.AuthenticationStateProvider` – liefert den angemeldeten `ClaimsPrincipal` im Circuit
- `Microsoft.AspNetCore.Http.IHttpContextAccessor` – liefert den `ClaimsPrincipal` während des Prerenderings
- Alle interaktiven Server-Komponenten, die `ApiClient` für Aufrufe an `api/*` verwenden, z. B.:
  - `Rezepte.Web.Components.Settings.UserProfile`
  - `Rezepte.Web.Components.Settings.AiSettings`
  - `Rezepte.Web.Components.Settings.ExportData`
  - `Rezepte.Web.Components.Pages.*` (viele Seitenkomponenten)
  - `Rezepte.Web.Components.Shared.*` (Dialoge, Overlays)

## Implementierungsansatz

Anstatt den `Authorization`-Header nur aus `IHttpContextAccessor` zu beziehen, wird ein zusätzlicher, aus dem Circuit-Scope auflösbarer `DelegatingHandler` eingeführt. Dieser Handler liest den `ClaimsPrincipal` über `AuthenticationStateProvider` und ergänzt den JWT-Token aus `ITokenService` für jede ausgehende Anfrage. Die `ApiClient`-Registrierung wird von einem typed `IHttpClientFactory`-Client auf einen scoped Dienst umgestellt, damit der neue Handler im aktuellen DI-Scope (Circuit/Request) erzeugt werden kann. Dadurch ist kein `HttpContext` mehr nötig, und alle Aufrufe über `ApiClient` werden automatisch autorisiert.

## Konfiguration

Keine neuen Konfigurationseinträge erforderlich.

## Offene Fragen

Keine offenen Fragen – die technische Ursache ist aus dem Log und der bestehenden Architektur ableitbar.
