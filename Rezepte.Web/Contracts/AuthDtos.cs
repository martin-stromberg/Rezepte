using System.ComponentModel.DataAnnotations;

namespace Rezepte.Web.Contracts;

public record RegisterRequest(
    [param: EmailAddress] string? Email,
    [param: Required, MinLength(3)] string Username,
    [param: Required, MinLength(6)] string Password
);

public record LoginRequest(
    [param: Required, MinLength(3)] string Username,
    [param: Required] string Password
);

public record AuthResponse(
    string UserId,
    string Username,
    string? Email
);
