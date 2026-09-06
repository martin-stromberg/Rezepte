using System.ComponentModel.DataAnnotations;

namespace Rezepte.Web.Contracts;

/// <summary>
/// Registers the request.
/// </summary>
/// <param name="Email">The email parameter.</param>
/// <param name="Username">The username parameter.</param>
/// <param name="Password">The password parameter.</param>
/// <returns>The result.</returns>
public record RegisterRequest(
    [param: EmailAddress] string? Email,
    [param: Required] string Username,
    [param: Required, MinLength(6)] string Password
);

/// <summary>
/// logins the request.
/// </summary>
/// <param name="Username">The username parameter.</param>
/// <param name="Password">The password parameter.</param>
/// <returns>The result.</returns>
public record LoginRequest(
    [param: Required, MinLength(3)] string Username,
    [param: Required] string Password
);

/// <summary>
/// auths the response.
/// </summary>
/// <param name="UserId">The user id parameter.</param>
/// <param name="Username">The username parameter.</param>
/// <param name="Email">The email parameter.</param>
/// <returns>The result.</returns>
public record AuthResponse(
    string UserId,
    string Username,
    string? Email
);
