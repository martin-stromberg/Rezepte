using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Rezepte.Web.Controllers;

/// <summary>
/// Base class for API controllers that resolve the authenticated user from the claims principal.
/// </summary>
public abstract class ApiControllerBase : ControllerBase
{
    /// <summary>
    /// Gets the user id.
    /// </summary>
    /// <returns>The result.</returns>
    protected string? GetUserId() => User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

    /// <summary>
    /// Gets the user id.
    /// </summary>
    /// <returns>The result.</returns>
    protected string CurrentUserId => GetUserId() ?? string.Empty;

    /// <summary>
    /// Tries to get user id.
    /// </summary>
    /// <param name="userId">The user id parameter.</param>
    /// <returns>The result.</returns>
    protected bool TryGetUserId(out string userId)
    {
        var value = GetUserId();
        userId = value ?? string.Empty;
        return !string.IsNullOrEmpty(value);
    }
}
