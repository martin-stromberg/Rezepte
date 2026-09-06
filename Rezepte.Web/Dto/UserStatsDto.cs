namespace Rezepte.Web.Dto;

/// <summary>
/// users the stats dto.
/// </summary>
/// <param>...</param>
/// <param>...</param>
/// <param>...</param>
/// <param name="TimeSinceRegistration">The time since registration parameter.</param>
/// <param name="CookbookCount">The cookbook count parameter.</param>
/// <param name="OwnRecipeCount">The own recipe count parameter.</param>
/// <param name="AiRequestCount">The ai request count parameter.</param>
/// <returns>The result. Jahre: The jahre parameter.</returns>
public record UserStatsDto(
    string TimeSinceRegistration, // z.B. "2 Jahre, 3 Monate"
    int CookbookCount,
    int OwnRecipeCount,
    int AiRequestCount
);
