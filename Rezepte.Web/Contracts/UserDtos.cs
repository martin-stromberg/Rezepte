using System.ComponentModel.DataAnnotations;

namespace Rezepte.Web.Contracts;

/// <summary>
/// users the profile dto.
/// </summary>
/// <param name="Id">The id parameter.</param>
/// <param name="Username">The username parameter.</param>
/// <param name="Email">The email parameter.</param>
/// <returns>The result.</returns>
public record UserProfileDto(
    string Id,
    string Username,
    string? Email
);

/// <summary>
/// Updates the profile request.
/// </summary>
/// <param name="Username">The username parameter.</param>
/// <param name="Email">The email parameter.</param>
/// <returns>The result.</returns>
public record UpdateProfileRequest(
    /// <summary>
    /// Represents the string class.
    /// </summary>
    [param: Required] string Username,
    [param: EmailAddress] string? Email
);

/// <summary>
/// changes the password request.
/// </summary>
/// <param name="CurrentPassword">The current password parameter.</param>
/// <param name="NewPassword">The new password parameter.</param>
/// <returns>The result.</returns>
public record ChangePasswordRequest(
    /// <summary>
    /// Represents the string class.
    /// </summary>
    [param: Required] string CurrentPassword,
    [param: Required, MinLength(6)] string NewPassword
);
