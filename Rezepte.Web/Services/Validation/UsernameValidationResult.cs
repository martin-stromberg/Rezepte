namespace Rezepte.Web.Services.Validation;

/// <summary>
/// usernames the validation result.
/// </summary>
/// <param name="IsValid">The is valid parameter.</param>
/// <param name="ErrorMessage">The error message parameter.</param>
/// <returns>The result.</returns>
public sealed record UsernameValidationResult(bool IsValid, string? ErrorMessage)
{
    /// <summary>
    /// Represents the public class.
    /// </summary>
    /// <param>...</param>
    /// <param>...</param>
    /// <param>...</param>
    /// <returns>The result. null: The null parameter. true: The true parameter.</returns>
    public static UsernameValidationResult Valid { get; } = new(true, null);

    /// <summary>
    /// invalids the value.
    /// </summary>
    /// <param name="errorMessage">The error message parameter.</param>
    /// <returns>The result.</returns>
    public static UsernameValidationResult Invalid(string errorMessage) => new(false, errorMessage);
}
