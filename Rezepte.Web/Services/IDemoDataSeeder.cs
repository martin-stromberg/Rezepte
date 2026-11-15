namespace Rezepte.Web.Services;

/// <summary>
/// Defines the entry point for the business operation "seed demo data for a user".
/// </summary>
public interface IDemoDataSeeder
{
    /// <summary>
    /// Creates the fixed demo data set (cookbooks, recipes, calendar events and shopping list
    /// items) for the given user.
    /// </summary>
    /// <param name="userId">The id of the user that owns the created demo data.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task SeedForUserAsync(string userId, CancellationToken ct);
}
