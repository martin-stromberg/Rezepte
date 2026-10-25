using System.Text.Json;
using Microsoft.Playwright;

namespace Rezepte.Tests.Browser.Infrastructure;

/// <summary>
/// Seeds recipes for the currently logged-in browser test user through the real application API:
/// a session token is fetched via the page's cookie session and each recipe is created with a
/// bearer-authenticated <c>POST api/recipes</c> call.
/// </summary>
public static class RecipeSeedHelper
{
    /// <summary>
    /// Creates one recipe per supplied title for the user that is logged in on the given page.
    /// </summary>
    /// <param name="page">The logged-in page; its browser context cookies authenticate the API calls.</param>
    /// <param name="baseAddress">The base address of the application under test.</param>
    /// <param name="titles">The recipe titles to create.</param>
    /// <returns>A task that represents the asynchronous seed operation.</returns>
    public static async Task SeedRecipesAsync(IPage page, string baseAddress, params string[] titles)
    {
        var apiRequest = page.Context.APIRequest;

        var tokenResponse = await apiRequest.GetAsync($"{baseAddress}/api/session/token");
        if (!tokenResponse.Ok)
        {
            throw new InvalidOperationException(
                $"Fetching the session token failed with status {tokenResponse.Status}: {await tokenResponse.TextAsync()}");
        }

        using var tokenDocument = JsonDocument.Parse(await tokenResponse.TextAsync());
        var token = tokenDocument.RootElement.GetProperty("token").GetString()
            ?? throw new InvalidOperationException("The session token response did not contain a token.");

        foreach (var title in titles)
        {
            var response = await apiRequest.PostAsync($"{baseAddress}/api/recipes", new APIRequestContextOptions
            {
                Headers = new Dictionary<string, string>
                {
                    ["Authorization"] = $"Bearer {token}"
                },
                DataObject = new
                {
                    cookbookId = (string?)null,
                    title,
                    description = $"Browser test recipe '{title}'.",
                    uri = (string?)null,
                    portions = (int?)4,
                    steps = Array.Empty<object>(),
                    sideDishRecipeIds = (List<string>?)null
                }
            });

            if (!response.Ok)
            {
                throw new InvalidOperationException(
                    $"Creating the recipe '{title}' failed with status {response.Status}: {await response.TextAsync()}");
            }
        }
    }
}
