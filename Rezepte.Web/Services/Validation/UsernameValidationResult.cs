namespace Rezepte.Web.Services.Validation;

public sealed record UsernameValidationResult(bool IsValid, string? ErrorMessage)
{
    public static UsernameValidationResult Valid { get; } = new(true, null);

    public static UsernameValidationResult Invalid(string errorMessage) => new(false, errorMessage);
}
