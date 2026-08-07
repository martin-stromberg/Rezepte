using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Rezepte.Web.Controllers;

/// <summary>
/// Base class for API controllers that resolve the authenticated user from the claims principal.
/// </summary>
public abstract class ApiControllerBase : ControllerBase
{
    protected string? GetUserId() => User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

    protected string CurrentUserId => GetUserId() ?? string.Empty;

    protected bool TryGetUserId(out string userId)
    {
        var value = GetUserId();
        userId = value ?? string.Empty;
        return !string.IsNullOrEmpty(value);
    }
}
