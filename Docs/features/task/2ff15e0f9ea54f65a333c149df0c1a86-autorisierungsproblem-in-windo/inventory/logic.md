# Logik

## `Rezepte.Web.Services.TokenService`
- Datei: `Rezepte.Web/Services/TokenService.cs`

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `CreateToken(string userId, string username, bool isAdmin)` | public | Erzeugt einen JWT für die Benutzer-ID und speichert ihn 8 Stunden im Memory-Cache. |
| `GetToken(string userId)` | public | Liefert den gecachten JWT oder `null`. |

## `Rezepte.Web.Services.ApiAuthHandler`
- Datei: `Rezepte.Web/Services/ApiAuthHandler.cs`

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `SendAsync(HttpRequestMessage, CancellationToken)` | protected override | Setzt `Authorization: Bearer <token>`, wenn `IHttpContextAccessor.HttpContext.User` einen `NameIdentifier`-Claim liefert und ein Token im Cache vorhanden ist. |

## `Rezepte.Web.ApiAuthHandler` (root-Namespace)
- Datei: `Rezepte.Web/ApiAuthHandler.cs`

Duplikat zu `Rezepte.Web.Services.ApiAuthHandler`. Unterscheidet sich nur leicht in der Logik (erzeugt Token bei Fehlen, statt nur im Cache nachzuschlagen). Wird aus `ServiceCollectionExtensions` mit `using Rezepte.Web.Services;` nicht referenziert.

## `Rezepte.Web.Services.AntiForgeryHandler`
- Datei: `Rezepte.Web/Services/AntiForgeryHandler.cs`

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `SendAsync(HttpRequestMessage, CancellationToken)` | protected override | Fügt bei mutierenden Aufrufen den `RequestVerificationToken` aus dem Cookie hinzu. Fällt im Circuit still aus, da kein `HttpContext`. |

## `Rezepte.Web.Services.ApiClient`
- Datei: `Rezepte.Web/Services/ApiClient.cs`

| Eigenschaft/Methode | Sichtbarkeit | Kurzbeschreibung |
|--------------------|-------------|------------------|
| `Http` | public | `HttpClient`, der in ViewModels/Komponenten verwendet wird. |
| `GetAsync`, `GetAsync<T>`, `PostAsJsonAsync`, `PutAsJsonAsync` | public | Wrapper um `HttpClient`. |

## `Rezepte.Web.Extensions.ServiceCollectionExtensions`
- Datei: `Rezepte.Web/Extensions/ServiceCollectionExtensions.cs`

Registriert Authentifizierung (Cookie + JWT), `IHttpContextAccessor`, `ApiAuthHandler`, `AntiForgeryHandler` und den typed `ApiClient` über `IHttpClientFactory`.

## `Rezepte.Web.ViewModels.UserProfileViewModel`
- Datei: `Rezepte.Web/ViewModels/UserProfileViewModel.cs`

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `LoadAsync` | public | Ruft `api/users/me` ab. |
| `SaveProfileAsync` | public | Ruft `api/users/me` (PUT) auf. |
| `ChangePasswordAsync` | public | Ruft `api/users/me/change-password` auf. |
