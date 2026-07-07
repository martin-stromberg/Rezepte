namespace Rezepte.Web.Services.Validation;

public interface IUsernameValidator
{
    UsernameValidationResult Validate(string? username);
}
