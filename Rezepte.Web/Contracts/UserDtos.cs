using System.ComponentModel.DataAnnotations;

namespace Rezepte.Web.Contracts;

public record UserProfileDto(
    string Id,
    string Username,
    string? Email
);

public record UpdateProfileRequest(
    [param: Required, MinLength(3)] string Username,
    [param: EmailAddress] string? Email
);

public record ChangePasswordRequest(
    [param: Required] string CurrentPassword,
    [param: Required, MinLength(6)] string NewPassword
);