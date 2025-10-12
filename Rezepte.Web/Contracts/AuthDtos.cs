using System.ComponentModel.DataAnnotations;

namespace Rezepte.Web.Contracts;

public record RegisterRequest(
    [parameter: EmailAddress] string? Email,
    [parameter: Required, MinLength(3)] string Username,
    [parameter: Required, MinLength(6)] string Password
);

public record LoginRequest(
    [parameter: Required, MinLength(3)] string Username,
    [parameter: Required] string Password
);

public record AuthResponse(
    string UserId,
    string Username,
    string? Email
);
