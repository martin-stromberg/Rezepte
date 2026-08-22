namespace Rezepte.Web.Security;

/// <summary>
/// Names of the configured rate limiting policies.
/// </summary>
public static class RateLimitPolicies
{
    /// <summary>Policy applied to login and registration endpoints.</summary>
    public const string Authentication = "authentication";
}
