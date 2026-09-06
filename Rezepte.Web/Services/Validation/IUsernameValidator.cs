namespace Rezepte.Web.Services.Validation;

/// <summary>
/// Defines the iusername validator interface.
/// </summary>
public interface IUsernameValidator
{
    /// <summary>
    /// Validates the value.
    /// </summary>
    /// <param name="username">The username parameter.</param>
    /// <returns>The result.</returns>
    UsernameValidationResult Validate(string? username);
}
