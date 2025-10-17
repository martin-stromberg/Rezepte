namespace Rezepte.Web.Dto;
public record UserStatsDto(
    string TimeSinceRegistration, // z.B. "2 Jahre, 3 Monate"
    int CookbookCount,
    int OwnRecipeCount,
    int AiRequestCount
);