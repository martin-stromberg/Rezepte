# Interfaces

## `Rezepte.Web.Services.ITokenService`
- Datei: `Rezepte.Web/Services/TokenService.cs`

| Methode | Parameter | Rückgabewert | Zweck |
|---------|-----------|--------------|-------|
| `CreateToken` | `string userId`, `string username`, `bool isAdmin` | `string` | JWT erzeugen und cachen. |
| `GetToken` | `string userId` | `string?` | Gecachten JWT abrufen. |
